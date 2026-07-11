using System.Text.RegularExpressions;

namespace JD.Efcpt.Build.Core.Diagnostics;

/// <summary>
/// Utilities for masking secrets (connection strings, passwords) before they are written to
/// build logs or diagnostic output.
/// </summary>
/// <remarks>
/// These helpers only affect logged / displayed text. They never alter the values passed to an
/// executed process, so redaction cannot change build behavior - it only prevents credentials
/// from leaking into build logs (which are frequently captured by CI, IDE extensions, and shared
/// in bug reports).
/// </remarks>
public static class SecretRedaction
{
    /// <summary>
    /// Placeholder substituted for a fully-redacted connection-string value.
    /// </summary>
    public const string ConnectionStringPlaceholder = "<connection-string redacted>";

    private const string Mask = "***";

    // Matches ADO.NET / ODBC / cloud-provider style sensitive key-value pairs. The key
    // (including '=') is captured so only the value is masked, leaving non-sensitive keys
    // (Server, Database, ...) visible. The value match is quote-aware: a double- or
    // single-quoted value is matched in full (so an embedded ';' inside quotes does not end the
    // value early), otherwise the value runs to the next ';'. Matching quoted values fully is
    // essential - a naive "[^;\"]*" would match ZERO characters against Password="p@ss;word",
    // leaving the secret intact after a fake mask.
    private static readonly Regex SensitiveKeyValuePattern = new(
        "(?<key>\\b(?:password|pwd|user\\s*id|uid|access\\s*token|accountkey|shared\\s*access\\s*signature|sas\\s*token)\\s*=)" +
        "(?<val>\"[^\"]*\"|'[^']*'|[^;]*)",
        RegexOptions.IgnoreCase
#if !NETFRAMEWORK
        | RegexOptions.Compiled
#endif
    );

    /// <summary>
    /// Masks sensitive key-value pairs (password / pwd / user id / uid) within an arbitrary
    /// string - e.g. a command line that embeds an ADO.NET connection string.
    /// </summary>
    /// <param name="text">The text to mask (may be <see langword="null"/>).</param>
    /// <returns>The text with sensitive values replaced by <c>***</c>.</returns>
    public static string MaskSecrets(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return SensitiveKeyValuePattern.Replace(text!, m => m.Groups["key"].Value + Mask);
    }

    /// <summary>
    /// Redacts a known connection-string value from <paramref name="text"/> by replacing every
    /// occurrence with <see cref="ConnectionStringPlaceholder"/>, then masks any residual
    /// sensitive key-value pairs as defense in depth.
    /// </summary>
    /// <param name="text">The text (e.g. a command line) to redact.</param>
    /// <param name="connectionString">
    /// The exact connection-string value to remove, or <see langword="null"/>/empty when the
    /// invocation is not in connection-string mode.
    /// </param>
    /// <returns>The redacted text, safe to write to a build log.</returns>
    public static string RedactConnectionString(string? text, string? connectionString)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        var result = text!;
        if (!string.IsNullOrWhiteSpace(connectionString))
            result = result.Replace(connectionString, ConnectionStringPlaceholder);

        return MaskSecrets(result);
    }
}
