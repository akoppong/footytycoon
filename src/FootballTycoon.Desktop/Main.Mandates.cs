using Godot;
using FootballTycoon.Core;

namespace FootballTycoon.Desktop;

public partial class Main
{
    private MandateState? mandateDraft;
    private MandateProposal? mandateProposal;
    private string? mandateFocus;
    private bool mandateHistoryExpanded;

    private static string ManagerName(ManagerMandate mode) => mode == ManagerMandate.Development ? "Development" : "Readiness";
    private static string DirectorName(DirectorMandate mode) => mode == DirectorMandate.LowerCostValue ? "Lower-cost value" : "Current recommendation";
    private static string MandateNames(ManagerMandate manager, DirectorMandate director) => $"Manager: {ManagerName(manager)} · Director: {DirectorName(director)}";
    private string PersonName(PersonId id) => view.PlayerNames.GetValueOrDefault(id, $"Player {id.Value}");

    private void ClearMandateEditor() { mandateDraft = null; mandateProposal = null; mandateFocus = null; }

    private void BeginMandates()
    {
        selected = null; confirming = false; auxiliary = ""; workspace = "People";
        mandateDraft = view.Mandates.Current; mandateProposal = null; mandateFocus = "Manager: Readiness"; Render();
    }

    private void CancelMandates()
    {
        ClearMandateEditor(); workspace = "People"; mandateFocus = "Change staff mandates";
        status.Text = "Mandate change cancelled. Your saved mandates remain in force."; Render();
    }

    private Button MandateButton(string title, Action action)
    {
        var button = Button(title, action);
        if (mandateFocus == title) { mandateFocus = null; button.CallDeferred(Control.MethodName.GrabFocus); }
        return button;
    }

    private void StaffMandates(VBoxContainer staff)
    {
        staff.AddChild(Label("Staff mandates", 22));
        staff.AddChild(Label(MandateNames(view.Mandates.Current.Manager, view.Mandates.Current.Director), 16));
        staff.AddChild(Label("Callum selects the lineup. Jonas recommends free agents; you still approve every approach. Paid transfers, renewals and academy intake keep their existing rules.", 14));
        var change = MandateButton("Change staff mandates", BeginMandates); staff.AddChild(change);
        if (view.Status != CareerStatus.Active && !(view.Status == CareerStatus.SeasonReview && view.Season < Seasons.PlayableSeasons))
        {
            const string reason = "Mandate changes are available during active ownership or a nonterminal season review.";
            Unavailable(change, reason); staff.AddChild(Label(reason, 13));
        }
    }

    private void RenderMandates()
    {
        var draft = mandateDraft!;
        var card = Card(); card.AddChild(Caption(mandateProposal is null ? "STAFF MANDATES · TIME IS HELD" : "FINAL MANDATE CHANGE · NOT YET SAVED"));
        card.AddChild(Label(mandateProposal is null ? "What should your staff prioritize?" : "Confirm your instructions", 28));
        card.AddChild(Label("Saved now · " + MandateNames(view.Mandates.Current.Manager, view.Mandates.Current.Director), 16));
        if (mandateProposal is { } proposal)
        {
            card.AddChild(Label("Proposed · " + MandateNames(proposal.Command.Manager, proposal.Command.Director), 20));
            card.AddChild(Label(proposal.Effect, 14));
            card.AddChild(Label("Existing approved commitments", 22));
            foreach (var commitment in proposal.PendingCommitments) card.AddChild(Label(commitment, 14));
            if (proposal.PendingCommitments.IsEmpty) card.AddChild(Label("No pending approved approaches. Existing signed contracts stay in force.", 14));
            foreach (var reason in proposal.BlockingReasons) card.AddChild(Label("BLOCKED · " + reason, 14));
            var confirm = MandateButton("Confirm mandate change", CommitMandates); Primary(confirm); card.AddChild(confirm);
            if (!proposal.BlockingReasons.IsEmpty) Unavailable(confirm, string.Join("\n", proposal.BlockingReasons));
            card.AddChild(MandateButton("Edit mandate choices", () => { mandateProposal = null; mandateFocus = "Review mandate change"; Render(); }));
        }
        else
        {
            card.AddChild(Label("Callum Price · Manager", 22));
            card.AddChild(Label("Readiness keeps current selection. Development can start up to two available outfield players aged 23 or younger in place of players over 23 in their natural role, each at a cost of at most five ability points. No suitable alternative means no replacement; extra appearances do not guarantee growth.", 14));
            foreach (var mode in new[] { ManagerMandate.Readiness, ManagerMandate.Development })
            {
                var title = "Manager: " + ManagerName(mode);
                var button = MandateButton(title, () => { mandateDraft = draft with { Manager = mode }; mandateFocus = title; Render(); });
                button.ToggleMode = true; button.ButtonPressed = draft.Manager == mode; card.AddChild(button);
            }
            card.AddChild(Label("Jonas Reed · Sporting director", 22));
            card.AddChild(Label("Current recommendation prioritizes squad shortages, then current ability. Lower-cost value can accept up to five fewer ability points in the same role for a lower weekly wage or total wage commitment, with neither cost increasing. A shorter contract also means less future cover. If none qualifies, the current recommendation stays.", 14));
            foreach (var mode in new[] { DirectorMandate.CurrentRecommendation, DirectorMandate.LowerCostValue })
            {
                var title = "Director: " + DirectorName(mode);
                var button = MandateButton(title, () => { mandateDraft = draft with { Director = mode }; mandateFocus = title; Render(); });
                button.ToggleMode = true; button.ButtonPressed = draft.Director == mode; card.AddChild(button);
            }
            card.AddChild(Label("Director priority applies only to free-agent recommendations. Every approach still needs your approval and can fail existing checks or be refused. Paid transfers, renewals and academy intake are unchanged.", 14));
            card.AddChild(Label("Changing a mandate costs nothing immediately. It applies prospectively and leaves approved offers, required capital decisions and the week unchanged.", 14));
            var review = MandateButton("Review mandate change", () => Start(async () =>
            {
                mandateFocus = "Review mandate change";
                mandateProposal = await session.PreviewMandatesAsync(new(Guid.NewGuid().ToString("N"), view.Revision, draft.Manager, draft.Director));
                mandateFocus = mandateProposal.BlockingReasons.IsEmpty ? "Confirm mandate change" : "Edit mandate choices";
                return "Review both instructions and existing commitments before confirming.";
            }));
            Primary(review); card.AddChild(review);
        }
        card.AddChild(MandateButton("Cancel mandate change", CancelMandates));
    }

    private void CommitMandates()
    {
        if (mandateProposal is not { } proposal || !proposal.BlockingReasons.IsEmpty) return;
        mandateFocus = "Confirm mandate change";
        Start(async () =>
        {
            var receipt = await session.CommitMandatesAsync(proposal);
            view = await session.QueryAsync();
            ClearMandateEditor(); workspace = "People"; mandateFocus = "Change staff mandates";
            return receipt.ProposalId == proposal.Id ? $"Saved locally · {receipt.Summary}"
                : "This command ID already belongs to an earlier action. Its saved receipt was returned; the requested mandate change was not applied. Review the current mandates before trying again.";
        });
    }

    private void RecommendationFacts(VBoxContainer card, RecommendationEvidence evidence)
    {
        card.AddChild(Label($"Origin · {MandateNames(evidence.Origin.Manager, evidence.Origin.Director)}", 13));
        string Candidate(RecommendationCandidate candidate) => $"{PersonName(candidate.PersonId)} · {candidate.Role} · age {candidate.Age} · ability {candidate.Ability}\n"
            + $"{Money.Format(candidate.WeeklyWage)}/week · wages {Calendar.FullDay(candidate.WageStartWeek)}–{Calendar.FullDay(candidate.ContractEndWeek)} "
            + $"({candidate.ContractEndWeek - candidate.WageStartWeek + 1} weeks) · total commitment {Money.Format(candidate.TotalCommitment)} · no fee";
        if (evidence.Reason == RecommendationReason.LowerCostAlternative)
        {
            card.AddChild(Label("Current-ranking alternative · " + Candidate(evidence.Baseline), 14));
            card.AddChild(Label("Value recommendation · " + Candidate(evidence.Selected), 14));
            card.AddChild(Label($"Readiness tradeoff: {evidence.Baseline.Ability - evidence.Selected.Ability} ability points. Lower commitment can include shorter contracted cover; it is not cash already saved.", 13));
        }
        else
        {
            card.AddChild(Label(Candidate(evidence.Selected), 14));
            card.AddChild(Label(evidence.Reason == RecommendationReason.NoLowerCostAlternative
                ? "No qualifying lower-cost alternative. The current recommendation is retained."
                : "Current recommendation · existing readiness and squad-cover ranking.", 13));
        }
    }

    private void ApproachMandate(VBoxContainer card, string forecastId)
    {
        var approach = view.Mandates.Approaches.SingleOrDefault(a => a.ForecastId == forecastId);
        if (approach?.Recommendation is { } recommendation) RecommendationFacts(card, recommendation);
        else card.AddChild(Label("Originating staff mandate and comparison were not recorded for this older approval.", 13));
        if (approach?.Outcome is { } outcome)
        {
            card.AddChild(Label($"{Calendar.FullDay(outcome.ResolvedWeek)} · {(outcome.Outcome == ApproachOutcome.Signed ? "Signed" : "Approach closed")}\n{outcome.Reason}", 14));
            card.AddChild(Label(outcome.Outcome == ApproachOutcome.Signed
                ? "A contract was created. Paid wages are shown separately in season mandate evidence."
                : "No new contract or payment was created by this approach.", 13));
        }
        else
        {
            var pending = view.MandateSeasonReports.SelectMany(s => s.Approaches).Any(a => a.ForecastId == forecastId && a.Pending);
            card.AddChild(Label(pending ? "Approved approach pending · no signing or wage payment yet." : "The outcome was not recorded in mandate evidence. See the original review; no result is inferred here.", 13));
        }
    }

    private void SelectionMandate(VBoxContainer card, MatchResult match)
    {
        var evidence = view.Mandates.Selections.SingleOrDefault(s => s.FixtureId == match.FixtureId);
        if (evidence is null) { card.AddChild(Label("Staff mandate origin was not recorded for this older match.", 13)); return; }
        card.AddChild(Label($"Manager mandate at selection: {ManagerName(evidence.Origin.Manager)}", 14));
        foreach (var swap in evidence.Swaps)
            card.AddChild(Label($"{PersonName(swap.Selected.PersonId)} (age {swap.Selected.Age}, ability {swap.Selected.Ability}) started instead of {PersonName(swap.Displaced.PersonId)} "
                + $"(age {swap.Displaced.Age}, ability {swap.Displaced.Ability}) at {swap.Selected.Role} · readiness cost {swap.AbilitySacrifice} ability points.", 14));
        if (evidence.Swaps.IsEmpty) card.AddChild(Label(evidence.Origin.Manager == ManagerMandate.Development
            ? "No qualifying development replacement; the readiness lineup was retained."
            : "The manager used readiness selection; no development replacements were requested.", 13));
        card.AddChild(Label("The appearance is recorded. This result alone does not establish whether the mandate helped or hurt.", 13));
    }

    private void MandateHistory()
    {
        var card = Card(); card.AddChild(Label("Mandate history and outcomes", 24));
        card.AddChild(Label(MandateNames(view.Mandates.Current.Manager, view.Mandates.Current.Director), 16));
        card.AddChild(MandateButton(mandateHistoryExpanded ? "Hide mandate evidence" : "Show mandate evidence", () =>
        {
            mandateHistoryExpanded = !mandateHistoryExpanded;
            mandateFocus = mandateHistoryExpanded ? "Hide mandate evidence" : "Show mandate evidence"; Render();
        }));
        if (!mandateHistoryExpanded) { card.AddChild(Label("Inspect dated instructions, recorded selections, recruitment outcomes and paid wages. Older missing evidence stays unknown.", 14)); return; }
        foreach (var change in view.Mandates.History.Reverse())
            card.AddChild(Label($"{Calendar.FullDay(change.Week)}\n{MandateNames(change.Before.Manager, change.Before.Director)}\n→ {MandateNames(change.After.Manager, change.After.Director)}", 14));
        if (view.Mandates.History.IsEmpty) card.AddChild(Label("No mandate changes recorded. The saved defaults remain in force; older decisions without evidence are not assigned an origin.", 14));
        foreach (var report in view.MandateSeasonReports.Reverse())
        {
            var season = Card(); season.AddChild(Label($"Season {report.Season} · mandate evidence", 22));
            season.AddChild(Label($"Through {Calendar.FullDay(report.ThroughWeek)} · {(report.Complete ? "closing review available" : "season in progress")}\n"
                + $"Unknown origins: {report.MatchesWithoutOrigin} matches, {report.ApprovalsWithoutOrigin} approvals.", 14));
            foreach (var period in report.ObservedPeriods)
                season.AddChild(Label($"{MandateNames(period.Origin.Manager, period.Origin.Director)}\n"
                    + $"Observed {Calendar.FullDay(period.FirstDecisionWeek)}–{Calendar.FullDay(period.LastDecisionWeek)} · {period.Matches} matches, {period.DevelopmentSwaps} replacements, {period.Approvals} approvals.", 14));
            foreach (var approach in report.Approaches)
            {
                season.AddChild(Label($"{approach.ApprovedTerms.Player.Name} · approved {Calendar.FullDay(approach.ApprovalWeek)}\n"
                    + (approach.WagesPaid is { } paid ? $"Wages actually paid during this report: {Money.Format(paid)}." : "Historical paid-wage attribution is unknown; this is not £0 spending."), 14));
                ApproachMandate(season, approach.ForecastId);
            }
            var details = new VBoxContainer { Visible = false }; Button? toggle = null;
            toggle = Button($"Show season {report.Season} selections and appearances", () =>
            {
                details.Visible = !details.Visible;
                toggle!.Text = $"{(details.Visible ? "Hide" : "Show")} season {report.Season} selections and appearances";
            });
            season.AddChild(toggle); season.AddChild(details);
            foreach (var appearance in report.Appearances)
                details.AddChild(Label($"{appearance.Name} · {appearance.Starts} owned-club starts, {appearance.DevelopmentSelections} explicit development replacements"
                    + (appearance.Development is { } development ? $" · annual ability {development.AbilityBefore} → {development.AbilityAfter}; {development.Appearances} total recorded appearances" : " · annual development not recorded here"), 13));
            foreach (var match in view.Mandates.HistoricalMatches.Where(m => m.Week > report.StartWeek && m.Week <= report.ThroughWeek).Reverse())
            {
                details.AddChild(Label($"{Calendar.FullDay(match.Week)} · {ClubName(match.Home)} {match.HomeGoals}–{match.AwayGoals} {ClubName(match.Away)}", 16));
                SelectionMandate(details, match);
            }
            season.AddChild(Label(report.Interpretation, 13));
        }
    }
}
