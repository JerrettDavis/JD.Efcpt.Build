using Microsoft.Build.Utilities;
using PatternKit.Structural.Decorator;

namespace JD.Efcpt.Build.Tasks.Decorators;

/// <summary>
/// Context for MSBuild task execution containing logging infrastructure and task identification.
/// </summary>
public readonly record struct TaskExecutionContext(
    TaskLoggingHelper Logger,
    string TaskName
);

/// <summary>
/// Decorator that wraps MSBuild task execution logic with exception handling.
/// </summary>
/// <remarks>
/// This decorator provides consistent error handling across all tasks:
/// <list type="bullet">
/// <item>Catches all exceptions from core logic</item>
/// <item>Logs exceptions with full stack traces to MSBuild</item>
/// <item>Returns false to indicate task failure</item>
/// <item>Preserves successful results from core logic</item>
/// </list>
/// </remarks>
internal static class TaskExecutionDecorator
{
    /// <summary>
    /// Static constructor ensures assembly resolver is initialized before any task runs.
    /// This is critical for loading dependencies from the task assembly's directory.
    /// </summary>
    static TaskExecutionDecorator()
    {
        TaskAssemblyResolver.Initialize();
    }
    /// <summary>
    /// Creates a decorator that wraps the given core logic with exception handling.
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
}