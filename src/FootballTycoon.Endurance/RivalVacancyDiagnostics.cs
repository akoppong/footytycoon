using FootballTycoon.Core;

namespace FootballTycoon.Endurance;

public sealed record RivalVacancy(int ClubId, int Week, int SquadVacancies, string[] MissingRoles, string Gate,
    int? EligibleCandidates, int? WageEligibleCandidates, string? LastResolvedOutcome, int? LastResolvedWeek);
public sealed record VacancyEpisode(int ClubId, int StartWeek, int LastWeek, int Weeks, bool Open);
public sealed record RivalVacancyReport(int ObservedClubWeeks, int RoleGapClubWeeks, int TotalOnlyClubWeeks,
    Dictionary<string, int> GateClubWeeks, VacancyEpisode[] Episodes);

// End-of-week observations, not a replay of the earlier recruitment decision or an assurance of signing.
public static class RivalVacancyDiagnostics
{
    public static RivalVacancy[] Sample(World world) => world.Clubs
        .Where(c => c.Id != world.OwnedClubId && !Contracts.Shortages(c.Players).IsEmpty)
        .OrderBy(c => c.Id.Value).Select(c => Inspect(world, c)).ToArray();

    private static RivalVacancy Inspect(World world, Club club)
    {
        var roles = Contracts.Minimum.Where(m => club.Players.Count(p => p.Role == m.Role) < m.Minimum).Select(m => m.Role).ToArray();
        var offers = world.RivalApproaches.Where(a => a.ClubId == club.Id).ToArray();
        var lastResolved = offers.Where(a => a.ResolvedWeek is not null).OrderByDescending(a => a.ResolvedWeek).ThenByDescending(a => a.Id).FirstOrDefault();
        RivalVacancy Result(string gate, int? eligible = null, int? wages = null) => new(club.Id.Value, world.Week,
            Math.Max(0, Contracts.MinimumSquad - club.Players.Count), roles.Select(r => r.ToString()).ToArray(), gate,
            eligible, wages, lastResolved?.Outcome.ToString(), lastResolved?.ResolvedWeek);
        // One explicit priority makes buckets disjoint. Other restrictions can coexist.
        if (world.Status is CareerStatus.LostControl or CareerStatus.PrototypeComplete or CareerStatus.SeasonReview) return Result("CareerBoundary");
        if (world.Week >= Seasons.EndWeek(world) - 1) return Result("SeasonClosing");
        if (club.Players.Count >= Academy.SquadLimit) return Result("SquadCapacity");
        if (offers.Any(a => a.Outcome == ApproachOutcome.Pending)) return Result("PendingOffer");
        if (club.Cash < world.ReserveTarget) return Result("CashBelowReserve");
        if (offers.Any(a => world.Week < a.ApprovedWeek + 4)) return Result("Cooldown");
        if (world.Arrears.Any(a => a.ClubId == club.Id)) return Result("Arrears");
        var available = world.FreeAgents.Select(f => f.Player).Where(p => p.Age < FreeAgents.RetirementAge(p.Role)).ToArray();
        if (available.Length == 0) return Result("NoAvailableSupply", 0, 0);
        var eligible = available.Where(p => RivalRecruitment.Needs(world, club, p, world.Week)).ToArray();
        if (eligible.Length == 0)
        {
            if (roles.Length > 0 && !available.Any(p => roles.Contains(p.Role))) return Result("NoNeededRoleSupply", 0, 0);
            return Result("NoSuitableCandidate", 0, 0);
        }
        var affordable = eligible.Where(p => RivalRecruitment.WageAllowed(world, club, FreeAgents.Wage(club, p))).ToArray();
        if (affordable.Length == 0) return Result("WageLimit", eligible.Length, 0);
        var forecast = Finance.ProjectCash(world, club.Id);
        var covered = affordable.Any(p => RivalRecruitment.Covers(forecast, FreeAgents.Wage(club, p), world.Week + 2, FreeAgents.ContractEnd(world, p), world.ReserveTarget));
        return Result(covered ? "EligibleNow" : "DownsideReserve", eligible.Length, affordable.Length);
    }
}

public sealed class RivalVacancyTracker
{
    private int lastWeek;
    private int roleWeeks;
    private int totalOnlyWeeks;
    private readonly Dictionary<string, int> gates = new(StringComparer.Ordinal);
    private readonly Dictionary<int, VacancyEpisode> active = [];
    private readonly List<VacancyEpisode> completed = [];

    public RivalVacancy[] Observe(World world)
    {
        if (world.Week != lastWeek + 1) throw new InvalidOperationException("Vacancy observations require each completed week exactly once, starting at week one.");
        var rows = RivalVacancyDiagnostics.Sample(world);
        var shortIds = rows.Select(r => r.ClubId).ToHashSet();
        foreach (var id in active.Keys.Where(id => !shortIds.Contains(id)).ToArray())
        {
            completed.Add(active[id] with { Open = false }); active.Remove(id);
        }
        foreach (var row in rows)
        {
            gates[row.Gate] = gates.GetValueOrDefault(row.Gate) + 1;
            if (row.MissingRoles.Length > 0) roleWeeks++; else totalOnlyWeeks++;
            active[row.ClubId] = active.TryGetValue(row.ClubId, out var episode)
                ? episode with { LastWeek = world.Week, Weeks = episode.Weeks + 1 }
                : new(row.ClubId, world.Week, world.Week, 1, true);
        }
        lastWeek = world.Week;
        return rows;
    }

    public RivalVacancyReport Report() => new(roleWeeks + totalOnlyWeeks, roleWeeks, totalOnlyWeeks,
        gates.OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Value),
        completed.Concat(active.Values).OrderBy(e => e.ClubId).ThenBy(e => e.StartWeek).ToArray());
}
