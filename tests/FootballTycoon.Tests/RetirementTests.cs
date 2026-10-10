using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class RetirementTests
{
    internal static World Opening()
    {
        var world = ControlledWorld.Create();
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        return world;
    }

    internal static Player Veteran(World world, Club club, Player player, int age, int end)
    {
        var changed = player with { Age = age, ContractEndWeek = end };
        club.Players[club.Players.IndexOf(player)] = changed;
        world.Obligations = world.Obligations.Select(o => o.Description == $"Player contract {player.ContractId.Value}" ? o with { EndWeek = end } : o).ToList();
        return changed;
    }

    private static Proposal Renewal(World world) => Proposals.Preview(world, new OwnerCommand(Allocation.StartNextSeason) { DeclineAcademy = true }, world.Revision);

    [Fact]
    public void GoalkeepersRetireWithoutForcedRenewalAndEveryLastWageRemainsPaid()
    {
        var world = Opening();
        var keepers = world.OwnedClub.Players.Where(p => p.Role == Role.Goalkeeper).ToArray()
            .Select(p => Veteran(world, world.OwnedClub, p, 41, 52)).ToArray();
        var contracts = keepers.Select(p => world.Obligations.Single(o => o.Description == $"Player contract {p.ContractId.Value}")).ToArray();
        Simulation.AdvanceWeek(world);
        Assert.Equal(2, world.RetirementNotices.Count);
        Assert.All(world.RetirementNotices, n => { Assert.Equal(1, n.AnnouncedWeek); Assert.Equal(52, n.RetirementWeek); });
        var loaded = WorldCodec.Clone(world);
        while (world.Week < 52) { Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(loaded); }
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
        Assert.Empty(world.Retirements); // Frozen annual review, before mandatory departures are applied.
        var before = WorldCodec.Encode(world); var proposal = Renewal(world);
        Assert.Equal(before, WorldCodec.Encode(world)); Assert.Empty(proposal.BlockingReasons);
        Assert.Equal(2, proposal.Renewal!.Retired); Assert.Contains(proposal.Renewal.Shortages, s => s.StartsWith("Goalkeeper: 0"));
        var forbidden = Proposals.Preview(world, new OwnerCommand(Allocation.StartNextSeason) { DeclineAcademy = true, ContractOverrides = [keepers[0].Id] }, world.Revision);
        Assert.Contains(forbidden.BlockingReasons, s => s.Contains("cannot be reversed"));
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "invalid-reversal", world.Revision, forbidden));
        var receipt = Proposals.Commit(world, "retirement-renewal", world.Revision, proposal);
        Assert.Equal(receipt, Proposals.Commit(world, "retirement-renewal", proposal.Revision, proposal));
        Assert.Equal(2, world.Retirements.Count);
        foreach (var keeper in keepers)
        {
            Assert.DoesNotContain(world.OwnedClub.Players, p => p.Id == keeper.Id);
            Assert.DoesNotContain(world.FreeAgents, f => f.Player.Id == keeper.Id);
            Assert.Contains(world.Retirements, r => r.PlayerId == keeper.Id && r.Age == 42 && r.Week == 52);
            Assert.Contains(world.SeasonSummaries.Single().Development, p => p.PlayerId == keeper.Id && p.Name == keeper.Name);
        }
        SimulationTests.Commit(world, Allocation.PreserveReserve, "new-year");
        var matchWeek = world.Fixtures.Where(f => f.Week > 52 && (f.Home == world.OwnedClubId || f.Away == world.OwnedClubId)).Min(f => f.Week);
        while (world.Week < matchWeek) Simulation.AdvanceWeek(world);
        foreach (var contract in contracts)
        {
            Assert.Equal(contract, world.Obligations.Single(o => o.Id == contract.Id));
            Assert.Contains(world.Journal, j => j.Reference == $"obligation/{contract.Id.Value}/52" && j.Amount == contract.WeeklyAmount);
            Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{contract.Id.Value}/53");
        }
        Assert.Contains(world.Results, r => r.Week == matchWeek && (r.Home == world.OwnedClubId || r.Away == world.OwnedClubId));
        Assert.Equal(CareerStatus.Active, world.Status);
    }

    [Fact]
    public void VoluntaryReleasesStillCannotMakeMandatoryShortagesWorse()
    {
        var world = Opening(); var players = world.OwnedClub.Players.Take(3).ToArray();
        foreach (var player in players) Veteran(world, world.OwnedClub, player, player.Role == Role.Goalkeeper ? 41 : 39, 52);
        while (world.Week < 52) Simulation.AdvanceWeek(world);
        var recommended = Renewal(world); Assert.Empty(recommended.BlockingReasons);
        var another = recommended.Renewal!.Contracts.First(c => c.Recommended == ContractAction.Renew);
        var release = Proposals.Preview(world, new OwnerCommand(Allocation.StartNextSeason) { DeclineAcademy = true, ContractOverrides = [another.PlayerId] }, world.Revision);
        Assert.NotEmpty(release.BlockingReasons);
    }

    [Fact]
    public void LossOfControlAtTheAnnualBoundaryDoesNotArchiveUnagedRetirees()
    {
        var world = Opening();
        var keeper = Veteran(world, world.OwnedClub, world.OwnedClub.Players.First(p => p.Role == Role.Goalkeeper), 41, 52);
        while (world.Week < 51) Simulation.AdvanceWeek(world);
        Assert.True(RetirementLifecycle.Announced(world, keeper.Id));
        Finance.Post(world, WorldFactory.Account(world.OwnedClubId), -world.OwnedClub.Cash, CashKind.Operations, "stress");
        var obligation = world.Obligations.First(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages);
        world.Arrears.Add(new(obligation.Id, world.OwnedClubId, 48, 900000000));
        world.Status = CareerStatus.Administration; world.AdministrationWeek = 48;
        var result = Simulation.AdvanceWeek(world);
        Assert.Equal(52, world.Week); Assert.Equal(CareerStatus.LostControl, world.Status);
        Assert.Equal("LostControl", result.StopReason);
        Assert.Empty(world.Retirements);
        Assert.Contains(world.OwnedClub.Players, p => p.Id == keeper.Id);
        WorldFactory.Validate(world);
    }

    [Fact]
    public void ALongLiveContractIsNotCutWhenTheAgeThresholdIsReached()
    {
        var world = Opening(); var player = Veteran(world, world.OwnedClub, world.OwnedClub.Players.First(p => p.Role == Role.Forward), 39, 156);
        while (world.Week < 104)
        {
            if (world.Status == CareerStatus.SeasonReview)
            {
                var quote = Renewal(world); Proposals.Commit(world, "renew-1", world.Revision, quote);
                SimulationTests.Commit(world, Allocation.PreserveReserve, "plan-2");
            }
            Simulation.AdvanceWeek(world);
        }
        Assert.Contains(world.OwnedClub.Players, p => p.Id == player.Id && p.ContractId == player.ContractId && p.ContractEndWeek == 156);
        Assert.DoesNotContain(world.Retirements, r => r.PlayerId == player.Id);
        Assert.Contains(world.RetirementNotices, n => n.PlayerId == player.Id && n.AnnouncedWeek == 104 && n.RetirementWeek == 156);
        var terms = Renewal(world); Proposals.Commit(world, "renew-2", world.Revision, terms);
        SimulationTests.Commit(world, Allocation.PreserveReserve, "plan-3");
        while (world.Week < 156) Simulation.AdvanceWeek(world);
        Assert.Equal(CareerStatus.PrototypeComplete, world.Status);
        Assert.Contains(world.Retirements, r => r.PlayerId == player.Id && r.Week == 156 && r.Age == 42);
        Assert.DoesNotContain(world.OwnedClub.Players, p => p.Id == player.Id);
        Assert.Contains(world.SeasonSummaries.Last().Development, p => p.PlayerId == player.Id);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    [Fact]
    public void MidseasonExpiryPreservesArrearsAndDoesNotAllowATransferExtension()
    {
        var world = Opening(); var target = RecruitmentMarket.Recommend(world, Allocation.Recruitment)!;
        var seller = world.Clubs.Single(c => c.Players.Any(p => p.Id == target.Player.Id));
        var player = Veteran(world, seller, seller.Players.Single(p => p.Id == target.Player.Id), 40, 5);
        Simulation.AdvanceWeek(world);
        Assert.NotEqual(player.Id, RecruitmentMarket.Recommend(world, Allocation.Recruitment)?.Player.Id);
        var debt = world.Obligations.Single(o => o.Description == $"Player contract {player.ContractId.Value}");
        // Genuine unpaid debt is retained after the person retires; no forgiveness is implied.
        world.Arrears.Add(new(debt.Id, seller.Id, 1, long.MaxValue / 4));
        while (world.Week < 5) Simulation.AdvanceWeek(world);
        Assert.Contains(world.Retirements, r => r.PlayerId == player.Id && r.Week == 5);
        Assert.Contains(world.Arrears, a => a.ObligationId == debt.Id);
        Assert.Equal(5, world.Obligations.Single(o => o.Id == debt.Id).EndWeek);
    }

    [Fact]
    public void MigrationAnnouncesCurrentContractsWithoutChangingEmploymentOrMoney()
    {
        var world = Opening(); Veteran(world, world.OwnedClub, world.OwnedClub.Players[0], 41, 52);
        var node = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        node["SchemaVersion"] = 14; node["SimulationVersion"] = "rival-market-14"; node.Remove("RetirementNotices");
        var bytes = Encoding.UTF8.GetBytes(node.ToJsonString()); var copy = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(copy, bytes); Assert.Equal(19, migrated.SchemaVersion); Assert.Single(migrated.RetirementNotices);
        Assert.Empty(migrated.Retirements);
        Assert.Equal(world.Clubs.SelectMany(c => c.Players), migrated.Clubs.SelectMany(c => c.Players));
        Assert.Equal(world.Obligations, migrated.Obligations); Assert.Equal(world.Journal, migrated.Journal); Assert.Equal(world.RandomStates, migrated.RandomStates);
        migrated.RetirementNotices.Add(migrated.RetirementNotices[0]);
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(migrated));
    }

    [Fact]
    public void ApprovedTransferCannotExtendANewlyAnnouncedRetirement()
    {
        var world = ControlledWorld.Create();
        SimulationTests.Commit(world, Allocation.Acquire);
        var target = RecruitmentMarket.Recommend(world, Allocation.Recruitment)!;
        var seller = world.Clubs.Single(c => c.Players.Any(p => p.Id == target.Player.Id));
        Veteran(world, seller, seller.Players.Single(p => p.Id == target.Player.Id), 40, 52);
        SimulationTests.Commit(world, Allocation.Recruitment);
        Simulation.AdvanceWeek(world);
        Assert.Contains(world.RetirementNotices, n => n.PlayerId == target.Player.Id);
        Assert.DoesNotContain(world.OwnedClub.Players, p => p.Id == target.Player.Id);
        Assert.Contains(seller.Players, p => p.Id == target.Player.Id && p.ContractId == target.Player.ContractId && p.ContractEndWeek == 52);
        Assert.Contains(world.Reviews, r => r.Title == "Recruitment closed without a signing");
        Assert.DoesNotContain(world.Journal, j => j.Kind == CashKind.Transfer);
    }
}
