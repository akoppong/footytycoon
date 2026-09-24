using Godot;
using FootballTycoon.Core;
using FootballTycoon.Application;

namespace FootballTycoon.Desktop;

// Presentation only: every number and available command comes from the immutable session view.
public partial class Main
{
    private static readonly Color Navy = new("0f1b2d"), Paper = new("f6f3ec"), White = new("fffdf8"),
        Rule = new("d9d2c4"), Muted = new("5c6472"), Amber = new("e0a33c"), Red = new("a4361f");
    private Font serifFont = null!, monoFont = null!;
    private VBoxContainer shell = null!, rail = null!;
    private string auxiliary = "", inboxSelection = "brief";
    private bool chartExpanded = true, confirming;
    private readonly HashSet<string> filedReviews = [];
    private bool showFiled;
    private Review? chosenReview;
    private MatchResult? chosenMatch;
    private DecisionRecord? chosenHistory;

    private void InitializeDesign()
    {
        serifFont = GD.Load<FontFile>("res://Assets/Fonts/SourceSerif4.ttf");
        monoFont = GD.Load<FontFile>("res://Assets/Fonts/IBMPlexMono.ttf");
        var background = new ColorRect { Color = Paper, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(background);
        var root = new VBoxContainer(); root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0); AddChild(root);
        shell = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        shell.AddThemeConstantOverride("separation", 0); root.AddChild(shell);
        var footer = Surface(Navy, 10); root.AddChild(footer);
        status = Label("Loading the opening opportunity…", 11); status.AddThemeColorOverride("font_color", new Color("afbdce")); footer.AddChild(status);
    }

    private static PanelContainer Surface(Color color, int padding = 16)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = color, ContentMarginLeft = padding,
            ContentMarginRight = padding, ContentMarginTop = padding, ContentMarginBottom = padding,
            BorderWidthBottom = 1, BorderColor = Rule });
        return panel;
    }
    private Label Caption(string text, Color? color = null, int size = 10)
    {
        var label = Label(text, size); label.AddThemeFontOverride("font", monoFont);
        label.AddThemeColorOverride("font_color", color ?? Muted); return label;
    }
    private static VBoxContainer Stack(Node parent, int gap = 10)
    {
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", gap); parent.AddChild(box); return box;
    }
    private VBoxContainer Pane(Node parent, Color color, int width = 0)
    {
        var panel = Surface(color, 0); panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.SizeFlagsHorizontal = width == 0 ? SizeFlags.ExpandFill : SizeFlags.Fill;
        panel.CustomMinimumSize = new Vector2(width, 0); parent.AddChild(panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true,
            SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddChild(scroll); return Stack(scroll, 0);
    }
    private void Primary(Button button)
    {
        button.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color("1f3a5f"), ContentMarginLeft = 14,
            ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12 });
        button.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color("294e7b"), ContentMarginLeft = 14,
            ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12 });
        foreach (var state in new[] { "font_color", "font_hover_color", "font_focus_color" }) button.AddThemeColorOverride(state, White);
    }
    private void Unavailable(Button button, string reason)
    {
        button.SetMeta("unavailable", true); button.Disabled = true; button.TooltipText = reason;
    }
    private void Navigate(string name)
    {
        workspace = name; selected = null; confirming = false; auxiliary = ""; Render();
    }
    private void SelectInbox(string id)
    {
        workspace = "Owner Desk"; auxiliary = ""; selected = null; confirming = false; inboxSelection = id; Render();
    }
    private void BuildShell()
    {
        foreach (var child in shell.GetChildren()) { shell.RemoveChild(child); child.QueueFree(); }
        controls.RemoveAll(b => !GodotObject.IsInstanceValid(b) || b.IsQueuedForDeletion() || !b.IsInsideTree());
        var header = Surface(Navy, 12); shell.AddChild(header);
        var top = new HBoxContainer(); header.AddChild(top);
        var identity = Stack(top, 2); identity.SizeFlagsStretchRatio = 1.4f;
        var name = Label(view.Club, 21); name.AddThemeColorOverride("font_color", White); identity.AddChild(name);
        identity.AddChild(Caption($"DIVISION {view.Division}  ·  SEASON {view.Season}  ·  {(view.SeasonWeek == 0 ? "SEASON START" : $"WEEK {view.SeasonWeek:00}")}", new Color("8fa3bc")));
        var fixture = view.Fixtures.OrderBy(f => f.Week).FirstOrDefault(f => f.Week > view.Week);
        var next = Stack(top, 3);
        next.AddChild(Caption("NEXT MATCH", new Color("8fa3bc")));
        date = Label(fixture is null ? "Season schedule complete" : $"{(fixture.Competition == Competition.Cup ? Cups.RoundName(fixture.CupRound) + " · " : "")}{ClubName(fixture.Home == view.ClubId ? fixture.Away : fixture.Home)} · {(fixture.Home == view.ClubId ? "H" : "A")} · W{SeasonWeek(fixture.Week)}", 12);
        date.AddThemeColorOverride("font_color", White); next.AddChild(date);
        foreach (var metric in new[] { ("CLUB CASH", view.ClubCash), ("YOUR RESERVE", view.PersonalReserve) })
        {
            var box = Stack(top, 3); box.AddChild(Caption(metric.Item1, new Color("8fa3bc")));
            box.AddChild(Caption(ShortMoney(metric.Item2), White, 18));
        }
        var advance = Button(selected is not null ? "Time is held" : view.Status == CareerStatus.Acquisition ? "Review purchase" : view.Status == CareerStatus.SeasonReview ? "Review next season" : !view.AllocationChosen ? "Choose a plan" : "Continue →", ContinueCareer);
        advance.CustomMinimumSize = new Vector2(155, 0); advance.SizeFlagsHorizontal = SizeFlags.Fill; Primary(advance); top.AddChild(advance);
        if (selected is not null || auxiliary != "" || view.Status is CareerStatus.LostControl or CareerStatus.PrototypeComplete)
            Unavailable(advance, "Finish or leave this screen before advancing. Completed careers cannot advance.");
        else advance.TooltipText = $"{advanceKey}: continue to the next review or required decision.";
        var navPanel = Surface(Navy, 4); shell.AddChild(navPanel);
        var nav = new HFlowContainer(); navPanel.AddChild(nav); nav.AddThemeConstantOverride("h_separation", 2);
        foreach (var tab in new[] { "Owner Desk", "Club", "Football", "Business", "People", "History" })
        {
            var button = Button(tab, () => Navigate(tab)); button.SizeFlagsHorizontal = SizeFlags.Fill; button.AutowrapMode = TextServer.AutowrapMode.Off; button.CustomMinimumSize = Vector2.Zero;
            button.AddThemeFontSizeOverride("font_size", 13 * textScale / 100);
            var active = tab == workspace && auxiliary == "";
            button.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = active ? new Color("24374d") : Navy,
                BorderWidthBottom = active ? 2 : 0, BorderColor = Amber, ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 8 });
            button.AddThemeColorOverride("font_color", active ? Amber : Paper); nav.AddChild(button);
        }
        foreach (var item in new[] { ("Save", (Action)Save), ("Load / recover", (Action)ShowSaves), ("Settings", (Action)ShowSettings) })
        {
            var b = Button(item.Item1, item.Item2); b.SizeFlagsHorizontal = SizeFlags.Fill; b.AutowrapMode = TextServer.AutowrapMode.Off; b.CustomMinimumSize = Vector2.Zero;
            b.AddThemeFontSizeOverride("font_size", 11 * textScale / 100); nav.AddChild(b);
        }
        if (auxiliary == "" && (selected is not null || !view.AllocationChosen || view.Status != CareerStatus.Active))
        {
            var banner = Surface(new Color("f3e7cc"), 8); shell.AddChild(banner);
            banner.AddChild(Caption(view.Status is CareerStatus.LostControl or CareerStatus.PrototypeComplete
                ? $"{view.Status.ToString().ToUpperInvariant()}  ·  Review your season in History."
                : selected is not null ? "TIME IS HELD  ·  Review the terms. Nothing is committed until you confirm."
                : view.Status == CareerStatus.Acquisition ? "NEW CAREER  ·  Review the acquisition before taking control."
                : view.Status == CareerStatus.Administration ? "ADMINISTRATION  ·  Review overdue obligations in Business."
                : view.Status == CareerStatus.SeasonReview ? "SEASON COMPLETE  ·  Review next season’s renewals before continuing."
                : "NEEDS YOU  ·  Choose this season’s capital plan before continuing.", new Color("785411"), 11));
        }
        if (view.Status != CareerStatus.Acquisition && auxiliary == "") BuildSeasonLine();
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 0); shell.AddChild(body);
        var acquisition = view.Status == CareerStatus.Acquisition && workspace == "Owner Desk" && selected is null && auxiliary == "";
        if (!acquisition && auxiliary == "") BuildInbox(Pane(body, new Color("f1ede3"), textScale == 150 ? 235 : 218));
        content = Pane(body, Paper);
        rail = Pane(body, new Color("efebe1"), textScale == 150 ? 280 : 258);
        BuildRail();
        SetBusy();
    }
    private void BuildSeasonLine()
    {
        var panel = Surface(White, 12); shell.AddChild(panel); var box = Stack(panel, 5);
        var row = new HBoxContainer(); box.AddChild(row);
        row.AddChild(Label(selected?.Renewal is { } renewal ? $"Season {renewal.NextSeason} · proposed cash" : "The season so far", 22));
        row.AddChild(Caption($"{Standing()}  ·  {Form()}", Muted, 11));
        var toggle = Button(chartExpanded ? "Collapse chart" : "Expand chart", () => { chartExpanded = !chartExpanded; Render(); });
        toggle.SizeFlagsHorizontal = SizeFlags.Fill; toggle.AutowrapMode = TextServer.AutowrapMode.Off; toggle.CustomMinimumSize = Vector2.Zero; toggle.AddThemeFontSizeOverride("font_size", 11 * textScale / 100); row.AddChild(toggle);
        if (!chartExpanded) return;
        var original = inboxSelection == "commitment" ? chosenHistory?.OriginalForecast
            : inboxSelection.StartsWith("review:") ? view.History.LastOrDefault(h => h.OriginalForecast.Id == chosenReview?.ForecastId)?.OriginalForecast : null;
        var chart = new SeasonChart(view, selected, monoFont, textScale, original) { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        chart.MatchSelected += match => { chosenMatch = match; SelectInbox("match"); };
        box.AddChild(chart);
        box.AddChild(Caption("NAVY  Actual / dashed base     RED DASH  Downside     AMBER  Proposed base / dashed downside     GREY DOT  Original base", Muted, 10));
        var inspection = Caption("", Navy, 11); inspection.Visible = false; box.AddChild(inspection);
        chart.InspectionChanged += detail => { inspection.Text = detail; inspection.Visible = detail.Length > 0; };
    }
    private void BuildInbox(VBoxContainer inbox)
    {
        var heading = Surface(new Color("f1ede3"), 14); inbox.AddChild(heading); heading.AddChild(Label("Needs you", 22));
        void Item(string id, string tag, string title, string from, Action action)
        {
            var b = Button($"{tag}\n{title}\n{from}", action); b.AddThemeFontSizeOverride("font_size", 12 * textScale / 100);
            b.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = inboxSelection == id && workspace == "Owner Desk" ? White : new Color("f1ede3"),
                BorderWidthLeft = inboxSelection == id && workspace == "Owner Desk" ? 3 : 0, BorderWidthBottom = 1, BorderColor = Rule,
                ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 14 }); inbox.AddChild(b);
        }
        Item("brief", !view.AllocationChosen ? "DECISION REQUIRED" : "OWNER BRIEF", !view.AllocationChosen ? "Where should the money go?" : "Your club, this week", "Mara Ellis · CEO", () => SelectInbox("brief"));
        if (view.History.LastOrDefault(h => h.Command.Allocation != Allocation.Acquire) is { } history)
            Item("commitment", "COMMITTED", history.Intent, $"Career week {history.Week} · {history.Executive}", () => { chosenHistory = history; SelectInbox("commitment"); });
        foreach (var review in view.Reviews.Reverse().Where(r => showFiled || !filedReviews.Contains(ReviewKey(r))).Take(6))
            Item(ReviewKey(review), filedReviews.Contains(ReviewKey(review)) ? "FILED" : $"REVIEW · CAREER WEEK {review.Week}", review.Title, "Saved evidence", () => { chosenReview = review; SelectInbox(ReviewKey(review)); });
        if (view.Results.LastOrDefault() is { } match)
            Item("match", $"{(MatchFixture(match).Competition == Competition.Cup ? "CUP" : "MATCH")} · WEEK {SeasonWeek(match.Week)}", MatchTitle(match), "Callum Price · Manager", () => { chosenMatch = match; SelectInbox("match"); });
        var boxPanel = Surface(new Color("f1ede3"), 14); inbox.AddChild(boxPanel); var questions = Stack(boxPanel);
        questions.AddChild(Caption("OPEN QUESTIONS"));
        foreach (var question in view.OpenQuestions) questions.AddChild(Label(question, 12));
        if (view.OpenQuestions.IsEmpty) questions.AddChild(Label("No unresolved questions. Quiet weeks can be skipped.", 12));
        questions.AddChild(Button(showFiled ? "Hide filed reviews" : "Show filed reviews", () => { showFiled = !showFiled; Render(); }));
    }
    private void BuildRail()
    {
        var panel = Surface(new Color("efebe1"), 16); rail.AddChild(panel); var box = Stack(panel, 11);
        box.AddChild(Label(selected is null ? "At a glance" : confirming ? "Confirm terms" : "Your proposed plan", 22));
        box.AddChild(Caption(selected is null ? $"SEASON {view.Season} · WEEK {view.SeasonWeek}" : "NOT YET COMMITTED"));
        if (selected is { } railProposal)
        {
            var action = Button(confirming ? "Confirm and commit" : "Review final terms", () => { if (confirming) CommitSelected(); else { confirming = true; Render(); } });
            Primary(action); box.AddChild(action);
            if (!railProposal.BlockingReasons.IsEmpty) Unavailable(action, string.Join("\n", railProposal.BlockingReasons));
            box.AddChild(Button(confirming ? "Keep editing" : "Back to comparison", () => { if (confirming) confirming = false; else selected = null; Render(); }));
        }
        var forecast = selected?.Forecast ?? view.Forecast;
        Fact(box, "Club cash now", Money.Format(view.ClubCash));
        Fact(box, "Personal reserve", Money.Format(view.PersonalReserve));
        Fact(box, "Club reserve target", Money.Format(view.ReserveTarget));
        Fact(box, "Base low · next 52 weeks", $"{Money.Format(forecast.LowestBase)} · career W{forecast.LowestBaseWeek}");
        Fact(box, "Downside low · next 52 weeks", $"{Money.Format(forecast.LowestDownside)} · career W{forecast.LowestDownsideWeek}", forecast.LowestDownside < view.ReserveTarget ? Red : null);
        box.AddChild(Label($"The chart ends at season week 52. Forecast lows above include the full rolling 52-week horizon; dates beyond the season are illustrative.", 11));
        if (selected is { } proposal)
        {
            Fact(box, proposal.Command.Allocation == Allocation.Acquire ? "Personal payment to seller" : proposal.Command.Allocation == Allocation.InjectCapital ? "Personal transfer to club" : "Upfront / fee ceiling",
                Money.Format(proposal.Command.Allocation == Allocation.Acquire ? proposal.TotalCommitment : proposal.Command.Allocation == Allocation.InjectCapital ? proposal.Command.Amount : proposal.UpfrontCash));
            if (proposal.Command.Allocation != Allocation.Acquire && proposal.Renewal is null) Fact(box, "Continuing cost", Money.Format(proposal.WeeklyCost) + "/week");
            foreach (var reason in proposal.BlockingReasons) box.AddChild(Label("BLOCKED · " + reason, 12));
        }
        else
        {
            Fact(box, "League position", Standing());
            Fact(box, "Annual squad wages", Money.Format(view.AnnualWages));
            if (view.Arrears > 0) { Fact(box, "OVERDUE", Money.Format(view.Arrears), Red); box.AddChild(Button("Review owner funding", () => Navigate("Business"))); }
            box.AddChild(Caption("DECISION HISTORY"));
            foreach (var history in view.History.TakeLast(3).Reverse())
                box.AddChild(Button($"Career W{history.Week} · {history.Intent}", () => { chosenHistory = history; SelectInbox("commitment"); }));
            if (view.History.IsEmpty) box.AddChild(Label("Your record begins with the acquisition.", 12));
        }
    }
    private void Fact(VBoxContainer box, string label, string value, Color? color = null)
    {
        var row = Stack(box, 3); row.AddChild(Caption(label.ToUpperInvariant())); row.AddChild(Caption(value, color ?? Navy, 14)); row.AddChild(new HSeparator());
    }
    private void RenderDesk()
    {
        if (view.Status == CareerStatus.Acquisition) { Acquisition(); return; }
        if (inboxSelection.StartsWith("review:") && chosenReview is not null)
        {
            var card = Card(); card.AddChild(Caption($"REVIEW · CAREER WEEK {chosenReview.Week}")); card.AddChild(Label(chosenReview.Title, 28));
            card.AddChild(Label(chosenReview.Evidence));
            var original = view.History.LastOrDefault(h => h.OriginalForecast.Id == chosenReview.ForecastId);
            if (original is not null) Comparison(card, original, chosenReview.Week);
            card.AddChild(Button(filedReviews.Contains(ReviewKey(chosenReview)) ? "Restore to inbox" : "File away", () =>
            {
                var key = ReviewKey(chosenReview); if (!filedReviews.Add(key)) filedReviews.Remove(key); SelectInbox("brief");
            })); return;
        }
        if (inboxSelection == "match" && chosenMatch is not null) { MatchCard(chosenMatch, full: true); return; }
        if (inboxSelection == "commitment" && chosenHistory is not null)
        {
            var card = Card(); card.AddChild(Caption($"SAVED COMMITMENT · CAREER WEEK {chosenHistory.Week}"));
            card.AddChild(Label(chosenHistory.Intent, 28)); card.AddChild(Label(chosenHistory.Executive));
            Comparison(card, chosenHistory, view.Week);
            RecruitmentHistory(card, chosenHistory);
            card.AddChild(Label("The original forecast is kept as it was when you committed. It is an estimate, not a promise. Recruitment approval does not guarantee a signing."));
            card.AddChild(Button("Open decision history", () => Navigate("History"))); return;
        }
        var intro = Card(); intro.AddChild(Caption(inboxSelection == "resume" ? "WELCOME BACK · VALIDATED CHECKPOINT" : "OWNER BRIEF · MARA ELLIS, CEO"));
        intro.AddChild(Label(view.Status == CareerStatus.PrototypeComplete ? "Your three-season career is complete" : view.Status == CareerStatus.SeasonReview ? $"Season {view.Season} is complete" : view.Status == CareerStatus.LostControl ? "Your ownership has ended" : !view.AllocationChosen ? "Where should the money go?" : "Your club, this week", 30));
        intro.AddChild(Label(!view.AllocationChosen ? "“We can back a forward, build hospitality, or protect our reserve. Each choice leaves something for later. Compare the cash path before you commit.”"
            : $"{Money.Format(view.ClubCash)} in club cash. {Standing()}. Your reserve is {Money.Format(view.PersonalReserve)}; it remains separate from the club."));
        if (!view.AllocationChosen && view.Status == CareerStatus.Active) { AllocationCards(); return; }
        if (view.Status == CareerStatus.SeasonReview)
        {
            SeasonReport(view.SeasonSummaries.Last());
            var renew = Button("Review next season’s renewals →", () => Preview(Allocation.StartNextSeason));
            Primary(renew); intro.AddChild(renew); return;
        }
        if (view.Status is CareerStatus.LostControl or CareerStatus.PrototypeComplete)
            intro.AddChild(Label("Review the results and original commitments in History. This milestone supports three seasons. Negotiated renewals and ownership exits remain future systems."));
        else
        {
            if (view.MarketAvailable)
            {
                intro.AddChild(Label(view.MarketStatus, 14));
                intro.AddChild(Button("Review midseason recruitment →", () => Navigate("Football")));
            }
            var continueButton = Button("Continue to next review →", ContinueCareer); Primary(continueButton); intro.AddChild(continueButton);
            intro.AddChild(Button("Advance one week", () => Advance(AdvanceTarget.Week)));
        }
        if (inboxSelection == "resume")
        {
            intro.AddChild(Caption("WHAT IS DECIDED")); intro.AddChild(Label(view.History.LastOrDefault()?.Intent ?? "Acquisition awaits your approval."));
            intro.AddChild(Caption("WHAT AWAITS")); intro.AddChild(Label(string.Join("\n", view.OpenQuestions.DefaultIfEmpty("No required decision. Continue to the next review."))));
        }
        if (view.Reviews.LastOrDefault() is { } review) ReviewCard(review);
        if (view.Results.LastOrDefault() is { } latest) MatchCard(latest);
    }
    private void Acquisition()
    {
        var card = Card(); card.AddChild(Caption("NEW CAREER · ACQUISITION"));
        card.AddChild(Label("A club worth building.", 38)); card.AddChild(Label("Stonebridge Football Club", 28));
        card.AddChild(Label("An established local club with a competitive squad and limited hospitality. Supporters value continuity. Decide what deserves capital while Mara Ellis keeps the bills covered."));
        var metrics = new GridContainer { Columns = 2 }; card.AddChild(metrics);
        Metric(metrics, "ASKING PRICE · PERSONAL CASH", ShortMoney(Balance.Load().PurchasePrice));
        Metric(metrics, "YOUR RESERVE AFTER PURCHASE", ShortMoney(view.PersonalReserve - Balance.Load().PurchasePrice));
        Metric(metrics, "CLUB CASH AT TAKEOVER", ShortMoney(view.ClubCash));
        Metric(metrics, "ANNUAL SQUAD WAGES", ShortMoney(view.AnnualWages));
        card.AddChild(Caption("OBLIGATIONS YOU INHERIT"));
        foreach (var obligation in view.Obligations.GroupBy(o => o.Kind))
            Fact(card, obligation.Key.ToString(), Money.Format(obligation.Sum(o => o.WeeklyAmount)) + "/week · signed net");
        card.AddChild(Label("The purchase pays the seller. The club receives no new cash. No opening debt; ownership authority covers capital and priorities, while staff run football operations.", 13));
        var buy = Button("Review acquisition terms →", () => Preview(Allocation.Acquire)); Primary(buy); card.AddChild(buy);
    }
    private void AllocationCards()
    {
        var balance = Balance.Load();
        var options = new GridContainer { Columns = textScale == 100 ? 3 : 1, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        options.AddThemeConstantOverride("h_separation", 1); content.AddChild(options);
        foreach (var allocation in new[] { Allocation.Hospitality, Allocation.Recruitment, Allocation.PreserveReserve })
        {
            var panel = Surface(White, 16); panel.SizeFlagsHorizontal = SizeFlags.ExpandFill; options.AddChild(panel);
            var card = Stack(panel); var hospitality = allocation == Allocation.Hospitality; var recruitment = allocation == Allocation.Recruitment;
            card.AddChild(Caption(recruitment ? "JONAS REED · SPORTING DIRECTOR" : "MARA ELLIS · CEO"));
            card.AddChild(Label(hospitality ? "Build hospitality" : recruitment ? "Back the forward search" : "Protect the reserve", 24));
            card.AddChild(Caption(hospitality ? $"{Money.Format(balance.HospitalityCost)} cash now · {Money.Format(balance.HospitalityWeeklyUpkeep)}/week after opening"
                : recruitment ? $"{Money.Format(balance.TransferFeeCeiling)} fee ceiling · {Money.Format(balance.RecruitWeeklyWage)}/week wage ceiling" : "£0 new spending · retain the current squad and facilities", Navy, 13));
            card.AddChild(Label(hospitality ? $"A 28-week build. Future receipts depend on home matches and demand; upkeep runs through career week {view.SeasonEndWeek + 52}."
                : recruitment ? "Authorize a search, not a guaranteed signing. Jonas negotiates within your ceiling; the fee is paid only if a deal completes."
                : "Keep capital available. Retaining cash is a valid plan; it cannot guarantee sporting success.", 13));
            card.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
            card.AddChild(Button("Compare this plan →", () => Preview(allocation)));
        }
    }
    private void RecruitmentHistory(VBoxContainer card, DecisionRecord decision)
    {
        if (decision.Recruitment is not { } terms) return;
        card.AddChild(Label($"Approved target: {terms.Player.Name} · {terms.Seller}\nFee ceiling {Money.Format(terms.FeeCeiling)} · {Money.Format(terms.WeeklyWage)}/week · contract through career week {terms.ContractEndWeek}. These are the saved mandate terms; the negotiation outcome is reported separately.", 14));
    }

    private void DesignProposal(Proposal proposal)
    {
        var card = Card(); card.AddChild(Caption(confirming ? "FINAL TERMS · NOTHING SPENT YET" : "PROPOSAL · TIME IS HELD"));
        card.AddChild(Label(proposal.Title, 30)); card.AddChild(Label(proposal.Executive));
        if (proposal.Command.Allocation == Allocation.Acquire)
        {
            Fact(card, "Payment from personal money", Money.Format(proposal.TotalCommitment));
            Fact(card, "Personal reserve after purchase", Money.Format(view.PersonalReserve - proposal.TotalCommitment));
            Fact(card, "Club cash after purchase", Money.Format(view.ClubCash));
        }
        else if (proposal.Command.Allocation == Allocation.InjectCapital)
        {
            Fact(card, "Transfer from personal money to club", Money.Format(proposal.Command.Amount));
            Fact(card, "Personal reserve after transfer", Money.Format(view.PersonalReserve - proposal.Command.Amount));
            Fact(card, "Club cash after transfer · before overdue payments", Money.Format(view.ClubCash + proposal.Command.Amount));
        }
        else if (proposal.Renewal is { } renewal)
        {
            Fact(card, "Next season’s competition", Pyramid.Outcome(renewal.CurrentDivision, renewal.NextDivision));
            Fact(card, "Annual broadcast · current → next", $"{Money.Format(renewal.CurrentAnnualBroadcast)} → {Money.Format(renewal.AnnualBroadcast)}");
            Fact(card, "Annual sponsorship · current → next", $"{Money.Format(renewal.CurrentAnnualSponsor)} → {Money.Format(renewal.AnnualSponsor)}");
            Fact(card, "Expiring player contracts · extended one season", renewal.PlayerContracts.ToString());
            Fact(card, "Annual wages for renewed players", Money.Format(renewal.RenewedAnnualWages));
            Fact(card, "Annual operations run rate · including carried contracts", Money.Format(renewal.AnnualOperations));
            Fact(card, "Arrears carried forward", Money.Format(renewal.Arrears));
            Fact(card, "Cash on confirmation · unchanged", Money.Format(view.ClubCash));
        }
        else
        {
            Fact(card, RecruitmentMarket.IsSigning(proposal.Command.Allocation) ? "Transfer fee ceiling · conditional" : "Cash committed now", Money.Format(proposal.UpfrontCash));
            Fact(card, "Continuing cost", Money.Format(proposal.WeeklyCost) + "/week");
            Fact(card, "Total commitment / authorized ceiling", Money.Format(proposal.TotalCommitment));
            Fact(card, "Review · career week", proposal.ReviewWeek.ToString());
        }
        if (proposal.Recruitment is { } recruit)
        {
            Fact(card, "Director’s target", $"{recruit.Player.Name} · {recruit.Seller}");
            Fact(card, "Role / age / current ability", $"{recruit.Player.Role} · {recruit.Player.Age} · {recruit.Player.Ability}");
            Fact(card, "Your two leading forwards · average ability", recruit.CurrentForwardAbility.ToString("0.0"));
            Fact(card, "Contract if signed next week", $"Wages from career week {view.Week + 2} through career week {recruit.ContractEndWeek} · no automatic tier wage changes");
            Fact(card, "Annual wage commitment", Money.Format(recruit.WeeklyWage * Seasons.Weeks));
            card.AddChild(Label("An additional forward competes for places; selection stays with the manager. No buyer or resale value is guaranteed.", 14));
        }
        card.AddChild(Caption("WHAT STAYS UNCERTAIN")); card.AddChild(Label(proposal.Uncertainty));
        if (!compact) card.AddChild(Label(proposal.Forecast.Assumptions, 12));
        foreach (var reason in proposal.BlockingReasons) card.AddChild(Label("BLOCKED · " + reason, 14));
        if (!proposal.Command.ReserveException && proposal.BlockingReasons.Any(r => r.Contains("reserve exception", StringComparison.OrdinalIgnoreCase)))
            card.AddChild(Button("Review explicit reserve exception", () => { confirming = false; Preview(proposal.Command.Allocation, proposal.Command.Amount, true); }));
        if (proposal.Command.ReserveException) card.AddChild(Label("EXPLICIT EXCEPTION · You are accepting forecast cash below the advised reserve target.", 14));
        var confirm = Button(confirming ? "Confirm and commit" : "Review final terms →", () => { if (confirming) CommitSelected(); else { confirming = true; Render(); } });
        Primary(confirm); card.AddChild(confirm); if (!proposal.BlockingReasons.IsEmpty) Unavailable(confirm, string.Join("\n", proposal.BlockingReasons));
        card.AddChild(Button(confirming ? "Keep editing" : "Back to comparison", () => { if (confirming) confirming = false; else selected = null; Render(); }));
    }
    private void CommitSelected()
    {
        if (!confirming || selected is not { } proposal || !proposal.BlockingReasons.IsEmpty) return;
        Start(async () =>
        {
            var receipt = await session.CommitAsync(retryCommandId!, proposal.Revision, proposal);
            view = await session.QueryAsync(); selected = null; confirming = false; inboxSelection = "brief";
            return $"Saved locally · {receipt.Summary}";
        });
    }
    private void ContinueCareer()
    {
        if (busy || selected is not null || auxiliary != "") return;
        if (view.Status == CareerStatus.Acquisition) { Preview(Allocation.Acquire); return; }
        if (view.Status == CareerStatus.SeasonReview) { Preview(Allocation.StartNextSeason); return; }
        if (view.Status is CareerStatus.LostControl or CareerStatus.PrototypeComplete) return;
        if (!view.AllocationChosen && view.Status == CareerStatus.Active) { SelectInbox("brief"); return; }
        Advance(AdvanceTarget.NextDecision);
    }
    private void Comparison(VBoxContainer card, DecisionRecord decision, int week)
    {
        var point = decision.OriginalForecast.Points.FirstOrDefault(p => p.Week == week);
        if (point is null) { card.AddChild(Label("This week is outside the original forecast horizon.")); return; }
        var actual = view.SeasonSummaries.FirstOrDefault(s => s.EndWeek == week && decision.Week < s.EndWeek)?.ClosingCash
            ?? Balance.Load().OpeningClubCash + view.CashLines.Where(l => l.Week <= week).Sum(l => l.Amount);
        Fact(card, $"Original base forecast · career week {week}", Money.Format(point.BaseCash));
        Fact(card, $"Actual closing cash · career week {week}", Money.Format(actual));
        Fact(card, "Difference from original base", Money.Format(actual - point.BaseCash));
        card.AddChild(Label("The difference includes all club cash movements, including later owner decisions. It does not establish what a rejected investment would have earned.", 12));
    }
    private void SeasonReport(SeasonSummary season)
    {
        var card = Card(); card.AddChild(Label($"Season {season.Season} · closing report", 26));
        var position = season.FinalTable.ToList().FindIndex(r => r.ClubId == view.ClubId) + 1;
        Fact(card, "Final league position", $"Division {season.Division} · {position} / {season.FinalTable.Length}");
        Fact(card, "Opening cash", Money.Format(season.OpeningCash));
        Fact(card, "Cash at season close", Money.Format(season.ClosingCash));
        Fact(card, "Operating net cash", Money.Format(season.OperatingNet));
        Fact(card, "Owner injections during season", Money.Format(season.OwnerInjections));
        Fact(card, "Domestic Cup", season.CupResult);
        Fact(card, "Cup prize money", Money.Format(season.CupPrize));
        card.AddChild(Label(season.Plan));
        card.AddChild(Label("Recorded at the final weekly settlement. Later owner funding remains in the cash journal.", 12));
    }
    private void MatchCard(MatchResult match, bool full = false)
    {
        var fixture = MatchFixture(match);
            var card = Card(); card.AddChild(Caption($"{(fixture.Competition == Competition.Cup ? Cups.RoundName(fixture.CupRound).ToUpperInvariant() : "MATCH REPORT")} · WEEK {SeasonWeek(match.Week)}")); card.AddChild(Label(MatchTitle(match), 24));
        if (match.ExtraTime) card.AddChild(Label(match.Shootout is null ? "Decided after extra time." : $"Level after extra time · {match.Shootout} on penalties.", 13));
        card.AddChild(Label($"Shots {match.HomeShots}–{match.AwayShots} · Attendance {match.Attendance:N0} · Home receipts {Money.Format(match.Receipts)}", 13));
        string Name(PersonId id) => view.PlayerNames.TryGetValue(id, out var name) ? name : "Unknown player";
        static string Rating(Appearance a) => (a.Rating / 10m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        foreach (var moment in match.Moments) card.AddChild(Label($"{moment.Minute}'  {moment.Text}{(moment.AssistId is { } assist ? $" Assist: {Name(assist)}." : "")}"));
        if (match.Moments.IsEmpty) card.AddChild(Label("Neither side found the net."));
        var star = match.HomeLineup.Concat(match.AwayLineup).FirstOrDefault(a => a.Player == match.PlayerOfMatch);
        if (star is not null) card.AddChild(Label($"Player of the match: {Name(star.Player)} · {ClubName(match.HomeLineup.Contains(star) ? match.Home : match.Away)} · {Rating(star)}", 14));
        if (!full) return;
        card.AddChild(Caption("CARDS AND INJURIES"));
        string Matches(int count) => count == 1 ? "1 match" : $"{count} matches";
        foreach (var e in match.Events)
            card.AddChild(Label($"{e.Minute}'  " + e.Kind switch
            {
                MatchEventKind.Yellow => $"Yellow card · {Name(e.Player)} · {ClubName(e.ClubId)}" + (e.Duration > 0 ? $" · {Matchday.YellowsPerBan * e.Duration} cautions this season, suspended for {Matches(e.Duration)}" : ""),
                MatchEventKind.SecondYellow => $"Second yellow, sent off · {Name(e.Player)} · {ClubName(e.ClubId)} · suspended for {Matches(e.Duration)}",
                MatchEventKind.Red => $"Red card · {Name(e.Player)} · {ClubName(e.ClubId)} · suspended for {Matches(e.Duration)}",
                _ => $"Injury · {Name(e.Player)} · {ClubName(e.ClubId)} · out for about {e.Duration} week{(e.Duration == 1 ? "" : "s")}"
            }, 13));
        if (match.Events.IsEmpty) card.AddChild(Label(match.HomeLineup.IsEmpty ? "Not recorded for this match." : "No cards or injuries.", 13));
        card.AddChild(Caption("LINE-UPS AND RATINGS"));
        if (match.HomeLineup.IsEmpty && match.AwayLineup.IsEmpty) card.AddChild(Label("Line-ups and ratings were not recorded for matches played before this update.", 13));
        else
        {
            var columns = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill }; columns.AddThemeConstantOverride("h_separation", 24); card.AddChild(columns);
            foreach (var (club, lineup) in new[] { (match.Home, match.HomeLineup), (match.Away, match.AwayLineup) })
            {
                var column = Stack(columns, 3); column.CustomMinimumSize = new Vector2(220 * textScale / 100, 0);
                column.AddChild(Caption(ClubName(club).ToUpperInvariant(), Navy, 11));
                foreach (var a in lineup)
                    column.AddChild(Caption($"{a.Position switch { Role.Goalkeeper => "GK", Role.Defender => "DF", Role.Midfielder => "MF", _ => "FW" }}  {Rating(a)}  {Name(a.Player)}", a.Player == match.PlayerOfMatch ? Navy : Muted, 12));
            }
        }
        if (match.Home == view.ClubId || match.Away == view.ClubId)
        {
            card.AddChild(Caption("CALLUM PRICE · MANAGER"));
            card.AddChild(Label($"“{Matchday.ManagerLine(match, view.ClubId, fixture.Competition == Competition.Cup, view.PlayerNames)}”"));
        }
    }
    private string ClubName(ClubId id) => view.Clubs.Single(c => c.Id == id).Name;
    private Fixture MatchFixture(MatchResult match) => view.Fixtures.Single(f => f.Id == match.FixtureId);
    private string MatchTitle(MatchResult m) => $"{ClubName(m.Home)}  {m.HomeGoals}–{m.AwayGoals}  {ClubName(m.Away)}";
    private string Standing() => $"{view.Table.ToList().FindIndex(row => row.ClubId == view.ClubId) + 1} / {view.Table.Length} · {view.Table.Single(r => r.ClubId == view.ClubId).Points} pts";
    private string Form()
    {
        var league = view.Results.Where(m => MatchFixture(m).Competition == Competition.League).TakeLast(5).ToArray();
        return league.Length == 0 ? "No league matches played" : "Form " + string.Join(" ", league.Select(m => m.HomeGoals == m.AwayGoals ? "D" : (m.Home == view.ClubId ? m.HomeGoals > m.AwayGoals : m.AwayGoals > m.HomeGoals) ? "W" : "L"));
    }
    private static string ReviewKey(Review r) => $"review:{r.Week}:{r.Title}";
    internal static string ShortMoney(long value) => Math.Abs(value) >= 100000000 ? $"£{value / 100000000m:0.00}m" : Math.Abs(value) >= 100000 ? $"£{value / 100000m:0}k" : Money.Format(value);
}

