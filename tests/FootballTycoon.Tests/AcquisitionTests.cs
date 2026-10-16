using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class AcquisitionTests
{
    [Theory]
    [InlineData(99, true)]
    [InlineData(84, false)]
    public void ResponseIsDurableAndPurchaseRequiresSeparateConfirmation(int percent, bool accepted)
    {
        var world = WorldFactory.Create(2026);
        var asking = Balance.Load().PurchasePrice;
        var amount = asking * percent / 100;
        var owner = world.OwnerCash; var cash = world.OwnedClub.Cash;
        var stalePurchase = Proposals.Preview(world, new(Allocation.Acquire), world.Revision);
        var bytes = WorldCodec.Encode(world);
        var quote = Proposals.Preview(world, new(Allocation.Counteroffer, amount), world.Revision);
        Assert.Empty(quote.BlockingReasons); Assert.Equal(0, quote.TotalCommitment);
        Assert.Equal(bytes, WorldCodec.Encode(world));
        var replay = WorldCodec.Clone(world);
        var receipt = Proposals.Commit(world, "offer", world.Revision, quote);
        Proposals.Commit(replay, "offer", replay.Revision, quote);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(replay));
        Assert.Equal(new AcquisitionCounteroffer(amount, accepted), world.AcquisitionOffer);
        Assert.Equal(CareerStatus.Acquisition, world.Status); Assert.Equal(0, world.Week);
        Assert.Equal(owner, world.OwnerCash); Assert.Equal(cash, world.OwnedClub.Cash);
        Assert.Equal(0, world.OwnerInvested); Assert.Empty(world.Journal); Assert.Empty(world.Decisions);
        Assert.Contains(accepted ? "accepted" : "rejected", receipt.Summary);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "stale-purchase", world.Revision, stalePurchase));
        world = WorldCodec.Clone(world);
        bytes = WorldCodec.Encode(world);
        Assert.Equal(receipt, Proposals.Commit(world, "offer", 0, quote));
        Assert.Equal(bytes, WorldCodec.Encode(world));
        var second = Proposals.Preview(world, new(Allocation.Counteroffer, asking - 100), world.Revision);
        Assert.NotEmpty(second.BlockingReasons);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "second", world.Revision, second));
        var purchase = Proposals.Preview(world, new(Allocation.Acquire), world.Revision);
        var price = accepted ? amount : asking;
        Assert.Empty(purchase.BlockingReasons); Assert.Equal(price, purchase.TotalCommitment);
        Proposals.Commit(world, "purchase", world.Revision, purchase);
        Assert.Equal(CareerStatus.Active, world.Status); Assert.Equal(owner - price, world.OwnerCash);
        Assert.Equal(price, world.OwnerInvested); Assert.Equal(cash, world.OwnedClub.Cash);
        Assert.Equal(-price, Assert.Single(world.Journal, j => j.Account == "owner").Amount);
        Assert.Equal(price, Assert.Single(world.Journal, j => j.Account == "seller").Amount);
        Assert.Equal(2, world.Journal.Count); Assert.Single(world.Decisions);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldCodec.Clone(world)));
        Assert.NotEmpty(Proposals.Preview(world, new(Allocation.Counteroffer, amount), world.Revision).BlockingReasons);
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(280000000)]
    [InlineData(long.MaxValue)]
    public void InvalidOffersCannotConsumeTheAttemptOrMoveMoney(long amount)
    {
        var world = WorldFactory.Create(8); var bytes = WorldCodec.Encode(world);
        var quote = Proposals.Preview(world, new(Allocation.Counteroffer, amount), world.Revision);
        Assert.NotEmpty(quote.BlockingReasons);
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "invalid", world.Revision, quote));
        Assert.Equal(bytes, WorldCodec.Encode(world));
    }

    [Fact]
    public void UnfundedOfferAndUnfundedPurchaseAreBlocked()
    {
        var world = WorldFactory.Create(8); world.OwnerCash = world.OpeningOwnerCash = 100;
        foreach (var command in new[] { new OwnerCommand(Allocation.Counteroffer, 200), new(Allocation.Acquire) })
        {
            var quote = Proposals.Preview(world, command, world.Revision);
            Assert.NotEmpty(quote.BlockingReasons);
            Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "unfunded", world.Revision, quote));
        }
        Assert.Null(world.AcquisitionOffer); Assert.Empty(world.Journal);
    }

    [Fact]
    public void SellerResponseVariesWithSeedWithoutChangingFootballStreams()
    {
        var outcomes = new HashSet<bool>();
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var world = WorldFactory.Create(seed); var before = world.RandomStates.ToDictionary();
            Proposals.Commit(world, "offer", 0, Proposals.Preview(world, new(Allocation.Counteroffer, 252000000), 0));
            outcomes.Add(world.AcquisitionOffer!.Accepted);
            Assert.Equal(before.Count + 1, world.RandomStates.Count);
            foreach (var stream in before) Assert.Equal(stream.Value, world.RandomStates[stream.Key]);
        }
        Assert.Equal(2, outcomes.Count);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(84)]
    public void NegotiatedCareerCanAdvanceFundTheClubAndResumeExactly(int percent)
    {
        var world = WorldFactory.Create(2026);
        Proposals.Commit(world, "offer", 0, Proposals.Preview(world,
            new(Allocation.Counteroffer, Balance.Load().PurchasePrice * percent / 100), 0));
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        while (world.Week < 8) Simulation.AdvanceWeek(world);
        var invested = world.OwnerInvested;
        Proposals.Commit(world, "fund", world.Revision, Proposals.Preview(world, new(Allocation.InjectCapital, 10000), world.Revision));
        Assert.Equal(invested + 10000, world.OwnerInvested);
        var loaded = WorldCodec.Clone(world);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(loaded);
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
        Assert.Equal(2, world.Journal.Count(j => j.Kind == CashKind.Purchase));
        Assert.Single(world.History, h => h.Command.Allocation == Allocation.Counteroffer);
    }

    [Fact]
    public void TamperedSavedResponseAndPaymentAreRejected()
    {
        var world = WorldFactory.Create(1);
        Proposals.Commit(world, "offer", 0, Proposals.Preview(world, new(Allocation.Counteroffer, 277200000), 0));
        var bytes = WorldCodec.Encode(world);
        var node = JsonNode.Parse(bytes)!; node["AcquisitionOffer"]!["Accepted"] = false;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(node.ToJsonString())));
        node = JsonNode.Parse(bytes)!; node["AcquisitionOffer"] = null;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(Encoding.UTF8.GetBytes(node.ToJsonString())));
        SimulationTests.Commit(world, Allocation.Acquire);
        world.Journal[0] = world.Journal[0] with { Amount = -Balance.Load().PurchasePrice };
        world.OwnerCash = world.OpeningOwnerCash - Balance.Load().PurchasePrice;
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(WorldCodec.Encode(world)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SchemaTwentyOneMigrationPreservesOpeningAndCompletedPurchases(bool bought)
    {
        var world = WorldFactory.Create(2026);
        if (bought) SimulationTests.Commit(world, Allocation.Acquire);
        world.SchemaVersion = 21; world.SimulationVersion = "renewal-wage-basis-21";
        var bytes = WorldCodec.Encode(world); var original = bytes.ToArray();
        var stale = Proposals.Preview(world, new(bought ? Allocation.PreserveReserve : Allocation.Acquire), world.Revision);
        var migrated = WorldCodec.Decode(bytes);
        var expected = JsonNode.Parse(bytes)!; expected["SchemaVersion"] = 23; expected["SimulationVersion"] = "compact-journal-23";
        Assert.True(JsonNode.DeepEquals(expected, System.Text.Json.JsonSerializer.SerializeToNode(migrated))); Assert.Equal(original, bytes);
        Assert.Null(migrated.AcquisitionOffer); Assert.Equal(Balance.Load().PurchasePrice, Acquisition.Price(migrated));
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(migrated, "stale", migrated.Revision, stale));
        var quote = Proposals.Preview(migrated, new(Allocation.Counteroffer, 252000000), migrated.Revision);
        Assert.Equal(bought, !quote.BlockingReasons.IsEmpty);
    }

    [Fact]
    public async Task FailedSaveRetainsTheAttemptAndSuccessfulRetrySurvivesReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var fail = true; var writes = 0;
            var vault = new LocalSaveVault(directory, stage => { if (stage == "before-rename" && ++writes == 2 && fail) throw new IOException("Counteroffer save failed"); });
            await using var session = new GameSession(vault);
            var before = await session.ExportCheckpointAsync();
            var view = await session.QueryAsync();
            var quote = await session.PreviewAsync(new(Allocation.Counteroffer, 252000000), view.Revision);
            await Assert.ThrowsAsync<IOException>(() => session.CommitAsync("offer", view.Revision, quote));
            Assert.Equal(before, await session.ExportCheckpointAsync());
            fail = false;
            var receipt = await session.CommitAsync("offer", view.Revision, quote);
            view = await session.QueryAsync();
            Assert.NotNull(view.AcquisitionOffer); Assert.Equal(CareerStatus.Acquisition, view.Status);
            var expected = WorldCodec.Decode(before);
            Proposals.Commit(expected, "offer", expected.Revision, quote);
            Assert.Equal(WorldCodec.Encode(expected), await session.ExportCheckpointAsync());
            var checkpoint = await session.SaveAsync();
            var purchase = await session.PreviewAsync(new(Allocation.Acquire), view.Revision);
            await session.CommitAsync("purchase", view.Revision, purchase);
            await session.LoadAsync(checkpoint.SnapshotId);
            var restored = await session.QueryAsync();
            Assert.Equal(view.AcquisitionOffer, restored.AcquisitionOffer);
            Assert.Equal(view.AcquisitionPrice, restored.AcquisitionPrice);
            Assert.Equal(view.PersonalReserve, restored.PersonalReserve);
            Assert.Equal(CareerStatus.Acquisition, restored.Status);
            Assert.Equal(receipt, await session.CommitAsync("offer", 0, quote));
            Assert.Equal(WorldCodec.Encode(expected), await session.ExportCheckpointAsync());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
