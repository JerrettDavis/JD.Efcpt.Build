using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JD.Efcpt.Ide.Core;

/// <summary>
/// Severity of a <see cref="JdDiagnostic"/> parsed from MSBuild output.
/// </summary>
public enum JdDiagnosticSeverity
{
    /// <summary>
    /// A non-fatal warning (MSBuild logged it via <c>warning JDxxxx: ...</c>).
    /// </summary>
    Warning,

    /// <summary>
    /// A build-breaking error (MSBuild logged it via <c>error JDxxxx: ...</c>).
    /// </summary>
    Error
}

/// <summary>
/// A single JD.Efcpt.Build diagnostic (warning or error) parsed from a line of MSBuild output.
/// </summary>
public sealed class JdDiagnostic
{
    /// <summary>
    /// Initializes a new <see cref="JdDiagnostic"/>.
    /// </summary>
    /// <param name="severity">Whether this diagnostic was logged as a warning or an error.</param>
    /// <param name="code">The <c>JDxxxx</c> diagnostic code, e.g. <c>"JD0002"</c>.</param>
    /// <param name="message">The diagnostic message text, with any trailing MSBuild project-context suffix stripped.</param>
    public JdDiagnostic(JdDiagnosticSeverity severity, string code, string message)
    {
        Severity = severity;
        Code = code;
        Message = message;
    }

    /// <summary>
    /// Whether this diagnostic was logged as a warning or an error.
    /// </summary>
    public JdDiagnosticSeverity Severity { get; }

    /// <summary>
    /// The <c>JDxxxx</c> diagnostic code, e.g. <c>"JD0002"</c>. Documented in
    /// <c>docs/user-guide/error-codes.md</c>.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The diagnostic message text, with any trailing MSBuild project-context suffix
    /// (e.g. <c>" [C:\path\Project.csproj]"</c>) stripped.
    /// </summary>
    public string Message { get; }
}

/// <summary>
/// Parses MSBuild output lines for JD.Efcpt.Build task-level diagnostics, e.g.:
/// <c>warning JD0002: Connection string 'MyDatabase' not found in appsettings.json</c>.
/// </summary>
/// <remarks>
/// This mirrors <c>ide/vscode/src/jdDiagnostics.ts</c> so the Visual Studio extension surfaces
/// the exact same set of diagnostics as the VS Code extension. It has no dependency on the
/// Visual Studio SDK, so it can be exercised on ubuntu CI via <c>JD.Efcpt.Ide.Core.Tests</c>.
/// </remarks>
public static class JdDiagnosticParser
{
    /// <summary>
    /// Matches <c>warning JDxxxx: message</c> / <c>error JDxxxx: message</c> anywhere in a line.
    /// </summary>
    public static readonly Regex DiagnosticPattern = new(
        @"\b(warning|error)\s+(JD\d{4}):\s*(.+)",
        RegexOptions.Compiled);

    private static readonly Regex TrailingProjectSuffix = new(
        @"\s*\[[^\]]*\]\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a single line of build output. Returns <see langword="null"/> when the line does
    /// not contain a JD.Efcpt.Build diagnostic.
    /// </summary>
    /// <param name="line">A single line of MSBuild/<c>dotnet build</c> output.</param>
    /// <returns>The parsed <see cref="JdDiagnostic"/>, or <see langword="null"/> when no match.</returns>
    public static JdDiagnostic? TryParseLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return null;

        var match = DiagnosticPattern.Match(line);
        if (!match.Success)
            return null;

        var severity = string.Equals(match.Groups[1].Value, "error", System.StringComparison.OrdinalIgnoreCase)
            ? JdDiagnosticSeverity.Error
            : JdDiagnosticSeverity.Warning;
        var code = match.Groups[2].Value;
        // Strip the trailing MSBuild project-context suffix, e.g. " [C:\path\Project.csproj]",
        // that MSBuild appends to task diagnostics.
        var message = TrailingProjectSuffix.Replace(match.Groups[3].Value, string.Empty).Trim();

        return new JdDiagnostic(severity, code, message);
    }

    /// <summary>
    /// Parses multi-line build output, returning every JDxxxx diagnostic found, in order.
    /// </summary>
    /// <param name="output">The full captured <c>dotnet build</c> stdout/stderr text.</param>
    /// <returns>All JD.Efcpt.Build diagnostics found in <paramref name="output"/>, in encounter order.</returns>
    public static IReadOnlyList<JdDiagnostic> ParseLines(string output)
    {
        var results = new List<JdDiagnostic>();
        if (string.IsNullOrEmpty(output))
            return results;

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            var diagnostic = TryParseLine(trimmed);
            if (diagnostic != null)
                results.Add(diagnostic);
        }

        return results;
    }
}
