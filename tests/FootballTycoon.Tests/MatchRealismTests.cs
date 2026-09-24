using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class MatchRealismTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    // One full season (720 league and 47 cup matches) shared by the statistical tests.
    private static readonly Lazy<World> Season = new(() => SeasonTests.EndFirstSeason());

    private static IEnumerable<(MatchResult Match, ClubId Club, Appearance Appearance)> Appearances(World world) =>
        world.Results.SelectMany(r => r.HomeLineup.Select(a => (r, r.Home, a)).Concat(r.AwayLineup.Select(a => (r, r.Away, a))));

    [Fact]
    public void SeasonRatesMatchLowerLeagueFootball()
    {
        var world = Season.Value; var matches = world.Results.Count;
        var events = world.Results.SelectMany(r => r.Events).ToArray();
        var goals = world.Results.Sum(r => r.HomeGoals + r.AwayGoals) / (double)matches;
        var yellows = events.Count(e => e.Kind == MatchEventKind.Yellow) / (double)matches;
        var reds = events.Count(e => e.Kind is MatchEventKind.Red or MatchEventKind.SecondYellow) / (double)matches;
        var injuries = events.Where(e => e.Kind == MatchEventKind.Injury).ToArray();
        Assert.InRange(goals, 2.45, 2.85);
        Assert.InRange(yellows, 3.4, 4.1);
        Assert.InRange(reds, 0.08, 0.18);
        Assert.InRange(injuries.Length / (2.0 * matches), 0.2, 0.4);
        Assert.All(injuries, e => Assert.InRange(e.Duration, 1, 26));
        Assert.InRange(injuries.Count(e => e.Duration <= 4) / (double)injuries.Length, 0.75, 0.95);
        Assert.Contains(injuries, e => e.Duration > 8);
        Assert.InRange(injuries.Count(e => e.Duration > 8) / (double)injuries.Length, 0.005, 0.08);
    }

    [Fact]
    public void ScorersFavourForwardsThenMidfieldersThenDefenders()
    {
        var world = Season.Value;
        var positions = Appearances(world).ToDictionary(x => (x.Match.FixtureId, x.Appearance.Player), x => x.Appearance.Position);
        var scorers = world.Results.SelectMany(r => r.Moments.Select(m => positions[(r.FixtureId, m.ScorerId)])).ToArray();
        double Share(Role role) => scorers.Count(p => p == role) / (double)scorers.Length;
        Assert.InRange(Share(Role.Forward), 0.52, 0.64);
        Assert.InRange(Share(Role.Midfielder), 0.22, 0.33);
        Assert.InRange(Share(Role.Defender), 0.08, 0.17);
        Assert.InRange(Share(Role.Goalkeeper), 0, 0.005);
        var assisted = world.Results.SelectMany(r => r.Moments).Where(m => m.AssistId is not null).ToArray();
        Assert.InRange(assisted.Length / (double)scorers.Length, 0.6, 0.8);
        Assert.All(assisted, m => Assert.NotEqual(m.ScorerId, m.AssistId));
    }

    [Fact]
    public void ReportsAgreeWithTheirLineupsRatingsAndEvents()
    {
        var world = Season.Value;
        Assert.All(world.Results, r =>
        {
            Assert.Equal(11, r.HomeLineup.Length); Assert.Equal(11, r.AwayLineup.Length);
            Assert.Equal(1, r.HomeLineup.Count(a => a.Position == Role.Goalkeeper));
            var lineups = r.HomeLineup.Select(a => (r.Home, a.Player)).Concat(r.AwayLineup.Select(a => (r.Away, a.Player))).ToHashSet();
            Assert.Equal(22, lineups.Count);
            Assert.All(r.HomeLineup.Concat(r.AwayLineup), a => Assert.InRange(a.Rating, 50, 100));
            Assert.Contains(r.HomeLineup.Concat(r.AwayLineup), a => a.Player == r.PlayerOfMatch);
            Assert.Equal(r.HomeLineup.Concat(r.AwayLineup).Max(a => a.Rating), r.HomeLineup.Concat(r.AwayLineup).Single(a => a.Player == r.PlayerOfMatch).Rating);
            Assert.All(r.Moments, m => Assert.Contains((m.ClubId, m.ScorerId), lineups));
            Assert.All(r.Moments.Where(m => m.AssistId is not null), m => Assert.Contains((m.ClubId, m.AssistId!.Value), lineups));
            Assert.All(r.Events, e => { Assert.Contains((e.ClubId, e.Player), lineups); Assert.InRange(e.Minute, 1, 90); });
            // Nobody scores, assists or is booked after leaving the pitch.
            foreach (var exit in r.Events.Where(e => e.Kind is not MatchEventKind.Yellow))
            {
                Assert.DoesNotContain(r.Moments, m => m.Minute > exit.Minute && (m.ScorerId == exit.Player || m.AssistId == exit.Player));
                Assert.DoesNotContain(r.Events, e => e.Minute > exit.Minute && e.Player == exit.Player);
            }
            Assert.True(r.Events.Where(e => e.Kind != MatchEventKind.Injury).GroupBy(e => e.Player).All(g => g.Count() <= 2));
        });
    }

    [Fact]
    public void BansAndInjuriesKeepPlayersOutOfTheirClubsFollowingMatches()
    {
        var world = Season.Value;
        var clubMatches = world.Clubs.ToDictionary(c => c.Id, c => world.Results.Where(r => r.Home == c.Id || r.Away == c.Id).OrderBy(r => r.Week).ToArray());
        bool Played(MatchResult r, PersonId player) => r.HomeLineup.Concat(r.AwayLineup).Any(a => a.Player == player);
        var checkedBans = 0; var checkedInjuries = 0;
        foreach (var match in world.Results)
            foreach (var e in match.Events.Where(e => e.Duration > 0))
            {
                var later = clubMatches[e.ClubId].Where(r => r.Week > match.Week).ToArray();
                if (e.Kind == MatchEventKind.Injury)
                {
                    var missed = later.Where(r => r.Week <= match.Week + e.Duration).ToArray();
                    Assert.All(missed, r => Assert.False(Played(r, e.Player)));
                    checkedInjuries += missed.Length;
                }
                else
                {
                    var missed = later.Take(e.Duration).ToArray();
                    Assert.All(missed, r => Assert.False(Played(r, e.Player)));
                    checkedBans += missed.Length;
                }
            }
        Assert.True(checkedBans > 20 && checkedInjuries > 100);
        // Every fifth caution in a season carries a ban of one match per five cautions.
        foreach (var player in world.Results.OrderBy(r => r.Week).ThenBy(r => r.FixtureId.Value).SelectMany(r => r.Events)
                     .Where(e => e.Kind == MatchEventKind.Yellow).GroupBy(e => e.Player))
            Assert.Equal(player.Select((e, i) => (i + 1) % Matchday.YellowsPerBan == 0 ? (i + 1) / Matchday.YellowsPerBan : 0), player.Select(e => e.Duration));
        Assert.Contains(world.Results.SelectMany(r => r.Events), e => e.Kind == MatchEventKind.Yellow && e.Duration > 0);
    }

    [Fact]
    public void SelectionUsesAvailablePlayersByRoleThenOutOfPosition()
    {
        var club = WorldFactory.Create(1).OwnedClub; const int week = 10;
        var fit = Matchday.Select(club, week);
        Assert.Equal(new[] { Role.Goalkeeper, Role.Defender, Role.Defender, Role.Defender, Role.Defender, Role.Midfielder, Role.Midfielder,
            Role.Midfielder, Role.Midfielder, Role.Forward, Role.Forward }, fit.Select(s => s.Position));
        Assert.All(fit, s => Assert.Equal(s.Position, s.Player.Role));
        Assert.Equal(club.Players.Where(p => p.Role == Role.Forward).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).Take(2).Select(p => p.Id),
            fit.Where(s => s.Position == Role.Forward).Select(s => s.Player.Id));

        Player Update(Player p) => p.Role == Role.Forward ? p with { InjuredUntilWeek = week + 1 }
            : p.Role == Role.Goalkeeper ? p with { SuspendedMatches = 1 } : p;
        club.Players = club.Players.Select(Update).ToList();
        var depleted = Matchday.Select(club, week);
        Assert.Equal(11, depleted.Length);
        Assert.DoesNotContain(depleted, s => s.Player.Role is Role.Forward or Role.Goalkeeper);
        var midfielders = club.Players.Where(p => p.Role == Role.Midfielder).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).ToArray();
        Assert.Equal(midfielders.Skip(4).Take(2).Select(p => p.Id), depleted.Where(s => s.Position == Role.Forward).Select(s => s.Player.Id));
        var defenders = club.Players.Where(p => p.Role == Role.Defender).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).ToArray();
        Assert.Equal(defenders[4].Id, depleted.Single(s => s.Position == Role.Goalkeeper).Player.Id);
        Assert.Equal(Availability.Injured, Matchday.Status(club.Players.First(p => p.Role == Role.Forward), week).Status);
        Assert.Equal(Availability.Suspended, Matchday.Status(club.Players.First(p => p.Role == Role.Goalkeeper), week).Status);
        Assert.Equal(Availability.Available, Matchday.Status(club.Players.First(p => p.Role == Role.Forward), week + 1).Status);

        // With fewer than eleven eligible players the side is short rather than inventing players.
        club.Players = club.Players.Select((p, i) => i < 10 ? p with { InjuredUntilWeek = 0, SuspendedMatches = 0 } : p with { SuspendedMatches = 2 }).ToList();
        var shortSide = Matchday.Select(club, week);
        Assert.Equal(10, shortSide.Length);
        Assert.Single(shortSide, s => s.Position == Role.Goalkeeper);
    }

    private static World Opening(ulong seed = 2026)
    {
        var world = WorldFactory.Create(seed);
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.PreserveReserve);
        return world;
    }

    private static MatchResult PlayNextOwnedMatch(World world)
    {
        var played = world.Results.Count(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId);
        while (world.Results.Count(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId) == played) Simulation.AdvanceWeek(world);
        return world.Results.Last(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId);
    }

    private static ImmutableArray<Appearance> OwnLineup(World world, MatchResult r) => r.Home == world.OwnedClubId ? r.HomeLineup : r.AwayLineup;

    [Fact]
    public void SuspensionIsServedOverTheClubsNextCompetitiveMatches()
    {
        var world = Opening();
        var striker = world.OwnedClub.Players.Where(p => p.Role == Role.Forward).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).First();
        var index = world.OwnedClub.Players.IndexOf(striker);
        world.OwnedClub.Players[index] = striker with { SuspendedMatches = 2 };
        for (var served = 1; served <= 2; served++)
        {
            var match = PlayNextOwnedMatch(world);
            Assert.DoesNotContain(OwnLineup(world, match), a => a.Player == striker.Id);
            Assert.Equal(2 - served, world.OwnedClub.Players.Single(p => p.Id == striker.Id).SuspendedMatches);
        }
        Assert.Contains(OwnLineup(world, PlayNextOwnedMatch(world)), a => a.Player == striker.Id);
    }

    [Fact]
    public void InjuryKeepsAPlayerOutUntilItsReturnWeek()
    {
        var world = Opening();
        var owned = world.Fixtures.Where(f => f.Home == world.OwnedClubId || f.Away == world.OwnedClubId).OrderBy(f => f.Week).ToArray();
        var striker = world.OwnedClub.Players.Where(p => p.Role == Role.Forward).OrderByDescending(p => p.Ability).ThenBy(p => p.Id.Value).First();
        world.OwnedClub.Players[world.OwnedClub.Players.IndexOf(striker)] = striker with { InjuredUntilWeek = owned[1].Week };
        var status = Matchday.Status(world.OwnedClub.Players.Single(p => p.Id == striker.Id), owned[0].Week);
        Assert.Equal(Availability.Injured, status.Status); Assert.Equal(owned[1].Week, status.ReturnWeek);
        Assert.DoesNotContain(OwnLineup(world, PlayNextOwnedMatch(world)), a => a.Player == striker.Id);
        var returned = PlayNextOwnedMatch(world);
        Assert.Equal(owned[1].Id, returned.FixtureId);
        Assert.Contains(OwnLineup(world, returned), a => a.Player == striker.Id);
    }

    [Fact]
    public void RolloverClearsSeasonCautionsButKeepsInjuriesAndBans()
    {
        var world = SeasonTests.EndFirstSeason();
        var club = world.OwnedClub;
        club.Players[0] = club.Players[0] with { SeasonYellows = 4, SuspendedMatches = 1, InjuredUntilWeek = 60 };
        SimulationTests.Commit(world, Allocation.StartNextSeason);
        Assert.All(world.Clubs.SelectMany(c => c.Players), p => Assert.Equal(0, p.SeasonYellows));
        Assert.Equal(1, world.OwnedClub.Players[0].SuspendedMatches);
        Assert.Equal(60, world.OwnedClub.Players[0].InjuredUntilWeek);
    }

    [Fact]
    public void SchemaFiveSaveMigratesWithEmptyMatchDetailAndAnAvailableSquad()
    {
        var world = Opening(11);
        while (world.Week < 4) Simulation.AdvanceWeek(world);
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 5; legacy["SimulationVersion"] = "market-5";
        foreach (var result in legacy["Results"]!.AsArray())
        {
            foreach (var name in new[] { "HomeLineup", "AwayLineup", "Events", "PlayerOfMatch" }) result!.AsObject().Remove(name);
            foreach (var moment in result!["Moments"]!.AsArray()) moment!.AsObject().Remove("AssistId");
        }
        foreach (var club in legacy["Clubs"]!.AsArray())
            foreach (var player in club!["Players"]!.AsArray())
                foreach (var name in new[] { "InjuredUntilWeek", "SuspendedMatches", "SeasonYellows" }) player!.AsObject().Remove(name);
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(source, bytes);
        Assert.Equal(7, migrated.SchemaVersion); Assert.Equal("contracts-7", migrated.SimulationVersion);
        Assert.Equal(world.Results.Select(r => (r.FixtureId, r.HomeGoals, r.AwayGoals)), migrated.Results.Select(r => (r.FixtureId, r.HomeGoals, r.AwayGoals)));
        Assert.All(migrated.Results, r =>
        {
            Assert.Empty(r.HomeLineup); Assert.Empty(r.AwayLineup); Assert.Empty(r.Events); Assert.Null(r.PlayerOfMatch);
            Assert.All(r.Moments, m => Assert.Null(m.AssistId));
        });
        Assert.All(migrated.Clubs.SelectMany(c => c.Players), p => Assert.Equal(Availability.Available, Matchday.Status(p, migrated.Week + 1).Status));
        Assert.Equal(world.OwnedClub.Cash, migrated.OwnedClub.Cash);
        var replay = WorldCodec.Clone(migrated);
        Simulation.AdvanceWeek(migrated); Simulation.AdvanceWeek(replay);
        Assert.Equal(WorldCodec.Encode(migrated), WorldCodec.Encode(replay));
        Assert.All(migrated.Results.Where(r => r.Week == migrated.Week), r => Assert.Equal(11, r.HomeLineup.Length));
    }

    [Fact]
    public void ManagerLineReportsResultAvailabilityAndStandoutWithoutEmDashes()
    {
        ClubId us = new(1), them = new(2);
        PersonId keeper = new(1), striker = new(2), defender = new(3), rival = new(4);
        var names = new Dictionary<PersonId, string> { [keeper] = "Theo Keeper", [striker] = "Luca Striker", [defender] = "Adam Stopper", [rival] = "Rival Player" };
        var match = new MatchResult(new(1), 3, us, them, 2, 0, 14, 8, 5000, 0, [new(20, us, striker, "Luca Striker scores."), new(70, us, striker, "Luca Striker scores.")])
        {
            Winner = us,
            HomeLineup = [new(keeper, Role.Goalkeeper, 72), new(striker, Role.Forward, 86), new(defender, Role.Defender, 60)],
            AwayLineup = [new(rival, Role.Midfielder, 61)],
            Events = [new(40, us, defender, MatchEventKind.Red, 3), new(60, us, keeper, MatchEventKind.Injury, 2)],
            PlayerOfMatch = striker
        };
        var line = Matchday.ManagerLine(match, us, false, names);
        Assert.Contains("Luca Striker", line);
        Assert.Contains("Adam Stopper", line); Assert.Contains("3 matches", line);
        Assert.Contains("Theo Keeper", line); Assert.Contains("2 weeks", line);
        Assert.DoesNotContain("—", line);
        var loss = Matchday.ManagerLine(match, them, true, names);
        Assert.NotEqual(line, loss);
        Assert.DoesNotContain("—", loss);
    }

    [Fact]
    public async Task ReadModelExposesAvailabilityAndNamesForMatchReports()
    {
        await using var session = new GameSession(new LocalSaveVault(directory));
        foreach (var allocation in new[] { Allocation.Acquire, Allocation.PreserveReserve })
        {
            var current = await session.QueryAsync();
            await session.CommitAsync(allocation.ToString(), current.Revision, await session.PreviewAsync(new(allocation), current.Revision));
        }
        await session.AdvanceAsync(AdvanceTarget.Month);
        var view = await session.QueryAsync();
        Assert.Equal(view.Squad.Select(p => p.Id), view.SquadAvailability.Select(a => a.Player));
        Assert.All(view.SquadAvailability, a => Assert.Equal(a.Status == Availability.Available, Matchday.IsAvailable(view.Squad.Single(p => p.Id == a.Player), view.Week + 1)));
        Assert.NotEmpty(view.Results);
        Assert.All(view.Results.SelectMany(r => r.HomeLineup.Concat(r.AwayLineup)), a => Assert.True(view.PlayerNames.ContainsKey(a.Player)));
    }
}
