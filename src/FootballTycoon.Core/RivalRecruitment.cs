namespace FootballTycoon.Core;

// Conservative replacement policy, not an unrestricted transfer market or a financial rescue.
public static class RivalRecruitment
{
    private static int Target(Role role) => role switch { Role.Goalkeeper => 2, Role.Defender or Role.Midfielder => 6, _ => 4 };
    private static bool Needs(World world, Club club, Player player, int approvalWeek) =>
        FreeAgents.HasShortage(club, player.Role) || FreeAgents.WindowOpen(world, approvalWeek)
        && club.Players.Count < 18 && club.Players.Count(p => p.Role == player.Role) < Target(player.Role)
        && player.Ability > club.Players.Where(p => p.Role == player.Role).Min(p => p.Ability);

    private static bool WageAllowed(World world, Club club, long wage) =>
        club.Cash >= world.ReserveTarget && !world.Arrears.Any(a => a.ClubId == club.Id)
        && Finance.AnnualWages(club) + wage * Seasons.Weeks <= Money.Scale(Finance.EligibleRevenue(world, club.Id), Balance.Load().WageLimit);

    // One baseline per club review, with the same finite wage subtraction as Finance.Forecast.
    private static bool Covers(Forecast baseline, long wage, int starts, int end, long reserve) =>
        baseline.Points.All(p => p.DownsideCash - checked(wage * Math.Max(0, Math.Min(p.Week, end) - starts + 1)) >= reserve);

    internal static void Plan(World world)
    {
        if (world.Week >= Seasons.EndWeek(world) - 1 || world.FreeAgents.Count == 0) return;
        // The administration deadline tick ends in LostControl, so an offer made now could never resolve.
        if (world.Status == CareerStatus.Administration && world.AdministrationWeek is { } start && world.Week >= start + 4) return;
        foreach (var club in world.Clubs.Where(c => c.Id != world.OwnedClubId).OrderBy(c => c.Id.Value))
        {
            if (club.Players.Count >= Academy.SquadLimit || club.Cash < world.ReserveTarget
                || world.RivalApproaches.Any(a => a.ClubId == club.Id && (a.Outcome == ApproachOutcome.Pending || world.Week < a.ApprovedWeek + 4))) continue;
            Forecast? baseline = null;
            var target = world.FreeAgents.Select(f => f.Player)
                .Where(p => Needs(world, club, p, world.Week) && p.Age < FreeAgents.RetirementAge(p.Role))
                .OrderByDescending(p => FreeAgents.HasShortage(club, p.Role)).ThenByDescending(p => p.Ability).ThenBy(p => p.Age).ThenBy(p => p.Id.Value)
                .FirstOrDefault(p => WageAllowed(world, club, FreeAgents.Wage(club, p))
                    && Covers(baseline ??= Finance.Forecast(world, club.Id), FreeAgents.Wage(club, p), world.Week + 2, FreeAgents.ContractEnd(world, p), world.ReserveTarget));
            if (target is null) continue;
            world.RivalApproaches.Add(new(world.RivalApproaches.Count + 1, club.Id, world.Week, target,
                FreeAgents.Wage(club, target), FreeAgents.ContractEnd(world, target), ApproachOutcome.Pending, null));
        }
    }

    internal static void Resolve(World world, Club club)
    {
        var bid = world.RivalApproaches.SingleOrDefault(a => a.ClubId == club.Id && a.Outcome == ApproachOutcome.Pending);
        if (bid is null || world.Week <= bid.ApprovedWeek) return;
        var free = world.FreeAgents.SingleOrDefault(f => f.Player.Id == bid.Player.Id);
        var eligible = free is not null && world.Week == bid.ApprovedWeek + 1 && world.Week < bid.ContractEndWeek
            && club.Players.Count < Academy.SquadLimit && free.Player.Age < FreeAgents.RetirementAge(free.Player.Role)
            && Needs(world, club, free.Player, bid.ApprovedWeek)
            && WageAllowed(world, club, bid.WeeklyWage)
            && Covers(Finance.Forecast(world, club.Id), bid.WeeklyWage, world.Week + 1, bid.ContractEndWeek, world.ReserveTarget);
        var outcome = free is null ? ApproachOutcome.Unavailable : !eligible ? ApproachOutcome.ChecksFailed
            : RandomStreams.Next(world, $"rival-free-agent/{bid.Id}", 100) < 70 ? ApproachOutcome.Signed : ApproachOutcome.Declined;
        if (outcome == ApproachOutcome.Signed) FreeAgents.Sign(world, club, free!, bid.WeeklyWage, bid.ContractEndWeek);
        world.RivalApproaches[world.RivalApproaches.IndexOf(bid)] = bid with { Outcome = outcome, ResolvedWeek = world.Week };
        if (outcome == ApproachOutcome.Signed && world.Departures.Any(d => d.ClubId == world.OwnedClubId && d.PlayerId == bid.Player.Id))
            world.Reviews.Add(new(world.Week, "Former player joins a rival", $"{bid.Player.Name} joined {club.Name} without a fee. Their new wages are {Money.Format(bid.WeeklyWage)}/week from {Calendar.FullDay(world.Week + 1)} through {Calendar.FullDay(bid.ContractEndWeek)}. Your club receives no payment.", null));
    }
}
