namespace FootballTycoon.Core;

public static class RetirementLifecycle
{
    public static bool Announced(World world, PersonId id) => world.RetirementNotices.Any(n => n.PlayerId == id);
    public static bool Due(World world, PersonId id) => world.RetirementNotices.Any(n => n.PlayerId == id && n.RetirementWeek <= world.Week);

    // Fictional age policy, announced in the final contract year. Never shorten a signed term.
    internal static void Announce(World world)
    {
        foreach (var club in world.Clubs.OrderBy(c => c.Id.Value))
            foreach (var player in club.Players.OrderBy(p => p.Id.Value))
            {
                var ageAtExpiry = player.Age + Math.Max(0, player.ContractEndWeek / Seasons.Weeks - world.Week / Seasons.Weeks);
                if (Announced(world, player.Id) || player.ContractEndWeek > world.Week + Seasons.Weeks
                    || ageAtExpiry < FreeAgents.RetirementAge(player.Role)) continue;
                var end = Math.Max(world.Week, player.ContractEndWeek);
                world.RetirementNotices.Add(new(player.Id, player.Name, player.Role, club.Id, player.ContractId, world.Week, end));
                if (club.Id == world.OwnedClubId)
                    world.Reviews.Add(new(world.Week, "Retirement announced", $"{player.Name} will retire at contract expiry on {Calendar.FullDay(end)}. Their existing wages remain payable. The director must plan for the vacancy; an affordable replacement is not guaranteed.", null));
            }
    }

    internal static void CompleteDue(World world)
    {
        foreach (var club in world.Clubs.OrderBy(c => c.Id.Value))
            foreach (var player in club.Players.Where(p => Due(world, p.Id)).ToArray()) Complete(world, club, player);
    }

    internal static void Complete(World world, Club club, Player player)
    {
        if (!Due(world, player.Id) || player.ContractEndWeek > world.Week)
            throw new InvalidOperationException("Retirement requires the announced contract to have expired.");
        world.Departures.Add(new(player.Id, player.Name, player.Role, player.Age, player.Ability, club.Id, world.Week, "Retired at contract expiry"));
        FreeAgents.RecordRetirement(world, new(player, club.Id, world.Week));
        club.Players.Remove(player);
    }
}
