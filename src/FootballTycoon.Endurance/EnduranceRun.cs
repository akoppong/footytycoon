using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using FootballTycoon.Core;

namespace FootballTycoon.Endurance;

public sealed record RunOptions(int Seasons, ulong Seed, Allocation Strategy, TimeSpan TimeLimit)
{
    public void Validate()
    {
        if (Seasons is < 1 or > 50 || !Core.Seasons.IsCapitalPlan(Strategy) || TimeLimit <= TimeSpan.Zero || TimeLimit > TimeSpan.FromDays(1))
            throw new ArgumentException("Choose 1..50 seasons, a capital-plan strategy, and a positive time limit up to one day.");
    }
}

public sealed record ClubSample(int Id, int Division, int Players, int Goalkeepers, int Defenders, int Midfielders, int Forwards,
    long Cash, long Arrears, long AnnualWages, long EligibleRevenue, bool BelowCover);
public sealed record PopulationSample(string Kind, int Season, int Week, string Status, int Contracted, int Available, int Active,
    int Graduated, int Retired, int Departures, int RivalSignings, int OwnedSignings, long OwnerReserve, ClubSample[] Clubs,
    CoverageReport Coverage);
public sealed record RunResult(string Outcome, string Detail, RunOptions Options, int CompletedSeasons, int Week,
    int MinimumActive, int MaximumActive, int WeeksOutsidePopulationTarget, int ClubWeeksBelowCover,
    double ElapsedSeconds, long MaximumWeekMilliseconds, long CheckpointBytes, string? GameplaySha256,
    Dictionary<string, DiagnosticTimings.Measurement> Timings);

public static class EnduranceRun
{
    // Full simulation, ordinary owner commands, no direct cash changes or invented replacement people.
    // Annual output is observational. Every week still executes the core's validation and an identity accounting check.
    public static RunResult Execute(RunOptions options, TextWriter metrics, TextWriter progress)
    {
        options.Validate();
        DiagnosticTimings.Reset();
        var timer = Stopwatch.StartNew();
        var world = WorldFactory.Create(options.Seed);
        var openingIds = world.Clubs.SelectMany(c => c.Players).Select(p => p.Id).ToHashSet();
        var minimum = int.MaxValue; var maximum = 0; var outside = 0; var shortWeeks = 0; long maximumWeek = 0;
        var outcome = "Incomplete"; var detail = ""; var completed = 0;
        var assembly = typeof(EnduranceRun).Assembly;
        metrics.WriteLine(JsonSerializer.Serialize(new
        {
            Kind = "configuration",
            Options = options,
            DiagnosticOnly = true,
            SimulationVersion = world.SimulationVersion,
            Runtime = Environment.Version.ToString(),
            BuildVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
            OwnedClubId = world.OwnedClubId.Value,
            Policy = "Selected opening strategy, then preserve reserve; accept recommended renewals/intake unless blocked, then decline intake; no owner free-agent approaches or injections. No autosave, pruning, rescue or silent calendar advance."
        }));
        void Observe(bool weekly)
        {
            VerifyPeople(world, openingIds);
            var active = world.Clubs.Sum(c => c.Players.Count) + world.FreeAgents.Count;
            minimum = Math.Min(minimum, active); maximum = Math.Max(maximum, active);
            if (weekly)
            {
                if (active is < 1100 or > 1500) outside++;
                shortWeeks += world.Clubs.Count(c => !Contracts.Shortages(c.Players).IsEmpty);
            }
        }
        void Write(string kind)
        {
            Observe(false);
            metrics.WriteLine(JsonSerializer.Serialize(Sample(world, kind)));
        }
        void Commit(OwnerCommand command)
        {
            var proposal = Proposals.Preview(world, command, world.Revision);
            if (command.Allocation == Allocation.StartNextSeason && !proposal.BlockingReasons.IsEmpty && proposal.Renewal is { AcademyIntake.IsEmpty: false })
                proposal = Proposals.Preview(world, command with { DeclineAcademy = true }, world.Revision);
            if (!proposal.BlockingReasons.IsEmpty) throw new InvalidOperationException("Owner command blocked: " + string.Join("; ", proposal.BlockingReasons));
            Proposals.Commit(world, $"endurance/{world.Season}/{world.Week}/{world.History.Count}/{command.Allocation}", world.Revision, proposal);
        }
        try
        {
            Write("opening");
            Commit(new(Allocation.Acquire)); Commit(new(options.Strategy));
            while (world.Week < options.Seasons * Core.Seasons.Weeks)
            {
                if (timer.Elapsed >= options.TimeLimit) { detail = "Time limit reached at a completed weekly boundary; evidence is partial."; break; }
                if (world.Status == CareerStatus.SeasonReview)
                {
                    Commit(new(Allocation.StartNextSeason));
                    if (world.Status == CareerStatus.Active) Commit(new(Allocation.PreserveReserve));
                    Write("renewal");
                }
                var before = world.Week; var watch = Stopwatch.StartNew();
                var advance = Simulation.AdvanceWeek(world);
                maximumWeek = Math.Max(maximumWeek, watch.ElapsedMilliseconds);
                if (world.Week == before) { detail = "Simulation stopped: " + advance.StopReason; break; }
                Observe(true);
                if (world.Week % Core.Seasons.Weeks == 0)
                {
                    completed = world.SeasonSummaries.Count;
                    Write("season-close");
                    // Exercise serialization without changing, pruning or replacing the running world.
                    var bytes = WorldCodec.Encode(world);
                    if (!bytes.AsSpan().SequenceEqual(WorldCodec.Encode(WorldCodec.Decode(bytes))))
                        throw new InvalidDataException("Annual codec round trip changed the world.");
                    progress.WriteLine($"Season {world.Season}: active {world.Clubs.Sum(c => c.Players.Count) + world.FreeAgents.Count}; retired {world.Retirements.Count}; elapsed {timer.Elapsed.TotalSeconds:F1}s");
                }
                else if (world.Week % 13 == 0) progress.WriteLine($"Week {world.Week}: {timer.Elapsed.TotalSeconds:F1}s");
            }
            if (world.Week == options.Seasons * Core.Seasons.Weeks && completed == options.Seasons)
            { outcome = "Completed"; detail = "Requested horizon completed. This is not a population, economy, save-durability or release acceptance pass."; }
            Write("final");
        }
        catch (Exception error)
        {
            outcome = "Failed"; detail = error.ToString();
            metrics.WriteLine(JsonSerializer.Serialize(new { Kind = "failure", world.Season, world.Week, Error = detail }));
        }
        var checkpoint = WorldCodec.Encode(world);
        return new(outcome, detail, options, completed, world.Week, minimum, maximum, outside, shortWeeks,
            timer.Elapsed.TotalSeconds, maximumWeek, checkpoint.LongLength, Convert.ToHexString(SHA256.HashData(checkpoint)), DiagnosticTimings.Snapshot());
    }

    public static void VerifyPeople(World world, IReadOnlySet<PersonId> openingIds)
    {
        var expected = openingIds.Concat(world.AcademyGraduates.Select(g => g.Player.Id)).ToArray();
        var current = world.Clubs.SelectMany(c => c.Players).Select(p => p.Id)
            .Concat(world.FreeAgents.Select(f => f.Player.Id)).Concat(world.Retirements.Select(r => r.PlayerId)).ToArray();
        if (expected.Distinct().Count() != expected.Length || current.Distinct().Count() != current.Length || !expected.ToHashSet().SetEquals(current))
            throw new InvalidDataException("Population identity accounting failed: every opening player and graduate must be contracted, available or retired exactly once.");
        if (world.Clubs.SelectMany(c => c.Players).Any(p => p.ContractEndWeek < world.Week))
            throw new InvalidDataException("A contracted player has an overdue wage term.");
    }

    public static PopulationSample Sample(World world, string kind)
    {
        var clubs = world.Clubs.OrderBy(c => c.Id.Value).Select(c => new ClubSample(c.Id.Value, c.Division, c.Players.Count,
            c.Players.Count(p => p.Role == Role.Goalkeeper), c.Players.Count(p => p.Role == Role.Defender),
            c.Players.Count(p => p.Role == Role.Midfielder), c.Players.Count(p => p.Role == Role.Forward),
            c.Cash, world.Arrears.Where(a => a.ClubId == c.Id).Sum(a => a.Amount), Finance.AnnualWages(c), Finance.EligibleRevenue(world, c.Id),
            !Contracts.Shortages(c.Players).IsEmpty)).ToArray();
        var contracted = clubs.Sum(c => c.Players);
        return new(kind, world.Season, world.Week, world.Status.ToString(), contracted, world.FreeAgents.Count, contracted + world.FreeAgents.Count,
            world.AcademyGraduates.Count, world.Retirements.Count, world.Departures.Count,
            world.RivalApproaches.Count(a => a.Outcome == ApproachOutcome.Signed),
            world.Reviews.Count(r => r.Title is "Forward signed" or "Free agent signed"), world.OwnerCash, clubs, CoverageDiagnostics.Sample(world));
    }
}
