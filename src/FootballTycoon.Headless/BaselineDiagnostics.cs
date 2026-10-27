using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

namespace FootballTycoon.Headless;

public sealed record DevelopmentPair(int StarterId, int YoungId, Role Role, int AbilitySacrifice);
public sealed record SelectionObservation(int Week, int[] ReadinessIds, DevelopmentPair[] Pairs);

// E01 only: observes the unchanged production rules; never installs a policy or edits a scenario.
public static class BaselineDiagnostics
{
    private static readonly Allocation[] Commands = [Allocation.Acquire, Allocation.PreserveReserve];
    public static SelectionObservation ObserveSelection(Club club, int week)
    {
        var lineup = Matchday.Select(club, week);
        var selected = lineup.Select(x => x.Player.Id).ToHashSet();
        var pairs = (from starter in lineup
                     where starter.Position != Role.Goalkeeper && starter.Player.Role == starter.Position && starter.Player.Age > 23
                     from young in club.Players
                     where !selected.Contains(young.Id) && young.Age <= 23 && young.Role == starter.Position
                         && Matchday.IsAvailable(young, week) && young.Ability <= starter.Player.Ability
                         && young.Ability >= starter.Player.Ability - 5
                     select new DevelopmentPair(starter.Player.Id.Value, young.Id.Value, young.Role, starter.Player.Ability - young.Ability))
                    .OrderBy(p => p.StarterId).ThenBy(p => p.YoungId).ToArray();
        return new(week, lineup.Select(x => x.Player.Id.Value).ToArray(), pairs);
    }

    public static void CommitOpening(World world)
    {
        foreach (var allocation in Commands)
        {
            var proposal = Proposals.Preview(world, new(allocation), world.Revision);
            Proposals.Commit(world, $"baseline-{allocation}", world.Revision, proposal);
        }
    }

    public static async Task RunAsync(ulong seed, string output)
    {
        output = Path.GetFullPath(output);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Choose a new empty directory; baseline evidence is never replaced.");
        Directory.CreateDirectory(output);
        using var writer = new StreamWriter(new FileStream(Path.Combine(output, "observations.jsonl"), FileMode.CreateNew));
        void Record(object value) { writer.WriteLine(JsonSerializer.Serialize(value)); writer.Flush(); }
        void WriteBytes(string name, byte[] bytes)
        {
            using var file = new FileStream(Path.Combine(output, name), FileMode.CreateNew);
            file.Write(bytes);
        }
        string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
        var world = WorldFactory.Create(seed);
        var assembly = typeof(BaselineDiagnostics).Assembly;
        Record(new
        {
            Kind = "configuration",
            Seed = seed,
            world.SchemaVersion,
            world.SimulationVersion,
            Runtime = Environment.Version.ToString(),
            ProductionPlayableSeasons = Seasons.PlayableSeasons,
            Build = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            AssemblySha256 = Hash(File.ReadAllBytes(assembly.Location)),
            CoreAssemblySha256 = Hash(File.ReadAllBytes(typeof(World).Assembly.Location)),
            Commands = Commands.Select(a => $"baseline-{a}: {a}"),
            Policy = "Acquire at asking price, PreserveReserve; preview but never approve optional paid recruitment; no free-agent approvals, injections, renewals or rollover. Stop at week 52 SeasonReview."
        });
        CommitOpening(world);
        var openingPlayers = world.OwnedClub.Players.ToArray();
        var cohort = openingPlayers.Where(p => p.Age <= 23).ToArray();
        var openingContracts = openingPlayers.Select(p => p.ContractId).ToHashSet();
        var openingObligations = world.Obligations.Select(o => o.Id).ToHashSet();
        Record(new { Kind = "opening-squad", Players = openingPlayers });
        WriteBytes("opening.json", WorldCodec.Encode(world));
        long minimumCash = world.OwnedClub.Cash;
        var shortageWeeks = 0; var arrearsWeeks = 0; var matchOpportunities = 0; var positiveCostOpportunities = 0;
        var reviewWeeks = 0; var recommendationWeeks = 0; var sampledWeeks = 0; var quoteCount = 0;
        var weekTimings = new List<double>();
        byte[]? weekFour = null;
        for (var week = 0; week < Seasons.Weeks; week++)
        {
            var canReview = FreeAgents.CanReview(world);
            var recommendation = FreeAgents.Recommend(world);
            sampledWeeks++;
            if (canReview) reviewWeeks++;
            if (recommendation is not null) recommendationWeeks++;
            // A closed review gate proves zero actionable candidates; an open gate is explicitly unmeasured.
            Record(new
            {
                Kind = "recruitment-opportunity",
                world.Week,
                CanReview = canReview,
                ActionableCandidateCount = canReview ? (int?)null : 0,
                Recommendation = recommendation
            });
            if (RecruitmentMarket.Available(world))
                foreach (var allocation in new[] { Allocation.MidseasonRecruitment, Allocation.MidseasonValue })
                {
                    var quote = Proposals.Preview(world, new(allocation), world.Revision);
                    quoteCount++;
                    Record(new
                    {
                        Kind = "paid-quote-only",
                        world.Week,
                        Allocation = allocation.ToString(),
                        quote.Recruitment,
                        quote.TotalCommitment,
                        quote.BlockingReasons
                    });
                }
            var fixtures = world.Fixtures.Where(f => f.Week == world.Week + 1 && (f.Home == world.OwnedClubId || f.Away == world.OwnedClubId)).ToArray();
            var selection = ObserveSelection(world.OwnedClub, world.Week + 1);
            var watch = Stopwatch.StartNew();
            var advanced = Simulation.AdvanceWeek(world);
            weekTimings.Add(watch.Elapsed.TotalMilliseconds);
            if (advanced.Week != week + 1) throw new InvalidDataException($"Baseline stopped unexpectedly: {advanced.StopReason}");
            foreach (var fixture in fixtures)
            {
                var result = world.Results.Single(r => r.FixtureId == fixture.Id);
                var actual = result.Home == world.OwnedClubId ? result.HomeLineup : result.AwayLineup;
                if (!actual.Select(a => a.Player.Value).SequenceEqual(selection.ReadinessIds))
                    throw new InvalidDataException("Pre-week selection was not the played lineup; opportunity evidence requires a closer observation point.");
                if (selection.Pairs.Length > 0) matchOpportunities++;
                if (selection.Pairs.Any(p => p.AbilitySacrifice > 0)) positiveCostOpportunities++;
                Record(new { Kind = "match-opportunity", Fixture = fixture, Selection = selection, Actual = actual });
            }
            minimumCash = Math.Min(minimumCash, world.OwnedClub.Cash);
            var shortages = Contracts.Shortages(world.OwnedClub.Players);
            var arrears = world.Arrears.Where(a => a.ClubId == world.OwnedClubId).Sum(a => a.Amount);
            if (!shortages.IsEmpty) shortageWeeks++;
            if (arrears > 0) arrearsWeeks++;
            Record(new { Kind = "week", world.Week, world.OwnedClub.Cash, Arrears = arrears, Shortages = shortages });
            if (world.Week == 4) weekFour = WorldCodec.Encode(world);
        }
        if (world.Status != CareerStatus.SeasonReview || world.Season != 1 || world.SeasonSummaries.Count != 1)
            throw new InvalidDataException("Expected exactly one completed season and no renewal.");
        var checkpoint = WorldCodec.Encode(world);
        WriteBytes("season-one.json", checkpoint);
        if (!checkpoint.AsSpan().SequenceEqual(WorldCodec.Encode(WorldCodec.Decode(checkpoint))))
            throw new InvalidDataException("Season-one codec replay changed bytes.");
        // Independent unobserved replay demonstrates the observer, quotes and encoding have no simulation effect.
        var replay = WorldFactory.Create(seed);
        CommitOpening(replay);
        for (var week = 0; week < Seasons.Weeks; week++) Simulation.AdvanceWeek(replay);
        if (!checkpoint.AsSpan().SequenceEqual(WorldCodec.Encode(replay))) throw new InvalidDataException("Unobserved replay diverged.");
        var matches = world.Results.Where(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId).ToArray();
        var starts = matches.SelectMany(r => r.Home == world.OwnedClubId ? r.HomeLineup : r.AwayLineup).GroupBy(a => a.Player).ToDictionary(g => g.Key, g => g.Count());
        var season = world.SeasonSummaries.Single();
        var summary = new
        {
            Seed = seed,
            world.Week,
            Status = world.Status.ToString(),
            Season = world.Season,
            GameplaySha256 = Hash(checkpoint),
            ReplayExact = true,
            CodecExact = true,
            OpeningCash = season.OpeningCash,
            ClosingCash = world.OwnedClub.Cash,
            MinimumCash = minimumCash,
            ShortageWeeks = shortageWeeks,
            ArrearsWeeks = arrearsWeeks,
            Table = season.FinalTable.Single(t => t.ClubId == world.OwnedClubId),
            season.CupResult,
            MatchCount = matches.Length,
            MatchOpportunities = matchOpportunities,
            PositiveAbilityCostMatchOpportunities = positiveCostOpportunities,
            RecruitmentSampleWeeks = sampledWeeks,
            FreeAgentReviewWeeks = reviewWeeks,
            FreeAgentRecommendationWeeks = recommendationWeeks,
            MultipleEligibleFreeAgentWeeks = reviewWeeks == 0 ? (int?)0 : null,
            PaidQuotes = quoteCount,
            RecruitmentApprovals = world.History.Count(h => h.Recruitment is not null),
            NewContracts = world.OwnedClub.Players.Count(p => !openingContracts.Contains(p.ContractId)),
            NewWageObligations = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && !openingObligations.Contains(o.Id)).ToArray(),
            ActualWagesPaid = -world.Journal.Where(l => l.Account == WorldFactory.Account(world.OwnedClubId) && l.Kind == CashKind.Wages).Sum(l => l.Amount),
            RemainingWageCommitment = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && o.WeeklyAmount < 0)
                .Sum(o => checked(-o.WeeklyAmount * Math.Max(0, o.EndWeek - Math.Max(world.Week, o.StartWeek - 1)))),
            OpeningAnnualWages = openingPlayers.Sum(p => p.WeeklyWage) * Seasons.Weeks,
            ClosingAnnualWages = Finance.AnnualWages(world.OwnedClub),
            ClosingSquad = world.OwnedClub.Players,
            YoungCohort = cohort.Select(p => new
            {
                Player = p,
                Starts = starts.GetValueOrDefault(p.Id),
                CappedAppearances = Math.Min(25, starts.GetValueOrDefault(p.Id)),
                TwentyAppearanceThreshold = starts.GetValueOrDefault(p.Id) >= 20,
                Development = season.Development.Single(d => d.PlayerId == p.Id)
            }).ToArray(),
            CoreAdvanceMilliseconds = weekTimings,
            CoreAdvanceTotalMilliseconds = weekTimings.Sum()
        };
        WriteBytes("summary.json", JsonSerializer.SerializeToUtf8Bytes(summary, new JsonSerializerOptions { WriteIndented = true }));
        var app = await ApplicationSample(seed, Path.Combine(output, "application-saves"), weekFour!);
        WriteBytes("application-sample.json", JsonSerializer.SerializeToUtf8Bytes(app, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"E01 seed {seed}: week {world.Week}, {world.Status}; exact replay; {matches.Length} matches; {matchOpportunities} selection opportunities; {reviewWeeks} free-agent review weeks. Evidence: {output}");
    }

    private static async Task<object> ApplicationSample(ulong seed, string directory, byte[] expectedWeekFour)
    {
        var times = new List<object>();
        var watch = Stopwatch.StartNew();
        await using var session = new GameSession(new LocalSaveVault(directory), seed);
        times.Add(new { Operation = "construct", Milliseconds = watch.Elapsed.TotalMilliseconds });
        foreach (var allocation in Commands)
        {
            watch.Restart(); var view = await session.QueryAsync();
            times.Add(new { Operation = $"query-{allocation}", Milliseconds = watch.Elapsed.TotalMilliseconds });
            watch.Restart(); var proposal = await session.PreviewAsync(new(allocation), view.Revision);
            times.Add(new { Operation = $"preview-{allocation}", Milliseconds = watch.Elapsed.TotalMilliseconds });
            watch.Restart(); await session.CommitAsync($"baseline-{allocation}", view.Revision, proposal);
            times.Add(new { Operation = $"commit-{allocation}", Milliseconds = watch.Elapsed.TotalMilliseconds });
        }
        for (var week = 1; week <= 4; week++)
        {
            watch.Restart(); await session.AdvanceAsync(AdvanceTarget.Week);
            times.Add(new { Operation = $"durable-advance-{week}", Milliseconds = watch.Elapsed.TotalMilliseconds });
        }
        var actual = await session.ExportCheckpointAsync();
        if (!actual.AsSpan().SequenceEqual(expectedWeekFour)) throw new InvalidDataException("Application and Core differ at week four.");
        var saved = (await session.ListSavesAsync()).First(s => s.Week == 4);
        watch.Restart(); await session.LoadAsync(saved.SnapshotId);
        times.Add(new { Operation = "load-week-four", Milliseconds = watch.Elapsed.TotalMilliseconds });
        var reloaded = await session.ExportCheckpointAsync();
        if (!actual.AsSpan().SequenceEqual(reloaded)) throw new InvalidDataException("Application reload changed bytes.");
        return new
        {
            Weeks = 4,
            CoreEquivalent = true,
            ReloadExact = true,
            Measurements = times,
            SnapshotCount = Directory.EnumerateFiles(directory, "*.ftsave").Count(),
            Note = "One local cold/warm mixed sample; commit includes recovery and durable publication, advances include clone/validation/serialization/write/verification. No full-season application timing or performance percentile claim."
        };
    }
}
