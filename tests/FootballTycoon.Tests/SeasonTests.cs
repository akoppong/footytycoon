using FootballTycoon.Core;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace FootballTycoon.Tests;

public class SeasonTests
{
    internal static World EndFirstSeason(bool includeCup = true)
    {
        var world = WorldFactory.Create(2026);
        if (!includeCup)
        {
            world.Fixtures.RemoveAll(f => f.Competition == Competition.Cup);
            world.CupStartSeason = 2;
        }
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        while (world.Week < 52) Simulation.AdvanceWeek(world);
        return world;
    }

    [Fact]
    public void ThreeSeasonsRenewWithoutCashResetAndRetainIndependentTables()
    {
        var world = EndFirstSeason();
        for (var season = 2; season <= 3; season++)
        {
            var before = WorldCodec.Encode(world);
            var cash = world.OwnedClub.Cash;
            var owner = world.OwnerCash;
            var proposal = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
            Assert.Empty(proposal.BlockingReasons);
            Assert.Equal(world.OwnedClub.Players.Count(p => p.ContractEndWeek <= world.Week), proposal.Renewal!.PlayerContracts);
            Assert.Equal(proposal.Renewal.PlayerContracts, proposal.Renewal.Renewed + proposal.Renewal.Released);
            var squadBefore = world.OwnedClub.Players.Count;
            Assert.Equal(before, WorldCodec.Encode(world));
            var receipt = Proposals.Commit(world, $"renew-{season}", world.Revision, proposal);
            Assert.Equal(squadBefore - proposal.Renewal.Released, world.OwnedClub.Players.Count);
            Assert.True(world.OwnedClub.Players.Count >= Contracts.MinimumSquad);
            Assert.Equal(cash, world.OwnedClub.Cash);
            Assert.Equal(owner, world.OwnerCash);
            Assert.Equal(season, world.Season);
            Assert.All(world.OwnedClub.Players, p => Assert.True(p.ContractEndWeek > world.Week));
            Assert.Equal(720 * season, world.Fixtures.Count(f => f.Competition == Competition.League));
            Assert.All(Simulation.Table(world, 2), row => Assert.Equal(0, row.Played));
            Assert.All(world.SeasonSummaries[^1].FinalTable, row => Assert.Equal(30, row.Played));
            Assert.Equal("Owner decision required", Simulation.AdvanceWeek(world).StopReason);
            Assert.Equal(receipt, Proposals.Commit(world, $"renew-{season}", proposal.Revision, proposal));
            SimulationTests.Commit(world, Allocation.PreserveReserve, $"plan-{season}");
            var restored = WorldCodec.Clone(world);
            Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(restored);
            Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(restored));
            var wages = world.Journal.Where(j => j.Week == world.Week && j.Account == WorldFactory.Account(world.OwnedClubId) && j.Kind == CashKind.Wages).Sum(j => j.Amount);
            Assert.Equal(-world.OwnedClub.Players.Sum(p => p.WeeklyWage), wages);
            var operations = world.Journal.Where(j => j.Week == world.Week && j.Account == WorldFactory.Account(world.OwnedClubId)
                && j.Kind == CashKind.Operations).Sum(j => j.Amount);
            Assert.Equal(-Balance.Load().WeeklyOperations, operations);
            while (world.Week < season * 52) Simulation.AdvanceWeek(world);
        }
        Assert.Equal(CareerStatus.PrototypeComplete, world.Status);
        Assert.Equal(2160, world.Results.Count(r => world.Fixtures.Single(f => f.Id == r.FixtureId).Competition == Competition.League));
        Assert.Equal(3, world.SeasonSummaries.Count);
        Assert.All(Simulation.Table(world, 2), row => Assert.Equal(30, row.Played));
        Assert.NotEmpty(Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision).BlockingReasons);
        Assert.Equal(156, Simulation.AdvanceWeek(world).Week);
    }

    [Fact]
    public void LegacyCompletedSaveMigratesWithoutChangingItsSourceOrCash()
    {
        var world = EndFirstSeason(includeCup: false);
        world.SchemaVersion = 1; world.SimulationVersion = "prototype-1";
        world.Status = CareerStatus.PrototypeComplete; world.SeasonSummaries.Clear();
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        foreach (var name in new[] { "Season", "SeasonOpeningCash", "SeasonOpeningLedgerSequence", "SeasonSummaries", "CupStartSeason" }) legacy.Remove(name);
        foreach (var fixture in legacy["Fixtures"]!.AsArray())
            foreach (var name in new[] { "Division", "Competition", "CupRound" }) fixture!.AsObject().Remove(name);
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(source, bytes);
        Assert.Equal(7, migrated.SchemaVersion);
        Assert.Equal(CareerStatus.SeasonReview, migrated.Status);
        Assert.Single(migrated.SeasonSummaries);
        Assert.Equal(16, migrated.SeasonSummaries[0].FinalTable.Length);
        Assert.All(migrated.SeasonSummaries[0].FinalTable, row => Assert.Equal(30, row.Played));
        Assert.Equal("Not held (legacy season)", migrated.SeasonSummaries[0].CupResult);
        Assert.DoesNotContain(migrated.Fixtures, f => f.Competition == Competition.Cup);
        Assert.Equal(world.OwnedClub.Cash, migrated.OwnedClub.Cash);
        Assert.Equal(world.Journal, migrated.Journal);
        SimulationTests.Commit(migrated, Allocation.StartNextSeason);
        Assert.Equal(2, migrated.Season);
        Assert.Equal(16, migrated.Fixtures.Count(f => f.Competition == Competition.Cup));
    }

    [Fact]
    public void AdministrationDeadlineSurvivesRolloverAndUnchosenAnnualPlan()
    {
        var world = EndFirstSeason();
        Finance.Post(world, WorldFactory.Account(world.OwnedClubId), -world.OwnedClub.Cash, CashKind.Operations, "stress");
        var obligation = world.Obligations.First(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages);
        world.Arrears.Add(new(obligation.Id, world.OwnedClubId, 51, 900000000));
        world.AdministrationWeek = 51;
        SimulationTests.Commit(world, Allocation.StartNextSeason);
        Assert.Equal(CareerStatus.Administration, world.Status);
        Assert.Equal(51, world.AdministrationWeek);
        var recovered = WorldCodec.Clone(world);
        SimulationTests.Commit(recovered, Allocation.InjectCapital, amount: recovered.OwnerCash);
        // A smaller payable arrear exercises recovery with the same inherited deadline.
        recovered.Arrears[0] = recovered.Arrears[0] with { Amount = 10000000 };
        Simulation.AdvanceWeek(recovered);
        Assert.Equal(CareerStatus.Active, recovered.Status);
        Assert.Null(recovered.AdministrationWeek);
        Assert.Equal("Owner decision required", Simulation.AdvanceWeek(recovered).StopReason);
        while (world.Week < 55) Simulation.AdvanceWeek(world);
        Assert.Equal(CareerStatus.LostControl, world.Status);
    }

    [Fact]
    public void RecurringRevenueUsesLastSeasonTradingAndExcludesOwnerFunding()
    {
        var world = EndFirstSeason(); var club = world.OwnedClub;
        var expected = club.AnnualBroadcast + club.AnnualSponsor + world.Journal.Where(j => j.Account == WorldFactory.Account(club.Id)
            && j.Week > 0 && (j.Kind == CashKind.Commercial || j.Kind is CashKind.Tickets or CashKind.Hospitality
                && world.Fixtures.Single(f => f.Id.Value == int.Parse(j.Reference["fixture/".Length..])).Competition == Competition.League)).Sum(j => j.Amount);
        SimulationTests.Commit(world, Allocation.InjectCapital, amount: 10000000);
        Assert.Equal(expected, Finance.EligibleRevenue(world, club.Id));
    }
}
