using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace FootballTycoon.Tests;

public class RivalRecruitmentTests(ITestOutputHelper output)
{
    internal static (World World, Club[] Rivals) Market(int count = 2)
    {
        var world = FreeAgentTests.Available();
        var role = world.FreeAgents.Single().Player.Role;
        var rivals = world.Clubs.Where(c => c.Id != world.OwnedClubId).Take(count).ToArray();
        foreach (var club in rivals)
        {
            club.Players.RemoveAll(p => p.Role == role);
            Finance.Post(world, WorldFactory.Account(club.Id), 1000000000 - club.Cash, CashKind.Rescue, "Controlled test funding");
        }
        return (world, rivals);
    }

    [Fact]
    public void CompetingOffersResolveOnceWithExactPaymentsAndLoadedReplay()
    {
        var (world, rivals) = Market();
        var candidate = world.FreeAgents.Single().Player;
        Simulation.AdvanceWeek(world);
        Assert.Equal(2, world.RivalApproaches.Count);
        Assert.All(world.RivalApproaches, a => Assert.Equal(ApproachOutcome.Pending, a.Outcome));
        Assert.DoesNotContain(rivals.SelectMany(c => c.Players), p => p.Id == candidate.Id);
        // Predetermine acceptance for both independent streams; stable club order must still admit only once.
        while (world.RivalApproaches.Any(a => RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{a.Id}", 100) >= 70)) world.Seed++;
        var saved = WorldCodec.Clone(world);
        var transfers = world.Journal.Count(j => j.Kind == CashKind.Transfer);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(saved);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(saved));
        Assert.Equal(transfers, world.Journal.Count(j => j.Kind == CashKind.Transfer));
        var winner = Assert.Single(world.RivalApproaches, a => a.Outcome == ApproachOutcome.Signed);
        Assert.Equal(rivals.MinBy(c => c.Id.Value)!.Id, winner.ClubId);
        Assert.Single(world.RivalApproaches, a => a.Outcome == ApproachOutcome.Unavailable);
        var player = Assert.Single(world.Clubs.SelectMany(c => c.Players), p => p.Id == candidate.Id);
        var wage = Assert.Single(world.Obligations, o => o.Description == $"Player contract {player.ContractId.Value}");
        Assert.Equal(55, wage.StartWeek); Assert.Equal(156, wage.EndWeek);
        Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{wage.Id.Value}/54");
        Simulation.AdvanceWeek(world);
        Assert.Contains(world.Journal, j => j.Reference == $"obligation/{wage.Id.Value}/55" && j.Amount == -winner.WeeklyWage);
    }

    [Fact]
    public void OwnerAndRivalsCompeteForTheSamePersonWithoutDuplicateContracts()
    {
        var (world, _) = Market();
        Simulation.AdvanceWeek(world);
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Empty(quote.BlockingReasons);
        var decision = world.History.Count + 1;
        while (RandomStreams.Next(WorldCodec.Clone(world), $"free-agent-negotiation/{decision}", 100) >= 70
            || world.RivalApproaches.Any(a => RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{a.Id}", 100) >= 70)) world.Seed++;
        // Changing the seed invalidates previews; obtain the final immutable mandate before committing.
        quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Proposals.Commit(world, "competing-owner", world.Revision, quote);
        var reverse = WorldCodec.Clone(world); reverse.Clubs.Reverse();
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(reverse);
        var holder = Assert.Single(world.Clubs, c => c.Players.Any(p => p.Id == quote.Recruitment!.Player.Id));
        var firstClub = world.RivalApproaches.Select(a => a.ClubId).Append(world.OwnedClubId).MinBy(id => id.Value);
        Assert.Equal(firstClub, holder.Id);
        Assert.Equal(world.RivalApproaches, reverse.RivalApproaches);
        Assert.Equal(holder.Players, reverse.Clubs.Single(c => c.Id == holder.Id).Players);
        Assert.Empty(world.FreeAgents); Assert.Empty(world.FreeAgentBids);
    }

    [Fact]
    public void RefusalIsRetainedAndDoesNotImmediatelyRepeat()
    {
        var (world, _) = Market(1);
        Simulation.AdvanceWeek(world);
        var bid = Assert.Single(world.RivalApproaches);
        while (RandomStreams.Next(WorldCodec.Clone(world), $"rival-free-agent/{bid.Id}", 100) < 70) world.Seed++;
        Simulation.AdvanceWeek(world);
        Assert.Equal(ApproachOutcome.Declined, Assert.Single(world.RivalApproaches).Outcome);
        Assert.Single(world.FreeAgents);
        while (world.Week < 56) Simulation.AdvanceWeek(world);
        Assert.Single(world.RivalApproaches);
        Simulation.AdvanceWeek(world);
        Assert.Equal(2, world.RivalApproaches.Count); // Still a real shortage outside the window.
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("wages")]
    [InlineData("cover")]
    public void NewFactsClosePendingApproachesWithoutSigning(string reason)
    {
        var (world, rivals) = Market(1); var rival = rivals.Single();
        Simulation.AdvanceWeek(world);
        var bid = Assert.Single(world.RivalApproaches);
        if (reason == "cash") WorldFactory.AddObligation(world, rival.Id, 54, 104, -1000000000, CashKind.Operations, "New essential cost");
        if (reason == "wages") rival.Players[0] = rival.Players[0] with { WeeklyWage = 1000000000 };
        if (reason == "cover")
            while (rival.Players.Count < Academy.SquadLimit)
                rival.Players.Add(rival.Players[0] with { Id = new(10000 + rival.Players.Count), Role = bid.Player.Role });
        Simulation.AdvanceWeek(world);
        Assert.Equal(ApproachOutcome.ChecksFailed, Assert.Single(world.RivalApproaches).Outcome);
        Assert.DoesNotContain(rival.Players, p => p.Id == bid.Player.Id);
        Assert.Single(world.FreeAgents);
    }

    [Fact]
    public void MigrationPreservesExistingOwnerMandateAndRejectsInvalidRivalHistory()
    {
        var (world, _) = Market(1);
        SimulationTests.Commit(world, Allocation.FreeAgentRecruitment, "owner-offer");
        var node = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        node["SchemaVersion"] = 13; node["SimulationVersion"] = "free-agents-13"; node.Remove("RivalApproaches");
        var bytes = Encoding.UTF8.GetBytes(node.ToJsonString()); var copy = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(copy, bytes); Assert.Equal(18, migrated.SchemaVersion);
        Assert.Equal(world.FreeAgentBids, migrated.FreeAgentBids); Assert.Equal(world.FreeAgents, migrated.FreeAgents);
        Assert.Equal(world.Journal, migrated.Journal); Assert.Equal(world.RandomStates, migrated.RandomStates);
        Assert.Empty(migrated.RivalApproaches);
        var (invalid, _) = Market(1); Simulation.AdvanceWeek(invalid);
        invalid.RivalApproaches.Add(invalid.RivalApproaches[0]);
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(invalid));
    }

    [Fact]
    public void UnaffordableLargePoolUsesOneBaselineAndLeavesNoPromises()
    {
        var (world, rivals) = Market(1);
        var free = world.FreeAgents.Single();
        world.FreeAgents = Enumerable.Range(0, 500).Select(i => free with { Player = free.Player with { Id = new(200000 + i) } }).ToList();
        WorldFactory.AddObligation(world, rivals.Single().Id, 55, 55, -2000000000, CashKind.Operations, "Committed future cost");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        Simulation.AdvanceWeek(world);
        timer.Stop(); output.WriteLine($"500-candidate unaffordable pool: {timer.ElapsedMilliseconds} ms for one weekly step.");
        Assert.Empty(world.RivalApproaches); Assert.Equal(500, world.FreeAgents.Count);
    }

    [Fact]
    public async Task FailedWeeklySaveDoesNotPublishARecruitmentDecision()
    {
        var (world, _) = Market(1);
        var directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var vault = new LocalSaveVault(directory);
            var saved = vault.Write(WorldCodec.Encode(world), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "market");
            var fail = true;
            var fault = new LocalSaveVault(directory, stage => { if (fail && stage == "before-rename") throw new IOException("Interrupted recruitment save"); });
            await using var session = new GameSession(fault);
            await session.LoadAsync(saved.SnapshotId);
            var before = await session.ExportCheckpointAsync();
            await Assert.ThrowsAsync<IOException>(() => session.AdvanceAsync(AdvanceTarget.Week));
            Assert.Equal(before, await session.ExportCheckpointAsync());
            fail = false;
            await session.AdvanceAsync(AdvanceTarget.Week);
            Assert.Single((await session.QueryAsync()).RivalApproaches);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
