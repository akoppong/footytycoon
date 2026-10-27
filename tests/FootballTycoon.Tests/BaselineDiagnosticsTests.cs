using System.Text.Json;
using FootballTycoon.Core;
using FootballTycoon.Headless;
using Xunit;

namespace FootballTycoon.Tests;

public class BaselineDiagnosticsTests
{
    [Fact]
    public void ObservationPreservesExactWorldBytesAndNextWeekReplay()
    {
        var world = WorldFactory.Create(2026);
        BaselineDiagnostics.CommitOpening(world);
        var before = WorldCodec.Encode(world);
        var control = WorldCodec.Decode(before);
        var first = BaselineDiagnostics.ObserveSelection(world.OwnedClub, world.Week + 1);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(BaselineDiagnostics.ObserveSelection(world.OwnedClub, world.Week + 1)));
        Assert.Equal(before, WorldCodec.Encode(world));
        Simulation.AdvanceWeek(world);
        Simulation.AdvanceWeek(control);
        Assert.Equal(WorldCodec.Encode(control), WorldCodec.Encode(world));
    }

    [Fact]
    public void PairsRespectNativeOutfieldRoleAgeAvailabilityAndFivePointBoundary()
    {
        static Player P(int id, Role role, int ability, int age) => new(new(id), $"Player {id}", role, ability, age, 10000, new(id), 104);
        var club = new Club
        {
            Players = [P(1, Role.Goalkeeper, 70, 30), P(2, Role.Goalkeeper, 65, 20),
            P(3, Role.Forward, 70, 30), P(4, Role.Forward, 80, 30), P(5, Role.Forward, 65, 23),
            P(6, Role.Forward, 64, 20), P(7, Role.Forward, 69, 24),
            P(8, Role.Forward, 69, 22) with { InjuredUntilWeek = 6 },
            P(9, Role.Forward, 69, 22) with { SuspendedMatches = 1 },
            P(10, Role.Defender, 75, 30), P(11, Role.Defender, 75, 30), P(12, Role.Defender, 75, 30), P(13, Role.Defender, 75, 30),
            P(14, Role.Midfielder, 75, 30), P(15, Role.Midfielder, 75, 30), P(16, Role.Midfielder, 75, 30), P(17, Role.Midfielder, 75, 30)]
        };
        var pair = Assert.Single(BaselineDiagnostics.ObserveSelection(club, 5).Pairs);
        Assert.Equal(new DevelopmentPair(3, 5, Role.Forward, 5), pair);
        Assert.Contains(BaselineDiagnostics.ObserveSelection(club, 6).Pairs, p => p.YoungId == 8 && p.AbilitySacrifice == 1);
    }

    [Fact]
    public async Task ExistingEvidenceDirectoryIsRejectedWithoutOverwriting()
    {
        var directory = Path.Combine(Path.GetTempPath(), "e01-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var sentinel = Path.Combine(directory, "keep.txt");
            await File.WriteAllTextAsync(sentinel, "existing evidence");
            await Assert.ThrowsAsync<IOException>(() => BaselineDiagnostics.RunAsync(2026, directory));
            Assert.Equal("existing evidence", await File.ReadAllTextAsync(sentinel));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
