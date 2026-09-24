using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class ContractTests
{
    // Season one ends with every contract expiring (generated contracts otherwise first expire at week 104).
    // Wage obligations are cut to the same week, so the world stays consistent with a real expiry.
    internal static World Expiring(Func<Player, bool>? keep = null)
    {
        var world = SeasonTests.EndFirstSeason();
        foreach (var club in world.Clubs)
            for (var i = 0; i < club.Players.Count; i++)
            {
                var player = club.Players[i];
                if (keep?.Invoke(player) == true) continue;
                world.Obligations = world.Obligations.Select(o => o.Description == $"Player contract {player.ContractId.Value}" ? o with { EndWeek = world.Week } : o).ToList();
                club.Players[i] = player with { ContractEndWeek = world.Week };
            }
        WorldFactory.Validate(world);
        return world;
    }

    private static OwnerCommand Renew(params Player[] overrides) =>
        new(Allocation.StartNextSeason) { ContractOverrides = overrides.Select(p => p.Id).ToImmutableArray() };

    private static void Set(Club club, Func<Player, bool> which, Func<Player, Player> change)
    {
        for (var i = 0; i < club.Players.Count; i++) if (which(club.Players[i])) club.Players[i] = change(club.Players[i]);
    }

    // A controlled owned squad: one young key forward, an aging veteran starter, an aging weak midfielder,
    // two weak defenders (only one can go without breaching the minimum) and regulars everywhere else.
    private static (World World, int Par, Player Key, Player Veteran, Player Aging, Player Weakest, Player Cover, Player Regular) Controlled()
    {
        var world = Expiring();
        var club = world.OwnedClub;
        var par = Contracts.Par(Pyramid.NextDivisions(world)[club.Id]);
        Set(club, _ => true, p => p with { Age = 26, Ability = par });
        var forwards = club.Players.Where(p => p.Role == Role.Forward).ToArray();
        var midfielders = club.Players.Where(p => p.Role == Role.Midfielder).ToArray();
        var defenders = club.Players.Where(p => p.Role == Role.Defender).ToArray();
        Set(club, p => p.Id == forwards[0].Id, p => p with { Age = 21, Ability = par + 10 });
        Set(club, p => p.Id == forwards[1].Id, p => p with { Age = 33, Ability = par + 7 });
        Set(club, p => p.Id == midfielders[0].Id, p => p with { Age = 32, Ability = par - 1 });
        Set(club, p => p.Id == defenders[0].Id, p => p with { Age = 27, Ability = par - 9 });
        Set(club, p => p.Id == defenders[1].Id, p => p with { Age = 25, Ability = par - 8 });
        Player Get(Player p) => club.Players.Single(x => x.Id == p.Id);
        return (world, par, Get(forwards[0]), Get(forwards[1]), Get(midfielders[0]), Get(defenders[0]), Get(defenders[1]), Get(midfielders[1]));
    }

    [Fact]
    public void DirectorRecommendationsAreDeterministicExplainedAndKeepAViableSquad()
    {
        var (world, _, key, veteran, aging, weakest, cover, regular) = Controlled();
        var before = WorldCodec.Encode(world);
        var terms = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision).Renewal!;
        Assert.Equal(before, WorldCodec.Encode(world));
        Assert.Equal(terms.Contracts.ToArray(), Proposals.Preview(WorldCodec.Clone(world), new(Allocation.StartNextSeason), world.Revision).Renewal!.Contracts.ToArray());
        Assert.Equal(18, terms.Contracts.Length); Assert.Equal(18, terms.PlayerContracts);
        ContractReview Of(Player p) => terms.Contracts.Single(c => c.PlayerId == p.Id);
        Assert.All(terms.Contracts, c =>
        {
            Assert.Equal(c.Recommended, c.Chosen);
            Assert.DoesNotContain("—", c.Reason);
            Assert.Contains($", {c.Age}", c.Reason);
            Assert.InRange(c.Years, 1, 3);
            Assert.True(c.OfferedWage > 0);
            Assert.EndsWith(c.Recommended == ContractAction.Release ? "recommends release"
                : $"recommends {c.Years} year{(c.Years == 1 ? "" : "s")} at {Money.Format(c.OfferedWage)}/wk", c.Reason);
        });
        Assert.Equal(ContractAction.Renew, Of(key).Recommended); Assert.Equal(3, Of(key).Years); Assert.StartsWith("Key player", Of(key).Reason);
        Assert.Equal(ContractAction.Renew, Of(veteran).Recommended); Assert.Equal(1, Of(veteran).Years);
        Assert.Equal(ContractAction.Renew, Of(regular).Recommended); Assert.Equal(2, Of(regular).Years);
        Assert.Equal(ContractAction.Release, Of(aging).Recommended); Assert.StartsWith("Aging", Of(aging).Reason);
        Assert.Equal(ContractAction.Release, Of(weakest).Recommended);
        // Releasing the second weak defender would leave four; he is kept for cover on a short, cheaper deal.
        Assert.Equal(ContractAction.Renew, Of(cover).Recommended); Assert.Equal(1, Of(cover).Years); Assert.Contains("needed for cover", Of(cover).Reason);
        // Same current wage throughout, so offers order by standing: raise for the young key player, cuts for age and weakness.
        Assert.True(Of(key).OfferedWage > Of(veteran).OfferedWage);
        Assert.True(Of(veteran).OfferedWage > Of(regular).OfferedWage);
        Assert.True(Of(regular).OfferedWage > Of(cover).OfferedWage);
        Assert.Equal(2, terms.Released); Assert.Equal(16, terms.Renewed);
        Assert.False(terms.RaisesHeld);
        Assert.Equal(terms.Contracts.Where(c => c.Chosen == ContractAction.Renew).Sum(c => c.OfferedWage) * 52, terms.RenewedAnnualWages);
        Assert.Equal(terms.Contracts.Sum(c => c.CurrentWage) * 52, terms.ExpiringAnnualWages);
        Assert.Equal(Finance.AnnualWages(world.OwnedClub), terms.AnnualWagesBefore);
        Assert.Equal(terms.RenewedAnnualWages, terms.AnnualWagesAfter);
    }

    [Fact]
    public void OwnerOverridesAreExecutedAndRecordedAgainstTheRecommendation()
    {
        var (world, _, key, _, aging, weakest, _, _) = Controlled();
        var untouched = world.OwnedClub.Players.First(p => p.Role == Role.Goalkeeper);
        // One goalkeeper keeps a contract to week 104: not expiring, so not reviewed or changed.
        world.OwnedClub.Players[world.OwnedClub.Players.IndexOf(untouched)] = untouched with { ContractEndWeek = 104 };
        world.Obligations = world.Obligations.Select(o => o.Description == $"Player contract {untouched.ContractId.Value}" ? o with { EndWeek = 104 } : o).ToList();
        untouched = untouched with { ContractEndWeek = 104 };
        var proposal = Proposals.Preview(world, Renew(aging, key), world.Revision);
        Assert.Empty(proposal.BlockingReasons);
        var terms = proposal.Renewal!;
        Assert.Equal(17, terms.PlayerContracts);
        Assert.DoesNotContain(terms.Contracts, c => c.PlayerId == untouched.Id);
        Assert.Equal(ContractAction.Renew, terms.Contracts.Single(c => c.PlayerId == aging.Id).Chosen);
        Assert.Equal(ContractAction.Release, terms.Contracts.Single(c => c.PlayerId == key.Id).Chosen);
        Proposals.Commit(world, "renew-with-overrides", world.Revision, proposal);

        var club = world.OwnedClub;
        Assert.DoesNotContain(world.Clubs.SelectMany(c => c.Players), p => p.Id == key.Id || p.Id == weakest.Id);
        Assert.Equal(untouched with { SeasonYellows = 0 }, club.Players.Single(p => p.Id == untouched.Id));
        var renewedAging = club.Players.Single(p => p.Id == aging.Id);
        var agingTerms = terms.Contracts.Single(c => c.PlayerId == aging.Id);
        Assert.Equal(52 + 52 * agingTerms.Years, renewedAging.ContractEndWeek);
        Assert.Equal(agingTerms.OfferedWage, renewedAging.WeeklyWage);
        Assert.NotEqual(aging.ContractId, renewedAging.ContractId);
        var obligation = world.Obligations.Single(o => o.Description == $"Player contract {renewedAging.ContractId.Value}");
        Assert.Equal((53, renewedAging.ContractEndWeek, -agingTerms.OfferedWage), (obligation.StartWeek, obligation.EndWeek, obligation.WeeklyAmount));
        // Released players' contracts ended at the season close; nothing is payable afterwards.
        foreach (var gone in new[] { key, weakest })
            Assert.All(world.Obligations.Where(o => o.Description == $"Player contract {gone.ContractId.Value}"), o => Assert.True(o.EndWeek <= 52));

        var record = world.History.Last();
        Assert.Equal(Allocation.StartNextSeason, record.Command.Allocation);
        Assert.Equal(terms.Contracts.ToArray(), record.Renewal!.Contracts.ToArray());
        Assert.Contains(record.Renewal.Contracts, c => c.PlayerId == key.Id && c.Recommended == ContractAction.Renew && c.Chosen == ContractAction.Release);
        Assert.Contains(world.Reviews, r => r.Title == "Season 2 begins" && r.Evidence.Contains("released") && !r.Evidence.Contains("—"));

        SimulationTests.Commit(world, Allocation.PreserveReserve, "plan-2");
        Simulation.AdvanceWeek(world);
        var wages = world.Journal.Where(j => j.Week == 53 && j.Account == WorldFactory.Account(club.Id) && j.Kind == CashKind.Wages).Sum(j => j.Amount);
        Assert.Equal(-club.Players.Sum(p => p.WeeklyWage), wages);
        Assert.Equal(record.Renewal.Contracts.ToArray(), WorldCodec.Clone(world).History.Single(h => h.Renewal is not null).Renewal!.Contracts.ToArray());
    }

    [Fact]
    public void ReleasesThatBreachTheMinimumSquadAreBlocked()
    {
        var world = Expiring();
        var keepers = world.OwnedClub.Players.Where(p => p.Role == Role.Goalkeeper).ToArray();
        var recommended = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision).Renewal!;
        Assert.All(keepers, k => Assert.Equal(ContractAction.Renew, recommended.Contracts.Single(c => c.PlayerId == k.Id).Recommended));
        var proposal = Proposals.Preview(world, Renew(keepers[0]), world.Revision);
        Assert.Contains(proposal.BlockingReasons, r => r.Contains("goalkeepers") && r.Contains("at least 2"));
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "short-of-keepers", world.Revision, proposal));
        Assert.Equal(1, world.Season);

        // Releasing three healthy outfielders from different roles still breaches the 16-player total.
        var club = world.OwnedClub;
        var spare = new[] { Role.Defender, Role.Midfielder, Role.Forward }
            .Select(role => club.Players.Where(p => p.Role == role && recommended.Contracts.Single(c => c.PlayerId == p.Id).Recommended == ContractAction.Renew).First()).ToArray();
        var alreadyReleased = recommended.Contracts.Count(c => c.Recommended == ContractAction.Release);
        var total = Proposals.Preview(world, Renew(spare), world.Revision);
        if (18 - alreadyReleased - 3 < Contracts.MinimumSquad)
            Assert.Contains(total.BlockingReasons, r => r.Contains($"at least {Contracts.MinimumSquad} players"));
        Assert.Contains(Proposals.Preview(world, new(Allocation.StartNextSeason) { ContractOverrides = [new(999999)] }, world.Revision).BlockingReasons,
            r => r.Contains("expiring"));
        Assert.Contains(Proposals.Preview(world, new(Allocation.StartNextSeason) { ContractOverrides = [keepers[0].Id, keepers[0].Id] }, world.Revision).BlockingReasons,
            r => r.Contains("expiring"));
    }

    [Fact]
    public void RenewalsThatPushWagesOverTheLimitAreBlockedAndTheDirectorHoldsRaises()
    {
        var (world, _, _, _, aging, _, _, _) = Controlled();
        var club = world.OwnedClub;
        // Everyone else becomes a young key player, so the director's plan raises the wage bill.
        Set(club, p => p.Id != aging.Id, p => p with { Age = 22, Ability = Contracts.Par(Pyramid.NextDivisions(world)[club.Id]) + 10 });
        var open = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision).Renewal!;
        Assert.Equal(ContractAction.Release, open.Contracts.Single(c => c.PlayerId == aging.Id).Recommended);
        var offer = open.Contracts.Single(c => c.PlayerId == aging.Id).OfferedWage;
        Assert.True(open.RenewedAnnualWages > open.ExpiringAnnualWages - aging.WeeklyWage * 52);

        // A non-expiring star leaves room for the director's plan but not for also keeping the aging player.
        var star = (open.WageLimit - open.AnnualWagesAfter) / 52 - offer / 2;
        Assert.True(star > 0);
        club.Players.Add(new(new(900001), "Stress Keeper", Role.Goalkeeper, 70, 27, star, new(900001), 156));
        var accepted = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        Assert.Empty(accepted.BlockingReasons); Assert.False(accepted.Renewal!.RaisesHeld);
        var kept = Proposals.Preview(world, Renew(aging), world.Revision);
        Assert.Contains(kept.BlockingReasons, r => r.Contains("75%"));
        Assert.True(kept.Renewal!.AnnualWagesAfter > kept.Renewal.WageLimit);

        // With a bigger fixed wage the full plan would breach the limit, so raises are held at current wages.
        club.Players[^1] = club.Players[^1] with { WeeklyWage = star + offer * 2 };
        var held = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        Assert.True(held.Renewal!.RaisesHeld);
        Assert.Empty(held.BlockingReasons);
        Assert.All(held.Renewal.Contracts.Where(c => c.Chosen == ContractAction.Renew), c => Assert.True(c.OfferedWage <= c.CurrentWage));
        Assert.True(held.Renewal.AnnualWagesAfter <= held.Renewal.AnnualWagesBefore);
    }

    [Fact]
    public void ForecastIncludesTheChosenRenewalWages()
    {
        var (world, _, key, _, aging, _, _, _) = Controlled();
        var accepted = Proposals.Preview(world, new(Allocation.StartNextSeason), world.Revision);
        var released = Proposals.Preview(world, Renew(key, aging), world.Revision);
        Assert.Empty(released.BlockingReasons);
        var saving = accepted.Renewal!.Contracts.Single(c => c.PlayerId == key.Id).OfferedWage
            - accepted.Renewal.Contracts.Single(c => c.PlayerId == aging.Id).OfferedWage;
        var week = accepted.Forecast.Points.Single(p => p.Week == 53);
        Assert.Equal(week.KnownNet + saving, released.Forecast.Points.Single(p => p.Week == 53).KnownNet);
        Assert.Equal(accepted.Renewal.AnnualWagesAfter - saving * 52, released.Renewal!.AnnualWagesAfter);
        Assert.NotEqual(accepted.Id, released.Id);
        // Commit rechecks the exact terms: a proposal cannot be replayed with different choices.
        Assert.Throws<InvalidOperationException>(() => Proposals.Commit(world, "swap", world.Revision, accepted with { Command = released.Command }));
    }

    [Fact]
    public void RivalClubsApplyTheDirectorPolicyAndStayViable()
    {
        var world = Expiring();
        var before = world.Clubs.ToDictionary(c => c.Id, c => c.Players.Count);
        SimulationTests.Commit(world, Allocation.StartNextSeason);
        var released = 0;
        foreach (var club in world.Clubs.Where(c => c.Id != world.OwnedClubId))
        {
            Assert.True(club.Players.Count >= Contracts.MinimumSquad);
            foreach (var (role, minimum) in Contracts.Minimum) Assert.True(club.Players.Count(p => p.Role == role) >= minimum);
            Assert.All(club.Players, p => Assert.InRange(p.ContractEndWeek, 104, 208));
            var active = world.Obligations.Where(o => o.ClubId == club.Id && o.Kind == CashKind.Wages && o.StartWeek <= 53 && o.EndWeek >= 53).Sum(o => o.WeeklyAmount);
            Assert.Equal(-club.Players.Sum(p => p.WeeklyWage), active);
            released += before[club.Id] - club.Players.Count;
        }
        Assert.InRange(released, 1, 47 * 2);
        Assert.Contains(world.Reviews, r => r.Title == "Season 2 begins" && r.Evidence.Contains("Rival clubs"));
        // Contracts now expire in different seasons instead of all at once.
        Assert.True(world.Clubs.SelectMany(c => c.Players).Select(p => p.ContractEndWeek).Distinct().Count() >= 2);
    }

    [Fact]
    public void RenewalReplaysIdenticallyAcrossSaveAndLoad()
    {
        var world = Expiring();
        var loaded = WorldCodec.Clone(world);
        foreach (var copy in new[] { world, loaded })
        {
            var proposal = Proposals.Preview(copy, new(Allocation.StartNextSeason), copy.Revision);
            Proposals.Commit(copy, "renew", copy.Revision, proposal);
        }
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
        loaded = WorldCodec.Clone(loaded);
        foreach (var copy in new[] { world, loaded })
        {
            SimulationTests.Commit(copy, Allocation.PreserveReserve, "plan");
            for (var i = 0; i < 6; i++) Simulation.AdvanceWeek(copy);
        }
        Assert.Equal(WorldCodec.Encode(world), WorldCodec.Encode(loaded));
    }

    [Fact]
    public void SchemaSixSaveAtTheRenewalReviewMigratesAndReceivesRecommendations()
    {
        var world = Expiring();
        var legacy = JsonNode.Parse(WorldCodec.Encode(world))!.AsObject();
        legacy["SchemaVersion"] = 6; legacy["SimulationVersion"] = "matchday-6";
        foreach (var record in legacy["History"]!.AsArray())
        {
            record!.AsObject().Remove("Renewal");
            record["Command"]!.AsObject().Remove("ContractOverrides");
        }
        var bytes = Encoding.UTF8.GetBytes(legacy.ToJsonString()); var source = bytes.ToArray();
        var migrated = WorldCodec.Decode(bytes);
        Assert.Equal(source, bytes);
        Assert.Equal(7, migrated.SchemaVersion); Assert.Equal("contracts-7", migrated.SimulationVersion);
        Assert.Equal(CareerStatus.SeasonReview, migrated.Status);
        Assert.All(migrated.History, h => { Assert.Null(h.Renewal); Assert.Empty(h.Command.ContractOverrides); });
        Assert.Equal(world.OwnedClub.Players, migrated.OwnedClub.Players);
        var proposal = Proposals.Preview(migrated, new(Allocation.StartNextSeason), migrated.Revision);
        Assert.Empty(proposal.BlockingReasons);
        Assert.Equal(18, proposal.Renewal!.Contracts.Length);
        Proposals.Commit(migrated, "renew", migrated.Revision, proposal);
        Assert.Equal(2, migrated.Season);
        Assert.Equal(proposal.Renewal.Renewed, migrated.OwnedClub.Players.Count);
    }
}
