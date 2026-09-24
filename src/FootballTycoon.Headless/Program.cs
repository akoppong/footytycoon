using System.Security.Cryptography;
using FootballTycoon.Application;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

var strategy = args.Length > 0 ? Enum.Parse<Allocation>(args[0], true) : Allocation.PreserveReserve;
if (strategy is not (Allocation.PreserveReserve or Allocation.Hospitality or Allocation.Recruitment))
    throw new ArgumentException("Choose PreserveReserve, Hospitality, or Recruitment.");
var seed = args.Length > 1 ? ulong.Parse(args[1]) : 2026;
var directory = args.Length > 2 ? args[2] : Path.Combine("artifacts", "careers", $"{strategy}-{seed}");
await using var session = new GameSession(new LocalSaveVault(directory), seed);
foreach (var allocation in new[] { Allocation.Acquire, strategy })
{
    var view = await session.QueryAsync();
    var proposal = await session.PreviewAsync(new(allocation), view.Revision);
    await session.CommitAsync($"headless-{allocation}", view.Revision, proposal);
}
while ((await session.QueryAsync()).Status is CareerStatus.Active or CareerStatus.Administration or CareerStatus.SeasonReview)
{
    var current = await session.QueryAsync();
    if (current.Status == CareerStatus.SeasonReview)
    {
        var renewal = await session.PreviewAsync(new(Allocation.StartNextSeason), current.Revision);
        await session.CommitAsync($"renew-{current.Season}", current.Revision, renewal);
        current = await session.QueryAsync();
        // The selected strategy applies to season one; later seasons retain cash.
        if (current.Status == CareerStatus.Active)
        {
            var plan = await session.PreviewAsync(new(Allocation.PreserveReserve), current.Revision);
            await session.CommitAsync($"plan-{current.Season}", current.Revision, plan);
        }
    }
    var result = await session.AdvanceAsync(AdvanceTarget.Month);
    var view = await session.QueryAsync();
    Console.WriteLine($"Week {view.Week,2} | {Money.Format(view.ClubCash),12} | {view.Results.Length,2} matches | {result.StopReason}");
    if (result.StopReason == "Owner decision required") break;
}
var final = await session.QueryAsync();
Console.WriteLine($"Outcome: {final.Status}; personal reserve {Money.Format(final.PersonalReserve)}; owner capital {Money.Format(final.OwnerInvested)}");
Console.WriteLine($"Gameplay SHA256: {Convert.ToHexString(SHA256.HashData(await session.ExportCheckpointAsync()))}");
Console.WriteLine($"Local snapshots: {Path.GetFullPath(directory)}");
