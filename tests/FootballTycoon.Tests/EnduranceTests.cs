extern alias Endurance;
using FootballTycoon.Core;
using Xunit;
using Diagnostic = Endurance::FootballTycoon.Core;
using Probe = Endurance::FootballTycoon.Endurance;

namespace FootballTycoon.Tests;

public class EnduranceTests
{
    [Fact]
    public void DiagnosticBuildKeepsOpeningTermsAndWeeklyGameplayIdentical()
    {
        Assert.Equal(3, Seasons.PlayableSeasons); Assert.Equal(50, Diagnostic.Seasons.PlayableSeasons);
        var production = WorldFactory.Create(2026);
        Assert.Equal(WorldCodec.Encode(production), Diagnostic.WorldCodec.Encode(Diagnostic.WorldFactory.Create(2026)));
        SimulationTests.Commit(production, Allocation.Acquire);
        SimulationTests.Commit(production, Allocation.PreserveReserve);
        var diagnostic = Diagnostic.WorldCodec.Decode(WorldCodec.Encode(production));
        for (var i = 0; i < 52; i++) { Simulation.AdvanceWeek(production); Diagnostic.Simulation.AdvanceWeek(diagnostic); }
        Assert.Equal(WorldCodec.Encode(production), Diagnostic.WorldCodec.Encode(diagnostic));
    }

    [Theory]
    [InlineData(0, Diagnostic.Allocation.PreserveReserve, 1)]
    [InlineData(51, Diagnostic.Allocation.PreserveReserve, 1)]
    [InlineData(1, Diagnostic.Allocation.Acquire, 1)]
    [InlineData(1, Diagnostic.Allocation.PreserveReserve, 0)]
    [InlineData(1, Diagnostic.Allocation.PreserveReserve, 1441)]
    public void InvalidRunOptionsAreRejected(int seasons, Diagnostic.Allocation strategy, int minutes)
    {
        Assert.Throws<ArgumentException>(() => new Probe.RunOptions(seasons, 2026, strategy, TimeSpan.FromMinutes(minutes)).Validate());
    }

    [Fact]
    public void AccountingDetectsMissingAndDuplicatePeopleWithoutFabricatingReplacements()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        var opening = world.Clubs.SelectMany(c => c.Players).Select(p => p.Id).Concat(world.FreeAgents.Select(f => f.Player.Id)).ToHashSet();
        Probe.EnduranceRun.VerifyPeople(world, opening);
        var player = world.OwnedClub.Players[0]; world.OwnedClub.Players.RemoveAt(0);
        Assert.Throws<InvalidDataException>(() => Probe.EnduranceRun.VerifyPeople(world, opening));
        world.Retirements.Add(new(player.Id, player.Name, player.Role, player.Age, player.Ability, world.OwnedClubId, 0));
        Probe.EnduranceRun.VerifyPeople(world, opening);
        var sample = Probe.EnduranceRun.Sample(world, "controlled");
        Assert.Equal(1199, sample.Active); Assert.Equal(1, sample.Retired); Assert.Equal(0, sample.Graduated);
        world.OwnedClub.Players.Add(player);
        Assert.Throws<InvalidDataException>(() => Probe.EnduranceRun.VerifyPeople(world, opening));
    }

    [Fact]
    public void DiagnosticWorldBeyondMilestoneCannotLoadInProduction()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        world.Season = 4; world.Week = 156;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Diagnostic.WorldCodec.Encode(world)));
    }

    [Fact]
    public void TimeLimitReportsPartialEvidenceInsteadOfSuccess()
    {
        var metrics = new StringWriter();
        var result = Probe.EnduranceRun.Execute(new(4, 2026, Diagnostic.Allocation.PreserveReserve, TimeSpan.FromTicks(1)), metrics, new StringWriter());
        Assert.Equal("Incomplete", result.Outcome); Assert.Equal(0, result.Week); Assert.Equal(0, result.CompletedSeasons);
        Assert.Equal(1200, result.MinimumActive); Assert.Contains("partial", result.Detail);
        using var config = System.Text.Json.JsonDocument.Parse(metrics.ToString().Split('\n')[0]);
        Assert.Equal(64, config.RootElement.GetProperty("AssemblySha256").GetString()!.Length);
    }

    [Fact]
    public void RunnerCrossesTheThirdSeasonWithoutResettingTheWorld()
    {
        var metrics = new StringWriter();
        var result = Probe.EnduranceRun.Execute(new(4, 2026, Diagnostic.Allocation.PreserveReserve, TimeSpan.FromMinutes(10)), metrics, new StringWriter());
        Assert.Equal("Completed", result.Outcome); Assert.Equal(4, result.CompletedSeasons); Assert.Equal(208, result.Week);
        Assert.True(result.CheckpointBytes > 0); Assert.Equal(64, result.GameplaySha256!.Length);
        Assert.Equal(0, result.WeeksOutsidePopulationTarget); // This four-year seed remains in range; longer probes still count every breach.
        Assert.Contains("\"Kind\":\"renewal\",\"Season\":4", metrics.ToString());
        Assert.Contains("\"Kind\":\"season-close\",\"Season\":4", metrics.ToString());
    }
}
