namespace FootballTycoon.Core;

// Published fictional-world tier terms. No speculative promotion income before renewal.
public static class Pyramid
{
    public static decimal Factor(int division) => division switch
    {
        1 => 1.6m, 2 => 1m, 3 => .65m,
        _ => throw new ArgumentOutOfRangeException(nameof(division))
    };
    public static long Broadcast(int division) => Money.Scale(Balance.Load().AnnualBroadcast, Factor(division));
    public static long Sponsor(int division) => Money.Scale(Balance.Load().AnnualSponsor, Factor(division));
    public static long TradingBudget(Club club, long openingBudget) => Money.Scale(openingBudget,
        Factor(club.Division) / Factor(club.OpeningDivision == 0 ? club.Division : club.OpeningDivision));

    public static Dictionary<ClubId, int> NextDivisions(World world)
    {
        var destinations = world.Clubs.ToDictionary(c => c.Id, c => c.Division);
        // Determine every movement from the same closed tables, before mutating any club.
        for (var division = 1; division <= 3; division++)
        {
            var table = Simulation.Table(world, division);
            if (division > 1) foreach (var row in table.Take(2)) destinations[row.ClubId] = division - 1;
            if (division < 3) foreach (var row in table.TakeLast(2)) destinations[row.ClubId] = division + 1;
        }
        return destinations;
    }

    public static string Outcome(int from, int to) => to < from ? $"Promoted to Division {to}."
        : to > from ? $"Relegated to Division {to}." : $"Retained a place in Division {to}.";
}
