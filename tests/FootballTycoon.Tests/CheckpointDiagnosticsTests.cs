extern alias Endurance;
using System.Text;
using System.Text.Json;
using Xunit;
using Diagnostic = Endurance::FootballTycoon.Core;
using Probe = Endurance::FootballTycoon.Endurance;

namespace FootballTycoon.Tests;

public class CheckpointDiagnosticsTests
{
    [Fact]
    public void MeasuresUtf8ValuesAndTopLevelArrayItemsWithoutCountingNestedItems()
    {
        const string json = " { \"people\" : [ {\"name\":\"Zoë ⚽\",\"years\":[1,2]}, null, [] ], \"metadata\" : {\"v\":1}, \"empty\":[], \"flag\":true, \"nothing\":null } ";
        var bytes = Encoding.UTF8.GetBytes(json);
        var before = bytes.ToArray();
        var report = Probe.CheckpointDiagnostics.Measure(bytes);
        using var document = JsonDocument.Parse(bytes);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var section = Assert.Single(report.Sections, s => s.Name == property.Name);
            Assert.Equal(Encoding.UTF8.GetByteCount(property.Value.GetRawText()), section.ValueBytes);
            Assert.Equal(property.Value.ValueKind == JsonValueKind.Array ? (int?)property.Value.GetArrayLength() : null, section.ArrayItems);
        }
        Assert.Equal(3, report.Sections.Single(s => s.Name == "people").ArrayItems);
        Assert.Equal(0, report.Sections.Single(s => s.Name == "empty").ArrayItems);
        Assert.Equal(bytes.Length, report.TotalBytes);
        Assert.Equal(report.TotalBytes, report.EnvelopeBytes + report.Sections.Sum(s => s.ValueBytes));
        Assert.Equal(before, bytes);
    }

    [Fact]
    public void OpeningWorldReportIsDeterministicAndPreservesGameplayBytes()
    {
        var world = Diagnostic.WorldFactory.Create(2026);
        var bytes = Diagnostic.WorldCodec.Encode(world);
        var report = Probe.CheckpointDiagnostics.Measure(bytes);
        Assert.Equal(48, report.Sections.Single(s => s.Name == "Clubs").ArrayItems);
        Assert.Equal(144, report.Sections.Single(s => s.Name == "FreeAgents").ArrayItems);
        Assert.Equal(bytes, Diagnostic.WorldCodec.Encode(world));
        Assert.Equal(JsonSerializer.Serialize(report), JsonSerializer.Serialize(Probe.CheckpointDiagnostics.Measure(bytes)));
        Assert.Equal(report.Sections.OrderByDescending(s => s.ValueBytes).ThenBy(s => s.Name, StringComparer.Ordinal), report.Sections);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{\"x\":[]")]
    [InlineData("{\"x\":[1,}")]
    [InlineData("{} {}")]
    [InlineData("{\"x\":0,\"x\":1}")]
    public void InvalidInputNeverProducesAReport(string json)
    {
        var error = Record.Exception(() => Probe.CheckpointDiagnostics.Measure(Encoding.UTF8.GetBytes(json)));
        Assert.True(error is JsonException or InvalidDataException);
    }
}
