using System.Text.Json;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class DirectorMandateTests
{
    [Fact]
    public void ValueUsesActualQuotedTermsAndReturnsPureDecisionTimeEvidence()
    {
        var world = Candidates();
        var baseline = Set(world, 0, 90, 200000);
        var value = Set(world, 1, 85, 150000);
        var current = FreeAgents.RecommendWithEvidence(world)!;
        Assert.Equal(baseline.Id, current.Terms.Player.Id);
        Assert.Equal(RecommendationReason.Current, current.Evidence.Reason);
        Value(world);
        var before = WorldCodec.Encode(world);
        var result = FreeAgents.RecommendWithEvidence(world)!;
        var preview = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Equal(before, WorldCodec.Encode(world));
        Assert.Equal(value.Id, result.Terms.Player.Id);
        Assert.Equal(result.Terms, preview.Recruitment);
        Assert.Empty(preview.BlockingReasons);
        Assert.Equal(0, preview.UpfrontCash);
        Assert.Equal(150000, result.Evidence.Selected.WeeklyWage);
        Assert.Equal((4, 52, 7350000L), (result.Evidence.Selected.WageStartWeek, result.Evidence.Selected.ContractEndWeek, result.Evidence.Selected.TotalCommitment));
        Assert.Equal(preview.TotalCommitment, result.Evidence.Selected.TotalCommitment);
        Assert.Equal(current.Evidence.Baseline, result.Evidence.Baseline);
        Assert.Equal(world.Revision, result.Evidence.DecisionWorldRevision);
        Assert.Equal(new(ManagerMandate.Readiness, DirectorMandate.LowerCostValue, 1), result.Evidence.Origin);
        Assert.Equal(RecommendationReason.LowerCostAlternative, result.Evidence.Reason);
        Set(world, 1, 99, 180000);
        Assert.Equal(85, result.Evidence.Selected.Ability);
        Assert.Equal(150000, result.Evidence.Selected.WeeklyWage);
        Assert.Empty(world.MandateApproaches); // E06 attaches evidence to approved approaches.
    }

    [Theory]
    [InlineData("equal-cost")]
    [InlineData("longer-term")]
    [InlineData("weekly-higher")]
    [InlineData("ability-gap")]
    [InlineData("wrong-role")]
    [InlineData("retired")]
    [InlineData("wage-limit")]
    public void IneligibleOrNonDominatingAlternativeFallsBackToCurrent(string boundary)
    {
        var world = Candidates();
        var baseline = Set(world, 0, 90, 200000, age: boundary == "weekly-higher" ? 25 : 30);
        Set(world, 1, boundary == "ability-gap" ? 84 : 85,
            boundary == "equal-cost" ? 200000 : boundary == "weekly-higher" ? 201000 : boundary == "wage-limit" ? 1000000000 : 150000,
            age: boundary == "longer-term" ? 24 : boundary == "retired" ? 40 : 30,
            role: boundary == "wrong-role" ? Role.Midfielder : Role.Forward);
        Value(world);
        var result = FreeAgents.RecommendWithEvidence(world)!;
        Assert.Equal(baseline.Id, result.Terms.Player.Id);
        Assert.Equal(result.Evidence.Baseline, result.Evidence.Selected);
        Assert.Equal(RecommendationReason.NoLowerCostAlternative, result.Evidence.Reason);
    }

    [Theory]
    [InlineData("total")]
    [InlineData("weekly")]
    [InlineData("ability")]
    [InlineData("age")]
    [InlineData("id")]
    public void ValueOrderingIsTotalThenWeeklyThenAbilityAgeAndId(string ordering)
    {
        var world = Candidates();
        Set(world, 0, 90, 300000, age: 25);
        var first = Set(world, 1, 86, 150000, age: 31);
        var second = Set(world, 2, 85, 150000, age: 31);
        if (ordering == "total")
        {
            first = Set(world, 1, 85, 160000, age: 30);
            Set(world, 2, 89, 150000, age: 25);
        }
        if (ordering == "weekly")
        {
            first = Set(world, 1, 85, 98000, age: 25);
            Set(world, 2, 89, 202000, age: 30); // 98,000 * 101 == 202,000 * 49.
        }
        if (ordering == "age") first = Set(world, 1, 85, 150000, age: 30);
        if (ordering == "id") first = Set(world, 1, 85, 150000, age: 31);
        Value(world);
        var expected = ordering == "id" ? new[] { first.Id, second.Id }.MinBy(id => id.Value) : first.Id;
        Assert.Equal(expected, FreeAgents.Recommend(world)!.Player.Id);
        world.FreeAgents.Reverse();
        Assert.Equal(expected, FreeAgents.Recommend(world)!.Player.Id);
    }

    [Fact]
    public void MissingRolePriorityAndEmergencyWorkingMixArePreservedOutsideWindow()
    {
        var world = Candidates();
        Set(world, 0, 90, 200000, role: Role.Defender);
        var value = Set(world, 1, 85, 150000, role: Role.Defender);
        Set(world, 2, 100, 100000, role: Role.Forward);
        world.OwnedClub.Players.RemoveAll(p => p.Role == Role.Defender);
        world.Week = 3;
        Value(world);
        Assert.Equal(value.Id, FreeAgents.Recommend(world)!.Player.Id);

        var (depth, _, _) = MinimumSquadFixture.Create(15, 3, owned: true);
        Value(depth);
        Assert.NotNull(FreeAgents.Recommend(depth)); // Existing weak depth remains eligible below sixteen.
        var (covered, _, _) = MinimumSquadFixture.Create(16, 3, owned: true);
        Value(covered);
        Assert.Null(FreeAgents.Recommend(covered));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("full")]
    [InlineData("unplanned")]
    [InlineData("review")]
    [InlineData("late")]
    [InlineData("window")]
    public void ExistingReviewAndEligibilityGatesReturnNoRecommendation(string gate)
    {
        var world = Candidates();
        Set(world, 0, 90, 200000);
        Set(world, 1, 85, 150000);
        Value(world);
        if (gate == "empty") world.FreeAgents.Clear();
        if (gate == "full") world.OwnedClub.Players.AddRange(world.FreeAgents.Select(f => f.Player).Take(7));
        if (gate == "unplanned") world.AllocationChosen = false;
        if (gate == "review") world.Status = CareerStatus.SeasonReview;
        if (gate == "late") world.Week = 51;
        if (gate == "window")
        {
            world.OwnedClub.Players.Add(world.FreeAgents[2].Player); // Sixteen and all roles covered.
            world.Week = 3;
        }
        Assert.Null(FreeAgents.RecommendWithEvidence(world));
        Assert.Null(FreeAgents.Recommend(world));
    }

    [Fact]
    public void RankingDoesNotHideReserveFailureOrSearchAgainAfterPreviewIsBlocked()
    {
        var world = Candidates();
        Set(world, 0, 90, 200000);
        var value = Set(world, 1, 85, 150000);
        Value(world);
        WorldFactory.AddObligation(world, world.OwnedClubId, 10, 10, -world.OwnedClub.Cash * 2, CashKind.Operations, "Controlled future cost");
        var before = WorldCodec.Encode(world);
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Equal(value.Id, quote.Recruitment!.Player.Id);
        Assert.NotEmpty(quote.BlockingReasons);
        Assert.Equal(before, WorldCodec.Encode(world));
    }

    [Fact]
    public void ChangingDirectorDoesNotRewriteApprovedTermsOrChangeAcceptanceStreamAndCooldown()
    {
        var world = Candidates();
        Set(world, 0, 90, 200000);
        Set(world, 1, 85, 150000);
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Proposals.Commit(world, "original-approach", world.Revision, quote);
        var control = WorldCodec.Clone(world);
        var bid = Assert.Single(world.FreeAgentBids);
        Value(world);
        Assert.Equal(bid, Assert.Single(world.FreeAgentBids));
        Assert.Null(FreeAgents.Recommend(world));
        Assert.Equal(quote.Recruitment, world.History.Last().Recruitment);
        Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(control);
        Assert.Equal(control.RandomStates.OrderBy(s => s.Key), world.RandomStates.OrderBy(s => s.Key));
        Assert.Equal(JsonSerializer.Serialize(control.Clubs), JsonSerializer.Serialize(world.Clubs));
        Assert.Equal(control.Obligations, world.Obligations);
        Assert.Equal(control.Journal, world.Journal);
        Assert.Equal(control.Reviews, world.Reviews);
        // A failed or successful original target cannot be approached again inside the existing cooldown.
        Assert.NotEqual(bid.PlayerId, FreeAgents.Recommend(world)?.Player.Id);
    }

    private static World Candidates()
    {
        var (world, _, _) = MinimumSquadFixture.Create(15, 2, owned: true);
        world.FreeAgents = world.FreeAgents.OrderBy(f => f.Player.Id.Value).ToList();
        for (var i = 0; i < world.FreeAgents.Count; i++) Set(world, i, 1, 1000000000);
        return world;
    }

    private static Player Set(World world, int index, int ability, long wage, int age = 30, Role role = Role.Forward)
    {
        var free = world.FreeAgents[index];
        var player = free.Player with { Ability = ability, WeeklyWage = wage, Age = age, Role = role };
        world.FreeAgents[index] = free with { Player = player };
        return player;
    }

    // E05 owns the public command; this fixture installs a coherent E02 transition for behavior tests.
    private static void Value(World world)
    {
        var before = world.Mandates;
        var after = new MandateState(before.Manager, DirectorMandate.LowerCostValue, before.Revision + 1);
        world.Revision++;
        const string id = "test-value";
        world.Commands.Add(id, new(id, "test-value-proposal", world.Revision, "Mandates changed"));
        world.MandateHistory.Add(new(id, world.Week, world.Revision, before, after));
        world.Mandates = after;
    }
}
