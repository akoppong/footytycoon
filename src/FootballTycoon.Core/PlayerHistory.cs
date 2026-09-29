using System.Collections.Immutable;

namespace FootballTycoon.Core;

public static class PlayerHistory
{
    public const string ContractRelease = "Released at contract expiry";

    public static List<PlayerDeparture> RecoverDepartures(World world) => world.History
        .Where(h => h.Command.Allocation == Allocation.StartNextSeason && h.Renewal is not null)
        .SelectMany(h => h.Renewal!.Contracts.Where(c => c.Chosen == ContractAction.Release)
            .Select(c => new PlayerDeparture(c.PlayerId, c.Name, c.Role, c.Age, c.Ability,
                world.OwnedClubId, h.Week, ContractRelease))).ToList();

    // Current identities win. Historical reports provide only names actually saved at the time.
    public static ImmutableDictionary<PersonId, string> Names(World world)
    {
        var names = world.Clubs.SelectMany(c => c.Players).ToDictionary(p => p.Id, p => p.Name);
        foreach (var player in world.Departures) names.TryAdd(player.PlayerId, player.Name);
        foreach (var player in world.SeasonSummaries.SelectMany(s => s.Development)) names.TryAdd(player.PlayerId, player.Name);
        foreach (var decision in world.History)
        {
            if (decision.Recruitment is { } recruitment) names.TryAdd(recruitment.Player.Id, recruitment.Player.Name);
            if (decision.Renewal is not { } renewal) continue;
            foreach (var player in renewal.Contracts) names.TryAdd(player.PlayerId, player.Name);
        }
        return names.ToImmutableDictionary();
    }
}
