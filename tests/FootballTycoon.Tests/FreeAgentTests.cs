using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class FreeAgentTests
{
    internal static World Available(ulong seed = 2026)
    {
        var world = ContractTests.Expiring();
        SimulationTests.Commit(world, Allocation.StartNextSeason, "renew");
        SimulationTests.Commit(world, Allocation.PreserveReserve, "season-two-plan");
        // Make a real released player a useful, affordable controlled candidate without inventing an identity.
        var free = world.FreeAgents.First(f => f.PreviousClubId != world.OwnedClubId);
        world.FreeAgents = [free with { Player = free.Player with { Age = 25, Ability = 90, WeeklyWage = 10000 } }];
        world.Seed = seed;
        // These owner-only scenarios isolate the candidate from autonomous rival offers.
        foreach (var rival in world.Clubs.Where(c => c.Id != world.OwnedClubId))
            rival.Players = rival.Players.Select(p => p with { Ability = 100 }).ToList();
        Assert.NotNull(FreeAgents.Recommend(world)); WorldFactory.Validate(world);
        return world;
    }

    [Fact]
    public void ReleasesEnterOnePoolWithoutExtendingExpiredPayments()
    {
        var world = ContractTests.Expiring();
        var before = world.Clubs.SelectMany(c => c.Players).ToDictionary(p => p.Id);
        var quote = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        var receipt = Proposals.Commit(world, "renew", world.Revision, quote);
        Assert.NotEmpty(world.FreeAgents);
        foreach (var free in world.FreeAgents)
        {
            Assert.Equal(before[free.Player.Id] with { SeasonYellows = 0, TrainingExposure = 0 }, free.Player);
            Assert.Equal(52, free.AvailableSinceWeek);
            Assert.DoesNotContain(world.Clubs.SelectMany(c => c.Players), p => p.Id == free.Player.Id);
            Assert.Contains(world.Departures, d => d.PlayerId == free.Player.Id && d.ClubId == free.PreviousClubId);
            Assert.DoesNotContain(world.Obligations, o => o.Description == $"Player contract {free.Player.ContractId.Value}" && o.EndWeek > 52);
        }
        var saved = WorldCodec.Encode(world);
        Assert.Equal(receipt, Proposals.Commit(world, "renew", quote.Revision, quote));
        Assert.Equal(saved, WorldCodec.Encode(world));
        Assert.Equal(saved, WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    [Fact]
    public void MandatesArePureAndDoNotCreateAnObligationBeforeAgreement()
    {
        var world = Available(); var before = WorldCodec.Encode(world);
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Empty(quote.BlockingReasons); Assert.True(quote.Recruitment!.IsFreeAgent);
        Assert.Equal(0, quote.UpfrontCash);
        Assert.Equal(quote.WeeklyCost * 103, quote.TotalCommitment);
        Assert.Equal(before, WorldCodec.Encode(world));
        Assert.Equal(quote.Id, Proposals.Preview(WorldCodec.Clone(world), quote.Command, world.Revision).Id);
        var obligations = world.Obligations.ToArray(); var journal = world.Journal.ToArray();
        var receipt = Proposals.Commit(world, "approach", world.Revision, quote);
        Assert.Equal(obligations, world.Obligations); Assert.Equal(journal, world.Journal);
        Assert.Single(world.FreeAgentBids); Assert.Single(world.FreeAgents);
        Assert.Equal(receipt, Proposals.Commit(world, "approach", quote.Revision, quote));
        Assert.NotEmpty(Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision).BlockingReasons);
        Assert.NotEmpty(Proposals.Preview(world, new(Allocation.Recruitment), world.Revision).BlockingReasons);
    }

    [Fact]
    public void SuccessAndRefusalReplayWithNoSellerReceiptAndExactFirstLastWages()
    {
        var original = Available(); var successes = 0; var refusals = 0;
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var world = WorldCodec.Clone(original); world.Seed = seed;
            var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
            Proposals.Commit(world, "approach", world.Revision, quote);
            var loaded = WorldCodec.Clone(world);
            var transfers = world.Journal.Count(j => j.Kind == CashKind.Transfer);
            Simulation.AdvanceWeek(world); Simulation.AdvanceWeek(loaded);
            Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
            Assert.Empty(world.FreeAgentBids); Assert.Equal(transfers, world.Journal.Count(j => j.Kind == CashKind.Transfer));
            var signed = world.OwnedClub.Players.SingleOrDefault(p => p.Id == quote.Recruitment!.Player.Id);
            if (signed is null)
            {
                refusals++; Assert.Single(world.FreeAgents);
                Assert.Null(FreeAgents.Recommend(world)); // Same candidate cannot be spammed immediately after refusal.
                Assert.Contains(world.Reviews, r => r.Title == "Free-agent approach closed");
            }
            else
            {
                successes++; Assert.Empty(world.FreeAgents);
                Assert.Equal(quote.Recruitment!.Player.Name, signed.Name);
                Assert.Equal(quote.Recruitment.ContractEndWeek, signed.ContractEndWeek);
                var obligation = Assert.Single(world.Obligations, o => o.Description == $"Player contract {signed.ContractId.Value}");
                Assert.Equal(54, obligation.StartWeek); Assert.Equal(156, obligation.EndWeek);
                Assert.Equal(-quote.WeeklyCost, obligation.WeeklyAmount);
                Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{obligation.Id.Value}/53");
                Simulation.AdvanceWeek(world);
                Assert.Contains(world.Journal, j => j.Reference == $"obligation/{obligation.Id.Value}/54" && j.Amount == -quote.WeeklyCost);
            }
        }
        Assert.True(successes > 0); Assert.True(refusals > 0);
    }

    [Fact]
    public void VeteranExpiryIsReviewedBeforeAnotherWeekCanBePlayed()
    {
        var world = Available();
        var free = world.FreeAgents.Single();
        world.FreeAgents = [free with { Player = free.Player with { Age = 32 } }];
        // Choose a deterministic accepting stream before approval; no quote or outcome rerolls.
        while (RandomStreams.Next(WorldCodec.Clone(world), $"free-agent-negotiation/{world.History.Count + 1}", 100) >= 70) world.Seed++;
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Equal(104, quote.Recruitment!.ContractEndWeek);
        Assert.Equal(quote.WeeklyCost * 51, quote.TotalCommitment);
        Proposals.Commit(world, "veteran", world.Revision, quote);
        Simulation.AdvanceWeek(world);
        var signed = world.OwnedClub.Players.Single(p => p.Id == free.Player.Id);
        var obligation = world.Obligations.Single(o => o.Description == $"Player contract {signed.ContractId.Value}");
        var baseline = Finance.Forecast(world, world.OwnedClubId);
        var finite = Finance.Forecast(world, world.OwnedClubId, extraWeekly: signed.WeeklyWage, extraStarts: 54, extraEnds: 104);
        Assert.Equal(baseline.Points.Last().KnownNet, finite.Points.Last().KnownNet); // Week 105 has no fictitious extra wage.
        Assert.Equal(signed.WeeklyWage * 51, baseline.Points.Last().BaseCash - finite.Points.Last().BaseCash);
        while (world.Week < 104) Simulation.AdvanceWeek(world);
        Assert.Equal(CareerStatus.SeasonReview, world.Status);
        Assert.Contains(world.Journal, j => j.Reference == $"obligation/{obligation.Id.Value}/104" && j.Amount == -signed.WeeklyWage);
        var renewal = Proposals.Preview(world, new OwnerCommand(Allocation.StartNextSeason) { DeclineAcademy = true }, world.Revision);
        Assert.Contains(renewal.Renewal!.Contracts, c => c.PlayerId == signed.Id);
        Proposals.Commit(world, "veteran-renewal", world.Revision, renewal);
        SimulationTests.Commit(world, Allocation.PreserveReserve, "third-season");
        Simulation.AdvanceWeek(world);
        Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{obligation.Id.Value}/105");
        var retained = world.OwnedClub.Players.Single(p => p.Id == signed.Id);
        Assert.NotEqual(signed.ContractId, retained.ContractId);
        var renewed = world.Obligations.Single(o => o.Description == $"Player contract {retained.ContractId.Value}");
        Assert.Contains(world.Journal, j => j.Reference == $"obligation/{renewed.Id.Value}/105" && j.Amount == -retained.WeeklyWage);
        WorldFactory.Validate(world);
    }

    [Fact]
    public void NewFinancialOrRosterFactsCanStopAnAlreadyApprovedApproach()
    {
        var original = Available();
        foreach (var reason in new[] { "cash", "wages", "roster", "unavailable" })
        {
            var world = WorldCodec.Clone(original);
            var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
            Proposals.Commit(world, "approach", world.Revision, quote);
            if (reason == "cash") WorldFactory.AddObligation(world, world.OwnedClubId, 55, 104, -100000000, CashKind.Operations, "Known future cost");
            if (reason == "wages") world.OwnedClub.Players[0] = world.OwnedClub.Players[0] with { WeeklyWage = 100000000 };
            if (reason == "roster") while (world.OwnedClub.Players.Count < Academy.SquadLimit)
                world.OwnedClub.Players.Add(world.OwnedClub.Players[0] with { Id = new(8000 + world.OwnedClub.Players.Count) });
            if (reason == "unavailable") world.FreeAgents.Clear();
            Simulation.AdvanceWeek(world);
            Assert.Empty(world.FreeAgentBids);
            Assert.DoesNotContain(world.OwnedClub.Players, p => p.Id == quote.Recruitment!.Player.Id);
            Assert.Contains(world.Reviews, r => r.Title == "Free-agent approach closed");
        }
    }

    [Fact]
    public void OnlyGenuineRoleShortagesCanBeApproachedOutsideWindows()
    {
        var world = Available();
        while (world.Week < 56) Simulation.AdvanceWeek(world);
        Assert.Null(FreeAgents.Recommend(world));
        var role = world.FreeAgents.Single().Player.Role;
        var count = Contracts.Minimum.Single(m => m.Role == role).Minimum;
        foreach (var player in world.OwnedClub.Players.Where(p => p.Role == role).Skip(count - 1).ToArray()) world.OwnedClub.Players.Remove(player);
        Assert.NotNull(FreeAgents.Recommend(world));
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        Assert.Empty(quote.BlockingReasons);
    }

    [Theory]
    [InlineData(Role.Defender, 39, 40)]
    [InlineData(Role.Goalkeeper, 41, 42)]
    public void UncontractedPlayersAgeOnceAndRetireWithNamedHistory(Role role, int age, int retirementAge)
    {
        var world = Available(); var free = world.FreeAgents.Single();
        world.FreeAgents = [free with { Player = free.Player with { Role = role, Age = age, TrainingExposure = 0 } }];
        var activeAge = world.OwnedClub.Players[0].Age;
        while (world.Week < 104) Simulation.AdvanceWeek(world);
        Assert.Empty(world.FreeAgents);
        var retired = Assert.Single(world.Retirements);
        Assert.Equal(free.Player.Id, retired.PlayerId); Assert.Equal(free.Player.Name, retired.Name);
        Assert.Equal(retirementAge, retired.Age); Assert.Equal(104, retired.Week);
        Assert.Equal(activeAge + 1, world.OwnedClub.Players[0].Age);
        Assert.Equal(retired.Name, PlayerHistory.Names(world)[retired.PlayerId]);
        var encoded = WorldCodec.Encode(world); Seasons.Close(world);
        Assert.Equal(encoded, WorldCodec.Encode(world));
        Assert.Equal(encoded, WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    [Fact]
    public void MigrationDoesNotReactivateOldDepartureRecords()
    {
        var world = Available(); var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 12; legacy["SimulationVersion"] = "academy-12";
        legacy.Remove("FreeAgents"); legacy.Remove("Retirements"); legacy.Remove("FreeAgentBids");
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var original = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(original, bytes); Assert.Equal(14, migrated.SchemaVersion);
        Assert.Empty(migrated.FreeAgents); Assert.Empty(migrated.Retirements); Assert.Empty(migrated.FreeAgentBids);
        Assert.Equal(world.Departures, migrated.Departures);
        Assert.Equal(world.Clubs.SelectMany(c => c.Players), migrated.Clubs.SelectMany(c => c.Players));
        Assert.Equal(world.Journal, migrated.Journal); Assert.Equal(world.RandomStates, migrated.RandomStates);
    }

    [Fact]
    public void DuplicateOrStillContractedPeopleCannotEnterThePool()
    {
        var world = Available(); var valid = world.FreeAgents.Single();
        world.FreeAgents.Add(valid); Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        world.FreeAgents = [valid with { Player = world.OwnedClub.Players[0] }];
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        world.FreeAgents = [valid with { Player = valid.Player with { ContractEndWeek = 200 } }];
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
        world.FreeAgents = [valid with { Player = valid.Player with { Age = 100 } }];
        Assert.Throws<InvalidDataException>(() => WorldCodec.Clone(world));
    }
}
