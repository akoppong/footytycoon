using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

namespace FootballTycoon.Headless;

// A bounded application-path timing sample, separate from the registered policy comparisons.
public static class MandateTimingDiagnostics
{
    public static async Task RunAsync(string checkpointPath, string outputDirectory)
    {
        if (Directory.Exists(outputDirectory) || File.Exists(outputDirectory))
            throw new IOException("Choose a new timing output directory.");
        var source = File.ReadAllBytes(checkpointPath);
        var sourceHash = PlayableStartDiagnostics.Hash(source);
        if (sourceHash != "DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D")
            throw new InvalidDataException("Timing requires the registered original E01B primary checkpoint.");
        Directory.CreateDirectory(outputDirectory);
        var samples = new List<TimingSample>();
        var vault = new TimedVault(new LocalSaveVault(Path.Combine(outputDirectory, "vault")), samples);
        var imported = vault.Write(source, "e0800000000000000000000000000001", "e0800000000000000000000000000002", null, "import");
        await using var session = new GameSession(vault);
        await MeasureAsync("LoadAsync", () => session.LoadAsync(imported.SnapshotId));
        var core = WorldCodec.Decode(source);
        await AssertParity();
        for (var i = 0; i < 6; i++) await MeasureAsync("QueryAsync", async () => { _ = await session.QueryAsync(); });
        var command = new ChangeMandates("e08-timing-mandates", core.Revision, ManagerMandate.Development, DirectorMandate.LowerCostValue);
        MandateProposal? mandate = null;
        await MeasureAsync("PreviewMandatesAsync", async () => { mandate = await session.PreviewMandatesAsync(command); });
        if (mandate is null || !mandate.BlockingReasons.IsEmpty) throw new InvalidDataException("Timing mandate preview blocked.");
        await AssertParity();
        MandateCommands.Commit(core, MandateCommands.Preview(core, command));
        await MeasureAsync("CommitMandatesAsync", async () => { _ = await session.CommitMandatesAsync(mandate); });
        await AssertParity();
        var ownerCommand = new OwnerCommand(Allocation.FreeAgentRecruitment) { ReserveException = false };
        Proposal? proposal = null;
        for (var i = 0; i < 6; i++)
            await MeasureAsync("PreviewAsync.FreeAgent", async () => { proposal = await session.PreviewAsync(ownerCommand, core.Revision); });
        await AssertParity();
        if (proposal is null || !proposal.BlockingReasons.IsEmpty) throw new InvalidDataException("Timing recruitment preview blocked.");
        var revision = core.Revision;
        Proposals.Commit(core, "e08-timing-approach", revision, Proposals.Preview(core, ownerCommand, revision));
        await MeasureAsync("CommitAsync.FreeAgent", async () => { _ = await session.CommitAsync("e08-timing-approach", revision, proposal); });
        await AssertParity();
        for (var i = 0; i < 6; i++)
        {
            var week = core.Week;
            var timer = Stopwatch.StartNew();
            Simulation.AdvanceWeek(core);
            samples.Add(new("Core.AdvanceWeek", week, timer.Elapsed.TotalMilliseconds));
            await MeasureAsync("AdvanceAsync.Week", async () => { _ = await session.AdvanceAsync(AdvanceTarget.Week); });
            if (core.Week != week + 1) throw new InvalidDataException("Timing advance stopped before completing a week.");
            await AssertParity();
            await MeasureAsync("QueryAsync.AfterAdvance", async () => { _ = await session.QueryAsync(); });
        }
        await MeasureAsync("SaveAsync.Manual", async () => { _ = await session.SaveAsync("timing-manual"); });
        await AssertParity();
        if (!source.AsSpan().SequenceEqual(File.ReadAllBytes(checkpointPath))) throw new InvalidDataException("Source checkpoint changed.");
        File.WriteAllText(Path.Combine(outputDirectory, "timing.json"), JsonSerializer.Serialize(new
        {
            Machine = Environment.MachineName,
            OS = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            SourceSha256 = sourceHash,
            HeadlessBinarySha256 = PlayableStartDiagnostics.Hash(File.ReadAllBytes(typeof(MandateTimingDiagnostics).Assembly.Location)),
            CompletedWeek = core.Week,
            ExactCoreApplicationParity = true,
            SourcePreserved = true,
            Notes = "Sequential local samples, first calls include cold costs; no percentile or release-performance claim. Vault.Write timings include container compression, durable flush and verification but exclude the caller's WorldCodec.Encode. AdvanceAsync includes clone, simulation, encoding, durable autosave and publication. Query/preview samples include worker queue dispatch. Core is timed first each week, so these are not randomized performance comparisons.",
            Samples = samples
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"E08 timing complete at week {core.Week}: {Path.GetFullPath(outputDirectory)}");

        async Task MeasureAsync(string operation, Func<Task> action)
        {
            var timer = Stopwatch.StartNew();
            await action();
            samples.Add(new(operation, null, timer.Elapsed.TotalMilliseconds));
        }
        async Task AssertParity()
        {
            var actual = await session.ExportCheckpointAsync();
            if (!WorldCodec.Encode(core).AsSpan().SequenceEqual(actual))
                throw new InvalidDataException("Timing application state differs from identical direct Core commands.");
        }
    }

    private sealed record TimingSample(string Operation, int? Week, double Milliseconds);

    private sealed class TimedVault(ISaveVault inner, List<TimingSample> samples) : ISaveVault
    {
        public CheckpointInfo Write(byte[] world, string careerId, string branchId, string? parentId, string slot)
        {
            var timer = Stopwatch.StartNew();
            var result = inner.Write(world, careerId, branchId, parentId, slot);
            samples.Add(new($"Vault.Write.{slot}", result.Week, timer.Elapsed.TotalMilliseconds));
            return result;
        }
        public LoadedCheckpoint Read(string snapshotId) => inner.Read(snapshotId);
        public ImmutableArray<CheckpointInfo> List() => inner.List();
        public CheckpointPage ListPage(int offset, int limit) => inner.ListPage(offset, limit);
    }
}
