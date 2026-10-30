using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;
using Xunit;

namespace FootballTycoon.Tests;

public sealed class OwnerMandateStateTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "football-tycoon-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    // E02 tests persistent records directly. Command execution belongs to E05.
    private static void RecordChange(World world, ManagerMandate manager, DirectorMandate director)
    {
        var before = world.Mandates;
        var after = new MandateState(manager, director, before.Revision + 1);
        var id = $"mandates-{after.Revision}";
        world.Revision++;
        world.Commands.Add(id, new(id, $"test-proposal-{after.Revision}", world.Revision, "Mandates changed"));
        world.MandateHistory.Add(new(id, world.Week, world.Revision, before, after));
        world.Mandates = after;
    }
    private static MandateOrigin Origin(World world) => new(world.Mandates.Manager, world.Mandates.Director, world.Mandates.Revision);
    private static JsonObject Legacy(World world)
    {
        var json = JsonSerializer.SerializeToNode(world)!.AsObject();
        json["SchemaVersion"] = 23; json["SimulationVersion"] = "compact-journal-23";
        foreach (var key in new[] { "Mandates", "MandateHistory", "MandateSelections", "MandateApproaches" }) json.Remove(key);
        return json;
    }
    private static void Reject(World world, Action<World> corrupt)
    {
        var copy = WorldCodec.Clone(world); corrupt(copy);
        Assert.Throws<InvalidDataException>(() => WorldCodec.Decode(WorldCodec.Encode(copy)));
    }

    [Fact]
    public void CurrentRoundTripPreservesJointTransitionsAndSharedReceipts()
    {
        var world = WorldFactory.Create(2026);
        Assert.Equal(MandateState.Default, world.Mandates);
        SimulationTests.Commit(world, Allocation.Acquire);
        var decisions = world.History.Count;
        RecordChange(world, ManagerMandate.Development, DirectorMandate.LowerCostValue);
        RecordChange(world, ManagerMandate.Readiness, DirectorMandate.LowerCostValue);
        var bytes = WorldCodec.Encode(world); var loaded = WorldCodec.Decode(bytes);
        Assert.Equal(bytes, WorldCodec.Encode(loaded));
        Assert.Equal(2, loaded.Mandates.Revision); Assert.Equal(decisions, loaded.History.Count);
        Assert.Equal(3, loaded.Commands.Count); Assert.Equal(world.MandateHistory, loaded.MandateHistory);
    }

    [Fact]
    public void InvalidEnumsTransitionsAndReceiptReferencesFail()
    {
        var world = WorldFactory.Create(2026);
        RecordChange(world, ManagerMandate.Development, DirectorMandate.LowerCostValue);
        Reject(world, w => w.Mandates = w.Mandates with { Manager = (ManagerMandate)99 });
        Reject(world, w => w.Mandates = w.Mandates with { Director = (DirectorMandate)99 });
        Reject(world, w => w.Mandates = w.Mandates with { Revision = 2 });
        Reject(world, w => w.MandateHistory.Clear());
        Reject(world, w => w.MandateHistory.Add(w.MandateHistory[0]));
        Reject(world, w => w.MandateHistory[0] = w.MandateHistory[0] with { Week = 1 });
        Reject(world, w => w.MandateHistory[0] = w.MandateHistory[0] with { Before = w.Mandates });
        Reject(world, w => w.MandateHistory[0] = w.MandateHistory[0] with { WorldRevision = 0 });
        Reject(world, w => w.Commands.Clear());
        Reject(world, w => w.Commands["mandates-1"] = w.Commands["mandates-1"] with { Revision = 2 });
        Reject(world, w => { w.Mandates = MandateState.Default with { Revision = 1 }; w.MandateHistory[0] = w.MandateHistory[0] with { After = w.Mandates }; });
    }

    [Fact]
    public async Task LegacyPendingBidImportsProspectivelyWithoutRewritingSourceOrStoredIdentities()
    {
        var world = FreeAgentTests.Available();
        SimulationTests.Commit(world, Allocation.FreeAgentRecruitment, "legacy-bid");
        var legacy = Legacy(world); var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString());
        var original = bytes.ToArray();
        var vault = new LocalSaveVault(directory);
        var saved = vault.Write(bytes, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "schema-23");
        var path = Path.Combine(directory, saved.SnapshotId + ".ftsave"); var originalFile = File.ReadAllBytes(path);
        await using var session = new GameSession(vault); await session.LoadAsync(saved.SnapshotId);
        var imported = WorldCodec.Decode(await session.ExportCheckpointAsync());
        Assert.Equal(24, imported.SchemaVersion); Assert.Equal("owner-mandates-24", imported.SimulationVersion);
        Assert.Equal(MandateState.Default, imported.Mandates); Assert.Empty(imported.MandateHistory);
        Assert.Empty(imported.MandateSelections); Assert.Empty(imported.MandateApproaches);
        // Compare every preexisting field, including pending terms, forecast IDs, RNG and receipts.
        Assert.True(JsonNode.DeepEquals(legacy, Legacy(imported)));
        Assert.Equal(original, bytes); Assert.Equal(originalFile, File.ReadAllBytes(path));
        await session.AdvanceAsync(AdvanceTarget.Week);
        var after = WorldCodec.Decode(await session.ExportCheckpointAsync());
        Assert.Empty(after.FreeAgentBids);
        var legacyOutcome = Assert.Single(after.MandateApproaches);
        Assert.Null(legacyOutcome.Recommendation); Assert.Null(legacyOutcome.Outcome!.Origin);
        Assert.Equal(world.History.Select(h => h.OriginalForecast.Id), after.History.Select(h => h.OriginalForecast.Id));
        Assert.Equal(world.Commands["legacy-bid"], after.Commands["legacy-bid"]);
        Assert.Equal(originalFile, File.ReadAllBytes(path));
    }

    private static World SelectionWorld()
    {
        var world = WorldFactory.Create(2026);
        SimulationTests.Commit(world, Allocation.Acquire); SimulationTests.Commit(world, Allocation.PreserveReserve);
        RecordChange(world, ManagerMandate.Development, DirectorMandate.CurrentRecommendation);
        var revision = world.Revision;
        while (!world.Results.Any(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId))
        { revision = world.Revision; Simulation.AdvanceWeek(world); }
        var result = world.Results.First(r => r.Home == world.OwnedClubId || r.Away == world.OwnedClubId);
        var lineup = result.Home == world.OwnedClubId ? result.HomeLineup : result.AwayLineup;
        var selected = world.OwnedClub.Players.First(p => p.Role != Role.Goalkeeper && lineup.Any(a => a.Player == p.Id && a.Position == p.Role));
        var displaced = world.OwnedClub.Players.First(p => p.Role == selected.Role && !lineup.Any(a => a.Player == p.Id));
        // Snapshot values represent decision time, deliberately independent of current player values.
        var swap = new SelectionSwap(new(displaced.Id, selected.Role, 29, 63), new(selected.Id, selected.Role, 21, 60), 3);
        world.MandateSelections.RemoveAll(s => s.FixtureId == result.FixtureId);
        world.MandateSelections.Add(new(result.FixtureId, revision, Origin(world), [swap]));
        WorldFactory.Validate(world);
        return world;
    }

    [Fact]
    public void SelectionEvidenceValidatesOriginChronologyReferencesAndRetainsDepartedPeople()
    {
        var world = SelectionWorld();
        Reject(world, w => w.MandateSelections.Add(w.MandateSelections[0]));
        Reject(world, w => w.MandateSelections[0] = w.MandateSelections[0] with { FixtureId = new(-1) });
        Reject(world, w => w.MandateSelections[0] = w.MandateSelections[0] with { DecisionWorldRevision = 0 });
        Reject(world, w => w.MandateSelections[0] = w.MandateSelections[0] with { Origin = new(ManagerMandate.Readiness, DirectorMandate.CurrentRecommendation, 0) });
        Reject(world, w => w.MandateSelections[0] = w.MandateSelections[0] with { Swaps = [w.MandateSelections[0].Swaps[0] with { AbilitySacrifice = 6 }] });
        Reject(world, w => w.MandateSelections[0] = w.MandateSelections[0] with { Swaps = [w.MandateSelections[0].Swaps[0] with { Selected = new(new(-1), Role.Forward, 21, 60) }] });
        // Later mandates cannot relabel an older selection, including changes in the same week.
        RecordChange(world, ManagerMandate.Readiness, DirectorMandate.LowerCostValue);
        WorldFactory.Validate(world);
        Reject(world, w => w.MandateSelections[0] = w.MandateSelections[0] with { Origin = Origin(w) });
        var id = world.MandateSelections[0].Swaps[0].Selected.PersonId;
        var player = world.OwnedClub.Players.Single(p => p.Id == id); world.OwnedClub.Players.Remove(player);
        world.Departures.Add(new(id, player.Name, player.Role, player.Age, player.Ability, world.OwnedClubId, world.Week, "Test departure"));
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldCodec.Clone(world)));
    }

    private static World ApprovedWorld(World? prepared = null)
    {
        var world = prepared ?? FreeAgentTests.Available();
        var quote = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
        var terms = quote.Recruitment!;
        var candidate = new RecommendationCandidate(terms.Player.Id, terms.Player.Role, terms.Player.Age, terms.Player.Ability,
            terms.WeeklyWage, world.Week + 2, terms.ContractEndWeek, quote.TotalCommitment);
        var evidence = new RecommendationEvidence(world.Week, world.Revision, Origin(world), candidate, candidate, RecommendationReason.Current);
        Proposals.Commit(world, "approach", world.Revision, quote);
        world.MandateApproaches[world.MandateApproaches.FindIndex(a => a.ForecastId == quote.Forecast.Id)] = new(quote.Forecast.Id, evidence, null);
        WorldFactory.Validate(world); return world;
    }

    [Fact]
    public void EarlierRevisionCannotHideAPreviousWeekMandateChange()
    {
        var selection = SelectionWorld();
        Assert.True(selection.MandateHistory[0].Week < selection.Results[0].Week);
        Reject(selection, w => w.MandateSelections[0] = w.MandateSelections[0] with
        {
            Origin = new(ManagerMandate.Readiness, DirectorMandate.CurrentRecommendation, 0),
            DecisionWorldRevision = w.MandateHistory[0].WorldRevision - 1,
            Swaps = []
        });

        var recommendation = FreeAgentTests.Available();
        RecordChange(recommendation, ManagerMandate.Development, DirectorMandate.CurrentRecommendation);
        Simulation.AdvanceWeek(recommendation);
        ApprovedWorld(recommendation);
        var evidence = recommendation.MandateApproaches[0].Recommendation!;
        Assert.True(recommendation.MandateHistory[0].Week < evidence.Week);
        Reject(recommendation, w => w.MandateApproaches[0] = w.MandateApproaches[0] with
        {
            Recommendation = evidence with
            {
                Origin = new(ManagerMandate.Readiness, DirectorMandate.CurrentRecommendation, 0),
                DecisionWorldRevision = w.MandateHistory[0].WorldRevision - 1
            }
        });
    }

    [Fact]
    public void ApprovalEvidencePreservesOriginalMandateAndChecksTermsAndOutcomeLinks()
    {
        var world = ApprovedWorld(); var recommendation = world.MandateApproaches[0].Recommendation!;
        Reject(world, w => w.MandateApproaches.Add(w.MandateApproaches[0]));
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { ForecastId = "missing" });
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { Recommendation = recommendation with { Reason = (RecommendationReason)99 } });
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { Recommendation = recommendation with { Selected = recommendation.Selected with { TotalCommitment = 1 } } });
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { Recommendation = recommendation with { Baseline = recommendation.Baseline with { PersonId = new(-1) } } });
        RecordChange(world, ManagerMandate.Development, DirectorMandate.LowerCostValue);
        WorldFactory.Validate(world); Simulation.AdvanceWeek(world);
        var selected = world.OwnedClub.Players.SingleOrDefault(p => p.Id == recommendation.Selected.PersonId);
        var obligation = selected is null ? null : world.Obligations.Single(o => o.Description == $"Player contract {selected.ContractId.Value}");
        var outcome = new ApproachOutcomeEvidence(recommendation.Origin, recommendation.Selected.PersonId, world.Week,
            selected is null ? ApproachOutcome.Declined : ApproachOutcome.Signed, "Observed production resolution", selected?.ContractId, obligation?.Id);
        world.MandateApproaches[0] = world.MandateApproaches[0] with { Outcome = outcome };
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(WorldCodec.Clone(world)));
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { Outcome = outcome with { Origin = Origin(w) } });
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { Outcome = outcome with { Outcome = ApproachOutcome.Signed, ContractId = new(123), WageObligationId = new(-1) } });
        // A real legacy approval can have an outcome without invented recommendation/origin.
        world.MandateApproaches[0] = new(world.MandateApproaches[0].ForecastId, null, outcome with { Origin = null });
        WorldFactory.Validate(world);
        Reject(world, w => w.MandateApproaches[0] = w.MandateApproaches[0] with { Outcome = outcome });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SignedEvidenceRetainsOriginalContractAfterRenewalOrDeparture(bool release)
    {
        var world = FreeAgentTests.Available();
        var free = world.FreeAgents.Single();
        world.FreeAgents = [free with { Player = free.Player with { Age = 32 } }];
        // Controlled test fixture chooses an accepting stream before approval, never a playable-start recipe.
        while (RandomStreams.Next(WorldCodec.Clone(world), $"free-agent-negotiation/{world.History.Count + 1}", 100) >= 70) world.Seed++;
        ApprovedWorld(world);
        var recommendation = world.MandateApproaches[0].Recommendation!;
        Simulation.AdvanceWeek(world);
        var signed = world.OwnedClub.Players.Single(p => p.Id == free.Player.Id);
        var obligation = world.Obligations.Single(o => o.Description == $"Player contract {signed.ContractId.Value}");
        var outcome = world.MandateApproaches[0].Outcome!;
        Assert.Equal(ApproachOutcome.Signed, outcome.Outcome);
        Assert.Equal(signed.ContractId, outcome.ContractId); Assert.Equal(obligation.Id, outcome.WageObligationId);
        world = WorldCodec.Clone(world);
        while (world.Week < 104) Simulation.AdvanceWeek(world);
        var previousReport = MandateReports.Season(world, 2);
        Assert.True(previousReport.Complete);
        var originalPayments = Assert.Single(previousReport.Approaches);
        Assert.Equal(obligation, originalPayments.SignedWages);
        Assert.Equal(signed.WeeklyWage * (obligation.EndWeek - obligation.StartWeek + 1), originalPayments.WagesPaid);
        Assert.Equal(obligation.EndWeek - obligation.StartWeek + 1, originalPayments.WagePayments.Length);
        var command = new OwnerCommand(Allocation.StartNextSeason) { DeclineAcademy = true };
        var renewal = Proposals.Preview(world, command, world.Revision);
        var contract = renewal.Renewal!.Contracts.Single(c => c.PlayerId == signed.Id);
        Assert.Equal(ContractAction.Renew, contract.Recommended);
        if (release)
        {
            // Keep the other releasable contracts for cover before releasing this player.
            var overrides = renewal.Renewal.Contracts.Where(c => c.Recommended == ContractAction.Release)
                .Select(c => c.PlayerId).Append(signed.Id).ToImmutableArray();
            renewal = Proposals.Preview(world, command with { ContractOverrides = overrides }, world.Revision);
        }
        Assert.Empty(renewal.BlockingReasons);
        Proposals.Commit(world, "evidence-renewal", world.Revision, renewal);
        SimulationTests.Commit(world, Allocation.PreserveReserve, "third-season");
        Simulation.AdvanceWeek(world);
        world = WorldCodec.Clone(world);
        Assert.Equal(outcome, world.MandateApproaches[0].Outcome);
        Assert.Equal(recommendation, world.MandateApproaches[0].Recommendation);
        Assert.Equal(obligation, world.Obligations.Single(o => o.Id == obligation.Id));
        Assert.Contains(world.Journal, j => j.Reference == $"obligation/{obligation.Id.Value}/104" && j.Amount == -signed.WeeklyWage);
        Assert.DoesNotContain(world.Journal, j => j.Reference == $"obligation/{obligation.Id.Value}/105");
        Assert.Equal(JsonSerializer.Serialize(originalPayments), JsonSerializer.Serialize(Assert.Single(MandateReports.Season(world, 2).Approaches)));
        Assert.DoesNotContain(MandateReports.Season(world, 3).Approaches, a => a.ForecastId == originalPayments.ForecastId);
        if (release)
        {
            Assert.DoesNotContain(world.OwnedClub.Players, p => p.Id == signed.Id);
            Assert.Contains(world.Departures, p => p.PlayerId == signed.Id);
        }
        else Assert.NotEqual(signed.ContractId, world.OwnedClub.Players.Single(p => p.Id == signed.Id).ContractId);
    }

    [Fact]
    public async Task QueriesArePureImmutableAndKeepHistoricalMatchesAfterRollover()
    {
        var world = SelectionWorld(); var snapshot = OwnerMandates.View(world); var initialHistory = snapshot.History.Length;
        RecordChange(world, ManagerMandate.Readiness, DirectorMandate.LowerCostValue);
        Assert.Equal(initialHistory, snapshot.History.Length); Assert.Equal(1, snapshot.Current.Revision);
        while (world.Week < 52) Simulation.AdvanceWeek(world);
        SimulationTests.Commit(world, Allocation.StartNextSeason, "renew");
        var vault = new LocalSaveVault(directory);
        var saved = vault.Write(WorldCodec.Encode(world), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), null, "rolled");
        await using var session = new GameSession(vault); await session.LoadAsync(saved.SnapshotId);
        var before = await session.ExportCheckpointAsync(); var view = await session.QueryAsync();
        Assert.Empty(view.Results); Assert.NotEmpty(view.Mandates.HistoricalMatches);
        Assert.Contains(view.Mandates.HistoricalMatches, r => r.FixtureId == view.Mandates.Selections[0].FixtureId);
        var changedCopy = view.Mandates with { History = [], Current = MandateState.Default };
        Assert.Empty(changedCopy.History); Assert.Equal(2, (await session.QueryAsync()).Mandates.History.Length);
        Assert.Equal(before, await session.ExportCheckpointAsync());
    }
}
