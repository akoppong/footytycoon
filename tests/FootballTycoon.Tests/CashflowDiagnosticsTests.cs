extern alias Endurance;
using System.Text.Json;
using Xunit;
using Diagnostic = Endurance::FootballTycoon.Core;
using Probe = Endurance::FootballTycoon.Endurance;

namespace FootballTycoon.Tests;

public class CashflowDiagnosticsTests
{
    [Fact]
    public void OpeningReportReconcilesAndDoesNotMutateOrConsumeRandomness()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        var before = Diagnostic.WorldCodec.Encode(world);
        var report = Probe.EnduranceRun.Sample(world, "opening").Cashflow;
        Assert.Equal((-1, 0), (report.WindowStartExclusive, report.WindowEndInclusive));
        Assert.Equal(world.Clubs.Sum(c => c.Cash), report.OpeningCash);
        Assert.Equal(report.OpeningCash, report.ClosingCash);
        Assert.Equal(48, report.Clubs.Length); Assert.Single(report.Clubs, c => c.Owned);
        Assert.All(report.Cumulative, c => Assert.Equal((0L, 0L, 0L), (c.Inflows, c.Outflows, c.Net)));
        Assert.Equal(before, Diagnostic.WorldCodec.Encode(world));
        Assert.Equal(JsonSerializer.Serialize(report), JsonSerializer.Serialize(Probe.CashflowDiagnostics.Sample(world)));
    }

    [Fact]
    public void GrossTransfersCancelInAggregateWhileFundingAndUnpaidArrearsStayDistinct()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        var buyer = world.OwnedClub; var seller = world.Clubs.First(c => c.Id != buyer.Id);
        void Post(Diagnostic.Club club, long amount, Diagnostic.CashKind kind) =>
            Diagnostic.Finance.Post(world, Diagnostic.WorldFactory.Account(club.Id), amount, kind, "controlled");
        Post(buyer, -10000, Diagnostic.CashKind.Transfer); Post(seller, 10000, Diagnostic.CashKind.Transfer);
        Diagnostic.Finance.Post(world, "owner", -3000, Diagnostic.CashKind.Injection, "controlled");
        Diagnostic.Finance.Post(world, "seller", 5000, Diagnostic.CashKind.Purchase, "controlled");
        Post(buyer, 3000, Diagnostic.CashKind.Injection); Post(seller, 2000, Diagnostic.CashKind.Rescue);
        Post(buyer, -700, Diagnostic.CashKind.Wages); Post(buyer, 50, Diagnostic.CashKind.Wages);
        world.Arrears.Add(new(new(90000), buyer.Id, 0, 500));
        Diagnostic.WorldFactory.AddObligation(world, buyer.Id, 1, 52, -100, Diagnostic.CashKind.Operations, "Unpaid future commitment");
        var report = Probe.CashflowDiagnostics.Sample(world);
        Assert.Equal(500, report.Arrears); Assert.Equal(500, report.Clubs.Single(c => c.Owned).Arrears);
        Assert.Equal(new Probe.CashflowCategory("Transfer", 10000, 10000, 0), report.Cumulative.Single(c => c.Kind == "Transfer"));
        Assert.Equal(new Probe.CashflowCategory("Wages", 50, 700, -650), report.Cumulative.Single(c => c.Kind == "Wages"));
        Assert.Equal(4350, report.ClosingCash - report.OpeningCash);
        Assert.Equal(0, report.Cumulative.Single(c => c.Kind == "Purchase").Net);
        Assert.Equal(-7650, report.Clubs.Single(c => c.Owned).Cumulative.Sum(c => c.Net));
        Assert.Equal(12000, report.Clubs.Single(c => c.ClubId == seller.Id.Value).Cumulative.Sum(c => c.Net));
        Assert.Equal(report.Cumulative.Sum(c => c.Net), report.Clubs.Sum(c => c.Cumulative.Sum(k => k.Net)));
    }

    [Fact]
    public void RollingWindowExcludesBoundaryAndKeepsWeekZeroCapitalInCumulativeTotals()
    {
        var world = Diagnostic.WorldFactory.Create(2026); var club = world.OwnedClub;
        void Post(int week, long amount, Diagnostic.CashKind kind)
        {
            world.Week = week;
            Diagnostic.Finance.Post(world, Diagnostic.WorldFactory.Account(club.Id), amount, kind, "controlled");
        }
        Post(0, -1000, Diagnostic.CashKind.Construction);
        Assert.Equal(-1000, Probe.CashflowDiagnostics.Sample(world).Window.Sum(c => c.Net));
        Post(52, 200, Diagnostic.CashKind.Broadcast);
        Post(53, -50, Diagnostic.CashKind.Wages);
        Post(104, 300, Diagnostic.CashKind.Tickets);
        var report = Probe.CashflowDiagnostics.Sample(world);
        Assert.Equal((52, 104), (report.WindowStartExclusive, report.WindowEndInclusive));
        Assert.Equal(250, report.Window.Sum(c => c.Net));
        Assert.Equal(-550, report.Cumulative.Sum(c => c.Net));
        Assert.Equal(report.OpeningCash - 800, report.WindowOpeningCash);
        Assert.Equal(report.ClosingCash, report.WindowOpeningCash + report.Window.Sum(c => c.Net));
        Assert.Equal(report.ClosingCash, report.OpeningCash + report.Cumulative.Sum(c => c.Net));
    }

    [Theory]
    [InlineData("cash")]
    [InlineData("future")]
    [InlineData("unknown")]
    [InlineData("category")]
    public void InvalidAccountingIsRejectedInsteadOfPublishingAMisleadingBridge(string defect)
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        if (defect == "cash") world.OwnedClub.Cash++;
        else world.Journal.Add(new(1, defect == "future" ? 1 : 0,
            defect == "unknown" ? "club/999" : Diagnostic.WorldFactory.Account(world.OwnedClubId), 0,
            defect == "category" ? (Diagnostic.CashKind)999 : Diagnostic.CashKind.Wages, "controlled"));
        Assert.Throws<InvalidDataException>(() => Probe.CashflowDiagnostics.Sample(world));
    }
}
