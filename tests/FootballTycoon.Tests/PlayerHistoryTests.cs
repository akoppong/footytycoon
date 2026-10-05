using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class PlayerHistoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    private static (World World, Proposal Proposal) Review()
    {
        var world = ContractTests.Expiring();
        var proposal = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        if (proposal.Renewal!.Released == 0)
        {
            var forward = proposal.Renewal.Contracts.First(c => c.Role == Role.Forward);
            proposal = Proposals.Preview(world, new(Allocation.StartNextSeason) { ContractOverrides = [forward.PlayerId] }, world.Revision);
        }
        Assert.Empty(proposal.BlockingReasons);
        return (world, proposal);
    }

    [Fact]
    public void EveryActualReleaseIsArchivedOnceWithoutKeepingAPlayerInTheSquad()
    {
        var (world, proposal) = Review();
        var before = world.Clubs.SelectMany(c => c.Players.Select(p => (Club: c.Id, Player: p))).ToArray();
        var receipt = Proposals.Commit(world, "renew", world.Revision, proposal);
        var live = world.Clubs.SelectMany(c => c.Players).Select(p => p.Id).ToHashSet();
        var released = before.Where(p => !live.Contains(p.Player.Id)).ToArray();
        Assert.NotEmpty(released); Assert.Equal(released.Length, world.Departures.Count);
        foreach (var entry in released)
        {
            var departure = Assert.Single(world.Departures, d => d.PlayerId == entry.Player.Id);
            Assert.Equal(entry.Club, departure.ClubId);
            Assert.Equal((entry.Player.Name, entry.Player.Role, entry.Player.Age, entry.Player.Ability), (departure.Name, departure.Role, departure.Age, departure.Ability));
            Assert.Equal(52, departure.Week); Assert.Equal(PlayerHistory.ContractRelease, departure.Reason);
        }
        var saved = WorldCodec.Encode(world);
        Assert.Equal(receipt, Proposals.Commit(world, "renew", proposal.Revision, proposal));
        Assert.Equal(saved, WorldCodec.Encode(world));
        Assert.Equal(saved, WorldCodec.Encode(WorldCodec.Clone(world)));
        Assert.All(world.Departures, d => Assert.Equal(d.Name, PlayerHistory.Names(world)[d.PlayerId]));
    }

    [Fact]
    public async Task ReadModelKeepsAllKnownNamesAndShowsOnlyTheOwnedClubsDepartures()
    {
        var (world, proposal) = Review(); Proposals.Commit(world, "renew", world.Revision, proposal);
        var vault = new LocalSaveVault(directory);
        var saved = vault.Write(WorldCodec.Encode(world), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "after-renewal");
        await using var session = new GameSession(vault);
        await session.LoadAsync(saved.SnapshotId);
        var view = await session.QueryAsync();
        Assert.NotEmpty(view.Departures);
        Assert.All(view.Departures, d => Assert.Equal(world.OwnedClubId, d.ClubId));
        Assert.Equal(world.Departures.Count(d => d.ClubId == world.OwnedClubId), view.Departures.Length);
        Assert.All(world.Departures, d => Assert.Equal(d.Name, view.PlayerNames[d.PlayerId]));
        Assert.DoesNotContain(view.Squad, p => view.Departures.Any(d => d.PlayerId == p.Id));
    }

    [Fact]
    public void SchemaTenMigrationRecoversOnlyConfirmedOwnerReleases()
    {
        var (world, proposal) = Review(); Proposals.Commit(world, "renew", world.Revision, proposal);
        var expected = world.Departures.Where(d => d.ClubId == world.OwnedClubId).OrderBy(d => d.PlayerId.Value).ToArray();
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 10; legacy["SimulationVersion"] = "training-10"; legacy.Remove("Departures");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var original = bytes.ToArray();
        var loaded = WorldCodec.Decode(bytes);
        Assert.Equal(original, bytes); Assert.Equal(17, loaded.SchemaVersion);
        Assert.Equal(expected, loaded.Departures.OrderBy(d => d.PlayerId.Value));
        Assert.All(loaded.Departures, d => Assert.Equal(world.OwnedClubId, d.ClubId));
        Assert.Equal(world.Clubs.SelectMany(c => c.Players), loaded.Clubs.SelectMany(c => c.Players));
        Assert.Equal(world.Journal, loaded.Journal); Assert.Equal(world.RandomStates, loaded.RandomStates);
        Assert.Equal(WorldCodec.Encode(loaded), WorldCodec.Encode(WorldCodec.Clone(loaded)));
    }

    [Fact]
    public void NamesUseSavedEvidenceWithoutReplacingCurrentIdentities()
    {
        var world = WorldFactory.Create(9);
        var player = world.OwnedClub.Players[0];
        world.Departures.Add(new(player.Id, "Earlier Name", player.Role, player.Age, player.Ability, world.OwnedClubId, 0, PlayerHistory.ContractRelease));
        Assert.Equal(player.Name, PlayerHistory.Names(world)[player.Id]);
        world.OwnedClub.Players.RemoveAt(0);
        Assert.Equal("Earlier Name", PlayerHistory.Names(world)[player.Id]);
        Assert.False(PlayerHistory.Names(world).ContainsKey(new(999999)));
    }

    [Fact]
    public void MalformedAndDuplicatedDeparturesAreRejected()
    {
        var world = WorldFactory.Create(1); var player = world.OwnedClub.Players[0];
        var entry = new PlayerDeparture(player.Id, player.Name, player.Role, player.Age, player.Ability, world.OwnedClubId, 0, PlayerHistory.ContractRelease);
        foreach (var invalid in new[] { entry with { Week = 1 }, entry with { ClubId = new(999) }, entry with { PlayerId = new(0) }, entry with { Name = "" }, entry with { Ability = 101 } })
        {
            world.Departures = [invalid];
            Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        }
        world.Departures = [entry, entry];
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
    }
}
