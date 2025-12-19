namespace JD.Efcpt.Build.Tasks.Extensions;

/// <summary>
/// Extension methods for working with enumerable collections in a functional style.
/// </summary>
internal static class EnumerableExtensions
{
    /// <summary>
    /// Builds a deduplicated list of candidate file or directory names from an override and fallback names.
    /// </summary>
    /// <param name="candidateOverride">Optional override name to prioritize (can be partial path).</param>
    /// <param name="fallbackNames">Default names to use if override is not provided.</param>
    /// <returns>
    /// A case-insensitive deduplicated list with the override's filename first (if provided),
    /// followed by valid fallback names.
    /// </returns>
    /// <remarks>
    /// This method extracts just the filename portion of paths and performs case-insensitive
    /// deduplication, making it suitable for multi-platform file/directory resolution scenarios.
    /// </remarks>
    public static IReadOnlyList<string> BuildCandidateNames(
        string? candidateOverride,
        params string[] fallbackNames)
    {
        var names = new List<string>();

        if (PathUtils.HasValue(candidateOverride))
            names.Add(Path.GetFileName(candidateOverride)!);

        var validFallbacks = fallbackNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(Path.GetFileName)
            .Where(n => n != null)
            .Cast<string>();

        names.AddRange(validFallbacks);

        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
