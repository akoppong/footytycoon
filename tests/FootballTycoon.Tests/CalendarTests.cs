using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class CalendarTests
{
    [Fact]
    public void WeeklyTicksAreSaturdaysAnchoredToEachJuly()
    {
        Assert.Equal(new DateOnly(2026, 6, 27), Calendar.Date(0));
        Assert.Equal(new DateOnly(2026, 7, 4), Calendar.Date(1));
        Assert.Equal(new DateOnly(2027, 6, 26), Calendar.Date(52));
        Assert.Equal(new DateOnly(2027, 7, 3), Calendar.Date(53));
        Assert.Equal("Sat 14 Aug", Calendar.Day(59));
        Assert.Equal("Sat 26 Jun 2027", Calendar.FullDay(52));
        Assert.Equal("2026/27", Calendar.SeasonName(1));
        for (var season = 1; season <= 50; season++)
        {
            var start = (season - 1) * Seasons.Weeks;
            Assert.Equal(7, Calendar.Date(start + 1).Month);
            Assert.InRange(Calendar.Date(start + 1).Day, 1, 7);
            Assert.Equal(6, Calendar.Date(start + Seasons.Weeks).Month);
            for (var week = start + 1; week <= start + Seasons.Weeks; week++)
            {
                Assert.Equal(DayOfWeek.Saturday, Calendar.Date(week).DayOfWeek);
                Assert.Contains(Calendar.Date(week).DayNumber - Calendar.Date(week - 1).DayNumber, new[] { 7, 14 });
            }
        }
    }

    [Fact]
    public void DatedSeasonsOpenInAugustEndInMayAndNeverCollide()
    {
        var world = SeasonTests.EndFirstSeason();
        for (var season = 1; season <= Seasons.PlayableSeasons; season++)
        {
            if (season > 1) SimulationTests.Commit(world, Allocation.StartNextSeason, $"renew-{season}");
            if (season > 1) SimulationTests.Commit(world, Allocation.PreserveReserve, $"plan-{season}");
            while (world.Week < season * Seasons.Weeks) Simulation.AdvanceWeek(world);
            var start = (season - 1) * Seasons.Weeks;
            var fixtures = world.Fixtures.Where(f => f.Week > start && f.Week <= start + Seasons.Weeks).ToArray();
            var league = fixtures.Where(f => f.Competition == Competition.League).ToArray();
            var cup = fixtures.Where(f => f.Competition == Competition.Cup).ToArray();
            Assert.Equal(720, league.Length);
            Assert.Equal(47, cup.Length);
            var opener = Calendar.Date(league.Min(f => f.Week));
            var closer = Calendar.Date(league.Max(f => f.Week));
            Assert.Equal(8, opener.Month); Assert.InRange(opener.Day, 8, 21);
            Assert.Equal(5, closer.Month);
            Assert.Equal(8, Calendar.Date(cup.Where(f => f.CupRound == 1).Min(f => f.Week)).Month);
            Assert.True(cup.Min(f => f.Week) < league.Min(f => f.Week));
            var final = Calendar.Date(cup.Single(f => f.CupRound == 6).Week);
            Assert.Equal(5, final.Month); Assert.True(final > closer);
            Assert.Empty(league.Select(f => f.Week).Intersect(cup.Select(f => f.Week)));
            Assert.Contains(league, f => Calendar.Date(f.Week) is { Month: 12, Day: >= 26 } or { Month: 1, Day: <= 1 });
        }
    }

    [Fact]
    public void WinterWindowOpensOnFirstJanuaryAndCompletesByFebruary()
    {
        for (var season = 1; season <= 50; season++)
        {
            var world = WorldFactory.Create(2026);
            world.Season = season;
            var (opens, closes) = RecruitmentMarket.Window(world);
            var start = (season - 1) * Seasons.Weeks;
            var year = Calendar.Date(start + 1).Year + 1;
            Assert.True(Calendar.Date(start + opens) >= new DateOnly(year, 1, 1));
            Assert.True(Calendar.Date(start + opens - 1) < new DateOnly(year, 1, 1));
            Assert.True(Calendar.Date(start + closes + 1) <= new DateOnly(year, 2, 1));
            Assert.True(Calendar.Date(start + closes + 2) > new DateOnly(year, 2, 1));
            Assert.InRange(closes - opens + 1, 3, 4);
        }
    }

    [Fact]
    public void InterfaceStringsUseDatesInsteadOfWeekNumbers()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        Assert.Matches("^Next: Preliminary round · Sat 8 Aug$", Cups.Status(world, world.Clubs.First(c =>
            world.Fixtures.Any(f => f.Competition == Competition.Cup && f.Home == c.Id)).Id));
        Assert.Equal("January window opens Sat 2 Jan.", RecruitmentMarket.Status(world));
        while (world.Week < 27) Simulation.AdvanceWeek(world);
        var opened = Assert.Single(world.Reviews, r => r.Title == "January transfer window opens");
        Assert.Contains("Approve by Sat 23 Jan", opened.Evidence);
        Assert.Equal("January window open · approve by Sat 23 Jan; deals complete by Sat 30 Jan.", RecruitmentMarket.Status(world));
        var hospitality = Proposals.Preview(WorldFactory.Create(1), new(Allocation.Hospitality), 0).Uncertainty;
        Assert.Contains("through Sat 24 Jun 2028", hospitality);
        Assert.DoesNotContain("week 2", hospitality, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SchemaFiveSaveKeepsItsSeasonLayoutAndDatesTheNextSeason()
    {
        var world = WorldFactory.Create(2026);
        world.CalendarStartSeason = 2;
        world.Fixtures.Clear();
        world.Season = 1;
        // Rebuild the pre-calendar season-one layout exactly as a schema-5 save stored it.
        WorldFactory.AddSeasonFixtures(world);
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 5; legacy["SimulationVersion"] = "market-5"; legacy.Remove("CalendarStartSeason");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(source, bytes);
        Assert.Equal(6, migrated.SchemaVersion);
        Assert.Equal(2, migrated.CalendarStartSeason);
        Assert.Equal(3, migrated.Fixtures.Where(f => f.Competition == Competition.League).Min(f => f.Week));
        Assert.Equal((24, 27), RecruitmentMarket.Window(migrated));
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(migrated));
        Assert.Equal(1, WorldFactory.Create(2026).CalendarStartSeason);
        Assert.Equal(7, WorldFactory.Create(2026).Fixtures.Where(f => f.Competition == Competition.League).Min(f => f.Week));
        SimulationTests.Commit(migrated, Allocation.Acquire);
        SimulationTests.Commit(migrated, Allocation.PreserveReserve);
        while (migrated.Week < Seasons.Weeks) Simulation.AdvanceWeek(migrated);
        SimulationTests.Commit(migrated, Allocation.StartNextSeason);
        Assert.Equal(2, migrated.Season);
        Assert.Equal(Seasons.Weeks + 7, migrated.Fixtures.Where(f => f.Competition == Competition.League && f.Week > Seasons.Weeks).Min(f => f.Week));
    }
}
