using System.Collections.Immutable;

namespace FootballTycoon.Core;

public sealed record AdvanceResult(int Week, string StopReason, long Revision);
public sealed record TableRow(ClubId ClubId, string Name, int Played, int Won, int Drawn, int Lost, int GoalsFor, int GoalsAgainst, int Points, int Lot);

public static class Simulation
{
    public static AdvanceResult AdvanceWeek(World world)
    {
        if (world.Status is CareerStatus.Acquisition or CareerStatus.LostControl or CareerStatus.PrototypeComplete or CareerStatus.SeasonReview)
            return new(world.Week, world.Status.ToString(), world.Revision);
        if (world.Status == CareerStatus.Active && world.Decisions.Any(d => d.Required && !d.Resolved && d.DueWeek <= world.Week + 1))
            return new(world.Week, "Owner decision required", world.Revision);
        world.Week++;
        world.Phase = Phase.Payments;
        Settle(world);
        world.Phase = Phase.Negotiation;
        Negotiate(world);
        world.Phase = Phase.Matches;
        foreach (var fixture in world.Fixtures.Where(f => f.Week == world.Week).OrderBy(f => f.Id.Value)) Resolve(world, fixture);
        Cups.AdvanceDraw(world);
        world.Phase = Phase.Development;
        foreach (var project in world.Projects.Where(p => p.CompletionWeek == world.Week))
        {
            world.Clubs.Single(c => c.Id == project.ClubId).HospitalityLevel++;
            world.Reviews.Add(new(world.Week, "Hospitality opens", "Mara Ellis delivered the expansion. Extra receipts now depend on actual home dates and demand; construction cost is already paid.", project.ForecastId));
        }
        world.Phase = Phase.Reporting;
        var (windowOpens, windowCloses) = RecruitmentMarket.Window(world);
        if (world.Week == Seasons.StartWeek(world) + windowOpens)
            world.Reviews.Add(new(world.Week, $"{RecruitmentMarket.WindowName(world)} transfer window opens",
                $"Jonas Reed has reviewed the forward line. Visit Football to compare his first choice, a lower-cost alternative, or keeping the squad. Approve by {Calendar.Day(Seasons.StartWeek(world) + windowCloses)}; continuing beyond the window keeps the squad without a new commitment.", null));
        if (world.Status == CareerStatus.Administration && world.AdministrationWeek is { } start && world.Week >= start + 4)
        {
            world.Status = CareerStatus.LostControl;
            world.Reviews.Add(new(world.Week, "Control lost to creditors", "Payroll remained unpaid after four weeks. This prototype stops ownership here; the full creditor rescue and exit report remain a production gate.", null));
        }
        if (world.Week % 4 == 0)
        {
            var history = world.History.LastOrDefault(h => h.Command.Allocation is Allocation.Hospitality or Allocation.Recruitment or Allocation.PreserveReserve);
            var point = history?.OriginalForecast.Points.FirstOrDefault(p => p.Week == world.Week);
            world.Reviews.Add(new(world.Week, "Owner review", point is null ? "No opening allocation forecast to compare."
                : $"{history!.Intent}: club cash {Money.Format(world.OwnedClub.Cash)} versus original base {Money.Format(point.BaseCash)} and downside {Money.Format(point.DownsideCash)}. Match receipts vary with results and demand; an allocation cannot guarantee a result.", history?.OriginalForecast.Id));
        }
        if (world.Week == Seasons.EndWeek(world) && world.Status != CareerStatus.LostControl)
        {
            Seasons.Close(world);
            world.Status = world.Season == Seasons.PlayableSeasons ? CareerStatus.PrototypeComplete : CareerStatus.SeasonReview;
            world.Reviews.Add(new(world.Week, $"Season {world.Season} complete",
                world.Status == CareerStatus.SeasonReview ? "Review the final table and cash movements, then approve the next season's renewal terms. No time advances during the review."
                : "The three-season milestone is complete. All season reports and original decisions remain available.", null));
        }
        world.Phase = Phase.Decisions;
        world.Revision++;
        WorldFactory.Validate(world);
        return new(world.Week, world.Status == CareerStatus.Active ? (world.Week % 4 == 0 ? "Monthly review" : "Week completed") : world.Status.ToString(), world.Revision);
    }

    private static void Settle(World world)
    {
        foreach (var club in world.Clubs.OrderBy(c => c.Id.Value))
        {
            var due = world.Obligations.Where(o => o.ClubId == club.Id && o.StartWeek <= world.Week && o.EndWeek >= world.Week).OrderBy(o => o.Id.Value).ToArray();
            foreach (var receipt in due.Where(o => o.WeeklyAmount > 0))
                Finance.Post(world, WorldFactory.Account(club.Id), receipt.WeeklyAmount, receipt.Kind, $"obligation/{receipt.Id.Value}/{world.Week}");
            Finance.Post(world, WorldFactory.Account(club.Id), Money.Scale(Pyramid.TradingBudget(club, club.HistoricalCommercial) / 52,
                (85 + RandomStreams.Next(world, $"commercial/{club.Id.Value}", 31)) / 100m), CashKind.Commercial, $"commercial/{world.Week}");
            foreach (var arrear in world.Arrears.Where(a => a.ClubId == club.Id).OrderBy(a => a.Week).ThenBy(a => a.ObligationId.Value).ToArray())
            {
                if (club.Cash < arrear.Amount) continue;
                var obligation = world.Obligations.Single(o => o.Id == arrear.ObligationId);
                Finance.Post(world, WorldFactory.Account(club.Id), -arrear.Amount, obligation.Kind, $"arrear/{arrear.ObligationId.Value}/{arrear.Week}");
                world.Arrears.Remove(arrear);
            }
            foreach (var obligation in due.Where(o => o.WeeklyAmount < 0))
            {
                var amount = -obligation.WeeklyAmount;
                if (club.Cash < amount)
                {
                    world.Arrears.Add(new(obligation.Id, club.Id, world.Week, amount));
                    if (club.Id == world.OwnedClubId && world.Status == CareerStatus.Active)
                    {
                        world.Status = CareerStatus.Administration;
                        world.AdministrationWeek = world.Week;
                        world.Reviews.Add(new(world.Week, "Payment failure · administration", $"{obligation.Description} could not be paid. Four weeks to clear arrears using finite personal capital. New spending is restricted.", null));
                    }
                    continue;
                }
                Finance.Post(world, WorldFactory.Account(club.Id), -amount, obligation.Kind, $"obligation/{obligation.Id.Value}/{world.Week}");
            }
        }
        if (world.Status == CareerStatus.Administration && !world.Arrears.Any(a => a.ClubId == world.OwnedClubId))
        {
            world.Status = CareerStatus.Active; world.AdministrationWeek = null;
            world.Reviews.Add(new(world.Week, "Arrears cleared", "All outstanding obligations have been paid. Spending restrictions are lifted.", null));
        }
    }

    private static void Negotiate(World world)
    {
        foreach (var bid in world.Negotiations.OrderBy(n => n.DecisionId).ToArray())
        {
            var seller = world.Clubs.Single(c => c.Id == bid.Seller);
            var player = seller.Players.SingleOrDefault(p => p.Id == bid.PlayerId);
            var buyer = world.OwnedClub;
            var history = world.History.Single(h => h.OriginalForecast.Id == bid.ForecastId);
            var forecast = Finance.Forecast(world, buyer.Id, bid.FeeCeiling, bid.WeeklyWage, world.Week + 1);
            var minimum = history.Command.ReserveException ? 0 : world.ReserveTarget;
            var canSign = world.Week <= bid.ExpiryWeek && world.Status == CareerStatus.Active && player is not null
                && seller.Players.Count(p => p.Role == Role.Forward) > 2
                && (!RecruitmentMarket.IsMidseason(history.Command.Allocation) || RecruitmentMarket.Window(world) is var (opens, closes)
                    && world.Week - Seasons.StartWeek(world) > opens && world.Week - Seasons.StartWeek(world) <= closes + 1)
                && buyer.Cash >= bid.FeeCeiling && forecast.LowestDownside >= minimum
                && checked(Finance.AnnualWages(buyer) + bid.WeeklyWage * 52) <= Money.Scale(Finance.EligibleRevenue(world, buyer.Id), Balance.Load().WageLimit);
            var accepted = canSign && RandomStreams.Next(world, $"negotiation/{bid.DecisionId}", 100) < 70;
            if (accepted)
            {
                Finance.Post(world, WorldFactory.Account(buyer.Id), -bid.FeeCeiling, CashKind.Transfer, $"transfer/{bid.DecisionId}");
                Finance.Post(world, WorldFactory.Account(seller.Id), bid.FeeCeiling, CashKind.Transfer, $"transfer/{bid.DecisionId}");
                seller.Players.Remove(player!);
                var contractId = new ContractId(10000 + world.Obligations.Count + 1);
                var contractEnd = Seasons.EndWeek(world) + Seasons.Weeks;
                buyer.Players.Add(player! with { WeeklyWage = bid.WeeklyWage, ContractId = contractId, ContractEndWeek = contractEnd });
                world.Obligations = world.Obligations.Select(o => o.ClubId == seller.Id && o.Description == $"Player contract {player!.ContractId.Value}"
                    ? o with { EndWeek = world.Week } : o).ToList();
                WorldFactory.AddObligation(world, buyer.Id, world.Week + 1, contractEnd, -bid.WeeklyWage, CashKind.Wages, $"Player contract {contractId.Value}");
            }
            world.Reviews.Add(new(world.Week, accepted ? "Forward signed" : "Recruitment closed without a signing",
                accepted ? $"Jonas Reed signed {player!.Name} from {seller.Name} for {Money.Format(bid.FeeCeiling)}. Wages: {Money.Format(bid.WeeklyWage)}/week from next week through {Calendar.FullDay(Seasons.EndWeek(world) + Seasons.Weeks)}. Selection remains the manager's decision."
                : "The negotiation failed or no longer met its deadline, reserve, availability or wage checks. The fee was not spent; the existing squad continues.", bid.ForecastId));
            world.Negotiations.Remove(bid);
        }
    }

    private static void Resolve(World world, Fixture fixture)
    {
        var home = world.Clubs.Single(c => c.Id == fixture.Home);
        var away = world.Clubs.Single(c => c.Id == fixture.Away);
        static Player[] Select(Club club) => club.Players.GroupBy(p => p.Role).SelectMany(group => group.OrderByDescending(p => p.Ability)
            .ThenBy(p => p.Id.Value).Take(group.Key == Role.Goalkeeper ? 1 : group.Key == Role.Forward ? 2 : 4)).Take(11).ToArray();
        var ht = Select(home); var at = Select(away);
        var hStrength = ht.Average(p => p.Ability); var aStrength = at.Average(p => p.Ability);
        var hg = 0; var ag = 0; var hs = 0; var @as = 0;
        var moments = ImmutableArray.CreateBuilder<Moment>();
        var stream = $"match/{fixture.Id.Value}";
        for (var segment = 0; segment < 6; segment++)
        {
            var hChances = 1 + RandomStreams.Next(world, stream, 3);
            var aChances = 1 + RandomStreams.Next(world, stream, 3);
            hs += hChances; @as += aChances;
            for (var chance = 0; chance < hChances; chance++)
                if (RandomStreams.Next(world, stream, 100) < Math.Clamp(12 + (hStrength - aStrength) * 0.65 + 3, 4, 32))
                {
                    hg++; var scorer = ht.Where(p => p.Role != Role.Goalkeeper).ToArray()[RandomStreams.Next(world, stream, ht.Length - 1)];
                    moments.Add(new(segment * 15 + 1 + RandomStreams.Next(world, stream, 15), home.Id, scorer.Id, $"{scorer.Name} scores for {home.Name}."));
                }
            for (var chance = 0; chance < aChances; chance++)
                if (RandomStreams.Next(world, stream, 100) < Math.Clamp(12 + (aStrength - hStrength) * 0.65, 4, 32))
                {
                    ag++; var scorer = at.Where(p => p.Role != Role.Goalkeeper).ToArray()[RandomStreams.Next(world, stream, at.Length - 1)];
                    moments.Add(new(segment * 15 + 1 + RandomStreams.Next(world, stream, 15), away.Id, scorer.Id, $"{scorer.Name} scores for {away.Name}."));
                }
        }
        var demand = (80 + RandomStreams.Next(world, $"attendance/{home.Id.Value}", 36)) / 100m;
        var tickets = Money.Scale(Pyramid.TradingBudget(home, home.HistoricalTickets) / 15, demand);
        var hospitality = Money.Scale(Pyramid.TradingBudget(home, home.HistoricalHospitality) / 15 + home.HospitalityLevel * 1600000, demand);
        Finance.Post(world, WorldFactory.Account(home.Id), tickets, CashKind.Tickets, $"fixture/{fixture.Id.Value}");
        Finance.Post(world, WorldFactory.Account(home.Id), hospitality, CashKind.Hospitality, $"fixture/{fixture.Id.Value}");
        var extraTime = false; string? shootout = null; var winner = hg > ag ? home.Id : ag > hg ? away.Id : default;
        if (fixture.Competition == Competition.Cup && hg == ag)
        {
            extraTime = true;
            var homeExtra = RandomStreams.Next(world, stream + "/extra", 100) < Math.Clamp(18 + (int)(hStrength - aStrength), 8, 35);
            var awayExtra = RandomStreams.Next(world, stream + "/extra", 100) < Math.Clamp(18 + (int)(aStrength - hStrength), 8, 35);
            if (homeExtra)
            {
                hg++; hs++; var scorer = ht.Where(p => p.Role != Role.Goalkeeper).ToArray()[RandomStreams.Next(world, stream + "/extra", ht.Length - 1)];
                moments.Add(new(91 + RandomStreams.Next(world, stream + "/extra", 30), home.Id, scorer.Id, $"{scorer.Name} scores in extra time for {home.Name}."));
            }
            if (awayExtra)
            {
                ag++; @as++; var scorer = at.Where(p => p.Role != Role.Goalkeeper).ToArray()[RandomStreams.Next(world, stream + "/extra", at.Length - 1)];
                moments.Add(new(91 + RandomStreams.Next(world, stream + "/extra", 30), away.Id, scorer.Id, $"{scorer.Name} scores in extra time for {away.Name}."));
            }
            if (hg == ag)
            {
                var homePenalties = 3 + RandomStreams.Next(world, stream + "/penalties", 3);
                var awayPenalties = 3 + RandomStreams.Next(world, stream + "/penalties", 3);
                if (homePenalties == awayPenalties) { if (RandomStreams.Next(world, stream + "/penalties", 2) == 0) homePenalties++; else awayPenalties++; }
                shootout = $"{homePenalties}–{awayPenalties}";
                winner = homePenalties > awayPenalties ? home.Id : away.Id;
            }
            else winner = hg > ag ? home.Id : away.Id;
        }
        var supportWinner = winner == home.Id ? 1 : winner == away.Id ? -1 : Math.Sign(hg - ag);
        home.Support = Math.Clamp(home.Support + supportWinner, 0, 100);
        away.Support = Math.Clamp(away.Support - supportWinner, 0, 100);
        world.Results.Add(new(fixture.Id, world.Week, home.Id, away.Id, hg, ag, hs, @as, (int)(tickets / 2200),
            checked(tickets + hospitality), moments.OrderBy(m => m.Minute).ToImmutableArray())
            { Winner = winner, ExtraTime = extraTime, Shootout = shootout });
    }

    public static ImmutableArray<TableRow> Table(World world, int division, int? season = null)
    {
        var start = ((season ?? world.Season) - 1) * Seasons.Weeks;
        var leagueIds = world.Fixtures.Where(f => f.Competition == Competition.League && f.Week > start && f.Week <= start + Seasons.Weeks).Select(f => f.Id).ToHashSet();
        var results = world.Results.Where(r => leagueIds.Contains(r.FixtureId)).ToArray();
        var participants = world.Fixtures.Where(f => f.Competition == Competition.League && f.Week > start && f.Week <= start + Seasons.Weeks && f.Division == division)
            .SelectMany(f => new[] { f.Home, f.Away }).ToHashSet();
        // Older schemas had fixed divisions and no fixture division tag.
        var rows = world.Clubs.Where(c => participants.Count > 0 ? participants.Contains(c.Id) : c.Division == division).Select(club =>
        {
            var games = results.Where(r => r.Home == club.Id || r.Away == club.Id).ToArray();
            int Goals(MatchResult r, bool own) => (r.Home == club.Id) == own ? r.HomeGoals : r.AwayGoals;
            var wins = games.Count(r => Goals(r, true) > Goals(r, false));
            var draws = games.Count(r => Goals(r, true) == Goals(r, false));
            return new TableRow(club.Id, club.Name, games.Length, wins, draws, games.Length - wins - draws,
                games.Sum(r => Goals(r, true)), games.Sum(r => Goals(r, false)), wins * 3 + draws, club.Lot);
        });
        return rows.GroupBy(r => (r.Points, Difference: r.GoalsFor - r.GoalsAgainst, r.GoalsFor))
            .OrderByDescending(g => g.Key.Points).ThenByDescending(g => g.Key.Difference).ThenByDescending(g => g.Key.GoalsFor)
            .SelectMany(group =>
            {
                var tied = group.Select(r => r.ClubId).ToHashSet();
                (int Points, int Difference) Head(TableRow row)
                {
                    var games = results.Where(r => tied.Contains(r.Home) && tied.Contains(r.Away) && (r.Home == row.ClubId || r.Away == row.ClubId));
                    var points = 0; var difference = 0;
                    foreach (var game in games)
                    {
                        var diff = game.Home == row.ClubId ? game.HomeGoals - game.AwayGoals : game.AwayGoals - game.HomeGoals;
                        points += diff > 0 ? 3 : diff == 0 ? 1 : 0; difference += diff;
                    }
                    return (points, difference);
                }
                return group.OrderByDescending(r => Head(r).Points).ThenByDescending(r => Head(r).Difference).ThenBy(r => r.Lot).ThenBy(r => r.ClubId.Value);
            }).ToImmutableArray();
    }
}
