using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class ValidationIndexTests
{
    [Theory]
    [InlineData("duplicate-fixture")]
    [InlineData("missing-fixture")]
    [InlineData("unknown-club")]
    [InlineData("cup-winner")]
    [InlineData("club-cash")]
    [InlineData("owner-cash")]
    [InlineData("free-agent-wages")]
    public void IndexedValidationStillRejectsCorruptReferencesAndBalances(string corruption)
    {
        var world = WorldFactory.Create(2026);
        WorldFactory.Validate(world); // Revalidation must see mutations, never a stale index.
        switch (corruption)
        {
            case "duplicate-fixture": world.Fixtures.Add(world.Fixtures[0]); break;
            case "missing-fixture": world.Results.Add(new(new(999999), 0, world.Clubs[0].Id, world.Clubs[1].Id, 0, 0, 0, 0, 0, 0, [])); break;
            case "unknown-club": world.Fixtures[0] = world.Fixtures[0] with { Home = new(999999) }; break;
            case "cup-winner":
                var cup = world.Fixtures.First(f => f.Competition == Competition.Cup);
                world.Results.Add(new(cup.Id, cup.Week, cup.Home, cup.Away, 1, 0, 1, 0, 0, 0, []) { Winner = new(999999) }); break;
            case "club-cash": world.OwnedClub.Cash++; break;
            case "owner-cash": world.OwnerCash++; break;
            case "free-agent-wages":
                var player = world.OwnedClub.Players[0]; world.OwnedClub.Players.RemoveAt(0);
                world.FreeAgents.Add(new(player with { ContractEndWeek = 0 }, world.OwnedClubId, 0)); break;
        }
        Assert.Throws<InvalidDataException>(() => WorldFactory.Validate(world));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("club/1")]
    public void JournalAccumulationRetainsCheckedIntermediateOverflow(string account)
    {
        var world = WorldFactory.Create(2026);
        foreach (var amount in new[] { long.MaxValue, 1, -1, -long.MaxValue })
            world.Journal.Add(new(world.Journal.Count + 1, 0, account, amount, CashKind.Purchase, "overflow probe"));
        Assert.Throws<OverflowException>(() => WorldFactory.Validate(world));
    }

    [Fact]
    public void ReconciliationRetainsInterleavedAccountsAndIgnoresExternalSellerBalances()
    {
        var world = WorldFactory.Create(2026);
        foreach (var amount in new long[] { 100, -100, 500, -500 })
        {
            Finance.Post(world, "owner", amount, CashKind.Injection, "owner");
            Finance.Post(world, "club/1", -amount, CashKind.Injection, "club");
            Finance.Post(world, "seller", long.MaxValue, CashKind.Purchase, "external account");
        }
        var before = WorldCodec.Encode(world);
        WorldFactory.Validate(world);
        Assert.Equal(before, WorldCodec.Encode(world));
    }
}
