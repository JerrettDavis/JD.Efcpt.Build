using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JD.Efcpt.Ide.Core;

/// <summary>
/// Discovers <c>.csproj</c> files that reference the <c>JD.Efcpt.Build</c> NuGet package, so the
/// Visual Studio extension can resolve which project(s) in a solution to target for
/// <c>Regenerate Models</c>.
/// </summary>
/// <remarks>
/// Pure, file-system-agnostic logic only - mirrors <c>ide/vscode/src/projectDiscovery.ts</c>.
/// Actual file enumeration and reads are the caller's responsibility (in the VSIX, via
/// <c>Community.VisualStudio.Toolkit</c> solution/project APIs), which keeps this class
/// unit-testable on ubuntu CI without any Visual Studio SDK dependency.
/// </remarks>
public static class ProjectDiscovery
{
    /// <summary>
    /// Matches a <c>PackageReference</c> (or <c>PackageVersion</c>) element for
    /// <c>JD.Efcpt.Build</c>, self-closing or paired, case-insensitive and tolerant of attribute
    /// ordering.
    /// </summary>
    public static readonly Regex PackageReferencePattern = new(
        "<PackageReference\\s+[^>]*Include\\s*=\\s*\"JD\\.Efcpt\\.Build\"[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// True when the given <c>.csproj</c> file content contains a <c>PackageReference</c> to
    /// <c>JD.Efcpt.Build</c>.
    /// </summary>
    /// <param name="csprojContent">The full text content of a <c>.csproj</c> file.</param>
    /// <returns><see langword="true"/> when a matching <c>PackageReference</c> element is present.</returns>
    public static bool HasJdEfcptPackageReference(string csprojContent)
    {
        if (string.IsNullOrEmpty(csprojContent))
            return false;

        return PackageReferencePattern.IsMatch(csprojContent);
    }

    /// <summary>
    /// Filters a list of <c>.csproj</c> file paths down to those referencing
    /// <c>JD.Efcpt.Build</c>, using the supplied <paramref name="readFile"/> function to load
    /// content.
    /// </summary>
    /// <param name="csprojPaths">Candidate <c>.csproj</c> paths (e.g. every project in the open solution).</param>
    /// <param name="readFile">
    /// Reads the full text content of a file at the given path. Injected so callers can supply a
    /// Visual Studio document-aware reader (picking up unsaved edits) or a plain file-system
    /// reader.
    /// </param>
    /// <returns>The subset of <paramref name="csprojPaths"/> that reference <c>JD.Efcpt.Build</c>, in the original order.</returns>
    public static IReadOnlyList<string> DiscoverJdEfcptProjects(
        IReadOnlyList<string> csprojPaths,
        System.Func<string, string> readFile)
    {
        var matches = new List<string>();
        foreach (var path in csprojPaths)
        {
            string content;
            try
            {
                content = readFile(path);
            }
            catch
            {
                // Workspace discovery can race with file deletion/rename; skip unreadable files
                // rather than failing the whole discovery pass.
                continue;
            }

            if (HasJdEfcptPackageReference(content))
                matches.Add(path);
        }

        return matches;
    }
}
