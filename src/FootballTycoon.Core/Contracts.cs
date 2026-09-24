using System.Collections.Immutable;

namespace FootballTycoon.Core;

public sealed record ContractPlan(ImmutableArray<ContractReview> Reviews, bool RaisesHeld, long WagesBefore, long WagesAfter, long WageLimit);

// Sporting director's season-end contract policy. Pure and deterministic: no random draws, stable ordering.
public static class Contracts
{
    // A 1-4-4-2 plus one cover player per role, and one spare overall.
    public static readonly ImmutableArray<(Role Role, int Minimum)> Minimum =
        [(Role.Goalkeeper, 2), (Role.Defender, 5), (Role.Midfielder, 5), (Role.Forward, 3)];
    public const int MinimumSquad = 16;
    // Midpoint of generated ability in a division (75 - 8d plus 0 to 19); the director judges players against next season's division.
    public static int Par(int division) => 84 - 8 * division;
    // Opening squad wage in a division; the reference for renewals after promotion or relegation.
    public static long StandardWage(int division) => Money.Scale(170000, Pyramid.Factor(division));

    private static bool Candidate(Player p, int par) => p.Age >= 31 && p.Ability < par + 5 || p.Ability < par - 5;
    private static string Plural(Role role) => role switch
    {
        Role.Goalkeeper => "goalkeepers", Role.Defender => "defenders", Role.Midfielder => "midfielders", _ => "forwards"
    };

    // Next-season contracted broadcast and sponsor terms replace the current ones in the revenue denominator.
    public static long WageLimit(World world, Club club, int nextDivision) => Money.Scale(checked(Finance.EligibleRevenue(world, club.Id)
        - club.AnnualBroadcast - club.AnnualSponsor + Pyramid.Broadcast(nextDivision) + Pyramid.Sponsor(nextDivision)), Balance.Load().WageLimit);

    // Recommendations for every expiring contract, then the owner's reversals. If the director's own plan would
    // breach the wage rule, raises are held at current wages, so accepting the recommendations is always allowed.
    public static ContractPlan Plan(World world, Club club, int nextDivision, IEnumerable<PersonId>? overrides = null)
    {
        var before = Finance.AnnualWages(club);
        var limit = WageLimit(world, club, nextDivision);
        var reviews = Recommend(world, club, nextDivision, false);
        var held = Breaches(before, After(club, reviews), limit);
        if (held) reviews = Recommend(world, club, nextDivision, true);
        var flip = overrides?.ToHashSet() ?? [];
        reviews = reviews.Select(r => flip.Contains(r.PlayerId)
            ? r with { Chosen = r.Recommended == ContractAction.Renew ? ContractAction.Release : ContractAction.Renew } : r).ToImmutableArray();
        return new(reviews, held, before, After(club, reviews), limit);
    }

    // Wages already above the limit (after relegation, say) may continue; the rule stops net new wage commitments.
    private static bool Breaches(long before, long after, long limit) => after > limit && after > before;

    private static long After(Club club, ImmutableArray<ContractReview> reviews) => checked(club.Players.Sum(p =>
        reviews.FirstOrDefault(r => r.PlayerId == p.Id) is { } r ? r.Chosen == ContractAction.Renew ? r.OfferedWage : 0 : p.WeeklyWage) * Seasons.Weeks);

    private static ImmutableArray<ContractReview> Recommend(World world, Club club, int next, bool holdRaises)
    {
        var par = Par(next);
        // After promotion or relegation, new deals start halfway between the current wage and the next division's standard wage.
        long Basis(Player p) => next == club.Division ? p.WeeklyWage : (p.WeeklyWage + StandardWage(next)) / 2;
        var expiring = club.Players.Where(p => p.ContractEndWeek <= world.Week).ToArray();
        var counts = Minimum.ToDictionary(m => m.Role, m => club.Players.Count(p => p.Role == m.Role));
        var minimum = Minimum.ToDictionary(m => m.Role, m => m.Minimum);
        var total = club.Players.Count;
        var release = new HashSet<PersonId>(); var cover = new HashSet<PersonId>();
        // Weakest first; a release that would breach the minimum squad keeps the player as cover instead.
        foreach (var p in expiring.Where(p => Candidate(p, par)).OrderBy(p => p.Ability).ThenByDescending(p => p.Age).ThenBy(p => p.Id.Value))
        {
            if (total - 1 >= MinimumSquad && counts[p.Role] - 1 >= minimum[p.Role]) { release.Add(p.Id); total--; counts[p.Role]--; }
            else cover.Add(p.Id);
        }
        return expiring.OrderBy(p => p.Role).ThenByDescending(p => p.Ability).ThenBy(p => p.Id.Value).Select(p =>
        {
            var aging = p.Age >= 31 && p.Ability < par + 5;
            var standard = $"Division {next} standard";
            var (label, change, years) = release.Contains(p.Id) ? (aging ? $"Aging, {p.Age}, below {standard}" : $"Below {standard}, {p.Age}", -.10m, 1)
                : cover.Contains(p.Id) ? ($"{(aging ? "Aging" : $"Below {standard}")} but needed for cover, {p.Age}", -.10m, 1)
                : p.Ability >= par + 5 ? ($"Key player, {p.Age}", p.Age <= 29 ? .15m : .05m, Length(p.Age))
                : p.Age <= 23 ? ($"Young prospect, {p.Age}", p.Ability >= par ? .10m : 0m, Length(p.Age))
                : p.Age >= 30 ? ($"Veteran, {p.Age}", -.10m, 1)
                : ($"Squad regular, {p.Age}", 0m, Length(p.Age));
            var wage = Math.Max(1000, (Money.Scale(Basis(p), 1 + change) + 500) / 1000 * 1000);
            if (holdRaises) wage = Math.Min(wage, p.WeeklyWage);
            var action = release.Contains(p.Id) ? ContractAction.Release : ContractAction.Renew;
            var reason = action == ContractAction.Release ? $"{label}: recommends release"
                : $"{label}: recommends {years} year{(years == 1 ? "" : "s")} at {Money.Format(wage)}/wk";
            return new ContractReview(p.Id, p.Name, p.Role, p.Age, p.Ability, p.WeeklyWage, action, action, wage, years, reason);
        }).ToImmutableArray();
    }

    private static int Length(int age) => age <= 24 ? 3 : age <= 29 ? 2 : 1;

    public static ImmutableArray<string> Blocking(Club club, ContractPlan plan)
    {
        var reasons = ImmutableArray.CreateBuilder<string>();
        var released = plan.Reviews.Where(r => r.Chosen == ContractAction.Release).ToArray();
        if (released.Length > 0)
        {
            var remaining = club.Players.Where(p => released.All(r => r.PlayerId != p.Id)).ToArray();
            foreach (var (role, minimum) in Minimum)
            {
                var count = remaining.Count(p => p.Role == role);
                if (count < minimum && released.Any(r => r.Role == role))
                    reasons.Add($"The squad needs at least {minimum} {Plural(role)}; these choices leave {count}. Keep a {Plural(role)[..^1]} or accept the recommendation.");
            }
            if (remaining.Length < MinimumSquad)
                reasons.Add($"The squad needs at least {MinimumSquad} players; these choices leave {remaining.Length}. Renew another expiring contract.");
        }
        if (Breaches(plan.WagesBefore, plan.WagesAfter, plan.WageLimit))
            reasons.Add("These renewals take contracted squad wages over 75% of eligible recurring revenue. Release a player or accept the director's recommendations.");
        return reasons.ToImmutable();
    }

    // Applies reviewed choices at the season boundary: renewals start next week; releases leave on a free transfer.
    // A released player's wage obligation already ended with the contract, so nothing further is payable.
    public static void Execute(World world, Club club, ImmutableArray<ContractReview> reviews)
    {
        for (var i = 0; i < club.Players.Count; i++)
        {
            var player = club.Players[i];
            if (reviews.FirstOrDefault(r => r.PlayerId == player.Id) is not { Chosen: ContractAction.Renew } review) continue;
            var contractId = new ContractId(10000 + world.Obligations.Count + 1);
            var end = world.Week + review.Years * Seasons.Weeks;
            club.Players[i] = player with { ContractId = contractId, ContractEndWeek = end, WeeklyWage = review.OfferedWage };
            WorldFactory.AddObligation(world, club.Id, world.Week + 1, end, -review.OfferedWage, CashKind.Wages, $"Player contract {contractId.Value}");
        }
        club.Players.RemoveAll(p => reviews.Any(r => r.PlayerId == p.Id && r.Chosen == ContractAction.Release));
    }
}
