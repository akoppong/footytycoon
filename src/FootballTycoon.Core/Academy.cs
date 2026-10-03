using System.Collections.Immutable;

namespace FootballTycoon.Core;

// The inherited academy's annual cohort, not the benefits of a newly purchased upgrade.
// Offers are derived from seed/season/club on a private stream: opening a quote never advances simulation RNG.
public static class Academy
{
    public const int SquadLimit = 22;
    public const int ContractYears = 3;

    public static ImmutableArray<Player> Recommend(World world, Club club, int nextDivision, ContractPlan contracts)
    {
        if (world.Arrears.Any(a => a.ClubId == club.Id)) return [];
        var remaining = club.Players.Count - contracts.Reviews.Count(r => r.Chosen != ContractAction.Renew);
        var annual = contracts.WagesAfter;
        var budget = Math.Max(0, club.Cash - world.ReserveTarget);
        var random = new World { Seed = world.Seed };
        var stream = $"academy/{world.Season}/{club.Id.Value}";
        var count = RandomStreams.Next(random, stream, 3);
        var players = ImmutableArray.CreateBuilder<Player>();
        for (var i = 0; i < count; i++)
        {
            var id = checked(100000 + world.Season * 1000 + club.Id.Value * 10 + i);
            var name = WorldFactory.AcademyName(random, stream);
            var role = (Role)RandomStreams.Next(random, stream, 4);
            var ability = Math.Clamp(Contracts.Par(nextDivision) - 25 + RandomStreams.Next(random, stream, 26), 1, 100);
            var wage = Math.Max(10000, Money.Scale(Contracts.StandardWage(nextDivision), .20m) / 1000 * 1000);
            var cost = checked(wage * Seasons.Weeks);
            if (remaining >= SquadLimit || annual + cost > contracts.WageLimit || cost > budget) continue;
            // Separate reserved identity range; it is never reused after release or a declined offer.
            var player = new Player(new(id), name, role, ability, 17, wage, new(10000000 + id),
                world.Week + Seasons.Weeks * ContractYears);
            players.Add(player); remaining++; annual += cost; budget -= cost;
        }
        return players.ToImmutable();
    }

    internal static void Admit(World world, Club club, ImmutableArray<Player> intake)
    {
        foreach (var player in intake)
        {
            club.Players.Add(player);
            world.AcademyGraduates.Add(new(club.Id, world.Week, player));
            WorldFactory.AddObligation(world, club.Id, world.Week + 1, player.ContractEndWeek,
                -player.WeeklyWage, CashKind.Wages, $"Player contract {player.ContractId.Value}");
        }
    }
}
