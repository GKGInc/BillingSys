using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BillingSys.Functions.Services;

namespace BillingSys.Functions.Infrastructure;

/// <summary>
/// Ensures all deserialized <see cref="DateTime"/> values have <see cref="DateTimeKind.Utc"/> so Azure SDK
/// and Table Storage do not reject <see cref="DateTimeKind.Unspecified"/> (e.g. ISO strings without "Z").
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            throw new JsonException("Cannot read null as non-nullable DateTime.");

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected token parsing DateTime: {reader.TokenType}.");

        var s = reader.GetString();
        if (string.IsNullOrEmpty(s))
            return default;

        if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            throw new JsonException($"Unable to parse DateTime from \"{s}\".");

        return DateTimeUtc.EnsureUtc(dt);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = DateTimeUtc.EnsureUtc(value);
        writer.WriteStringValue(utc);
    }
}

/// <summary>
/// Same as <see cref="UtcDateTimeConverter"/> for nullable date/time fields.
/// </summary>
public sealed class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected token parsing DateTime?: {reader.TokenType}.");

        var s = reader.GetString();
        if (string.IsNullOrEmpty(s))
            return null;

        if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            throw new JsonException($"Unable to parse DateTime? from \"{s}\".");

        return DateTimeUtc.EnsureUtc(dt);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var utc = DateTimeUtc.EnsureUtc(value.Value);
        writer.WriteStringValue(utc);
    }
}
