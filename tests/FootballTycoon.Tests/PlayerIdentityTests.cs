using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class PlayerIdentityTests
{
    [Fact]
    public void NamesAreUniqueRealisticAndNotSharedAcrossASquad()
    {
        var world = WorldFactory.Create(7);
        var players = world.Clubs.SelectMany(c => c.Players).ToArray();
        Assert.Equal(players.Length, players.Select(p => p.Name).Distinct().Count());
        Assert.All(players, p =>
        {
            Assert.DoesNotContain(p.Name, char.IsDigit);
            Assert.Equal(2, p.Name.Split(' ').Length);
        });
        foreach (var club in world.Clubs)
        {
            Assert.True(club.Players.Select(p => p.Name.Split(' ')[1]).Distinct().Count() >= 15, $"{club.Name} surnames repeat too often");
            Assert.True(club.Players.Select(p => p.Name.Split(' ')[0]).Distinct().Count() >= 12, $"{club.Name} first names repeat too often");
        }
        Assert.True(world.Clubs.Select(c => c.Players[0].Name.Split(' ')[0]).Distinct().Count() >= 20);
    }

    [Fact]
    public void NamesAreDeterministicPerSeedAndDifferAcrossSeeds()
    {
        static string[] Names(ulong seed) => WorldFactory.Create(seed).Clubs.SelectMany(c => c.Players).Select(p => p.Name).ToArray();
        Assert.Equal(Names(11), Names(11));
        Assert.NotEqual(Names(11), Names(12));
    }

    [Fact]
    public void AgesSpreadRealisticallyWithinEachSquad()
    {
        var world = WorldFactory.Create(3);
        Assert.All(world.Clubs.SelectMany(c => c.Players), p => Assert.InRange(p.Age, 17, 35));
        foreach (var club in world.Clubs)
            Assert.True(club.Players.Select(p => p.Age).Distinct().Count() >= 6, $"{club.Name} ages are too uniform");
        var firstKeeperAges = world.Clubs.Select(c => c.Players[0].Age).Distinct().Count();
        Assert.True(firstKeeperAges >= 5);
    }

    [Fact]
    public void WagesFollowAbilityWhileEachClubKeepsItsOpeningWageBill()
    {
        var world = WorldFactory.Create(5);
        foreach (var club in world.Clubs)
        {
            var factor = club.Division == 1 ? 1.6m : club.Division == 2 ? 1m : 0.65m;
            Assert.Equal(Money.Scale(170000, factor) * 18, club.Players.Sum(p => p.WeeklyWage));
            Assert.True(club.Players.Select(p => p.WeeklyWage).Distinct().Count() >= 8, $"{club.Name} wages are too uniform");
            Assert.All(club.Players, p => Assert.True(p.WeeklyWage > 0));
            var best = club.Players.MaxBy(p => p.Ability)!;
            var worst = club.Players.MinBy(p => p.Ability)!;
            if (best.Ability > worst.Ability + 5) Assert.True(best.WeeklyWage > worst.WeeklyWage, $"{club.Name} pays its weakest player more than its best");
        }
    }

    [Fact]
    public void ContractsExpireAcrossDifferentSeasonBoundaries()
    {
        var world = WorldFactory.Create(9);
        Assert.All(world.Clubs.SelectMany(c => c.Players), p => Assert.Contains(p.ContractEndWeek, new[] { 52, 104, 156 }));
        foreach (var club in world.Clubs)
            Assert.True(club.Players.Select(p => p.ContractEndWeek).Distinct().Count() >= 2, $"{club.Name} contracts all end together");
        foreach (var player in world.Clubs.SelectMany(c => c.Players))
        {
            var obligation = world.Obligations.Single(o => o.Description == $"Player contract {player.ContractId.Value}");
            Assert.Equal(player.ContractEndWeek, obligation.EndWeek);
            Assert.Equal(-player.WeeklyWage, obligation.WeeklyAmount);
        }
    }
}
