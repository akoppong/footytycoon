using System.Collections.Immutable;

namespace FootballTycoon.Core;

public enum ManagerMandate { Readiness, Development }
public enum DirectorMandate { CurrentRecommendation, LowerCostValue }
public enum RecommendationReason { Current, LowerCostAlternative, NoLowerCostAlternative }

public sealed record MandateState(ManagerMandate Manager, DirectorMandate Director, long Revision)
{
    public static MandateState Default { get; } = new(ManagerMandate.Readiness, DirectorMandate.CurrentRecommendation, 0);
}
public sealed record MandateOrigin(ManagerMandate Manager, DirectorMandate Director, long Revision);
public sealed record MandateChange(string CommandId, int Week, long WorldRevision, MandateState Before, MandateState After);

// E05 implements transactions. The command ID shares World.Commands with investment commands.
public sealed record ChangeMandates(string CommandId, long ExpectedRevision, ManagerMandate Manager, DirectorMandate Director);
public sealed record MandateProposal(string Id, string WorldFingerprint, ChangeMandates Command, MandateState Before,
    ImmutableArray<string> PendingCommitments, ImmutableArray<string> BlockingReasons, string Effect);

public sealed record SelectionPlayer(PersonId PersonId, Role Role, int Age, int Ability);
public sealed record SelectionSwap(SelectionPlayer Displaced, SelectionPlayer Selected, int AbilitySacrifice);
// DecisionWorldRevision is the world revision when the pure decision was computed, before publication.
public sealed record SelectionEvidence(FixtureId FixtureId, long DecisionWorldRevision, MandateOrigin Origin,
    ImmutableArray<SelectionSwap> Swaps);
public sealed record RecommendationCandidate(PersonId PersonId, Role Role, int Age, int Ability, long WeeklyWage,
    int WageStartWeek, int ContractEndWeek, long TotalCommitment);
public sealed record RecommendationEvidence(int Week, long DecisionWorldRevision, MandateOrigin Origin,
    RecommendationCandidate Baseline, RecommendationCandidate Selected, RecommendationReason Reason);
public sealed record ApproachOutcomeEvidence(MandateOrigin? Origin, PersonId PersonId, int ResolvedWeek,
    ApproachOutcome Outcome, string Reason, ContractId? ContractId, ObligationId? WageObligationId);
// No recommendation is invented for a legacy approval. An eventual outcome can retain a null origin.
public sealed record MandateApproach(string ForecastId, RecommendationEvidence? Recommendation, ApproachOutcomeEvidence? Outcome);
public sealed record MandateView(MandateState Current, ImmutableArray<MandateChange> History,
    ImmutableArray<SelectionEvidence> Selections, ImmutableArray<MandateApproach> Approaches,
    ImmutableArray<MatchResult> HistoricalMatches);

public static class OwnerMandates
{
    public static MandateView View(World world) => new(world.Mandates, world.MandateHistory.ToImmutableArray(),
        world.MandateSelections.ToImmutableArray(), world.MandateApproaches.ToImmutableArray(),
        world.Results.Where(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId).ToImmutableArray());

    internal static void Validate(World world)
    {
        if (world.Mandates is null || world.MandateHistory is null || world.MandateSelections is null || world.MandateApproaches is null)
            Fail("Missing mandate state.");
        CheckState(world.Mandates!);
        var current = MandateState.Default;
        long lastRevision = -1;
        var lastWeek = 0;
        var commands = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in world.MandateHistory!)
        {
            if (change is null || change.Before is null || change.After is null) Fail("Missing mandate transition.");
            CheckState(change!.Before); CheckState(change.After);
            if (string.IsNullOrWhiteSpace(change.CommandId) || change.CommandId.Length > 128 || !commands.Add(change.CommandId)
                || change.Before != current || change.After.Revision != current.Revision + 1
                || change.After.Manager == current.Manager && change.After.Director == current.Director
                || change.WorldRevision <= lastRevision || change.WorldRevision <= 0 || change.WorldRevision > world.Revision
                || change.Week < lastWeek || change.Week > world.Week
                || !world.Commands.TryGetValue(change.CommandId, out var receipt) || receipt is null || receipt.CommandId != change.CommandId
                || receipt.Revision != change.WorldRevision || string.IsNullOrWhiteSpace(receipt.ProposalId))
                Fail("Invalid mandate transition or shared receipt.");
            current = change.After; lastRevision = change.WorldRevision; lastWeek = change.Week;
        }
        if (current != world.Mandates) Fail("Mandate history does not reach the current state.");
        if (world.MandateSelections!.Count == 0 && world.MandateApproaches!.Count == 0) return;

        var people = PlayerHistory.Names(world).Keys.ToHashSet();
        var fixtures = world.Fixtures.ToDictionary(f => f.Id);
        var results = world.Results.ToDictionary(r => r.FixtureId);
        var selections = new HashSet<FixtureId>();
        foreach (var selection in world.MandateSelections!)
        {
            if (selection is null || !selections.Add(selection.FixtureId) || !fixtures.TryGetValue(selection.FixtureId, out var fixture)
                || !results.TryGetValue(selection.FixtureId, out var result)
                || fixture.Home != world.OwnedClubId && fixture.Away != world.OwnedClubId
                || fixture.Week > world.Week || selection.Swaps.IsDefault || selection.Swaps.Length > 2)
                Fail("Invalid mandate match reference.");
            var played = results[selection!.FixtureId];
            CheckOrigin(selection.Origin, selection.DecisionWorldRevision, fixtures[selection.FixtureId].Week);
            if (selection.Origin.Manager == ManagerMandate.Readiness && !selection.Swaps.IsEmpty) Fail("Readiness cannot record development swaps.");
            var lineup = (played.Home == world.OwnedClubId ? played.HomeLineup : played.AwayLineup);
            var used = new HashSet<PersonId>();
            foreach (var swap in selection.Swaps)
            {
                if (swap is null || swap.Displaced is null || swap.Selected is null) Fail("Missing selection snapshot.");
                CheckPlayer(swap!.Displaced); CheckPlayer(swap.Selected);
                if (!used.Add(swap.Displaced.PersonId) || !used.Add(swap.Selected.PersonId)
                    || swap.Displaced.Role != swap.Selected.Role || swap.Selected.Role == Role.Goalkeeper
                    || swap.Displaced.Age <= 23 || swap.Selected.Age > 23 || swap.AbilitySacrifice is < 0 or > 5
                    || swap.Displaced.Ability - swap.Selected.Ability != swap.AbilitySacrifice
                    || lineup.Any(p => p.Player == swap.Displaced.PersonId)
                    || !lineup.Any(p => p.Player == swap.Selected.PersonId && p.Position == swap.Selected.Role))
                    Fail("Invalid development swap evidence.");
            }
        }

        var approaches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var approach in world.MandateApproaches!)
        {
            if (approach is null || string.IsNullOrWhiteSpace(approach.ForecastId) || !approaches.Add(approach.ForecastId)
                || approach.Recommendation is null && approach.Outcome is null) Fail("Invalid mandate approach identity.");
            var approvals = world.History.Where(h => h.OriginalForecast.Id == approach!.ForecastId).ToArray();
            if (approvals.Length != 1 || approvals[0].Command.Allocation != Allocation.FreeAgentRecruitment
                || approvals[0].Recruitment is not { IsFreeAgent: true }) Fail("Missing free-agent approval.");
            var approval = approvals[0]; var terms = approval.Recruitment!;
            if (approach!.Recommendation is { } recommendation)
            {
                CheckOrigin(recommendation.Origin, recommendation.DecisionWorldRevision, recommendation.Week);
                CheckCandidate(recommendation.Baseline, recommendation.Week); CheckCandidate(recommendation.Selected, recommendation.Week);
                var b = recommendation.Baseline; var s = recommendation.Selected;
                if (recommendation.Week != approval.Week || !Enum.IsDefined(recommendation.Reason)
                    || s.PersonId != terms.Player.Id || s.Role != terms.Player.Role || s.Age != terms.Player.Age || s.Ability != terms.Player.Ability
                    || s.WeeklyWage != terms.WeeklyWage || s.ContractEndWeek != terms.ContractEndWeek || b.Role != s.Role)
                    Fail("Recommendation disagrees with the approved terms.");
                if (recommendation.Reason == RecommendationReason.LowerCostAlternative)
                {
                    if (recommendation.Origin.Director != DirectorMandate.LowerCostValue || s.PersonId == b.PersonId
                        || s.Ability < b.Ability - 5 || s.WeeklyWage > b.WeeklyWage || s.TotalCommitment > b.TotalCommitment
                        || s.WeeklyWage == b.WeeklyWage && s.TotalCommitment == b.TotalCommitment) Fail("Invalid lower-cost evidence.");
                }
                else if (b != s || (recommendation.Reason == RecommendationReason.Current
                    ? recommendation.Origin.Director != DirectorMandate.CurrentRecommendation
                    : recommendation.Origin.Director != DirectorMandate.LowerCostValue)) Fail("Invalid recommendation fallback.");
            }
            if (approach.Outcome is { } outcome)
            {
                if (outcome.Origin != approach.Recommendation?.Origin || outcome.PersonId != terms.Player.Id || !people.Contains(outcome.PersonId)
                    || outcome.ResolvedWeek != approval.Week + 1 || outcome.ResolvedWeek > world.Week
                    || !Enum.IsDefined(outcome.Outcome) || outcome.Outcome == ApproachOutcome.Pending || string.IsNullOrWhiteSpace(outcome.Reason)
                    || world.FreeAgentBids.Any(b => b.ForecastId == approach.ForecastId)) Fail("Invalid approach outcome.");
                if (outcome.Outcome == ApproachOutcome.Signed)
                {
                    if (outcome.ContractId is not { Value: > 0 } contract || outcome.WageObligationId is not { } obligationId
                        || !world.Obligations.Any(o => o.Id == obligationId && o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages
                            && o.Description == $"Player contract {contract.Value}" && o.StartWeek == outcome.ResolvedWeek + 1
                            && o.EndWeek == terms.ContractEndWeek && o.WeeklyAmount == -terms.WeeklyWage)) Fail("Missing signed wage commitment.");
                }
                else if (outcome.ContractId is not null || outcome.WageObligationId is not null) Fail("Closed approaches cannot invent contracts.");
            }
        }

        void CheckOrigin(MandateOrigin origin, long revision, int week)
        {
            if (origin is null || revision < 0 || revision > world.Revision || week < 0 || week > world.Week) Fail("Invalid mandate origin.");
            var transition = world.MandateHistory.LastOrDefault(h => h.WorldRevision <= revision);
            var state = transition?.After ?? MandateState.Default;
            if (world.MandateHistory.Any(h => h.Week < week && h.WorldRevision > revision)
                || transition is not null && transition.Week > week || origin != new MandateOrigin(state.Manager, state.Director, state.Revision))
                Fail("Mandate origin was not current at the decision revision.");
        }
        void CheckPlayer(SelectionPlayer player)
        {
            if (!people.Contains(player.PersonId) || !Enum.IsDefined(player.Role) || player.Age is < 16 or > 100 || player.Ability is < 1 or > 100)
                Fail("Invalid historical selection person.");
        }
        void CheckCandidate(RecommendationCandidate candidate, int week)
        {
            if (candidate is null || !people.Contains(candidate.PersonId) || !Enum.IsDefined(candidate.Role)
                || candidate.Age is < 16 or > 100 || candidate.Ability is < 1 or > 100 || candidate.WeeklyWage <= 0
                || candidate.WageStartWeek != week + 2 || candidate.ContractEndWeek < candidate.WageStartWeek
                || candidate.ContractEndWeek % Seasons.Weeks != 0
                || candidate.TotalCommitment != checked(candidate.WeeklyWage * (candidate.ContractEndWeek - candidate.WageStartWeek + 1)))
                Fail("Invalid recommendation quote snapshot.");
        }
    }
    private static void CheckState(MandateState state)
    {
        if (state is null || !Enum.IsDefined(state.Manager) || !Enum.IsDefined(state.Director) || state.Revision < 0)
            Fail("Invalid current mandate values.");
    }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string message) => throw new InvalidDataException(message);
}
