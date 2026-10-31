using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

// Standalone diagnostic: load the actual old and new Core binaries into separate contexts.
// No game dependency is compiled into this tool and neither source checkpoint is rewritten.
if (args.Length != 4) throw new ArgumentException("Expected old Core DLL, new Core DLL, E01B seed directory root, new output directory.");
var oldApi = new CoreApi(args[0]);
var newApi = new CoreApi(args[1]);
var output = Path.GetFullPath(args[3]);
if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new IOException("Use a fresh output directory.");
Directory.CreateDirectory(output);
var results = new List<object>();
Run("fresh-2026", oldApi.Create(2026), newApi.Create(2026), 104, true);
foreach (var seed in new[] { 2026, 2030 })
{
    var path = Path.Combine(args[2], seed.ToString(), "checkpoint.json");
    var source = File.ReadAllBytes(path);
    Run($"checkpoint-{seed}", oldApi.Decode(source), newApi.Decode(source), 104, false);
    // Approve in the old runtime BEFORE migration: its IDs/terms must survive exactly.
    var pending = oldApi.Decode(source);
    var proposal = oldApi.Preview(pending, "FreeAgentRecruitment");
    oldApi.Commit(pending, "legacy-pending-approach", proposal);
    var pendingBytes = oldApi.Encode(pending);
    Run($"legacy-pending-{seed}", oldApi.Decode(pendingBytes), newApi.Decode(pendingBytes), 104, false);
    if (!source.AsSpan().SequenceEqual(File.ReadAllBytes(path))) throw new InvalidDataException("Source checkpoint changed.");
}
var report = new
{
    OldCoreSha256 = Hash(File.ReadAllBytes(args[0])),
    NewCoreSha256 = Hash(File.ReadAllBytes(args[1])),
    Normalization = "Only schema/simulation version and explicitly empty/default mandate state; newly generated proposal/forecast IDs mapped bijectively at preview creation and only at typed ID/reference positions. Imported IDs are not mapped.",
    Results = results
};
File.WriteAllText(Path.Combine(output, "equivalence.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"DEFAULT EQUIVALENCE PASS: {results.Count} actual-runtime scenarios; report {output}");

void Run(string label, object oldWorld, object newWorld, int endWeek, bool opening)
{
    var identities = new IdentityMap();
    var checks = 0;
    CheckWorld("initial/import");
    if (opening) { Command("Acquire"); Command("PreserveReserve"); }
    else if (CoreApi.Json(oldWorld)["FreeAgentBids"]!.AsArray().Count == 0) Command("FreeAgentRecruitment");
    while (CoreApi.Get<int>(oldWorld, "Week") < endWeek)
    {
        if (CoreApi.Get<object>(oldWorld, "Status").ToString() == "SeasonReview")
        {
            Command("StartNextSeason", true); Command("PreserveReserve"); Command("FreeAgentRecruitment");
        }
        if ((bool)oldApi.Call("RecruitmentMarket", "Available", oldWorld)) Command("MidseasonWait");
        var before = CoreApi.Get<int>(oldWorld, "Week");
        var a = oldApi.Call("Simulation", "AdvanceWeek", oldWorld);
        var b = newApi.Call("Simulation", "AdvanceWeek", newWorld);
        Equal(CoreApi.Json(a), identities.Normalize(CoreApi.Json(b)), $"{label}/advance-result/{before}");
        if (CoreApi.Get<int>(oldWorld, "Week") != before + 1) throw new InvalidDataException($"{label} failed to advance at {before}.");
        CheckWorld($"week-{before + 1}");
        // Round-trip the new schema regularly, without changing the old control runtime.
        if ((before + 1) % 13 == 0)
        {
            var bytes = newApi.Encode(newWorld);
            newWorld = newApi.Decode(bytes);
            if (!bytes.AsSpan().SequenceEqual(newApi.Encode(newWorld))) throw new InvalidDataException("Current-schema round trip changed bytes.");
            CheckWorld($"reload-{before + 1}");
        }
    }
    if (CoreApi.Get<object>(oldWorld, "Status").ToString() != "SeasonReview") throw new InvalidDataException("Missing final review.");
    results.Add(new
    {
        Scenario = label,
        Checks = checks,
        FinalWeek = endWeek,
        GeneratedIdentityPairs = identities.Pairs,
        OldFinalSha256 = Hash(oldApi.Encode(oldWorld)),
        NewFinalSha256 = Hash(newApi.Encode(newWorld))
    });
    Console.WriteLine($"{label}: {checks} strict comparisons; week {endWeek} review.");

    void Command(string allocation, bool declineAcademy = false)
    {
        var beforeA = oldApi.Encode(oldWorld); var beforeB = newApi.Encode(newWorld);
        var a = oldApi.Preview(oldWorld, allocation, declineAcademy);
        var b = newApi.Preview(newWorld, allocation, declineAcademy);
        if (!beforeA.AsSpan().SequenceEqual(oldApi.Encode(oldWorld)) || !beforeB.AsSpan().SequenceEqual(newApi.Encode(newWorld)))
            throw new InvalidDataException("Preview changed state or RNG.");
        var jsonA = CoreApi.Json(a); var jsonB = CoreApi.Json(b);
        identities.Add(jsonA["Id"]!.GetValue<string>(), jsonB["Id"]!.GetValue<string>());
        identities.Add(jsonA["Forecast"]!["Id"]!.GetValue<string>(), jsonB["Forecast"]!["Id"]!.GetValue<string>());
        Equal(jsonA, identities.Normalize(jsonB, proposal: true), $"{label}/preview/{allocation}");
        if (jsonA["BlockingReasons"]!.AsArray().Count > 0) throw new InvalidDataException($"Unexpected blocked {allocation}: {jsonA["BlockingReasons"]}");
        var id = $"equivalence-{allocation}-{CoreApi.Get<long>(oldWorld, "Revision")}";
        var receiptA = oldApi.Commit(oldWorld, id, a); var receiptB = newApi.Commit(newWorld, id, b);
        Equal(CoreApi.Json(receiptA), identities.Normalize(CoreApi.Json(receiptB)), $"{label}/receipt/{allocation}");
        CheckWorld($"commit-{allocation}");
    }

    void CheckWorld(string step)
    {
        oldApi.Call("WorldFactory", "Validate", oldWorld); newApi.Call("WorldFactory", "Validate", newWorld);
        var a = JsonNode.Parse(oldApi.Encode(oldWorld))!.AsObject();
        var b = JsonNode.Parse(newApi.Encode(newWorld))!.AsObject();
        if (a["SchemaVersion"]!.GetValue<int>() != 23 || b["SchemaVersion"]!.GetValue<int>() != 24)
            throw new InvalidDataException("Expected actual schema23 and schema24 runtimes.");
        if (a["SimulationVersion"]!.GetValue<string>() != "compact-journal-23" || b["SimulationVersion"]!.GetValue<string>() != "owner-mandates-24")
            throw new InvalidDataException("Unexpected simulation version.");
        a.Remove("SchemaVersion"); b.Remove("SchemaVersion"); a.Remove("SimulationVersion"); b.Remove("SimulationVersion");
        // Fail closed when the persistence contract changes; never strip arbitrary extra fields.
        var mandates = b["Mandates"]?.AsObject() ?? throw new InvalidDataException("Missing mandate state.");
        Equal(JsonNode.Parse("{\"Manager\":0,\"Director\":0,\"Revision\":0}")!, mandates, "default mandate state");
        b.Remove("Mandates");
        foreach (var field in new[] { "MandateHistory", "MandateSelections", "MandateApproaches" })
        {
            if (b[field] is not JsonArray { Count: 0 }) throw new InvalidDataException($"Unexpected nonempty/missing {field}.");
            b.Remove(field);
        }
        Equal(a, identities.Normalize(b), $"{label}/{step}");
        checks++;
    }
}

static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
static void Equal(JsonNode a, JsonNode b, string label)
{
    if (!JsonNode.DeepEquals(a, b)) throw new InvalidDataException($"{label}: {Difference(a, b, "$")}");
}
static string Difference(JsonNode? a, JsonNode? b, string path)
{
    if (a is JsonObject oa && b is JsonObject ob)
    {
        foreach (var key in oa.Select(p => p.Key).Union(ob.Select(p => p.Key)))
            if (!oa.ContainsKey(key) || !ob.ContainsKey(key) || !JsonNode.DeepEquals(oa[key], ob[key])) return Difference(oa[key], ob[key], path + "." + key);
    }
    else if (a is JsonArray aa && b is JsonArray ab)
    {
        if (aa.Count != ab.Count) return $"{path}: array count {aa.Count} != {ab.Count}";
        for (var i = 0; i < aa.Count; i++) if (!JsonNode.DeepEquals(aa[i], ab[i])) return Difference(aa[i], ab[i], $"{path}[{i}]");
    }
    return $"{path}: {a?.ToJsonString()} != {b?.ToJsonString()}";
}

sealed class CoreApi
{
    private readonly Assembly assembly;
    public CoreApi(string path) => assembly = new AssemblyLoadContext(Guid.NewGuid().ToString()).LoadFromAssemblyPath(Path.GetFullPath(path));
    public object Call(string type, string method, params object[] args)
    {
        var target = assembly.GetType("FootballTycoon.Core." + type)!.GetMethod(method, BindingFlags.Public | BindingFlags.Static)!;
        var supplied = args.Concat(target.GetParameters().Skip(args.Length).Select(p => p.HasDefaultValue ? p.DefaultValue! : throw new InvalidOperationException("Missing required argument."))).ToArray();
        return target.Invoke(null, supplied)!;
    }
    public object Create(ulong seed) => Call("WorldFactory", "Create", seed);
    public object Decode(byte[] bytes) => Call("WorldCodec", "Decode", bytes);
    public byte[] Encode(object world) => (byte[])Call("WorldCodec", "Encode", world);
    public object Preview(object world, string allocation, bool declineAcademy = false)
    {
        var type = assembly.GetType("FootballTycoon.Core.OwnerCommand")!;
        var command = Activator.CreateInstance(type, Enum.Parse(assembly.GetType("FootballTycoon.Core.Allocation")!, allocation), 0L, false)!;
        type.GetProperty("DeclineAcademy")!.SetValue(command, declineAcademy);
        return Call("Proposals", "Preview", world, command, Get<long>(world, "Revision"));
    }
    public object Commit(object world, string id, object proposal) => Call("Proposals", "Commit", world, id, Get<long>(world, "Revision"), proposal);
    public static T Get<T>(object value, string name) => (T)value.GetType().GetProperty(name)!.GetValue(value)!;
    public static JsonObject Json(object value) => JsonSerializer.SerializeToNode(value, value.GetType())!.AsObject();
}

sealed class IdentityMap
{
    private readonly Dictionary<string, string> newToOld = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> oldToNew = new(StringComparer.Ordinal);
    public object[] Pairs => newToOld.Select(p => (object)new { New = p.Key, Old = p.Value }).ToArray();
    public void Add(string oldId, string newId)
    {
        if (newToOld.TryGetValue(newId, out var priorOld) && priorOld != oldId
            || oldToNew.TryGetValue(oldId, out var priorNew) && priorNew != newId)
            throw new InvalidDataException("Generated IDs are not bijective.");
        newToOld[newId] = oldId; oldToNew[oldId] = newId;
    }
    public JsonNode Normalize(JsonNode source, bool proposal = false)
    {
        var clone = source.DeepClone();
        Visit(clone, proposal ? "Proposal" : "Root");
        return clone;
    }
    private void Visit(JsonNode? node, string container)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToArray())
            {
                if ((key is "ForecastId" or "ProposalId" || key == "Id" && container is "Proposal" or "Forecast" or "OriginalForecast")
                    && obj[key] is JsonValue value && value.TryGetValue<string>(out var id) && newToOld.TryGetValue(id, out var oldId)) obj[key] = oldId;
                else Visit(obj[key], key);
            }
        }
        else if (node is JsonArray array) foreach (var item in array) Visit(item, container);
    }
}
