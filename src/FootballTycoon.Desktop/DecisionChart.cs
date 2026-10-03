using Godot;
using FootballTycoon.Core;

namespace FootballTycoon.Desktop;

// The opening-decision cash path for one plan: base, downside band, the plan it replaces (ghost) and the reserve target.
public partial class DecisionChart : Control
{
    private static readonly Color Ivory = new("f2eee4"), Lime = new("b9db3f"), Coral = new("e88b6e"), Slate = new("4b5d76"),
        Muted = new("93a1b5"), Amber = new("e8a93a"), Axis = new("33455e");
    private readonly Forecast plan, ghost;
    private readonly long reserve, top, floor;
    private readonly Font font;
    private readonly int fs;
    private readonly string ghostLabel, note;
    private readonly float noteX, noteY;
    private readonly ForecastPoint baseLow, downLow;

    public DecisionChart(Forecast plan, Forecast ghost, string ghostLabel, long reserve, long cash, Font font, int textScale, string note, float noteX, float noteY)
    {
        this.plan = plan; this.ghost = ghost; this.ghostLabel = ghostLabel; this.reserve = reserve; this.font = font;
        this.note = note; this.noteX = noteX; this.noteY = noteY;
        fs = 12 * textScale / 100;
        var all = plan.Points.Concat(ghost.Points).SelectMany(p => new[] { p.BaseCash, p.DownsideCash }).Append(cash).Append(reserve).ToArray();
        top = all.Max(); floor = Math.Min(0, all.Min());
        baseLow = plan.Points.MinBy(p => p.BaseCash)!; downLow = plan.Points.MinBy(p => p.DownsideCash)!;
        CustomMinimumSize = new Vector2(0, 150 + (textScale - 100) * .6f);
        SizeFlagsVertical = SizeFlags.ExpandFill; MouseFilter = MouseFilterEnum.Pass;
        TooltipText = $"Season cash forecast. Base lowest {Main.ShortMoney(baseLow.BaseCash)}, downside lowest {Main.ShortMoney(downLow.DownsideCash)} on {Calendar.FullDay(downLow.Week)}. Reserve target {Main.ShortMoney(reserve)}.";
        Resized += QueueRedraw;
    }

    private float Left => fs * 4.6f;
    private float Right => Size.X - fs * 12.4f;
    private float PlotTop => fs;
    private float PlotBottom => Size.Y - fs * 1.9f;
    private int Start => plan.Points[0].Week;
    private int End => plan.Points[^1].Week;
    private float X(int week) => Left + (Right - Left) * (week - Start) / Math.Max(1, End - Start);
    private float Y(long cash) => PlotTop + (PlotBottom - PlotTop) * (1f - (float)((double)(cash - floor) / Math.Max(1, top - floor)));
    private void Text(Vector2 at, string text, Color color, bool right = false)
    {
        if (right) at.X -= font.GetStringSize(text, HorizontalAlignment.Left, -1, fs).X;
        DrawString(font, at, text, HorizontalAlignment.Left, -1, fs, color);
    }
    private void Line(IEnumerable<(int Week, long Cash)> values, Color color, float width, bool dashed = false, float dash = 6)
    {
        Vector2? previous = null;
        foreach (var (week, cash) in values)
        {
            var next = new Vector2(X(week), Y(cash));
            if (previous is { } from) { if (dashed) DrawDashedLine(from, next, color, width, dash, true, true); else DrawLine(from, next, color, width, true); }
            previous = next;
        }
    }

    public override void _Draw()
    {
        if (Size.X < 160) return;
        var points = plan.Points;
        for (var i = 1; i < points.Length; i++)
            DrawColoredPolygon([new(X(points[i - 1].Week), Y(points[i - 1].BaseCash)), new(X(points[i].Week), Y(points[i].BaseCash)),
                new(X(points[i].Week), Y(points[i].DownsideCash)), new(X(points[i - 1].Week), Y(points[i - 1].DownsideCash))], new Color(Ivory, .09f));
        DrawLine(new Vector2(Left, PlotBottom), new Vector2(Right, PlotBottom), Axis);
        DrawDashedLine(new Vector2(Left, Y(reserve)), new Vector2(Right, Y(reserve)), Lime, 1.5f, 3);
        Text(new Vector2(0, PlotTop + fs * .4f), Main.ShortMoney(top), Muted);
        Text(new Vector2(0, Y(reserve) + fs * .4f), Main.ShortMoney(reserve), Lime);
        if (floor < 0)
        {
            Text(new Vector2(0, PlotBottom + fs * .2f), Main.ShortMoney(floor), Muted);
            DrawLine(new Vector2(Left, Y(0)), new Vector2(Right, Y(0)), Muted);
            Text(new Vector2(0, Y(0) + fs * .4f), "£0", Muted);
        }
        else if (Math.Abs(PlotBottom - Y(reserve)) > fs * 1.4f)
            Text(new Vector2(0, PlotBottom + fs * .2f), "£0", Muted);
        Line(ghost.Points.Select(p => (p.Week, p.BaseCash)), Slate, 2, true, 3);
        Line(points.Select(p => (p.Week, p.BaseCash)), Ivory, 3.5f);
        Line(points.Select(p => (p.Week, p.DownsideCash)), Coral, 2.5f, true, 9);

        var labelX = Right + fs;
        // A fixed legend stays readable when end balances coincide or cluster near the top.
        DrawMultilineString(font, new Vector2(labelX, PlotTop + fs), ghostLabel, HorizontalAlignment.Left, Size.X - labelX, fs, 2, Muted);
        Text(new Vector2(labelX, PlotTop + fs * 4), "━ Base " + Main.ShortMoney(points[^1].BaseCash), Ivory);
        Text(new Vector2(labelX, PlotTop + fs * 5.5f), "╍ Downside " + Main.ShortMoney(points[^1].DownsideCash), Coral);
        Text(new Vector2(labelX, PlotTop + fs * 7), "┄ Reserve", Lime);

        var bx = X(baseLow.Week);
        DrawCircle(new Vector2(bx, Y(baseLow.BaseCash)), 7, new Color(Amber, .3f)); DrawCircle(new Vector2(bx, Y(baseLow.BaseCash)), 4.5f, Ivory);
        var dx = X(downLow.Week); var dy = Y(downLow.DownsideCash);
        DrawColoredPolygon([new(dx, dy - 5), new(dx + 5, dy), new(dx, dy + 5), new(dx - 5, dy)], Coral);
        var callout = "Lowest · " + Calendar.Day(downLow.Week);
        var below = dy > (PlotTop + PlotBottom) / 2;
        Text(new Vector2(Math.Clamp(dx - fs * 3, Left, Math.Max(Left, Right - font.GetStringSize(callout, HorizontalAlignment.Left, -1, fs).X)),
            below ? dy - fs * 1.1f : dy + fs * 1.8f), callout, Amber);
        if (note.Length > 0) Text(new Vector2(Left + (Right - Left) * noteX, PlotTop + (PlotBottom - PlotTop) * noteY), note, note.StartsWith('▼') ? Coral : Muted);

        var months = 5;
        for (var m = 0; m < months; m++)
        {
            var week = Start + (End - Start) * m / (months - 1);
            var label = Calendar.Date(week).ToString("MMM", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
            var x = X(week); var size = font.GetStringSize(label, HorizontalAlignment.Left, -1, fs).X;
            Text(new Vector2(Math.Clamp(x - size / 2, 0, Size.X - size), Size.Y - 3), label, Muted);
        }
    }
}

// Lowest forecast cash as a range on the cash axis, with the reserve target ticked in lime.
public partial class LowBar : Control
{
    private readonly float from, to, reserveAt;
    public LowBar(long downside, long baseLow, long reserve, long axisTop)
    {
        var top = (float)Math.Max(1, axisTop);
        from = Math.Clamp(downside / top, 0, 1); to = Math.Clamp(baseLow / top, 0, 1); reserveAt = Math.Clamp(reserve / top, 0, 1);
        CustomMinimumSize = new Vector2(0, 14); MouseFilter = MouseFilterEnum.Ignore;
    }
    public override void _Draw()
    {
        var y = (Size.Y - 8) / 2;
        DrawRect(new Rect2(0, y, Size.X, 8), new Color("22344b"));
        DrawRect(new Rect2(Size.X * from, y, Math.Max(2, Size.X * (to - from)), 8), new Color("f2eee4"));
        DrawRect(new Rect2(Size.X * reserveAt - 1, y - 5, 2, 18), new Color("b9db3f"));
    }
}
