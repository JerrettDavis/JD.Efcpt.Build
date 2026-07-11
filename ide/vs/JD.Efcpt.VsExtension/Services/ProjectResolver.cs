using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.Ide.Core;
using Microsoft.VisualStudio.Shell;

namespace JD.Efcpt.VsExtension.Services;

/// <summary>
/// The outcome of resolving which project to target: the resolved <c>.csproj</c> (or
/// <see langword="null"/> when none references JD.Efcpt.Build), plus any candidate projects that
/// could not be read so callers can surface a permissions/lock problem instead of treating an
/// unreadable project as "no JD.Efcpt project".
/// </summary>
internal sealed class ProjectResolution
{
    /// <summary>Initializes a new <see cref="ProjectResolution"/>.</summary>
    public ProjectResolution(string? projectPath, IReadOnlyList<SkippedProject> skipped)
    {
        ProjectPath = projectPath;
        Skipped = skipped;
    }

    /// <summary>The resolved target <c>.csproj</c>, or <see langword="null"/> when none matched.</summary>
    public string? ProjectPath { get; }

    /// <summary>Candidate projects that could not be inspected, with the reason for each.</summary>
    public IReadOnlyList<SkippedProject> Skipped { get; }
}

/// <summary>
/// Resolves which project in the open solution the JD.Efcpt.Build commands and tool window should
/// target: the active project when it references JD.Efcpt.Build, otherwise the first matching
/// project found anywhere in the solution. Shared by
/// <see cref="Commands.RegenerateModelsCommand"/> and the build-status tool window so both agree
/// on the same project in a multi-project solution.
/// </summary>
internal static class ProjectResolver
{
    /// <summary>
    /// Resolves the target project. The active project is preferred when it references
    /// JD.Efcpt.Build, otherwise the first matching project anywhere in the solution is used.
    /// </summary>
    public static async Task<ProjectResolution> ResolveTargetProjectAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var skipped = new List<SkippedProject>();

        // Fast path: the active project, guarded the same way the fallback scan is - a transient
        // lock or permissions error must not throw unhandled, only exclude it from the fast path.
        var activeProject = await VS.Solutions.GetActiveProjectAsync().ConfigureAwait(true);
        var activePath = activeProject?.FullPath;
        if (!string.IsNullOrEmpty(activePath) && File.Exists(activePath))
        {
            var activeResult = ProjectDiscovery.DiscoverJdEfcptProjects(
                new[] { activePath! },
                File.ReadAllText);

            if (activeResult.Matches.Count > 0)
                return new ProjectResolution(activeResult.Matches[0], activeResult.Skipped);

            skipped.AddRange(activeResult.Skipped);
        }

        var allProjects = await VS.Solutions.GetAllProjectsAsync().ConfigureAwait(true);
        var candidatePaths = allProjects
            .Select(p => p.FullPath)
            .Where(path => !string.IsNullOrEmpty(path) && File.Exists(path))
            .Select(path => path!)
            .ToList();

        var result = ProjectDiscovery.DiscoverJdEfcptProjects(candidatePaths, File.ReadAllText);
        skipped.AddRange(result.Skipped);

        return new ProjectResolution(result.Matches.FirstOrDefault(), skipped);
    }

    /// <summary>
    /// Computes the expected <c>obj/efcpt/build-profile.json</c> path for a given project.
    /// </summary>
    /// <param name="projectPath">Full path to the project's <c>.csproj</c>.</param>
    public static string GetBuildProfilePath(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? string.Empty;
        return Path.Combine(projectDirectory, "obj", "efcpt", "build-profile.json");
    }
}
