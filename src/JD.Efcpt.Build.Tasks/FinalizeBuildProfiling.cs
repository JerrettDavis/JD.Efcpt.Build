using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Profiling;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that finalizes build profiling and writes the profile to disk.
/// </summary>
/// <remarks>
/// This task should run at the end of the build pipeline to capture the complete
/// build graph and timing information.
/// </remarks>
public sealed class FinalizeBuildProfiling : Task
{
    /// <summary>
    /// Full path to the project file being built.
    /// </summary>
    [Required]
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Path where the profiling JSON file should be written.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the build succeeded.
    /// </summary>
    public bool BuildSucceeded { get; set; } = true;

    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(FinalizeBuildProfiling));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var profiler = BuildProfilerManager.TryGet(ProjectPath);
        if (profiler == null || !profiler.Enabled)
        {
            return true;
        }

        try
        {
            BuildProfilerManager.Complete(ProjectPath, OutputPath);
            ctx.Logger.LogMessage(MessageImportance.High, $"Build profile written to: {OutputPath}");
        }
        catch (System.Exception ex)
        {
            ctx.Logger.LogWarning($"Failed to write build profile: {ex.Message}");
        }

        return true;
    }
}
