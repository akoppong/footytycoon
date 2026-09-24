using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class PersistenceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    private static readonly string Career = Guid.NewGuid().ToString("N");
    private static readonly string Branch = Guid.NewGuid().ToString("N");
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [Fact]
    public void PagedDiscoveryValidatesDisplayedFilesAndRetainsOlderCheckpoints()
    {
        var vault = new LocalSaveVault(directory); var bytes = WorldCodec.Encode(WorldFactory.Create(9));
        for (var i = 0; i < 25; i++) vault.Write(bytes, Career, Branch, null, $"checkpoint-{i}");
        var expected = vault.List();
        var first = vault.ListPage(0, 20);
        Assert.Equal(expected.Take(20), first.Entries); Assert.Equal(20, first.NextOffset);
        var last = vault.ListPage(first.NextOffset!.Value, 20);
        Assert.Equal(expected.Skip(20), last.Entries); Assert.Null(last.NextOffset);
        // Changing a body after enumeration must never bypass checksum validation on the next page read.
        var path = Path.Combine(directory, expected[0].SnapshotId + ".ftsave");
        var corrupt = File.ReadAllBytes(path); corrupt[^40] ^= 0xff; File.WriteAllBytes(path, corrupt);
        var refreshed = vault.ListPage(0, 20);
        Assert.Equal(19, refreshed.Entries.Length); Assert.Equal(20, refreshed.NextOffset);
        Assert.DoesNotContain(refreshed.Entries, i => i.SnapshotId == expected[0].SnapshotId);
        Assert.Equal(last.Entries.ToArray(), vault.ListPage(20, 20).Entries.ToArray());
        Assert.True(File.Exists(path));
        Assert.Throws<ArgumentOutOfRangeException>(() => vault.ListPage(-1, 20));
    }

    [Fact]
    public async Task RolloverIsDurableAndFailedWriteKeepsTheSeasonReview()
    {
        var vault = new LocalSaveVault(directory);
        var saved = vault.Write(WorldCodec.Encode(SeasonTests.EndFirstSeason()), Career, Branch, null, "year-end");
        var fail = true;
        var writes = 0;
        var fault = new LocalSaveVault(directory, stage => { if (stage == "before-rename" && ++writes == 2 && fail) throw new IOException("Rollover write failed"); });
        await using var session = new GameSession(fault);
        await session.LoadAsync(saved.SnapshotId);
        var before = await session.ExportCheckpointAsync();
        var view = await session.QueryAsync();
        var renewal = await session.PreviewAsync(new(Allocation.StartNextSeason), view.Revision);
        await Assert.ThrowsAsync<IOException>(() => session.CommitAsync("renew", view.Revision, renewal));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        fail = false;
        await session.CommitAsync("renew", view.Revision, renewal);
        var next = await session.QueryAsync();
        Assert.Equal(2, next.Season); Assert.Equal(52, next.Week); Assert.Equal(0, next.SeasonWeek); Assert.Empty(next.Results);
        Assert.All(next.Fixtures, f => Assert.InRange(f.Week, 53, 104));
        var checkpoint = await session.SaveAsync();
        var plan = await session.PreviewAsync(new(Allocation.PreserveReserve), next.Revision);
        await session.CommitAsync("year-two-plan", next.Revision, plan);
        await session.AdvanceAsync(AdvanceTarget.Month);
        var advanced = await session.QueryAsync();
        Assert.Equal(56, advanced.Week); Assert.Equal(4, advanced.SeasonWeek);
        await session.LoadAsync(checkpoint.SnapshotId);
        Assert.Equal(52, (await session.QueryAsync()).Week);
        Assert.Equal(0, (await session.QueryAsync()).SeasonWeek);
        Assert.False((await session.QueryAsync()).AllocationChosen);
        Assert.Equal(before, vault.Read(saved.SnapshotId).WorldBytes);
    }

    [Fact]
    public async Task SeasonLineFixturesComeFromTheSavedWorldAndSurviveReload()
    {
        await using var session = new GameSession(new LocalSaveVault(directory));
        var initial = await session.QueryAsync();
        var world = WorldCodec.Decode(await session.ExportCheckpointAsync());
        var expected = world.Fixtures.Where(f => f.Home == world.OwnedClubId || f.Away == world.OwnedClubId).ToArray();
        Assert.Equal(30, initial.Fixtures.Count(f => f.Competition == Competition.League));
        Assert.Equal(expected, initial.Fixtures.ToArray());
        var proposal = await session.PreviewAsync(new(Allocation.Acquire), initial.Revision);
        await session.CommitAsync("acquire", initial.Revision, proposal);
        var acquired = await session.QueryAsync();
        proposal = await session.PreviewAsync(new(Allocation.PreserveReserve), acquired.Revision);
        await session.CommitAsync("retain", acquired.Revision, proposal);
        await session.AdvanceAsync(AdvanceTarget.Month);
        var advanced = await session.QueryAsync();
        Assert.Equal(0, initial.Week); // Published views are snapshots, not mutable world aliases.
        Assert.All(expected, fixture => Assert.Contains(fixture, advanced.Fixtures));
        Assert.All(advanced.Results, match => Assert.Contains(advanced.Fixtures, fixture => fixture.Id == match.FixtureId));
        var checkpoint = await session.SaveAsync("season-line");
        var expectedAdvanced = advanced.Fixtures;
        await session.AdvanceAsync(AdvanceTarget.Week);
        await session.LoadAsync(checkpoint.SnapshotId);
        var restored = await session.QueryAsync();
        Assert.Equal(advanced.Week, restored.Week);
        Assert.Equal(expectedAdvanced.ToArray(), restored.Fixtures.ToArray());
    }

    [Fact]
    public void CorruptionIsRejectedAndValidSnapshotsRemainDiscoverable()
    {
        var vault = new LocalSaveVault(directory); var data = WorldCodec.Encode(WorldFactory.Create(1));
        var valid = vault.Write(data, Career, Branch, null, "manual");
        var bad = vault.Write(data, Career, Branch, valid.SnapshotId, "auto-0");
        var path = Path.Combine(directory, bad.SnapshotId + ".ftsave");
        var bytes = File.ReadAllBytes(path); bytes[bytes.Length / 2] ^= 0xff; File.WriteAllBytes(path, bytes);
        Assert.Throws<InvalidDataException>(() => vault.Read(bad.SnapshotId));
        Assert.Equal(data, vault.Read(valid.SnapshotId).WorldBytes);
        Assert.Single(vault.List()); Assert.True(File.Exists(path));
        Assert.Throws<InvalidDataException>(() => vault.Read("../outside"));
    }

    [Fact]
    public void TruncatedFileIsRejectedWithoutLosingPreviousSnapshot()
    {
        var vault = new LocalSaveVault(directory); var bytes = WorldCodec.Encode(WorldFactory.Create(3));
        var previous = vault.Write(bytes, Career, Branch, null, "manual");
        var broken = vault.Write(bytes, Career, Branch, previous.SnapshotId, "auto-1");
        var path = Path.Combine(directory, broken.SnapshotId + ".ftsave");
        using (var file = new FileStream(path, FileMode.Open, FileAccess.Write)) file.SetLength(12);
        Assert.Throws<InvalidDataException>(() => vault.Read(broken.SnapshotId));
        Assert.Single(vault.List());
        Assert.Equal(bytes, vault.Read(previous.SnapshotId).WorldBytes);
    }

    [Theory]
    [InlineData("after-body")]
    [InlineData("after-flush")]
    [InlineData("before-rename")]
    public void InterruptedWritePreservesLastGoodSnapshot(string stage)
    {
        var vault = new LocalSaveVault(directory); var bytes = WorldCodec.Encode(WorldFactory.Create(2));
        var saved = vault.Write(bytes, Career, Branch, null, "manual");
        var failing = new LocalSaveVault(directory, point => { if (point == stage) throw new IOException("Injected full disk/interruption"); });
        Assert.Throws<IOException>(() => failing.Write(bytes, Career, Branch, saved.SnapshotId, "manual"));
        Assert.Equal(bytes, vault.Read(saved.SnapshotId).WorldBytes);
        Assert.Single(vault.List()); Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public async Task FailedCommitPersistenceDoesNotPublishCandidate()
    {
        var writes = 0;
        var vault = new LocalSaveVault(directory, stage => { if (stage == "before-rename" && ++writes == 2) throw new IOException("Post-commit checkpoint failed"); });
        await using var session = new GameSession(vault);
        var before = await session.ExportCheckpointAsync();
        var proposal = await session.PreviewAsync(new(Allocation.Acquire), 0);
        await Assert.ThrowsAsync<IOException>(() => session.CommitAsync("acquire", 0, proposal));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        Assert.Single(vault.List()); Assert.Equal("recovery", vault.List()[0].Slot);
        await session.CommitAsync("acquire", 0, proposal);
        Assert.Equal(CareerStatus.Active, (await session.QueryAsync()).Status);
    }

    [Fact]
    public async Task LoadingCreatesANewBranchAndRetainsCommandReceipts()
    {
        await using var session = new GameSession(new LocalSaveVault(directory));
        var proposal = await session.PreviewAsync(new(Allocation.Acquire), 0);
        var result = await session.CommitAsync("purchase", 0, proposal);
        var original = await session.SaveAsync("manual");
        await session.LoadAsync(original.SnapshotId);
        Assert.Equal(result, await session.CommitAsync("purchase", 0, proposal));
        var branched = await session.SaveAsync("continued");
        Assert.Equal(original.SnapshotId, branched.ParentId);
        Assert.NotEqual(original.BranchId, branched.BranchId);
        Assert.Equal(original.CareerId, branched.CareerId);
    }

    [Fact]
    public async Task ConcurrentDuplicateCommandsHaveOneEffect()
    {
        await using var session = new GameSession(new LocalSaveVault(directory));
        var proposal = await session.PreviewAsync(new(Allocation.Acquire), 0);
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => session.CommitAsync("purchase", 0, proposal)));
        Assert.Single(results.Distinct()); Assert.Equal(220000000, (await session.QueryAsync()).PersonalReserve);
    }

    [Fact]
    public async Task FailedWeeklyAutosaveRetainsLastDurableWeek()
    {
        var fail = false;
        var vault = new LocalSaveVault(directory, stage => { if (fail && stage == "before-rename") throw new IOException("Disk full"); });
        await using var session = new GameSession(vault);
        foreach (var allocation in new[] { Allocation.Acquire, Allocation.PreserveReserve })
        {
            var view = await session.QueryAsync();
            await session.CommitAsync(allocation.ToString(), view.Revision, await session.PreviewAsync(new(allocation), view.Revision));
        }
        await session.AdvanceAsync(AdvanceTarget.Week);
        var before = await session.ExportCheckpointAsync(); fail = true;
        await Assert.ThrowsAsync<IOException>(() => session.AdvanceAsync(AdvanceTarget.Month));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        Assert.Equal(1, (await session.QueryAsync()).Week);
        fail = false; await session.AdvanceAsync(AdvanceTarget.Week);
        Assert.Equal(2, (await session.QueryAsync()).Week);
    }
}
