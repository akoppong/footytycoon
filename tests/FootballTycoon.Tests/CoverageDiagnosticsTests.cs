extern alias Endurance;
using Xunit;
using Diagnostic = Endurance::FootballTycoon.Core;
using Probe = Endurance::FootballTycoon.Endurance;

namespace FootballTycoon.Tests;

public class CoverageDiagnosticsTests
{
    [Fact]
    public void CensusSeparatesAvailableSupplyFromCoverAndDoesNotMutateTheWorld()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        world.FreeAgents.Clear(); // Isolate one over-age available keeper.
        var club = world.OwnedClub;
        var goalkeeper = club.Players.First(p => p.Role == Diagnostic.Role.Goalkeeper);
        club.Players.Remove(goalkeeper);
        world.FreeAgents.Add(new(goalkeeper with { Age = 42 }, club.Id, 0));
        var before = Diagnostic.WorldCodec.Encode(world);
        var sample = Probe.EnduranceRun.Sample(world, "controlled");
        var role = sample.Coverage.Roles.Single(r => r.Role == "Goalkeeper");
        Assert.Equal(95, role.Contracted); Assert.Equal(1, role.Available); Assert.Equal(0, role.AvailableBelowRetirementAge);
        Assert.Equal(1, role.CoverVacancies); Assert.Equal(1, role.ClubsBelowCover);
        var shortage = Assert.Single(sample.Coverage.ShortClubs);
        Assert.True(shortage.Owned); Assert.Equal(0, shortage.SquadVacancies);
        var gap = Assert.Single(shortage.Roles);
        Assert.Equal("Goalkeeper", gap.Role); Assert.Equal(1, gap.Players); Assert.Equal(2, gap.Target);
        Assert.Equal(0, gap.AvailableBelowRetirementAge);
        Assert.Equal(sample.Contracted, sample.Coverage.Roles.Sum(r => r.Contracted));
        Assert.Equal(sample.Available, sample.Coverage.Roles.Sum(r => r.Available));
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(sample.Coverage),
            System.Text.Json.JsonSerializer.Serialize(Probe.CoverageDiagnostics.Sample(world)));
    }

    [Fact]
    public void CohortAccountingFollowsIdentityAcrossClubsPoolAndRetirement()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        var origin = world.Clubs[0]; var other = world.Clubs[1];
        var graduates = origin.Players.Take(3).ToArray();
        foreach (var player in graduates) { world.AcademyGraduates.Add(new(origin.Id, 52, player)); origin.Players.Remove(player); }
        other.Players.Add(graduates[0]);
        world.FreeAgents.Add(new(graduates[1], origin.Id, 104));
        var retired = graduates[2];
        world.Retirements.Add(new(retired.Id, retired.Name, retired.Role, 40, retired.Ability, origin.Id, 104));
        var later = origin.Players[0]; world.AcademyGraduates.Add(new(origin.Id, 104, later));
        var report = Probe.CoverageDiagnostics.Sample(world);
        Assert.Equal(new Probe.GraduateCohort(52, 3, 1, 1, 1), report.Cohorts[0]);
        Assert.Equal(new Probe.GraduateCohort(104, 1, 1, 0, 0), report.Cohorts[1]);
        Assert.Equal(4, report.Roles.Sum(r => r.Graduated)); Assert.Equal(1, report.Roles.Sum(r => r.Retired));
    }

    [Fact]
    public void RecentOutcomesUseResolutionDateAndDoNotCountTheWindowBoundaryOrFuture()
    {
        var world = Diagnostic.WorldFactory.Create(2026); world.Week = 104;
        var club = world.Clubs.First(c => c.Id != world.OwnedClubId);
        var player = club.Players[0]; club.Players.Clear();
        void Offer(Diagnostic.ApproachOutcome outcome, int? resolved) => world.RivalApproaches.Add(
            new(world.RivalApproaches.Count + 1, club.Id, 1, player, 10000, 156, outcome, resolved));
        Offer(Diagnostic.ApproachOutcome.Signed, 52); Offer(Diagnostic.ApproachOutcome.Signed, 53);
        Offer(Diagnostic.ApproachOutcome.Declined, 104); Offer(Diagnostic.ApproachOutcome.Unavailable, 105);
        Offer(Diagnostic.ApproachOutcome.ChecksFailed, 103); Offer(Diagnostic.ApproachOutcome.Pending, null);
        var report = Probe.CoverageDiagnostics.Sample(world);
        var shortage = Assert.Single(report.ShortClubs);
        Assert.False(shortage.Owned); Assert.Equal(16, shortage.SquadVacancies); Assert.Equal(4, shortage.Roles.Length);
        Assert.Equal(1, shortage.RecentSigned); Assert.Equal(1, shortage.RecentDeclined);
        Assert.Equal(0, shortage.RecentUnavailable); Assert.Equal(1, shortage.RecentChecksFailed); Assert.Equal(1, shortage.PendingOffers);
        var totals = report.RivalOffers.Single(r => r.Role == player.Role.ToString());
        Assert.Equal(2, totals.Signed); Assert.Equal(1, totals.Unavailable);
        Assert.Equal(6, report.RivalOffers.Sum(r => r.Pending + r.Signed + r.Declined + r.Unavailable + r.ChecksFailed));
    }
}
