using System.Collections.Immutable;
using System.Threading.Channels;
using FootballTycoon.Core;

namespace FootballTycoon.Application;

public sealed record CheckpointInfo(string SnapshotId, string? ParentId, string BranchId, string CareerId, string Slot, int Week, DateTimeOffset CreatedAt);
public sealed record LoadedCheckpoint(CheckpointInfo Info, byte[] WorldBytes);
public sealed record CheckpointPage(ImmutableArray<CheckpointInfo> Entries, int? NextOffset);
public interface ISaveVault
{
    CheckpointInfo Write(byte[] world, string careerId, string branchId, string? parentId, string slot);
    LoadedCheckpoint Read(string snapshotId);
    ImmutableArray<CheckpointInfo> List();
    CheckpointPage ListPage(int offset, int limit);
}
public sealed record SyncStatus(bool Available, string Message);
public interface IPlatformAdapter
{
    Task<SyncStatus> SynchronizeAsync(CancellationToken cancellationToken = default);
}
public sealed record CashLine(int Week, CashKind Kind, long Amount, string Reference) { public long Sequence { get; init; } }
public sealed record ClubSummary(ClubId Id, string Name, int Division, long Cash);
public sealed record GameView(long Revision, int Week, CareerStatus Status, string Club, ClubId ClubId,
    long ClubCash, long PersonalReserve, long OwnerInvested, long ReserveTarget, long EligibleRevenue, long AnnualWages,
    long Arrears, Forecast Forecast, ImmutableArray<TableRow> Table, ImmutableArray<Player> Squad,
    ImmutableArray<MatchResult> Results, ImmutableArray<Review> Reviews, ImmutableArray<DecisionRecord> History,
    ImmutableArray<Decision> Decisions, ImmutableArray<string> OpenQuestions, ImmutableArray<CashLine> CashLines,
    ImmutableArray<Obligation> Obligations, ImmutableArray<ClubSummary> Clubs, bool AllocationChosen)
{
    public int Division { get; init; }
    public int Season { get; init; } = 1;
    public int SeasonStartWeek => (Season - 1) * Seasons.Weeks;
    public int SeasonEndWeek => Season * Seasons.Weeks;
    public int SeasonWeek => Seasons.WeekInSeason(Week, Season);
    public long SeasonOpeningCash { get; init; }
    public long SeasonOpeningLedgerSequence { get; init; }
    public ImmutableArray<SeasonSummary> SeasonSummaries { get; init; } = [];
    public ImmutableArray<Fixture> Fixtures { get; init; } = [];
    public string CupStatus { get; init; } = "Not entered";
    public long CupPrize { get; init; }
    public string MarketStatus { get; init; } = "";
    public bool MarketAvailable { get; init; }
    public ImmutableArray<RecruitmentTerms> RecruitmentOptions { get; init; } = [];
    // Same order as Squad; status is for the coming week's fixtures.
    public ImmutableArray<PlayerAvailability> SquadAvailability { get; init; } = [];
    // Every current player, so match reports can name both line-ups.
    public ImmutableDictionary<PersonId, string> PlayerNames { get; init; } = ImmutableDictionary<PersonId, string>.Empty;
}

// All reads, commands, checkpoints and loads share one queue. No engine state escapes to the UI.
public sealed class GameSession : IAsyncDisposable
{
    private readonly Channel<Action> queue = Channel.CreateUnbounded<Action>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Task worker;
    private readonly ISaveVault vault;
    private World world;
    private string careerId = Guid.NewGuid().ToString("N");
    private string branchId = Guid.NewGuid().ToString("N");
    private string? parentId;
    private bool branchOnWrite;

    public GameSession(ISaveVault vault, ulong seed = 2026)
    {
        this.vault = vault;
        world = WorldFactory.Create(seed);
        worker = Task.Run(async () => { await foreach (var job in queue.Reader.ReadAllAsync()) job(); });
    }

    private Task<T> Enqueue<T>(Func<T> action)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.Writer.TryWrite(() => { try { completion.SetResult(action()); } catch (Exception e) { completion.SetException(e); } }))
            completion.SetException(new ObjectDisposedException(nameof(GameSession)));
        return completion.Task;
    }

    public Task<Proposal> PreviewAsync(OwnerCommand command, long revision) => Enqueue(() => Proposals.Preview(world, command, revision));
    public Task<CommitReceipt> CommitAsync(string commandId, long revision, Proposal proposal) => Enqueue(() =>
    {
        if (world.Commands.TryGetValue(commandId, out var previous)) return previous;
        var candidate = WorldCodec.Clone(world);
        var result = Proposals.Commit(candidate, commandId, revision, proposal);
        SaveCurrent("recovery");
        Persist(candidate, "commit");
        world = candidate;
        return result;
    });

    public Task<AdvanceResult> AdvanceAsync(AdvanceTarget target) => Enqueue(() =>
    {
        var limit = target == AdvanceTarget.Week ? world.Week + 1 : target == AdvanceTarget.Month ? Math.Min(Seasons.EndWeek(world), (world.Week / 4 + 1) * 4) : Seasons.EndWeek(world);
        var result = new AdvanceResult(world.Week, world.Status.ToString(), world.Revision);
        while (world.Week < limit)
        {
            var candidate = WorldCodec.Clone(world);
            result = Simulation.AdvanceWeek(candidate);
            if (candidate.Revision == world.Revision) break;
            // Publish each completed week only after its autosave succeeds. An I/O failure leaves the last saved week live.
            Persist(candidate, $"auto-{candidate.Week % 3}");
            world = candidate;
            if (world.Status != CareerStatus.Active || (target == AdvanceTarget.NextDecision && result.StopReason == "Monthly review")) break;
        }
        return result;
    });

    public Task<CheckpointInfo> SaveAsync(string name = "manual") => Enqueue(() => SaveCurrent(name));
    public Task<ImmutableArray<CheckpointInfo>> ListSavesAsync() => Enqueue(vault.List);
    public Task<CheckpointPage> ListSavePageAsync(int offset = 0) => Enqueue(() => vault.ListPage(offset, 20));
    public Task LoadAsync(string snapshotId) => Enqueue(() =>
    {
        var checkpoint = vault.Read(snapshotId);
        var candidate = WorldCodec.Decode(checkpoint.WorldBytes);
        world = candidate; careerId = checkpoint.Info.CareerId; branchId = checkpoint.Info.BranchId;
        parentId = checkpoint.Info.SnapshotId; branchOnWrite = true;
        return true;
    });
    public Task<byte[]> ExportCheckpointAsync() => Enqueue(() => WorldCodec.Encode(world));
    public Task<GameView> QueryAsync() => Enqueue(() =>
    {
        var club = world.OwnedClub;
        var forecast = Finance.Forecast(world, club.Id);
        var questions = ImmutableArray.CreateBuilder<string>();
        if (RecruitmentMarket.Available(world)) questions.Add("Back a forward for the run-in or keep the squad? Review Football before the midseason window closes.");
        if (world.Status == CareerStatus.SeasonReview) questions.Add("Are the next season’s renewal terms affordable?");
        if (world.Decisions.Any(d => !d.Resolved)) questions.Add("Which capital plan should receive this season’s limited funds?");
        if (world.Negotiations.Count > 0) questions.Add("Will Jonas find a forward within the approved ceiling?");
        if (world.Projects.Any(p => p.CompletionWeek > world.Week)) questions.Add("Will hospitality demand justify the cash committed to the expansion?");
        if (world.Arrears.Any(a => a.ClubId == club.Id)) questions.Add("Can personal reserves clear the overdue obligations within four weeks?");
        else if (forecast.LowestDownside < world.ReserveTarget) questions.Add("Can the club protect its reserve through the lowest forecast week?");
        return new GameView(world.Revision, world.Week, world.Status, club.Name, club.Id, club.Cash, world.OwnerCash,
            world.OwnerInvested, world.ReserveTarget, Finance.EligibleRevenue(world, club.Id), Finance.AnnualWages(club),
            world.Arrears.Where(a => a.ClubId == club.Id).Sum(a => a.Amount), forecast, Simulation.Table(world, club.Division),
            club.Players.ToImmutableArray(), world.Results.Where(r => r.Week > Seasons.StartWeek(world) && (r.Home == club.Id || r.Away == club.Id)).ToImmutableArray(),
            world.Reviews.ToImmutableArray(), world.History.ToImmutableArray(), world.Decisions.Where(d => !d.Resolved).ToImmutableArray(),
            questions.Take(3).ToImmutableArray(), world.Journal.Where(j => j.Account == WorldFactory.Account(club.Id))
                .Select(j => new CashLine(j.Week, j.Kind, j.Amount, j.Reference) { Sequence = j.Sequence }).ToImmutableArray(),
            world.Obligations.Where(o => o.ClubId == club.Id).ToImmutableArray(),
            world.Clubs.Select(c => new ClubSummary(c.Id, c.Name, c.Division, c.Cash)).ToImmutableArray(), world.AllocationChosen)
        {
            Division = club.Division, Season = world.Season, SeasonOpeningCash = world.SeasonOpeningCash,
            SeasonOpeningLedgerSequence = world.SeasonOpeningLedgerSequence,
            SeasonSummaries = world.SeasonSummaries.ToImmutableArray(),
            Fixtures = world.Fixtures.Where(f => f.Week > Seasons.StartWeek(world) && (f.Home == club.Id || f.Away == club.Id)).ToImmutableArray(),
            CupStatus = Cups.Status(world, club.Id), CupPrize = Cups.PrizeToDate(world, club.Id),
            MarketStatus = RecruitmentMarket.Status(world), MarketAvailable = RecruitmentMarket.Available(world),
            RecruitmentOptions = RecruitmentMarket.Options(world),
            SquadAvailability = club.Players.Select(p => Matchday.Status(p, world.Week + 1)).ToImmutableArray(),
            PlayerNames = world.Clubs.SelectMany(c => c.Players).ToImmutableDictionary(p => p.Id, p => p.Name)
        };
    });

    private CheckpointInfo SaveCurrent(string slot) => Persist(world, slot);
    private CheckpointInfo Persist(World candidate, string slot)
    {
        var nextBranch = branchOnWrite ? Guid.NewGuid().ToString("N") : branchId;
        var checkpoint = vault.Write(WorldCodec.Encode(candidate), careerId, nextBranch, parentId, slot);
        parentId = checkpoint.SnapshotId; branchId = nextBranch; branchOnWrite = false;
        return checkpoint;
    }

    public async ValueTask DisposeAsync()
    {
        queue.Writer.TryComplete();
        await worker.ConfigureAwait(false);
    }
}
