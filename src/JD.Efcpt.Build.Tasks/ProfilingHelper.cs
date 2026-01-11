using JD.Efcpt.Build.Tasks.Profiling;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Helper methods for working with build profiling in MSBuild tasks.
/// </summary>
internal static class ProfilingHelper
{
    /// <summary>
    /// Gets the build profiler for a project, if profiling is enabled.
    /// </summary>
    /// <param name="projectPath">Full path to the project file.</param>
    /// <returns>The profiler instance, or null if profiling is not enabled.</returns>
    public static BuildProfiler? GetProfiler(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return null;

        return BuildProfilerManager.TryGet(projectPath);
    }
}
