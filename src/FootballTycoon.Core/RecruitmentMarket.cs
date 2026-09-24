using System.Collections.Immutable;

namespace FootballTycoon.Core;

public static class RecruitmentMarket
{
    public static bool IsMidseason(Allocation allocation) => allocation is Allocation.MidseasonRecruitment or Allocation.MidseasonValue or Allocation.MidseasonWait;
    public static bool IsSigning(Allocation allocation) => allocation == Allocation.Recruitment || allocation is Allocation.MidseasonRecruitment or Allocation.MidseasonValue;

    // Season-relative approval weeks; a mandate approved on the last day resolves the following week.
    // Dated seasons open on the first Saturday on or after 1 January and complete every deal by 1 February.
    public static (int Opens, int Closes) Window(World world)
    {
        if (!Calendar.IsDated(world, world.Season)) return (24, 27);
        var start = Seasons.StartWeek(world);
        var year = Calendar.Date(start + 1).Year + 1;
        var opens = Enumerable.Range(1, Seasons.Weeks).First(w => Calendar.Date(start + w) >= new DateOnly(year, 1, 1));
        var closes = Enumerable.Range(opens, Seasons.Weeks - opens).Last(w => Calendar.Date(start + w + 1) <= new DateOnly(year, 2, 1));
        return (opens, closes);
    }
    public static bool WindowOpen(World world) => Window(world) is var (opens, closes) && world.Week - Seasons.StartWeek(world) >= opens && world.Week - Seasons.StartWeek(world) <= closes;
    public static string WindowName(World world) => Calendar.IsDated(world, world.Season) ? "January" : "Midseason";
    public static bool Decided(World world) => world.History.Any(h => h.Week >= Seasons.StartWeek(world) && IsMidseason(h.Command.Allocation));
    public static bool Available(World world) => world.Status == CareerStatus.Active && world.AllocationChosen && WindowOpen(world) && !Decided(world);

    public static RecruitmentTerms? Recommend(World world, Allocation allocation)
    {
        var b = Balance.Load();
        var sellers = world.Clubs.Where(c => c.Id != world.OwnedClubId && c.Division == world.OwnedClub.Division
            && c.Players.Count(p => p.Role == Role.Forward) > 2).OrderBy(c => c.Id.Value);
        (Club Seller, Player Player)? target;
        if (allocation == Allocation.Recruitment)
        {
            var seller = sellers.FirstOrDefault();
            target = seller is null ? null : (seller, seller.Players.Where(p => p.Role == Role.Forward)
                .OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).First());
        }
        else
        {
            // Rivals retain their two strongest forwards. The director presents a bounded shortlist.
            var pool = sellers.SelectMany(c => c.Players.Where(p => p.Role == Role.Forward)
                .OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).Skip(2).Select(p => (Seller: c, Player: p)))
                .OrderByDescending(x => x.Player.Ability).ThenBy(x => x.Player.Id.Value).ToArray();
            if (pool.Length == 0) return null;
            var first = pool[0];
            target = allocation == Allocation.MidseasonValue
                ? pool.Where(x => x.Player.Id != first.Player.Id).OrderBy(x => x.Player.WeeklyWage)
                    .ThenBy(x => x.Player.Ability).ThenBy(x => x.Player.Id.Value).Select(x => ((Club Seller, Player Player)?)x).FirstOrDefault()
                : first;
        }
        if (target is not { } choice) return null;
        var value = allocation == Allocation.MidseasonValue;
        var wage = value ? Math.Max(Money.Scale(choice.Player.WeeklyWage, 1.1m), Money.Scale(b.RecruitWeeklyWage, .75m)) : b.RecruitWeeklyWage;
        var forwards = world.OwnedClub.Players.Where(p => p.Role == Role.Forward).OrderByDescending(p => p.Ability).Take(2).ToArray();
        return new(allocation, choice.Player, choice.Seller.Name, value ? Money.Scale(b.TransferFeeCeiling, .65m) : b.TransferFeeCeiling,
            wage, Seasons.EndWeek(world) + Seasons.Weeks, forwards.Length == 0 ? 0 : forwards.Average(p => (decimal)p.Ability));
    }

    public static ImmutableArray<RecruitmentTerms> Options(World world) => Available(world)
        ? new[] { Recommend(world, Allocation.MidseasonRecruitment), Recommend(world, Allocation.MidseasonValue) }
            .OfType<RecruitmentTerms>().ToImmutableArray() : [];

    public static string Status(World world)
    {
        var name = WindowName(world);
        if (Decided(world)) return $"{name} window decision recorded · see History for the original terms and outcome.";
        var offset = Seasons.StartWeek(world);
        var (opens, closes) = Window(world);
        if (world.Week < offset + opens) return $"{name} window opens {Calendar.Day(offset + opens)}.";
        if (world.Week > offset + closes) return $"{name} window closed · the existing squad continues.";
        return $"{name} window open · approve by {Calendar.Day(offset + closes)}; deals complete by {Calendar.Day(offset + closes + 1)}.";
    }
}
