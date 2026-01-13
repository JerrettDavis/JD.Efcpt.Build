using JD.Efcpt.Build.Tasks.Profiling;
using Microsoft.Build.Utilities;
using PatternKit.Structural.Decorator;

namespace JD.Efcpt.Build.Tasks.Decorators;

/// <summary>
/// Context for MSBuild task execution containing logging infrastructure and task identification.
/// </summary>
public readonly record struct TaskExecutionContext(
    TaskLoggingHelper Logger,
    string TaskName,
    BuildProfiler? Profiler = null
);

/// <summary>
/// Decorator that wraps MSBuild task execution logic with cross-cutting concerns.
/// </summary>
/// <remarks>
/// <para>This decorator provides consistent behavior across all tasks:</para>
/// <list type="bullet">
/// <item><strong>Exception Handling:</strong> Catches all exceptions from core logic, logs with full stack traces</item>
/// <item><strong>Profiling (Optional):</strong> Automatically captures timing, inputs, and outputs when profiler is present</item>
/// </list>
///
/// <para><strong>Usage - Basic (No Profiling):</strong></para>
/// <code>
/// public override bool Execute()
/// {
///     var decorator = TaskExecutionDecorator.Create(ExecuteCore);
///     var ctx = new TaskExecutionContext(Log, nameof(MyTask));
///     return decorator.Execute(in ctx);
/// }
/// </code>
///
/// <para><strong>Usage - With Automatic Profiling:</strong></para>
/// <code>
/// public override bool Execute()
/// {
///     return TaskExecutionDecorator.ExecuteWithProfiling(
///         this,
///         ExecuteCore,
///         ProfilingHelper.GetProfiler(ProjectPath));
/// }
/// </code>
/// </remarks>
internal static class TaskExecutionDecorator
{
    // NOTE: Assembly resolver initialization has been moved to ModuleInitializer.cs
    // which runs before any code in this assembly, solving the chicken-and-egg problem
    // where PatternKit types need to be loaded before this static constructor can run.

    /// <summary>
    /// Creates a decorator that wraps the given core logic with exception handling only.
    /// </summary>
    /// <param name="coreLogic">The task's core execution logic.</param>
    /// <returns>A decorator that handles exceptions and logging.</returns>
    public static Decorator<TaskExecutionContext, bool> Create(
        Func<TaskExecutionContext, bool> coreLogic)
        => Decorator<TaskExecutionContext, bool>
            .Create(a => coreLogic(a))
            .Around((ctx, next) =>
            {
                try
                {
                    return next(ctx);
                }
                catch (Exception ex)
                {
                    ctx.Logger.LogErrorFromException(ex, showStackTrace: true);
                    return false;
                }
            })
            .Build();

    /// <summary>
    /// Executes a task with automatic profiling and exception handling.
    /// </summary>
    /// <typeparam name="T">The task type.</typeparam>
    /// <param name="task">The task instance.</param>
    /// <param name="coreLogic">The task's core execution logic.</param>
    /// <param name="profiler">Optional profiler instance (null if profiling disabled).</param>
    /// <returns>True if the task succeeded, false otherwise.</returns>
    /// <remarks>
    /// This method provides a fully bolt-on profiling experience:
    /// <list type="bullet">
    /// <item>Automatically captures inputs from [Required] and [ProfileInput] properties</item>
    /// <item>Automatically captures outputs from [Output] and [ProfileOutput] properties</item>
    /// <item>Wraps execution with BeginTask/EndTask lifecycle</item>
    /// <item>Zero overhead when profiler is null</item>
    /// </list>
    /// </remarks>
    public static bool ExecuteWithProfiling<T>(
        T task,
        Func<TaskExecutionContext, bool> coreLogic,
        BuildProfiler? profiler) where T : Microsoft.Build.Utilities.Task
    {
        var ctx = new TaskExecutionContext(
            task.Log,
            task.GetType().Name,
            profiler);

        var decorator = Create(innerCtx =>
            ProfilingBehavior.ExecuteWithProfiling(task, coreLogic, innerCtx));

        return decorator.Execute(in ctx);
    }
}