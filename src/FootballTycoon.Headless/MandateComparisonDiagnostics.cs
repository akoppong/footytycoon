using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

namespace FootballTycoon.Headless;

// E08 observation only. Each arm starts from the published E01B bytes, never a manufactured scenario.
public static class MandateComparisonDiagnostics
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private static readonly JsonSerializerOptions LineJson = new() { Converters = { new JsonStringEnumConverter() } };
    private static readonly (ulong Seed, string Hash, bool Eligible)[] Starts =
    [
        (2026, "DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D", true),
        (2027, "B567C5FDE0B3715FEEC3A1599EEBE32C3B3A21280FA7DD51729AB74841A02C09", false),
        (2028, "19015200CD04951C6424673FB603193E6865250E5BE32D4BE8BC7C968A2F9410", false),
        (2029, "37D88AB137EB1B29783EF7CC17452A4FF914D13E86F19C17F37B7CE8290682F7", false),
        (2030, "97BDF6C558EC3B240EA755FCC09CC5F09BA4D3B1413B09FB751818FF4A167283", true)
    ];
    private static readonly (string Id, ManagerMandate Manager, DirectorMandate Director)[] Arms =
    [
        ("RC", ManagerMandate.Readiness, DirectorMandate.CurrentRecommendation),
        ("DC", ManagerMandate.Development, DirectorMandate.CurrentRecommendation),
        ("RV", ManagerMandate.Readiness, DirectorMandate.LowerCostValue),
        ("DV", ManagerMandate.Development, DirectorMandate.LowerCostValue)
    ];
    private sealed record WeekObservation(int Week, long Cash, long ReserveTarget, long Arrears, string[] Shortages, double CoreAdvanceMilliseconds);

    public static Task RunAsync(string sourceDirectory, string outputDirectory)
    {
        var source = Path.GetFullPath(sourceDirectory);
        var output = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Choose a new empty output directory; comparison evidence is never replaced.");
        Directory.CreateDirectory(output);
        // Validate the entire preregistered set before running any treatment, including excluded seeds.
        var starts = Starts.Select(s =>
        {
            var path = Path.Combine(source, s.Seed.ToString(), "checkpoint.json");
            var bytes = File.ReadAllBytes(path);
            Require(Hash(bytes) == s.Hash, $"Seed {s.Seed}: published source hash changed.");
            var manifest = JsonNode.Parse(File.ReadAllBytes(Path.Combine(source, s.Seed.ToString(), "manifest.json")))!;
            Require(manifest["SubstantiveCriteriaPass"]!.GetValue<bool>() == s.Eligible
                && manifest["GameplaySha256"]!.GetValue<string>() == s.Hash, "E01B manifest disagrees with registration.");
            var failed = manifest["FailedCriteria"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();
            return (s.Seed, s.Hash, s.Eligible, Path: path, Bytes: bytes, Failed: failed);
        }).ToArray();
        Write(output, "manifest.json", new
        {
            Protocol = "E00 amendment A1; E01B run-3; fixed 2x2, weeks 52 through 104",
            SourceDirectory = source,
            AssemblyVersion = typeof(MandateComparisonDiagnostics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            AssemblySha256 = Hash(File.ReadAllBytes(typeof(MandateComparisonDiagnostics).Assembly.Location)),
            CoreAssemblySha256 = Hash(File.ReadAllBytes(typeof(World).Assembly.Location)),
            Runtime = Environment.Version.ToString(),
            Seeds = starts.Select(s => new { s.Seed, SourceSha256 = s.Hash, s.Eligible, FailedCriteria = s.Failed, SourcePath = s.Path }),
            Arms = Arms.Select(a => new { Arm = a.Id, a.Manager, a.Director }),
            Policy = "Initial ordinary free-agent preview; subsequently once per week only Contracts.Shortages. Approve only unblocked, no reserve exception. Midseason wait. Freeze mandates; no season-three renewal.",
            MoneyUnit = "integer pence",
            Note = "E01B eligibility exclusions are fixed before treatment. Desktop validation is reported separately; original manifests are provenance, not fresh recovery checks."
        });
        var allComplete = true;
        foreach (var start in starts.Where(s => s.Eligible))
        {
            var seedDirectory = Path.Combine(output, start.Seed.ToString());
            Directory.CreateDirectory(seedDirectory);
            var world = WorldCodec.Decode(start.Bytes);
            Require(world.Seed == start.Seed && world.Week == 52 && world.Season == 2
                && world.Status == CareerStatus.Active && world.AllocationChosen && world.Mandates == MandateState.Default,
                "Unexpected common checkpoint state.");
            var migrated = WorldCodec.Encode(world);
            var originalNode = JsonNode.Parse(start.Bytes)!;
            var migratedNode = JsonNode.Parse(migrated)!;
            foreach (var field in new[] { "History", "Commands", "RandomStates", "Negotiations", "FreeAgentBids" })
                Require(JsonNode.DeepEquals(originalNode[field], migratedNode[field]), $"Migration changed imported {field}.");
            WriteBytes(seedDirectory, "migrated-start.json", migrated);
            foreach (var arm in Arms)
                allComplete &= RunArm(start.Seed, arm.Id, arm.Manager, arm.Director, migrated, start.Hash, seedDirectory);
            Require(Hash(File.ReadAllBytes(start.Path)) == start.Hash, "Original checkpoint was modified.");
        }
        Write(output, "completion.json", new { Complete = allComplete, ExpectedArms = 8 });
        Require(allComplete, "One or more arms failed. Partial worlds, traces and summaries were retained; do not infer success.");
        return Task.CompletedTask;
    }

    private static bool RunArm(ulong seed, string arm, ManagerMandate manager, DirectorMandate director,
        byte[] initial, string sourceHash, string seedDirectory)
    {
        var directory = Path.Combine(seedDirectory, arm);
        Directory.CreateDirectory(directory);
        using var trace = new StreamWriter(new FileStream(Path.Combine(directory, "trace.jsonl"), FileMode.CreateNew));
        void Record(object value) { trace.WriteLine(JsonSerializer.Serialize(value, LineJson)); trace.Flush(); }
        var world = WorldCodec.Decode(initial);
        var replay = WorldCodec.Decode(initial);
        var openingSquad = world.OwnedClub.Players.ToArray();
        var openingPeople = openingSquad.Select(p => p.Id).ToHashSet();
        var cohort = openingSquad.Where(p => p.Age <= 23).ToArray();
        var cohortIds = cohort.Select(p => p.Id).ToHashSet();
        var openingCash = world.OwnedClub.Cash;
        var ownerCash = world.OwnerCash; var invested = world.OwnerInvested;
        var openingHistory = world.History.ToArray();
        var openingCommands = world.Commands.ToDictionary();
        var openingObligations = world.Obligations.Select(o => o.Id).ToHashSet();
        var weeks = new List<WeekObservation>();
        Proposal? initialPreview = null;
        RecommendationEvidence? initialRecommendation = null;
        string? failure = null;
        var replayExact = false; var codecExact = false; var middleReloadExact = false;
        try
        {
            void SetMandates(World target, Action<object> record)
            {
                if (manager == ManagerMandate.Readiness && director == DirectorMandate.CurrentRecommendation) return;
                var before = WorldCodec.Encode(target);
                var proposal = MandateCommands.Preview(target, new($"e08-{seed}-{arm}-mandates", target.Revision, manager, director));
                Equal(before, WorldCodec.Encode(target), "Mandate preview mutated world.");
                record(new { Kind = "mandate-preview", target.Week, Proposal = proposal });
                var receipt = MandateCommands.Commit(target, proposal);
                record(new { Kind = "mandate-commit", target.Week, Receipt = receipt });
            }
            SetMandates(world, Record); SetMandates(replay, _ => { });
            while (world.Week < 104)
            {
                var before = world.Week;
                Require(world.Status == CareerStatus.Active, $"Stopped before weekly advance: {world.Status}.");
                BeforeAdvance(world, seed, arm, Record, out var quote, out var recommendation);
                BeforeAdvance(replay, seed, arm, _ => { }, out _, out _);
                if (before == 52) { initialPreview = quote; initialRecommendation = recommendation; }
                var pure = WorldCodec.Encode(world);
                _ = OwnerMandates.View(world);
                _ = Matchday.Select(world);
                Equal(pure, WorldCodec.Encode(world), "Observation changed gameplay bytes.");
                Equal(pure, WorldCodec.Encode(replay), "Same commands diverged before advance.");
                var watch = Stopwatch.StartNew();
                var advanced = Simulation.AdvanceWeek(world);
                watch.Stop();
                Simulation.AdvanceWeek(replay);
                Require(world.Week == before + 1, $"Advance stopped: {advanced.StopReason}.");
                WorldFactory.Validate(world); // Includes exact financial reconciliation and saved-evidence links.
                Require(world.OwnerCash == ownerCash && world.OwnerInvested == invested, "Owner authority/capital changed.");
                Require(world.Mandates.Manager == manager && world.Mandates.Director == director, "Frozen mandate changed.");
                Require(world.History.Skip(openingHistory.Length).All(h => !h.Command.ReserveException
                    && h.Command.Allocation is Allocation.FreeAgentRecruitment or Allocation.MidseasonWait), "Unexpected owner command.");
                Require(openingCommands.All(c => world.Commands.TryGetValue(c.Key, out var receipt) && receipt == c.Value), "Imported receipt changed.");
                Require(JsonSerializer.Serialize(world.History.Take(openingHistory.Length)) == JsonSerializer.Serialize(openingHistory), "Imported history changed.");
                var newWages = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && !openingObligations.Contains(o.Id));
                Require(newWages.All(o => world.MandateApproaches.Any(a => a.Outcome is { Outcome: ApproachOutcome.Signed } outcome
                    && outcome.WageObligationId == o.Id)), "New owned wages lack an executed approved approach.");
                var observation = new WeekObservation(world.Week, world.OwnedClub.Cash, world.ReserveTarget,
                    world.Arrears.Where(a => a.ClubId == world.OwnedClubId).Sum(a => a.Amount),
                    Contracts.Shortages(world.OwnedClub.Players).ToArray(), watch.Elapsed.TotalMilliseconds);
                weeks.Add(observation);
                Record(new
                {
                    Kind = "week",
                    Observation = observation,
                    Advance = advanced,
                    Approaches = world.MandateApproaches,
                    Selections = world.MandateSelections.Where(s => world.Fixtures.Single(f => f.Id == s.FixtureId).Week == world.Week)
                });
                if (world.Week == 78)
                {
                    var middle = WorldCodec.Encode(replay);
                    var vault = new LocalSaveVault(Path.Combine(directory, "middle-vault"));
                    var saved = vault.Write(middle, "00000000000000000000000000000008", "00000000000000000000000000000001", null, "Registered middle-season replay");
                    replay = WorldCodec.Decode(vault.Read(saved.SnapshotId).WorldBytes);
                    Equal(middle, WorldCodec.Encode(replay), "Middle save/load changed bytes.");
                    middleReloadExact = true;
                    Record(new { Kind = "middle-reload", saved.SnapshotId, Week = replay.Week, Sha256 = Hash(middle) });
                }
                Equal(WorldCodec.Encode(world), WorldCodec.Encode(replay), "Observed/uninterrupted versus reload replay diverged.");
            }
            Require(world.Status == CareerStatus.SeasonReview && world.Season == 2
                && world.SeasonSummaries.Count(s => s.Season == 2) == 1, "Missing full season and closing review.");
            replayExact = middleReloadExact;
            var final = WorldCodec.Encode(world);
            Equal(final, WorldCodec.Encode(WorldCodec.Decode(final)), "Final codec changed bytes.");
            codecExact = true;
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Record(new { Kind = "failure", world.Week, Error = failure });
        }

        var bytes = WorldCodec.Encode(world);
        WriteBytes(directory, "final-world.json", bytes);
        var matches = world.Results.Where(r => r.Week > 52 && (r.Home == world.OwnedClubId || r.Away == world.OwnedClubId)).ToArray();
        var starts = matches.SelectMany(r => r.Home == world.OwnedClubId ? r.HomeLineup : r.AwayLineup)
            .GroupBy(p => p.Player).ToDictionary(g => g.Key, g => g.Count());
        var season = world.SeasonSummaries.SingleOrDefault(s => s.Season == 2);
        var table = season?.FinalTable.Single(t => t.ClubId == world.OwnedClubId);
        object Exposure(Player p)
        {
            var development = season?.Development.SingleOrDefault(d => d.PlayerId == p.Id);
            return new
            {
                PersonId = p.Id.Value,
                p.Name,
                p.Role,
                p.Age,
                p.Ability,
                Starts = starts.GetValueOrDefault(p.Id),
                Development = development,
                CappedAppearances = development is null ? (int?)null : Math.Min(25, development.Appearances),
                TwentyAppearanceThreshold = development is null ? (bool?)null : development.Appearances >= 20,
                AgeEligible = p.Age <= 23,
                BelowAbilityCeiling = p.Ability < 100,
                AbilityCeiling = 100
            };
        }
        var complete = failure is null && replayExact && codecExact;
        Write(directory, "summary.json", new
        {
            Seed = seed,
            Arm = arm,
            Complete = complete,
            Failure = failure,
            world.Week,
            world.Status,
            world.Season,
            SourceSha256 = sourceHash,
            InitialSha256 = Hash(initial),
            FinalSha256 = Hash(bytes),
            ReplayExact = replayExact,
            CodecExact = codecExact,
            MiddleReloadExact = middleReloadExact,
            Guards = new { PreviewPurity = complete, CashReconciliation = complete, OwnerAuthority = complete, ImportedIdentities = complete, SwapBounds = complete },
            OpeningCash = openingCash,
            ClosingCash = world.OwnedClub.Cash,
            MinimumCash = weeks.Select(w => w.Cash).Append(openingCash).Min(),
            world.ReserveTarget,
            ArrearsWeeks = weeks.Count(w => w.Arrears > 0),
            ShortageWeeks = weeks.Count(w => w.Shortages.Length > 0),
            ClosingArrears = world.Arrears.Where(a => a.ClubId == world.OwnedClubId).Sum(a => a.Amount),
            ClosingShortages = Contracts.Shortages(world.OwnedClub.Players),
            Points = table?.Points,
            Position = season is null ? (int?)null : Array.FindIndex(season.FinalTable.ToArray(), t => t.ClubId == world.OwnedClubId) + 1,
            Table = table,
            CupResult = season?.CupResult,
            MatchCount = matches.Length,
            ActualWagesPaid = -world.Journal.Where(j => j.Week > 52 && j.Account == WorldFactory.Account(world.OwnedClubId) && j.Kind == CashKind.Wages).Sum(j => j.Amount),
            ClosingAnnualWages = Finance.AnnualWages(world.OwnedClub),
            RemainingWageCommitment = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && o.WeeklyAmount < 0)
                .Sum(o => checked(-o.WeeklyAmount * Math.Max(0, o.EndWeek - Math.Max(world.Week, o.StartWeek - 1)))),
            OpeningSquad = openingSquad,
            ClosingSquad = world.OwnedClub.Players,
            OpeningObligationIds = openingObligations.Select(o => o.Value).Order().ToArray(),
            NewWageObligations = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && !openingObligations.Contains(o.Id)),
            YoungCohort = cohort.Select(Exposure),
            YoungCohortStarts = cohort.Sum(p => starts.GetValueOrDefault(p.Id)),
            LaterYoungRecruits = world.History.Skip(openingHistory.Length).Where(h => h.Recruitment is { Player.Age: <= 23 }
                    && !openingPeople.Contains(h.Recruitment.Player.Id)
                    && world.MandateApproaches.Any(a => a.ForecastId == h.OriginalForecast.Id && a.Outcome?.Outcome == ApproachOutcome.Signed))
                .Select(h => Exposure(h.Recruitment!.Player)),
            InitialRecommendation = initialRecommendation,
            InitialPreview = initialPreview,
            Approaches = world.MandateApproaches,
            Approvals = world.History.Skip(openingHistory.Length),
            DevelopmentSwaps = world.MandateSelections.Sum(s => s.Swaps.Length),
            Fixtures = matches.Select(m =>
            {
                var fixture = world.Fixtures.Single(f => f.Id == m.FixtureId);
                var lineup = m.Home == world.OwnedClubId ? m.HomeLineup : m.AwayLineup;
                var evidence = world.MandateSelections.SingleOrDefault(s => s.FixtureId == m.FixtureId);
                return new
                {
                    FixtureId = fixture.Id.Value,
                    fixture.Week,
                    Season = 2,
                    fixture.Competition,
                    fixture.CupRound,
                    Home = fixture.Home.Value,
                    Away = fixture.Away.Value,
                    m.HomeGoals,
                    m.AwayGoals,
                    OwnedStarters = lineup.Select(p => p.Player.Value),
                    CohortStarts = lineup.Count(p => cohortIds.Contains(p.Player)),
                    Swaps = evidence?.Swaps,
                    Evidence = evidence
                };
            }),
            Weeks = weeks
        });
        Console.WriteLine($"E08 {seed}/{arm}: week {world.Week}, complete={complete}, exact replay={replayExact}; {directory}");
        return complete;
    }

    private static void BeforeAdvance(World world, ulong seed, string arm, Action<object> record,
        out Proposal? quote, out RecommendationEvidence? recommendation)
    {
        void Approve(OwnerCommand command, string suffix)
        {
            var before = WorldCodec.Encode(world);
            var proposal = Proposals.Preview(world, command, world.Revision);
            Equal(before, WorldCodec.Encode(world), "Owner preview mutated world.");
            record(new { Kind = "owner-preview", world.Week, Proposal = proposal });
            var receipt = Proposals.Commit(world, $"e08-{seed}-{arm}-{world.Week}-{suffix}", world.Revision, proposal);
            record(new { Kind = "owner-commit", world.Week, Receipt = receipt });
        }
        if (RecruitmentMarket.Available(world)) Approve(new(Allocation.MidseasonWait), "wait");
        quote = null; recommendation = null;
        var shortages = Contracts.Shortages(world.OwnedClub.Players);
        if (world.Week != 52 && shortages.IsEmpty)
        {
            record(new { Kind = "cover-policy", world.Week, Attempted = false, Shortages = shortages });
            return;
        }
        var bytes = WorldCodec.Encode(world);
        recommendation = FreeAgents.RecommendWithEvidence(world)?.Evidence;
        quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Equal(bytes, WorldCodec.Encode(world), "Recommendation/preview mutated world.");
        record(new { Kind = "recruitment-preview", world.Week, Recommendation = recommendation, Proposal = quote });
        if (quote.Recruitment is not null && quote.BlockingReasons.IsEmpty)
        {
            var receipt = Proposals.Commit(world, $"e08-{seed}-{arm}-{world.Week}-cover", world.Revision, quote);
            record(new { Kind = "recruitment-commit", world.Week, Receipt = receipt });
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    private static void Equal(byte[] expected, byte[] actual, string message) => Require(expected.AsSpan().SequenceEqual(actual), message);
    private static void Write(string directory, string name, object value) => WriteBytes(directory, name, JsonSerializer.SerializeToUtf8Bytes(value, Json));
    private static void WriteBytes(string directory, string name, byte[] bytes)
    {
        using var file = new FileStream(Path.Combine(directory, name), FileMode.CreateNew);
        file.Write(bytes);
    }
}
