using FootballTycoon.Core;

namespace FootballTycoon.Endurance;

public sealed record RolePopulation(string Role, int Contracted, int Available, int AvailableBelowRetirementAge,
    int Graduated, int Retired, int CoverVacancies, int ClubsBelowCover);
public sealed record GraduateCohort(int Week, int Admitted, int Contracted, int Available, int Retired);
public sealed record RoleVacancy(string Role, int Players, int Target, int AvailableBelowRetirementAge);
public sealed record ShortClub(int ClubId, bool Owned, int SquadVacancies, RoleVacancy[] Roles,
    int PendingOffers, int RecentSigned, int RecentDeclined, int RecentUnavailable, int RecentChecksFailed);
public sealed record OfferOutcomes(string Role, int Pending, int Signed, int Declined, int Unavailable, int ChecksFailed);
public sealed record CoverageReport(RolePopulation[] Roles, GraduateCohort[] Cohorts, ShortClub[] ShortClubs,
    OfferOutcomes[] RivalOffers);

// Observations only: availability is not a promise of affordability, acceptance or an actionable owner quote.
public static class CoverageDiagnostics
{
    public static CoverageReport Sample(World world)
    {
        var contracted = world.Clubs.SelectMany(c => c.Players).ToArray();
        var contractedIds = contracted.Select(p => p.Id).ToHashSet();
        var availableIds = world.FreeAgents.Select(f => f.Player.Id).ToHashSet();
        var retiredIds = world.Retirements.Select(r => r.PlayerId).ToHashSet();
        var available = Enum.GetValues<Role>().ToDictionary(role => role,
            role => world.FreeAgents.Count(f => f.Player.Role == role && f.Player.Age < FreeAgents.RetirementAge(role)));
        var roles = Contracts.Minimum.Select(m => new RolePopulation(m.Role.ToString(),
            contracted.Count(p => p.Role == m.Role), world.FreeAgents.Count(f => f.Player.Role == m.Role), available[m.Role],
            world.AcademyGraduates.Count(g => g.Player.Role == m.Role), world.Retirements.Count(r => r.Role == m.Role),
            world.Clubs.Sum(c => Math.Max(0, m.Minimum - c.Players.Count(p => p.Role == m.Role))),
            world.Clubs.Count(c => c.Players.Count(p => p.Role == m.Role) < m.Minimum))).ToArray();
        var cohorts = world.AcademyGraduates.GroupBy(g => g.Week).OrderBy(g => g.Key).Select(g => new GraduateCohort(
            g.Key, g.Count(), g.Count(p => contractedIds.Contains(p.Player.Id)),
            g.Count(p => availableIds.Contains(p.Player.Id)), g.Count(p => retiredIds.Contains(p.Player.Id)))).ToArray();
        var shortClubs = world.Clubs.Where(c => !Contracts.Shortages(c.Players).IsEmpty).OrderBy(c => c.Id.Value).Select(c =>
        {
            var offers = world.RivalApproaches.Where(a => a.ClubId == c.Id).ToArray();
            // Resolutions in the last 52 completed weeks; pending offers are counted independently of approval date.
            int Recent(ApproachOutcome outcome) => offers.Count(a => a.Outcome == outcome
                && a.ResolvedWeek is { } week && week > world.Week - Seasons.Weeks && week <= world.Week);
            return new ShortClub(c.Id.Value, c.Id == world.OwnedClubId, Math.Max(0, Contracts.MinimumSquad - c.Players.Count),
                Contracts.Minimum.Where(m => c.Players.Count(p => p.Role == m.Role) < m.Minimum).Select(m =>
                    new RoleVacancy(m.Role.ToString(), c.Players.Count(p => p.Role == m.Role), m.Minimum, available[m.Role])).ToArray(),
                offers.Count(a => a.Outcome == ApproachOutcome.Pending), Recent(ApproachOutcome.Signed), Recent(ApproachOutcome.Declined),
                Recent(ApproachOutcome.Unavailable), Recent(ApproachOutcome.ChecksFailed));
        }).ToArray();
        var outcomes = Enum.GetValues<Role>().Select(role =>
        {
            var offers = world.RivalApproaches.Where(a => a.Player.Role == role).ToArray();
            int Count(ApproachOutcome outcome) => offers.Count(a => a.Outcome == outcome);
            return new OfferOutcomes(role.ToString(), Count(ApproachOutcome.Pending), Count(ApproachOutcome.Signed),
                Count(ApproachOutcome.Declined), Count(ApproachOutcome.Unavailable), Count(ApproachOutcome.ChecksFailed));
        }).ToArray();
        return new(roles, cohorts, shortClubs, outcomes);
    }
}
