using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JD.Efcpt.Build.Tasks.Profiling;

/// <summary>
/// Custom JSON converter for TimeSpan that serializes to ISO 8601 duration format.
/// </summary>
/// <remarks>
/// Formats TimeSpan as ISO 8601 duration (e.g., "PT1M30.5S" for 1 minute, 30.5 seconds).
/// This format is deterministic and widely supported for machine-readable durations.
/// </remarks>
public sealed class JsonTimeSpanConverter : JsonConverter<TimeSpan>
{
    private const string Iso8601DurationPrefix = "PT";

    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return TimeSpan.Zero;

        // Support both ISO 8601 duration format and simple numeric seconds
        if (value.StartsWith(Iso8601DurationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return System.Xml.XmlConvert.ToTimeSpan(value);
        }

        // Fall back to parsing as total seconds
        if (double.TryParse(value, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        throw new JsonException($"Unable to parse TimeSpan from value: {value}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Write as ISO 8601 duration (e.g., "PT1H30M15.5S")
        var duration = System.Xml.XmlConvert.ToString(value);
        writer.WriteStringValue(duration);
    }
}
