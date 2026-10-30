using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class MandateEvidenceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    private static void Change(World world, ManagerMandate manager, DirectorMandate director)
        => MandateCommands.Commit(world, MandateCommands.Preview(world, new($"mandate-{world.Mandates.Revision + 1}", world.Revision, manager, director)));

    private static World Available(bool accepted = true, int week = 2)
    {
        var (world, _, _) = MinimumSquadFixture.Create(15, week, owned: true);
        // Fix the controlled fixture's stream before approval, not a playable experiment or outcome reroll.
        while ((RandomStreams.Next(WorldCodec.Clone(world), $"free-agent-negotiation/{world.History.Count + 1}", 100) < 70) != accepted) world.Seed++;
        return world;
    }

    private static Proposal Approve(World world)
    {
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Empty(quote.BlockingReasons);
        Proposals.Commit(world, "approach", world.Revision, quote);
        return quote;
    }

    private static World Legacy(World world)
    {
        var json = JsonSerializer.SerializeToNode(world)!.AsObject();
        json["SchemaVersion"] = 23; json["SimulationVersion"] = "compact-journal-23";
        foreach (var key in new[] { "Mandates", "MandateHistory", "MandateSelections", "MandateApproaches" }) json.Remove(key);
        return WorldCodec.Decode(Encoding.UTF8.GetBytes(json.ToJsonString()));
    }

    [Fact]
    public async Task ApprovalRetainsItsOriginAcrossSameWeekChangeSaveLoadAndResolution()
    {
        var world = Available(); var vault = new LocalSaveVault(directory);
        var saved = vault.Write(WorldCodec.Encode(world), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "start");
        await using var session = new GameSession(vault); await session.LoadAsync(saved.SnapshotId);
        var before = await session.ExportCheckpointAsync();
        var view = await session.QueryAsync();
        Assert.Equal(view.FreeAgentRecommendation!.Player.Id, view.FreeAgentRecommendationEvidence!.Selected.PersonId);
        Assert.Equal(before, await session.ExportCheckpointAsync());
        var quote = await session.PreviewAsync(new(Allocation.FreeAgentRecruitment), view.Revision);
        var receipt = await session.CommitAsync("approach", view.Revision, quote);
        var approved = WorldCodec.Decode(await session.ExportCheckpointAsync());
        var original = Assert.Single(approved.MandateApproaches).Recommendation!;
        Assert.Equal(view.Revision, original.DecisionWorldRevision); Assert.Null(approved.MandateApproaches[0].Outcome);
        var mandates = await session.PreviewMandatesAsync(new("switch", receipt.Revision, ManagerMandate.Development, DirectorMandate.LowerCostValue));
        await session.CommitMandatesAsync(mandates);
        var pending = await session.SaveAsync("pending"); await session.LoadAsync(pending.SnapshotId);
        await session.AdvanceAsync(AdvanceTarget.Week);
        var resolved = WorldCodec.Decode(await session.ExportCheckpointAsync());
        var approach = Assert.Single(resolved.MandateApproaches);
        Assert.Equal(original, approach.Recommendation); Assert.Equal(original.Origin, approach.Outcome!.Origin);
        Assert.Equal(ApproachOutcome.Signed, approach.Outcome.Outcome); Assert.Equal(0, approach.Outcome.Origin!.Revision);
        Assert.Equal(1, resolved.Mandates.Revision);
        Assert.Equal(quote.Recruitment, resolved.History.Last().Recruitment);
        var report = MandateReports.Season(resolved, 1);
        var contract = Assert.Single(report.Approaches);
        Assert.NotNull(contract.SignedWages); Assert.Equal(0, contract.WagesPaid); Assert.False(contract.Pending);
        Assert.Empty(contract.WagePayments);
        var savedOutcome = await session.SaveAsync("outcome"); await session.LoadAsync(savedOutcome.SnapshotId);
        Assert.Equal(approach, (await session.QueryAsync()).Mandates.Approaches.Single());
        Assert.Equal(WorldCodec.Encode(world), vault.Read(saved.SnapshotId).WorldBytes);
    }

    [Theory]
    [InlineData("unavailable", "The player is no longer available.")]
    [InlineData("deadline", "The approved approach expired.")]
    [InlineData("inactive", "The club is not in active ownership.")]
    [InlineData("squad", "The squad has no vacant place.")]
    [InlineData("retirement", "The player has reached retirement age.")]
    [InlineData("role", "The role is no longer eligible under window and squad-cover rules.")]
    [InlineData("wages", "The approved wage would exceed the annual wage limit.")]
    [InlineData("arrears", "The club has unpaid obligations.")]
    [InlineData("reserve", "The approved contract would breach the required cash reserve.")]
    [InlineData("refusal", "The player declined the approved contract.")]
    public void ActualOwnerResolverRecordsFirstBindingReasonWithoutAnAcceptanceDrawForFailedChecks(string failure, string reason)
    {
        var world = Available(accepted: failure != "refusal", week: failure == "role" ? 3 : 2);
        Approve(world); var bid = Assert.Single(world.FreeAgentBids); world.Week++;
        var free = world.FreeAgents.Single(f => f.Player.Id == bid.PlayerId);
        // Guard-isolation fixtures intentionally stress inputs at execution. They are not playable checkpoints.
        if (failure == "unavailable") { world.FreeAgents.Remove(free); world.FreeAgentBids[0] = bid with { ExpiryWeek = world.Week - 1 }; }
        if (failure == "deadline") { world.FreeAgentBids[0] = bid with { ExpiryWeek = world.Week - 1 }; world.Status = CareerStatus.Administration; }
        if (failure == "inactive") world.Status = CareerStatus.Administration;
        if (failure is "squad" or "role")
        {
            var needed = failure == "squad" ? 22 : 16;
            foreach (var rival in world.Clubs.Where(c => c.Id != world.OwnedClubId))
            {
                if (world.OwnedClub.Players.Count >= needed) break;
                var player = rival.Players.First(); rival.Players.Remove(player); world.OwnedClub.Players.Add(player);
            }
        }
        if (failure == "retirement") world.FreeAgents[world.FreeAgents.IndexOf(free)] = free with { Player = free.Player with { Age = FreeAgents.RetirementAge(free.Player.Role) } };
        if (failure == "wages") world.OwnedClub.Players[0] = world.OwnedClub.Players[0] with { WeeklyWage = 1000000000 };
        if (failure == "arrears") world.Arrears.Add(new(world.Obligations.First(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages).Id, world.OwnedClubId, world.Week - 1, 10000));
        if (failure is "reserve" or "arrears") world.ReserveTarget = world.OwnedClub.Cash * 2;
        var cash = world.OwnedClub.Cash; var obligations = world.Obligations.ToArray(); var journal = world.Journal.ToArray();
        var random = world.RandomStates.OrderBy(p => p.Key).ToArray();
        // Isolate the actual resolver from settlement/rival decisions so a check cannot be cleared by an earlier phase.
        typeof(FreeAgents).GetMethod("ResolveOwner", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [world]);
        var outcome = Assert.Single(world.MandateApproaches).Outcome!;
        Assert.Equal(reason, outcome.Reason); Assert.Contains(reason, world.Reviews.Last().Evidence);
        Assert.Equal(failure == "unavailable" ? ApproachOutcome.Unavailable : failure == "refusal" ? ApproachOutcome.Declined : ApproachOutcome.ChecksFailed, outcome.Outcome);
        Assert.Null(outcome.ContractId); Assert.Null(outcome.WageObligationId); Assert.Empty(world.FreeAgentBids);
        Assert.Equal(cash, world.OwnedClub.Cash); Assert.Equal(obligations, world.Obligations); Assert.Equal(journal, world.Journal);
        if (failure == "refusal") Assert.Contains($"free-agent-negotiation/{bid.DecisionId}", world.RandomStates.Keys);
        else Assert.Equal(random, world.RandomStates.OrderBy(p => p.Key));
    }

    [Fact]
    public void SelectionSnapshotsDescribeExecutedLineupAndReportsKeepSameWeekChangesDistinct()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.PreserveReserve);
        world.OwnedClub.Players = world.OwnedClub.Players.Select(p => p with { Age = 30, Ability = 70 }).ToList();
        var prospect = world.OwnedClub.Players.Where(p => p.Role == Role.Forward).OrderBy(p => p.Id.Value).ElementAt(2);
        world.OwnedClub.Players[world.OwnedClub.Players.IndexOf(prospect)] = prospect with { Age = 20, Ability = 68 };
        Change(world, ManagerMandate.Development, DirectorMandate.CurrentRecommendation);
        while (world.MandateSelections.Count == 0) Simulation.AdvanceWeek(world);
        var evidence = world.MandateSelections.Single();
        Assert.Single(evidence.Swaps); Assert.Equal(68, evidence.Swaps[0].Selected.Ability);
        var played = world.Results.Single(r => r.FixtureId == evidence.FixtureId);
        Assert.Contains(played.Home == world.OwnedClubId ? played.HomeLineup : played.AwayLineup, a => a.Player == prospect.Id);
        Change(world, ManagerMandate.Readiness, DirectorMandate.LowerCostValue);
        var encoded = WorldCodec.Encode(world);
        var report = MandateReports.Season(world, 1);
        Assert.Equal(encoded, WorldCodec.Encode(world)); Assert.Equal(2, report.Changes.Length);
        var period = Assert.Single(report.ObservedPeriods);
        Assert.Equal(1, period.Origin.Revision); Assert.Equal(evidence.DecisionWorldRevision, period.FirstDecisionRevision);
        Assert.Equal(1, period.DevelopmentSwaps); Assert.Equal(0, report.MatchesWithoutOrigin);
        Assert.Equal(1, report.Appearances.Single(a => a.PersonId == prospect.Id).DevelopmentSelections);
        var clone = WorldCodec.Clone(world);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(clone);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(clone));
        Assert.Equal(evidence, world.MandateSelections[0]);
    }

    [Fact]
    public void LegacyCompletedSigningNeverBecomesAZeroPaymentClaimOrBackfilledMandate()
    {
        var world = Available(); Approve(world); Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(world);
        var original = world.MandateApproaches.Single(); Assert.Equal(ApproachOutcome.Signed, original.Outcome!.Outcome);
        var paid = MandateReports.Season(world, 1).Approaches.Single(); Assert.True(paid.WagesPaid > 0);
        world = Legacy(world); var bytes = WorldCodec.Encode(world);
        var legacy = MandateReports.Season(world, 1).Approaches.Single();
        Assert.Null(legacy.Evidence); Assert.Null(legacy.SignedWages); Assert.Null(legacy.WagesPaid); Assert.False(legacy.Pending);
        Assert.Empty(legacy.WagePayments); Assert.Equal(bytes, WorldCodec.Encode(world));
        Assert.Equal(1, MandateReports.Season(world, 1).ApprovalsWithoutOrigin);
    }

    [Fact]
    public void WageReportUsesOnlyOriginalObligationAndArrearReferencesAndKeepsBoundaryCashSeparate()
    {
        var world = Available(); Approve(world); Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(world);
        var obligation = world.Obligations.Single(o => o.Id == world.MandateApproaches.Single().Outcome!.WageObligationId);
        Finance.Post(world, WorldFactory.Account(world.OwnedClubId), -1234, CashKind.Wages, $"arrear/{obligation.Id.Value}/{world.Week - 1}");
        Finance.Post(world, WorldFactory.Account(world.OwnedClubId), -5678, CashKind.Wages, $"obligation/{obligation.Id.Value}0/{world.Week}");
        var before = WorldCodec.Encode(world); var report = MandateReports.Season(world, 1); var approach = report.Approaches.Single();
        Assert.Equal(-obligation.WeeklyAmount + 1234, approach.WagesPaid);
        Assert.Equal(2, approach.WagePayments.Length); Assert.Equal(before, WorldCodec.Encode(world));
        Assert.Contains("cannot establish", report.Interpretation);
        Assert.Contains("original accounting boundary", report.Interpretation);
    }
}
