using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class OwnerMinimumSquadTests
{
    [Theory]
    [InlineData(15, 3, true)]
    [InlineData(15, 1, true)]
    [InlineData(16, 3, false)]
    [InlineData(17, 3, false)]
    [InlineData(16, 1, false)]
    public void WeakDepthQualifiesOnlyWhileTotalCoverIsMissing(int squad, int week, bool eligible)
    {
        var (world, club, candidate) = MinimumSquadFixture.Create(squad, week, owned: true);
        Assert.All(Contracts.Minimum, m => Assert.False(FreeAgents.HasShortage(club, m.Role)));
        Assert.True(candidate.Ability < club.Players.Where(p => p.Role == candidate.Role).Min(p => p.Ability));
        var before = WorldCodec.Encode(world);
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Equal(eligible, quote.Recruitment is not null);
        Assert.Equal(eligible, quote.BlockingReasons.IsEmpty);
        Assert.Equal(before, WorldCodec.Encode(world));
        if (eligible) Assert.Contains("depth without improving", quote.Uncertainty);
    }

    [Fact]
    public void OwnerMustApproveAndAcceptanceUsesExactTermsWithLoadedReplay()
    {
        var (world, club, candidate) = MinimumSquadFixture.Create(15, 3, owned: true);
        var decision = world.History.Count + 1;
        while (RandomStreams.Next(WorldCodec.Clone(world), $"free-agent-negotiation/{decision}", 100) >= 70) world.Seed++;
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        var debts = world.Obligations.ToArray(); var cash = club.Cash; var reserve = world.OwnerCash;
        Assert.Empty(world.FreeAgentBids); Assert.Equal(15, club.Players.Count);
        var receipt = Proposals.Commit(world, "depth", world.Revision, quote);
        Assert.Equal(debts, world.Obligations); Assert.Equal(cash, club.Cash); Assert.Equal(reserve, world.OwnerCash);
        Assert.False(world.History.Last().Command.ReserveException); Assert.Single(world.FreeAgentBids);
        Assert.Equal(receipt, Proposals.Commit(world, "depth", quote.Revision, quote));
        Assert.Null(FreeAgents.Recommend(world));
        var loaded = WorldCodec.Clone(world);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(loaded);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
        var signed = Assert.Single(club.Players, p => p.Id == candidate.Id);
        Assert.Equal(16, club.Players.Count); Assert.Equal(quote.Recruitment!.ContractEndWeek, signed.ContractEndWeek);
        var wage = Assert.Single(world.Obligations, o => o.Description == $"Player contract {signed.ContractId.Value}");
        Assert.Equal((5, signed.ContractEndWeek, -quote.WeeklyCost), (wage.StartWeek, wage.EndWeek, wage.WeeklyAmount));
        Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{wage.Id.Value}/4");
        Simulation.AdvanceWeek(world);
        Assert.Contains(world.Journal, j => j.Reference == $"obligation/{wage.Id.Value}/5" && j.Amount == -quote.WeeklyCost);
    }

    [Fact]
    public void EmergencyDepthDoesNotAddAThirdKeeperOrDisplaceMissingRolePriority()
    {
        var (world, club, _) = MinimumSquadFixture.Create(15, 3, owned: true);
        var pool = world.FreeAgents.ToList();
        world.FreeAgents = pool.Select(f => f with { Player = f.Player with { Role = Role.Goalkeeper, Ability = 100 } }).ToList();
        Assert.Null(FreeAgents.Recommend(world));
        world.FreeAgents = pool;
        var defender = club.Players.First(p => p.Role == Role.Defender);
        club.Players.Remove(defender);
        world.FreeAgents[1] = world.FreeAgents[1] with { Player = world.FreeAgents[1].Player with { Role = Role.Forward, Ability = 100, WeeklyWage = 10000 } };
        Assert.Equal(Role.Defender, FreeAgents.Recommend(world)!.Player.Role);
    }

    [Theory]
    [InlineData("covered")]
    [InlineData("cash")]
    public void ChangedFactsStopApprovedDepthWithoutCreatingAContract(string change)
    {
        var (world, club, candidate) = MinimumSquadFixture.Create(15, 3, owned: true);
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Proposals.Commit(world, "depth", world.Revision, quote);
        if (change == "covered")
        {
            var other = world.FreeAgents.Last(); world.FreeAgents.Remove(other);
            var replacement = other.Player with { ContractId = new(90000), WeeklyWage = 10000, ContractEndWeek = 52 };
            club.Players.Add(replacement);
            WorldFactory.AddObligation(world, club.Id, 4, 52, -replacement.WeeklyWage, CashKind.Wages, $"Player contract {replacement.ContractId.Value}");
        }
        else WorldFactory.AddObligation(world, club.Id, 10, 10, -club.Cash * 2, CashKind.Operations, "Future essential cost");
        var debts = world.Obligations.ToArray();
        WorldFactory.Validate(world); Simulation.AdvanceWeek(world);
        Assert.Empty(world.FreeAgentBids); Assert.Equal(debts, world.Obligations);
        Assert.DoesNotContain(club.Players, p => p.Id == candidate.Id);
        Assert.Contains(world.Reviews, r => r.Title == "Free-agent approach closed");
    }

    [Fact]
    public void RefusalLeavesDepthOpenAndSameCandidateOnCooldown()
    {
        var (world, club, candidate) = MinimumSquadFixture.Create(15, 3, owned: true);
        var decision = world.History.Count + 1;
        while (RandomStreams.Next(WorldCodec.Clone(world), $"free-agent-negotiation/{decision}", 100) < 70) world.Seed++;
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Proposals.Commit(world, "depth", world.Revision, quote); Simulation.AdvanceWeek(world);
        Assert.Equal(15, club.Players.Count); Assert.Null(FreeAgents.Recommend(world));
        while (world.Week < 7) Simulation.AdvanceWeek(world);
        Assert.Equal(candidate.Id, FreeAgents.Recommend(world)!.Player.Id);
    }

    [Fact]
    public void SchemaNineteenRetainsOwnerMandatesWithoutAuthorizingAnotherApproach()
    {
        var world = FreeAgentTests.Available();
        SimulationTests.Commit(world, Allocation.FreeAgentRecruitment, "old-offer");
        world.SchemaVersion = 19; world.SimulationVersion = "rival-minimum-squad-19";
        var quote = Proposals.Preview(world, new(Allocation.InjectCapital) { Amount = 100 }, world.Revision);
        var bytes = WorldCodec.Encode(world); var original = bytes.ToArray(); var loaded = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!; expected["SchemaVersion"] = 23; expected["SimulationVersion"] = "compact-journal-23";
        Assert.True(JsonNode.DeepEquals(expected, System.Text.Json.JsonSerializer.SerializeToNode(loaded))); Assert.Equal(original, bytes);
        Assert.Null(FreeAgents.Recommend(loaded)); Assert.Single(loaded.FreeAgentBids);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(loaded, "old-quote", loaded.Revision, quote));
        var replay = WorldCodec.Clone(loaded); Simulation.AdvanceWeek(loaded); Simulation.AdvanceWeek(replay);
        Assert.Equal(WorldCodec.Encode(loaded), WorldCodec.Encode(replay));
        var corrupt = JsonNode.Parse(bytes)!; corrupt["OwnerCash"] = world.OwnerCash + 1;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(corrupt.ToJsonString())));
    }
}
