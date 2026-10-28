using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class RenewalWageBasisTests
{
    [Theory]
    [InlineData(34000, 26, 0, 102000)]
    [InlineData(340000, 26, 0, 255000)]
    [InlineData(34000, 22, 6, 117000)]
    [InlineData(34000, 22, 0, 112000)]
    [InlineData(34000, 30, 0, 92000)]
    public void SameDivisionRenewalsReassessPayBeforeStandingAdjustments(long current, int age, int abilityOffset, long expected)
    {
        var world = ContractTests.Expiring(); var club = world.OwnedClub;
        Assert.Equal(2, club.Division);
        var player = club.Players[0] with { WeeklyWage = current, Age = age, Ability = Contracts.Par(2) + abilityOffset };
        club.Players[0] = player;
        var before = WorldCodec.Encode(world);
        var plan = Contracts.Plan(world, club, 2);
        Assert.False(plan.RaisesHeld);
        var offer = plan.Reviews.Single(r => r.PlayerId == player.Id);
        Assert.Equal(ContractAction.Renew, offer.Recommended); Assert.Equal(expected, offer.OfferedWage);
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Fact]
    public void RepeatedRegularRenewalsApproachTheReferenceInsteadOfKeepingAcademyPay()
    {
        var world = ContractTests.Expiring(); var club = world.OwnedClub;
        var player = club.Players[0] with { WeeklyWage = 34000, Age = 26, Ability = Contracts.Par(2) };
        var wages = new List<long>();
        // Isolate repeated pricing at fixed standing; this is not a claim about aging or future match outcomes.
        for (var i = 0; i < 4; i++)
        {
            club.Players[0] = player;
            var offer = Contracts.Plan(world, club, 2).Reviews.Single(r => r.PlayerId == player.Id);
            wages.Add(offer.OfferedWage); player = player with { WeeklyWage = offer.OfferedWage };
        }
        Assert.Equal(new long[] { 102000, 136000, 153000, 162000 }, wages);
    }

    [Fact]
    public void WageLimitStillHoldsRaisesAtCurrentPay()
    {
        var world = ContractTests.Expiring(); var club = world.OwnedClub;
        club.Players = club.Players.Select(p => p with { WeeklyWage = 150000, Ability = Contracts.Par(2) + 6, Age = 22 }).ToList();
        club.AnnualBroadcast = 0; club.AnnualSponsor = 0;
        world.Journal.RemoveAll(j => j.Account == WorldFactory.Account(club.Id));
        var plan = Contracts.Plan(world, club, 2);
        Assert.True(plan.RaisesHeld);
        Assert.All(plan.Reviews, r => Assert.Equal(150000, r.OfferedWage));
        Assert.Empty(Contracts.Blocking(club, plan));
    }

    [Fact]
    public void ApprovedRenewalsKeepQuotedTermsAndLeaveUnexpiredContractsUntouched()
    {
        var world = ContractTests.Expiring(); var club = world.OwnedClub;
        var unchanged = club.Players[0] with { ContractEndWeek = 104 };
        club.Players[0] = unchanged;
        world.Obligations = world.Obligations.Select(o => o.Description == $"Player contract {unchanged.ContractId.Value}"
            ? o with { EndWeek = 104 } : o).ToList();
        var before = WorldCodec.Encode(world);
        var quote = Proposals.Preview(world, new(Allocation.StartNextSeason) { DeclineAcademy = true }, world.Revision);
        Assert.Contains("halfway", quote.Uncertainty); Assert.Empty(quote.BlockingReasons);
        Assert.Equal(before, WorldCodec.Encode(world));
        var replay = WorldCodec.Clone(world);
        Proposals.Commit(world, "renew", world.Revision, quote);
        Proposals.Commit(replay, "renew", replay.Revision, quote);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(replay));
        Assert.Equal(unchanged with { SeasonYellows = 0 }, club.Players.Single(p => p.Id == unchanged.Id));
        foreach (var review in quote.Renewal!.Contracts.Where(r => r.Chosen == ContractAction.Renew))
        {
            var player = club.Players.Single(p => p.Id == review.PlayerId);
            var wage = Assert.Single(world.Obligations, o => o.Description == $"Player contract {player.ContractId.Value}");
            Assert.Equal((53, 52 + review.Years * 52, -review.OfferedWage), (wage.StartWeek, wage.EndWeek, wage.WeeklyAmount));
        }
        SimulationTests.Commit(world, Allocation.PreserveReserve, "plan-2"); Simulation.AdvanceWeek(world);
        Assert.Equal(-club.Players.Sum(p => p.WeeklyWage), world.Journal.Where(j => j.Week == 53
            && j.Account == WorldFactory.Account(club.Id) && j.Kind == CashKind.Wages).Sum(j => j.Amount));
    }

    [Fact]
    public void SchemaTwentyMigrationPreservesSignedTermsAndPendingMandates()
    {
        var world = FreeAgentTests.Available(); SimulationTests.Commit(world, Allocation.FreeAgentRecruitment);
        world.SchemaVersion = 20; world.SimulationVersion = "owner-minimum-squad-20";
        var quote = Proposals.Preview(world, new(Allocation.InjectCapital) { Amount = 100 }, world.Revision);
        var bytes = WorldCodec.Encode(world); var original = bytes.ToArray(); var migrated = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!; expected["SchemaVersion"] = 24; expected["SimulationVersion"] = "owner-mandates-24";
        Assert.True(JsonNode.DeepEquals(expected, System.Text.Json.JsonSerializer.SerializeToNode(migrated))); Assert.Equal(original, bytes);
        Assert.Single(migrated.FreeAgentBids);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(migrated, "stale", migrated.Revision, quote));
        var invalid = JsonNode.Parse(bytes)!; invalid["OwnerCash"] = world.OwnerCash + 1;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(invalid.ToJsonString())));
    }
}
