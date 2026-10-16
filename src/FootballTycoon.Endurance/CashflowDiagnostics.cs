using FootballTycoon.Core;

namespace FootballTycoon.Endurance;

public sealed record CashflowCategory(string Kind, long Inflows, long Outflows, long Net);
public sealed record ClubCashflow(int ClubId, bool Owned, long OpeningCash, long ClosingCash, long Arrears,
    CashflowCategory[] Cumulative, long WindowOpeningCash, CashflowCategory[] Window);
public sealed record CashflowReport(int WindowStartExclusive, int WindowEndInclusive, long OpeningCash, long ClosingCash,
    long Arrears, CashflowCategory[] Cumulative, long WindowOpeningCash, CashflowCategory[] Window, ClubCashflow[] Clubs);

// Cash movements only, not accounting profit. Owner/seller accounts and unpaid commitments are not club receipts/payments.
public static class CashflowDiagnostics
{
    public static CashflowReport Sample(World world)
    {
        var start = Math.Max(-1, world.Week - Seasons.Weeks);
        var accounts = world.Clubs.Select(c => WorldFactory.Account(c.Id)).ToHashSet(StringComparer.Ordinal);
        var entries = world.Journal.Where(j => j.Account.StartsWith("club/", StringComparison.Ordinal)).ToArray();
        if (entries.Any(j => !accounts.Contains(j.Account) || j.Week < 0 || j.Week > world.Week || !Enum.IsDefined(j.Kind)))
            throw new InvalidDataException("Cashflow diagnostics require dated entries for known club accounts and cash categories.");
        var byAccount = entries.ToLookup(j => j.Account, StringComparer.Ordinal);
        var clubs = world.Clubs.OrderBy(c => c.Id.Value).Select(c =>
        {
            var ledger = byAccount[WorldFactory.Account(c.Id)].ToArray();
            var cumulative = Categories(ledger);
            if (checked(c.OpeningCash + cumulative.Sum(k => k.Net)) != c.Cash)
                throw new InvalidDataException($"Cashflow reconciliation failed for club {c.Id.Value}.");
            var window = Categories(ledger.Where(j => j.Week > start));
            return new ClubCashflow(c.Id.Value, c.Id == world.OwnedClubId, c.OpeningCash, c.Cash,
                world.Arrears.Where(a => a.ClubId == c.Id).Sum(a => a.Amount), cumulative,
                checked(c.Cash - window.Sum(k => k.Net)), window);
        }).ToArray();
        return new(start, world.Week, clubs.Sum(c => c.OpeningCash), clubs.Sum(c => c.ClosingCash), clubs.Sum(c => c.Arrears),
            Combine(clubs.SelectMany(c => c.Cumulative)), clubs.Sum(c => c.WindowOpeningCash),
            Combine(clubs.SelectMany(c => c.Window)), clubs);
    }

    private static CashflowCategory[] Categories(IEnumerable<LedgerEntry> entries)
    {
        var kinds = entries.ToLookup(j => j.Kind);
        return Enum.GetValues<CashKind>().Select(kind =>
        {
            var inflows = kinds[kind].Where(j => j.Amount > 0).Sum(j => j.Amount);
            var outflows = kinds[kind].Where(j => j.Amount < 0).Sum(j => checked(-j.Amount));
            return new CashflowCategory(kind.ToString(), inflows, outflows, checked(inflows - outflows));
        }).ToArray();
    }

    private static CashflowCategory[] Combine(IEnumerable<CashflowCategory> categories) => categories.GroupBy(c => c.Kind)
        .Select(g => new CashflowCategory(g.Key, g.Sum(c => c.Inflows), g.Sum(c => c.Outflows), g.Sum(c => c.Net))).ToArray();
}
