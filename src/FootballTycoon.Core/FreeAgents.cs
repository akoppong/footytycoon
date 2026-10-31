namespace FootballTycoon.Core;

// A pure recommendation result; E06 records its evidence when the owner approves the approach.
public sealed record FreeAgentRecommendation(RecruitmentTerms Terms, RecommendationEvidence Evidence);

public static class FreeAgents
{
    public static int RetirementAge(Role role) => role == Role.Goalkeeper ? 42 : 40;
    internal static long Wage(Club club, Player player) => Math.Max(10000, (Math.Max(player.WeeklyWage, Contracts.StandardWage(club.Division) / 2) + 500) / 1000 * 1000);
    internal static int ContractEnd(World world, Player player) => Seasons.EndWeek(world) + (player.Age <= 24 ? 2 : player.Age <= 29 ? 1 : 0) * Seasons.Weeks;
    public static bool HasShortage(Club club, Role role) => club.Players.Count(p => p.Role == role) < Contracts.Minimum.Single(m => m.Role == role).Minimum;
    internal static int WorkingTarget(Role role) => role switch { Role.Goalkeeper => 2, Role.Defender or Role.Midfielder => 6, _ => 4 };
    internal static bool NeedsCover(Club club, Role role) => HasShortage(club, role)
        || club.Players.Count < Contracts.MinimumSquad && club.Players.Count(p => p.Role == role) < WorkingTarget(role);
    public static bool WindowOpen(World world, int week) => week >= Seasons.StartWeek(world)
        && (week <= Seasons.StartWeek(world) + 2 || RecruitmentMarket.Window(world) is var (opens, closes)
            && week >= Seasons.StartWeek(world) + opens && week <= Seasons.StartWeek(world) + closes);
    public static bool EligibleRole(World world, Role role, int approvalWeek) => WindowOpen(world, approvalWeek) || NeedsCover(world.OwnedClub, role);
    public static bool CanReview(World world) => world.Status == CareerStatus.Active && world.AllocationChosen
        && world.Week < Seasons.EndWeek(world) - 1 && world.Negotiations.Count == 0 && world.FreeAgentBids.Count == 0 && world.OwnedClub.Players.Count < Academy.SquadLimit;

    public static RecruitmentTerms? Recommend(World world) => RecommendWithEvidence(world)?.Terms;

    public static FreeAgentRecommendation? RecommendWithEvidence(World world)
    {
        if (!CanReview(world)) return null;
        var club = world.OwnedClub;
        var limit = Money.Scale(Finance.EligibleRevenue(world, club.Id), Balance.Load().WageLimit);
        var eligible = world.FreeAgents.Select(f => f.Player)
            .Where(p => p.Age < RetirementAge(p.Role) && EligibleRole(world, p.Role, world.Week)
                && (NeedsCover(club, p.Role) || p.Ability > club.Players.Where(s => s.Role == p.Role).Min(s => s.Ability))
                && Finance.AnnualWages(club) + Wage(club, p) * Seasons.Weeks <= limit
                && !world.History.Any(h => h.Command.Allocation == Allocation.FreeAgentRecruitment
                    && h.Recruitment?.Player.Id == p.Id && world.Week < h.Week + 4)).ToArray();
        var baseline = eligible
            .OrderByDescending(p => HasShortage(club, p.Role)).ThenByDescending(p => p.Ability).ThenBy(p => p.Age).ThenBy(p => p.Id.Value)
            .FirstOrDefault();
        if (baseline is null) return null;
        RecommendationCandidate Snapshot(Player p) => new(p.Id, p.Role, p.Age, p.Ability, Wage(club, p),
            world.Week + 2, ContractEnd(world, p), checked(Wage(club, p) * (ContractEnd(world, p) - world.Week - 1)));
        var baselineSnapshot = Snapshot(baseline);
        var target = baseline;
        var reason = RecommendationReason.Current;
        if (world.Mandates.Director == DirectorMandate.LowerCostValue)
        {
            var alternative = eligible.Where(p => p.Role == baseline.Role && p.Ability >= baseline.Ability - 5)
                .Select(p => (Player: p, Quote: Snapshot(p)))
                .Where(p => p.Quote.WeeklyWage <= baselineSnapshot.WeeklyWage && p.Quote.TotalCommitment <= baselineSnapshot.TotalCommitment
                    && (p.Quote.WeeklyWage < baselineSnapshot.WeeklyWage || p.Quote.TotalCommitment < baselineSnapshot.TotalCommitment))
                .OrderBy(p => p.Quote.TotalCommitment).ThenBy(p => p.Quote.WeeklyWage).ThenByDescending(p => p.Player.Ability)
                .ThenBy(p => p.Player.Age).ThenBy(p => p.Player.Id.Value).Select(p => p.Player).FirstOrDefault();
            target = alternative ?? baseline;
            reason = alternative is null ? RecommendationReason.NoLowerCostAlternative : RecommendationReason.LowerCostAlternative;
        }
        var current = club.Players.Where(p => p.Role == target.Role).OrderByDescending(p => p.Ability).Take(2).ToArray();
        var terms = new RecruitmentTerms(Allocation.FreeAgentRecruitment, target, "Free agent", 0, Wage(club, target), ContractEnd(world, target), 0)
        { IsFreeAgent = true, CurrentRoleAbility = current.Length == 0 ? 0 : current.Average(p => (decimal)p.Ability) };
        return new(terms, new(world.Week, world.Revision, new(world.Mandates.Manager, world.Mandates.Director, world.Mandates.Revision),
            baselineSnapshot, Snapshot(target), reason));
    }

    internal static void Release(World world, Club club, Player player)
    {
        var free = new FreeAgent(player with { TrainingExposure = 0 }, club.Id, world.Week);
        if (player.Age >= RetirementAge(player.Role)) RecordRetirement(world, free);
        else world.FreeAgents.Add(free);
    }

    internal static void CloseSeason(World world)
    {
        foreach (var free in world.FreeAgents.Where(f => f.Player.Age >= RetirementAge(f.Player.Role)).ToArray())
        { RecordRetirement(world, free); world.FreeAgents.Remove(free); }
    }

    internal static void RecordRetirement(World world, FreeAgent free)
    {
        var p = free.Player;
        world.Retirements.Add(new(p.Id, p.Name, p.Role, p.Age, p.Ability, free.PreviousClubId, world.Week));
        if (free.PreviousClubId == world.OwnedClubId)
            world.Reviews.Add(new(world.Week, $"{p.Name} retires", $"The former {p.Role.ToString().ToLowerInvariant()} retired at age {p.Age} after leaving professional employment. Their expired contract creates no new payment.", null));
    }

    internal static void Resolve(World world)
    {
        // Owner and rival offers share the same stable club order; a refusal leaves the player available.
        foreach (var club in world.Clubs.OrderBy(c => c.Id.Value))
            if (club.Id == world.OwnedClubId) ResolveOwner(world);
            else RivalRecruitment.Resolve(world, club);
        RivalRecruitment.Plan(world);
    }

    internal static void Sign(World world, Club club, FreeAgent free, long wage, int end)
    {
        var contract = new ContractId(10000 + world.Obligations.Count + 1);
        club.Players.Add(free.Player with { WeeklyWage = wage, ContractId = contract, ContractEndWeek = end, TrainingExposure = 0 });
        world.FreeAgents.Remove(free);
        WorldFactory.AddObligation(world, club.Id, world.Week + 1, end, -wage, CashKind.Wages, $"Player contract {contract.Value}");
    }

    private static void ResolveOwner(World world)
    {
        foreach (var bid in world.FreeAgentBids.OrderBy(b => b.DecisionId).ToArray())
        {
            var free = world.FreeAgents.SingleOrDefault(f => f.Player.Id == bid.PlayerId);
            var history = world.History.Single(h => h.OriginalForecast.Id == bid.ForecastId);
            var club = world.OwnedClub;
            var forecast = Finance.ProjectCash(world, club.Id, extraWeekly: bid.WeeklyWage, extraStarts: world.Week + 1, extraEnds: bid.ContractEndWeek);
            var minimum = history.Command.ReserveException ? 0 : world.ReserveTarget;
            var eligible = free is not null && world.Week <= bid.ExpiryWeek && world.Status == CareerStatus.Active
                && club.Players.Count < Academy.SquadLimit && free.Player.Age < RetirementAge(free.Player.Role)
                && EligibleRole(world, free.Player.Role, history.Week)
                && Finance.AnnualWages(club) + bid.WeeklyWage * Seasons.Weeks <= Money.Scale(Finance.EligibleRevenue(world, club.Id), Balance.Load().WageLimit)
                && !world.Arrears.Any(a => a.ClubId == club.Id) && forecast.LowestDownside >= minimum;
            var accepted = eligible && RandomStreams.Next(world, $"free-agent-negotiation/{bid.DecisionId}", 100) < 70;
            if (accepted)
                Sign(world, club, free!, bid.WeeklyWage, bid.ContractEndWeek);
            world.Reviews.Add(new(world.Week, accepted ? "Free agent signed" : "Free-agent approach closed",
                accepted ? $"{free!.Player.Name} joined without a fee. Wages of {Money.Format(bid.WeeklyWage)}/week begin next week through {Calendar.FullDay(bid.ContractEndWeek)}. Selection remains the manager's decision."
                : "The player declined or the availability, deadline, squad, reserve or wage checks no longer allowed the signing. No new contract or payment was created; the existing squad continues.", bid.ForecastId));
            world.FreeAgentBids.Remove(bid);
        }
    }
}
