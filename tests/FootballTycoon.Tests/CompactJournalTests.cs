using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class CompactJournalTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [Fact]
    public void TupleRowsPreserveIntegerExtremesUnicodeEnumsAndOrder()
    {
        var world = WorldFactory.Create(2026);
        // Serializer bounds, independent of club-account arithmetic: seller is not a club cash balance.
        world.Journal = Enum.GetValues<CashKind>().Select((kind, i) => new LedgerEntry(i + 1, 0, "seller",
            i % 2 == 0 ? long.MinValue : long.MaxValue, kind, "Zoë ⚽ \"quoted\"\n\\history")).ToList();
        var bytes = WorldCodec.Encode(world);
        var before = bytes.ToArray();
        using var json = JsonDocument.Parse(bytes);
        var rows = json.RootElement.GetProperty("Journal");
        Assert.All(rows.EnumerateArray(), row => Assert.Equal(6, row.GetArrayLength()));
        Assert.Equal(long.MinValue, rows[0][3].GetInt64());
        Assert.Equal(long.MaxValue, rows[1][3].GetInt64());
        var loaded = WorldCodec.Decode(bytes);
        Assert.Equal(world.Journal, loaded.Journal);
        Assert.Equal(before, bytes); Assert.Equal(bytes, WorldCodec.Encode(loaded));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyMigrationPreservesAllLogicalFieldsAndRejectsStaleQuotes(bool acquired)
    {
        var world = WorldFactory.Create(2026);
        if (acquired)
        {
            SimulationTests.Commit(world, Allocation.Acquire);
            SimulationTests.Commit(world, Allocation.PreserveReserve);
            for (var week = 0; week < 4; week++) Simulation.AdvanceWeek(world);
        }
        world.SchemaVersion = 22; world.SimulationVersion = "acquisition-counteroffer-22";
        var stale = Proposals.Preview(world, new(acquired ? Allocation.InjectCapital : Allocation.Acquire, acquired ? 100 : 0), world.Revision);
        var bytes = WorldCodec.Encode(world); var original = bytes.ToArray();
        using var source = JsonDocument.Parse(bytes);
        Assert.All(source.RootElement.GetProperty("Journal").EnumerateArray(), row => Assert.Equal(JsonValueKind.Object, row.ValueKind));
        var loaded = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!;
        expected["SchemaVersion"] = 24; expected["SimulationVersion"] = "owner-mandates-24";
        Assert.True(JsonNode.DeepEquals(expected, JsonSerializer.SerializeToNode(loaded)));
        Assert.Equal(original, bytes);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(loaded, "stale", loaded.Revision, stale));
        if (acquired)
        {
            var before = WorldCodec.Encode(loaded);
            Assert.Equal(world.Commands["Acquire"], Proposals.Commit(loaded, "Acquire", 0, stale));
            Assert.Equal(before, WorldCodec.Encode(loaded));
        }
        Assert.Equal(WorldCodec.Encode(loaded), WorldCodec.Encode(WorldCodec.Clone(loaded)));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[1,0,\"seller\",0,0]")]
    [InlineData("[1,0,\"seller\",0,0,\"x\",9]")]
    [InlineData("[\"1\",0,\"seller\",0,0,\"x\"]")]
    [InlineData("[2147483648,0,\"seller\",0,0,\"x\"]")]
    [InlineData("[1,0,7,0,0,\"x\"]")]
    [InlineData("[1,0,null,0,0,\"x\"]")]
    [InlineData("[1,0,\"seller\",9223372036854775808,0,\"x\"]")]
    [InlineData("[1,0,\"seller\",0.5,0,\"x\"]")]
    [InlineData("[1,0,\"seller\",\"0\",0,\"x\"]")]
    [InlineData("[1,0,\"seller\",0,{},\"x\"]")]
    [InlineData("[1,0,\"seller\",0,0,null]")]
    [InlineData("{\"Sequence\":1,\"Week\":0,\"Amount\":0,\"Kind\":0}")]
    public void MalformedRowsFailWithControlledJsonErrors(string row)
    {
        Assert.Throws<JsonException>(() => WorldCodec.Decode(WithRow(row)));
    }

    [Fact]
    public void CanonicalEncodingDoesNotPublishNullRowsOrText()
    {
        var world = WorldFactory.Create(2026);
        foreach (var row in new LedgerEntry[] { null!, new(1, 0, null!, 0, CashKind.Purchase, "x"), new(1, 0, "seller", 0, CashKind.Purchase, null!) })
        {
            world.Journal = [row];
            Assert.Throws<JsonException>(() => WorldCodec.Encode(world));
        }
    }

    [Fact]
    public void ReloadedCareerRetainsEveryJournalEntryAndContinuesExactly()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.Hospitality);
        for (var week = 0; week < 26; week++) Simulation.AdvanceWeek(world);
        var bytes = WorldCodec.Encode(world);
        var restored = WorldCodec.Decode(bytes);
        var objectBytes = JsonSerializer.SerializeToUtf8Bytes(world);
        Assert.True(bytes.Length < objectBytes.Length * .65, "The real journal should materially reduce uncompressed checkpoint size.");
        Assert.Equal(objectBytes, JsonSerializer.SerializeToUtf8Bytes(restored));
        for (var week = 0; week < 6; week++) { Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(restored); }
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(restored));
    }

    [Fact]
    public async Task LegacyFileAndReceiptsSurviveMigrationAndFailedCompactSave()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.PreserveReserve);
        for (var week = 0; week < 4; week++) Simulation.AdvanceWeek(world);
        world.SchemaVersion = 22; world.SimulationVersion = "acquisition-counteroffer-22";
        var legacy = WorldCodec.Encode(world);
        var vault = new LocalSaveVault(directory);
        var saved = vault.Write(legacy, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "legacy");
        var path = Path.Combine(directory, saved.SnapshotId + ".ftsave");
        var originalFile = File.ReadAllBytes(path);
        var fail = false;
        var writes = 0;
        var failingVault = new LocalSaveVault(directory, stage => { if (fail && stage == "before-rename" && ++writes == 2) throw new IOException("Injected candidate interruption"); });
        await using var session = new GameSession(failingVault);
        await session.LoadAsync(saved.SnapshotId);
        var before = await session.ExportCheckpointAsync();
        Assert.Equal(world.Journal, WorldCodec.Decode(before).Journal);
        var view = await session.QueryAsync();
        var proposal = await session.PreviewAsync(new(Allocation.InjectCapital, 100), view.Revision);
        Assert.Equal(world.Commands["Acquire"], await session.CommitAsync("Acquire", 0, proposal));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        fail = true;
        await Assert.ThrowsAsync<IOException>(() => session.CommitAsync("inject", view.Revision, proposal));
        Assert.Equal(2, writes); // The recovery save succeeds; the compact candidate fails before publication.
        Assert.Equal(before, await session.ExportCheckpointAsync());
        fail = false;
        await session.CommitAsync("inject", view.Revision, proposal);
        var checkpoint = await session.SaveAsync("compact");
        var compact = await session.ExportCheckpointAsync();
        Assert.Equal(compact, vault.Read(checkpoint.SnapshotId).WorldBytes);
        await session.LoadAsync(checkpoint.SnapshotId);
        Assert.Equal(compact, await session.ExportCheckpointAsync());
        Assert.Equal(originalFile, File.ReadAllBytes(path)); Assert.Equal(legacy, vault.Read(saved.SnapshotId).WorldBytes);
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[1,0,\"seller\",0,0,\"x\",9]")]
    [InlineData("[1,0,\"seller\",9223372036854775808,0,\"x\"]")]
    public void MalformedChecksummedSaveDoesNotBreakDiscoveryOrAlterValidFiles(string row)
    {
        var vault = new LocalSaveVault(directory); var bytes = WorldCodec.Encode(WorldFactory.Create(2026));
        var career = Guid.NewGuid().ToString("N"); var branch = Guid.NewGuid().ToString("N");
        var valid = vault.Write(bytes, career, branch, null, "valid");
        var bad = vault.Write(bytes, career, branch, valid.SnapshotId, "bad");
        var path = Path.Combine(directory, bad.SnapshotId + ".ftsave");
        ReplacePayload(path, WithRow(row));
        var badFile = File.ReadAllBytes(path);
        Assert.Throws<JsonException>(() => vault.Read(bad.SnapshotId));
        Assert.Equal(valid, Assert.Single(vault.List()));
        Assert.Equal(valid, Assert.Single(vault.ListPage(0, 20).Entries));
        Assert.Equal(bytes, vault.Read(valid.SnapshotId).WorldBytes); Assert.Equal(badFile, File.ReadAllBytes(path));
    }

    private static byte[] WithRow(string row)
    {
        var node = JsonSerializer.SerializeToNode(WorldFactory.Create(2026))!;
        node["Journal"] = JsonNode.Parse("[" + row + "]");
        return Encoding.UTF8.GetBytes(node.ToJsonString());
    }

    private static void ReplacePayload(string path, byte[] payload)
    {
        var file = File.ReadAllBytes(path);
        var header = JsonNode.Parse(file.AsSpan(12, BitConverter.ToInt32(file, 8)))!;
        header["UncompressedBytes"] = payload.Length;
        var headerBytes = Encoding.UTF8.GetBytes(header.ToJsonString());
        using var body = new MemoryStream();
        using (var writer = new BinaryWriter(body, Encoding.UTF8, true))
        {
            writer.Write(file.AsSpan(0, 8)); writer.Write(headerBytes.Length); writer.Write(headerBytes);
            using var gzip = new GZipStream(body, CompressionLevel.Fastest, true); gzip.Write(payload);
        }
        var content = body.ToArray();
        File.WriteAllBytes(path, content.Concat(SHA256.HashData(content)).ToArray());
    }
}
