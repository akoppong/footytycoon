using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class PyramidTests
{
    [Fact]
    public void AllMovesAreSimultaneousAndHistoricalMembershipSurvives()
    {
        var world = SeasonTests.EndFirstSeason();
        var tables = Enumerable.Range(1, 3).ToDictionary(d => d, d => Simulation.Table(world, d).ToArray());
        var clubs = world.Clubs.ToDictionary(c => c.Id, c => c.Division);
        var cash = world.Clubs.ToDictionary(c => c.Id, c => c.Cash);
        var salaries = world.Clubs.ToDictionary(c => c.Id, c => Finance.AnnualWages(c));
        var terms = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        var before = WorldCodec.Encode(world);
        var destinations = Pyramid.NextDivisions(world);
        Assert.Equal(before, WorldCodec.Encode(world));
        foreach (var (division, table) in tables)
        {
            foreach (var row in table.Take(2)) Assert.Equal(Math.Max(1, division - 1), destinations[row.ClubId]);
            foreach (var row in table.TakeLast(2)) Assert.Equal(Math.Min(3, division + 1), destinations[row.ClubId]);
            foreach (var row in table.Skip(2).Take(12)) Assert.Equal(division, destinations[row.ClubId]);
        }
        SimulationTests.Commit(world, Allocation.StartNextSeason);
        Assert.Equal(8, world.Clubs.Count(c => c.Division != clubs[c.Id]));
        Assert.All(Enumerable.Range(1, 3), d => Assert.Equal(16, world.Clubs.Count(c => c.Division == d)));
        Assert.Equal(terms.Renewal!.AnnualBroadcast, world.OwnedClub.AnnualBroadcast);
        Assert.Equal(terms.Renewal.AnnualSponsor, world.OwnedClub.AnnualSponsor);
        Assert.Equal(terms.Renewal.NextDivision, world.OwnedClub.Division);
        foreach (var club in world.Clubs)
        {
            Assert.Equal(cash[club.Id], club.Cash);
            Assert.Equal(salaries[club.Id], Finance.AnnualWages(club));
            Assert.Equal(Pyramid.Broadcast(club.Division), club.AnnualBroadcast);
        }
        foreach (var (division, table) in tables) Assert.Equal(table, Simulation.Table(world, division, 1).ToArray());
        var fixtures = world.Fixtures.Where(f => f.Competition == Competition.League && f.Week > 52).ToArray();
        Assert.Equal(720, fixtures.Length);
        Assert.All(fixtures, f =>
        {
            Assert.Equal(f.Division, world.Clubs.Single(c => c.Id == f.Home).Division);
            Assert.Equal(f.Division, world.Clubs.Single(c => c.Id == f.Away).Division);
        });
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    [Fact]
    public void PreviousSeasonHeadToHeadCannotBreakThisSeasonsTie()
    {
        var world = WorldFactory.Create(7);
        var clubs = world.Clubs.Where(c => c.Division == 1).Take(3).ToArray();
        var a = clubs[0].Id; var b = clubs[1].Id; var c = clubs[2].Id;
        world.Season = 2; WorldFactory.AddSeasonFixtures(world);
        clubs[0].Lot = 30; clubs[1].Lot = 10; clubs[2].Lot = 20;
        void Game(int season, ClubId winner, ClubId loser, int goals)
        {
            var fixture = world.Fixtures.First(f => f.Competition == Competition.League
                && f.Week > (season - 1) * 52 && f.Week <= season * 52
                && (f.Home == winner && f.Away == loser || f.Home == loser && f.Away == winner));
            var homeGoals = fixture.Home == winner ? goals : 0; var awayGoals = fixture.Away == winner ? goals : 0;
            world.Results.Add(new(fixture.Id, fixture.Week, fixture.Home, fixture.Away, homeGoals, awayGoals, homeGoals, awayGoals, 0, 0, []));
        }
        Game(1, a, b, 9); // Would unfairly lift A if old head-to-head leaked.
        Game(2, a, b, 1); Game(2, b, c, 1); Game(2, c, a, 1);
        var tied = Simulation.Table(world, 1).Where(r => r.ClubId == a || r.ClubId == b || r.ClubId == c).ToArray();
        Assert.All(tied, row => { Assert.Equal(2, row.Played); Assert.Equal(3, row.Points); });
        Assert.Equal(new[] { b, c, a }, tied.Select(r => r.ClubId));
    }

    [Fact]
    public void TierBudgetsDoNotCompoundOrRewriteRealizedRevenue()
    {
        var world = WorldFactory.Create(2); var club = world.OwnedClub;
        var baseline = club.HistoricalTickets;
        club.Division = 1;
        Assert.Equal(Money.Scale(baseline, 1.6m), Pyramid.TradingBudget(club, baseline));
        club.Division = 3;
        Assert.Equal(Money.Scale(baseline, .65m), Pyramid.TradingBudget(club, baseline));
        club.Division = 2;
        Assert.Equal(baseline, Pyramid.TradingBudget(club, baseline));
        Assert.Empty(world.Journal);
    }

    [Fact]
    public void SchemaTwoMigrationBackfillsFixedDivisionHistoryWithoutChangingCash()
    {
        var world = WorldFactory.Create(6);
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 2; legacy["SimulationVersion"] = "career-2";
        legacy.Remove("CupStartSeason");
        var fixtureArray = legacy["Fixtures"]!.AsArray();
        for (var i = fixtureArray.Count - 1; i >= 0; i--)
            if (fixtureArray[i]!["Competition"]?.GetValue<int>() == (int)Competition.Cup) fixtureArray.RemoveAt(i);
        foreach (var club in legacy["Clubs"]!.AsArray()) club!.AsObject().Remove("OpeningDivision");
        foreach (var fixture in legacy["Fixtures"]!.AsArray()) fixture!.AsObject().Remove("Division");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(source, bytes); Assert.Equal(7, migrated.SchemaVersion);
        Assert.All(migrated.Fixtures.Where(f => f.Competition == Competition.League), f => Assert.Equal(migrated.Clubs.Single(c => c.Id == f.Home).Division, f.Division));
        Assert.Equal(16, migrated.Fixtures.Count(f => f.Competition == Competition.Cup && f.CupRound == 1));
        Assert.All(migrated.Clubs, c => Assert.Equal(c.Division, c.OpeningDivision));
        Assert.Equal(world.OwnedClub.Cash, migrated.OwnedClub.Cash);
    }
}
