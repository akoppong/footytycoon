using System.Globalization;

namespace FootballTycoon.Core;

// Dates are a presentation of the integer career-week clock. Each weekly tick is a Saturday; every season
// re-anchors to the first Saturday of July, so the calendar never drifts and season N week 52 falls in June.
public static class Calendar
{
    public const int FirstSeasonYear = 2026;

    // Season-relative weeks for seasons that use the dated layout (see World.CalendarStartSeason).
    // Cup preliminary round in early August, league from mid-August to mid-May with international-break
    // gaps and a festive run, cup final in late May, June as the close season.
    private static readonly int[] FirstHalf = [7, 8, 9, 10, 12, 13, 14, 15, 17, 18, 20, 22, 23, 24, 25];
    private static readonly int[] SecondHalf = [26, 27, 29, 30, 31, 33, 34, 35, 36, 38, 40, 41, 44, 45, 46];
    private static readonly int[] CupRounds = [6, 19, 28, 37, 42, 47];
    // Pre-calendar layout, kept so schema-5 saves finish their current season unchanged.
    private static readonly int[] LegacyFirstHalf = Enumerable.Range(3, 15).ToArray();
    private static readonly int[] LegacySecondHalf = Enumerable.Range(22, 15).ToArray();
    private static readonly int[] LegacyCupRounds = [2, 19, 20, 37, 43, 47];

    public static bool IsDated(World world, int season) => season >= world.CalendarStartSeason;
    public static int LeagueWeek(World world, int round, bool returnLeg) =>
        (IsDated(world, world.Season) ? returnLeg ? SecondHalf : FirstHalf : returnLeg ? LegacySecondHalf : LegacyFirstHalf)[round];
    public static int CupWeek(World world, int round) => (IsDated(world, world.Season) ? CupRounds : LegacyCupRounds)[round - 1];

    public static int SeasonOf(int careerWeek) => careerWeek <= 0 ? 1 : (careerWeek - 1) / Seasons.Weeks + 1;
    public static DateOnly Date(int careerWeek)
    {
        var season = SeasonOf(careerWeek);
        return Anchor(season).AddDays(7 * (careerWeek - (season - 1) * Seasons.Weeks - 1));
    }

    public static string Day(int careerWeek) => Date(careerWeek).ToString("ddd d MMM", CultureInfo.InvariantCulture);
    public static string FullDay(int careerWeek) => Date(careerWeek).ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture);
    public static string SeasonName(int season) => $"{FirstSeasonYear + season - 1}/{(FirstSeasonYear + season) % 100:00}";

    private static DateOnly Anchor(int season)
    {
        var july = new DateOnly(FirstSeasonYear + season - 1, 7, 1);
        return july.AddDays(((int)DayOfWeek.Saturday - (int)july.DayOfWeek + 7) % 7);
    }
}
