using System.Security.Cryptography;
using System.Text.Json;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

namespace FootballTycoon.Headless;

public sealed record MandateSwap(Player Starter, Player Young, Role Role, int AbilitySacrifice);
public sealed record CandidateDiagnostic(Player Player, long WeeklyWage, int WageStartWeek, int ContractEndWeek,
    long TotalCommitment, long LowestDownside, bool Feasible);
public sealed record StartDiagnostic(int Week, int Season, int SquadSize, bool VacancyAndCover, bool ActiveAndClear,
    int[] ReadinessIds, MandateSwap[] SelectedSwaps, bool PositiveDevelopmentSwap,
    CandidateDiagnostic[] EligibleCandidates, Proposal CurrentPreview, CandidateDiagnostic? ValueCandidate,
    bool MaterialValueTradeoff, Player[] CurrentRoleStarters, Player[] CurrentRoleDepth, string[] FailedCriteria)
{
    public bool SubstantiveCriteriaPass => FailedCriteria.Length == 0;
}

// E01B diagnostic only: these pure copies of the E00 algorithms never install a gameplay policy.
public static class PlayableStartDiagnostics
{
    public const string Recipe = "E01B-A1-recipe-1";
    public static readonly ulong[] Seeds = [2026, 2027, 2028, 2029, 2030];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    public static MandateSwap[] DevelopmentSwaps(Club club, int week)
    {
        var lineup = Matchday.Select(club, week);
        var selected = lineup.Select(p => p.Player.Id).ToHashSet();
        var pairs = (from starter in lineup
                     where starter.Position != Role.Goalkeeper && starter.Position == starter.Player.Role && starter.Player.Age > 23
                     from young in club.Players
                     where !selected.Contains(young.Id) && young.Age <= 23 && young.Role == starter.Position
                         && Matchday.IsAvailable(young, week) && young.Ability <= starter.Player.Ability
                         && young.Ability >= starter.Player.Ability - 5
                     select new MandateSwap(starter.Player, young, starter.Position, starter.Player.Ability - young.Ability))
            .OrderBy(p => p.AbilitySacrifice).ThenByDescending(p => p.Young.Ability).ThenBy(p => p.Young.Age)
            .ThenBy(p => p.Role).ThenBy(p => p.Young.Id.Value).ThenBy(p => p.Starter.Id.Value);
        var used = new HashSet<PersonId>();
        var chosen = new List<MandateSwap>();
        foreach (var pair in pairs)
        {
            if (used.Contains(pair.Starter.Id) || used.Contains(pair.Young.Id)) continue;
            chosen.Add(pair); used.Add(pair.Starter.Id); used.Add(pair.Young.Id);
            if (chosen.Count == 2) break;
        }
        return chosen.ToArray();
    }

    // Duplicates only the production eligibility/quote rules inaccessible outside Core; tests compare its
    // first candidate and amounts with the real default recommendation and proposal. Reserve is NOT a rank filter.
    public static CandidateDiagnostic[] Candidates(World world)
    {
        if (!FreeAgents.CanReview(world)) return [];
        var club = world.OwnedClub;
        bool NeedsCover(Role role) => FreeAgents.HasShortage(club, role) || club.Players.Count < Contracts.MinimumSquad
            && club.Players.Count(p => p.Role == role) < (role == Role.Goalkeeper ? 2 : role == Role.Forward ? 4 : 6);
        long Wage(Player p) => Math.Max(10000, checked(Math.Max(p.WeeklyWage, Contracts.StandardWage(club.Division) / 2) + 500) / 1000 * 1000);
        var limit = Money.Scale(Finance.EligibleRevenue(world, club.Id), Balance.Load().WageLimit);
        return world.FreeAgents.Select(f => f.Player)
            .Where(p => p.Age < FreeAgents.RetirementAge(p.Role) && FreeAgents.EligibleRole(world, p.Role, world.Week)
                && (NeedsCover(p.Role) || p.Ability > club.Players.Where(s => s.Role == p.Role).Min(s => s.Ability))
                && checked(Finance.AnnualWages(club) + Wage(p) * Seasons.Weeks) <= limit
                && !world.History.Any(h => h.Command.Allocation == Allocation.FreeAgentRecruitment && h.Recruitment?.Player.Id == p.Id && world.Week < h.Week + 4))
            .OrderByDescending(p => FreeAgents.HasShortage(club, p.Role)).ThenByDescending(p => p.Ability)
            .ThenBy(p => p.Age).ThenBy(p => p.Id.Value)
            .Select(p =>
            {
                var wage = Wage(p);
                var end = Seasons.EndWeek(world) + (p.Age <= 24 ? 2 : p.Age <= 29 ? 1 : 0) * Seasons.Weeks;
                var cash = Finance.ProjectCash(world, club.Id, extraWeekly: wage, extraStarts: world.Week + 2, extraEnds: end);
                return new CandidateDiagnostic(p, wage, world.Week + 2, end, checked(wage * (end - world.Week - 1)), cash.LowestDownside,
                    !world.Arrears.Any(a => a.ClubId == club.Id) && cash.LowestDownside >= world.ReserveTarget);
            }).ToArray();
    }

    public static CandidateDiagnostic? ValueAlternative(CandidateDiagnostic[] candidates)
    {
        if (candidates.Length == 0) return null;
        var baseline = candidates[0];
        return candidates.Where(c => c.Player.Id != baseline.Player.Id && c.Player.Role == baseline.Player.Role
            && c.Player.Ability >= baseline.Player.Ability - 5 && c.WeeklyWage <= baseline.WeeklyWage
            && c.TotalCommitment <= baseline.TotalCommitment
            && (c.WeeklyWage < baseline.WeeklyWage || c.TotalCommitment < baseline.TotalCommitment))
            .OrderBy(c => c.TotalCommitment).ThenBy(c => c.WeeklyWage).ThenByDescending(c => c.Player.Ability)
            .ThenBy(c => c.Player.Age).ThenBy(c => c.Player.Id.Value).FirstOrDefault();
    }

    public static StartDiagnostic Inspect(World world)
    {
        var before = WorldCodec.Encode(world);
        var candidates = Candidates(world);
        var preview = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        var baseline = candidates.FirstOrDefault();
        if (baseline?.Player.Id != preview.Recruitment?.Player.Id
            || baseline is not null && (baseline.WeeklyWage != preview.WeeklyCost || baseline.TotalCommitment != preview.TotalCommitment
                || baseline.ContractEndWeek != preview.Recruitment!.ContractEndWeek))
            throw new InvalidDataException("Diagnostic eligibility/quote drifted from production recommendation.");
        var alternate = ValueAlternative(candidates);
        var swaps = DevelopmentSwaps(world.OwnedClub, world.Week + 1);
        var vacancy = world.OwnedClub.Players.Count is >= Contracts.MinimumSquad and < Academy.SquadLimit
            && Contracts.Shortages(world.OwnedClub.Players).IsEmpty;
        var clear = world.Status == CareerStatus.Active && world.Season == 2 && world.Week == 52 && world.AllocationChosen
            && !world.Decisions.Any(d => d.Required && !d.Resolved && d.DueWeek <= world.Week + 1)
            && world.Negotiations.Count == 0 && world.FreeAgentBids.Count == 0 && !world.Arrears.Any(a => a.ClubId == world.OwnedClubId);
        // Decimal avoids overflow for the registered exact integer-pence inequality.
        var tradeoff = alternate is not null && baseline is not null && alternate.Feasible
            && baseline.Player.Ability - alternate.Player.Ability is >= 1 and <= 5
            && ((decimal)alternate.WeeklyWage * 10 <= (decimal)baseline.WeeklyWage * 9
                || (decimal)alternate.TotalCommitment * 10 <= (decimal)baseline.TotalCommitment * 9);
        var failures = new List<string>();
        if (!vacancy) failures.Add("No genuine vacancy with minimum squad/role cover.");
        if (!clear) failures.Add("Not a clear active season-two week-52 checkpoint.");
        if (!swaps.Any(s => s.AbilitySacrifice > 0)) failures.Add("Exact ordered/capped Development swaps contain no positive readiness sacrifice.");
        if (!FreeAgents.CanReview(world) || preview.Recruitment is null || !preview.BlockingReasons.IsEmpty)
            failures.Add("Current recommendation has no unblocked production preview.");
        if (!tradeoff) failures.Add("Exact first Value alternative lacks feasible >=10% saving and positive ability sacrifice.");
        var lineup = Matchday.Select(world.OwnedClub, world.Week + 1);
        var role = baseline?.Player.Role;
        var result = new StartDiagnostic(world.Week, world.Season, world.OwnedClub.Players.Count, vacancy, clear,
            lineup.Select(p => p.Player.Id.Value).ToArray(), swaps, swaps.Any(s => s.AbilitySacrifice > 0), candidates, preview, alternate, tradeoff,
            lineup.Where(p => p.Position == role).Select(p => p.Player).ToArray(),
            world.OwnedClub.Players.Where(p => p.Role == role).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).ToArray(), failures.ToArray());
        if (!before.AsSpan().SequenceEqual(WorldCodec.Encode(world))) throw new InvalidDataException("Suitability diagnostics mutated world/RNG.");
        return result;
    }

    public static bool Prepare(World world, Action<object> record)
    {
        Commit(world, new(Allocation.Acquire), "baseline-Acquire", record);
        Commit(world, new(Allocation.PreserveReserve), "baseline-PreserveReserve", record);
        for (var week = 0; week < Seasons.Weeks; week++)
        {
            BeforeAdvance(world, false, record);
            var before = world.Week;
            var result = Simulation.AdvanceWeek(world);
            record(new { Kind = "prelude-advance", BeforeWeek = before, Result = result });
            if (world.Week != before + 1) return false;
        }
        if (world.Status != CareerStatus.SeasonReview) return false;
        var command = new OwnerCommand(Allocation.StartNextSeason) { DeclineAcademy = true };
        var recommended = Proposals.Preview(world, command, world.Revision);
        record(new { Kind = "renewal-preview", Proposal = recommended });
        var terms = recommended.Renewal!;
        var remaining = world.OwnedClub.Players.Count - terms.Contracts.Count(c => c.Chosen != ContractAction.Renew);
        Proposal? chosen = recommended.BlockingReasons.IsEmpty && terms.Shortages.IsEmpty && remaining is >= 16 and < 22 ? recommended : null;
        if (remaining == Academy.SquadLimit)
        {
            foreach (var candidate in terms.Contracts.Where(c => c.Recommended == ContractAction.Renew)
                .OrderBy(c => c.Ability).ThenByDescending(c => c.Age).ThenBy(c => c.PlayerId.Value))
            {
                var proposal = Proposals.Preview(world, command with { ContractOverrides = [candidate.PlayerId] }, world.Revision);
                record(new { Kind = "release-preview", Proposal = proposal });
                var count = world.OwnedClub.Players.Count - proposal.Renewal!.Contracts.Count(c => c.Chosen != ContractAction.Renew);
                if (proposal.BlockingReasons.IsEmpty && proposal.Renewal.Shortages.IsEmpty && count is >= 16 and < 22) { chosen = proposal; break; }
            }
        }
        if (chosen is null) return false;
        Commit(world, chosen.Command, "e01b-renew-1", record);
        Commit(world, new(Allocation.PreserveReserve), "e01b-plan-2", record);
        return true;
    }

    private static void Commit(World world, OwnerCommand command, string id, Action<object> record)
    {
        var proposal = Proposals.Preview(world, command, world.Revision);
        record(new { Kind = "command", world.Week, CommandId = id, Proposal = proposal });
        Proposals.Commit(world, id, world.Revision, proposal);
    }

    private static void BeforeAdvance(World world, bool initial, Action<object> record)
    {
        if (RecruitmentMarket.Available(world) && (world.Season == 2 || world.Decisions.Any(d => d.Required && !d.Resolved && d.DueWeek <= world.Week + 1))) Commit(world, new(Allocation.MidseasonWait), $"e01b-wait-{world.Season}", record);
        if (!initial && Contracts.Shortages(world.OwnedClub.Players).IsEmpty) return;
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        record(new { Kind = "control-approach-preview", world.Week, Initial = initial, Proposal = quote });
        if (quote.Recruitment is not null && quote.BlockingReasons.IsEmpty)
            Commit(world, quote.Command, $"e01b-cover-{world.Season}-{world.Week}", record);
    }

    public static async Task RunAsync(string output)
    {
        output = Path.GetFullPath(output);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Choose a new empty directory; E01B evidence is never replaced.");
        Directory.CreateDirectory(output);
        Write(output, "recipe.json", new
        {
            Recipe,
            Seeds,
            Contract = "E00 A1",
            PlayableSeasons = Seasons.PlayableSeasons,
            HeadlessSha256 = Hash(File.ReadAllBytes(typeof(PlayableStartDiagnostics).Assembly.Location)),
            CoreSha256 = Hash(File.ReadAllBytes(typeof(World).Assembly.Location)),
            Note = "Recipe frozen in docs/OWNER_MANDATE_BASELINE.md before runs. No mandate treatments or alternate production previews."
        });
        var manifests = new List<object>();
        foreach (var seed in Seeds)
        {
            var directory = Path.Combine(output, seed.ToString()); Directory.CreateDirectory(directory);
            using var trace = new StreamWriter(new FileStream(Path.Combine(directory, "trace.jsonl"), FileMode.CreateNew));
            void Record(object value) { trace.WriteLine(JsonSerializer.Serialize(value)); trace.Flush(); }
            var world = WorldFactory.Create(seed);
            var prepared = Prepare(world, Record);
            var bytes = WorldCodec.Encode(world);
            File.WriteAllBytes(Path.Combine(directory, "checkpoint.json"), bytes);
            var replay = WorldFactory.Create(seed);
            var replayPrepared = Prepare(replay, _ => { });
            if (prepared != replayPrepared || !bytes.AsSpan().SequenceEqual(WorldCodec.Encode(replay)))
                throw new InvalidDataException("Ordinary preparation trace did not replay exactly.");
            if (!prepared)
            {
                var failed = new { Seed = seed, Prepared = false, GameplaySha256 = Hash(bytes), Failure = "Frozen renewal/prelude recipe could not produce a covered vacancy.", ReplayExact = true };
                Write(directory, "manifest.json", failed); manifests.Add(failed); continue;
            }
            var inspection = Inspect(world);
            Write(directory, "suitability.json", inspection);
            var vault = new LocalSaveVault(Path.Combine(directory, "application-vault"));
            var saved = vault.Write(bytes, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "E01B-common-start");
            await using var session = new GameSession(vault, seed);
            await session.LoadAsync(saved.SnapshotId);
            var loadedBytes = await session.ExportCheckpointAsync();
            if (!bytes.AsSpan().SequenceEqual(loadedBytes)) throw new InvalidDataException("Application recovery changed checkpoint bytes.");
            var sample = WorldCodec.Decode(bytes); Simulation.AdvanceWeek(sample);
            await session.AdvanceAsync(AdvanceTarget.Week);
            var advancedBytes = await session.ExportCheckpointAsync();
            if (!WorldCodec.Encode(sample).AsSpan().SequenceEqual(advancedBytes)) throw new InvalidDataException("Recovered application advance diverged.");
            if (!bytes.AsSpan().SequenceEqual(vault.Read(saved.SnapshotId).WorldBytes)) throw new InvalidDataException("Recovery overwrote common source.");
            var control = RunControl(world, Record);
            var controlReplay = WorldCodec.Decode(bytes); RunControl(controlReplay, _ => { });
            if (!WorldCodec.Encode(world).AsSpan().SequenceEqual(WorldCodec.Encode(controlReplay))) throw new InvalidDataException("Observed control replay diverged.");
            Write(directory, "control.json", control);
            File.WriteAllBytes(Path.Combine(directory, "control-final.json"), WorldCodec.Encode(world));
            var manifest = new
            {
                Seed = seed,
                Prepared = true,
                GameplaySha256 = Hash(bytes),
                ReplayExact = true,
                ControlReplayExact = true,
                ApplicationLoadExact = true,
                ApplicationAdvanceExact = true,
                SourcePreserved = true,
                saved.SnapshotId,
                inspection.SubstantiveCriteriaPass,
                inspection.FailedCriteria,
                DesktopRecovery = "Not yet verified",
                Control = control
            };
            Write(directory, "manifest.json", manifest); manifests.Add(manifest);
            Console.WriteLine($"E01B seed {seed}: squad {inspection.SquadSize}; positive swaps {inspection.PositiveDevelopmentSwap}; Value tradeoff {inspection.MaterialValueTradeoff}; substantive pass {inspection.SubstantiveCriteriaPass}");
        }
        Write(output, "manifest.json", manifests);
        Console.WriteLine($"Evidence: {output}");
    }

    private static object RunControl(World world, Action<object> record)
    {
        var start = world.Week;
        var openingSequence = world.Journal.Count;
        var openingHistory = world.History.Count;
        var openingObligations = world.Obligations.Select(o => o.Id).ToHashSet();
        var openingPeople = world.OwnedClub.Players.Select(p => p.Id).ToHashSet();
        var cohort = world.OwnedClub.Players.Where(p => p.Age <= 23).ToArray();
        var openingCash = world.OwnedClub.Cash;
        var minimumCash = openingCash; var arrearsWeeks = 0; var shortageWeeks = 0;
        while (world.Week < 104 && world.Status == CareerStatus.Active)
        {
            BeforeAdvance(world, world.Week == start, record);
            var before = world.Week;
            var nextFixtures = world.Fixtures.Where(f => f.Week == before + 1 && (f.Home == world.OwnedClubId || f.Away == world.OwnedClubId)).ToArray();
            var prospective = new { Week = before + 1, Readiness = Matchday.Select(world.OwnedClub, before + 1).Select(p => new { p.Player, p.Position }).ToArray(), Swaps = DevelopmentSwaps(world.OwnedClub, before + 1) };
            var result = Simulation.AdvanceWeek(world);
            foreach (var fixture in nextFixtures)
                record(new
                {
                    Kind = "control-match",
                    Fixture = fixture,
                    Prospective = prospective,
                    Actual = world.Results.SingleOrDefault(r => r.FixtureId == fixture.Id),
                    Note = "Pre-advance diagnostic; ordinary signing/retirement resolution occurs before selection and can change the actual lineup."
                });
            record(new
            {
                Kind = "control-week",
                Result = result,
                world.OwnedClub.Cash,
                Arrears = world.Arrears.Where(a => a.ClubId == world.OwnedClubId).Sum(a => a.Amount),
                Shortages = Contracts.Shortages(world.OwnedClub.Players)
            });
            if (world.Week != before + 1) break;
            minimumCash = Math.Min(minimumCash, world.OwnedClub.Cash);
            if (world.Arrears.Any(a => a.ClubId == world.OwnedClubId)) arrearsWeeks++;
            if (!Contracts.Shortages(world.OwnedClub.Players).IsEmpty) shortageWeeks++;
        }
        var matches = world.Results.Where(r => r.Week > start && (r.Home == world.OwnedClubId || r.Away == world.OwnedClubId)).ToArray();
        var starts = matches.SelectMany(r => r.Home == world.OwnedClubId ? r.HomeLineup : r.AwayLineup).GroupBy(p => p.Player).ToDictionary(g => g.Key, g => g.Count());
        var summary = world.SeasonSummaries.SingleOrDefault(s => s.Season == 2);
        return new
        {
            Complete = world.Week == 104 && world.Status == CareerStatus.SeasonReview && summary is not null,
            world.Week,
            Status = world.Status.ToString(),
            OpeningCash = openingCash,
            ClosingCash = world.OwnedClub.Cash,
            MinimumCash = minimumCash,
            ArrearsWeeks = arrearsWeeks,
            ShortageWeeks = shortageWeeks,
            MatchCount = matches.Length,
            Table = summary?.FinalTable.Single(t => t.ClubId == world.OwnedClubId),
            summary?.CupResult,
            ClosingAnnualWages = Finance.AnnualWages(world.OwnedClub),
            ActualWagesPaid = -world.Journal.Where(j => j.Sequence > openingSequence && j.Account == WorldFactory.Account(world.OwnedClubId) && j.Kind == CashKind.Wages).Sum(j => j.Amount),
            RemainingWageCommitment = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && o.WeeklyAmount < 0)
                .Sum(o => checked(-o.WeeklyAmount * Math.Max(0, o.EndWeek - Math.Max(world.Week, o.StartWeek - 1)))),
            Approvals = world.History.Skip(openingHistory).Where(h => h.Command.Allocation == Allocation.FreeAgentRecruitment).ToArray(),
            ApproachOutcomes = world.Reviews.Where(r => r.Week > start && r.Title is "Free agent signed" or "Free-agent approach closed").ToArray(),
            NewWageObligations = world.Obligations.Where(o => o.ClubId == world.OwnedClubId && o.Kind == CashKind.Wages && !openingObligations.Contains(o.Id)).ToArray(),
            LaterYoungRecruits = world.OwnedClub.Players.Where(p => !openingPeople.Contains(p.Id) && p.Age - 1 <= 23)
                .Select(p => new { Player = p, Starts = starts.GetValueOrDefault(p.Id), Note = "Age minus one recovers entry age for this ordinary one-season control; full evidence remains in approved terms." }).ToArray(),
            ClosingSquad = world.OwnedClub.Players,
            YoungCohort = cohort.Select(p => new
            {
                Player = p,
                Starts = starts.GetValueOrDefault(p.Id),
                CappedAppearances = Math.Min(25, starts.GetValueOrDefault(p.Id)),
                TwentyAppearanceThreshold = starts.GetValueOrDefault(p.Id) >= 20,
                Development = summary?.Development.SingleOrDefault(d => d.PlayerId == p.Id)
            }).ToArray(),
            FinalSha256 = Hash(WorldCodec.Encode(world))
        };
    }

    private static void Write(string directory, string name, object value)
    {
        using var file = new FileStream(Path.Combine(directory, name), FileMode.CreateNew);
        JsonSerializer.Serialize(file, value, JsonOptions);
    }
}
