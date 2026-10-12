using FootballTycoon.Core;

namespace FootballTycoon.Tests;

internal static class MinimumSquadFixture
{
    internal static (World World, Club Club, Player Candidate) Create(int squad = 15, int week = 2, bool owned = false)
    {
        var world = ControlledWorld.Create();
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.PreserveReserve);
        while (world.Week < week) Simulation.AdvanceWeek(world);
        var club = owned ? world.OwnedClub : world.Clubs.First(c => c.Id != world.OwnedClubId);
        var minimum = club.Players.GroupBy(p => p.Role).SelectMany(g => g.Take(Contracts.Minimum.Single(m => m.Role == g.Key).Minimum)).ToList();
        var retained = minimum.Concat(club.Players.Except(minimum).Take(squad - minimum.Count)).ToList();
        var removed = club.Players.Except(retained).ToArray(); club.Players = retained;
        // Controlled expiries preserve people and every payment already made, with no future wages after release.
        foreach (var player in removed)
        {
            world.Obligations = world.Obligations.Select(o => o.Description == $"Player contract {player.ContractId.Value}" ? o with { EndWeek = world.Week } : o).ToList();
            world.FreeAgents.Add(new(player with { ContractEndWeek = world.Week, TrainingExposure = 0, Age = 25, Ability = 1, WeeklyWage = player == removed[0] ? 10000 : 1000000000 }, club.Id, world.Week));
        }
        Finance.Post(world, WorldFactory.Account(club.Id), 1000000000 - club.Cash, CashKind.Rescue, "Controlled test funding");
        WorldFactory.Validate(world);
        return (world, club, world.FreeAgents[0].Player);
    }
}
