extern alias Endurance;
using Xunit;
using Diagnostic = Endurance::FootballTycoon.Core;
using Probe = Endurance::FootballTycoon.Endurance;

namespace FootballTycoon.Tests;

public class RivalVacancyDiagnosticsTests
{
    // Read-only snapshots isolate gates; these fixtures are not passed to simulation or save validation.
    private static (Diagnostic.World World, Diagnostic.Club Club) Snapshot()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        world.Status = Diagnostic.CareerStatus.Active; world.Week = 1;
        var club = world.Clubs.First(c => c.Id != world.OwnedClubId);
        club.Players.Remove(club.Players.First(p => p.Role == Diagnostic.Role.Goalkeeper));
        club.Cash = 100_000_000_000;
        return (world, club);
    }

    private static Probe.RivalVacancy Row(Diagnostic.World world) => Assert.Single(Probe.RivalVacancyDiagnostics.Sample(world));

    [Fact]
    public void SamplesExcludeOwnerAndPreserveEveryWorldByteIncludingRandomStreams()
    {
        var (world, club) = Snapshot();
        world.OwnedClub.Players.Clear();
        var before = Diagnostic.WorldCodec.Encode(world);
        var row = Row(world);
        Assert.Equal(club.Id.Value, row.ClubId); Assert.Equal("EligibleNow", row.Gate);
        Assert.Equal(new[] { "Goalkeeper" }, row.MissingRoles); Assert.Equal(0, row.SquadVacancies);
        Assert.True(row.EligibleCandidates > 0); Assert.True(row.WageEligibleCandidates > 0);
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
    }

    [Fact]
    public void PriorityAndCooldownBoundaryAreDisjointAndKeepResolutionContext()
    {
        var (world, club) = Snapshot();
        var player = world.FreeAgents[0].Player;
        world.RivalApproaches.Add(new(1, club.Id, 1, player, 10000, 52, Diagnostic.ApproachOutcome.Pending, null));
        club.Cash = world.ReserveTarget - 1;
        Assert.Equal("PendingOffer", Row(world).Gate); Assert.Null(Row(world).EligibleCandidates);
        world.RivalApproaches[0] = world.RivalApproaches[0] with { Outcome = Diagnostic.ApproachOutcome.Declined, ResolvedWeek = 2 };
        world.Week = 4;
        Assert.Equal("CashBelowReserve", Row(world).Gate);
        club.Cash = world.ReserveTarget;
        Assert.Equal("Cooldown", Row(world).Gate);
        club.Cash = 100_000_000_000; world.Week = 5;
        var row = Row(world);
        Assert.Equal("EligibleNow", row.Gate); Assert.Equal("Declined", row.LastResolvedOutcome); Assert.Equal(2, row.LastResolvedWeek);
        world.Arrears.Add(new(new(1), club.Id, 5, 1));
        Assert.Equal("Arrears", Row(world).Gate);
    }

    [Theory]
    [InlineData(50, Diagnostic.CareerStatus.Active, "EligibleNow")]
    [InlineData(51, Diagnostic.CareerStatus.Active, "SeasonClosing")]
    [InlineData(52, Diagnostic.CareerStatus.SeasonReview, "CareerBoundary")]
    [InlineData(20, Diagnostic.CareerStatus.LostControl, "CareerBoundary")]
    [InlineData(20, Diagnostic.CareerStatus.PrototypeComplete, "CareerBoundary")]
    public void ClosingWeeksAndCareerBoundariesTakePriority(int week, Diagnostic.CareerStatus status, string gate)
    {
        var (world, _) = Snapshot(); world.Week = week; world.Status = status;
        Assert.Equal(gate, Row(world).Gate);
    }

    [Fact]
    public void RoleGapAtFullSquadReportsCapacityBeforeOtherRestrictions()
    {
        var (world, club) = Snapshot();
        club.Players.Add(world.FreeAgents.First(f => f.Player.Role == Diagnostic.Role.Defender).Player);
        club.Cash = 0;
        Assert.Equal("SquadCapacity", Row(world).Gate);
    }

    [Fact]
    public void SupplyGatesDistinguishRetirementAgeAndNeededRoles()
    {
        var (world, _) = Snapshot();
        world.FreeAgents = world.FreeAgents.Where(f => f.Player.Role != Diagnostic.Role.Goalkeeper).ToList();
        Assert.Equal("NoNeededRoleSupply", Row(world).Gate);
        world.FreeAgents = world.FreeAgents.Select(f => f with { Player = f.Player with { Age = Diagnostic.FreeAgents.RetirementAge(f.Player.Role) } }).ToList();
        var row = Row(world);
        Assert.Equal("NoAvailableSupply", row.Gate); Assert.Equal(0, row.EligibleCandidates); Assert.Equal(0, row.WageEligibleCandidates);
    }

    [Fact]
    public void TotalOnlyGapHonorsWindowAndImprovementRequirement()
    {
        var (world, club) = Snapshot();
        club.Players.Add(world.FreeAgents.First(f => f.Player.Role == Diagnostic.Role.Goalkeeper).Player);
        club.Players = club.Players.GroupBy(p => p.Role).SelectMany(g => g.Take(Diagnostic.Contracts.Minimum.Single(m => m.Role == g.Key).Minimum)).ToList();
        world.FreeAgents = world.FreeAgents.Where(f => f.Player.Role == Diagnostic.Role.Defender)
            .Select(f => f with { Player = f.Player with { Ability = 1 } }).ToList();
        world.Week = 2;
        Assert.Equal("NoSuitableCandidate", Row(world).Gate);
        world.Week = 3;
        Assert.Equal("OutsideWindowTotalGap", Row(world).Gate); Assert.Empty(Row(world).MissingRoles); Assert.Equal(1, Row(world).SquadVacancies);
        world.Week = 2;
        world.FreeAgents = world.FreeAgents.Select(f => f with { Player = f.Player with { Ability = 100 } }).ToList();
        Assert.Equal("EligibleNow", Row(world).Gate);
        // An eligible improvement can be in another role, without filling the missing keeper place.
        club.Players.Remove(club.Players.First(p => p.Role == Diagnostic.Role.Goalkeeper));
        Assert.Equal("EligibleNow", Row(world).Gate); Assert.Equal(new[] { "Goalkeeper" }, Row(world).MissingRoles);
    }

    [Fact]
    public void WageAndDownsideConstraintsAreSeparateFromCurrentCash()
    {
        var (world, club) = Snapshot();
        club.AnnualBroadcast = club.AnnualSponsor = club.HistoricalCommercial = club.HistoricalHospitality = club.HistoricalTickets = 0;
        Assert.Equal("WageLimit", Row(world).Gate); Assert.True(Row(world).EligibleCandidates > 0); Assert.Equal(0, Row(world).WageEligibleCandidates);
        club.AnnualBroadcast = 100_000_000_000;
        world.Obligations.Add(new(new(99999), club.Id, 2, 2, -club.Cash, Diagnostic.CashKind.Operations, "Controlled future liability"));
        Assert.Equal("DownsideReserve", Row(world).Gate); Assert.True(Row(world).WageEligibleCandidates > 0);
    }

    [Fact]
    public void EpisodesCloseReopenAndPartitionRoleAndTotalOnlyWeeksWithoutDoubleCounting()
    {
        var (world, club) = Snapshot(); var original = club.Players.ToList();
        club.Players.Clear();
        var tracker = new Probe.RivalVacancyTracker();
        Assert.Single(tracker.Observe(world));
        Assert.Throws<InvalidOperationException>(() => tracker.Observe(world));
        world.Week = 3; Assert.Throws<InvalidOperationException>(() => tracker.Observe(world));
        world.Week = 2; tracker.Observe(world);
        club.Players = original; club.Players.Add(world.FreeAgents.First(f => f.Player.Role == Diagnostic.Role.Goalkeeper).Player);
        world.Week = 3; Assert.Empty(tracker.Observe(world));
        club.Players = club.Players.GroupBy(p => p.Role).SelectMany(g => g.Take(Diagnostic.Contracts.Minimum.Single(m => m.Role == g.Key).Minimum)).ToList();
        world.Week = 4; tracker.Observe(world);
        var report = tracker.Report();
        Assert.Equal(3, report.ObservedClubWeeks); Assert.Equal(2, report.RoleGapClubWeeks); Assert.Equal(1, report.TotalOnlyClubWeeks);
        Assert.Equal(report.ObservedClubWeeks, report.GateClubWeeks.Values.Sum());
        Assert.Equal(report.ObservedClubWeeks, report.Episodes.Sum(e => e.Weeks));
        Assert.Equal(new[] { new Probe.VacancyEpisode(club.Id.Value, 1, 2, 2, false), new Probe.VacancyEpisode(club.Id.Value, 4, 4, 1, true) }, report.Episodes);
        report.GateClubWeeks.Clear(); report.Episodes[0] = report.Episodes[0] with { Weeks = 999 };
        Assert.Equal(3, tracker.Report().GateClubWeeks.Values.Sum()); Assert.Equal(3, tracker.Report().Episodes.Sum(e => e.Weeks));
    }
}
