using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Profiling;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that initializes build profiling for the current project.
/// </summary>
/// <remarks>
/// This task should run early in the build pipeline to ensure all subsequent tasks
/// can access the profiler instance for capturing telemetry.
/// </remarks>
public sealed class InitializeBuildProfiling : Task
{
    /// <summary>
    /// Whether profiling is enabled for this build.
    /// </summary>
    [Required]
    public string EnableProfiling { get; set; } = "false";

    /// <summary>
    /// Full path to the project file being built.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Name of the project.
    /// </summary>
    [Required]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Target framework (e.g., "net8.0").
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Build configuration (e.g., "Debug", "Release").
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Path to the efcpt configuration JSON file.
    /// </summary>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Path to the efcpt renaming JSON file.
    /// </summary>
    public string? RenamingPath { get; set; }

    /// <summary>
    /// Path to the template directory.
    /// </summary>
    public string? TemplateDir { get; set; }

    /// <summary>
    /// Path to the SQL project (if used).
    /// </summary>
    public string? SqlProjectPath { get; set; }

    /// <summary>
    /// Path to the DACPAC file (if used).
    /// </summary>
    public string? DacpacPath { get; set; }

    /// <summary>
    /// Database provider (e.g., "mssql", "postgresql").
    /// </summary>
    public string? Provider { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(InitializeBuildProfiling));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var enabled = EnableProfiling.Equals("true", System.StringComparison.OrdinalIgnoreCase);

        if (!enabled)
        {
            // Create a disabled profiler so downstream tasks don't fail
            BuildProfilerManager.GetOrCreate(ProjectPath, false, ProjectName);
            return true;
        }

        var profiler = BuildProfilerManager.GetOrCreate(
            ProjectPath,
            enabled: true,
            ProjectName,
            TargetFramework,
            Configuration);

        // Set build configuration
        profiler.SetConfiguration(new BuildConfiguration
        {
            ConfigPath = ConfigPath,
            RenamingPath = RenamingPath,
            TemplateDir = TemplateDir,
            SqlProjectPath = SqlProjectPath,
            DacpacPath = DacpacPath,
            Provider = Provider
        });

        ctx.Logger.LogMessage(MessageImportance.High, $"Build profiling enabled for {ProjectName}");

        return true;
    }
}
