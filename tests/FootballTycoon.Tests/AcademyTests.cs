using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class AcademyTests
{
    private static World WithIntake(int minimum = 1)
    {
        var world = SeasonTests.EndFirstSeason();
        // Select a cohort-bearing seed for this controlled season-review fixture.
        for (var seed = 1UL; seed < 100; seed++)
        {
            world.Seed = seed;
            if (Seasons.Terms(world).AcademyIntake.Length >= minimum) return world;
        }
        throw new InvalidOperationException("No affordable cohort in the test fixture.");
    }

    [Fact]
    public void PreviewDeclineAndReconsiderPreserveIdentityAndDiscloseEveryWage()
    {
        var world = WithIntake();
        var before = WorldCodec.Encode(world);
        var yes = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        var no = Proposals.Preview(world, new(Allocation.StartNextSeason) { DeclineAcademy = true }, world.Revision);
        var again = Proposals.Preview(WorldCodec.Clone(world), yes.Command, world.Revision);
        Assert.Equal(before, WorldCodec.Encode(world));
        Assert.Empty(yes.BlockingReasons); Assert.Empty(no.BlockingReasons);
        Assert.NotEmpty(yes.Renewal!.AcademyIntake); Assert.Empty(no.Renewal!.AcademyIntake);
        Assert.Equal(yes.Renewal.AcademyIntake.ToArray(), again.Renewal!.AcademyIntake.ToArray());
        Assert.Equal(yes.Id, again.Id); Assert.NotEqual(yes.Id, no.Id);
        var weekly = yes.Renewal.AcademyIntake.Sum(p => p.WeeklyWage);
        Assert.Equal(weekly * 156, yes.TotalCommitment - no.TotalCommitment);
        Assert.Equal(no.Renewal.TotalAnnualWagesAfter + weekly * 52, yes.Renewal.TotalAnnualWagesAfter);
        for (var i = 1; i < yes.Forecast.Points.Length; i++)
        {
            Assert.Equal(no.Forecast.Points[i].KnownNet - weekly, yes.Forecast.Points[i].KnownNet);
            Assert.Equal(no.Forecast.Points[i].DownsideCash - weekly * i, yes.Forecast.Points[i].DownsideCash);
        }
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "forged", world.Revision, yes with { Command = no.Command }));
        Assert.NotEmpty(Proposals.Preview(world, new(Allocation.PreserveReserve) { DeclineAcademy = true }, world.Revision).BlockingReasons);
    }

    [Fact]
    public void GraduationCreatesPersistentPeopleAndExactContractsWithoutUpfrontCash()
    {
        var world = WithIntake();
        var clone = WorldCodec.Clone(world);
        var cash = world.Clubs.ToDictionary(c => c.Id, c => c.Cash);
        var names = world.Clubs.SelectMany(c => c.Players).Select(p => p.Id).ToHashSet();
        var proposal = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        var receipt = Proposals.Commit(world, "intake", world.Revision, proposal);
        Proposals.Commit(clone, "intake", clone.Revision, Proposals.Preview(clone, proposal.Command, clone.Revision));
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(clone));
        Assert.NotEmpty(world.AcademyGraduates);
        Assert.Equal(proposal.Renewal!.AcademyIntake.ToArray(), world.AcademyGraduates.Where(g => g.ClubId == world.OwnedClubId).Select(g => g.Player).ToArray());
        Assert.All(world.Clubs, c => Assert.Equal(cash[c.Id], c.Cash));
        Assert.All(world.AcademyGraduates, g =>
        {
            Assert.DoesNotContain(g.Player.Id, names);
            Assert.Equal(17, g.Player.Age); Assert.Equal(52, g.Week);
            Assert.Equal(g.Player, world.Clubs.Single(c => c.Id == g.ClubId).Players.Single(p => p.Id == g.Player.Id));
            var wage = Assert.Single(world.Obligations, o => o.Description == $"Player contract {g.Player.ContractId.Value}");
            Assert.Equal(53, wage.StartWeek); Assert.Equal(208, wage.EndWeek);
            Assert.Equal(-g.Player.WeeklyWage, wage.WeeklyAmount);
        });
        var saved = WorldCodec.Encode(world);
        Assert.Equal(receipt, Proposals.Commit(world, "intake", proposal.Revision, proposal));
        Assert.Equal(saved, WorldCodec.Encode(world));
        Assert.Equal(saved, WorldCodec.Encode(WorldCodec.Clone(world)));
        SimulationTests.Commit(world, Allocation.PreserveReserve, "season-two-plan");
        Simulation.AdvanceWeek(world);
        Assert.All(world.AcademyGraduates, g => Assert.Contains(world.Journal, j => j.Week == 53
            && j.Account == WorldFactory.Account(g.ClubId) && j.Amount == -g.Player.WeeklyWage && j.Kind == CashKind.Wages));
    }

    [Fact]
    public void DecliningOnlyChangesTheOwnedIntakeAndDoesNotCreateGhostGraduates()
    {
        var yes = WithIntake(); var no = WorldCodec.Clone(yes);
        Proposals.Commit(yes, "yes", yes.Revision, Proposals.Preview(yes, new(Allocation.StartNextSeason), yes.Revision));
        Proposals.Commit(no, "no", no.Revision, Proposals.Preview(no, new(Allocation.StartNextSeason) { DeclineAcademy = true }, no.Revision));
        Assert.DoesNotContain(no.AcademyGraduates, g => g.ClubId == no.OwnedClubId);
        Assert.Equal(yes.AcademyGraduates.Where(g => g.ClubId != yes.OwnedClubId), no.AcademyGraduates);
        Assert.Equal(yes.RandomStates, no.RandomStates);
        Assert.All(no.OwnedClub.Players, p => Assert.True(p.Id.Value < 100000));
    }

    [Fact]
    public void IntakesRespectCashWagesArrearsAndSquadCapacity()
    {
        var world = WithIntake(); var club = world.OwnedClub;
        var next = Pyramid.NextDivisions(world)[club.Id];
        var contracts = Contracts.Plan(world, club, next);
        Assert.NotEmpty(Academy.Recommend(world, club, next, contracts));
        Assert.Empty(Academy.Recommend(world, club, next, contracts with { WageLimit = contracts.WagesAfter }));
        var cash = club.Cash; club.Cash = world.ReserveTarget;
        Assert.Empty(Academy.Recommend(world, club, next, contracts)); club.Cash = cash;
        world.Arrears.Add(new(new(1), club.Id, world.Week, 1));
        Assert.Empty(Academy.Recommend(world, club, next, contracts)); world.Arrears.Clear();
        while (club.Players.Count < Academy.SquadLimit) club.Players.Add(club.Players[0] with { Id = new(9000 + club.Players.Count), ContractEndWeek = 208 });
        Assert.Empty(Academy.Recommend(world, club, next, contracts with { Reviews = [] }));
    }

    [Fact]
    public void CohortsVaryWithoutGuaranteeingAStarOrAdvancingAnyWorldStream()
    {
        var world = WithIntake(); var club = world.OwnedClub;
        var next = Pyramid.NextDivisions(world)[club.Id]; var plan = Contracts.Plan(world, club, next);
        var counts = new HashSet<int>(); var ratings = new HashSet<int>();
        for (var seed = 1UL; seed <= 100; seed++)
        {
            world.Seed = seed; var before = WorldCodec.Encode(world);
            var intake = Academy.Recommend(world, club, next, plan);
            counts.Add(intake.Length);
            Assert.All(intake, p => { Assert.InRange(p.Ability, Contracts.Par(next) - 25, Contracts.Par(next)); ratings.Add(p.Ability); });
            Assert.Equal(before, WorldCodec.Encode(world));
        }
        Assert.Equal(new[] { 0, 1, 2 }, counts.Order()); Assert.True(ratings.Count > 10);
    }

    [Fact]
    public void APartialIntakeCannotSpendTheBudgetOrSquadPlaceTwice()
    {
        var world = WithIntake(2); var club = world.OwnedClub;
        var next = Pyramid.NextDivisions(world)[club.Id]; var plan = Contracts.Plan(world, club, next);
        var full = Academy.Recommend(world, club, next, plan);
        var cost = full[0].WeeklyWage * Seasons.Weeks;
        Assert.Single(Academy.Recommend(world, club, next, plan with { WageLimit = plan.WagesAfter + cost }));
        var cash = club.Cash; club.Cash = world.ReserveTarget + cost;
        Assert.Single(Academy.Recommend(world, club, next, plan)); club.Cash = cash;
        // Isolate exactly one admission slot independently of the generated roster and departures.
        club.Players = club.Players.Take(Academy.SquadLimit - 1).ToList();
        while (club.Players.Count < Academy.SquadLimit - 1) club.Players.Add(club.Players[0] with { Id = new(9000 + club.Players.Count), ContractEndWeek = 208 });
        Assert.Single(Academy.Recommend(world, club, next, plan with { Reviews = [] }));
    }

    [Fact]
    public void AnUnsafeDownsideCanBeResolvedByDecliningTheIntake()
    {
        var world = WithIntake();
        WorldFactory.AddObligation(world, world.OwnedClubId, 53, 104, -100000000, CashKind.Operations, "Known future liability");
        var yes = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        Assert.NotEmpty(yes.Renewal!.AcademyIntake);
        Assert.True(yes.Forecast.LowestDownside < 0);
        Assert.Contains(yes.BlockingReasons, r => r.Contains("Decline this intake"));
        var no = Proposals.Preview(world, new(Allocation.StartNextSeason) { DeclineAcademy = true }, world.Revision);
        // Existing renewal policy allows inherited distress; new academy wages do not add to it.
        Assert.Empty(no.BlockingReasons);
        Proposals.Commit(world, "decline", world.Revision, no);
        Assert.DoesNotContain(world.AcademyGraduates, g => g.ClubId == world.OwnedClubId);
    }

    [Fact]
    public void SchemaElevenMigrationDoesNotInventGraduatesOrAlterExistingPlayers()
    {
        var world = SeasonTests.EndFirstSeason();
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 11; legacy["SimulationVersion"] = "people-history-11"; legacy.Remove("AcademyGraduates");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var original = bytes.ToArray();
        var loaded = WorldCodec.Decode(bytes);
        Assert.Equal(original, bytes); Assert.Equal(18, loaded.SchemaVersion); Assert.Empty(loaded.AcademyGraduates);
        Assert.Equal(world.Clubs.SelectMany(c => c.Players), loaded.Clubs.SelectMany(c => c.Players));
        Assert.Equal(world.Journal, loaded.Journal); Assert.Equal(world.RandomStates, loaded.RandomStates);
        Assert.Equal(WorldCodec.Encode(loaded), WorldCodec.Encode(WorldCodec.Clone(loaded)));
    }

    [Fact]
    public void InvalidGraduationHistoryIsRejected()
    {
        var world = WithIntake();
        Proposals.Commit(world, "intake", world.Revision, Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision));
        var good = world.AcademyGraduates.ToList(); var first = good[0];
        foreach (var bad in new[] { first with { Week = 53 }, first with { ClubId = new(999) }, first with { Player = first.Player with { Age = 18 } }, first with { Player = first.Player with { WeeklyWage = -1 } } })
        {
            world.AcademyGraduates = [bad]; Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        }
        world.AcademyGraduates = [first, first]; Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
    }
}
