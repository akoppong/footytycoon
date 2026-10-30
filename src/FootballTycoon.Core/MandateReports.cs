using System.Collections.Immutable;

namespace FootballTycoon.Core;

// Computed views only: no additional saved state or financial ledger.
public sealed record ObservedMandatePeriod(MandateOrigin Origin, int FirstDecisionWeek, int LastDecisionWeek,
    long FirstDecisionRevision, long LastDecisionRevision, int Matches, int DevelopmentSwaps, int Approvals);
public sealed record MandateAppearanceReport(PersonId PersonId, string Name, int Starts, int StartsWithKnownMandate,
    int DevelopmentSelections, PlayerProgress? Development);
public sealed record MandateApproachReport(string ForecastId, int ApprovalWeek, bool ApprovedThisSeason, bool Pending,
    RecruitmentTerms ApprovedTerms, MandateApproach? Evidence, Obligation? SignedWages,
    ImmutableArray<LedgerEntry> WagePayments, long? WagesPaid);
public sealed record MandateSeasonReport(int Season, int StartWeek, int ThroughWeek, bool Complete,
    ImmutableArray<MandateChange> Changes, ImmutableArray<ObservedMandatePeriod> ObservedPeriods,
    ImmutableArray<MandateAppearanceReport> Appearances, ImmutableArray<MandateApproachReport> Approaches,
    int MatchesWithoutOrigin, int ApprovalsWithoutOrigin, SeasonSummary? Summary,
    ImmutableArray<LedgerEntry> CashMovements, string Interpretation);

public static class MandateReports
{
    public static ImmutableArray<MandateSeasonReport> All(World world) => Enumerable.Range(1, world.Season)
        .Select(season => Season(world, season)).ToImmutableArray();

    public static MandateSeasonReport Season(World world, int season)
    {
        if (season < 1 || season > world.Season) throw new ArgumentOutOfRangeException(nameof(season));
        var start = (season - 1) * Seasons.Weeks;
        var end = Math.Min(world.Week, season * Seasons.Weeks);
        var summary = world.SeasonSummaries.SingleOrDefault(s => s.Season == season);
        var names = PlayerHistory.Names(world);
        var matches = world.Results.Where(r => r.Week > start && r.Week <= end
            && (r.Home == world.OwnedClubId || r.Away == world.OwnedClubId)).ToArray();
        var selections = world.MandateSelections.Where(s => matches.Any(r => r.FixtureId == s.FixtureId)).ToDictionary(s => s.FixtureId);
        var lines = world.Journal.Where(j => j.Account == WorldFactory.Account(world.OwnedClubId) && j.Week > start && j.Week <= end).ToImmutableArray();
        var approaches = world.History.Where(h => h.Command.Allocation == Allocation.FreeAgentRecruitment && h.Week < season * Seasons.Weeks && h.Week <= end)
            .Select(h =>
            {
                var evidence = world.MandateApproaches.SingleOrDefault(a => a.ForecastId == h.OriginalForecast.Id);
                var pending = world.FreeAgentBids.Any(b => b.ForecastId == h.OriginalForecast.Id);
                var obligation = evidence?.Outcome?.WageObligationId is { } id ? world.Obligations.Single(o => o.Id == id) : null;
                // Match the original obligation ID with a delimiter, never a person's current contract or a quote.
                var paid = obligation is null ? [] : lines.Where(j => j.Kind == CashKind.Wages
                    && (j.Reference.StartsWith($"obligation/{obligation.Id.Value}/", StringComparison.Ordinal)
                        || j.Reference.StartsWith($"arrear/{obligation.Id.Value}/", StringComparison.Ordinal))).ToImmutableArray();
                return new MandateApproachReport(h.OriginalForecast.Id, h.Week, h.Week >= start, pending,
                    h.Recruitment!, evidence, obligation, paid, obligation is not null ? -paid.Sum(j => j.Amount)
                        : pending || evidence?.Outcome is not null ? 0 : null);
            }).Where(a => a.ApprovedThisSeason || !a.WagePayments.IsEmpty
                || a.SignedWages is { } wage && wage.StartWeek <= end && wage.EndWeek > start).ToImmutableArray();
        var observed = selections.Values.Select(s => (s.Origin, Week: matches.Single(r => r.FixtureId == s.FixtureId).Week,
            Revision: s.DecisionWorldRevision, Matches: 1, Swaps: s.Swaps.Length, Approvals: 0))
            .Concat(approaches.Where(a => a.ApprovedThisSeason && a.Evidence?.Recommendation is not null).Select(a =>
            {
                var r = a.Evidence!.Recommendation!;
                return (r.Origin, r.Week, Revision: r.DecisionWorldRevision, Matches: 0, Swaps: 0, Approvals: 1);
            })).GroupBy(d => d.Origin).OrderBy(g => g.Key.Revision)
            .Select(g => new ObservedMandatePeriod(g.Key, g.Min(d => d.Week), g.Max(d => d.Week),
                g.Min(d => d.Revision), g.Max(d => d.Revision), g.Sum(d => d.Matches), g.Sum(d => d.Swaps), g.Sum(d => d.Approvals))).ToImmutableArray();
        var appearances = matches.SelectMany(r => (r.Home == world.OwnedClubId ? r.HomeLineup : r.AwayLineup)
            .Select(a => (a.Player, Known: selections.ContainsKey(r.FixtureId),
                Development: selections.TryGetValue(r.FixtureId, out var selection) && selection.Swaps.Any(s => s.Selected.PersonId == a.Player))))
            .GroupBy(a => a.Player).OrderBy(g => g.Key.Value)
            .Select(g => new MandateAppearanceReport(g.Key, names.GetValueOrDefault(g.Key, $"Player {g.Key.Value}"), g.Count(),
                g.Count(a => a.Known), g.Count(a => a.Development), summary?.Development.SingleOrDefault(d => d.PlayerId == g.Key))).ToImmutableArray();
        return new(season, start, end, summary is not null,
            world.MandateHistory.Where(h => h.Week >= start && h.Week <= end).ToImmutableArray(), observed, appearances, approaches,
            matches.Count(r => !selections.ContainsKey(r.FixtureId)), approaches.Count(a => a.ApprovedThisSeason && a.Evidence?.Recommendation is null),
            summary, lines,
            "Observed periods cover recorded decisions only; missing historical origins stay unknown. Same-week changes are ordered by world revision. "
            + "Boundary-week changes can appear in adjacent season reports. Cash movements here cover played weeks after the season boundary; the saved season summary retains its original accounting boundary. "
            + "Approvals, completed signings and paid wages are distinct; null wages mean missing historical payment attribution, not zero spending. Shorter commitments also mean less contracted cover. "
            + "Appearances and development are measured facts; a result or growth roll cannot establish that a mandate caused success.");
    }
}
