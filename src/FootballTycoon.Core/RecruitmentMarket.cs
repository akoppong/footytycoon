using System.Collections.Immutable;

namespace FootballTycoon.Core;

public static class RecruitmentMarket
{
    // Approval weeks; a mandate approved on the last day resolves the following week.
    public const int MidseasonOpens = 24;
    public const int MidseasonCloses = 27;
    public static bool IsMidseason(Allocation allocation) => allocation is Allocation.MidseasonRecruitment or Allocation.MidseasonValue or Allocation.MidseasonWait;
    public static bool IsSigning(Allocation allocation) => allocation == Allocation.Recruitment || allocation is Allocation.MidseasonRecruitment or Allocation.MidseasonValue;
    public static bool WindowOpen(World world) => world.Week - Seasons.StartWeek(world) is >= MidseasonOpens and <= MidseasonCloses;
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
        if (Decided(world)) return "Midseason decision recorded · see History for the original terms and outcome.";
        var offset = Seasons.StartWeek(world);
        if (world.Week < offset + MidseasonOpens) return $"Midseason review opens in season week {MidseasonOpens}.";
        if (world.Week > offset + MidseasonCloses) return "Midseason window closed · the existing squad continues.";
        return $"Midseason window · approve by season week {MidseasonCloses}; negotiations finish by season week {MidseasonCloses + 1}.";
    }
}
