using System.Collections.Immutable;

namespace FootballTycoon.Core;

public static class Seasons
{
    public const int Weeks = 52;
    // A bounded playable milestone, not a claim of the PRD's 50-season production readiness.
    public const int PlayableSeasons = 3;
    public static int StartWeek(World world) => (world.Season - 1) * Weeks;
    public static int EndWeek(World world) => world.Season * Weeks;
    public static int WeekInSeason(int careerWeek, int season) => careerWeek - (season - 1) * Weeks;
    public static bool IsCapitalPlan(Allocation allocation) => allocation is Allocation.PreserveReserve or Allocation.Hospitality or Allocation.Recruitment;

    public static void Close(World world)
    {
        if (world.SeasonSummaries.Any(s => s.Season == world.Season)) return;
        var lines = world.Journal.Where(j => j.Account == WorldFactory.Account(world.OwnedClubId)
            && j.Sequence > world.SeasonOpeningLedgerSequence).ToArray();
        var plan = world.History.LastOrDefault(h => h.Week >= StartWeek(world) && IsCapitalPlan(h.Command.Allocation));
        world.SeasonSummaries.Add(new(world.Season, world.Week, world.SeasonOpeningCash, world.OwnedClub.Cash,
            lines.Where(j => j.Kind is CashKind.Broadcast or CashKind.Sponsorship or CashKind.Commercial or CashKind.Tickets
                or CashKind.Hospitality or CashKind.Wages or CashKind.Operations).Sum(j => j.Amount),
            lines.Where(j => j.Kind == CashKind.Injection).Sum(j => j.Amount), Simulation.Table(world, world.OwnedClub.Division),
            plan?.Intent ?? "No capital plan committed")
        {
            Division = world.OwnedClub.Division,
            CupResult = Cups.Status(world, world.OwnedClubId),
            CupPrize = lines.Where(j => j.Kind == CashKind.Prize).Sum(j => j.Amount)
        });
    }

    public static RenewalTerms Terms(World world)
    {
        var club = world.OwnedClub;
        var expiring = club.Players.Where(p => p.ContractEndWeek <= world.Week).ToArray();
        var nextDivision = Pyramid.NextDivisions(world)[club.Id];
        return new(world.Season + 1, expiring.Length, expiring.Sum(p => p.WeeklyWage) * Weeks,
            Pyramid.Broadcast(nextDivision), Pyramid.Sponsor(nextDivision),
            -world.Obligations.Where(o => o.ClubId == club.Id && o.Kind == CashKind.Operations && o.EndWeek >= world.Week)
                .Sum(o => o.WeeklyAmount) * Weeks,
            world.Arrears.Where(a => a.ClubId == club.Id).Sum(a => a.Amount))
        {
            CurrentDivision = club.Division, NextDivision = nextDivision,
            CurrentAnnualBroadcast = club.AnnualBroadcast, CurrentAnnualSponsor = club.AnnualSponsor
        };
    }

    // Called only on a preview clone or a private transaction candidate. No cash or RNG changes at rollover.
    public static void StartNext(World world)
    {
        if (world.Status != CareerStatus.SeasonReview || world.Week != EndWeek(world) || world.Season >= PlayableSeasons)
            throw new InvalidOperationException("A completed season review is required before starting the next season.");
        var destinations = Pyramid.NextDivisions(world);
        var previousDivision = world.OwnedClub.Division;
        foreach (var club in world.Clubs)
        {
            club.Division = destinations[club.Id];
            club.AnnualBroadcast = Pyramid.Broadcast(club.Division);
            club.AnnualSponsor = Pyramid.Sponsor(club.Division);
        }
        world.Season++;
        world.SeasonOpeningCash = world.OwnedClub.Cash;
        world.SeasonOpeningLedgerSequence = world.Journal.Count;
        var end = EndWeek(world);
        foreach (var club in world.Clubs.OrderBy(c => c.Id.Value))
        {
            WorldFactory.AddObligation(world, club.Id, world.Week + 1, end, club.AnnualBroadcast / Weeks,
                CashKind.Broadcast, $"Season {world.Season} broadcast · published tier rate");
            WorldFactory.AddObligation(world, club.Id, world.Week + 1, end, club.AnnualSponsor / Weeks,
                CashKind.Sponsorship, $"Season {world.Season} sponsor · published tier rate");
            foreach (var obligation in world.Obligations.Where(o => o.ClubId == club.Id && o.Kind == CashKind.Operations
                         && o.EndWeek == world.Week).ToArray())
                WorldFactory.AddObligation(world, club.Id, world.Week + 1, end, obligation.WeeklyAmount,
                    obligation.Kind, obligation.Description);
            for (var i = 0; i < club.Players.Count; i++)
            {
                var player = club.Players[i];
                if (player.ContractEndWeek > world.Week) continue;
                var contractId = new ContractId(10000 + world.Obligations.Count + 1);
                club.Players[i] = player with { ContractId = contractId, ContractEndWeek = end };
                WorldFactory.AddObligation(world, club.Id, world.Week + 1, end, -player.WeeklyWage,
                    CashKind.Wages, $"Player contract {contractId.Value}");
            }
        }
        WorldFactory.AddSeasonFixtures(world);
        world.AllocationChosen = false;
        world.Decisions.Add(new(new(world.Decisions.Count + 1), world.Week, true, $"Choose season {world.Season}'s capital plan"));
        world.Status = world.Arrears.Any(a => a.ClubId == world.OwnedClubId) ? CareerStatus.Administration : CareerStatus.Active;
        // Administration deadlines remain absolute; crossing a season never resets a creditor's clock.
        world.Reviews.Add(new(world.Week, $"Season {world.Season} begins",
            $"{Pyramid.Outcome(previousDivision, world.OwnedClub.Division)} Annual broadcast and sponsor agreements use the new tier’s published rates. Expiring squad and operating contracts were extended for one season at unchanged wages and costs. Choose this season’s capital plan; inherited commitments and arrears remain payable.", null));
    }
}
