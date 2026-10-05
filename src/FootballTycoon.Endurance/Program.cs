using System.Globalization;
using System.Text.Json;
using FootballTycoon.Core;
using FootballTycoon.Endurance;

try { return Run(args); }
catch (Exception error) when (error is ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(error.Message);
    return 64;
}
static int Run(string[] args)
{
    if (args.Length is < 3 or > 5)
    {
        Console.Error.WriteLine("Usage: FootballTycoon.Endurance <seasons 1..50> <seed> <new-output-directory> [PreserveReserve|Hospitality|Recruitment|Training] [max-minutes, default 30]");
        return 64;
    }
    var options = new RunOptions(int.Parse(args[0], CultureInfo.InvariantCulture), ulong.Parse(args[1], CultureInfo.InvariantCulture),
        args.Length > 3 ? Enum.Parse<Allocation>(args[3], true) : Allocation.PreserveReserve,
        TimeSpan.FromMinutes(args.Length > 4 ? double.Parse(args[4], CultureInfo.InvariantCulture) : 30));
    options.Validate();
    var directory = Path.GetFullPath(args[2]);
    if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
        throw new IOException("Choose an empty output directory; existing evidence is never replaced.");
    Directory.CreateDirectory(directory);
    // Exclusive files preserve previous evidence; the tool never opens or alters a game save.
    using var metrics = new StreamWriter(new FileStream(Path.Combine(directory, "metrics.jsonl"), FileMode.CreateNew, FileAccess.Write)) { AutoFlush = true };
    using var summary = new FileStream(Path.Combine(directory, "summary.json"), FileMode.CreateNew, FileAccess.Write);
    var result = EnduranceRun.Execute(options, metrics, Console.Out);
    JsonSerializer.Serialize(summary, result, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine($"{result.Outcome}: {result.CompletedSeasons}/{options.Seasons} seasons; {result.Detail}");
    Console.WriteLine($"Diagnostic evidence: {directory}");
    return result.Outcome == "Completed" ? 0 : 1;
}
