using System.Text.Json;
using System.Text.Json.Serialization;

namespace FootballTycoon.Core;

// Schema 23 stores journal rows in this fixed order. Historical object rows remain readable.
// This is storage only: never infer, aggregate, reorder or drop any ledger value.
internal sealed class LedgerEntryConverter : JsonConverter<LedgerEntry>
{
    public override bool HandleNull => true;

    public override LedgerEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var legacy = JsonSerializer.Deserialize<LedgerEntry>(ref reader);
            if (legacy is null || legacy.Account is null || legacy.Reference is null)
                throw new JsonException("Missing journal entry text.");
            return legacy;
        }
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Invalid journal entry.");
        var sequence = ReadInt(ref reader);
        var week = ReadInt(ref reader);
        var account = ReadString(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var amount))
            throw new JsonException("Invalid journal amount.");
        var kind = (CashKind)ReadInt(ref reader);
        var reference = ReadString(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Journal rows must contain exactly six values.");
        return new(sequence, week, account, amount, kind, reference);
    }

    public override void Write(Utf8JsonWriter writer, LedgerEntry value, JsonSerializerOptions options)
    {
        if (value is null || value.Account is null || value.Reference is null)
            throw new JsonException("Missing journal entry text.");
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Sequence); writer.WriteNumberValue(value.Week);
        writer.WriteStringValue(value.Account); writer.WriteNumberValue(value.Amount);
        writer.WriteNumberValue((int)value.Kind); writer.WriteStringValue(value.Reference);
        writer.WriteEndArray();
    }

    private static int ReadInt(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
            throw new JsonException("Invalid journal integer.");
        return value;
    }

    private static string ReadString(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            throw new JsonException("Invalid journal text.");
        return reader.GetString()!;
    }
}
