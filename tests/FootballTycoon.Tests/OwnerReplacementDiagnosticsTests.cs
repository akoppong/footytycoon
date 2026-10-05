extern alias Endurance;
using Xunit;
using Diagnostic = Endurance::FootballTycoon.Core;
using Probe = Endurance::FootballTycoon.Endurance;

namespace FootballTycoon.Tests;

public class OwnerReplacementDiagnosticsTests
{
    private static Diagnostic.World Opening(bool shortage = true)
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        if (shortage)
        {
            // Controlled pre-career expiry: retain the person, remove the unsigned wage schedule.
            var keeper = world.OwnedClub.Players.First(p => p.Role == Diagnostic.Role.Goalkeeper);
            world.OwnedClub.Players.Remove(keeper);
            world.FreeAgents.Add(new(keeper with { ContractEndWeek = 0 }, world.OwnedClubId, 0));
            world.RetirementNotices.RemoveAll(n => n.PlayerId == keeper.Id);
            world.Obligations = world.Obligations.Where(o => o.Description != $"Player contract {keeper.ContractId.Value}")
                .Select((o, i) => o with { Id = new(i + 1) }).ToList();
        }
        foreach (var allocation in new[] { Diagnostic.Allocation.Acquire, Diagnostic.Allocation.PreserveReserve })
            Diagnostic.Proposals.Commit(world, allocation.ToString(), world.Revision, Diagnostic.Proposals.Preview(world, new(allocation), world.Revision));
        Diagnostic.WorldFactory.Validate(world);
        return world;
    }

    [Fact]
    public void DisabledHealthyPendingAndBoundaryStatesDoNotChangeTheWorld()
    {
        void Unchanged(Diagnostic.World world, Probe.OwnerRecruitmentMode mode)
        {
            var before = Diagnostic.WorldCodec.Encode(world);
            Assert.Null(Probe.DiagnosticOwnerRecruitment.TryApprove(world, mode));
            Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
        }
        Unchanged(Opening(), Probe.OwnerRecruitmentMode.None);
        Unchanged(Opening(false), Probe.OwnerRecruitmentMode.CoverShortages);
        var pending = Opening();
        Assert.Equal("Approved", Probe.DiagnosticOwnerRecruitment.TryApprove(pending, Probe.OwnerRecruitmentMode.CoverShortages)!.Outcome);
        Unchanged(pending, Probe.OwnerRecruitmentMode.CoverShortages);
        var boundary = Opening(); boundary.Week = 51;
        Unchanged(boundary, Probe.OwnerRecruitmentMode.CoverShortages);
        boundary.Week = 52; boundary.Status = Diagnostic.CareerStatus.SeasonReview;
        Unchanged(boundary, Probe.OwnerRecruitmentMode.CoverShortages);
    }

    [Fact]
    public void ApprovalRetainsExactDirectorTermsAndReplaysWithoutInventingPeopleOrMoney()
    {
        var world = Opening();
        var identities = world.Clubs.SelectMany(c => c.Players).Select(p => p.Id).Concat(world.FreeAgents.Select(f => f.Player.Id)).ToHashSet();
        var quote = Diagnostic.Proposals.Preview(world, new(Diagnostic.Allocation.FreeAgentRecruitment), world.Revision);
        var cash = world.OwnedClub.Cash; var reserve = world.OwnerCash; var debts = world.Obligations.ToArray();
        var decision = Probe.DiagnosticOwnerRecruitment.TryApprove(world, Probe.OwnerRecruitmentMode.CoverShortages)!;
        Assert.Equal("Approved", decision.Outcome); Assert.Empty(decision.BlockingReasons);
        Assert.Equal(quote.Recruitment!.Player.Id.Value, decision.PlayerId);
        Assert.Equal(quote.Recruitment, world.History.Last().Recruitment);
        Assert.Equal(quote.Command, world.History.Last().Command);
        Assert.False(world.History.Last().Command.ReserveException);
        Assert.Single(world.FreeAgentBids);
        Assert.Equal(debts, world.Obligations); Assert.Equal(cash, world.OwnedClub.Cash); Assert.Equal(reserve, world.OwnerCash);
        var loaded = Diagnostic.WorldCodec.Clone(world);
        for (var i = 0; i < 3; i++) { Diagnostic.Simulation.AdvanceWeek(world); Diagnostic.Simulation.AdvanceWeek(loaded); }
        Assert.Equal(Diagnostic.WorldCodec.Encode(world), Diagnostic.WorldCodec.Encode(loaded));
        Probe.EnduranceRun.VerifyPeople(world, identities);
        Assert.Empty(world.FreeAgentBids);
        Assert.Equal(reserve, world.OwnerCash);
    }

    [Fact]
    public void BlockedAndMissingRecommendationsAreReportedWithoutMutationOrReserveExceptions()
    {
        var world = Opening();
        Diagnostic.Finance.Post(world, Diagnostic.WorldFactory.Account(world.OwnedClubId), -world.OwnedClub.Cash, Diagnostic.CashKind.Operations, "Controlled empty cash");
        var before = Diagnostic.WorldCodec.Encode(world);
        var blocked = Probe.DiagnosticOwnerRecruitment.TryApprove(world, Probe.OwnerRecruitmentMode.CoverShortages)!;
        Assert.Equal("Blocked", blocked.Outcome); Assert.NotEmpty(blocked.BlockingReasons); Assert.NotNull(blocked.PlayerId);
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
        world.FreeAgents.Clear(); before = Diagnostic.WorldCodec.Encode(world);
        var missing = Probe.DiagnosticOwnerRecruitment.TryApprove(world, Probe.OwnerRecruitmentMode.CoverShortages)!;
        Assert.Equal("NoRecommendation", missing.Outcome); Assert.Null(missing.PlayerId);
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
    }

    [Fact]
    public void TotalSquadGapsDoNotBypassTheOrdinaryOutsideWindowRoleRule()
    {
        var world = Opening(false);
        // Isolate a 15-person read-only snapshot with every role minimum met.
        world.OwnedClub.Players = world.OwnedClub.Players.GroupBy(p => p.Role)
            .SelectMany(g => g.Take(Diagnostic.Contracts.Minimum.Single(m => m.Role == g.Key).Minimum)).ToList();
        world.Week = 3;
        Assert.Equal(15, world.OwnedClub.Players.Count);
        Assert.All(Enum.GetValues<Diagnostic.Role>(), r => Assert.False(Diagnostic.FreeAgents.HasShortage(world.OwnedClub, r)));
        var before = Diagnostic.WorldCodec.Encode(world);
        var decision = Probe.DiagnosticOwnerRecruitment.TryApprove(world, Probe.OwnerRecruitmentMode.CoverShortages)!;
        Assert.Equal("NoRecommendation", decision.Outcome);
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
    }

    [Fact]
    public void WeeklyCoverCountsPartitionClubsWithoutDoubleCountingMultipleRoleGaps()
    {
        var world = Opening();
        var before = Diagnostic.WorldCodec.Encode(world);
        Assert.Equal((1, 0), Probe.EnduranceRun.CountShortClubs(world));
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
        foreach (var club in world.Clubs.Where(c => c.Id != world.OwnedClubId).Take(2)) club.Players.Clear();
        Assert.Equal((1, 2), Probe.EnduranceRun.CountShortClubs(world));
    }

    [Fact]
    public void RunnerExecutesTheSelectedPolicyAndAccountsForItsRecordedApprovals()
    {
        var metrics = new StringWriter();
        var result = Probe.EnduranceRun.Execute(new(6, 2026, Diagnostic.Allocation.PreserveReserve, TimeSpan.FromMinutes(10), Probe.OwnerRecruitmentMode.CoverShortages), metrics, new StringWriter());
        Assert.Equal("Completed", result.Outcome); Assert.Equal(6, result.CompletedSeasons);
        Assert.True(result.OwnerRecruitment.Approved > 0);
        Assert.Equal(result.ClubWeeksBelowCover, result.OwnedWeeksBelowCover + result.RivalClubWeeksBelowCover);
        var vacancies = result.RivalVacancies;
        Assert.Equal(result.RivalClubWeeksBelowCover, vacancies.ObservedClubWeeks);
        Assert.Equal(vacancies.ObservedClubWeeks, vacancies.RoleGapClubWeeks + vacancies.TotalOnlyClubWeeks);
        Assert.Equal(vacancies.ObservedClubWeeks, vacancies.GateClubWeeks.Values.Sum());
        Assert.Equal(vacancies.ObservedClubWeeks, vacancies.Episodes.Sum(e => e.Weeks));
        var events = metrics.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => System.Text.Json.Nodes.JsonNode.Parse(line)!).ToArray();
        Assert.Equal("CoverShortages", events[0]["OwnerRecruitment"]!.GetValue<string>());
        var decisions = events.Where(e => e["Kind"]!.GetValue<string>() == "owner-recruitment").Select(e => e["Decision"]!).ToArray();
        Assert.Equal(result.OwnerRecruitment.Approved, decisions.Count(d => d["Outcome"]!.GetValue<string>() == "Approved"));
        Assert.Equal(result.OwnerRecruitment.Blocked, decisions.Count(d => d["Outcome"]!.GetValue<string>() == "Blocked"));
        Assert.Equal(result.OwnerRecruitment.NoRecommendation, decisions.Count(d => d["Outcome"]!.GetValue<string>() == "NoRecommendation"));
        Assert.True(events.Last()["OwnedSignings"]!.GetValue<int>() > 0);
        var observations = events.Where(e => e["Kind"]!.GetValue<string>() == "rival-vacancy").Select(e => e["Vacancy"]!).ToArray();
        Assert.Equal(vacancies.ObservedClubWeeks, observations.Length);
        Assert.Equal(observations.Length, observations.Select(e => (e["ClubId"]!.GetValue<int>(), e["Week"]!.GetValue<int>())).Distinct().Count());
        Assert.All(observations, e => Assert.NotEqual(events[0]["OwnedClubId"]!.GetValue<int>(), e["ClubId"]!.GetValue<int>()));
        Assert.All(vacancies.GateClubWeeks, gate => Assert.Equal(gate.Value, observations.Count(e => e["Gate"]!.GetValue<string>() == gate.Key)));
    }

    [Fact]
    public void UnsupportedPoliciesAreRejectedBeforeWorkStarts()
    {
        Assert.Throws<ArgumentException>(() => new Probe.RunOptions(1, 2026, Diagnostic.Allocation.PreserveReserve, TimeSpan.FromMinutes(1), (Probe.OwnerRecruitmentMode)99).Validate());
        var world = Opening(); var before = Diagnostic.WorldCodec.Encode(world);
        Assert.Throws<ArgumentOutOfRangeException>(() => Probe.DiagnosticOwnerRecruitment.TryApprove(world, (Probe.OwnerRecruitmentMode)99));
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
    }
}
