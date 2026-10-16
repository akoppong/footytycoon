using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class OpeningPopulationTests
{
    [Theory]
    [InlineData(2026UL)]
    [InlineData(2027UL)]
    [InlineData(7UL)]
    public void NewCareersHaveTwelveHundredDistinctPeopleAndPreserveOpeningWageBudgets(ulong seed)
    {
        var world = WorldFactory.Create(seed);
        var people = world.Clubs.SelectMany(c => c.Players).Concat(world.FreeAgents.Select(f => f.Player)).ToArray();
        Assert.Equal(1200, people.Length);
        Assert.Equal(1200, people.Select(p => p.Id).Distinct().Count());
        Assert.Equal(1200, people.Select(p => p.ContractId).Distinct().Count());
        Assert.Equal(1200, people.Select(p => p.Name).Distinct().Count());
        Assert.All(people, p => Assert.InRange(p.Age, 17, FreeAgents.RetirementAge(p.Role) - 1));
        Assert.Contains(people, p => p.Age > 35);
        Assert.Empty(world.Departures); Assert.Empty(world.Retirements); Assert.Empty(world.AcademyGraduates);
        foreach (var club in world.Clubs)
        {
            Assert.Equal(22, club.Players.Count);
            Assert.Equal(new[] { 2, 8, 8, 4 }, Enum.GetValues<Role>().Select(r => club.Players.Count(p => p.Role == r)));
            Assert.Equal(18 * Contracts.StandardWage(club.Division), club.Players.Sum(p => p.WeeklyWage));
            Assert.All(club.Players, p =>
            {
                Assert.True(p.WeeklyWage > 0);
                var debt = Assert.Single(world.Obligations, o => o.Description == $"Player contract {p.ContractId.Value}");
                Assert.Equal((club.Id, 1, p.ContractEndWeek, -p.WeeklyWage), (debt.ClubId, debt.StartWeek, debt.EndWeek, debt.WeeklyAmount));
            });
        }
        Assert.Equal(144, world.FreeAgents.Count);
        Assert.Equal(new[] { 19, 48, 48, 29 }, Enum.GetValues<Role>().Select(r => world.FreeAgents.Count(f => f.Player.Role == r)));
        Assert.All(world.Clubs, c => Assert.Equal(3, world.FreeAgents.Count(f => f.PreviousClubId == c.Id)));
        Assert.All(world.FreeAgents, f =>
        {
            Assert.Equal(0, f.AvailableSinceWeek); Assert.Equal(0, f.Player.ContractEndWeek);
            Assert.True(f.Player.WeeklyWage > 0);
            Assert.DoesNotContain(world.Obligations, o => o.Description == $"Player contract {f.Player.ContractId.Value}");
        });
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldFactory.Create(seed)));
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    [Fact]
    public void OpeningVeteransAnnounceBeforeTheOwnerCanApproveATransfer()
    {
        var world = WorldFactory.Create(2026);
        var expected = world.Clubs.SelectMany(c => c.Players)
            .Where(p => p.ContractEndWeek == 52 && p.Age + 1 >= FreeAgents.RetirementAge(p.Role)).ToArray();
        Assert.NotEmpty(expected);
        Assert.Equal(expected.Select(p => p.Id).OrderBy(p => p.Value), world.RetirementNotices.Select(n => n.PlayerId).OrderBy(p => p.Value));
        Assert.All(world.RetirementNotices, n => { Assert.Equal(0, n.AnnouncedWeek); Assert.Equal(52, n.RetirementWeek); });
        SimulationTests.Commit(world, Allocation.Acquire);
        var recommendation = RecruitmentMarket.Recommend(world, Allocation.Recruitment);
        Assert.NotNull(recommendation);
        Assert.DoesNotContain(expected, p => p.Id == recommendation.Player.Id);
    }

    [Fact]
    public void SchemaSeventeenMigrationDoesNotBackfillPeopleOrRewriteTermsAndRejectsOldQuotes()
    {
        var world = ControlledWorld.Create(); // 864 people and no available players: a smaller existing career.
        world.SchemaVersion = 17; world.SimulationVersion = "recruitment-contention-17";
        var quote = Proposals.Preview(world, new(Allocation.Acquire), world.Revision);
        var bytes = WorldCodec.Encode(world); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!;
        expected["SchemaVersion"] = 19; expected["SimulationVersion"] = "rival-minimum-squad-19";
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(WorldCodec.Encode(migrated))));
        Assert.Equal(source, bytes); Assert.Empty(migrated.FreeAgents);
        Assert.Equal(864, migrated.Clubs.Sum(c => c.Players.Count));
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(migrated, "old-quote", migrated.Revision, quote));
        Assert.Equal(WorldCodec.Encode(migrated), WorldCodec.Encode(WorldCodec.Clone(migrated)));
        var corrupt = JsonNode.Parse(bytes)!;
        corrupt["OwnerCash"] = world.OwnerCash + 1;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(corrupt.ToJsonString())));
    }
}
