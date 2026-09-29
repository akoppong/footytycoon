using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class TrainingTests
{
    private static World Acquired(ulong seed = 2026)
    {
        var world = WorldFactory.Create(seed);
        SimulationTests.Commit(world, Allocation.Acquire);
        return world;
    }

    [Fact]
    public void TrainingQuoteMatchesSignedCashScheduleWithoutInventingIncome()
    {
        var world = Acquired(); var original = WorldCodec.Encode(world);
        var baseline = Finance.Forecast(world, world.OwnedClubId);
        var proposal = Proposals.Preview(world, new(Allocation.Training), world.Revision);
        Assert.Empty(proposal.BlockingReasons);
        Assert.Equal(original, WorldCodec.Encode(world));
        Assert.Equal(65000000, proposal.UpfrontCash);
        Assert.Equal(100000, proposal.WeeklyCost);
        Assert.Equal(32, proposal.ReviewWeek);
        Assert.Equal(65000000 + 72 * 100000, proposal.TotalCommitment);
        foreach (var point in proposal.Forecast.Points)
        {
            var cost = proposal.UpfrontCash + proposal.WeeklyCost * Math.Max(0, point.Week - 32);
            Assert.Equal(baseline.Points.Single(p => p.Week == point.Week).BaseCash - cost, point.BaseCash);
            Assert.Equal(baseline.Points.Single(p => p.Week == point.Week).DownsideCash - cost, point.DownsideCash);
        }
        Proposals.Commit(world, "training", world.Revision, proposal);
        Assert.True(world.AllocationChosen);
        Assert.Equal(Balance.Load().OpeningClubCash - proposal.UpfrontCash, world.OwnedClub.Cash);
        Assert.Equal(FacilityKind.Training, Assert.Single(world.Projects).Kind);
        var upkeep = Assert.Single(world.Obligations, o => o.Description == "Training staff and upkeep");
        Assert.Equal((33, 104, -100000L), (upkeep.StartWeek, upkeep.EndWeek, upkeep.WeeklyAmount));
        var committed = Finance.Forecast(world, world.OwnedClubId);
        Assert.Equal(proposal.Forecast.Points.Select(p => (p.Week, p.BaseCash, p.DownsideCash)), committed.Points.Select(p => (p.Week, p.BaseCash, p.DownsideCash)));
        Assert.Equal(proposal.Forecast.Points.Skip(1), committed.Points.Skip(1));
        Assert.Equal(0, committed.Points[0].KnownNet); // Upfront payment is already in committed cash.
        Assert.All(world.OwnedClub.Players, p => Assert.Equal(0, p.TrainingExposure));
    }

    [Fact]
    public void TrainingStartsAfterDeliveryAndReplaysAcrossThatBoundary()
    {
        var world = Acquired(); SimulationTests.Commit(world, Allocation.Training);
        while (world.Week < 31) Simulation.AdvanceWeek(world);
        var replay = WorldCodec.Clone(world);
        foreach (var copy in new[] { world, replay })
        {
            Simulation.AdvanceWeek(copy);
            Assert.Equal(1, copy.OwnedClub.TrainingLevel); Assert.Equal(0, copy.OwnedClub.HospitalityLevel);
            Assert.All(copy.OwnedClub.Players, p => Assert.Equal(0, p.TrainingExposure));
            Assert.Contains(copy.Reviews, r => r.Title == "Training center opens" && r.ForecastId == copy.Projects.Single().ForecastId);
            Simulation.AdvanceWeek(copy);
            Assert.All(copy.OwnedClub.Players, p => Assert.Equal(10, p.TrainingExposure));
            var operations = copy.Journal.Where(j => j.Account == WorldFactory.Account(copy.OwnedClubId) && j.Kind == CashKind.Operations).ToArray();
            Assert.Equal(-Balance.Load().WeeklyOperations, operations.Where(j => j.Week == 32).Sum(j => j.Amount));
            Assert.Equal(-Balance.Load().WeeklyOperations - 100000, operations.Where(j => j.Week == 33).Sum(j => j.Amount));
        }
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(replay));
    }

    [Fact]
    public void UpgradesAccrueWeightedExposureAndReportsPreserveItBeforeReset()
    {
        var world = WorldFactory.Create(13);
        world.OwnedClub.Players = world.OwnedClub.Players.Select((p, i) => p with { Age = i == 0 ? 21 : i == 1 ? 26 : 32 }).ToList();
        for (var week = 1; week <= 52; week++)
        {
            world.Week = week; world.OwnedClub.TrainingLevel = week <= 32 ? 1 : 2;
            Facilities.AccrueTraining(world);
        }
        Assert.All(world.OwnedClub.Players, p => Assert.Equal(640, p.TrainingExposure));
        var restored = WorldCodec.Clone(world);
        foreach (var copy in new[] { world, restored })
        {
            Seasons.Close(copy);
            var report = copy.SeasonSummaries.Single().Development;
            Assert.Equal(12, report[0].TrainingBonus); Assert.Equal(6, report[1].TrainingBonus);
            Assert.All(report.Skip(2), p => Assert.Equal(0, p.TrainingBonus));
            Assert.Contains("12 percentage points", report[0].Evidence);
            Assert.All(copy.Clubs.SelectMany(c => c.Players), p => Assert.Equal(0, p.TrainingExposure));
        }
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(restored));
    }

    [Fact]
    public void ATransferredPlayerKeepsPriorExposureAndUsesTheBuyersFacilityInTransferWeek()
    {
        var signed = false;
        for (ulong seed = 1; seed <= 20 && !signed; seed++)
        {
            var world = Acquired(seed); world.Week = 1;
            world.OwnedClub.TrainingLevel = 1;
            var target = RecruitmentMarket.Recommend(world, Allocation.Recruitment)!.Player;
            var seller = world.Clubs.Single(c => c.Players.Any(p => p.Id == target.Id));
            seller.TrainingLevel = 2;
            seller.Players = seller.Players.Select(p => p.Id == target.Id ? p with { TrainingExposure = 16 } : p).ToList();
            SimulationTests.Commit(world, Allocation.Recruitment);
            Simulation.AdvanceWeek(world);
            if (world.OwnedClub.Players.SingleOrDefault(p => p.Id == target.Id) is not { } bought) continue;
            signed = true;
            Assert.Equal(26, bought.TrainingExposure);
            Assert.DoesNotContain(seller.Players, p => p.Id == target.Id);
            Assert.Equal(26, WorldCodec.Clone(world).OwnedClub.Players.Single(p => p.Id == target.Id).TrainingExposure);
        }
        Assert.True(signed, "At least one seeded negotiation must succeed to exercise the transfer.");
    }

    [Fact]
    public void FacilityBenefitsImproveChancesWithoutGuaranteeingGrowthOrChangingRngConsumption()
    {
        var trained = WorldFactory.Create(73); trained.Week = 52;
        foreach (var club in trained.Clubs)
            club.Players = club.Players.Select(p => p with { Age = 19, Ability = 70, TrainingExposure = 1040 }).ToList();
        var baseline = WorldCodec.Clone(trained);
        foreach (var club in baseline.Clubs) club.Players = club.Players.Select(p => p with { TrainingExposure = 0 }).ToList();
        Seasons.Close(trained); Seasons.Close(baseline);
        var without = baseline.Clubs.SelectMany(c => c.Players).ToDictionary(p => p.Id);
        var after = trained.Clubs.SelectMany(c => c.Players).ToArray();
        Assert.All(after, p => Assert.True(p.Ability >= without[p.Id].Ability));
        Assert.Contains(after, p => p.Ability > without[p.Id].Ability);
        Assert.Contains(after, p => p.Ability == 70);
        Assert.Equal(baseline.RandomStates, trained.RandomStates);
    }

    [Fact]
    public void AnnualPlanConstructionAndThreeStepLimitsAreEnforced()
    {
        var world = Acquired(); SimulationTests.Commit(world, Allocation.Training);
        Assert.Contains(Proposals.Preview(world, new(Allocation.Hospitality), world.Revision).BlockingReasons, r => r.Contains("capital plan"));
        world.AllocationChosen = false;
        Assert.Contains(Proposals.Preview(world, new(Allocation.Training), world.Revision).BlockingReasons, r => r.Contains("one construction"));
        world.Projects.Clear(); world.OwnedClub.TrainingLevel = 3;
        Assert.Contains(Proposals.Preview(world, new(Allocation.Training), world.Revision).BlockingReasons, r => r.Contains("three-step"));
        Assert.Equal(80000000, Facilities.TrainingCost(1)); Assert.Equal(95000000, Facilities.TrainingCost(2));
        Assert.Equal(125000, Facilities.TrainingUpkeep(1)); Assert.Equal(150000, Facilities.TrainingUpkeep(2));
    }

    [Fact]
    public void UpkeepRenewsOnceAndSeasonReportsLinkTheTrainingPlan()
    {
        var world = Acquired(); SimulationTests.Commit(world, Allocation.Training);
        while (world.Week < 52) Simulation.AdvanceWeek(world);
        Assert.Contains("training", world.SeasonSummaries.Single().Plan);
        Assert.Contains(world.SeasonSummaries.Single().Development, p => p.TrainingBonus == 3);
        SimulationTests.Commit(world, Allocation.StartNextSeason, "renew-2");
        SimulationTests.Commit(world, Allocation.PreserveReserve, "plan-2");
        while (world.Week < 104) Simulation.AdvanceWeek(world);
        SimulationTests.Commit(world, Allocation.StartNextSeason, "renew-3");
        var signed = world.Obligations.Where(o => o.Description == "Training staff and upkeep").ToArray();
        Assert.Equal(2, signed.Length);
        Assert.Equal((33, 104), (signed[0].StartWeek, signed[0].EndWeek));
        Assert.Equal((105, 156), (signed[1].StartWeek, signed[1].EndWeek));
        SimulationTests.Commit(world, Allocation.PreserveReserve, "plan-3"); Simulation.AdvanceWeek(world);
        Assert.Equal(-Balance.Load().WeeklyOperations - 100000, world.Journal.Where(j => j.Week == 105 && j.Account == WorldFactory.Account(world.OwnedClubId) && j.Kind == CashKind.Operations).Sum(j => j.Amount));
    }

    [Fact]
    public void SchemaNineMigrationKeepsHospitalityAndDevelopmentButAddsNoTrainingExposure()
    {
        var world = SeasonTests.EndFirstSeason();
        world.Projects.Add(new(new(1), world.OwnedClubId, 0, 28, 65000000, 26000000, "legacy"));
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 9; legacy["SimulationVersion"] = "development-9";
        foreach (var club in legacy["Clubs"]!.AsArray())
        {
            club!.AsObject().Remove("TrainingLevel");
            foreach (var player in club["Players"]!.AsArray()) player!.AsObject().Remove("TrainingExposure");
        }
        foreach (var project in legacy["Projects"]!.AsArray()) project!.AsObject().Remove("Kind");
        foreach (var summary in legacy["SeasonSummaries"]!.AsArray())
            foreach (var row in summary!["Development"]!.AsArray()) row!.AsObject().Remove("TrainingBonus");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var original = bytes.ToArray();
        var restored = WorldCodec.Decode(bytes);
        Assert.Equal(original, bytes); Assert.Equal(10, restored.SchemaVersion);
        Assert.All(restored.Clubs, c => Assert.Equal(0, c.TrainingLevel));
        Assert.All(restored.Clubs.SelectMany(c => c.Players), p => Assert.Equal(0, p.TrainingExposure));
        Assert.Equal(FacilityKind.Hospitality, restored.Projects.Single().Kind);
        Assert.Equal(world.SeasonSummaries.Single().Development.ToArray(), restored.SeasonSummaries.Single().Development.ToArray());
    }

    [Fact]
    public void InvalidTrainingStateIsRejected()
    {
        var world = WorldFactory.Create(1); world.OwnedClub.TrainingLevel = 4;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        world.OwnedClub.TrainingLevel = 0;
        world.OwnedClub.Players[0] = world.OwnedClub.Players[0] with { TrainingExposure = 1 };
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
    }
}
