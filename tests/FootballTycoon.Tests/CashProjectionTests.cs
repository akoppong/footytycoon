using FootballTycoon.Core;
using Xunit;

namespace FootballTycoon.Tests;

public class CashProjectionTests
{
    [Theory]
    [InlineData(10, 12)]
    [InlineData(10, 4)]
    [InlineData(50, 80)]
    public void InternalCashChecksKeepExactFiniteWageDatesAndOwnerQuoteValues(int starts, int ends)
    {
        var world = WorldFactory.Create(2026);
        var original = WorldCodec.Encode(world);
        var baseline = Finance.Forecast(world, world.OwnedClubId);
        var cash = Finance.ProjectCash(world, world.OwnedClubId, 50000, 12345, starts, extraEnds: ends);
        var quote = Finance.Forecast(world, world.OwnedClubId, 50000, 12345, starts, extraEnds: ends);
        foreach (var point in cash.Points)
        {
            var payments = Enumerable.Range(1, point.Week).Count(w => w >= starts && w <= ends);
            var difference = 50000 + 12345L * payments;
            Assert.Equal(baseline.Points[point.Week].BaseCash - difference, point.BaseCash);
            Assert.Equal(baseline.Points[point.Week].DownsideCash - difference, point.DownsideCash);
        }
        Assert.Equal(cash.Points.ToArray(), quote.Points.ToArray());
        Assert.Equal(cash.LowestBase, quote.LowestBase); Assert.Equal(cash.LowestDownside, quote.LowestDownside);
        Assert.Equal(cash.LowestBaseWeek, quote.LowestBaseWeek); Assert.Equal(cash.LowestDownsideWeek, quote.LowestDownsideWeek);
        Assert.Equal(original, WorldCodec.Encode(world));
    }

    [Fact]
    public void OwnerForecastStillFingerprintsWorldChangesOutsideItsCashPath()
    {
        var world = WorldFactory.Create(2026);
        var before = Finance.Forecast(world, world.OwnedClubId);
        world.RandomStates["unrelated-saved-state"] = 1;
        var after = Finance.Forecast(world, world.OwnedClubId);
        Assert.Equal(before.Points.ToArray(), after.Points.ToArray());
        Assert.NotEqual(before.Id, after.Id);
    }
}
