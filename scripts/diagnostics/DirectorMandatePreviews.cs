using System.Security.Cryptography;
using System.Text.Json;
using FootballTycoon.Core;
using FootballTycoon.Infrastructure;

if (args.Length != 2) throw new ArgumentException("Usage: <E01B checkpoint root> <new output directory>");
if (Directory.Exists(args[1])) throw new IOException("Choose a new evidence directory.");
Directory.CreateDirectory(args[1]);
var json = new JsonSerializerOptions { WriteIndented = true };
var results = new List<object>();
var expectedHashes = new Dictionary<int, string>
{
    [2026] = "DC33DBA821D4A6A833D17477ECD46DBDE808467556648A54EFA0352D299FFF1D",
    [2030] = "97BDF6C558EC3B240EA755FCC09CC5F09BA4D3B1413B09FB751818FF4A167283"
};
foreach (var seed in new[] { 2026, 2030 })
{
    var directory = Path.Combine(args[0], seed.ToString());
    using var manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "manifest.json")));
    var snapshotId = manifest.RootElement.GetProperty("SnapshotId").GetString()!;
    var vaultDirectory = Path.Combine(directory, "application-vault");
    var savePath = Path.Combine(vaultDirectory, snapshotId + ".ftsave");
    var originalContainer = File.ReadAllBytes(savePath);
    var originalGameplay = File.ReadAllBytes(Path.Combine(directory, "checkpoint.json"));
    var loaded = new LocalSaveVault(vaultDirectory).Read(snapshotId);
    if (!loaded.WorldBytes.AsSpan().SequenceEqual(originalGameplay) || Hash(loaded.WorldBytes) != expectedHashes[seed])
        throw new InvalidDataException("The save does not match the registered E01B checkpoint.");
    var world = WorldCodec.Decode(loaded.WorldBytes); // Production migration, preserving the schema-23 source.
    var currentBefore = WorldCodec.Encode(world);
    var current = FreeAgents.RecommendWithEvidence(world) ?? throw new InvalidDataException("Current recommendation missing.");
    var currentPreview = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
    RequirePreview(world, currentBefore, current, currentPreview);

    // E04 behavior probe only: E05 owns the public mandate-change transaction.
    var before = world.Mandates;
    var after = new MandateState(before.Manager, DirectorMandate.LowerCostValue, before.Revision + 1);
    world.Revision++;
    world.Commands.Add("e04-value-probe", new("e04-value-probe", "e04-value-probe", world.Revision, "Diagnostic mandate transition"));
    world.MandateHistory.Add(new("e04-value-probe", world.Week, world.Revision, before, after));
    world.Mandates = after;
    WorldFactory.Validate(world);
    var valueBefore = WorldCodec.Encode(world);
    var value = FreeAgents.RecommendWithEvidence(world) ?? throw new InvalidDataException("Value recommendation missing.");
    var valuePreview = Proposals.Preview(world, new(Allocation.FreeAgentRecruitment), world.Revision);
    RequirePreview(world, valueBefore, value, valuePreview);
    if (value.Evidence.Reason != RecommendationReason.LowerCostAlternative || current.Terms.Player.Id == value.Terms.Player.Id)
        throw new InvalidDataException("Registered checkpoint has no actual Value alternative.");
    if (!originalContainer.AsSpan().SequenceEqual(File.ReadAllBytes(savePath))
        || !originalGameplay.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(directory, "checkpoint.json"))))
        throw new InvalidDataException("Original evidence changed.");
    var result = new
    {
        Seed = seed, SourceSave = savePath, SnapshotId = snapshotId,
        GameplaySha256 = Hash(originalGameplay), ContainerSha256 = Hash(originalContainer),
        SourcePreserved = true, PreviewPure = true, world.Week, world.Season,
        MandateSetup = "Coherent E02 diagnostic transition; public E05 command not under test",
        Current = current, CurrentPreview = currentPreview,
        Value = value, ValuePreview = valuePreview
    };
    File.WriteAllText(Path.Combine(args[1], seed + ".json"), JsonSerializer.Serialize(result, json));
    results.Add(new { Seed = seed, current.Terms.Player.Name, CurrentPersonId = current.Terms.Player.Id.Value,
        ValueName = value.Terms.Player.Name, ValuePersonId = value.Terms.Player.Id.Value,
        Current = current.Evidence.Selected, Value = value.Evidence.Selected,
        CurrentPreviewFeasible = currentPreview.BlockingReasons.IsEmpty, ValuePreviewFeasible = valuePreview.BlockingReasons.IsEmpty,
        SourcePreserved = true, PreviewPure = true });
}
File.WriteAllText(Path.Combine(args[1], "results.json"), JsonSerializer.Serialize(new
{
    Contract = "E00-A1", CoreSha256 = Hash(File.ReadAllBytes(typeof(World).Assembly.Location)), Results = results
}, json));
Console.WriteLine("PASS: both original E01B saves migrated; Current and Value production previews feasible and pure; source bytes preserved.");

static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
static void RequirePreview(World world, byte[] before, FreeAgentRecommendation recommendation, Proposal preview)
{
    if (!before.AsSpan().SequenceEqual(WorldCodec.Encode(world))) throw new InvalidDataException("Preview mutated state.");
    if (!preview.BlockingReasons.IsEmpty || preview.Recruitment != recommendation.Terms
        || preview.TotalCommitment != recommendation.Evidence.Selected.TotalCommitment || preview.UpfrontCash != 0)
        throw new InvalidDataException("Actual production preview does not validate the recommendation.");
}
