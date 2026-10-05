using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class RecruitmentContentionTests
{
    private static FreeAgent Alternative(World world, int ability = 80, long wage = 10000) => world.FreeAgents[0] with
    {
        Player = world.FreeAgents[0].Player with { Id = new(90000), ContractId = new(90000), Ability = ability, WeeklyWage = wage }
    };

    [Fact]
    public void DirectorsSpreadOffersThenStillCompeteWhenCandidatesAreScarce()
    {
        var (world, rivals) = RivalRecruitmentTests.Market(3);
        var best = world.FreeAgents.Single(); var alternative = Alternative(world);
        world.FreeAgents.Add(alternative);
        var reverse = WorldCodec.Clone(world); reverse.Clubs.Reverse(); reverse.FreeAgents.Reverse();
        var obligations = world.Obligations.Count;
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(reverse);
        Assert.Equal(world.RivalApproaches, reverse.RivalApproaches);
        Assert.Equal(new[] { best.Player.Id, alternative.Player.Id, best.Player.Id }, world.RivalApproaches.Select(a => a.Player.Id));
        Assert.Equal(obligations, world.Obligations.Count); Assert.Equal(2, world.FreeAgents.Count);
        Assert.All(world.RivalApproaches, a => Assert.Equal(ApproachOutcome.Pending, a.Outcome));
        // Both distinct candidates accept; the third offer still loses under the existing stable resolution order.
        while (world.RivalApproaches.Take(2).Any(a => RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{a.Id}", 100) >= 70)) world.Seed++;
        var loaded = WorldCodec.Clone(world);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(loaded);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
        Assert.Equal(2, world.RivalApproaches.Count(a => a.Outcome == ApproachOutcome.Signed));
        Assert.Equal(ApproachOutcome.Unavailable, world.RivalApproaches.Single(a => a.ClubId == rivals[2].Id).Outcome);
        Assert.Empty(world.FreeAgents);
        Assert.Single(rivals[0].Players, p => p.Id == best.Player.Id);
        Assert.Single(rivals[1].Players, p => p.Id == alternative.Player.Id);
    }

    [Fact]
    public void AnUnaffordableUncontestedCandidateDoesNotDisplaceAnAffordableContestedOne()
    {
        var (world, _) = RivalRecruitmentTests.Market();
        var best = world.FreeAgents.Single().Player;
        world.FreeAgents.Add(Alternative(world, wage: 1000000000));
        Simulation.AdvanceWeek(world);
        Assert.Equal(2, world.RivalApproaches.Count);
        Assert.All(world.RivalApproaches, a => Assert.Equal(best.Id, a.Player.Id));
    }

    [Fact]
    public void UrgentRoleCoverTakesPriorityOverAnUncontestedWindowUpgrade()
    {
        var (world, rivals) = RivalRecruitmentTests.Market();
        var needed = world.FreeAgents.Single().Player;
        var otherRole = needed.Role == Role.Defender ? Role.Midfielder : Role.Defender;
        foreach (var club in rivals)
        {
            while (club.Players.Count(p => p.Role == otherRole) > 5)
                club.Players.Remove(club.Players.First(p => p.Role == otherRole));
            club.Players = club.Players.Select(p => p.Role == otherRole ? p with { Ability = 50 } : p).ToList();
            Assert.Equal(5, club.Players.Count(p => p.Role == otherRole));
        }
        var upgrade = Alternative(world, ability: 99);
        world.FreeAgents.Add(upgrade with { Player = upgrade.Player with { Role = otherRole } });
        Simulation.AdvanceWeek(world);
        Assert.Equal(2, world.RivalApproaches.Count);
        Assert.All(world.RivalApproaches, a => Assert.Equal(needed.Id, a.Player.Id));
    }

    [Fact]
    public void ResolvedOffersDoNotCreatePermanentInterestOrReserveCandidates()
    {
        var (world, rivals) = RivalRecruitmentTests.Market(1);
        var best = world.FreeAgents.Single().Player;
        world.FreeAgents.Add(Alternative(world));
        Simulation.AdvanceWeek(world);
        var offer = Assert.Single(world.RivalApproaches);
        while (RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{offer.Id}", 100) < 70) world.Seed++;
        Simulation.AdvanceWeek(world);
        Assert.Equal(ApproachOutcome.Declined, world.RivalApproaches[0].Outcome);
        Assert.Equal(2, world.FreeAgents.Count);
        while (world.Week < offer.ApprovedWeek + 4) Simulation.AdvanceWeek(world);
        Assert.Equal(2, world.RivalApproaches.Count);
        Assert.Equal(best.Id, world.RivalApproaches[1].Player.Id);
        Assert.Equal(rivals[0].Id, world.RivalApproaches[1].ClubId);
    }

    [Fact]
    public void SchemaSixteenMigrationRetainsPendingCompetitionAndOnlyChangesVersions()
    {
        var (world, _) = RivalRecruitmentTests.Market();
        Simulation.AdvanceWeek(world);
        SimulationTests.Commit(world, Allocation.FreeAgentRecruitment, "owner-pending");
        Assert.Equal(2, world.RivalApproaches.Count); Assert.Single(world.FreeAgentBids);
        world.SchemaVersion = 16; world.SimulationVersion = "academy-balance-16";
        var oldQuote = Proposals.Preview(world, new(Allocation.InjectCapital) { Amount = 100 }, world.Revision);
        var bytes = WorldCodec.Encode(world); var copy = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!;
        expected["SchemaVersion"] = 18; expected["SimulationVersion"] = "opening-population-18";
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(WorldCodec.Encode(migrated))));
        Assert.Equal(copy, bytes);
        var before = WorldCodec.Encode(migrated);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(migrated, "stale", migrated.Revision, oldQuote));
        Assert.Equal(before, WorldCodec.Encode(migrated));
        var loaded = WorldCodec.Clone(migrated);
        Simulation.AdvanceWeek(migrated); Simulation.AdvanceWeek(loaded);
        Assert.Equal(WorldCodec.Encode(migrated), WorldCodec.Encode(loaded));
        var corrupt = JsonNode.Parse(bytes)!; corrupt["OwnerCash"] = world.OwnerCash + 1;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(corrupt.ToJsonString())));
    }
}
