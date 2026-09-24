using System.Text.Json;

namespace FootballTycoon.Core;

public sealed record Balance(string Version, string[] ClubNames, int OwnedClubIndex, long PurchasePrice, long OwnerCapital,
    long OpeningClubCash, long ReserveTarget, long AnnualBroadcast, long AnnualSponsor, long HistoricalTickets,
    long HistoricalHospitality, long HistoricalCommercial, long WeeklyOperations, long HospitalityCost,
    long HospitalityWeeklyUpkeep, int HospitalityBuildWeeks, long TransferFeeCeiling, long RecruitWeeklyWage, decimal WageLimit)
{
    public static Balance Load()
    {
        using var stream = typeof(Balance).Assembly.GetManifestResourceStream("FootballTycoon.Core.Content.opening.json")!;
        var data = JsonSerializer.Deserialize<Balance>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (data.ClubNames.Length != 48 || data.ClubNames.Distinct().Count() != 48 || data.ClubNames.Any(string.IsNullOrWhiteSpace)
            || data.OwnedClubIndex is < 0 or >= 48 || data.PurchasePrice <= 0 || data.OwnerCapital < data.PurchasePrice
            || data.OpeningClubCash <= 0 || data.ReserveTarget < 0 || data.WageLimit is <= 0 or > 1
            || data.HospitalityBuildWeeks is < 1 or > 52 || data.HospitalityCost <= 0 || data.RecruitWeeklyWage <= 0)
            throw new InvalidDataException("Invalid opening content.");
        return data;
    }
}

public static class RandomStreams
{
    // SplitMix64 v1; FNV-1a namespacing is stable across runtimes (unlike string.GetHashCode).
    public static int Next(World world, string stream, int exclusiveMax)
    {
        if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        if (!world.RandomStates.TryGetValue(stream, out var state))
        {
            state = 14695981039346656037UL ^ world.Seed;
            foreach (var c in stream) state = unchecked((state ^ c) * 1099511628211UL);
        }
        state = unchecked(state + 0x9E3779B97F4A7C15UL);
        world.RandomStates[stream] = state;
        var value = state;
        value = unchecked((value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL);
        value = unchecked((value ^ (value >> 27)) * 0x94D049BB133111EBUL);
        value ^= value >> 31;
        return (int)(value % (uint)exclusiveMax);
    }
}

public static class WorldFactory
{
    public static World Create(ulong seed)
    {
        var b = Balance.Load();
        var world = new World { Seed = seed, OwnedClubId = new(b.OwnedClubIndex + 1), OwnerCash = b.OwnerCapital,
            OpeningOwnerCash = b.OwnerCapital, SeasonOpeningCash = b.OpeningClubCash, ReserveTarget = b.ReserveTarget, Status = CareerStatus.Acquisition };
        string[] firstNames = ["Theo", "Ellis", "Luca", "Adam", "Noah", "Max", "Owen", "Sam", "Finn", "Leo", "Kai", "Ben", "Jude", "Alex", "Rory", "Evan", "Louis", "Isaac"];
        for (var i = 0; i < 48; i++)
        {
            var division = i / 16 + 1;
            var factor = division == 1 ? 1.6m : division == 2 ? 1m : 0.65m;
            var club = new Club { Id = new(i + 1), Name = b.ClubNames[i], Division = division, OpeningDivision = division,
                Cash = Money.Scale(b.OpeningClubCash, factor), OpeningCash = Money.Scale(b.OpeningClubCash, factor),
                AnnualBroadcast = Money.Scale(b.AnnualBroadcast, factor), AnnualSponsor = Money.Scale(b.AnnualSponsor, factor),
                HistoricalTickets = Money.Scale(b.HistoricalTickets, factor), HistoricalHospitality = Money.Scale(b.HistoricalHospitality, factor),
                HistoricalCommercial = Money.Scale(b.HistoricalCommercial, factor), Lot = RandomStreams.Next(world, $"lots/{i}", int.MaxValue) };
            for (var p = 0; p < 18; p++)
                club.Players.Add(new(new(i * 18 + p + 1), $"{firstNames[p]} {new[] { "Mercer", "Ward", "Hale", "Bennett", "Reed", "Clarke" }[i % 6]} {i + 1}",
                    p < 2 ? Role.Goalkeeper : p < 8 ? Role.Defender : p < 14 ? Role.Midfielder : Role.Forward,
                    75 - division * 8 + RandomStreams.Next(world, $"players/{i}", 20),
                    19 + p % 14, Money.Scale(170000, factor), new(i * 18 + p + 1), 104));
            world.Clubs.Add(club);
            foreach (var player in club.Players)
                AddObligation(world, club.Id, 1, player.ContractEndWeek, -player.WeeklyWage, CashKind.Wages, $"Player contract {player.ContractId.Value}");
            AddObligation(world, club.Id, 1, 104, -Money.Scale(b.WeeklyOperations, factor), CashKind.Operations, "Operations including three leadership salaries");
            AddObligation(world, club.Id, 1, 52, club.AnnualBroadcast / 52, CashKind.Broadcast, "Signed broadcast agreement");
            AddObligation(world, club.Id, 1, 52, club.AnnualSponsor / 52, CashKind.Sponsorship, "Signed principal sponsor");
        }
        AddSeasonFixtures(world);
        Validate(world);
        return world;
    }

    public static void AddSeasonFixtures(World world)
    {
        var fixtureId = world.Fixtures.Count;
        var offset = Seasons.StartWeek(world);
        for (var division = 1; division <= 3; division++)
        {
            var ids = world.Clubs.Where(c => c.Division == division).Select(c => c.Id).ToList();
            for (var round = 0; round < 15; round++)
            {
                for (var pair = 0; pair < 8; pair++)
                {
                    var home = ids[pair]; var away = ids[15 - pair];
                    if (round % 2 == 1) (home, away) = (away, home);
                    world.Fixtures.Add(new(new(++fixtureId), offset + round + 3, home, away) { Division = division });
                    world.Fixtures.Add(new(new(++fixtureId), offset + round + 22, away, home) { Division = division });
                }
                ids.Insert(1, ids[^1]); ids.RemoveAt(16);
            }
        }
        if (world.Season >= world.CupStartSeason) Cups.AddOpeningRound(world);
    }

    public static void AddObligation(World world, ClubId club, int start, int end, long weekly, CashKind kind, string description) =>
        world.Obligations.Add(new(new(world.Obligations.Count + 1), club, start, end, weekly, kind, description));

    public static void Validate(World world, bool legacy = false, bool priorCareer = false, bool priorPyramid = false, bool priorCompetition = false)
    {
        if (world.SchemaVersion != (legacy ? 1 : priorCareer ? 2 : priorPyramid ? 3 : priorCompetition ? 4 : 5) || world.SimulationVersion != (legacy ? "prototype-1" : priorCareer ? "career-2" : priorPyramid ? "pyramid-3" : priorCompetition ? "competition-4" : "market-5") || world.ContentVersion != "prototype-1" || world.RandomVersion != 1)
            throw new InvalidDataException("Unsupported save, simulation, content or random version. The source was not changed.");
        if (world.Clubs.Count != 48 || world.Clubs.Select(c => c.Id).Distinct().Count() != 48
            || world.Clubs.Count(c => c.Id == world.OwnedClubId) != 1 || (legacy ? world.Week is < 0 or > 52 : world.Season is < 1 or > Seasons.PlayableSeasons || world.Week < Seasons.StartWeek(world) || world.Week > Seasons.EndWeek(world)) || world.Revision < 0
            || !Enum.IsDefined(world.Status) || !Enum.IsDefined(world.Phase) || world.OwnerCash < 0)
            throw new InvalidDataException("Invalid world boundaries.");
        if (world.Clubs.Any(c => c.Division is < 1 or > 3) || Enumerable.Range(1, 3).Any(d => world.Clubs.Count(c => c.Division == d) != 16))
            throw new InvalidDataException("Invalid division membership.");
        if (!legacy && !priorCareer && (world.Clubs.Any(c => c.OpeningDivision is < 1 or > 3)
            || world.Fixtures.Any(f => f.Competition == Competition.League && f.Division is < 1 or > 3)))
            throw new InvalidDataException("Missing season division history.");
        var people = world.Clubs.SelectMany(c => c.Players).ToArray();
        if (people.Select(p => p.Id).Distinct().Count() != people.Length || people.Any(p => p.WeeklyWage < 0 || p.Ability is < 1 or > 100)
            || world.Obligations.Select(o => o.Id).Distinct().Count() != world.Obligations.Count
            || world.Obligations.Any(o => !world.Clubs.Any(c => c.Id == o.ClubId) || o.StartWeek > o.EndWeek)
            || world.Results.Select(r => r.FixtureId).Distinct().Count() != world.Results.Count
            || world.Results.Any(r => !world.Fixtures.Any(f => f.Id == r.FixtureId))
            || world.Fixtures.Count(f => f.Competition == Competition.League) != 720 * (legacy ? 1 : world.Season) || world.Fixtures.Select(f => f.Id).Distinct().Count() != world.Fixtures.Count
            || world.Fixtures.Any(f => f.Home == f.Away || !world.Clubs.Any(c => c.Id == f.Home) || !world.Clubs.Any(c => c.Id == f.Away)))
            throw new InvalidDataException("Invalid identities or references.");
        if (!legacy && !priorCareer && !priorPyramid && (world.CupStartSeason < 1
            || world.Fixtures.Any(f => !Enum.IsDefined(f.Competition) || f.Competition == Competition.Cup && f.CupRound is < 1 or > 6)
            || world.Results.Any(r => world.Fixtures.Single(f => f.Id == r.FixtureId).Competition == Competition.Cup
                && r.Winner != r.Home && r.Winner != r.Away)
            || world.Fixtures.Count(f => f.Competition == Competition.Cup) > 47 * Math.Max(0, world.Season - world.CupStartSeason + 1)))
            throw new InvalidDataException("Invalid cup history.");
        if (!legacy && (world.SeasonOpeningCash < 0 || world.SeasonOpeningLedgerSequence < 0 || world.SeasonOpeningLedgerSequence > world.Journal.Count
            || world.SeasonSummaries.Select(s => s.Season).Distinct().Count() != world.SeasonSummaries.Count
            || world.SeasonSummaries.Any(s => s.Season < 1 || s.Season > world.Season || s.EndWeek != s.Season * Seasons.Weeks)
            || (world.Status == CareerStatus.SeasonReview && world.Week != Seasons.EndWeek(world))))
            throw new InvalidDataException("Invalid season boundaries.");
        foreach (var club in world.Clubs)
            if (club.Cash < 0 || checked(club.OpeningCash + world.Journal.Where(j => j.Account == Account(club.Id)).Sum(j => j.Amount)) != club.Cash)
                throw new InvalidDataException($"Cash journal does not reconcile for {club.Name}.");
        if (checked(world.OpeningOwnerCash + world.Journal.Where(j => j.Account == "owner").Sum(j => j.Amount)) != world.OwnerCash)
            throw new InvalidDataException("Personal cash journal does not reconcile.");
    }

    public static string Account(ClubId club) => $"club/{club.Value}";
}
