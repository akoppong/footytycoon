using FootballTycoon.Core;
using FootballTycoon.Headless;
using Xunit;

namespace FootballTycoon.Tests;

public class PlayableStartDiagnosticsTests
{
    private static Player Player(int id, Role role, int ability, int age) => new(new(id), $"Player {id}", role, ability, age, 10000, new(id), 104);

    [Fact]
    public void ZeroCostPairsCanExcludePositiveCostPairAtTheTwoSwapCap()
    {
        var club = new Club { Players = [Player(1, Role.Goalkeeper, 70, 30)] };
        for (var id = 2; id <= 5; id++) club.Players.Add(Player(id, Role.Defender, 70, 30));
        for (var id = 6; id <= 9; id++) club.Players.Add(Player(id, Role.Midfielder, 70, 30));
        club.Players.AddRange([Player(10, Role.Forward, 70, 30), Player(11, Role.Forward, 70, 30),
            Player(12, Role.Defender, 70, 20), Player(13, Role.Midfielder, 70, 20), Player(14, Role.Forward, 69, 20)]);
        var swaps = PlayableStartDiagnostics.DevelopmentSwaps(club, 53);
        Assert.Equal(new[] { 12, 13 }, swaps.Select(s => s.Young.Id.Value));
        Assert.All(swaps, s => Assert.Equal(0, s.AbilitySacrifice));
        Assert.Equal(2, swaps.Select(s => s.Starter.Id).Distinct().Count());
        club.Players.RemoveAll(p => p.Id.Value == 13);
        Assert.Contains(PlayableStartDiagnostics.DevelopmentSwaps(club, 53), s => s.Young.Id.Value == 14 && s.AbilitySacrifice == 1);
    }

    [Fact]
    public void ValueUsesBothActualCostsAndDoesNotSkipAnInfeasibleFirstRankedCandidate()
    {
        static CandidateDiagnostic C(int id, int ability, long wage, long total, bool feasible = true) =>
            new(Player(id, Role.Forward, ability, 30), wage, 54, 104, total, 0, feasible);
        var baseline = C(1, 70, 100000, 5100000);
        var longTerm = C(2, 69, 90000, 13950000);
        var equal = C(3, 69, 100000, 5100000);
        var tooWeak = C(4, 64, 70000, 3570000);
        var feasible = C(5, 68, 90000, 4590000);
        var blocked = C(6, 65, 80000, 4080000, false);
        Assert.Null(PlayableStartDiagnostics.ValueAlternative([baseline, longTerm, equal, tooWeak]));
        Assert.Equal(blocked, PlayableStartDiagnostics.ValueAlternative([baseline, longTerm, equal, tooWeak, feasible, blocked]));
    }

    [Fact]
    public void OrdinaryPreparationMatchesProductionTermsAndInspectionPreservesState()
    {
        var world = WorldFactory.Create(2026);
        Assert.True(PlayableStartDiagnostics.Prepare(world, _ => { }));
        var before = WorldCodec.Encode(world);
        var result = PlayableStartDiagnostics.Inspect(world);
        Assert.True(result.SubstantiveCriteriaPass);
        Assert.Equal(19, result.SquadSize);
        Assert.Equal(52, result.Week);
        Assert.Equal(result.EligibleCandidates[0].Player.Id, result.CurrentPreview.Recruitment!.Player.Id);
        Assert.Equal(result.EligibleCandidates[0].TotalCommitment, result.CurrentPreview.TotalCommitment);
        Assert.Equal(before, WorldCodec.Encode(world));
        var replay = WorldFactory.Create(2026);
        Assert.True(PlayableStartDiagnostics.Prepare(replay, _ => { }));
        Assert.Equal(before, WorldCodec.Encode(replay));
    }
}
