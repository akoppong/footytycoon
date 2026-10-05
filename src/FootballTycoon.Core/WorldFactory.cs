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
        var world = new World
        {
            Seed = seed,
            OwnedClubId = new(b.OwnedClubIndex + 1),
            OwnerCash = b.OwnerCapital,
            OpeningOwnerCash = b.OwnerCapital,
            SeasonOpeningCash = b.OpeningClubCash,
            ReserveTarget = b.ReserveTarget,
            Status = CareerStatus.Acquisition
        };
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 48; i++)
        {
            var division = i / 16 + 1;
            var factor = division == 1 ? 1.6m : division == 2 ? 1m : 0.65m;
            var club = new Club
            {
                Id = new(i + 1),
                Name = b.ClubNames[i],
                Division = division,
                OpeningDivision = division,
                Cash = Money.Scale(b.OpeningClubCash, factor),
                OpeningCash = Money.Scale(b.OpeningClubCash, factor),
                AnnualBroadcast = Money.Scale(b.AnnualBroadcast, factor),
                AnnualSponsor = Money.Scale(b.AnnualSponsor, factor),
                HistoricalTickets = Money.Scale(b.HistoricalTickets, factor),
                HistoricalHospitality = Money.Scale(b.HistoricalHospitality, factor),
                HistoricalCommercial = Money.Scale(b.HistoricalCommercial, factor),
                Lot = RandomStreams.Next(world, $"lots/{i}", int.MaxValue)
            };
            AddSquad(world, club, i, division, Contracts.StandardWage(division) * 18, usedNames);
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

    private static readonly string[] FirstNames =
    [
        "Theo", "Ellis", "Luca", "Adam", "Noah", "Max", "Owen", "Sam", "Finn", "Leo", "Kai", "Ben", "Jude", "Alex", "Rory", "Evan",
        "Louis", "Isaac", "Callum", "Jamie", "Connor", "Harvey", "Kieran", "Liam", "Mason", "Nathan", "Oscar", "Reece", "Ryan", "Tom",
        "Joe", "Dan", "Marcus", "Jordan", "Tyler", "Aaron", "Declan", "Ethan", "Harry", "Jack", "Lewis", "Matty", "Ollie", "Scott",
        "Kyle", "Dylan", "Ross", "Sean", "Niall", "Gareth", "Rhys", "Ewan", "Fraser", "Kofi", "Tariq", "Mateo", "Diego", "Mikel",
        "Tomas", "Andrei", "Pavel", "Jonas", "Lars", "Emeka", "Sami", "Yusuf", "Kwame", "Ibrahim", "Rafael", "Bruno", "Nico", "Hugo"
    ];

    private static readonly string[] Surnames =
    [
        "Mercer", "Ward", "Hale", "Bennett", "Reed", "Clarke", "Walsh", "Hughes", "Fletcher", "Barnes", "Holt", "Pearce", "Doyle",
        "Shaw", "Kerr", "Lowe", "Marsh", "Nolan", "Parry", "Quinn", "Rowe", "Sutton", "Tate", "Vaughan", "Whitaker", "Ashworth",
        "Bland", "Carver", "Dawson", "Egan", "Foley", "Gibbs", "Harding", "Ingram", "Jarvis", "Keane", "Lister", "McGowan", "Naylor",
        "Osborne", "Price", "Riley", "Sharpe", "Thorne", "Upton", "Vickers", "Webb", "Yates", "Abbott", "Brennan", "Coyle", "Dunne",
        "Ellison", "Frost", "Gallagher", "Hendry", "Irwin", "Jennings", "Kilbride", "Lennon", "Maguire", "Norris", "O'Neill", "Pritchard",
        "Robson", "Stokes", "Tierney", "Varley", "Wilder", "Adeyemi", "Boateng", "Mensah", "Okafor", "Diallo", "Traore", "Silva",
        "Costa", "Moreno", "Navarro", "Kowalski", "Novak", "Horvat", "Jansen", "Visser", "Lindqvist", "Berg", "Moretti", "Rossi",
        "Fischer", "Richter", "Dubois", "Laurent", "Haddad", "Petrov", "Evans", "Morgan", "Pryce", "Jenkins", "Campbell", "Kendall"
    ];

    internal static string AcademyName(World random, string stream) => $"{FirstNames[RandomStreams.Next(random, stream, FirstNames.Length)]} {Surnames[RandomStreams.Next(random, stream, Surnames.Length)]}";

    // A separate "identity/{club}" stream keeps the older "players/{club}" ability draws, and therefore match results, unchanged.
    private static void AddSquad(World world, Club club, int index, int division, long wageBill, HashSet<string> usedNames)
    {
        var stream = $"identity/{index}";
        var clubFirst = new HashSet<string>(StringComparer.Ordinal);
        var clubSurnames = new HashSet<string>(StringComparer.Ordinal);
        var drafts = new List<(string Name, Role Role, int Ability, int Age, int ContractEnd)>();
        for (var p = 0; p < 18; p++)
        {
            string first, surname;
            do first = FirstNames[RandomStreams.Next(world, stream, FirstNames.Length)]; while (!clubFirst.Add(first));
            do surname = Surnames[RandomStreams.Next(world, stream, Surnames.Length)];
            while (clubSurnames.Contains(surname) || usedNames.Contains($"{first} {surname}"));
            clubSurnames.Add(surname);
            usedNames.Add($"{first} {surname}");
            var age = 17 + RandomStreams.Next(world, stream, 10) + RandomStreams.Next(world, stream, 10);
            drafts.Add(($"{first} {surname}", p < 2 ? Role.Goalkeeper : p < 8 ? Role.Defender : p < 14 ? Role.Midfielder : Role.Forward,
                75 - division * 8 + RandomStreams.Next(world, $"players/{index}", 20), age,
                Seasons.Weeks * (1 + RandomStreams.Next(world, stream, Seasons.OpeningContractYears))));
        }
        // Stronger players in their prime earn more; the club's total opening wage bill stays exactly as balanced.
        var weakest = drafts.Min(d => d.Ability);
        var weights = drafts.Select(d => (long)(d.Ability - weakest + 12) * (d.Ability - weakest + 12)
            * (d.Age < 21 ? 6 : d.Age > 31 ? 8 : 10) + RandomStreams.Next(world, stream, 40)).ToArray();
        var totalWeight = weights.Sum();
        var wages = weights.Select(w => wageBill * w / totalWeight / 1000 * 1000).ToArray();
        wages[Array.IndexOf(weights, weights.Max())] += wageBill - wages.Sum();
        for (var p = 0; p < 18; p++)
        {
            var id = index * 18 + p + 1;
            club.Players.Add(new(new(id), drafts[p].Name, drafts[p].Role, drafts[p].Ability, drafts[p].Age, wages[p], new(id), drafts[p].ContractEnd));
        }
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
                    world.Fixtures.Add(new(new(++fixtureId), offset + Calendar.LeagueWeek(world, round, false), home, away) { Division = division });
                    world.Fixtures.Add(new(new(++fixtureId), offset + Calendar.LeagueWeek(world, round, true), away, home) { Division = division });
                }
                ids.Insert(1, ids[^1]); ids.RemoveAt(16);
            }
        }
        if (world.Season >= world.CupStartSeason) Cups.AddOpeningRound(world);
    }

    public static void AddObligation(World world, ClubId club, int start, int end, long weekly, CashKind kind, string description) =>
        world.Obligations.Add(new(new(world.Obligations.Count + 1), club, start, end, weekly, kind, description));

    public static void Validate(World world, bool legacy = false, bool priorCareer = false, bool priorPyramid = false, bool priorCompetition = false, bool priorMarket = false, bool priorCalendar = false, bool priorMatchday = false, bool priorContracts = false, bool priorDevelopment = false, bool priorTraining = false, bool priorHistory = false, bool priorAcademy = false, bool priorFreeAgents = false, bool priorRivalMarket = false)
    {
#if ENDURANCE
        using var timing = DiagnosticTimings.Measure("world-validation");
#endif
        if (world.SchemaVersion != (legacy ? 1 : priorCareer ? 2 : priorPyramid ? 3 : priorCompetition ? 4 : priorMarket ? 5 : priorCalendar ? 6 : priorMatchday ? 7 : priorContracts ? 8 : priorDevelopment ? 9 : priorTraining ? 10 : priorHistory ? 11 : priorAcademy ? 12 : priorFreeAgents ? 13 : priorRivalMarket ? 14 : 15) || world.SimulationVersion != (legacy ? "prototype-1" : priorCareer ? "career-2" : priorPyramid ? "pyramid-3" : priorCompetition ? "competition-4" : priorMarket ? "market-5" : priorCalendar ? "calendar-6" : priorMatchday ? "matchday-7" : priorContracts ? "contracts-8" : priorDevelopment ? "development-9" : priorTraining ? "training-10" : priorHistory ? "people-history-11" : priorAcademy ? "academy-12" : priorFreeAgents ? "free-agents-13" : priorRivalMarket ? "rival-market-14" : "retirement-15") || world.ContentVersion != "prototype-1" || world.RandomVersion != 1)
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
        if (world.SchemaVersion >= 15 && (world.RetirementNotices is null
            || world.RetirementNotices.Select(n => n.PlayerId).Distinct().Count() != world.RetirementNotices.Count
            || world.RetirementNotices.Any(n => n.PlayerId.Value <= 0 || string.IsNullOrWhiteSpace(n.Name) || !Enum.IsDefined(n.Role)
                || world.Clubs.All(c => c.Id != n.ClubId) || n.ContractId.Value <= 0 || n.AnnouncedWeek < 0 || n.AnnouncedWeek > world.Week
                || n.RetirementWeek < n.AnnouncedWeek
                || !(world.Clubs.Any(c => c.Id == n.ClubId && c.Players.Any(p => p.Id == n.PlayerId && p.ContractId == n.ContractId
                        && Math.Max(p.ContractEndWeek, n.AnnouncedWeek) == n.RetirementWeek && p.Role == n.Role && p.Name == n.Name))
                    || world.Retirements.Any(r => r.PlayerId == n.PlayerId && r.LastClubId == n.ClubId && r.Week >= n.RetirementWeek)))
            || world.History.Any(h => h.Renewal is { } renewal && renewal.Contracts.Any(c => c.Recommended == ContractAction.Retire
                && (c.Chosen != ContractAction.Retire || c.Years != 0 || c.OfferedWage != 0)))))
            throw new InvalidDataException("Invalid retirement announcements or fixed departures.");
        if (world.SchemaVersion >= 14 && (world.RivalApproaches is null
            || !world.RivalApproaches.Select(a => a.Id).SequenceEqual(Enumerable.Range(1, world.RivalApproaches.Count))
            || world.RivalApproaches.Where(a => a.Outcome == ApproachOutcome.Pending).GroupBy(a => a.ClubId).Any(g => g.Count() > 1)
            || world.RivalApproaches.Any(a => a.ClubId == world.OwnedClubId || world.Clubs.All(c => c.Id != a.ClubId)
                || a.Player is null || a.Player.Id.Value <= 0 || string.IsNullOrWhiteSpace(a.Player.Name) || !Enum.IsDefined(a.Player.Role)
                || a.Player.Age < 16 || a.Player.Age >= FreeAgents.RetirementAge(a.Player.Role) || a.Player.Ability is < 1 or > 100
                || a.ApprovedWeek < 0 || a.ApprovedWeek > world.Week || a.WeeklyWage <= 0
                || a.ContractEndWeek <= a.ApprovedWeek + 1 || a.ContractEndWeek % Seasons.Weeks != 0 || !Enum.IsDefined(a.Outcome)
                || (a.Outcome == ApproachOutcome.Pending ? a.ResolvedWeek is not null || a.ApprovedWeek != world.Week
                    : a.ResolvedWeek != a.ApprovedWeek + 1 || a.ResolvedWeek > world.Week))))
            throw new InvalidDataException("Invalid rival recruitment history.");
        if (world.SchemaVersion >= 13)
        {
            if (world.FreeAgents is null || world.Retirements is null || world.FreeAgentBids is null)
                throw new InvalidDataException("Missing free-agent lifecycle state.");
            var living = people.Select(p => p.Id).Concat(world.FreeAgents.Select(f => f.Player.Id)).ToArray();
            var livingIds = living.ToHashSet();
            var activeWageDescriptions = world.Obligations.Where(o => o.Kind == CashKind.Wages && o.EndWeek > world.Week)
                .Select(o => o.Description).ToHashSet(StringComparer.Ordinal);
            if (livingIds.Count != living.Length
                || world.Retirements.Select(r => r.PlayerId).Distinct().Count() != world.Retirements.Count
                || world.Retirements.Any(r => livingIds.Contains(r.PlayerId) || r.PlayerId.Value <= 0 || string.IsNullOrWhiteSpace(r.Name)
                    || !Enum.IsDefined(r.Role) || r.Age < FreeAgents.RetirementAge(r.Role) || r.Age > 100 || r.Ability is < 1 or > 100
                    || r.Week < 0 || r.Week > world.Week || world.Clubs.All(c => c.Id != r.LastClubId))
                || world.FreeAgents.Any(f => f.Player.Id.Value <= 0 || string.IsNullOrWhiteSpace(f.Player.Name) || !Enum.IsDefined(f.Player.Role)
                    || f.Player.Age < 16 || f.Player.Age >= FreeAgents.RetirementAge(f.Player.Role) || f.Player.Ability is < 1 or > 100
                    || f.AvailableSinceWeek < 0 || f.AvailableSinceWeek > world.Week || f.Player.ContractEndWeek > f.AvailableSinceWeek
                    || f.Player.WeeklyWage < 0 || f.Player.TrainingExposure != 0 || f.Player.SuspendedMatches < 0 || f.Player.SeasonYellows < 0
                    || f.Player.InjuredUntilWeek < 0 || world.Clubs.All(c => c.Id != f.PreviousClubId)
                    || activeWageDescriptions.Contains($"Player contract {f.Player.ContractId.Value}")))
                throw new InvalidDataException("Invalid free-agent identities or retirement history.");
            if (world.FreeAgentBids.Count > 1 || world.FreeAgentBids.Count + world.Negotiations.Count > 1
                || world.FreeAgentBids.Any(b => b.WeeklyWage <= 0 || b.ExpiryWeek < world.Week || b.ContractEndWeek <= b.ExpiryWeek
                    || world.History.All(h => h.Command.Allocation != Allocation.FreeAgentRecruitment || h.OriginalForecast.Id != b.ForecastId
                        || h.Recruitment is not { IsFreeAgent: true } t || t.Player.Id != b.PlayerId || t.WeeklyWage != b.WeeklyWage
                        || t.ContractEndWeek != b.ContractEndWeek || b.ExpiryWeek != h.Week + 1)))
                throw new InvalidDataException("Invalid free-agent mandate.");
        }
        if (world.SchemaVersion >= 12 && (world.AcademyGraduates.Select(g => g.Player.Id).Distinct().Count() != world.AcademyGraduates.Count
            || world.AcademyGraduates.GroupBy(g => (g.ClubId, g.Week)).Any(g => g.Count() > 2)
            || world.AcademyGraduates.Any(g => g.Week < Seasons.Weeks || g.Week > world.Week || g.Week % Seasons.Weeks != 0
                || world.Clubs.All(c => c.Id != g.ClubId) || g.Player.Id.Value < 100000 || string.IsNullOrWhiteSpace(g.Player.Name)
                || !Enum.IsDefined(g.Player.Role) || g.Player.Age != 17 || g.Player.Ability is < 1 or > 100 || g.Player.WeeklyWage <= 0
                || g.Player.ContractEndWeek != g.Week + Seasons.Weeks * Academy.ContractYears)))
            throw new InvalidDataException("Invalid academy graduation history.");
        if (world.SchemaVersion >= 11 && (world.Departures.Select(d => (d.PlayerId, d.ClubId, d.Week)).Distinct().Count() != world.Departures.Count
            || world.Departures.Any(d => d.PlayerId.Value <= 0 || string.IsNullOrWhiteSpace(d.Name) || string.IsNullOrWhiteSpace(d.Reason)
                || !Enum.IsDefined(d.Role) || d.Age is < 16 or > 100 || d.Ability is < 1 or > 100
                || d.Week < 0 || d.Week > world.Week || world.Clubs.All(c => c.Id != d.ClubId))))
            throw new InvalidDataException("Invalid player departure history.");
        if (world.SchemaVersion >= 10 && (world.Clubs.Any(c => c.TrainingLevel is < 0 or > 3)
            || people.Any(p => p.TrainingExposure < 0 || p.TrainingExposure > 20 * Seasons.WeekInSeason(world.Week, world.Season))
            || world.Projects.Any(p => !Enum.IsDefined(p.Kind))
            || world.SeasonSummaries.Any(s => s.Development.Any(p => p.TrainingBonus is < 0 or > 20))))
            throw new InvalidDataException("Invalid training development state.");
        if (world.SchemaVersion >= 9 && (people.Any(p => p.Age is < 16 or > 100)
            || world.SeasonSummaries.Any(s => s.Development.Select(p => p.PlayerId).Distinct().Count() != s.Development.Length
                || s.Development.Any(p => p.AgeBefore is < 16 or > 99 || p.AbilityBefore is < 1 or > 100
                    || p.AbilityAfter is < 1 or > 100 || p.Appearances < 0 || Math.Abs(p.Change) > 3))))
            throw new InvalidDataException("Invalid player development history.");
        // Rebuild local indexes on every validation; no cached state can outlive a mutation or load.
        var clubIds = world.Clubs.Select(c => c.Id).ToHashSet();
        var fixtures = new Dictionary<FixtureId, Fixture>();
        foreach (var fixture in world.Fixtures)
            if (!fixtures.TryAdd(fixture.Id, fixture)) throw new InvalidDataException("Invalid identities or references.");
        if (people.Select(p => p.Id).Distinct().Count() != people.Length || people.Any(p => p.WeeklyWage < 0 || p.Ability is < 1 or > 100)
            || world.Obligations.Select(o => o.Id).Distinct().Count() != world.Obligations.Count
            || world.Obligations.Any(o => !clubIds.Contains(o.ClubId) || o.StartWeek > o.EndWeek)
            || world.Results.Select(r => r.FixtureId).Distinct().Count() != world.Results.Count
            || world.Results.Any(r => !fixtures.ContainsKey(r.FixtureId))
            || world.Fixtures.Count(f => f.Competition == Competition.League) != 720 * (legacy ? 1 : world.Season)
            || world.Fixtures.Any(f => f.Home == f.Away || !clubIds.Contains(f.Home) || !clubIds.Contains(f.Away)))
            throw new InvalidDataException("Invalid identities or references.");
        if (world.CalendarStartSeason < 1) throw new InvalidDataException("Invalid calendar history.");
        if (people.Any(p => p.InjuredUntilWeek < 0 || p.SuspendedMatches < 0 || p.SeasonYellows < 0)
            || world.Results.Any(r => r.HomeLineup.Length > 11 || r.AwayLineup.Length > 11
                || r.HomeLineup.Concat(r.AwayLineup).Any(a => a.Rating is < 0 or > 100) || r.Events.Any(e => e.Duration < 0)))
            throw new InvalidDataException("Invalid availability or match detail.");
        if (!legacy && !priorCareer && !priorPyramid && (world.CupStartSeason < 1
            || world.Fixtures.Any(f => !Enum.IsDefined(f.Competition) || f.Competition == Competition.Cup && f.CupRound is < 1 or > 6)
            || world.Results.Any(r => fixtures[r.FixtureId].Competition == Competition.Cup
                && r.Winner != r.Home && r.Winner != r.Away)
            || world.Fixtures.Count(f => f.Competition == Competition.Cup) > 47 * Math.Max(0, world.Season - world.CupStartSeason + 1)))
            throw new InvalidDataException("Invalid cup history.");
        if (!legacy && (world.SeasonOpeningCash < 0 || world.SeasonOpeningLedgerSequence < 0 || world.SeasonOpeningLedgerSequence > world.Journal.Count
            || world.SeasonSummaries.Select(s => s.Season).Distinct().Count() != world.SeasonSummaries.Count
            || world.SeasonSummaries.Any(s => s.Season < 1 || s.Season > world.Season || s.EndWeek != s.Season * Seasons.Weeks)
            || (world.Status == CareerStatus.SeasonReview && world.Week != Seasons.EndWeek(world))))
            throw new InvalidDataException("Invalid season boundaries.");
        var balances = world.Clubs.ToDictionary(c => Account(c.Id), _ => 0L, StringComparer.Ordinal);
        balances.Add("owner", 0);
        foreach (var line in world.Journal)
            if (line.Account is not null && balances.TryGetValue(line.Account, out var total))
                balances[line.Account] = checked(total + line.Amount);
        foreach (var club in world.Clubs)
            if (club.Cash < 0 || checked(club.OpeningCash + balances[Account(club.Id)]) != club.Cash)
                throw new InvalidDataException($"Cash journal does not reconcile for {club.Name}.");
        if (checked(world.OpeningOwnerCash + balances["owner"]) != world.OwnerCash)
            throw new InvalidDataException("Personal cash journal does not reconcile.");
    }

    public static string Account(ClubId club) => $"club/{club.Value}";
}
