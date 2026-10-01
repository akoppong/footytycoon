using System.Collections.Immutable;

namespace FootballTycoon.Core;

// Annual development is settled once by Seasons.Close, before the next contract review.
// All clubs use the same policy. Separate streams preserve match and commercial randomness.
public static class PlayerDevelopment
{
    public static string Outlook(Player player) => player.Age <= 23 ? "Developing · progress is uncertain"
        : player.Age <= 27 ? "Approaching peak · modest growth possible"
        : player.Age < DeclineAge(player.Role) ? "Prime years · likely to hold level"
        : "Later career · plan for possible decline";

    private static int DeclineAge(Role role) => role == Role.Goalkeeper ? 34 : 31;

    internal static ImmutableArray<PlayerProgress> CloseSeason(World world)
    {
        if (world.Week != Seasons.EndWeek(world))
            throw new InvalidOperationException("Player development requires a completed season.");
        // Count recorded competitive appearances across clubs, including a player's pre-transfer matches.
        // Older saves can lack line-ups; absent records do not invent playing time.
        var appearances = world.Results.Where(r => r.Week > Seasons.StartWeek(world) && r.Week <= world.Week)
            .SelectMany(r => r.HomeLineup.Concat(r.AwayLineup)).GroupBy(a => a.Player)
            .ToDictionary(g => g.Key, g => g.Count());
        var report = ImmutableArray.CreateBuilder<PlayerProgress>();
        foreach (var club in world.Clubs.OrderBy(c => c.Id.Value))
        {
            for (var i = 0; i < club.Players.Count; i++)
            {
                var player = club.Players[i];
                var played = appearances.GetValueOrDefault(player.Id);
                var roll = RandomStreams.Next(world, $"development/{player.Id.Value}", 100);
                var training = player.Age <= 23 ? player.TrainingExposure / Seasons.Weeks
                    : player.Age <= 27 ? player.TrainingExposure / (Seasons.Weeks * 2) : 0;
                var change = 0;
                if (player.Age <= 23)
                {
                    var chance = 35 + Math.Min(25, played) + training;
                    if (roll < chance) change = 1 + (roll < 15 ? 1 : 0) + (played >= 20 && roll < 5 ? 1 : 0);
                }
                else if (player.Age <= 27 && roll < 15 + Math.Min(15, played) + training) change = 1;
                else if (player.Age >= DeclineAge(player.Role))
                {
                    var chance = Math.Min(90, 35 + (player.Age - DeclineAge(player.Role)) * 10);
                    if (roll < chance) change = -(1 + (player.Age >= DeclineAge(player.Role) + 3 && roll < 30 ? 1 : 0));
                }
                var ability = Math.Clamp(player.Ability + change, 1, 100);
                var evidence = ability > player.Ability ? "Progress during the development years; competitive exposure helps, but never guarantees growth."
                    : ability < player.Ability ? "Age-related decline; succession deserves a review before committing to another contract."
                    : player.Age <= 27 ? "No measurable improvement this season; youth and playing time do not guarantee progress."
                    : "Held their level this season; age alone does not determine an individual's outcome.";
                if (training > 0) evidence += $" Training exposure added {training} percentage points to the growth chance; it did not guarantee this outcome.";
                club.Players[i] = player with { Age = player.Age + 1, Ability = ability, TrainingExposure = 0 };
                if (club.Id == world.OwnedClubId)
                    report.Add(new(player.Id, player.Name, player.Role, player.Age, player.Ability, ability, played, evidence) { TrainingBonus = training });
            }
        }
        return report.ToImmutable();
    }
}
