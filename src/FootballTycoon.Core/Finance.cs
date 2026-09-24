using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace FootballTycoon.Core;

public static class Finance
{
    public static long EligibleRevenue(Club club, long? nextBroadcast = null, long? nextSponsor = null) =>
        checked(club.HistoricalTickets + club.HistoricalHospitality + club.HistoricalCommercial
            + (nextBroadcast ?? club.AnnualBroadcast) + (nextSponsor ?? club.AnnualSponsor));

    public static long EligibleRevenue(World world, ClubId id)
    {
        var club = world.Clubs.Single(c => c.Id == id);
        if (world.Week < Seasons.Weeks) return EligibleRevenue(club);
        var account = WorldFactory.Account(id);
        bool EligibleTrading(LedgerEntry line)
        {
            if (line.Kind == CashKind.Commercial) return true;
            if (line.Kind is not (CashKind.Tickets or CashKind.Hospitality)) return false;
            if (!line.Reference.StartsWith("fixture/", StringComparison.Ordinal)
                || !int.TryParse(line.Reference.AsSpan("fixture/".Length), out var fixtureId)) return false;
            return world.Fixtures.SingleOrDefault(f => f.Id == new FixtureId(fixtureId))?.Competition == Competition.League;
        }
        var realized = world.Journal.Where(j => j.Account == account && j.Week > world.Week - Seasons.Weeks
            && j.Week <= world.Week && EligibleTrading(j)).Sum(j => j.Amount);
        return checked(realized + club.AnnualBroadcast + club.AnnualSponsor);
    }

    public static long AnnualWages(Club club) => checked(club.Players.Sum(p => p.WeeklyWage) * 52);

    public static void Post(World world, string account, long amount, CashKind kind, string reference)
    {
        if (account == "owner") world.OwnerCash = Money.Add(world.OwnerCash, amount);
        else if (account.StartsWith("club/", StringComparison.Ordinal))
        {
            var club = world.Clubs.Single(c => WorldFactory.Account(c.Id) == account);
            club.Cash = Money.Add(club.Cash, amount);
        }
        world.Journal.Add(new(world.Journal.Count + 1, world.Week, account, amount, kind, reference));
    }

    public static Forecast Forecast(World world, ClubId id, long upfront = 0, long extraWeekly = 0, int extraStarts = 0,
        bool hospitality = false)
    {
        var club = world.Clubs.Single(c => c.Id == id);
        var baseCash = checked(club.Cash - upfront);
        var downsideCash = baseCash;
        var points = ImmutableArray.CreateBuilder<ForecastPoint>();
        points.Add(new(world.Week, baseCash, downsideCash, -upfront));
        for (var week = world.Week + 1; week <= world.Week + 52; week++)
        {
            var known = world.Obligations.Where(o => o.ClubId == id && o.StartWeek <= week && o.EndWeek >= week).Sum(o => o.WeeklyAmount);
            if (week == world.Week + 1) known -= world.Arrears.Where(a => a.ClubId == id).Sum(a => a.Amount);
            if (week >= extraStarts) known = checked(known - extraWeekly);
            var home = world.Fixtures.Any(f => f.Week == week && f.Home == id);
            // Beyond the authored season, no unsigned broadcast/sponsor or invented fixtures finance commitments.
            var earned = Pyramid.TradingBudget(club, club.HistoricalCommercial) / 52;
            if (home) earned = checked(earned + Pyramid.TradingBudget(club, club.HistoricalTickets) / 15 + Pyramid.TradingBudget(club, club.HistoricalHospitality) / 15);
            var levels = club.HospitalityLevel + world.Projects.Count(p => p.ClubId == id && p.CompletionWeek > world.Week && week > p.CompletionWeek)
                + (hospitality && week >= extraStarts ? 1 : 0);
            if (home) earned += levels * 1600000L;
            baseCash = checked(baseCash + known + earned);
            downsideCash = checked(downsideCash + known + Money.Scale(earned, 0.72m));
            points.Add(new(week, baseCash, downsideCash, known));
        }
        var array = points.ToImmutable();
        var low = array.MinBy(p => p.BaseCash)!;
        var down = array.MinBy(p => p.DownsideCash)!;
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{Convert.ToHexString(SHA256.HashData(WorldCodec.Encode(world)))}:{id.Value}:{upfront}:{extraWeekly}:{extraStarts}:{hospitality}")));
        return new(identity, world.Week, 52, array, low.BaseCash, low.Week, down.DownsideCash, down.Week,
            "Signed payment schedule in both cases. Future commercial and scheduled home-match receipts are estimates; downside receives 72% of base. No speculative sales, cup prizes, unsigned sponsorship or borrowing. Dates beyond the current season are illustrative; unsigned renewals and unscheduled next-season fixtures are excluded.");
    }
}

public static class Proposals
{
    public static Proposal Preview(World world, OwnerCommand command, long revision)
    {
        var b = Balance.Load();
        var reasons = ImmutableArray.CreateBuilder<string>();
        var club = world.OwnedClub;
        var upfront = 0L; var weekly = 0L; var total = 0L; var review = world.Week + 4;
        RenewalTerms? renewal = null;
        RecruitmentTerms? recruitment = null;
        var title = ""; var executive = "CEO · Mara Ellis"; var uncertainty = "";
        if (revision != world.Revision) reasons.Add("The world changed. Refresh the proposal.");
        if (!Enum.IsDefined(command.Allocation)) reasons.Add("Unknown allocation.");
        if (command.Amount != 0 && command.Allocation != Allocation.InjectCapital) reasons.Add("This quote does not accept a custom amount.");
        if (world.Status is CareerStatus.LostControl or CareerStatus.PrototypeComplete) reasons.Add("This career checkpoint is complete.");
        if (world.Status == CareerStatus.Acquisition && command.Allocation != Allocation.Acquire) reasons.Add("Acquire the club first.");
        if (world.Status == CareerStatus.SeasonReview && command.Allocation is not (Allocation.StartNextSeason or Allocation.InjectCapital)) reasons.Add("Review and renew the next season before choosing its capital plan.");
        if (world.Status != CareerStatus.Acquisition && command.Allocation == Allocation.Acquire) reasons.Add("Only one acquisition is allowed.");
        if (world.Status == CareerStatus.Administration && command.Allocation != Allocation.InjectCapital) reasons.Add("Administration restricts new spending. Fund outstanding payroll first.");
        if (command.Allocation is Allocation.Hospitality or Allocation.Recruitment or Allocation.PreserveReserve && world.AllocationChosen)
            reasons.Add("This season's capital plan is already committed.");
        if (RecruitmentMarket.IsMidseason(command.Allocation))
        {
            if (!world.AllocationChosen) reasons.Add("Choose the season's opening capital plan first.");
            if (!RecruitmentMarket.WindowOpen(world) && RecruitmentMarket.Window(world) is var (opens, closes))
                reasons.Add($"{RecruitmentMarket.WindowName(world)} window approvals run {Calendar.Day(Seasons.StartWeek(world) + opens)} to {Calendar.Day(Seasons.StartWeek(world) + closes)}.");
            if (RecruitmentMarket.Decided(world)) reasons.Add("This season's midseason decision is already recorded.");
        }
        switch (command.Allocation)
        {
            case Allocation.Acquire:
                title = $"Acquire {club.Name}"; total = b.PurchasePrice;
                if (world.OwnerCash < total) reasons.Add("Insufficient personal funds.");
                uncertainty = "Purchase pays the seller. Club cash is unchanged. This prototype has one acquisition at the asking price and no opening debt.";
                break;
            case Allocation.PreserveReserve:
                title = "Protect the cash reserve";
                uncertainty = "Keep the existing squad and facilities. Preserving cash cannot guarantee survival or promotion.";
                break;
            case Allocation.Hospitality:
                title = "Expand hospitality"; upfront = b.HospitalityCost; weekly = b.HospitalityWeeklyUpkeep;
                review = world.Week + b.HospitalityBuildWeeks;
                total = checked(upfront + weekly * Math.Max(0, Seasons.EndWeek(world) + Seasons.Weeks - review));
                if (world.Projects.Any(p => p.ClubId == club.Id && p.CompletionWeek > world.Week)) reasons.Add("Only one construction project may run at a time.");
                if (club.HospitalityLevel >= 3) reasons.Add("This facility is at its three-step limit.");
                uncertainty = $"Upfront construction payment; 28-week build. Extra receipts depend on home matches and demand. Upkeep begins the week after opening and is committed through {Calendar.FullDay(Seasons.EndWeek(world) + Seasons.Weeks)}. Recoverable construction value: 40%; cancellation UI is deferred.";
                break;
            case Allocation.Recruitment:
            case Allocation.MidseasonRecruitment:
            case Allocation.MidseasonValue:
                title = command.Allocation == Allocation.Recruitment ? "Authorize a forward search"
                    : command.Allocation == Allocation.MidseasonValue ? "Back the lower-cost forward" : "Back the first-choice forward";
                recruitment = RecruitmentMarket.Recommend(world, command.Allocation);
                upfront = recruitment?.FeeCeiling ?? b.TransferFeeCeiling; weekly = recruitment?.WeeklyWage ?? b.RecruitWeeklyWage;
                executive = "Sporting director · Jonas Reed";
                total = checked(upfront + weekly * (Seasons.EndWeek(world) + Seasons.Weeks - world.Week - 1));
                review = world.Week + 1;
                if (command.Allocation == Allocation.Recruitment && world.Week > Seasons.StartWeek(world) + 2) reasons.Add("This season's opening recruitment window has closed.");
                if (recruitment is null) reasons.Add("No suitable surplus forward is available for this recommendation.");
                if (world.Negotiations.Count > 0) reasons.Add("Let the current negotiation finish before authorizing another.");
                if (checked(Finance.AnnualWages(club) + weekly * 52) > Money.Scale(Finance.EligibleRevenue(world, club.Id), b.WageLimit))
                    reasons.Add("The proposed wages exceed 75% of eligible recurring revenue.");
                uncertainty = "Approval sets a maximum fee and wage mandate; no cash is deducted now. Jonas negotiates next week. The seller may refuse and affordability is checked again. The ceiling forecast reserves the full potential commitment. A signing adds competition for places; the manager selects the team. No resale proceeds are assumed and future development is uncertain.";
                break;
            case Allocation.MidseasonWait:
                title = "Keep the squad for the run-in";
                executive = "Sporting director · Jonas Reed";
                uncertainty = "No new fee or wage commitment. Keep the current squad and reserve; this closes the midseason review for this season. Existing contracts continue and sporting results remain uncertain.";
                break;
            case Allocation.StartNextSeason:
                title = $"Renew and start season {world.Season + 1}";
                if (world.Status != CareerStatus.SeasonReview || world.Week != Seasons.EndWeek(world) || world.Season >= Seasons.PlayableSeasons)
                    reasons.Add("A season-end review is required; this milestone supports three seasons.");
                else
                {
                    renewal = Seasons.Terms(world);
                    review = world.Week + 4;
                    total = renewal.RenewedAnnualWages + renewal.AnnualOperations;
                }
                uncertainty = "No upfront payment. Annual broadcast and sponsorship renew at the published next-tier rates. Expiring player and operating contracts extend for one season at unchanged rates. Existing wages, facility upkeep and arrears carry forward; no cash or personal reserve resets. Trading demand follows the new tier; existing wages do not automatically fall after relegation. Negotiated renewals remain a future system. Choose a new capital plan after confirmation.";
                break;
            case Allocation.InjectCapital:
                title = "Inject owner capital"; upfront = -command.Amount; total = command.Amount;
                if (command.Amount <= 0 || command.Amount > world.OwnerCash) reasons.Add("Injection must be positive and within your remaining personal reserve.");
                uncertainty = "Transfers personal funds into the club. This is invested capital, excluded from recurring revenue.";
                break;
        }
        // A malformed amount must never overflow or influence the preview calculation.
        if (command.Allocation == Allocation.InjectCapital && (command.Amount <= 0 || command.Amount > world.OwnerCash)) upfront = 0;
        var forecastWorld = world;
        if (renewal is not null) { forecastWorld = WorldCodec.Clone(world); Seasons.StartNext(forecastWorld); }
        var forecast = Finance.Forecast(forecastWorld, club.Id, upfront, weekly,
            command.Allocation == Allocation.Hospitality ? review + 1 : world.Week + 2, command.Allocation == Allocation.Hospitality);
        if (command.Allocation == Allocation.Hospitality || RecruitmentMarket.IsSigning(command.Allocation))
        {
            if (club.Cash < upfront) reasons.Add("The club cannot cover the upfront cash commitment.");
            if (forecast.LowestDownside < 0) reasons.Add("The downside forecast cannot cover essential obligations.");
            else if (forecast.LowestDownside < world.ReserveTarget && !command.ReserveException)
                reasons.Add("The downside forecast crosses the owner reserve. An explicit reserve exception is required.");
        }
        var proposalId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{Convert.ToHexString(SHA256.HashData(WorldCodec.Encode(world)))}:{(int)command.Allocation}:{command.Amount}:{command.ReserveException}")));
        return new(proposalId, world.Revision, command, title, executive, upfront, weekly, total, review, forecast, reasons.ToImmutable(), uncertainty) { Renewal = renewal, Recruitment = recruitment };
    }

    // Caller supplies a private candidate aggregate and publishes it only when persistence succeeds.
    public static CommitReceipt Commit(World world, string commandId, long expectedRevision, Proposal supplied)
    {
        if (string.IsNullOrWhiteSpace(commandId) || commandId.Length > 128) throw new InvalidOperationException("A bounded command ID is required.");
        if (world.Commands.TryGetValue(commandId, out var prior)) return prior;
        if (expectedRevision != world.Revision || supplied.Revision != world.Revision) throw new InvalidOperationException("Stale proposal. Refresh the terms.");
        var proposal = Preview(world, supplied.Command, expectedRevision);
        if (proposal.Id != supplied.Id) throw new InvalidOperationException("Proposal identity does not match these terms.");
        if (!proposal.BlockingReasons.IsEmpty) throw new InvalidOperationException(string.Join(" ", proposal.BlockingReasons));
        var b = Balance.Load();
        var club = world.OwnedClub;
        var reference = $"command/{commandId}";
        switch (proposal.Command.Allocation)
        {
            case Allocation.Acquire:
                Finance.Post(world, "owner", -b.PurchasePrice, CashKind.Purchase, reference);
                Finance.Post(world, "seller", b.PurchasePrice, CashKind.Purchase, reference);
                world.OwnerInvested = b.PurchasePrice;
                world.Status = CareerStatus.Active;
                world.Decisions.Add(new(new(1), 0, true, "Choose the opening capital plan"));
                break;
            case Allocation.StartNextSeason:
                Seasons.StartNext(world);
                break;
            case Allocation.InjectCapital:
                Finance.Post(world, "owner", -proposal.Command.Amount, CashKind.Injection, reference);
                Finance.Post(world, WorldFactory.Account(club.Id), proposal.Command.Amount, CashKind.Injection, reference);
                world.OwnerInvested = Money.Add(world.OwnerInvested, proposal.Command.Amount);
                break;
            case Allocation.Hospitality:
                Finance.Post(world, WorldFactory.Account(club.Id), -proposal.UpfrontCash, CashKind.Construction, reference);
                world.Projects.Add(new(new(world.Projects.Count + 1), club.Id, world.Week, proposal.ReviewWeek,
                    proposal.UpfrontCash, Money.Scale(proposal.UpfrontCash, 0.4m), proposal.Forecast.Id));
                WorldFactory.AddObligation(world, club.Id, proposal.ReviewWeek + 1, Seasons.EndWeek(world) + Seasons.Weeks, -proposal.WeeklyCost, CashKind.Operations, "Hospitality service and upkeep");
                break;
            case Allocation.Recruitment:
            case Allocation.MidseasonRecruitment:
            case Allocation.MidseasonValue:
                var target = proposal.Recruitment!.Player;
                var seller = world.Clubs.Single(c => c.Players.Any(p => p.Id == target.Id));
                var windowEnd = Seasons.StartWeek(world) + (proposal.Command.Allocation == Allocation.Recruitment ? 3 : RecruitmentMarket.Window(world).Closes + 1);
                world.Negotiations.Add(new(world.History.Count + 1, seller.Id, target.Id, Math.Min(world.Week + 2, windowEnd), proposal.UpfrontCash, proposal.WeeklyCost, proposal.Forecast.Id));
                break;
        }
        if (proposal.Command.Allocation is Allocation.Hospitality or Allocation.Recruitment or Allocation.PreserveReserve)
        {
            world.AllocationChosen = true;
            world.Decisions = world.Decisions.Select(d => !d.Resolved && d.Required ? d with { Resolved = true } : d).ToList();
        }
        world.History.Add(new(world.Week, proposal.Command, proposal.Executive, proposal.Title, proposal.Forecast) { Recruitment = proposal.Recruitment });
        world.Revision++;
        var receipt = new CommitReceipt(commandId, proposal.Id, world.Revision, proposal.Title);
        world.Commands.Add(commandId, receipt);
        WorldFactory.Validate(world);
        return receipt;
    }
}
