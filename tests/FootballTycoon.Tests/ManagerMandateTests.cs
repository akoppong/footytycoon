using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class ManagerMandateTests
{
    [Fact]
    public void DevelopmentUsesOriginalSlotsAndReturnsImmutableDecisionTimeSnapshotsWithoutMutation()
    {
        var world = Roster();
        var older = PlayerAt(world, Role.Forward, 0);
        var second = PlayerAt(world, Role.Forward, 1);
        var young = Set(world, Role.Forward, 2, age: 23, ability: 68);
        var youngest = Set(world, Role.Forward, 3, age: 19, ability: 65);
        Development(world);
        var before = WorldCodec.Encode(world);
        var baseline = Matchday.Select(world.OwnedClub, world.Week);
        var decision = Matchday.Select(world);
        Assert.Equal(before, WorldCodec.Encode(world));
        Assert.Equal(world.Revision, decision.DecisionWorldRevision);
        Assert.Equal(new(ManagerMandate.Development, DirectorMandate.CurrentRecommendation, 1), decision.Origin);
        Assert.Equal(new[] { older.Id, second.Id }, decision.Swaps.Select(s => s.Displaced.PersonId));
        Assert.Equal(new[] { young.Id, youngest.Id }, decision.Swaps.Select(s => s.Selected.PersonId));
        Assert.Equal(new[] { 2, 5 }, decision.Swaps.Select(s => s.AbilitySacrifice));
        Assert.Equal(baseline.Select(s => s.Position), decision.Lineup.Select(s => s.Position));
        Assert.Equal(new[] { young.Id, youngest.Id }, decision.Lineup.Where(s => s.Position == Role.Forward).Select(s => s.Player.Id));
        Assert.Equal(11, decision.Lineup.Select(s => s.Player.Id).Distinct().Count());
        Set(world, Role.Forward, 2, age: 24, ability: 71);
        Assert.Equal(23, decision.Swaps[0].Selected.Age);
        Assert.Equal(68, decision.Swaps[0].Selected.Ability);
    }

    [Theory]
    [InlineData("sacrifice", Role.Forward, Role.Midfielder)]
    [InlineData("ability", Role.Forward, Role.Midfielder)]
    [InlineData("age", Role.Midfielder, Role.Forward)]
    [InlineData("role", Role.Defender, Role.Midfielder)]
    public void PairRankingAndTwoSwapCapApplyAcrossRoles(string ordering, Role first, Role second)
    {
        var world = Roster();
        foreach (var role in new[] { Role.Defender, Role.Midfielder, Role.Forward })
        {
            var index = role == Role.Forward ? 2 : 4;
            var ability = ordering == "sacrifice" ? 67 + (int)role : 68;
            var age = ordering == "age" ? role == Role.Defender ? 21 : role == Role.Midfielder ? 19 : 20 : 20;
            if (ordering == "ability")
            {
                var starterAbility = 65 + 5 * (int)role;
                foreach (var player in world.OwnedClub.Players.Where(p => p.Role == role).Take(index).ToArray())
                    Replace(world, player with { Ability = starterAbility });
                ability = starterAbility - 1;
                // Keep all other bench players below the role's intended starters.
                foreach (var player in world.OwnedClub.Players.Where(p => p.Role == role).Skip(index).ToArray())
                    Replace(world, player with { Ability = 60 });
            }
            Set(world, role, index, age, ability);
        }
        Development(world);
        var decision = Matchday.Select(world);
        Assert.Equal(new[] { first, second }, decision.Swaps.Select(s => s.Selected.Role));
        Assert.Equal(2, decision.Swaps.Length);
        Assert.All(decision.Swaps, s => Assert.InRange(s.AbilitySacrifice, 0, 5));
    }

    [Fact]
    public void ZeroCostTiesUseYoungThenDisplacedIdsAndNeverReusePlayersOrRecomputePairs()
    {
        var world = Roster();
        var older = Enumerable.Range(0, 4).Select(i => PlayerAt(world, Role.Defender, i).Id).ToArray();
        var youth = Enumerable.Range(4, 3).Select(i => Set(world, Role.Defender, i, 20, 70).Id).ToArray();
        Development(world);
        var result = Matchday.Select(world);
        Assert.Equal(youth.Take(2), result.Swaps.Select(s => s.Selected.PersonId));
        Assert.Equal(older.Take(2), result.Swaps.Select(s => s.Displaced.PersonId));
        Assert.All(result.Swaps, s => Assert.Equal(0, s.AbilitySacrifice));
        Assert.DoesNotContain(result.Lineup, s => s.Player.Id == youth[2]);
        world.OwnedClub.Players.Reverse();
        Assert.Equal(result.Lineup.ToArray(), Matchday.Select(world).Lineup.ToArray());
        Assert.Equal(result.Swaps.ToArray(), Matchday.Select(world).Swaps.ToArray());
    }

    [Theory]
    [InlineData("gap")]
    [InlineData("age")]
    [InlineData("injury")]
    [InlineData("suspension")]
    [InlineData("young-starters")]
    public void NoCredibleAlternativeRetainsReadiness(string boundary)
    {
        var world = Roster();
        var young = Set(world, Role.Forward, 2, boundary == "age" ? 24 : 23, boundary == "gap" ? 64 : 65);
        if (boundary == "injury") Replace(world, young with { InjuredUntilWeek = world.Week + 1 });
        if (boundary == "suspension") Replace(world, young with { SuspendedMatches = 1 });
        if (boundary == "young-starters")
        {
            Set(world, Role.Forward, 0, 23, 70);
            Set(world, Role.Forward, 1, 19, 70);
        }
        Development(world);
        var result = Matchday.Select(world);
        Assert.Empty(result.Swaps);
        Assert.Equal(Matchday.Select(world.OwnedClub, world.Week).ToArray(), result.Lineup.ToArray());
        Assert.All(result.Lineup, s => Assert.True(Matchday.IsAvailable(s.Player, world.Week)));
    }

    [Fact]
    public void GoalkeepersAndOutOfRoleFallbackArePreservedAndShortSidesInventNoPlayers()
    {
        var world = Roster();
        Set(world, Role.Goalkeeper, 1, 20, 68);
        Development(world);
        Assert.Empty(Matchday.Select(world).Swaps);
        foreach (var player in world.OwnedClub.Players.Where(p => p.Role == Role.Goalkeeper).ToArray())
            Replace(world, player with { SuspendedMatches = 1 });
        Set(world, Role.Defender, 5, 20, 68);
        var baseline = Matchday.Select(world.OwnedClub, world.Week);
        var result = Matchday.Select(world);
        Assert.Single(result.Swaps);
        Assert.Equal(baseline.Single(s => s.Position == Role.Goalkeeper), result.Lineup.Single(s => s.Position == Role.Goalkeeper));
        Assert.DoesNotContain(result.Swaps, s => s.Displaced.PersonId == baseline.Single(p => p.Position == Role.Goalkeeper).Player.Id);
        for (var i = 0; i < world.OwnedClub.Players.Count; i++)
            world.OwnedClub.Players[i] = world.OwnedClub.Players[i] with { SuspendedMatches = i < 8 ? 0 : 1 };
        result = Matchday.Select(world);
        Assert.Equal(8, result.Lineup.Length);
        Assert.Empty(result.Swaps);
        Assert.Equal(Matchday.Select(world.OwnedClub, world.Week).ToArray(), result.Lineup.ToArray());
        world.OwnedClub.Players = world.OwnedClub.Players.Select(p => p with { SuspendedMatches = 1 }).ToList();
        Assert.Empty(Matchday.Select(world).Lineup);
    }

    [Fact]
    public void DefaultsMatchLegacyAndAnInapplicableMandateDoesNotChangeMatchesOrRandomConsumption()
    {
        var readiness = Roster();
        SimulationTests.Commit(readiness, Allocation.Acquire);
        SimulationTests.Commit(readiness, Allocation.PreserveReserve);
        var development = WorldCodec.Clone(readiness);
        Development(development); // All players are older; neither mode can substitute a prospect.
        var bytes = WorldCodec.Encode(readiness);
        Assert.Equal(Matchday.Select(readiness.OwnedClub, readiness.Week).ToArray(), Matchday.Select(readiness).Lineup.ToArray());
        Assert.Empty(Matchday.Select(readiness).Swaps);
        Assert.Equal(bytes, WorldCodec.Encode(readiness));
        while (readiness.Week < 10)
        {
            Simulation.AdvanceWeek(readiness); Simulation.AdvanceWeek(development);
            Assert.Equal(readiness.RandomStates.OrderBy(s => s.Key), development.RandomStates.OrderBy(s => s.Key));
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(readiness.Results), System.Text.Json.JsonSerializer.Serialize(development.Results));
            Assert.Equal(readiness.Clubs.SelectMany(c => c.Players), development.Clubs.SelectMany(c => c.Players));
            Assert.Equal(readiness.OwnedClub.Cash, development.OwnedClub.Cash);
        }
        Assert.NotEmpty(readiness.Results);
    }

    [Fact]
    public void ChangedCompetitiveAppearancesFeedAnnualDevelopmentAndReplayAcrossSave()
    {
        var readiness = Roster();
        var young = Set(readiness, Role.Forward, 2, 20, 68);
        var other = Set(readiness, Role.Forward, 3, 20, 65);
        SimulationTests.Commit(readiness, Allocation.Acquire);
        SimulationTests.Commit(readiness, Allocation.PreserveReserve);
        var development = WorldCodec.Clone(readiness);
        Development(development);
        World? replay = null;
        while (readiness.Week < 52)
        {
            Simulation.AdvanceWeek(readiness); Simulation.AdvanceWeek(development);
            if (replay is not null) Simulation.AdvanceWeek(replay);
            if (development.Week == 20) replay = WorldCodec.Clone(development);
        }
        Assert.Equal(CareerStatus.SeasonReview, development.Status);
        Assert.Equal(WorldCodec.Encode(development), WorldCodec.Encode(replay!));
        var improvedExposure = 0;
        foreach (var id in new[] { young.Id, other.Id })
        {
            var baseline = readiness.SeasonSummaries.Single().Development.Single(p => p.PlayerId == id);
            var actual = development.SeasonSummaries.Single().Development.Single(p => p.PlayerId == id);
            var appearances = development.Results.SelectMany(r => r.HomeLineup.Concat(r.AwayLineup)).Count(a => a.Player == id);
            Assert.Equal(appearances, actual.Appearances);
            Assert.Equal(baseline.AbilityBefore, actual.AbilityBefore);
            Assert.InRange(actual.AbilityAfter - actual.AbilityBefore, 0, 3);
            if (Math.Min(25, actual.Appearances) > Math.Min(25, baseline.Appearances)) improvedExposure++;
        }
        Assert.True(improvedExposure > 0, "At least one prospect must gain exposure used by the existing growth-chance calculation.");
        Assert.Equal(development.Results.Count(r => r.Home == development.OwnedClubId || r.Away == development.OwnedClubId), development.MandateSelections.Count);
        Assert.Contains(development.MandateSelections, s => !s.Swaps.IsEmpty);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActualOwnedHomeAndAwaySelectionChangesWhileCredibleRivalYouthStayOnTheirBench(bool ownedHome)
    {
        var world = Roster();
        SimulationTests.Commit(world, Allocation.Acquire);
        SimulationTests.Commit(world, Allocation.PreserveReserve);
        Development(world);
        var fixture = world.Fixtures.Where(f => f.Competition == Competition.League &&
            (ownedHome ? f.Home == world.OwnedClubId : f.Away == world.OwnedClubId)).OrderBy(f => f.Week).First();
        while (world.Week < fixture.Week - 1) Simulation.AdvanceWeek(world);
        var rival = world.Clubs.Single(c => c.Id == (ownedHome ? fixture.Away : fixture.Home));
        static PersonId Prepare(Club club)
        {
            club.Players = club.Players.Select(p => p with { Age = 30, Ability = 70, InjuredUntilWeek = 0, SuspendedMatches = 0 }).ToList();
            var prospect = club.Players.Where(p => p.Role == Role.Forward).OrderBy(p => p.Id.Value).ElementAt(2);
            club.Players[club.Players.IndexOf(prospect)] = prospect with { Age = 20, Ability = 68 };
            return prospect.Id;
        }
        var ownProspect = Prepare(world.OwnedClub);
        var rivalProspect = Prepare(rival);
        // Pure projection at the fixture week; actual selection below still happens inside AdvanceWeek.
        world.Week++;
        var projected = Matchday.Select(world);
        var rivalReadiness = Matchday.Select(rival, world.Week);
        world.Week--;
        Assert.Single(projected.Swaps);
        Assert.Equal(ownProspect, projected.Swaps[0].Selected.PersonId);
        Assert.DoesNotContain(rivalReadiness, s => s.Player.Id == rivalProspect);
        Simulation.AdvanceWeek(world);
        var result = world.Results.Single(r => r.FixtureId == fixture.Id);
        var ownedLineup = ownedHome ? result.HomeLineup : result.AwayLineup;
        var rivalLineup = ownedHome ? result.AwayLineup : result.HomeLineup;
        Assert.Equal(projected.Lineup.Select(s => (s.Player.Id, s.Position)), ownedLineup.Select(s => (s.Player, s.Position)));
        Assert.Equal(rivalReadiness.Select(s => (s.Player.Id, s.Position)), rivalLineup.Select(s => (s.Player, s.Position)));
        Assert.Contains(ownedLineup, s => s.Player == ownProspect);
        Assert.DoesNotContain(rivalLineup, s => s.Player == rivalProspect);
    }

    private static World Roster()
    {
        var world = WorldFactory.Create(2026);
        world.OwnedClub.Players = world.OwnedClub.Players.Select(p => p with { Age = 30, Ability = 70 }).ToList();
        return world;
    }

    private static Player PlayerAt(World world, Role role, int index) => world.OwnedClub.Players.Where(p => p.Role == role).OrderBy(p => p.Id.Value).ElementAt(index);

    private static Player Set(World world, Role role, int index, int age, int ability)
    {
        var player = PlayerAt(world, role, index) with { Age = age, Ability = ability };
        Replace(world, player);
        return player;
    }

    private static void Replace(World world, Player player) => world.OwnedClub.Players[world.OwnedClub.Players.FindIndex(p => p.Id == player.Id)] = player;

    // E05 owns the public transaction; this controlled fixture supplies E02's coherent saved transition.
    private static void Development(World world)
    {
        var before = world.Mandates;
        var after = new MandateState(ManagerMandate.Development, before.Director, before.Revision + 1);
        world.Revision++;
        const string id = "test-development";
        world.Commands.Add(id, new(id, "test-development-proposal", world.Revision, "Mandates changed"));
        world.MandateHistory.Add(new(id, world.Week, world.Revision, before, after));
        world.Mandates = after;
    }
}
