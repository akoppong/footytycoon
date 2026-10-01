using Godot;
using FootballTycoon.Core;

namespace FootballTycoon.Desktop;

// Hero shot 2 "The decision": the opening plans, one expanded with its own cash path. Every figure comes from the previewed proposals.
public partial class Main
{
    private static readonly Color Deep = new("0b1626"), Panel = new("13233a"), Ink = new("f2eee4"), InkSoft = new("c4cbd6"), InkMuted = new("93a1b5"),
        Lime = new("b9db3f"), Gold = new("e8a93a"), Coral = new("f0a58e"), Divider = new("22344b");
    private static readonly Allocation[] OpeningOrder = [Allocation.Hospitality, Allocation.Recruitment, Allocation.Training, Allocation.PreserveReserve];
    private Proposal[]? openingPlans;
    private string openingPlansKey = "", openingPlansFailedKey = "";
    private Allocation? planFocus;
    private string PlansKey => $"{view.Revision}:{view.Week}:{view.ClubCash}:{view.History.Length}";
    private Allocation openingChoice = Allocation.Hospitality;

    private Label Tone(string text, int size, Color color, bool mono = false, bool wrap = true)
    {
        var label = Label(text, size); label.AddThemeColorOverride("font_color", color);
        if (mono) label.AddThemeFontOverride("font", monoFont);
        if (!wrap) label.AutowrapMode = TextServer.AutowrapMode.Off;
        return label;
    }
    // Keeps the status line (for example a commit receipt) unless the load fails.
    private void LoadOpeningPlans()
    {
        var key = PlansKey; var revision = view.Revision; var keep = status.Text.StartsWith("Could not load") ? "Compare the plans, then review the exact terms before committing." : status.Text;
        Start(async () =>
        {
            try
            {
                var plans = new List<Proposal>();
                foreach (var allocation in OpeningOrder) plans.Add(await session.PreviewAsync(new(allocation), revision));
                openingPlans = [.. plans]; openingPlansKey = key;
                return keep;
            }
            catch (Exception error)
            {
                openingPlansFailedKey = key;
                return $"Could not load the cash forecasts: {error.Message}";
            }
        });
    }
    private void Choose(Allocation allocation) { openingChoice = allocation; planFocus = allocation; Render(); }
    private void KeepFocus(Allocation allocation, Button button)
    {
        if (planFocus != allocation) return;
        planFocus = null; button.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void AllocationCards()
    {
        var hero = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; content.AddChild(hero);
        hero.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Deep, ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 18, ContentMarginBottom = 18 });
        var page = Stack(hero, 14);
        var fixture = view.Fixtures.OrderBy(f => f.Week).FirstOrDefault(f => f.Week > view.Week);
        var opponent = fixture is null ? "the next fixture" : ClubName(fixture.Home == view.ClubId ? fixture.Away : fixture.Home);
        var side = fixture is null ? "" : fixture.Home == view.ClubId ? " (H)" : " (A)";
        page.AddChild(Tone($"{(view.Season == 1 ? "FIRST ALLOCATION" : $"SEASON {view.Season} ALLOCATION")} · {ShortMoney(view.ClubCash).ToUpperInvariant()} CLUB CASH · {opponent.ToUpperInvariant()}{side}{(fixture is null ? "" : " " + Calendar.Day(fixture.Week).ToUpperInvariant())}", 12, InkMuted, true));
        page.AddChild(Tone(fixture is null ? "Tonight is the money." : $"{opponent} is next.\nTonight is the money.", GetViewportRect().Size.X >= 1700 ? 36 : 28, Ink));
        if (openingPlans is null || openingPlansKey != PlansKey)
        {
            if (openingPlansFailedKey == PlansKey)
            {
                page.AddChild(Tone("The cash forecasts could not be loaded. Nothing has been committed.", 14, Coral));
                page.AddChild(Button("Retry loading forecasts", () => { openingPlansFailedKey = ""; Render(); })); return;
            }
            page.AddChild(Tone("Loading the cash forecasts for all four plans…", 14, InkMuted)); LoadOpeningPlans(); return;
        }
        var wide = GetViewportRect().Size.X - 218 - 258 >= 1150;
        if (!openingPlans.Any(p => p.Command.Allocation == openingChoice)) openingChoice = OpeningOrder[0];
        var chosen = openingPlans.Single(p => p.Command.Allocation == openingChoice);
        if (wide)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddThemeConstantOverride("separation", 0); page.AddChild(row);
            foreach (var plan in openingPlans)
            {
                var open = plan == chosen;
                var cell = open ? OpenPlan(plan, full: true) : CollapsedPlan(plan);
                cell.SizeFlagsHorizontal = SizeFlags.ExpandFill; cell.SizeFlagsStretchRatio = open ? 2.6f : 1f; row.AddChild(cell);
            }
            return;
        }
        var narrow = textScale > 100;
        BoxContainer layout = narrow ? new VBoxContainer() : new HBoxContainer();
        layout.SizeFlagsHorizontal = SizeFlags.ExpandFill; layout.SizeFlagsVertical = SizeFlags.ExpandFill; layout.AddThemeConstantOverride("separation", 12); page.AddChild(layout);
        var rail = new VBoxContainer { SizeFlagsHorizontal = narrow ? SizeFlags.ExpandFill : SizeFlags.Fill, CustomMinimumSize = new Vector2(narrow ? 0 : 250, 0) };
        rail.AddThemeConstantOverride("separation", 0); layout.AddChild(rail);
        foreach (var plan in openingPlans) rail.AddChild(RailPlan(plan, plan == chosen));
        var detail = OpenPlan(chosen, full: false); detail.SizeFlagsHorizontal = SizeFlags.ExpandFill; layout.AddChild(detail);
    }

    private static string PlanLetter(Allocation a) => ((char)('A' + Array.IndexOf(OpeningOrder, a))).ToString();
    private static string PlanTitle(Allocation a) => a == Allocation.Hospitality ? "Build hospitality suites" : a == Allocation.Recruitment ? "Search for a forward" : a == Allocation.Training ? "Invest in training" : "Keep the reserve";
    private static string PlanShort(Allocation a) => a == Allocation.Hospitality ? "Hospitality build" : a == Allocation.Recruitment ? "Forward search" : a == Allocation.Training ? "Training build" : "Keep the reserve";
    private static string PlanByline(Allocation a) => a == Allocation.Recruitment ? "proposed by Jonas Reed" : a != Allocation.PreserveReserve ? "proposed by Mara Ellis" : "your call";
    private string PlanFigure(Proposal p) => p.Command.Allocation switch
    {
        Allocation.Recruitment => "≤ " + ShortMoney(p.UpfrontCash),
        Allocation.PreserveReserve => "£0 spent",
        _ => ShortMoney(p.UpfrontCash)
    };
    private string PlanBasis(Proposal p) => p.Command.Allocation == Allocation.Hospitality ? $"Cash now · {p.ReviewWeek - view.Week}-week build · {Money.Format(p.WeeklyCost)}/week upkeep after opening"
        : p.Command.Allocation == Allocation.Training ? $"Cash now · {p.ReviewWeek - view.Week}-week build · {Money.Format(p.WeeklyCost)}/week upkeep after opening"
        : p.Command.Allocation == Allocation.Recruitment ? $"Fee paid only if a player signs · up to {Money.Format(p.WeeklyCost)}/week wage"
        : $"Nothing committed · {ShortMoney(view.ClubCash)} held · the midseason window stays open";
    private Forecast Ghost(Proposal p) => openingPlans!.First(o => o.Command.Allocation == (p.Command.Allocation == Allocation.PreserveReserve ? Allocation.Hospitality : Allocation.PreserveReserve)).Forecast;
    private string LowRange(Proposal p) => $"{ShortMoney(p.Forecast.LowestDownside)} – {ShortMoney(p.Forecast.LowestBase)}";

    private Control CollapsedPlan(Proposal p)
    {
        var a = p.Command.Allocation;
        var cell = new PanelContainer { MouseFilter = MouseFilterEnum.Pass };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Colors.Transparent, BorderWidthTop = 4, BorderWidthLeft = 1, BorderColor = a == Allocation.PreserveReserve ? Lime : new Color("33455e"), ContentMarginLeft = 18, ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 14 });
        var box = Stack(cell, 8);
        var pick = PlanButton($"{PlanLetter(a)}\n{PlanTitle(a)}", () => Choose(a), $"Option {PlanLetter(a)}: {PlanTitle(a)}, {PlanFigure(p)}. Lowest forecast {LowRange(p)}");
        box.AddChild(pick); KeepFocus(a, pick);
        box.AddChild(Tone(PlanFigure(p), 30, Ink, wrap: false));
        box.AddChild(Tone(PlanBasis(p), 12, InkSoft));
        box.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        box.AddChild(Tone("LOWEST CASH", 12, InkMuted, true)); box.AddChild(Tone(LowRange(p), 14, Ink, true, false));
        box.AddChild(new LowBar(p.Forecast.LowestDownside, p.Forecast.LowestBase, view.ReserveTarget, view.ClubCash));
        box.AddChild(Tone(Person(a).Name + " →", 12, Gold));
        return cell;
    }
    private Button PlanButton(string text, Action action, string aria)
    {
        var button = Button(text, action); button.TooltipText = aria; button.Flat = true; button.CustomMinimumSize = Vector2.Zero;
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" }) button.AddThemeColorOverride(state, Ink);
        var blank = new StyleBoxFlat { BgColor = Colors.Transparent, ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 2, ContentMarginBottom = 2 };
        button.AddThemeStyleboxOverride("normal", blank); button.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color("101e31"), ContentMarginTop = 2, ContentMarginBottom = 2 });
        button.AddThemeStyleboxOverride("pressed", blank);
        return button;
    }
    private (string Name, string Role, string Initials) Person(Allocation a) => a == Allocation.Recruitment ? ("Jonas Reed", "Sporting director · accountable", "JR")
        : a != Allocation.PreserveReserve ? ("Mara Ellis", "CEO · accountable", "ME") : ("You, the owner", "Mara Ellis manages the cash", "YOU");

    private Control RailPlan(Proposal p, bool open)
    {
        var a = p.Command.Allocation;
        var row = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = open ? Panel : Colors.Transparent, BorderWidthLeft = 4, BorderWidthBottom = 1, BorderColor = open ? Gold : Divider, ContentMarginLeft = 14, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 });
        var box = Stack(row, 4);
        var button = PlanButton($"{PlanLetter(a)}{(open ? " ✓" : "")}   {PlanShort(a)}   {PlanFigure(p)}", () => Choose(a), $"Option {PlanLetter(a)}: {PlanTitle(a)}, {PlanFigure(p)}. Lowest forecast {LowRange(p)}");
        button.AutowrapMode = TextServer.AutowrapMode.Off; box.AddChild(button); KeepFocus(a, button);
        box.AddChild(Tone("Low " + LowRange(p), 12, InkSoft, true, false));
        box.AddChild(new LowBar(p.Forecast.LowestDownside, p.Forecast.LowestBase, view.ReserveTarget, view.ClubCash));
        return row;
    }

    private Control OpenPlan(Proposal p, bool full)
    {
        var a = p.Command.Allocation;
        var cell = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Panel, BorderWidthTop = 4, BorderColor = Gold, ContentMarginLeft = full ? 28 : 18, ContentMarginRight = full ? 28 : 18, ContentMarginTop = 18, ContentMarginBottom = 18 });
        var box = Stack(cell, 8);
        var head = new HBoxContainer(); head.AddThemeConstantOverride("separation", 12); box.AddChild(head);
        var chip = new PanelContainer(); chip.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Gold, ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 2, ContentMarginBottom = 2 });
        chip.AddChild(Tone(PlanLetter(a) + " ✓", 12, Deep, true, false)); chip.SizeFlagsVertical = SizeFlags.ShrinkCenter; head.AddChild(chip);
        var title = Tone(PlanTitle(a), full ? 22 : 18, Ink, wrap: false); title.SizeFlagsHorizontal = SizeFlags.ExpandFill; head.AddChild(title);
        head.AddChild(Tone(PlanFigure(p), full ? 34 : 28, Ink, wrap: false));
        box.AddChild(Tone($"{PlanByline(a)} · {PlanBasis(p)}", 13, InkSoft));

        var ghostName = a == Allocation.PreserveReserve ? "if you build hospitality" : a == Allocation.Recruitment ? "if no player signs" : "if you keep the reserve";
        var drop = a is Allocation.Hospitality or Allocation.Training ? $"▼ −{ShortMoney(p.UpfrontCash)} on approval" : a == Allocation.Recruitment ? "▼ ceiling set aside now" : "No commitment · cash follows the season";
        var chart = new DecisionChart(p.Forecast, Ghost(p), ghostName, view.ReserveTarget, view.ClubCash, monoFont, textScale, drop, .02f, a == Allocation.PreserveReserve ? .6f : .2f);
        chart.CustomMinimumSize = new Vector2(0, (full ? 230 : 170) * (1 + (textScale - 100) / 150f)); box.AddChild(chart);

        var low = Stack(box, 4);
        low.AddChild(new HSeparator());
        low.AddChild(Tone("LOWEST CASH", 12, InkMuted, true)); low.AddChild(Tone(LowRange(p), 18, Ink, true, false));
        low.AddChild(new LowBar(p.Forecast.LowestDownside, p.Forecast.LowestBase, view.ReserveTarget, view.ClubCash));
        var under = p.Forecast.LowestDownside < view.ReserveTarget;
        low.AddChild(Tone(under ? $"▼ Downside {ShortMoney(view.ReserveTarget - p.Forecast.LowestDownside)} under reserve" : $"▲ Clears reserve by {ShortMoney(p.Forecast.LowestDownside - view.ReserveTarget)}", 13, under ? Coral : InkSoft));
        if (full) { low.AddChild(Tone("STILL UNCERTAIN", 12, InkMuted, true)); low.AddChild(Tone(p.Uncertainty, 13, InkSoft)); }
        var person = Person(a);
        low.AddChild(Tone($"{person.Name} · {person.Role}", 13, Ink));
        foreach (var reason in p.BlockingReasons) low.AddChild(Tone("BLOCKED · " + reason, 13, Coral));
        var go = Button("Review final terms →", () => Preview(a));
        go.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; go.Alignment = HorizontalAlignment.Center; go.CustomMinimumSize = new Vector2(0, 0);
        foreach (var (state, color) in new[] { ("normal", Gold), ("hover", new Color("f2bc5a")), ("pressed", new Color("f2bc5a")), ("disabled", new Color("5a5340")) })
            go.AddThemeStyleboxOverride(state, new StyleBoxFlat { BgColor = color, ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 12, ContentMarginBottom = 12 });
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_disabled_color" }) go.AddThemeColorOverride(state, Deep);
        low.AddChild(go); KeepFocus(a, go);
        return cell;
    }
}
