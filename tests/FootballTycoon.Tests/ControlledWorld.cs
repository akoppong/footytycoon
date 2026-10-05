using FootballTycoon.Core;

namespace FootballTycoon.Tests;

internal static class ControlledWorld
{
    // Isolate expiry/recruitment edge cases with the original 2/6/6/4 cover,
    // no background market and no unrelated retirements. This is not a new-career fixture.
    internal static World Create(ulong seed = 2026)
    {
        var world = WorldFactory.Create(seed);
        world.FreeAgents.Clear();
        world.RetirementNotices.Clear();
        world.Reviews.Clear();
        foreach (var club in world.Clubs)
            club.Players = club.Players.GroupBy(p => p.Role)
                .SelectMany(g => g.Take(g.Key is Role.Defender or Role.Midfielder ? 6 : g.Count()))
                .Select(p => p with { Age = Math.Min(p.Age, 35) }).ToList();
        var contracts = world.Clubs.SelectMany(c => c.Players).Select(p => $"Player contract {p.ContractId.Value}").ToHashSet();
        world.Obligations = world.Obligations.Where(o => o.Kind != CashKind.Wages || contracts.Contains(o.Description))
            .Select((o, i) => o with { Id = new(i + 1) }).ToList();
        WorldFactory.Validate(world);
        return world;
    }
}
