using System.Text.Json;

namespace FootballTycoon.Endurance;

public sealed record CheckpointSection(string Name, long ValueBytes, int? ArrayItems);
public sealed record CheckpointCapacityReport(long TotalBytes, long EnvelopeBytes, CheckpointSection[] Sections);

public static class CheckpointDiagnostics
{
    // Scan the already encoded checkpoint without allocating another world or JSON document.
    // Value bytes include nested content; envelope bytes contain property names and root punctuation/whitespace.
    public static CheckpointCapacityReport Measure(ReadOnlySpan<byte> checkpoint)
    {
        var reader = new Utf8JsonReader(checkpoint);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            throw new InvalidDataException("Checkpoint capacity requires a JSON object.");
        var sections = new List<CheckpointSection>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new InvalidDataException("Expected a checkpoint property.");
            var name = reader.GetString()!;
            if (!names.Add(name)) throw new InvalidDataException("Duplicate checkpoint property.");
            if (!reader.Read()) throw new InvalidDataException("Missing checkpoint value.");
            var start = reader.TokenStartIndex;
            int? count = null;
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                count = 0;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    count++;
                    reader.Skip();
                }
            }
            else reader.Skip();
            sections.Add(new(name, reader.BytesConsumed - start, count));
        }
        if (reader.TokenType != JsonTokenType.EndObject || reader.Read())
            throw new InvalidDataException("Checkpoint must contain exactly one complete object.");
        return new(checkpoint.Length, checkpoint.Length - sections.Sum(s => s.ValueBytes),
            sections.OrderByDescending(s => s.ValueBytes).ThenBy(s => s.Name, StringComparer.Ordinal).ToArray());
    }
}
