using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FootballTycoon.Core;

public readonly record struct ClubId(int Value);
public readonly record struct PersonId(int Value);
public readonly record struct ObligationId(int Value);
public readonly record struct FixtureId(int Value);
public readonly record struct DecisionId(int Value);
public readonly record struct ProjectId(int Value);
public readonly record struct ContractId(int Value);

public static class Money
{
    public static long Add(long a, long b) => checked(a + b);
    public static long Scale(long amount, decimal factor) => checked((long)decimal.Round(amount * factor, 0, MidpointRounding.AwayFromZero));
    public static string Format(long amount) => (amount / 100m).ToString("C0", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}

public enum CareerStatus { Acquisition, Active, Administration, LostControl, PrototypeComplete, SeasonReview }
public enum Phase { Decisions, Payments, Negotiation, Matches, Development, Reporting }
public enum Allocation { Acquire, PreserveReserve, Hospitality, Recruitment, InjectCapital, StartNextSeason, MidseasonRecruitment, MidseasonValue, MidseasonWait }
public enum AdvanceTarget { Week, Month, NextDecision }
public enum Role { Goalkeeper, Defender, Midfielder, Forward }
public enum Competition { League, Cup }
public enum CashKind { Purchase, Injection, Wages, Operations, Broadcast, Sponsorship, Commercial, Tickets, Hospitality, Transfer, Construction, Rescue, Prize }

public sealed record OwnerCommand(Allocation Allocation, long Amount = 0, bool ReserveException = false);
public sealed record LedgerEntry(int Sequence, int Week, string Account, long Amount, CashKind Kind, string Reference);
public sealed record Obligation(ObligationId Id, ClubId ClubId, int StartWeek, int EndWeek, long WeeklyAmount, CashKind Kind, string Description);
public sealed record Arrear(ObligationId ObligationId, ClubId ClubId, int Week, long Amount);
public sealed record Player(PersonId Id, string Name, Role Role, int Ability, int Age, long WeeklyWage, ContractId ContractId, int ContractEndWeek)
{
    // Availability (schema 6). Defaults mean fit and eligible, so they are omitted from saves.
    // Unavailable for fixtures in career weeks before InjuredUntilWeek.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int InjuredUntilWeek { get; init; }
    // Remaining competitive matches of the player's club to miss.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int SuspendedMatches { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int SeasonYellows { get; init; }
}
public sealed record Fixture(FixtureId Id, int Week, ClubId Home, ClubId Away)
{
    public int Division { get; init; }
    public Competition Competition { get; init; }
    public int CupRound { get; init; }
}
public sealed record Moment(int Minute, ClubId ClubId, PersonId ScorerId, string Text)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public PersonId? AssistId { get; init; }
}
public enum MatchEventKind { Yellow, SecondYellow, Red, Injury }
// Rating is in tenths (68 = 6.8). Position is the slot played, which can differ from the player's role.
public sealed record Appearance(PersonId Player, Role Position, int Rating);
// Duration: weeks out for an injury; matches banned for a dismissal or a caution that completes a set of five.
public sealed record MatchEvent(int Minute, ClubId ClubId, PersonId Player, MatchEventKind Kind, int Duration);
public sealed record MatchResult(FixtureId FixtureId, int Week, ClubId Home, ClubId Away, int HomeGoals, int AwayGoals,
    int HomeShots, int AwayShots, int Attendance, long Receipts, ImmutableArray<Moment> Moments)
{
    public ClubId Winner { get; init; }
    public bool ExtraTime { get; init; }
    public string? Shootout { get; init; }
    // Empty for matches played before schema 6.
    public ImmutableArray<Appearance> HomeLineup { get; init; } = [];
    public ImmutableArray<Appearance> AwayLineup { get; init; } = [];
    public ImmutableArray<MatchEvent> Events { get; init; } = [];
    public PersonId? PlayerOfMatch { get; init; }
}
public sealed record Project(ProjectId Id, ClubId ClubId, int StartedWeek, int CompletionWeek, long Cost, long RecoverableCash, string ForecastId);
public sealed record Negotiation(int DecisionId, ClubId Seller, PersonId PlayerId, int ExpiryWeek, long FeeCeiling, long WeeklyWage, string ForecastId);
public sealed record Decision(DecisionId Id, int DueWeek, bool Required, string Title, bool Resolved = false);
public sealed record ForecastPoint(int Week, long BaseCash, long DownsideCash, long KnownNet);
public sealed record Forecast(string Id, int CreatedWeek, int HorizonWeeks, ImmutableArray<ForecastPoint> Points,
    long LowestBase, int LowestBaseWeek, long LowestDownside, int LowestDownsideWeek, string Assumptions);
public sealed record DecisionRecord(int Week, OwnerCommand Command, string Executive, string Intent, Forecast OriginalForecast)
{
    public RecruitmentTerms? Recruitment { get; init; }
}
public sealed record Review(int Week, string Title, string Evidence, string? ForecastId);
public sealed record CommitReceipt(string CommandId, string ProposalId, long Revision, string Summary);
public sealed record SeasonSummary(int Season, int EndWeek, long OpeningCash, long ClosingCash,
    long OperatingNet, long OwnerInjections, ImmutableArray<TableRow> FinalTable, string Plan)
{
    public int Division { get; init; }
    public string CupResult { get; init; } = "Not entered";
    public long CupPrize { get; init; }
}
public sealed record RenewalTerms(int NextSeason, int PlayerContracts, long RenewedAnnualWages,
    long AnnualBroadcast, long AnnualSponsor, long AnnualOperations, long Arrears)
{
    public int CurrentDivision { get; init; }
    public int NextDivision { get; init; }
    public long CurrentAnnualBroadcast { get; init; }
    public long CurrentAnnualSponsor { get; init; }
}
public sealed record Proposal(string Id, long Revision, OwnerCommand Command, string Title, string Executive,
    long UpfrontCash, long WeeklyCost, long TotalCommitment, int ReviewWeek, Forecast Forecast,
    ImmutableArray<string> BlockingReasons, string Uncertainty)
{
    public RenewalTerms? Renewal { get; init; }
    public RecruitmentTerms? Recruitment { get; init; }
}

public sealed record RecruitmentTerms(Allocation Allocation, Player Player, string Seller, long FeeCeiling,
    long WeeklyWage, int ContractEndWeek, decimal CurrentForwardAbility);

// Mutable aggregate is owned only by the application worker. Screens receive separate immutable records.
public sealed class Club
{
    public ClubId Id { get; set; }
    public string Name { get; set; } = "";
    public int Division { get; set; }
    public int OpeningDivision { get; set; }
    public long Cash { get; set; }
    public long OpeningCash { get; set; }
    public long AnnualBroadcast { get; set; }
    public long AnnualSponsor { get; set; }
    public long HistoricalTickets { get; set; }
    public long HistoricalHospitality { get; set; }
    public long HistoricalCommercial { get; set; }
    public int Support { get; set; } = 60;
    public int HospitalityLevel { get; set; }
    public int Lot { get; set; }
    public List<Player> Players { get; set; } = [];
}

public sealed class World
{
    public int SchemaVersion { get; set; } = 6;
    public string SimulationVersion { get; set; } = "matchday-6";
    public string ContentVersion { get; set; } = "prototype-1";
    public int RandomVersion { get; set; } = 1;
    public long Revision { get; set; }
    public ulong Seed { get; set; }
    public int Week { get; set; }
    public int Season { get; set; } = 1;
    public int CupStartSeason { get; set; } = 1;
    public long SeasonOpeningCash { get; set; }
    public int SeasonOpeningLedgerSequence { get; set; }
    public List<SeasonSummary> SeasonSummaries { get; set; } = [];
    public Phase Phase { get; set; }
    public CareerStatus Status { get; set; }
    public ClubId OwnedClubId { get; set; }
    public long OwnerCash { get; set; }
    public long OpeningOwnerCash { get; set; }
    public long OwnerInvested { get; set; }
    public long ReserveTarget { get; set; }
    public int? AdministrationWeek { get; set; }
    public bool AllocationChosen { get; set; }
    public List<Club> Clubs { get; set; } = [];
    public List<Obligation> Obligations { get; set; } = [];
    public List<Arrear> Arrears { get; set; } = [];
    public List<Fixture> Fixtures { get; set; } = [];
    public List<MatchResult> Results { get; set; } = [];
    public List<LedgerEntry> Journal { get; set; } = [];
    public List<Project> Projects { get; set; } = [];
    public List<Negotiation> Negotiations { get; set; } = [];
    public List<Decision> Decisions { get; set; } = [];
    public List<DecisionRecord> History { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    public Dictionary<string, ulong> RandomStates { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, CommitReceipt> Commands { get; set; } = new(StringComparer.Ordinal);
    [System.Text.Json.Serialization.JsonIgnore]
    public Club OwnedClub => Clubs.Single(c => c.Id == OwnedClubId);
}

public static class WorldCodec
{
    public static byte[] Encode(World world) => JsonSerializer.SerializeToUtf8Bytes(world);
    public static World Decode(byte[] bytes)
    {
        var world = JsonSerializer.Deserialize<World>(bytes) ?? throw new InvalidDataException("Empty world.");
        var closeLegacySeason = world.SchemaVersion == 1 && world.Status == CareerStatus.PrototypeComplete;
        if (world.SchemaVersion == 1)
        {
            WorldFactory.Validate(world, legacy: true);
            world.SchemaVersion = 2;
            world.SimulationVersion = "career-2";
            world.Season = 1;
            world.SeasonOpeningCash = world.OwnedClub.OpeningCash;
            world.SeasonOpeningLedgerSequence = 0;
            world.SeasonSummaries = [];
            if (world.Status == CareerStatus.PrototypeComplete)
            {
                world.Status = CareerStatus.SeasonReview;
            }
        }
        if (world.SchemaVersion == 2)
        {
            WorldFactory.Validate(world, priorCareer: true);
            foreach (var club in world.Clubs) club.OpeningDivision = club.Division;
            world.Fixtures = world.Fixtures.Select(f => f with { Division = world.Clubs.Single(c => c.Id == f.Home).Division }).ToList();
            // Legacy fixtures need their division tags before rebuilding the completed table.
            if (closeLegacySeason) Seasons.Close(world);
            world.SeasonSummaries = world.SeasonSummaries.Select(s => s with { Division = world.OwnedClub.Division }).ToList();
            world.SchemaVersion = 3; world.SimulationVersion = "pyramid-3";
        }
        if (world.SchemaVersion == 3)
        {
            WorldFactory.Validate(world, priorPyramid: true);
            world.SeasonSummaries = world.SeasonSummaries.Select(s => s with { CupResult = "Not held (legacy season)", CupPrize = 0 }).ToList();
            world.SchemaVersion = 4; world.SimulationVersion = "competition-4";
            world.CupStartSeason = world.Week == Seasons.StartWeek(world) && world.Status != CareerStatus.PrototypeComplete
                ? world.Season : world.Season + 1;
            if (world.CupStartSeason == world.Season) Cups.AddOpeningRound(world);
        }
        if (world.SchemaVersion == 4)
        {
            WorldFactory.Validate(world, priorCompetition: true);
            world.SchemaVersion = 5; world.SimulationVersion = "market-5";
        }
        if (world.SchemaVersion == 5)
        {
            WorldFactory.Validate(world, priorMarket: true);
            // Earlier matches keep empty line-ups, ratings and events; every player starts fit and eligible.
            foreach (var club in world.Clubs)
                club.Players = club.Players.Select(p => p with { InjuredUntilWeek = 0, SuspendedMatches = 0, SeasonYellows = 0 }).ToList();
            world.SchemaVersion = 6; world.SimulationVersion = "matchday-6";
        }
        WorldFactory.Validate(world);
        return world;
    }
    public static World Clone(World world) => Decode(Encode(world));
}
