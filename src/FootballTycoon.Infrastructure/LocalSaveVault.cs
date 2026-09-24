using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using FootballTycoon.Application;
using FootballTycoon.Core;

namespace FootballTycoon.Infrastructure;

public sealed class LocalSaveVault : ISaveVault
{
    private const int MaxContainer = 32 * 1024 * 1024;
    private const int MaxWorld = 64 * 1024 * 1024;
    private const int MaxHeader = 4096;
    private static readonly byte[] Magic = "FTSAVE01"u8.ToArray();
    private readonly string directory;
    private readonly Action<string>? fault;
    private sealed record Header(int ContainerVersion, CheckpointInfo Info, int UncompressedBytes);

    public LocalSaveVault(string directory, Action<string>? faultInjector = null)
    {
        this.directory = Path.GetFullPath(directory);
        fault = faultInjector;
        Directory.CreateDirectory(this.directory);
    }

    public CheckpointInfo Write(byte[] world, string careerId, string branchId, string? parentId, string slot)
    {
        ValidateId(careerId); ValidateId(branchId);
        if (parentId is not null) ValidateId(parentId);
        if (string.IsNullOrWhiteSpace(slot) || slot.Length > 80) throw new InvalidDataException("Save name must contain 1–80 characters.");
        if (world.Length > MaxWorld) throw new InvalidDataException("Snapshot exceeds the supported size.");
        var state = WorldCodec.Decode(world);
        var info = new CheckpointInfo(Guid.NewGuid().ToString("N"), parentId, branchId, careerId, slot, state.Week, DateTimeOffset.UtcNow);
        var header = JsonSerializer.SerializeToUtf8Bytes(new Header(1, info, world.Length));
        var path = PathFor(info.SnapshotId); var temporary = path + ".tmp";
        try
        {
            using var body = new MemoryStream();
            using (var writer = new BinaryWriter(body, System.Text.Encoding.UTF8, true))
            {
                writer.Write(Magic); writer.Write(header.Length); writer.Write(header);
                using (var gzip = new GZipStream(body, CompressionLevel.Fastest, true)) gzip.Write(world);
            }
            var bytes = body.ToArray();
            if (bytes.Length + 32 > MaxContainer) throw new InvalidDataException("Save container exceeds the supported size.");
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                file.Write(bytes); fault?.Invoke("after-body"); file.Write(SHA256.HashData(bytes)); file.Flush(true);
            }
            fault?.Invoke("after-flush");
            _ = ReadPath(temporary);
            fault?.Invoke("before-rename");
            File.Move(temporary, path); // Same directory, immutable destination: last valid snapshots cannot be overwritten.
            // Discovery scans validated snapshots, so no fragile catalog update or destructive pruning is required.
            return info;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public LoadedCheckpoint Read(string snapshotId) => ReadPath(PathFor(snapshotId));
    public ImmutableArray<CheckpointInfo> List()
    {
        var list = new List<CheckpointInfo>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.ftsave"))
        {
            try { list.Add(ReadPath(path).Info); }
            catch (Exception e) when (e is IOException or InvalidDataException or JsonException or ArgumentException or OverflowException) { /* Invalid snapshots stay on disk for diagnosis. */ }
        }
        return list.OrderByDescending(i => i.CreatedAt).ThenBy(i => i.SnapshotId, StringComparer.Ordinal).ToImmutableArray();
    }

    public CheckpointPage ListPage(int offset, int limit)
    {
        if (offset < 0 || limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(offset));
        var candidates = new List<(string Path, CheckpointInfo Info)>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.ftsave"))
        {
            try
            {
                // Header data only orders candidates; it is never shown until full validation below.
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (file.Length is < 48 or > MaxContainer) continue;
                candidates.Add((path, ReadHeader(file).Info));
            }
            catch (Exception e) when (e is IOException or InvalidDataException or JsonException or ArgumentException or OverflowException) { }
        }
        var ordered = candidates.OrderByDescending(c => c.Info.CreatedAt).ThenBy(c => c.Info.SnapshotId, StringComparer.Ordinal).ToArray();
        var entries = ImmutableArray.CreateBuilder<CheckpointInfo>();
        foreach (var candidate in ordered.Skip(offset).Take(limit))
        {
            try { entries.Add(ReadPath(candidate.Path).Info); }
            catch (Exception e) when (e is IOException or InvalidDataException or JsonException or ArgumentException or OverflowException) { }
        }
        var next = (long)offset + limit;
        return new(entries.ToImmutable(), next < ordered.Length ? (int)next : null);
    }

    private static LoadedCheckpoint ReadPath(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length is < 48 or > MaxContainer) throw new InvalidDataException("Save is truncated or exceeds size limits.");
        var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes);
        var content = bytes.AsSpan(0, bytes.Length - 32);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(content), bytes.AsSpan(bytes.Length - 32)))
            throw new InvalidDataException("Save checksum failed. Choose an earlier snapshot; this file was preserved.");
        using var body = new MemoryStream(bytes, 0, bytes.Length - 32, false);
        var header = ReadHeader(body);
        using var gzip = new GZipStream(body, CompressionMode.Decompress);
        var world = new byte[header.UncompressedBytes]; gzip.ReadExactly(world);
        if (gzip.ReadByte() != -1) throw new InvalidDataException("Decompressed world exceeds its declared size.");
        var decoded = WorldCodec.Decode(world);
        if (decoded.Week != header.Info.Week) throw new InvalidDataException("Snapshot date disagrees with the world.");
        return new(header.Info, world);
    }

    private static Header ReadHeader(Stream body)
    {
        using var reader = new BinaryReader(body, System.Text.Encoding.UTF8, leaveOpen: true);
        if (!reader.ReadBytes(8).SequenceEqual(Magic)) throw new InvalidDataException("Unrecognized save container.");
        var length = reader.ReadInt32();
        if (length is < 1 or > MaxHeader || length > body.Length - body.Position) throw new InvalidDataException("Invalid save metadata length.");
        var header = JsonSerializer.Deserialize<Header>(reader.ReadBytes(length)) ?? throw new InvalidDataException("Missing metadata.");
        if (header.ContainerVersion != 1) throw new InvalidDataException("Unsupported newer save container. Source file preserved.");
        if (header.UncompressedBytes is < 1 or > MaxWorld) throw new InvalidDataException("Invalid world size.");
        if (header.Info is null) throw new InvalidDataException("Missing checkpoint identity.");
        ValidateId(header.Info.SnapshotId); ValidateId(header.Info.CareerId); ValidateId(header.Info.BranchId);
        if (header.Info.ParentId is not null) ValidateId(header.Info.ParentId);
        return header;
    }

    private string PathFor(string id) { ValidateId(id); return Path.Combine(directory, id + ".ftsave"); }
    private static void ValidateId(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("Invalid save identity.");
    }
}

public sealed class OfflinePlatformAdapter : IPlatformAdapter
{
    public Task<SyncStatus> SynchronizeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SyncStatus(false, "Saved locally. Steam Cloud is not connected in this prototype."));
}
