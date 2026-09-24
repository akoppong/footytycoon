using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class SimulationTests
{
    internal static void Commit(World world, Allocation allocation, string? id = null, long amount = 0)
    {
        var proposal = Proposals.Preview(world, new(allocation, amount), world.Revision);
        Proposals.Commit(world, id ?? allocation.ToString(), world.Revision, proposal);
    }

    [Fact]
    public void PurchaseAndInjectionReconcileSeparateAccounts()
    {
        var world = WorldFactory.Create(1); var cash = world.OwnedClub.Cash;
        Commit(world, Allocation.Acquire);
        Assert.Equal(cash, world.OwnedClub.Cash);
        Assert.Equal(220000000, world.OwnerCash);
        Commit(world, Allocation.InjectCapital, amount: 10000000);
        Assert.Equal(cash + 10000000, world.OwnedClub.Cash);
        Assert.Equal(210000000, world.OwnerCash);
        Assert.Equal(290000000, world.OwnerInvested);
        Assert.Equal(0, world.Journal.Where(j => j.Kind is CashKind.Purchase or CashKind.Injection).Sum(j => j.Amount));
        WorldFactory.Validate(world);
    }

    [Fact]
    public void PreviewIsPureAndCommitIsIdempotentAcrossLoad()
    {
        var world = WorldFactory.Create(4); Commit(world, Allocation.Acquire);
        var before = WorldCodec.Encode(world);
        var proposal = Proposals.Preview(world, new(Allocation.Hospitality), world.Revision);
        Assert.Equal(before, WorldCodec.Encode(world));
        var receipt = Proposals.Commit(world, "same", world.Revision, proposal);
        var loaded = WorldCodec.Decode(WorldCodec.Encode(world));
        var after = WorldCodec.Encode(loaded);
        Assert.Equal(receipt, Proposals.Commit(loaded, "same", proposal.Revision, proposal));
        Assert.Equal(after, WorldCodec.Encode(loaded));
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(loaded, "different", proposal.Revision, proposal));
    }

    [Fact]
    public void RequiredDecisionStopsWithoutConsumingAWeek()
    {
        var world = WorldFactory.Create(2); Commit(world, Allocation.Acquire);
        var before = WorldCodec.Encode(world);
        Assert.Equal("Owner decision required", Simulation.AdvanceWeek(world).StopReason);
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Fact]
    public void SameRevisionFromDifferentCheckpointCannotReuseProposal()
    {
        var world = WorldFactory.Create(4); Commit(world, Allocation.Acquire);
        var proposal = Proposals.Preview(world, new(Allocation.Hospitality), world.Revision);
        var otherBranch = WorldCodec.Clone(world);
        Finance.Post(otherBranch, WorldFactory.Account(otherBranch.OwnedClubId), -10000000, CashKind.Operations, "different-branch");
        Assert.Equal(world.Revision, otherBranch.Revision);
        var before = WorldCodec.Encode(otherBranch);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(otherBranch, "old-terms", otherBranch.Revision, proposal));
        Assert.Equal(before, WorldCodec.Encode(otherBranch));
    }

    [Fact]
    public void MultiClubTieUsesMiniTableThenPersistedLots()
    {
        var world = WorldFactory.Create(7);
        var clubs = world.Clubs.Where(c => c.Division == 1).Take(4).ToArray();
        var a = clubs[0].Id; var b = clubs[1].Id; var c = clubs[2].Id; var d = clubs[3].Id;
        void Game(int id, ClubId home, ClubId away, int hg, int ag) => world.Results.Add(new(new(id), 1, home, away, hg, ag, hg, ag, 0, 0, []));
        // A/B/C finish on 6 points, zero GD and 5 GF; mini-table GD is A +1, C 0, B -1.
        Game(1, a, b, 3, 0); Game(2, b, c, 3, 1); Game(3, c, a, 2, 0);
        Game(4, a, d, 2, 0); Game(5, b, d, 2, 0); Game(6, c, d, 2, 0);
        Game(7, d, a, 3, 0); Game(8, d, b, 1, 0); Game(9, d, c, 2, 0);
        var table = Simulation.Table(world, 1);
        var abc = table.Where(r => r.ClubId == a || r.ClubId == b || r.ClubId == c).ToArray();
        Assert.Equal(new[] { a, c, b }, abc.Select(r => r.ClubId));
        world.Results.Clear();
        Game(1, a, b, 1, 0); Game(2, b, c, 1, 0); Game(3, c, a, 1, 0);
        clubs[0].Lot = 30; clubs[1].Lot = 10; clubs[2].Lot = 20;
        Assert.Equal(new[] { b, c, a }, Simulation.Table(world, 1).Take(3).Select(r => r.ClubId));
        Assert.Equal(Simulation.Table(world, 1).ToArray(), Simulation.Table(WorldCodec.Clone(world), 1).ToArray());
    }

    [Theory]
    [InlineData(Allocation.PreserveReserve)]
    [InlineData(Allocation.Hospitality)]
    [InlineData(Allocation.Recruitment)]
    public void FullSeasonReplaySurvivesEveryWeeklyLoadAndRepeatedQueries(Allocation strategy)
    {
        var continuous = WorldFactory.Create(2026); Commit(continuous, Allocation.Acquire); Commit(continuous, strategy);
        var restored = WorldCodec.Clone(continuous);
        for (var week = 1; week <= 52; week++)
        {
            Simulation.AdvanceWeek(continuous);
            restored = WorldCodec.Clone(restored);
            _ = Finance.Forecast(restored, restored.OwnedClubId);
            _ = Simulation.Table(restored, 2);
            Simulation.AdvanceWeek(restored);
            Assert.Equal(WorldCodec.Encode(continuous), WorldCodec.Encode(restored));
        }
        Assert.Equal(CareerStatus.SeasonReview, continuous.Status);
        var leagueFixtures = continuous.Fixtures.Where(f => f.Competition == Competition.League).Select(f => f.Id).ToHashSet();
        Assert.Equal(720, continuous.Results.Count(r => leagueFixtures.Contains(r.FixtureId)));
        Assert.All(continuous.Clubs, club => Assert.Equal(30, Simulation.Table(continuous, club.Division).Single(r => r.ClubId == club.Id).Played));
        Assert.All(continuous.Results, r =>
        {
            Assert.Equal(r.HomeGoals, r.Moments.Count(m => m.ClubId == r.Home));
            Assert.Equal(r.AwayGoals, r.Moments.Count(m => m.ClubId == r.Away));
            Assert.True(r.HomeGoals <= r.HomeShots && r.AwayGoals <= r.AwayShots);
            Assert.All(r.Moments, m => Assert.InRange(m.Minute, 1, r.ExtraTime ? 120 : 90));
        });
    }

    [Fact]
    public void ScheduleHasEveryPairAtBothGrounds()
    {
        var world = WorldFactory.Create(0);
        foreach (var club in world.Clubs)
        {
            Assert.Equal(15, world.Fixtures.Count(f => f.Competition == Competition.League && f.Home == club.Id));
            Assert.Equal(15, world.Fixtures.Count(f => f.Competition == Competition.League && f.Away == club.Id));
            Assert.Equal(15, world.Fixtures.Where(f => f.Competition == Competition.League && f.Home == club.Id).Select(f => f.Away).Distinct().Count());
            Assert.All(world.Fixtures.Where(f => f.Competition == Competition.League && (f.Home == club.Id || f.Away == club.Id)).GroupBy(f => f.Week), g => Assert.Single(g));
        }
    }

    [Fact]
    public void RevenueDenominatorReplacesNextSeasonContracts()
    {
        var club = WorldFactory.Create(0).OwnedClub;
        Assert.Equal(336200000, Finance.EligibleRevenue(club));
        Assert.Equal(154200030, Finance.EligibleRevenue(club, 10, 20));
        Assert.Equal(101, Money.Scale(201, 0.5m));
        Assert.Throws<OverflowException>(() => Money.Add(long.MaxValue, 1));
    }

    [Fact]
    public void BudgetApprovalDoesNotDeductAndSuccessfulTransferReconciles()
    {
        var successes = 0; var failures = 0;
        for (ulong seed = 0; seed < 20; seed++)
        {
            var world = WorldFactory.Create(seed); Commit(world, Allocation.Acquire);
            var cash = world.OwnedClub.Cash; Commit(world, Allocation.Recruitment);
            Assert.Equal(cash, world.OwnedClub.Cash);
            var target = world.Negotiations.Single().PlayerId;
            Simulation.AdvanceWeek(world);
            var payments = world.Journal.Where(j => j.Kind == CashKind.Transfer).ToArray();
            Assert.Equal(0, payments.Sum(j => j.Amount));
            Assert.Single(world.Clubs.SelectMany(c => c.Players), p => p.Id == target);
            if (payments.Length == 2) successes++; else failures++;
            WorldFactory.Validate(world);
        }
        Assert.True(successes > 0 && failures > 0);
    }

    [Fact]
    public void ExpiredBidCannotSign()
    {
        var world = WorldFactory.Create(2); Commit(world, Allocation.Acquire); Commit(world, Allocation.Recruitment);
        world.Negotiations[0] = world.Negotiations[0] with { ExpiryWeek = 0 };
        Simulation.AdvanceWeek(world);
        Assert.Empty(world.Negotiations);
        Assert.DoesNotContain(world.Journal, j => j.Kind == CashKind.Transfer);
    }

    [Fact]
    public void MissedPayrollCreatesArrearsAndFiniteInjectionCanRecover()
    {
        var world = WorldFactory.Create(9); Commit(world, Allocation.Acquire); Commit(world, Allocation.PreserveReserve);
        Finance.Post(world, WorldFactory.Account(world.OwnedClubId), -world.OwnedClub.Cash, CashKind.Operations, "test-stress");
        WorldFactory.AddObligation(world, world.OwnedClubId, 1, 1, -50000000, CashKind.Wages, "Inherited stress payroll");
        Simulation.AdvanceWeek(world);
        Assert.Equal(CareerStatus.Administration, world.Status);
        Assert.NotEmpty(world.Arrears);
        Commit(world, Allocation.InjectCapital, amount: 100000000);
        Simulation.AdvanceWeek(world);
        Assert.Equal(CareerStatus.Active, world.Status);
        Assert.DoesNotContain(world.Arrears, a => a.ClubId == world.OwnedClubId);
        WorldFactory.Validate(world);
    }

    [Fact]
    public void UnresolvedArrearsLoseControlAfterFourWeeks()
    {
        var world = WorldFactory.Create(9); Commit(world, Allocation.Acquire); Commit(world, Allocation.PreserveReserve);
        WorldFactory.AddObligation(world, world.OwnedClubId, 1, 52, -500000000, CashKind.Wages, "Stress payroll");
        Simulation.AdvanceWeek(world); Assert.Equal(CareerStatus.Administration, world.Status);
        for (var i = 0; i < 4; i++) Simulation.AdvanceWeek(world);
        Assert.Equal(5, world.Week); Assert.Equal(CareerStatus.LostControl, world.Status);
        Assert.Equal(5, Simulation.AdvanceWeek(world).Week);
    }

    [Fact]
    public void OriginalForecastSurvivesCompletionAndBothCasesUseKnownPayments()
    {
        var world = WorldFactory.Create(1); Commit(world, Allocation.Acquire); Commit(world, Allocation.Hospitality);
        var original = world.History.Last().OriginalForecast;
        Assert.Equal(53, original.Points.Length);
        for (var i = 0; i < 32; i++) Simulation.AdvanceWeek(world);
        Assert.Equal(original, world.History.Last().OriginalForecast);
        Assert.Equal(1, world.OwnedClub.HospitalityLevel);
        Assert.Contains(world.Reviews, r => r.ForecastId == original.Id && r.Title == "Hospitality opens");
        Assert.Equal(1, world.Journal.Count(j => j.Kind == CashKind.Construction));
    }
}
