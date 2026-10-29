using System.Text.Json;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class MandateCommandTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    private static World Acquired()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire);
        return world;
    }
    private static ChangeMandates Change(World world, string id = "mandates") => new(id, world.Revision,
        ManagerMandate.Development, DirectorMandate.LowerCostValue);
    private static JsonObject WithoutChange(World world, string id)
    {
        var json = JsonSerializer.SerializeToNode(world)!.AsObject();
        foreach (var key in new[] { "Revision", "Mandates", "MandateHistory" }) json.Remove(key);
        json["Commands"]!.AsObject().Remove(id);
        return json;
    }
    private CheckpointInfo Save(LocalSaveVault vault, World world) => vault.Write(WorldCodec.Encode(world),
        Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "source");

    [Fact]
    public void PureOptionalPreviewAndJointCommitPreserveEveryUnrelatedFieldAndRequiredCapitalDecision()
    {
        var world = Acquired(); var before = WorldCodec.Encode(world);
        var proposal = MandateCommands.Preview(world, Change(world));
        Assert.Empty(proposal.BlockingReasons); Assert.Equal(MandateState.Default, proposal.Before);
        Assert.Contains("no immediate charge", proposal.Effect); Assert.Contains("Paid recruitment is unchanged", proposal.Effect);
        Assert.Equal(before, WorldCodec.Encode(world)); // Abandoning/cancelling a preview has no effect.
        var unrelated = WithoutChange(world, proposal.Command.CommandId);
        var receipt = MandateCommands.Commit(world, proposal);
        Assert.Equal(proposal.Command.ExpectedRevision + 1, receipt.Revision);
        Assert.Equal(proposal.Id, receipt.ProposalId);
        Assert.Equal(new(ManagerMandate.Development, DirectorMandate.LowerCostValue, 1), world.Mandates);
        Assert.Single(world.MandateHistory);
        Assert.Equal(world.Week, world.MandateHistory[0].Week);
        Assert.True(JsonNode.DeepEquals(unrelated, WithoutChange(world, proposal.Command.CommandId)));
        Assert.False(world.AllocationChosen); Assert.Contains(world.Decisions, d => d.Required && !d.Resolved);
        var changed = WorldCodec.Encode(world);
        Assert.Equal(receipt, MandateCommands.Commit(world, proposal));
        Assert.Equal(changed, WorldCodec.Encode(world));
        Assert.Equal(changed, WorldCodec.Encode(WorldCodec.Clone(world)));
        var noOp = MandateCommands.Preview(world, Change(world, "no-op"));
        Assert.Contains(noOp.BlockingReasons, r => r.Contains("already in force"));
        Assert.Throws<InvalidOperationException>(() => MandateCommands.Commit(world, noOp));
        Assert.Equal(changed, WorldCodec.Encode(world));
    }

    [Theory]
    [InlineData("revision")]
    [InlineData("branch")]
    [InlineData("manager")]
    [InlineData("director")]
    [InlineData("id")]
    [InlineData("long-id")]
    [InlineData("fingerprint")]
    [InlineData("before")]
    [InlineData("effect")]
    [InlineData("pending")]
    [InlineData("reasons")]
    public void StaleInvalidAndAlteredConfirmationsFailWithoutMutation(string kind)
    {
        var world = Acquired();
        var command = Change(world);
        command = kind switch
        {
            "revision" => command with { ExpectedRevision = world.Revision - 1 },
            "manager" => command with { Manager = (ManagerMandate)99 },
            "director" => command with { Director = (DirectorMandate)99 },
            "id" => command with { CommandId = " " },
            "long-id" => command with { CommandId = new string('x', 129) },
            _ => command
        };
        var proposal = MandateCommands.Preview(world, command);
        if (kind == "branch") world.Seed++; // A different valid branch at the same revision.
        proposal = kind switch
        {
            "fingerprint" => proposal with { WorldFingerprint = "other" },
            "before" => proposal with { Before = proposal.Before with { Revision = 99 } },
            "effect" => proposal with { Effect = "Guaranteed promotion" },
            "pending" => proposal with { PendingCommitments = ["No existing commitments"] },
            "reasons" => proposal with { BlockingReasons = ["altered"] },
            _ => proposal
        };
        var before = WorldCodec.Encode(world);
        Assert.Throws<InvalidOperationException>(() => MandateCommands.Commit(world, proposal));
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Fact]
    public void InvalidCurrentStateFailsBeforeAddingAChange()
    {
        var world = Acquired(); var proposal = MandateCommands.Preview(world, Change(world));
        world.Mandates = world.Mandates with { Manager = (ManagerMandate)99 };
        var before = WorldCodec.Encode(world);
        Assert.Throws<InvalidDataException>(() => MandateCommands.Commit(world, proposal));
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Theory]
    [InlineData(CareerStatus.Acquisition)]
    [InlineData(CareerStatus.Administration)]
    [InlineData(CareerStatus.LostControl)]
    [InlineData(CareerStatus.PrototypeComplete)]
    public void IneligibleCareerStatesDoNotPermitChanges(CareerStatus status)
    {
        var world = status == CareerStatus.Acquisition ? WorldFactory.Create(2026) : Acquired();
        world.Status = status;
        var before = WorldCodec.Encode(world);
        var proposal = MandateCommands.Preview(world, Change(world));
        Assert.NotEmpty(proposal.BlockingReasons);
        Assert.Throws<InvalidOperationException>(() => MandateCommands.Commit(world, proposal));
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Fact]
    public void NonterminalReviewChangesPersistAcrossOrdinaryRolloverWithoutResolvingIt()
    {
        var world = SeasonTests.EndFirstSeason();
        var original = WithoutChange(world, "mandates");
        var proposal = MandateCommands.Preview(world, Change(world));
        Assert.Empty(proposal.BlockingReasons); MandateCommands.Commit(world, proposal);
        Assert.Equal(CareerStatus.SeasonReview, world.Status);
        Assert.True(JsonNode.DeepEquals(original, WithoutChange(world, "mandates")));
        world = WorldCodec.Clone(world);
        SimulationTests.Commit(world, Allocation.StartNextSeason, "renew");
        Assert.Equal(new(ManagerMandate.Development, DirectorMandate.LowerCostValue, 1), world.Mandates);
        Assert.Single(world.MandateHistory); Assert.False(world.AllocationChosen);
        // Build the final season through ordinary renewals so its identities and schedule remain valid.
        for (var season = world.Season; season <= Seasons.PlayableSeasons; season++)
        {
            SimulationTests.Commit(world, Allocation.PreserveReserve, $"plan-{season}");
            while (world.Week < Seasons.EndWeek(world)) Simulation.AdvanceWeek(world);
            if (season < Seasons.PlayableSeasons) SimulationTests.Commit(world, Allocation.StartNextSeason, $"renew-{season + 1}");
        }
        Assert.Equal(CareerStatus.PrototypeComplete, world.Status);
        // Even a final-season review state cannot unlock an extra playable season.
        world.Status = CareerStatus.SeasonReview;
        var final = MandateCommands.Preview(world, new("terminal", world.Revision, ManagerMandate.Readiness, DirectorMandate.CurrentRecommendation));
        Assert.Contains(final.BlockingReasons, r => r.Contains("nonterminal"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingApprovedOffersKeepAllTermsHistoryAndAcceptanceStreams(bool freeAgent)
    {
        var world = freeAgent ? FreeAgentTests.Available() : Acquired();
        SimulationTests.Commit(world, freeAgent ? Allocation.FreeAgentRecruitment : Allocation.Recruitment, "approved");
        var command = Change(world) with { Manager = ManagerMandate.Readiness };
        var preview = MandateCommands.Preview(world, command);
        Assert.Empty(preview.BlockingReasons); Assert.Single(preview.PendingCommitments);
        Assert.Contains("acceptance attempt remain in force", preview.PendingCommitments[0]);
        var before = WithoutChange(world, command.CommandId);
        var control = WorldCodec.Clone(world);
        MandateCommands.Commit(world, preview);
        Assert.True(JsonNode.DeepEquals(before, WithoutChange(world, command.CommandId)));
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(control);
        Assert.Equal(control.RandomStates.OrderBy(p => p.Key), world.RandomStates.OrderBy(p => p.Key));
        Assert.Equal(control.FreeAgentBids, world.FreeAgentBids); Assert.Equal(control.Negotiations, world.Negotiations);
        Assert.Equal(JsonSerializer.Serialize(control.History), JsonSerializer.Serialize(world.History)); Assert.Equal(control.Obligations, world.Obligations);
        Assert.Equal(control.OwnedClub.Players, world.OwnedClub.Players);
        Assert.Equal(control.OwnedClub.Cash, world.OwnedClub.Cash);
    }

    [Fact]
    public void CoreReceiptNamespacePreventsCrossFamilySecondEffectsAndPreservesOriginalReceiptMeaning()
    {
        var world = Acquired(); var investment = world.Commands["Acquire"];
        var proposal = MandateCommands.Preview(world, Change(world, "Acquire"));
        var before = WorldCodec.Encode(world);
        Assert.Equal(investment, MandateCommands.Commit(world, proposal));
        Assert.NotEqual(proposal.Id, investment.ProposalId); Assert.Equal(before, WorldCodec.Encode(world));
        proposal = MandateCommands.Preview(world, Change(world));
        var mandate = MandateCommands.Commit(world, proposal);
        var capital = Proposals.Preview(world, new(Allocation.PreserveReserve), world.Revision);
        before = WorldCodec.Encode(world);
        Assert.Equal(mandate, Proposals.Commit(world, "mandates", world.Revision, capital));
        Assert.NotEqual(capital.Id, mandate.ProposalId); Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FailedRecoveryOrCandidateWritePreservesSourceAndLiveWorldThenRetryPublishesOnce(int failingWrite)
    {
        var vault = new LocalSaveVault(directory); var source = Save(vault, Acquired());
        var path = Path.Combine(directory, source.SnapshotId + ".ftsave"); var file = File.ReadAllBytes(path);
        var writes = 0; var fail = true;
        var fault = new LocalSaveVault(directory, stage =>
        { if (stage == "before-rename" && ++writes == failingWrite && fail) throw new IOException("Injected mandate write failure"); });
        await using var session = new GameSession(fault);
        await session.LoadAsync(source.SnapshotId);
        var before = await session.ExportCheckpointAsync();
        var view = await session.QueryAsync();
        var preview = await session.PreviewMandatesAsync(new("mandates", view.Revision, ManagerMandate.Development, DirectorMandate.LowerCostValue));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        await Assert.ThrowsAsync<IOException>(() => session.CommitMandatesAsync(preview));
        Assert.Equal(before, await session.ExportCheckpointAsync()); Assert.Equal(file, File.ReadAllBytes(path));
        fail = false;
        var receipts = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => session.CommitMandatesAsync(preview)));
        Assert.Single(receipts.Distinct());
        var changed = await session.ExportCheckpointAsync();
        Assert.Single(WorldCodec.Decode(changed).MandateHistory);
        var commit = vault.List().Single(s => s.Slot == "commit");
        Assert.NotEqual(source.BranchId, commit.BranchId); Assert.Equal(source.CareerId, commit.CareerId);
        Assert.Equal(changed, vault.Read(commit.SnapshotId).WorldBytes);
        var count = vault.List().Length;
        await session.LoadAsync(commit.SnapshotId);
        Assert.Equal(receipts[0], await session.CommitMandatesAsync(preview));
        Assert.Equal(changed, await session.ExportCheckpointAsync()); Assert.Equal(count, vault.List().Length);
        Assert.Equal(file, File.ReadAllBytes(path));
    }

    [Fact]
    public async Task SessionUsesSharedReceiptsAndRejectsSameRevisionPreviewAfterDifferentLoad()
    {
        var vault = new LocalSaveVault(directory); var world = Acquired(); var source = Save(vault, world);
        world.Seed++; var other = Save(vault, world);
        await using var session = new GameSession(vault); await session.LoadAsync(source.SnapshotId);
        var preview = await session.PreviewMandatesAsync(Change(world));
        await session.LoadAsync(other.SnapshotId); var before = await session.ExportCheckpointAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.CommitMandatesAsync(preview));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        var oldReceipt = world.Commands["Acquire"];
        var reused = await session.PreviewMandatesAsync(Change(world, "Acquire"));
        Assert.Equal(oldReceipt, await session.CommitMandatesAsync(reused));
        Assert.Equal(before, await session.ExportCheckpointAsync());
        preview = await session.PreviewMandatesAsync(Change(world));
        var receipt = await session.CommitMandatesAsync(preview);
        var view = await session.QueryAsync();
        var capital = await session.PreviewAsync(new(Allocation.PreserveReserve), view.Revision);
        var changed = await session.ExportCheckpointAsync();
        Assert.Equal(receipt, await session.CommitAsync("mandates", view.Revision, capital));
        Assert.Equal(changed, await session.ExportCheckpointAsync());
    }
}
