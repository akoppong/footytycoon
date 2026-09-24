namespace FootballTycoon.Core;

public static class Cups
{
    private static readonly int[] RoundWeeks = [2, 19, 20, 37, 43, 47];
    private static readonly long[] WinnerPrizes = [2_500_000, 5_000_000, 7_500_000, 12_500_000, 25_000_000, 50_000_000];
    public static string RoundName(int round) => round switch
    {
        1 => "Preliminary round", 2 => "Round of 32", 3 => "Round of 16",
        4 => "Quarter-final", 5 => "Semi-final", 6 => "Final", _ => "Domestic Cup"
    };

    public static void AddOpeningRound(World world)
    {
        if (world.Fixtures.Any(f => f.Competition == Competition.Cup && SeasonOf(f.Week) == world.Season)) return;
        var entrants = Shuffle(world, world.Clubs.Select(c => c.Id), 1).Take(32).ToArray();
        AddFixtures(world, entrants, 1);
    }

    public static void AdvanceDraw(World world)
    {
        var played = world.Fixtures.Where(f => f.Competition == Competition.Cup && f.Week == world.Week).OrderBy(f => f.Id.Value).ToArray();
        if (played.Length == 0) return;
        var results = played.Select(f => world.Results.SingleOrDefault(r => r.FixtureId == f.Id)).ToArray();
        if (results.Any(r => r is null)) return;
        foreach (var pair in played.Zip(results.Select(r => r!)))
        {
            var reference = $"cup-prize/{pair.First.Id.Value}";
            if (world.Journal.Any(j => j.Reference == reference)) continue;
            Finance.Post(world, WorldFactory.Account(pair.Second.Winner), WinnerPrizes[pair.First.CupRound - 1], CashKind.Prize, reference);
            if (pair.First.Home == world.OwnedClubId || pair.First.Away == world.OwnedClubId)
            {
                var won = pair.Second.Winner == world.OwnedClubId;
                world.Reviews.Add(new(world.Week, won && pair.First.CupRound == 6 ? "Domestic Cup won" : won ? "Cup progress" : "Cup run ends",
                    $"{RoundName(pair.First.CupRound)}: {world.Clubs.Single(c => c.Id == pair.First.Home).Name} {pair.Second.HomeGoals}–{pair.Second.AwayGoals} {world.Clubs.Single(c => c.Id == pair.First.Away).Name}"
                    + (pair.Second.Shootout is null ? "." : $" ({pair.Second.Shootout} on penalties).")
                    + (won ? $" Prize received: {Money.Format(WinnerPrizes[pair.First.CupRound - 1])}." : " No future cup receipts are forecast."), null));
            }
        }
        var round = played[0].CupRound;
        if (round == 6 || world.Fixtures.Any(f => f.Competition == Competition.Cup && SeasonOf(f.Week) == world.Season && f.CupRound == round + 1)) return;
        var qualifiers = results.Select(r => r!.Winner).ToList();
        if (round == 1)
        {
            var preliminary = played.SelectMany(f => new[] { f.Home, f.Away }).ToHashSet();
            qualifiers.AddRange(world.Clubs.Select(c => c.Id).Where(id => !preliminary.Contains(id)));
        }
        AddFixtures(world, Shuffle(world, qualifiers, round + 1), round + 1);
    }

    public static string Status(World world, ClubId club)
    {
        if (world.Season < world.CupStartSeason) return "Cup begins next season";
        var fixtures = world.Fixtures.Where(f => f.Competition == Competition.Cup && SeasonOf(f.Week) == world.Season
            && (f.Home == club || f.Away == club)).OrderBy(f => f.CupRound).ToArray();
        var loss = fixtures.Select(f => (Fixture: f, Result: world.Results.SingleOrDefault(r => r.FixtureId == f.Id)))
            .LastOrDefault(x => x.Result is not null && x.Result.Winner != club);
        if (loss.Result is not null) return $"Eliminated in the {RoundName(loss.Fixture.CupRound).ToLowerInvariant()}";
        var final = fixtures.FirstOrDefault(f => f.CupRound == 6);
        if (final is not null && world.Results.SingleOrDefault(r => r.FixtureId == final.Id)?.Winner == club) return "Domestic Cup winners";
        var next = fixtures.FirstOrDefault(f => !world.Results.Any(r => r.FixtureId == f.Id));
        return next is not null ? $"Next: {RoundName(next.CupRound)} · season week {Seasons.WeekInSeason(next.Week, world.Season)}" : "Awaiting the next cup draw";
    }

    public static long PrizeToDate(World world, ClubId club) => world.Journal.Where(j => j.Account == WorldFactory.Account(club)
        && j.Week > Seasons.StartWeek(world) && j.Kind == CashKind.Prize).Sum(j => j.Amount);

    private static int SeasonOf(int week) => (week - 1) / Seasons.Weeks + 1;
    private static ClubId[] Shuffle(World world, IEnumerable<ClubId> source, int round)
    {
        var values = source.OrderBy(id => id.Value).ToArray();
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = RandomStreams.Next(world, $"cup/{world.Season}/draw/{round}", i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
        return values;
    }
    private static void AddFixtures(World world, IReadOnlyList<ClubId> entrants, int round)
    {
        if (entrants.Count % 2 != 0) throw new InvalidOperationException("Cup draw requires an even field.");
        var week = Seasons.StartWeek(world) + RoundWeeks[round - 1];
        for (var i = 0; i < entrants.Count; i += 2)
            world.Fixtures.Add(new(new(world.Fixtures.Count + 1), week, entrants[i], entrants[i + 1])
                { Competition = Competition.Cup, CupRound = round });
    }
}
