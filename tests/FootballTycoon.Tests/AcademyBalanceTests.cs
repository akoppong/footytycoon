using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class AcademyBalanceTests
{
    [Fact]
    public void EveryRoleDrawBucketMatchesCoverWeightsWithoutConsultingVacancies()
    {
        var world = WorldFactory.Create(2026); world.Week = 52;
        var club = world.Clubs[0]; club.Players.Clear();
        var seen = new HashSet<int>();
        Role[] expected = [Role.Goalkeeper, Role.Goalkeeper, Role.Defender, Role.Defender, Role.Defender, Role.Defender,
            Role.Defender, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder, Role.Midfielder,
            Role.Forward, Role.Forward, Role.Forward];
        for (ulong seed = 1; seed <= 256; seed++)
        {
            world.Seed = seed;
            var random = new World { Seed = seed }; const string stream = "academy/1/1";
            var count = RandomStreams.Next(random, stream, 3);
            var intake = Academy.Recommend(world, club, club.Division, new([], false, 0, 0, long.MaxValue));
            Assert.Equal(count, intake.Length);
            if (count == 0) continue;
            // Two name draws precede the role. Bounds do not affect stream advancement.
            RandomStreams.Next(random, stream, 1); RandomStreams.Next(random, stream, 1);
            var draw = RandomStreams.Next(random, stream, 15); seen.Add(draw);
            Assert.Equal(expected[draw], intake[0].Role);
        }
        Assert.Equal(15, seen.Count);
    }

    [Fact]
    public void NewRoleWeightsPreserveCandidateIdentityAndOtherDraws()
    {
        // Captured from the schema-15 diagnostic binary (SHA256 96DDFD8C...F5CD31110).
        var world = WorldFactory.Create(2026); world.Week = 52;
        var club = world.Clubs[5]; club.Players.Clear();
        var before = WorldCodec.Encode(world);
        var intake = Academy.Recommend(world, club, 1, new([], false, 0, 0, long.MaxValue));
        Assert.Equal(2, intake.Length);
        Assert.Equal((101060, "Tom Tierney", 55, 17, 54000L, 10101060, 208),
            (intake[0].Id.Value, intake[0].Name, intake[0].Ability, intake[0].Age, intake[0].WeeklyWage, intake[0].ContractId.Value, intake[0].ContractEndWeek));
        Assert.Equal((101061, "Fraser Clarke", 68, 17, 54000L, 10101061, 208),
            (intake[1].Id.Value, intake[1].Name, intake[1].Ability, intake[1].Age, intake[1].WeeklyWage, intake[1].ContractId.Value, intake[1].ContractEndWeek));
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Fact]
    public void SchemaFifteenMigrationPreservesAllDataExceptVersionsAndRejectsOldQuotes()
    {
        var world = SeasonTests.EndFirstSeason();
        SimulationTests.Commit(world, Allocation.StartNextSeason);
        Assert.NotEmpty(world.AcademyGraduates); Assert.NotEmpty(world.History);
        world.SchemaVersion = 15; world.SimulationVersion = "retirement-15";
        var oldQuote = Proposals.Preview(world, new(Allocation.PreserveReserve), world.Revision);
        var bytes = WorldCodec.Encode(world); var copy = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!;
        expected["SchemaVersion"] = 16; expected["SimulationVersion"] = "academy-balance-16";
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(WorldCodec.Encode(migrated))));
        Assert.Equal(copy, bytes);
        Assert.Equal(WorldCodec.Encode(migrated), WorldCodec.Encode(WorldCodec.Clone(migrated)));
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(migrated, "old-quote", migrated.Revision, oldQuote));
        var fresh = Proposals.Preview(migrated, oldQuote.Command, migrated.Revision);
        Assert.Equal(fresh.Id, Proposals.Preview(WorldCodec.Clone(migrated), fresh.Command, migrated.Revision).Id);
        Proposals.Commit(migrated, "fresh-quote", migrated.Revision, fresh);
        var corrupt = JsonNode.Parse(bytes)!; corrupt["OwnerCash"] = world.OwnerCash + 1;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(corrupt.ToJsonString())));
    }
}
