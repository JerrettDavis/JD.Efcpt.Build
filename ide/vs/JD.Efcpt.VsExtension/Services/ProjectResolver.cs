using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.Ide.Core;
using Microsoft.VisualStudio.Shell;

namespace JD.Efcpt.VsExtension.Services;

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
    /// Resolves the full path of the <c>.csproj</c> to target, or <see langword="null"/> when no
    /// project in the solution references JD.Efcpt.Build.
    /// </summary>
    public static async Task<string?> ResolveTargetProjectAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var activeProject = await VS.Solutions.GetActiveProjectAsync().ConfigureAwait(true);
        var activePath = activeProject?.FullPath;
        if (!string.IsNullOrEmpty(activePath) && File.Exists(activePath) &&
            ProjectDiscovery.HasJdEfcptPackageReference(File.ReadAllText(activePath!)))
        {
            return activePath;
        }

        var allProjects = await VS.Solutions.GetAllProjectsAsync().ConfigureAwait(true);
        var candidatePaths = allProjects
            .Select(p => p.FullPath)
            .Where(path => !string.IsNullOrEmpty(path) && File.Exists(path))
            .Select(path => path!)
            .ToList();

        var matches = ProjectDiscovery.DiscoverJdEfcptProjects(candidatePaths, File.ReadAllText);
        return matches.FirstOrDefault();
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
