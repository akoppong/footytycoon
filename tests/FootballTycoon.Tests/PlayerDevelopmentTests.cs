using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class PlayerDevelopmentTests
{
    [Fact]
    public void SeasonEndAgesEveryClubAndReportsTheActualChangesBeforeRenewals()
    {
        var opening = WorldFactory.Create(2026).Clubs.SelectMany(c => c.Players).ToDictionary(p => p.Id);
        var world = SeasonTests.EndFirstSeason();
        Assert.All(world.Clubs.SelectMany(c => c.Players), p => Assert.Equal(opening[p.Id].Age + 1, p.Age));
        var progress = world.SeasonSummaries.Single().Development;
        Assert.Equal(world.OwnedClub.Players.Count, progress.Length);
        foreach (var row in progress)
        {
            var player = world.OwnedClub.Players.Single(p => p.Id == row.PlayerId);
            Assert.Equal(opening[player.Id].Ability, row.AbilityBefore);
            Assert.Equal(player.Ability, row.AbilityAfter);
            Assert.Equal(player.Age - 1, row.AgeBefore);
            Assert.Equal(world.Results.SelectMany(r => r.HomeLineup.Concat(r.AwayLineup)).Count(a => a.Player == player.Id), row.Appearances);
        }
        var terms = Seasons.Terms(world);
        foreach (var contract in terms.Contracts)
        {
            var row = progress.Single(p => p.PlayerId == contract.PlayerId);
            Assert.Equal(row.AgeBefore + 1, contract.Age);
            Assert.Equal(row.AbilityAfter, contract.Ability);
        }
        var closed = WorldCodec.Encode(world);
        Seasons.Close(world);
        Assert.Equal(closed, WorldCodec.Encode(world));
        Assert.Equal(closed, WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    [Fact]
    public void DevelopmentReplaysAndDoesNotSpendCashOrConsumeOtherRandomStreams()
    {
        var world = WorldFactory.Create(41); world.Week = 52;
        var loaded = WorldCodec.Clone(world);
        var cash = world.Clubs.Select(c => c.Cash).ToArray();
        var random = world.RandomStates.ToDictionary();
        Seasons.Close(world); Seasons.Close(loaded);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
        Assert.Equal(cash, world.Clubs.Select(c => c.Cash));
        Assert.Empty(world.Journal);
        foreach (var entry in random) Assert.Equal(entry.Value, world.RandomStates[entry.Key]);
        Assert.All(world.RandomStates.Keys.Except(random.Keys), key => Assert.StartsWith("development/", key));
    }

    [Fact]
    public void YouthCanImproveOrStallVeteransCanDeclineAndAbilityRemainsBounded()
    {
        var world = WorldFactory.Create(73); world.Week = 52;
        foreach (var club in world.Clubs)
            club.Players = club.Players.Select((p, i) => p with
            {
                Age = i < 6 ? 19 : i < 12 ? 38 : 29,
                Ability = i == 0 ? 100 : i == 6 ? 1 : 70
            }).ToList();
        var before = world.Clubs.SelectMany(c => c.Players).ToDictionary(p => p.Id);
        Seasons.Close(world);
        var after = world.Clubs.SelectMany(c => c.Players).ToArray();
        Assert.Contains(after, p => before[p.Id].Age == 19 && p.Ability > before[p.Id].Ability);
        Assert.Contains(after, p => before[p.Id].Age == 19 && p.Ability == before[p.Id].Ability && p.Ability < 100);
        Assert.Contains(after, p => before[p.Id].Age == 38 && p.Ability < before[p.Id].Ability);
        Assert.All(after, p => Assert.InRange(p.Ability, 1, 100));
        Assert.All(after.Where(p => before[p.Id].Age == 29), p => Assert.Equal(before[p.Id].Ability, p.Ability));
    }

    [Fact]
    public void ExposureIncludesPreTransferMatchesButExcludesPastSeasons()
    {
        var world = WorldFactory.Create(2); world.Season = 2; world.Week = 104;
        var player = world.OwnedClub.Players[0];
        MatchResult Result(int week, ClubId home) => new(new(week), week, home, new(1), 0, 0, 0, 0, 0, 0, [])
        { HomeLineup = [new(player.Id, player.Role, 65)] };
        world.Results = [Result(52, world.OwnedClubId), Result(54, new(2)), Result(60, world.OwnedClubId)];
        Seasons.Close(world);
        Assert.Equal(2, world.SeasonSummaries.Single().Development.Single(p => p.PlayerId == player.Id).Appearances);
    }

    [Fact]
    public void DevelopmentHistorySurvivesAPlayersDeparture()
    {
        var world = WorldFactory.Create(23); world.Week = 52;
        Seasons.Close(world);
        var report = world.SeasonSummaries.Single().Development;
        var player = world.OwnedClub.Players[0];
        world.OwnedClub.Players.RemoveAt(0);
        var restored = WorldCodec.Clone(world);
        Assert.Equal(report.ToArray(), restored.SeasonSummaries.Single().Development.ToArray());
        Assert.Equal(player.Name, restored.SeasonSummaries.Single().Development.Single(p => p.PlayerId == player.Id).Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SchemaEightMigrationPreservesAgesAndPastResultsWithoutInventingDevelopment(bool complete)
    {
        var world = WorldFactory.Create(9);
        if (complete) { world.Week = 52; Seasons.Close(world); world.Status = CareerStatus.SeasonReview; }
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 8; legacy["SimulationVersion"] = "contracts-8";
        foreach (var summary in legacy["SeasonSummaries"]!.AsArray()) summary!.AsObject().Remove("Development");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var original = bytes.ToArray();
        var loaded = WorldCodec.Decode(bytes);
        Assert.Equal(original, bytes);
        Assert.Equal(18, loaded.SchemaVersion);
        Assert.Equal(world.Clubs.SelectMany(c => c.Players), loaded.Clubs.SelectMany(c => c.Players));
        Assert.All(loaded.SeasonSummaries, s => Assert.Empty(s.Development));
        Assert.DoesNotContain(loaded.RandomStates.Keys.Except(world.RandomStates.Keys), key => key.StartsWith("development/"));
        if (complete)
        {
            var saved = WorldCodec.Encode(loaded);
            Seasons.Close(loaded);
            Assert.Equal(saved, WorldCodec.Encode(loaded));
        }
        else
        {
            loaded.Week = 52; Seasons.Close(loaded);
            Assert.NotEmpty(loaded.SeasonSummaries.Single().Development);
        }
    }

    [Fact]
    public void MalformedDevelopmentHistoryIsRejected()
    {
        var world = WorldFactory.Create(1); world.Week = 52; Seasons.Close(world);
        var summary = world.SeasonSummaries.Single();
        var row = summary.Development[0];
        world.SeasonSummaries[0] = summary with { Development = [row, row] };
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        world.SeasonSummaries[0] = summary with { Development = [row with { AbilityAfter = 101 }] };
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        world.SeasonSummaries[0] = summary with { Development = [row with { Appearances = -1 }] };
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
    }
}
