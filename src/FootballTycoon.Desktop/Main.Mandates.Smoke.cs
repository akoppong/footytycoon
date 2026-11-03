using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Godot;

namespace FootballTycoon.Desktop;

public partial class Main
{
    // Opt-in UI regression probe. Only its isolated vault is writable; the registered source is read-only.
    private async Task MandateSmoke(string output, Func<string, Task> capture)
    {
        var sourcePath = OS.GetEnvironment("FT_PLAYABLE_START_CHECKPOINT");
        var sourceBytes = System.IO.File.ReadAllBytes(sourcePath);
        var sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(sourceBytes));
        if (sourceHash != "DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D")
            throw new InvalidOperationException("Expected the registered seed-2026 E01B checkpoint.");
        var source = WorldCodec.Decode(sourceBytes);
        if (source.Week != 52 || source.Season != 2 || source.Status != CareerStatus.Active)
            throw new InvalidOperationException("Expected the ordinary season-two start.");
        var migratedBytes = WorldCodec.Encode(source);
        var probeDirectory = System.IO.Path.Combine(output, "mandate-vault");
        if (System.IO.Directory.Exists(probeDirectory)) throw new InvalidOperationException("Use a fresh mandate smoke output directory.");
        var injectFailure = false;
        var writes = 0;
        using var writeEntered = new ManualResetEventSlim();
        using var releaseWrite = new ManualResetEventSlim();
        var vault = new LocalSaveVault(probeDirectory, stage =>
        {
            if (!injectFailure || stage != "before-rename") return;
            var count = Interlocked.Increment(ref writes);
            if (count == 1)
            {
                writeEntered.Set();
                if (!releaseWrite.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Busy-state smoke assertion did not release the recovery write.");
            }
            if (count == 2) throw new System.IO.IOException("Injected mandate candidate write failure");
        });
        var original = vault.Write(sourceBytes, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "E01B original source");
        await session.DisposeAsync();
        session = new GameSession(vault);
        saveDirectory = probeDirectory;
        view = await session.QueryAsync(); selected = null; ClearMandateEditor(); workspace = "Owner Desk"; auxiliary = ""; Render();
        await MandateSmokeActivate("Load / recover");
        await MandateSmokeLoad(original.SnapshotId);
        RequireBytes(migratedBytes, await session.ExportCheckpointAsync(), "Desktop migration changed the registered start.");

        async Task CaptureScales(string name, string? heading = null)
        {
            foreach (var scale in new[] { 100, 125, 150 })
            {
                textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; Render(); await Settled();
                if (content.GetParent() is ScrollContainer list)
                {
                    list.ScrollVertical = 0;
                    if (heading is not null)
                    {
                        var label = MandateSmokeDescendants(content).OfType<Label>().First(l => l.Text == heading);
                        list.ScrollVertical += (int)(label.GlobalPosition.Y - list.GlobalPosition.Y);
                    }
                }
                await capture(name);
            }
            textScale = 100; Theme.DefaultFontSize = 14; Render(); await Settled();
        }
        async Task DraftDevelopmentValue()
        {
            await MandateSmokeActivate("Change staff mandates");
            await MandateSmokeActivate("Manager: Development");
            await MandateSmokeActivate("Director: Lower-cost value");
        }
        async Task AssertTimeHeld()
        {
            var before = await session.ExportCheckpointAsync();
            var held = controls.Single(b => GodotObject.IsInstanceValid(b) && b.IsInsideTree() && !b.IsQueuedForDeletion() && b.Text == "Time is held");
            if (!held.Disabled) throw new InvalidOperationException("Mandate editor left the advance control enabled.");
            GetViewport().GuiReleaseFocus();
            await MandateSmokeKey(advanceKey); await Settled();
            RequireBytes(before, await session.ExportCheckpointAsync(), "Keyboard advance escaped the mandate hold.");
        }

        await MandateSmokeActivate("Review staff mandates");
        await CaptureScales("Mandate-current");
        await DraftDevelopmentValue(); await AssertTimeHeld();
        await CaptureScales("Mandate-editor");
        await MandateSmokeActivate("Review mandate change"); await AssertTimeHeld();
        await CaptureScales("Mandate-confirmation");
        RequireBytes(migratedBytes, await session.ExportCheckpointAsync(), "Mandate preview mutated the world.");
        await MandateSmokeActivate("Cancel mandate change");
        RequireBytes(migratedBytes, await session.ExportCheckpointAsync(), "Cancelling changed the world.");
        if (mandateDraft is not null || mandateProposal is not null) throw new InvalidOperationException("Cancel retained an unconfirmed mandate.");

        await DraftDevelopmentValue(); await MandateSmokeActivate("Review mandate change");
        var confirmed = mandateProposal ?? throw new InvalidOperationException("No mandate confirmation to test.");
        injectFailure = true;
        await MandateSmokeActivate("Confirm mandate change", async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (!writeEntered.IsSet && DateTime.UtcNow < deadline) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!writeEntered.IsSet || !busy || controls.Any(b => GodotObject.IsInstanceValid(b) && b.IsInsideTree() && !b.IsQueuedForDeletion() && !b.Disabled))
                    throw new InvalidOperationException("Persistence did not hold UI controls busy.");
                await MandateSmokeKey(advanceKey);
                if (view.Week != source.Week || view.Mandates.Current != MandateState.Default)
                    throw new InvalidOperationException("Busy UI advanced or published before durable persistence.");
            }
            finally { releaseWrite.Set(); }
        });
        injectFailure = false;
        RequireBytes(migratedBytes, await session.ExportCheckpointAsync(), "Failed candidate write changed the live world.");
        if (!status.Text.Contains("Injected mandate candidate write failure", StringComparison.Ordinal)
            || mandateProposal?.Id != confirmed.Id || view.Mandates.Current != MandateState.Default)
            throw new InvalidOperationException("Failed save did not retain a truthful retryable confirmation.");
        await capture("Mandate-save-failed");
        await MandateSmokeActivate("Confirm mandate change");
        if (view.Mandates.Current != new MandateState(ManagerMandate.Development, DirectorMandate.LowerCostValue, 1)
            || view.Mandates.History.Length != 1 || view.Week != source.Week || view.ClubCash != source.OwnedClub.Cash)
            throw new InvalidOperationException("Retry did not publish exactly one prospective mandate change.");
        await CaptureScales("Mandate-value-recommendation", "Director's free-agent recommendation");
        await MandateSmokeActivate("Save");
        var changedSave = vault.List().First(s => s.Slot.StartsWith("Manual ·", StringComparison.Ordinal));
        var changedBytes = await session.ExportCheckpointAsync();

        // A second actual command from the common start supplies a different state at the same revision.
        // This is a load/cache regression fixture, not another experimental policy run.
        var other = WorldCodec.Decode(migratedBytes);
        MandateCommands.Commit(other, MandateCommands.Preview(other,
            new("same-revision-control", other.Revision, ManagerMandate.Readiness, DirectorMandate.LowerCostValue)));
        if (other.Revision != view.Revision) throw new InvalidOperationException("Same-revision fixture was not comparable.");
        var otherBytes = WorldCodec.Encode(other);
        var otherSave = vault.Write(otherBytes, original.CareerId, Guid.NewGuid().ToString("N"), original.SnapshotId, "Same-revision control");
        await MandateSmokeActivate("Change staff mandates");
        await MandateSmokeActivate("Manager: Readiness");
        await MandateSmokeActivate("Director: Current recommendation");
        await MandateSmokeActivate("Review mandate change");
        await MandateSmokeActivate("Load / recover"); await MandateSmokeLoad(otherSave.SnapshotId);
        if (mandateDraft is not null || mandateProposal is not null || view.Mandates.Current != other.Mandates
            || view.Mandates.History.Single().CommandId != "same-revision-control")
            throw new InvalidOperationException("Loading a same-revision checkpoint retained stale mandate state.");
        RequireBytes(otherBytes, await session.ExportCheckpointAsync(), "Different checkpoint was not loaded exactly.");
        await MandateSmokeActivate("People"); await CaptureScales("Mandate-same-revision-loaded");
        await MandateSmokeActivate("Load / recover"); await MandateSmokeLoad(changedSave.SnapshotId);
        RequireBytes(changedBytes, await session.ExportCheckpointAsync(), "Saved mandate change did not reload exactly.");

        await MandateSmokeActivate("People");
        await MandateSmokeActivate("Review free-agent approach");
        await MandateSmokeActivate("Review final terms →");
        var approvedTerms = selected!.Recruitment!;
        await MandateSmokeActivate(CommitLabel);
        var originalApproach = view.Mandates.Approaches.Single();
        if (originalApproach.Outcome is not null || originalApproach.Recommendation?.Origin.Director != DirectorMandate.LowerCostValue)
            throw new InvalidOperationException("Approval was displayed as a completed signing or lost its originating mandate.");
        await MandateSmokeActivate("People"); await MandateSmokeActivate("Change staff mandates");
        await MandateSmokeActivate("Director: Current recommendation"); await MandateSmokeActivate("Review mandate change");
        if (mandateProposal!.PendingCommitments.Length != 1
            || !mandateProposal.PendingCommitments[0].Contains(approvedTerms.Player.Name, StringComparison.Ordinal))
            throw new InvalidOperationException("Mandate confirmation omitted the existing approved commitment.");
        await capture("Mandate-pending-commitment");
        await MandateSmokeActivate("Confirm mandate change");
        await MandateSmokeActivate("Owner Desk");
        while (view.Mandates.Selections.IsEmpty && view.Week < source.Week + 10)
            await MandateSmokeActivate("Advance one week");
        if (view.Mandates.Selections.IsEmpty || view.Mandates.Selections.Any(s => s.Origin.Manager != ManagerMandate.Development))
            throw new InvalidOperationException("Ordinary keyboard continuation produced no linked Development selection.");
        var outcome = view.Mandates.Approaches.Single();
        if (outcome.Recommendation != originalApproach.Recommendation || outcome.Outcome is null
            || outcome.Outcome.Origin != originalApproach.Recommendation!.Origin || view.Mandates.Current.Director != DirectorMandate.CurrentRecommendation)
            throw new InvalidOperationException("Pending offer was relabeled under the later mandate.");
        await MandateSmokeActivate("History"); await MandateSmokeActivate("Show mandate evidence");
        await CaptureScales("Mandate-history");
        await CaptureScales("Mandate-season-evidence", $"Season {view.Season} · mandate evidence");
        await MandateSmokeActivate("Save");
        var finalBytes = await session.ExportCheckpointAsync();
        var finalSave = vault.List().First(s => s.Slot.StartsWith("Manual ·", StringComparison.Ordinal));
        await MandateSmokeActivate("Load / recover"); await MandateSmokeLoad(finalSave.SnapshotId);
        RequireBytes(finalBytes, await session.ExportCheckpointAsync(), "Outcome save/load changed evidence.");
        RequireBytes(sourceBytes, System.IO.File.ReadAllBytes(sourcePath), "The registered checkpoint source changed.");
        RequireBytes(sourceBytes, vault.Read(original.SnapshotId).WorldBytes, "The copied original snapshot changed.");
        RequireBytes(changedBytes, vault.Read(changedSave.SnapshotId).WorldBytes, "Continuing overwrote the earlier changed snapshot.");
        System.IO.File.WriteAllText(System.IO.Path.Combine(output, "mandate-result.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            SourceSha256 = sourceHash,
            SourcePreserved = true,
            KeyboardNavigation = "Tab and Enter through Godot input events",
            CandidateWriteFailureRetainedLiveState = true,
            CancelPure = true,
            SameRevisionLoadClearedDraft = true,
            FinalWeek = view.Week,
            view.Mandates.Current,
            view.Mandates.History,
            view.Mandates.Selections,
            view.Mandates.Approaches,
            TextScales = new[] { 100, 125, 150 },
            Display = DisplayServer.GetName()
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        GD.Print("MANDATE SMOKE PASS: ordinary checkpoint, keyboard change/confirm/cancel/advance/history/save/load, failed-write retry, same-revision recovery, preserved origins/source and three text scales.");

        static void RequireBytes(byte[] expected, byte[] actual, string failure)
        { if (!expected.AsSpan().SequenceEqual(actual)) throw new InvalidOperationException(failure); }
    }

    private Task MandateSmokeLoad(string id) => MandateSmokeActivate(b => b.HasMeta("snapshot_id") && b.GetMeta("snapshot_id").AsString() == id, "checkpoint " + id);
    private Task MandateSmokeActivate(string title, Func<Task>? whileBusy = null) => MandateSmokeActivate(b => b.Text == title, title, whileBusy);
    private async Task MandateSmokeActivate(Func<Button, bool> matches, string description, Func<Task>? whileBusy = null)
    {
        await Settled();
        var button = controls.FirstOrDefault(b => GodotObject.IsInstanceValid(b) && b.IsInsideTree() && !b.IsQueuedForDeletion()
            && b.IsVisibleInTree() && !b.Disabled && matches(b)) ?? throw new InvalidOperationException("No enabled keyboard control: " + description);
        // Traverse the engine's real focus order; do not emit a Pressed signal or call the action callback.
        for (var attempt = 0; GetViewport().GuiGetFocusOwner() != button; attempt++)
        {
            if (attempt > controls.Count + 20) throw new InvalidOperationException("Keyboard focus cannot reach: " + description);
            await MandateSmokeKey(Key.Tab);
        }
        await MandateSmokeKey(Key.Enter);
        if (whileBusy is not null) await whileBusy();
        await Settled();
    }
    private async Task MandateSmokeKey(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static IEnumerable<Node> MandateSmokeDescendants(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            yield return child;
            foreach (var descendant in MandateSmokeDescendants(child)) yield return descendant;
        }
    }
}
