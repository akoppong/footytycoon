using Godot;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

namespace FootballTycoon.Desktop;

public partial class Main : Control
{
    private GameSession session = null!;
    private GameView view = null!;
    private VBoxContainer content = null!;
    private Label status = null!;
    private Label date = null!;
    private readonly List<Button> controls = [];
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> updates = new();
    private string workspace = "Owner Desk";
    private bool busy;
    private bool compact;
    private int textScale = 100;
    private Key advanceKey = Key.Space;
    private bool rebinding;
    private string? retryCommandId;
    private Proposal? selected;
    private string saveDirectory = "";
    private int SeasonWeek(int careerWeek) => Seasons.WeekInSeason(careerWeek, view.Season);

    public override void _Ready()
    {
        Theme = GD.Load<Theme>("res://Theme.tres");
        var preferences = new ConfigFile();
        if (preferences.Load("user://preferences.cfg") == Error.Ok)
        {
            compact = preferences.GetValue("ui", "compact", false).AsBool();
            var scale = preferences.GetValue("ui", "scale", 100).AsInt32();
            textScale = scale is 100 or 125 or 150 ? scale : 100;
            var key = (Key)preferences.GetValue("ui", "advance_key", (long)Key.Space).AsInt64();
            if (Enum.IsDefined(key) && key is not (Key.Escape or Key.Tab or Key.Enter or Key.S)) advanceKey = key;
        }
        Theme.DefaultFontSize = 14 * textScale / 100;
        GetTree().AutoAcceptQuit = false;
        saveDirectory = ProjectSettings.GlobalizePath("user://saves/offline");
        session = new GameSession(new LocalSaveVault(saveDirectory));
        InitializeDesign();
        Start(async () =>
        {
            view = await session.QueryAsync();
            return "Three-season milestone · Stonebridge FC · all changes save locally before confirmation.";
        });
        if (OS.GetCmdlineUserArgs().Contains("--smoke-test")) _ = SmokeTest();
    }

    // Godot objects are only touched here/on UI callbacks, never on the simulation worker.
    public override void _Process(double delta) { while (updates.TryDequeue(out var update)) update(); }
    private void Start(Func<Task<string>> action, bool render = true)
    {
        if (busy) return;
        busy = true; SetBusy();
        _ = Execute();
        async Task Execute()
        {
            try
            {
                var message = await action().ConfigureAwait(false);
                updates.Enqueue(() => { busy = false; status.Text = message; SetBusy(); if (render) Render(); });
            }
            catch (Exception error)
            {
                // A month can have saved earlier weeks before an I/O failure; always refresh the last authoritative checkpoint.
                try { view = await session.QueryAsync().ConfigureAwait(false); } catch (Exception) { }
                updates.Enqueue(() => { busy = false; SetBusy(); status.Text = $"Action stopped: {error.Message} Last saved state retained. Retry or choose another save."; Render(); });
            }
        }
    }
    private void SetBusy() { foreach (var button in controls.Where(GodotObject.IsInstanceValid)) button.Disabled = busy || button.HasMeta("unavailable"); }

    private void Render()
    {
        if (view is null) return;
        BuildShell();
        if (workspace != "Owner Desk" && selected is null) Heading(workspace);
        if (selected is not null) { RenderProposal(selected); return; }
        switch (workspace)
        {
            case "Owner Desk": Desk(); break;
            case "Club": Club(); break;
            case "Football": Football(); break;
            case "Business": Business(); break;
            case "People": People(); break;
            case "History": History(); break;
        }
    }

    private void Desk() => RenderDesk();

    private void Club()
    {
        var card = Card(); card.AddChild(Label("Stonebridge Football Club", 28));
        card.AddChild(Label("Stonebridge · River district · Navy and lime\nA club built by the town's engineering works, with a loyal local following. Rival: Ravenswick."));
        card.AddChild(Label("Ownership thesis: build sustainable strength.\nFootball authority: manager selects and prepares the team; sporting director recruits; CEO administers the business."));
        card.AddChild(Label("Prototype scope: one acquisition, three seasons, annual capital plans, promotion/relegation and tier-based revenue renewals. Facilities outside hospitality, supporter identity effects, valuation and exits are not yet implemented."));
        card.AddChild(Label($"Invested owner capital: {Money.Format(view.OwnerInvested)}\nCash reserve target: {Money.Format(view.ReserveTarget)}\nOpening plan: {(view.AllocationChosen ? view.History.LastOrDefault(h => h.Command.Allocation is Allocation.PreserveReserve or Allocation.Hospitality or Allocation.Recruitment)?.Intent : "Awaiting owner decision")}"));
    }

    private void Football()
    {
        var market = Card(); market.AddChild(Label("The run-in · recruitment", 24));
        market.AddChild(Label(view.MarketStatus, 16));
        if (view.MarketAvailable)
        {
            market.AddChild(Label("Jonas Reed · Sporting director\nOne midseason mandate: strengthen the forward line or protect the reserve. Both targets come from a rival’s surplus; their two strongest forwards stay with that club.", 14));
            foreach (var option in view.RecruitmentOptions)
            {
                market.AddChild(Label($"{(option.Allocation == Allocation.MidseasonValue ? "Lower cost" : "First choice")} · {option.Player.Name} · {option.Seller}", 18));
                market.AddChild(Label($"Age {option.Player.Age} · Ability {option.Player.Ability} · Fee ceiling {Money.Format(option.FeeCeiling)} · {Money.Format(option.WeeklyWage)}/week", 14));
                market.AddChild(Button(option.Allocation == Allocation.MidseasonValue ? "Review lower-cost forward" : "Review first-choice forward", () => Preview(option.Allocation)));
            }
            if (view.RecruitmentOptions.IsEmpty) market.AddChild(Label("No suitable surplus forward is available. Keeping the squad adds no commitment."));
            market.AddChild(Button("Review keeping the squad", () => Preview(Allocation.MidseasonWait)));
        }
        var cup = Card(); cup.AddChild(Label("Domestic Cup", 24));
        cup.AddChild(Label(view.CupStatus, 18));
        cup.AddChild(Label($"Prize money received this season: {Money.Format(view.CupPrize)}. Only drawn fixtures contribute estimated gate receipts; future draws and all unearned cup prizes are excluded.", 13));
        foreach (var fixture in view.Fixtures.Where(f => f.Competition == Competition.Cup).OrderByDescending(f => f.CupRound))
        {
            var result = view.Results.FirstOrDefault(r => r.FixtureId == fixture.Id);
            var opponent = view.Clubs.Single(c => c.Id == (fixture.Home == view.ClubId ? fixture.Away : fixture.Home)).Name;
            cup.AddChild(Label(result is null ? $"{Cups.RoundName(fixture.CupRound)} · Season week {SeasonWeek(fixture.Week)} · {opponent}"
                : $"{Cups.RoundName(fixture.CupRound)} · {MatchTitle(result)}{(result.Shootout is null ? "" : $" · pens {result.Shootout}")}", 14));
        }
        var table = Card(); table.AddChild(Label($"Division {view.Division}", 24));
        table.AddChild(Label(view.Division == 1 ? "Bottom two: relegation to Division 2. Champions finish first." : view.Division == 3 ? "Top two: promotion to Division 2. This is the lowest division; no relegation." : "Top two: promotion to Division 1. Bottom two: relegation to Division 3.", 14));
        var standings = new GridContainer { Columns = 8 }; table.AddChild(standings);
        foreach (var title in new[] { "POS", "CLUB", "P", "W", "D", "L", "GD", "PTS" }) standings.AddChild(Caption(title));
        var position = 0;
        foreach (var row in view.Table)
        {
            var own = row.ClubId == view.ClubId;
            var values = new[] { (++position).ToString(), row.Name + (own ? " · you" : ""), row.Played.ToString(), row.Won.ToString(), row.Drawn.ToString(), row.Lost.ToString(), (row.GoalsFor - row.GoalsAgainst).ToString("+0;-0;0"), row.Points.ToString() };
            for (var column = 0; column < values.Length; column++)
            {
                var cell = column == 1 ? Label(values[column], 13) : Caption(values[column], own ? new Color("1f3a5f") : Muted, 12);
                cell.CustomMinimumSize = new Vector2((column == 1 ? 150 : 28) * textScale / 100, 26 * textScale / 100);
                cell.SizeFlagsHorizontal = column == 1 ? SizeFlags.ExpandFill : SizeFlags.Fill;
                if (own) cell.AddThemeColorOverride("font_color", new Color("1f3a5f"));
                standings.AddChild(cell);
            }
        }
        table.AddChild(Label("Tie-breaks: points → goal difference → goals scored → tied clubs' head-to-head points and difference → saved seeded lots.", 14));
        foreach (var match in view.Results.TakeLast(5).Reverse()) MatchCard(match);
    }

    private void Business()
    {
        var card = Card(); card.AddChild(Label("Cash first", 24));
        card.AddChild(Label($"Eligible recurring revenue {Money.Format(view.EligibleRevenue)}\nContracted annual squad wages {Money.Format(view.AnnualWages)} · {(decimal)view.AnnualWages / view.EligibleRevenue:P1} of revenue · limit 75%\nPersonal reserve {Money.Format(view.PersonalReserve)} · overdue club obligations {Money.Format(view.Arrears)}"));
        if ((view.Status is CareerStatus.Active or CareerStatus.Administration or CareerStatus.SeasonReview) && view.PersonalReserve > 0)
        {
            var amount = Math.Min(25000000, view.PersonalReserve);
            card.AddChild(Button($"Review {Money.Format(amount)} owner injection", () => Preview(Allocation.InjectCapital, amount)));
            if (view.Arrears > 0 && view.Arrears <= view.PersonalReserve && view.Arrears != amount)
                card.AddChild(Button($"Review injection to cover arrears · {Money.Format(view.Arrears)}", () => Preview(Allocation.InjectCapital, view.Arrears)));
        }
        var forecast = Card(); forecast.AddChild(Label("52-week cash forecast", 24));
        forecast.AddChild(Label(view.Forecast.Assumptions, 15));
        foreach (var point in view.Forecast.Points.Where((_, i) => i % 4 == 0))
            forecast.AddChild(Label($"Career week {point.Week,2}  ·  Base {Money.Format(point.BaseCash)}  ·  Downside {Money.Format(point.DownsideCash)}  ·  Signed net that week {Money.Format(point.KnownNet)}", 16));
        var obligations = Card(); obligations.AddChild(Label("Signed obligations and receipts", 24));
        foreach (var group in view.Obligations.GroupBy(o => (o.Kind, o.StartWeek, o.EndWeek)))
            obligations.AddChild(Label($"{group.Key.Kind} · {Money.Format(group.Sum(o => o.WeeklyAmount))}/week · career weeks {group.Key.StartWeek}–{group.Key.EndWeek}", 16));
        var journal = Card(); journal.AddChild(Label("Cash reconciliation", 24));
        foreach (var group in view.CashLines.GroupBy(line => line.Kind)) journal.AddChild(Label($"{group.Key}: {Money.Format(group.Sum(line => line.Amount))}"));
        journal.AddChild(Label($"Opening {Money.Format(Balance.Load().OpeningClubCash)} + net movements {Money.Format(view.CashLines.Sum(l => l.Amount))} = closing {Money.Format(view.ClubCash)}"));
    }

    private void People()
    {
        var staff = Card(); staff.AddChild(Label("Your leadership team", 24));
        staff.AddChild(Label("Mara Ellis · CEO · Cautious operator\nJonas Reed · Sporting director · Values immediate readiness\nCallum Price · Manager · Selects the squad independently"));
        staff.AddChild(Label("Appointments are fixed in this prototype. Hiring, dismissal, personality policies and contract negotiations are future systems.", 14));
        var squad = Card(); squad.AddChild(Label("Senior squad", 24));
        foreach (var player in view.Squad.OrderBy(p => p.Role).ThenByDescending(p => p.Ability))
            squad.AddChild(Label($"{player.Name} · {player.Role} · Age {player.Age} · Ability {player.Ability}\n{Money.Format(player.WeeklyWage)}/week · Contract through career week {player.ContractEndWeek}", 16));
    }

    private void History()
    {
        foreach (var season in view.SeasonSummaries.Reverse()) SeasonReport(season);
        foreach (var decision in view.History.Reverse())
        {
            var card = Card(); card.AddChild(Label($"Career week {decision.Week} · {decision.Intent}", 24));
            card.AddChild(Label($"{decision.Executive}\nOriginal forecast low: base {Money.Format(decision.OriginalForecast.LowestBase)}, downside {Money.Format(decision.OriginalForecast.LowestDownside)}. These figures retain the original proposal assumptions."));
            RecruitmentHistory(card, decision);
        }
        foreach (var review in view.Reviews.AsEnumerable().Reverse()) ReviewCard(review);
        if (view.History.IsEmpty) content.AddChild(Label("Your ownership history begins with the acquisition."));
    }

    private void Preview(Allocation allocation, long amount = 0, bool reserveException = false) => Start(async () =>
    {
        confirming = false;
        selected = await session.PreviewAsync(new(allocation, amount, reserveException), view.Revision);
        retryCommandId = Guid.NewGuid().ToString("N");
        return "Review the exact terms before committing.";
    });

    private void RenderProposal(Proposal proposal) => DesignProposal(proposal);

    private void Advance(AdvanceTarget target) => Start(async () =>
    {
        var result = await session.AdvanceAsync(target); view = await session.QueryAsync(); inboxSelection = "brief";
        return $"Saved locally · career week {result.Week} · {result.StopReason}";
    });
    private void Save() => Start(async () =>
    {
        var checkpoint = await session.SaveAsync($"Manual week {view.Week}"); return $"Saved locally · {checkpoint.CreatedAt.ToLocalTime():g}";
    }, false);

    private void ShowSaves() => ShowSavePage(0);
    private void ShowSavePage(int offset) => Start(async () =>
    {
        var page = await session.ListSavePageAsync(offset);
        updates.Enqueue(() =>
        {
            auxiliary = "saves"; selected = null; BuildShell(); Heading("Load a checkpoint");
            content.AddChild(Label("Continuing a loaded checkpoint creates a new branch. Existing snapshots remain available."));
            if (offset > 0) content.AddChild(Button("Newer checkpoints", () => ShowSavePage(Math.Max(0, offset - 20))));
            if (page.NextOffset is { } next) content.AddChild(Button("Older checkpoints", () => ShowSavePage(next)));
            foreach (var save in page.Entries)
            {
                var card = Card();
                card.AddChild(Label($"{save.Slot} · career week {save.Week} · {save.CreatedAt.ToLocalTime():g} · Branch {save.BranchId[..6]}"));
                card.AddChild(Button("Load this checkpoint", () => Start(async () =>
                {
                    await session.LoadAsync(save.SnapshotId); view = await session.QueryAsync(); selected = null; workspace = "Owner Desk"; auxiliary = ""; inboxSelection = "resume"; filedReviews.Clear(); showFiled = false; chosenReview = null; chosenMatch = null; chosenHistory = null; confirming = false;
                    return "Loaded validated checkpoint. Next save will preserve a new branch.";
                })));
            }
            if (page.Entries.IsEmpty) content.AddChild(Label("No valid snapshots on this page. Other pages and preserved files remain available."));
        });
        return $"Local vault: {saveDirectory}";
    }, false);

    private void ShowSettings()
    {
        auxiliary = "settings"; selected = null; BuildShell(); Heading("Settings");
        content.AddChild(Label("Guidance and text size affect presentation only. Match results and obligations stay the same."));
        content.AddChild(Button(compact ? "Guidance: compact" : "Guidance: explained", () => { compact = !compact; SavePreferences(); ShowSettings(); }));
        foreach (var scale in new[] { 100, 125, 150 })
            content.AddChild(Button($"Text size {scale}%{(textScale == scale ? " · selected" : "")}", () =>
            {
                textScale = scale; Theme.DefaultFontSize = 14 * scale / 100; SavePreferences(); ShowSettings();
            }));
        content.AddChild(Button($"Continue shortcut: {advanceKey} · change", () => { rebinding = true; status.Text = "Press a new key for Continue (Escape cancels)."; }));
        content.AddChild(Label("Reduced motion: always on in this build. No animation or audio is produced. Keyboard: Tab / Shift+Tab to move focus, Enter to activate, Ctrl+S to save, Escape to return."));
    }
    private void SavePreferences()
    {
        var config = new ConfigFile(); config.SetValue("ui", "compact", compact); config.SetValue("ui", "scale", textScale);
        config.SetValue("ui", "advance_key", (long)advanceKey);
        var result = config.Save("user://preferences.cfg"); if (result != Error.Ok) status.Text = "Could not save preferences: " + result;
    }
    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key || busy) return;
        if (rebinding)
        {
            rebinding = false;
            if (key.Keycode != Key.Escape && Enum.IsDefined(key.Keycode) && key.Keycode is not (Key.None or Key.Tab or Key.Enter or Key.S)) { advanceKey = key.Keycode; SavePreferences(); }
            ShowSettings(); GetViewport().SetInputAsHandled(); return;
        }
        if (key.CtrlPressed && key.Keycode == Key.S) { Save(); GetViewport().SetInputAsHandled(); }
        else if (key.Keycode == Key.Escape) { if (confirming) confirming = false; else selected = null; auxiliary = ""; Render(); GetViewport().SetInputAsHandled(); }
        else if (key.Keycode == advanceKey && auxiliary == "" && view is not null && !key.CtrlPressed && !key.AltPressed && GetViewport().GuiGetFocusOwner() is not Godot.Button)
        { ContinueCareer(); GetViewport().SetInputAsHandled(); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !busy)
            Start(async () => { await session.SaveAsync("Session close"); updates.Enqueue(() => GetTree().Quit()); return "Saved locally."; }, false);
    }
    public override void _ExitTree() { if (session is not null) _ = session.DisposeAsync(); }

    private void Heading(string title) { var panel = Surface(Paper, 18); panel.AddChild(Label(title, 28)); content.AddChild(panel); }
    private Label Label(string text, int size = 14)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        if (size >= 22) label.AddThemeFontOverride("font", serifFont);
        label.SetMeta("base_font_size", size);
        label.AddThemeFontSizeOverride("font_size", size * textScale / 100); return label;
    }
    private Button Button(string title, Action action)
    {
        var button = new Button { Text = title, Alignment = HorizontalAlignment.Left, FocusMode = FocusModeEnum.All, CustomMinimumSize = new Vector2(170, 0), AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += () => { if (!busy) action(); }; controls.Add(button); return button;
    }
    private VBoxContainer Card()
    {
        var panel = new PanelContainer(); var box = new VBoxContainer(); panel.AddChild(box); content.AddChild(panel); return box;
    }
    private void Metric(GridContainer parent, string title, string value)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; parent.AddChild(panel);
        var box = new VBoxContainer(); panel.AddChild(box); box.AddChild(Label(title, 12)); box.AddChild(Label(value, 28));
    }
    private void ReviewCard(Review review) { var card = Card(); card.AddChild(Label($"Career week {review.Week} · {review.Title}", 22)); card.AddChild(Label(review.Evidence)); }

}
