using JD.Efcpt.Build.Tasks.Extensions;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Helper methods for working with MSBuild property values.
/// </summary>
internal static class MsBuildPropertyHelpers
{
    /// <summary>
    /// Returns null if the value is empty or whitespace, otherwise returns the trimmed value.
    /// </summary>
    public static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// Parses a string to a nullable boolean, returning null if empty.
    /// </summary>
    public static bool? ParseBoolOrNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.IsTrue();

    /// <summary>
    /// Returns true if any of the string values is not null.
    /// </summary>
    public static bool HasAnyValue(params string?[] values) =>
        values.Any(v => v is not null);

    /// <summary>
    /// Returns true if any of the nullable boolean values has a value.
    /// </summary>
    public static bool HasAnyValue(params bool?[] values) =>
        values.Any(v => v.HasValue);

    /// <summary>
    /// Adds a key-value pair to the dictionary if the value is not empty.
    /// </summary>
    public static void AddIfNotEmpty(Dictionary<string, string> dict, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dict[key] = value;
        }
    }
}
