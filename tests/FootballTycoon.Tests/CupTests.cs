using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class CupTests
{
    [Fact]
    public void DomesticCupCompletesSavedKnockoutAndNeverChangesLeagueTable()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        var replay = WorldCodec.Clone(world);
        while (world.Week < 52)
        {
            Simulation.AdvanceWeek(world);
            replay = WorldCodec.Clone(replay);
            Simulation.AdvanceWeek(replay);
            Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(replay));
        }
        var cup = world.Fixtures.Where(f => f.Competition == Competition.Cup).OrderBy(f => f.CupRound).ThenBy(f => f.Id.Value).ToArray();
        Assert.Equal(new[] { 16, 16, 8, 4, 2, 1 }, cup.GroupBy(f => f.CupRound).Select(g => g.Count()));
        Assert.Equal(47, cup.Length);
        var cupResults = cup.Select(f => world.Results.Single(r => r.FixtureId == f.Id)).ToArray();
        Assert.All(cupResults, result =>
        {
            Assert.NotEqual(default, result.Winner);
            Assert.Contains(result.Winner, new[] { result.Home, result.Away });
            Assert.True(result.HomeGoals != result.AwayGoals || result.Shootout is not null);
        });
        Assert.Contains(cupResults, result => result.ExtraTime);
        Assert.Contains(cupResults, result => result.Shootout is not null);
        Assert.All(cupResults.Where(r => r.Shootout is not null), result => Assert.Equal(result.HomeGoals, result.AwayGoals));
        Assert.All(Simulation.Table(world, 2), row => Assert.Equal(30, row.Played));
        Assert.Equal(330_000_000, world.Journal.Where(j => j.Kind == CashKind.Prize).Sum(j => j.Amount));
        Assert.Single(cup, f => f.CupRound == 6);
        Assert.DoesNotContain("Awaiting", world.SeasonSummaries.Single().CupResult, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CupWindfallsAreExcludedFromRecurringRevenue()
    {
        var world = WorldFactory.Create(77);
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        while (world.Week < 52) Simulation.AdvanceWeek(world);
        var account = WorldFactory.Account(world.OwnedClubId);
        long expected = world.OwnedClub.AnnualBroadcast + world.OwnedClub.AnnualSponsor;
        foreach (var line in world.Journal.Where(j => j.Account == account && j.Week > 0))
        {
            if (line.Kind == CashKind.Commercial) expected += line.Amount;
            if (line.Kind is CashKind.Tickets or CashKind.Hospitality)
            {
                var fixtureId = new FixtureId(int.Parse(line.Reference["fixture/".Length..]));
                if (world.Fixtures.Single(f => f.Id == fixtureId).Competition == Competition.League) expected += line.Amount;
            }
        }
        Assert.True(world.Journal.Any(j => j.Account == account && j.Kind == CashKind.Prize)
            || world.Fixtures.Any(f => f.Competition == Competition.Cup && f.Home == world.OwnedClubId));
        Assert.Equal(expected, Finance.EligibleRevenue(world, world.OwnedClubId));
    }

    [Fact]
    public void SchemaThreeMigrationStartsCupOnlyAtAValidBoundary()
    {
        static byte[] Downgrade(World world)
        {
            var json = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
            json["SchemaVersion"] = 3; json["SimulationVersion"] = "pyramid-3"; json.Remove("CupStartSeason");
            var fixtures = json["Fixtures"]!.AsArray();
            for (var i = fixtures.Count - 1; i >= 0; i--)
                if (fixtures[i]!["Competition"]?.GetValue<int>() == (int)Competition.Cup) fixtures.RemoveAt(i);
            return Encoding.UTF8.GetBytes(json.ToJsonString());
        }
        var opening = WorldFactory.Create(4); var openingBytes = Downgrade(opening); var source = openingBytes.ToArray();
        var migratedOpening = WorldCodec.Decode(openingBytes);
        Assert.Equal(source, openingBytes); Assert.Equal(8, migratedOpening.SchemaVersion);
        Assert.Equal(1, migratedOpening.CupStartSeason);
        Assert.Equal(16, migratedOpening.Fixtures.Count(f => f.Competition == Competition.Cup));

        var underway = WorldFactory.Create(4);
        underway.Fixtures.RemoveAll(f => f.Competition == Competition.Cup);
        SimulationTests.Commit(underway, Allocation.Acquire); SimulationTests.Commit(underway, Allocation.PreserveReserve);
        while (underway.Week < 4) Simulation.AdvanceWeek(underway);
        var migratedUnderway = WorldCodec.Decode(Downgrade(underway));
        Assert.Equal(2, migratedUnderway.CupStartSeason);
        Assert.DoesNotContain(migratedUnderway.Fixtures, f => f.Competition == Competition.Cup);
    }
}
