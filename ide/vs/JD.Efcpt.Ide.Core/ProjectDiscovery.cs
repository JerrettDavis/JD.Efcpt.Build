using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace JD.Efcpt.Ide.Core;

/// <summary>
/// A <c>.csproj</c> that could not be read during discovery, together with the reason, so a
/// permissions/lock error is distinguishable from "this project does not reference JD.Efcpt.Build".
/// </summary>
public sealed class SkippedProject
{
    /// <summary>Initializes a new <see cref="SkippedProject"/>.</summary>
    /// <param name="path">The <c>.csproj</c> path that could not be read.</param>
    /// <param name="reason">A human-readable reason (typically the exception message).</param>
    public SkippedProject(string path, string reason)
    {
        Path = path;
        Reason = reason;
    }

    /// <summary>The <c>.csproj</c> path that could not be read.</summary>
    public string Path { get; }

    /// <summary>A human-readable reason the file was skipped (typically the exception message).</summary>
    public string Reason { get; }
}

/// <summary>
/// The result of a discovery pass: the matching projects, plus any candidates that could not be
/// read (so callers can distinguish "no JD.Efcpt project" from "a project could not be inspected").
/// </summary>
public sealed class ProjectDiscoveryResult
{
    /// <summary>Initializes a new <see cref="ProjectDiscoveryResult"/>.</summary>
    /// <param name="matches"><c>.csproj</c> paths referencing JD.Efcpt.Build, in input order.</param>
    /// <param name="skipped">Candidates that could not be read, with reasons.</param>
    public ProjectDiscoveryResult(IReadOnlyList<string> matches, IReadOnlyList<SkippedProject> skipped)
    {
        Matches = matches;
        Skipped = skipped;
    }

    /// <summary>The <c>.csproj</c> paths that reference JD.Efcpt.Build, in the original input order.</summary>
    public IReadOnlyList<string> Matches { get; }

    /// <summary>Candidate <c>.csproj</c> files that could not be read, with the reason for each.</summary>
    public IReadOnlyList<SkippedProject> Skipped { get; }
}

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
    /// Matches a <c>PackageReference</c> element for <c>JD.Efcpt.Build</c>, self-closing or paired,
    /// case-insensitive and tolerant of attribute ordering.
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
    /// content. Files that cannot be read (transient lock, permissions, deletion race) are
    /// recorded in <see cref="ProjectDiscoveryResult.Skipped"/> rather than silently dropped, so a
    /// read failure is not indistinguishable from "no JD.Efcpt project".
    /// </summary>
    /// <param name="csprojPaths">Candidate <c>.csproj</c> paths (e.g. every project in the open solution).</param>
    /// <param name="readFile">
    /// Reads the full text content of a file at the given path. Injected so callers can supply a
    /// Visual Studio document-aware reader (picking up unsaved edits) or a plain file-system
    /// reader.
    /// </param>
    /// <returns>The matches (in input order) plus any skipped candidates with reasons.</returns>
    public static ProjectDiscoveryResult DiscoverJdEfcptProjects(
        IReadOnlyList<string> csprojPaths,
        Func<string, string> readFile)
    {
        var matches = new List<string>();
        var skipped = new List<SkippedProject>();

        foreach (var path in csprojPaths)
        {
            string content;
            try
            {
                content = readFile(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Workspace discovery can race with file deletion/rename, and a project file may be
                // transiently locked or unreadable. Record the reason so callers can surface it
                // instead of treating an unreadable project as "not a JD.Efcpt project".
                skipped.Add(new SkippedProject(path, ex.Message));
                continue;
            }

            if (HasJdEfcptPackageReference(content))
                matches.Add(path);
        }

        return new ProjectDiscoveryResult(matches, skipped);
    }
}
