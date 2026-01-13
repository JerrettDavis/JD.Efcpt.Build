using System;
using System.Collections.Concurrent;

namespace JD.Efcpt.Build.Tasks.Profiling;

/// <summary>
/// Thread-safe manager for build profilers, allowing tasks to share a profiler instance across the build.
/// </summary>
/// <remarks>
/// MSBuild tasks are instantiated per-target and don't share state naturally. This manager
/// provides a static registry that tasks can use to coordinate profiling across the build pipeline.
/// </remarks>
public static class BuildProfilerManager
{
    private static readonly ConcurrentDictionary<string, BuildProfiler> _profilers = new();

    /// <summary>
    /// Gets or creates a profiler for the specified project.
    /// </summary>
    /// <param name="projectPath">Full path to the project being built.</param>
    /// <param name="enabled">Whether profiling is enabled.</param>
    /// <param name="projectName">Name of the project.</param>
    /// <param name="targetFramework">Target framework.</param>
    /// <param name="configuration">Build configuration.</param>
    /// <returns>A build profiler instance.</returns>
    public static BuildProfiler GetOrCreate(
        string projectPath,
        bool enabled,
        string projectName,
        string? targetFramework = null,
        string? configuration = null)
    {
        return _profilers.GetOrAdd(
            projectPath,
            _ => new BuildProfiler(enabled, projectPath, projectName, targetFramework, configuration));
    }

    /// <summary>
    /// Gets an existing profiler for the specified project, or null if none exists.
    /// </summary>
    /// <param name="projectPath">Full path to the project.</param>
    /// <returns>The profiler instance, or null if not found.</returns>
    public static BuildProfiler? TryGet(string projectPath)
    {
        return _profilers.TryGetValue(projectPath, out var profiler) ? profiler : null;
    }

    /// <summary>
    /// Completes and removes the profiler for the specified project.
    /// </summary>
    /// <param name="projectPath">Full path to the project.</param>
    /// <param name="outputPath">Path where the profile should be written.</param>
    public static void Complete(string projectPath, string outputPath)
    {
        if (_profilers.TryRemove(projectPath, out var profiler))
        {
            profiler.Complete(outputPath);
        }
    }

    /// <summary>
    /// Clears all profilers (primarily for testing).
    /// </summary>
    internal static void Clear()
    {
        _profilers.Clear();
    }
}
