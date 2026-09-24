using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class RecruitmentMarketTests
{
    private static World AtWeek(int week, ulong seed = 2026)
    {
        var world = WorldFactory.Create(seed);
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        while (world.Week < week) Simulation.AdvanceWeek(world);
        return world;
    }

    [Theory]
    [InlineData(26, false)]
    [InlineData(27, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void WindowBoundariesAndWaitingAreEnforced(int week, bool open)
    {
        var world = AtWeek(week);
        var proposal = Proposals.Preview(world, new(Allocation.MidseasonWait), world.Revision);
        Assert.Equal(open, proposal.BlockingReasons.IsEmpty);
        Assert.Equal(open, RecruitmentMarket.Available(world));
        if (!open) return;
        var cash = world.OwnedClub.Cash;
        var plan = world.History.Single(h => h.Command.Allocation == Allocation.PreserveReserve);
        Proposals.Commit(world, "midseason-wait", world.Revision, proposal);
        Assert.Equal(cash, world.OwnedClub.Cash);
        Assert.Empty(world.Negotiations);
        Assert.True(world.AllocationChosen);
        Assert.Contains(plan, world.History);
        Assert.False(RecruitmentMarket.Available(WorldCodec.Clone(world)));
        Assert.NotEmpty(Proposals.Preview(world, new(Allocation.MidseasonRecruitment), world.Revision).BlockingReasons);
    }

    [Fact]
    public void RecommendationsArePureDistinctAndRespectRivalForwardCover()
    {
        var world = AtWeek(27);
        var before = WorldCodec.Encode(world);
        var first = Proposals.Preview(world, new(Allocation.MidseasonRecruitment), world.Revision);
        var value = Proposals.Preview(world, new(Allocation.MidseasonValue), world.Revision);
        Assert.Empty(first.BlockingReasons); Assert.Empty(value.BlockingReasons);
        Assert.Equal(before, WorldCodec.Encode(world));
        Assert.NotEqual(first.Recruitment!.Player.Id, value.Recruitment!.Player.Id);
        Assert.True(value.UpfrontCash < first.UpfrontCash);
        Assert.True(value.TotalCommitment < first.TotalCommitment);
        Assert.Equal(first.UpfrontCash + first.WeeklyCost * (first.Recruitment.ContractEndWeek - world.Week - 1), first.TotalCommitment);
        foreach (var option in new[] { first.Recruitment, value.Recruitment })
        {
            var seller = world.Clubs.Single(c => c.Players.Any(p => p.Id == option.Player.Id));
            Assert.DoesNotContain(seller.Players.Where(p => p.Role == Role.Forward).OrderByDescending(p => p.Ability)
                .ThenBy(p => p.Id.Value).Take(2), p => p.Id == option.Player.Id);
        }
        Assert.Equal(first.Id, Proposals.Preview(WorldCodec.Clone(world), first.Command, world.Revision).Id);
    }

    [Fact]
    public void LastDayMandateReplaysAndSettlesOneBalancedTransferAtMost()
    {
        var successes = 0; var failures = 0;
        for (ulong seed = 0; seed < 4; seed++)
        {
            var world = AtWeek(30, seed);
            var beforeCash = world.OwnedClub.Cash;
            var proposal = Proposals.Preview(world, new(Allocation.MidseasonValue), world.Revision);
            Assert.Empty(proposal.BlockingReasons);
            var receipt = Proposals.Commit(world, "window-mandate", world.Revision, proposal);
            Assert.Equal(beforeCash, world.OwnedClub.Cash);
            var bid = Assert.Single(world.Negotiations);
            Assert.Equal(31, bid.ExpiryWeek);
            var replay = WorldCodec.Clone(world);
            Assert.Equal(proposal.Recruitment, replay.History.Last().Recruitment);
            Assert.Equal(receipt, Proposals.Commit(replay, "window-mandate", proposal.Revision, proposal));
            Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(replay);
            Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(replay));
            var transfers = world.Journal.Where(j => j.Kind == CashKind.Transfer).ToArray();
            Assert.Equal(0, transfers.Sum(j => j.Amount));
            Assert.Equal(864, world.Clubs.Sum(c => c.Players.Count));
            Assert.Empty(world.Negotiations);
            Assert.Single(world.Clubs.SelectMany(c => c.Players), p => p.Id == bid.PlayerId);
            if (transfers.Length == 2)
            {
                successes++;
                var signed = world.OwnedClub.Players.Single(p => p.Id == bid.PlayerId);
                var obligation = Assert.Single(world.Obligations, o => o.ClubId == world.OwnedClubId && o.Description == $"Player contract {signed.ContractId.Value}");
                Assert.Equal(32, obligation.StartWeek);
                Assert.Equal(proposal.Recruitment!.ContractEndWeek, obligation.EndWeek);
                Assert.Equal(-proposal.WeeklyCost, obligation.WeeklyAmount);
            }
            else { Assert.Empty(transfers); failures++; }
        }
        Assert.True(successes > 0 && failures > 0);
    }

    [Fact]
    public void SigningRechecksAffordabilityAndWindowBeforeSpending()
    {
        var world = AtWeek(27);
        SimulationTests.Commit(world, Allocation.MidseasonRecruitment);
        Finance.Post(world, WorldFactory.Account(world.OwnedClubId), -world.OwnedClub.Cash, CashKind.Operations, "cash-stress");
        Simulation.AdvanceWeek(world);
        Assert.Empty(world.Negotiations);
        Assert.DoesNotContain(world.Journal, j => j.Kind == CashKind.Transfer);
        var expired = AtWeek(30);
        SimulationTests.Commit(expired, Allocation.MidseasonValue);
        expired.Negotiations[0] = expired.Negotiations[0] with { ExpiryWeek = 30 };
        Simulation.AdvanceWeek(expired);
        Assert.DoesNotContain(expired.Journal, j => j.Kind == CashKind.Transfer);
    }

    [Fact]
    public void SchemaFourMigrationPreservesPendingOpeningMandateAndCash()
    {
        var world = WorldFactory.Create(8);
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.Recruitment);
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 4; legacy["SimulationVersion"] = "competition-4"; legacy.Remove("CalendarStartSeason");
        foreach (var history in legacy["History"]!.AsArray()) history!.AsObject().Remove("Recruitment");
        world.History = world.History.Select(h => h with { Recruitment = null }).ToList();
        world.CalendarStartSeason = 2;
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(source, bytes);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(migrated));
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(migrated);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(migrated));
    }
}
