using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class RivalMinimumSquadTests
{
    private static (World World, Club Club, Player Candidate) Market(int squad = 15, int week = 2)
    {
        var world = ControlledWorld.Create();
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.PreserveReserve);
        while (world.Week < week) Simulation.AdvanceWeek(world);
        var club = world.Clubs.First(c => c.Id != world.OwnedClubId);
        var minimum = club.Players.GroupBy(p => p.Role).SelectMany(g => g.Take(Contracts.Minimum.Single(m => m.Role == g.Key).Minimum)).ToList();
        var retained = minimum.Concat(club.Players.Except(minimum).Take(squad - minimum.Count)).ToList();
        var removed = club.Players.Except(retained).ToArray(); club.Players = retained;
        // Controlled expiries preserve people and every payment already made, with no future wages after release.
        foreach (var player in removed)
        {
            world.Obligations = world.Obligations.Select(o => o.Description == $"Player contract {player.ContractId.Value}" ? o with { EndWeek = world.Week } : o).ToList();
            world.FreeAgents.Add(new(player with { ContractEndWeek = world.Week, TrainingExposure = 0, Age = 25, Ability = 1, WeeklyWage = player == removed[0] ? 10000 : 1000000000 }, club.Id, world.Week));
        }
        Finance.Post(world, WorldFactory.Account(club.Id), 1000000000 - club.Cash, CashKind.Rescue, "Controlled test funding");
        WorldFactory.Validate(world);
        return (world, club, world.FreeAgents[0].Player);
    }

    [Fact]
    public void BelowSixteenCanSignWeakerDepthOutsideWindowWithExactWagesAndReplay()
    {
        var (world, club, candidate) = Market();
        Assert.Equal(15, club.Players.Count); Assert.All(Contracts.Minimum, m => Assert.False(FreeAgents.HasShortage(club, m.Role)));
        var obligations = world.Obligations.Count;
        Simulation.AdvanceWeek(world);
        Assert.False(FreeAgents.WindowOpen(world, world.Week));
        var bid = Assert.Single(world.RivalApproaches);
        Assert.Equal(candidate.Id, bid.Player.Id); Assert.Equal(ApproachOutcome.Pending, bid.Outcome);
        Assert.Equal(obligations, world.Obligations.Count);
        while (RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{bid.Id}", 100) >= 70) world.Seed++;
        var saved = WorldCodec.Clone(world);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(saved);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(saved));
        Assert.Equal(16, club.Players.Count); Assert.Equal(ApproachOutcome.Signed, world.RivalApproaches[0].Outcome);
        var signed = Assert.Single(club.Players, p => p.Id == candidate.Id);
        var wage = Assert.Single(world.Obligations, o => o.Description == $"Player contract {signed.ContractId.Value}");
        Assert.Equal((5, bid.ContractEndWeek, -bid.WeeklyWage), (wage.StartWeek, wage.EndWeek, wage.WeeklyAmount));
        Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{wage.Id.Value}/4");
        Simulation.AdvanceWeek(world);
        Assert.Contains(world.Journal, j => j.Reference == $"obligation/{wage.Id.Value}/5" && j.Amount == -bid.WeeklyWage);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    public void CoveredSquadsDoNotGainOutsideWindowUpgrades(int squad)
    {
        var (world, _, _) = Market(squad);
        world.FreeAgents = world.FreeAgents.Select(f => f with { Player = f.Player with { Ability = 100 } }).ToList();
        Simulation.AdvanceWeek(world);
        Assert.Empty(world.RivalApproaches);
    }

    [Fact]
    public void TotalGapDoesNotRecruitBeyondWorkingRoleTarget()
    {
        var (world, _, _) = Market();
        world.FreeAgents = world.FreeAgents.Select(f => f with { Player = f.Player with { Role = Role.Goalkeeper, Age = 25, Ability = 100 } }).ToList();
        Simulation.AdvanceWeek(world);
        Assert.Empty(world.RivalApproaches);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CoveredSquadsStillRequireAnImprovementInsideTheWindow(bool stronger)
    {
        var (world, _, candidate) = Market(16, 1);
        if (stronger) world.FreeAgents[0] = world.FreeAgents[0] with { Player = candidate with { Ability = 100 } };
        Simulation.AdvanceWeek(world);
        Assert.True(FreeAgents.WindowOpen(world, world.Week));
        if (stronger) Assert.Equal(candidate.Id, Assert.Single(world.RivalApproaches).Player.Id);
        else Assert.Empty(world.RivalApproaches);
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("wages")]
    [InlineData("forecast")]
    [InlineData("arrears")]
    public void EmergencyDepthStillRequiresOrdinaryFinances(string gate)
    {
        var (world, club, _) = Market();
        if (gate == "cash") Finance.Post(world, WorldFactory.Account(club.Id), -club.Cash, CashKind.Operations, "Controlled cash constraint");
        if (gate == "wages") club.Players = club.Players.Select(p => p with { WeeklyWage = 1000000000 }).ToList();
        if (gate == "forecast") WorldFactory.AddObligation(world, club.Id, 10, 10, -club.Cash * 2, CashKind.Operations, "Future liability");
        if (gate == "arrears") world.Arrears.Add(new(world.Obligations.First(o => o.ClubId == club.Id).Id, club.Id, world.Week, club.Cash * 2));
        Simulation.AdvanceWeek(world);
        Assert.Empty(world.RivalApproaches);
    }

    [Fact]
    public void TotalNeedIsRecheckedAndARefusalRetainsTheFourWeekCooldown()
    {
        var (world, club, _) = Market(); Simulation.AdvanceWeek(world);
        var bid = Assert.Single(world.RivalApproaches);
        var filled = WorldCodec.Clone(world); var other = filled.FreeAgents.Last();
        filled.FreeAgents.Remove(other);
        var replacement = other.Player with { ContractId = new(90000), ContractEndWeek = 52, WeeklyWage = 10000 };
        filled.Clubs.Single(c => c.Id == club.Id).Players.Add(replacement);
        WorldFactory.AddObligation(filled, club.Id, filled.Week + 1, 52, -replacement.WeeklyWage, CashKind.Wages, $"Player contract {replacement.ContractId.Value}");
        WorldFactory.Validate(filled);
        Simulation.AdvanceWeek(filled);
        Assert.Equal(ApproachOutcome.ChecksFailed, filled.RivalApproaches[0].Outcome);
        while (RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{bid.Id}", 100) < 70) world.Seed++;
        Simulation.AdvanceWeek(world);
        Assert.Equal(ApproachOutcome.Declined, world.RivalApproaches[0].Outcome); Assert.Equal(15, club.Players.Count);
        while (world.Week < bid.ApprovedWeek + 3) Simulation.AdvanceWeek(world);
        Assert.Single(world.RivalApproaches);
        Simulation.AdvanceWeek(world); Assert.Equal(2, world.RivalApproaches.Count);
    }

    [Fact]
    public void SchemaEighteenMigrationPreservesPendingTermsAndRejectsOldQuotes()
    {
        var (world, _) = RivalRecruitmentTests.Market(1);
        Simulation.AdvanceWeek(world); Assert.Single(world.RivalApproaches);
        world.SchemaVersion = 18; world.SimulationVersion = "opening-population-18";
        var quote = Proposals.Preview(world, new(Allocation.InjectCapital) { Amount = 100 }, world.Revision);
        var bytes = WorldCodec.Encode(world); var original = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!; expected["SchemaVersion"] = 19; expected["SimulationVersion"] = "rival-minimum-squad-19";
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(WorldCodec.Encode(migrated)))); Assert.Equal(original, bytes);
        var before = WorldCodec.Encode(migrated);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(migrated, "stale", migrated.Revision, quote));
        Assert.Equal(before, WorldCodec.Encode(migrated));
        var saved = WorldCodec.Clone(migrated); Simulation.AdvanceWeek(migrated); Simulation.AdvanceWeek(saved);
        Assert.Equal(WorldCodec.Encode(migrated), WorldCodec.Encode(saved));
        var corrupt = JsonNode.Parse(bytes)!; corrupt["OwnerCash"] = world.OwnerCash + 1;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(corrupt.ToJsonString())));
    }
}
