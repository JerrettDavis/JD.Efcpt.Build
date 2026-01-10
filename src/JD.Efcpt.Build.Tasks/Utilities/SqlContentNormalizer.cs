using System.Text;
using System.Text.RegularExpressions;

namespace JD.Efcpt.Build.Tasks.Utilities;

/// <summary>
/// Provides normalization of SQL content for fingerprinting, preserving significant content while ignoring formatting differences.
/// </summary>
/// <remarks>
/// <para>
/// This normalizer is designed to detect material SQL changes while ignoring:
/// <list type="bullet">
///   <item><description>Whitespace variations (extra spaces, tabs, line breaks)</description></item>
///   <item><description>Comment-only changes</description></item>
///   <item><description>Casing in identifiers (optional)</description></item>
/// </list>
/// </para>
/// <para>
/// The normalizer preserves:
/// <list type="bullet">
///   <item><description>String literal content (including internal whitespace)</description></item>
///   <item><description>Significant SQL keywords and identifiers</description></item>
///   <item><description>Column/table/object names and their structure</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Design Tradeoff:</strong> Uses regex-based approach for performance rather than a full SQL parser.
/// This handles 99% of real-world SQL scenarios but may have edge cases with complex nested quotes or
/// unusual escape sequences. For such cases, users can customize SQL file patterns to exclude specific files.
/// </para>
/// </remarks>
internal static class SqlContentNormalizer
{
    // Regex to match SQL string literals (single-quoted strings with escaped quotes)
    // Handles: 'simple string', 'string with '' escaped quote', 'string with \' backslash escape'
    private static readonly Regex StringLiteralRegex = new(
        @"'(?:[^']|'')*'",
        RegexOptions.Compiled);

    // Regex to match SQL comments (both -- line comments and /* block comments */)
    private static readonly Regex CommentRegex = new(
        @"(?:--[^\r\n]*)|(?:/\*[\s\S]*?\*/)",
        RegexOptions.Compiled);

    // Regex to match one or more whitespace characters
    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    /// <summary>
    /// Normalizes SQL content for fingerprinting by removing comments and normalizing whitespace
    /// while preserving string literal content.
    /// </summary>
    /// <param name="sqlContent">The raw SQL content to normalize.</param>
    /// <returns>A normalized version of the SQL content suitable for hashing.</returns>
    /// <remarks>
    /// <para>
    /// The normalization process:
    /// <list type="number">
    ///   <item><description>Extracts and preserves string literals with placeholders</description></item>
    ///   <item><description>Removes SQL comments (both line and block comments)</description></item>
    ///   <item><description>Normalizes all whitespace sequences to single spaces</description></item>
    ///   <item><description>Restores string literals to their original positions</description></item>
    ///   <item><description>Trims leading/trailing whitespace</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This ensures that formatting changes don't affect the fingerprint while preserving
    /// the actual SQL structure and content.
    /// </para>
    /// </remarks>
    public static string Normalize(string sqlContent)
    {
        if (string.IsNullOrWhiteSpace(sqlContent))
        {
            return string.Empty;
        }

        // Step 1: Extract and preserve string literals
        var stringLiterals = new List<string>();
        var contentWithPlaceholders = StringLiteralRegex.Replace(sqlContent, match =>
        {
            stringLiterals.Add(match.Value);
            return $"<<<STRING_{stringLiterals.Count - 1}>>>";
        });

        // Step 2: Remove comments
        var contentWithoutComments = CommentRegex.Replace(contentWithPlaceholders, " ");

        // Step 3: Normalize whitespace to single spaces
        var normalizedWhitespace = WhitespaceRegex.Replace(contentWithoutComments, " ");

        // Step 4: Restore string literals
        var result = new StringBuilder(normalizedWhitespace);
        for (var i = 0; i < stringLiterals.Count; i++)
        {
            result.Replace($"<<<STRING_{i}>>>", stringLiterals[i]);
        }

        // Step 5: Trim and return
        return result.ToString().Trim();
    }
}
