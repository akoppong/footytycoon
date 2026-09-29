namespace FootballTycoon.Core;

public static class Facilities
{
    public const int TrainingBuildWeeks = 32;
    public static long TrainingCost(int currentLevel) => 65000000L + 15000000L * Math.Clamp(currentLevel, 0, 2);
    public static long TrainingUpkeep(int currentLevel) => 100000L + 25000L * Math.Clamp(currentLevel, 0, 2);
    public static int GrowthBonus(int level) => level switch { 1 => 10, 2 => 16, 3 => 20, _ => 0 };
    public static bool IsConstruction(Allocation allocation) => allocation is Allocation.Hospitality or Allocation.Training;

    // Accrue before this week's deliveries. The squad after negotiations receives the week's training:
    // a transfer keeps previous weeks' exposure and trains at the buyer from its transfer week.
    public static void AccrueTraining(World world)
    {
        foreach (var club in world.Clubs)
        {
            var bonus = GrowthBonus(club.TrainingLevel);
            if (bonus == 0) continue;
            club.Players = club.Players.Select(p => p with { TrainingExposure = checked(p.TrainingExposure + bonus) }).ToList();
        }
    }
}
