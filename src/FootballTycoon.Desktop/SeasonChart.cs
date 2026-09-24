using Godot;
using FootballTycoon.Application;
using FootballTycoon.Core;

namespace FootballTycoon.Desktop;

// An engine-native chart: same week axis for the journal, forecasts, and authored fixtures.
public partial class SeasonChart : Control
{
    private readonly GameView view;
    private readonly Proposal? proposal;
    private readonly Font font;
    private readonly int fontSize;
    private readonly List<(int Week, long Cash)> actual = [];
    private readonly Forecast? original;
    private readonly long low, high;
    private int inspectedWeek;
    private readonly int start, end;
    public event Action<MatchResult>? MatchSelected;
    public event Action<string>? InspectionChanged;

    public SeasonChart(GameView view, Proposal? proposal, Font font, int textScale, Forecast? selectedOriginal = null)
    {
        this.view = view; this.proposal = proposal; this.font = font; fontSize = 12 * textScale / 100;
        inspectedWeek = view.Week;
        start = proposal?.Renewal is not null ? view.SeasonEndWeek : view.SeasonStartWeek; end = start + Seasons.Weeks;
        CustomMinimumSize = new Vector2(0, 168 + (textScale - 100) * .84f);
        FocusMode = FocusModeEnum.All; MouseDefaultCursorShape = CursorShape.Cross;
        original = selectedOriginal ?? view.History.LastOrDefault(h => h.Command.Allocation != Allocation.Acquire)?.OriginalForecast;
        var cash = proposal?.Renewal is not null ? view.ClubCash : view.SeasonOpeningCash;
        for (var week = start; week <= view.Week; week++)
        {
            cash += view.CashLines.Where(l => proposal?.Renewal is null && l.Sequence > view.SeasonOpeningLedgerSequence && l.Week == week).Sum(l => l.Amount);
            actual.Add((week, cash));
        }
        var values = actual.Select(p => p.Cash)
            .Concat(view.Forecast.Points.Where(p => p.Week >= start && p.Week <= end).SelectMany(p => new[] { p.BaseCash, p.DownsideCash }))
            .Concat((proposal?.Forecast.Points ?? []).Where(p => p.Week >= start && p.Week <= end).SelectMany(p => new[] { p.BaseCash, p.DownsideCash }))
            .Concat((original?.Points ?? []).Where(p => p.Week >= start && p.Week <= end).Select(p => p.BaseCash)).Append(view.ReserveTarget).ToArray();
        var span = Math.Max(10000000L, values.Max() - values.Min());
        low = values.Min() - span / 8; high = values.Max() + span / 8;
        Resized += QueueRedraw;
        FocusEntered += () => { InspectionChanged?.Invoke(WeekDetail(inspectedWeek)); QueueRedraw(); };
        FocusExited += () => { InspectionChanged?.Invoke(""); QueueRedraw(); };
    }
    private float X(int week) => 70 + (Size.X - 92) * (week - start) / 52f;
    // Reserve separate rows for the low annotation, fixture marks and dates at every text scale.
    private float PlotBottom => Size.Y - (3 * fontSize + 38);
    private float Y(long cash) => 22 + (PlotBottom - 22) * (1 - (float)((double)(cash - low) / (high - low)));
    private void Text(Vector2 at, string text, Color color, int? size = null) => DrawString(font, at, text, HorizontalAlignment.Left, -1, size ?? fontSize, color);
    private void Path(IEnumerable<(int Week, long Cash)> values, Color color, bool dashed = false, float width = 2, float dashLength = 4)
    {
        Vector2? previous = null;
        foreach (var point in values)
        {
            var next = new Vector2(X(point.Week), Y(point.Cash));
            if (previous is { } from)
            {
                if (dashed) DrawDashedLine(from, next, color, width, dashLength, true, true);
                else DrawLine(from, next, color, width, true);
            }
            previous = next;
        }
    }
    public override void _Draw()
    {
        if (Size.X < 100) return;
        var navy = new Color("1f3a5f"); var red = new Color("a4361f"); var grey = new Color("5c6472");
        var bottom = PlotBottom; var reserveY = Y(view.ReserveTarget);
        DrawRect(new Rect2(70, reserveY, Size.X - 92, Math.Max(0, bottom - reserveY)), new Color("fbede9"));
        DrawDashedLine(new Vector2(70, reserveY), new Vector2(Size.X - 22, reserveY), red, 1, 4);
        DrawLine(new Vector2(70, bottom), new Vector2(Size.X - 22, bottom), new Color("d9d2c4"));
        Text(new Vector2(0, 16), Main.ShortMoney(high), grey);
        Text(new Vector2(0, reserveY + fontSize / 2), Main.ShortMoney(view.ReserveTarget), red);
        if (original is not null) Path(original.Points.Where(p => p.Week >= start && p.Week <= end).Select(p => (p.Week, p.BaseCash)), grey, true, 1, 1);
        Path(view.Forecast.Points.Where(p => p.Week >= start && p.Week <= end).Select(p => (p.Week, p.BaseCash)), navy, true);
        Path(view.Forecast.Points.Where(p => p.Week >= start && p.Week <= end).Select(p => (p.Week, p.DownsideCash)), red, true);
        if (proposal is not null)
        {
            Path(proposal.Forecast.Points.Where(p => p.Week >= start && p.Week <= end).Select(p => (p.Week, p.BaseCash)), new Color("c9821c"), false, 3);
            Path(proposal.Forecast.Points.Where(p => p.Week >= start && p.Week <= end).Select(p => (p.Week, p.DownsideCash)), new Color("c9821c"), true, 1);
        }
        Path(actual, navy, false, 2.5f);
        DrawLine(new Vector2(X(view.Week), 18), new Vector2(X(view.Week), bottom), red);
        DrawCircle(new Vector2(X(view.Week), Y(view.ClubCash)), 3, navy);
        Text(new Vector2(Math.Clamp(X(view.Week) + 5, 70, Size.X - 130), 14), $"NOW · {Calendar.Day(view.Week).ToUpperInvariant()}", red);
        var minimum = (proposal?.Forecast ?? view.Forecast).Points.Where(p => p.Week >= start && p.Week <= end).MinBy(p => p.DownsideCash);
        if (minimum is not null)
        {
            DrawCircle(new Vector2(X(minimum.Week), Y(minimum.DownsideCash)), 3, red);
            var label = $"Season downside low {Main.ShortMoney(minimum.DownsideCash)} · {Calendar.Day(minimum.Week)}";
            var length = font.GetStringSize(label, HorizontalAlignment.Left, -1, fontSize).X;
            Text(new Vector2(Math.Clamp(X(minimum.Week) - length / 2, 72, Math.Max(72, Size.X - length - 6)), bottom + fontSize + 4), label, red);
        }
        foreach (var fixture in view.Fixtures.Where(f => f.Week > start && f.Week <= end))
        {
            var result = view.Results.FirstOrDefault(r => r.FixtureId == fixture.Id);
            var won = result is not null && (fixture.Competition == Competition.Cup ? result.Winner == view.ClubId
                : result.Home == view.ClubId ? result.HomeGoals > result.AwayGoals : result.AwayGoals > result.HomeGoals);
            var mark = result is null ? (fixture.Competition == Competition.Cup ? "C" : "·")
                : fixture.Competition == Competition.League && result.HomeGoals == result.AwayGoals ? "D" : won ? "W" : "L";
            var color = mark == "W" ? new Color("2f6b48") : mark == "L" ? red : grey;
            var y = bottom + 2 * fontSize + 16;
            DrawRect(new Rect2(X(fixture.Week) - 7, y - fontSize, 15, fontSize + 6), mark == "W" ? new Color("dce8df") : mark == "L" ? new Color("f7e2dc") : new Color("efebe1"));
            Text(new Vector2(X(fixture.Week) - 3, y + 2), mark, color);
        }
        Text(new Vector2(70, Size.Y - 5), $"{Calendar.Day(start)} · season start", grey);
        var closing = $"{Calendar.Day(end)} · season end";
        Text(new Vector2(Size.X - 22 - font.GetStringSize(closing, HorizontalAlignment.Left, -1, fontSize).X, Size.Y - 5), closing, grey);
        if (HasFocus())
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color("e0a33c"), false, 2);
            DrawLine(new Vector2(X(inspectedWeek), 18), new Vector2(X(inspectedWeek), Size.Y - 24), grey);
        }
    }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            inspectedWeek = Math.Clamp(start + (int)Math.Round((motion.Position.X - 70) / Math.Max(1, Size.X - 92) * 52), start, end);
            TooltipText = WeekDetail(inspectedWeek); QueueRedraw();
        }
        else if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            GrabFocus(); OpenMatch(); AcceptEvent();
        }
        else if (input is InputEventKey { Pressed: true } key)
        {
            if (key.Keycode is Key.Left or Key.Right)
            {
                inspectedWeek = Math.Clamp(inspectedWeek + (key.Keycode == Key.Right ? 1 : -1), start, end);
                TooltipText = WeekDetail(inspectedWeek); InspectionChanged?.Invoke(TooltipText); QueueRedraw(); AcceptEvent();
            }
            else if (key.Keycode == Key.Enter) { OpenMatch(); AcceptEvent(); }
        }
    }
    private void OpenMatch()
    {
        if (view.Results.FirstOrDefault(m => m.Week == inspectedWeek) is { } match) MatchSelected?.Invoke(match);
    }
    private string WeekDetail(int week)
    {
        var lines = new List<string> { $"{Calendar.FullDay(week)} · reserve target {Money.Format(view.ReserveTarget)}" };
        if (week <= view.Week) lines.Add("Actual closing cash: " + Money.Format(actual.Single(p => p.Week == week).Cash));
        if (view.Forecast.Points.FirstOrDefault(p => p.Week == week) is { } point)
            lines.Add($"Base: {Money.Format(point.BaseCash)} · downside: {Money.Format(point.DownsideCash)}");
        if (proposal?.Forecast.Points.FirstOrDefault(p => p.Week == week) is { } candidate)
            lines.Add($"Proposed base: {Money.Format(candidate.BaseCash)} · downside: {Money.Format(candidate.DownsideCash)}");
        if (original?.Points.FirstOrDefault(p => p.Week == week) is { } saved) lines.Add("Original base: " + Money.Format(saved.BaseCash));
        if (view.Fixtures.FirstOrDefault(f => f.Week == week) is { } fixture)
        {
            var opponent = view.Clubs.Single(c => c.Id == (fixture.Home == view.ClubId ? fixture.Away : fixture.Home));
            lines.Add($"{opponent.Name} · {(fixture.Home == view.ClubId ? "Home" : "Away")}");
            if (view.Results.Any(m => m.Week == week)) lines.Add("Click or Enter to open match report.");
        }
        return string.Join("\n", lines);
    }
}
