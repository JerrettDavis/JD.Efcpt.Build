using JD.MSBuild.Fluent.Fluent;

namespace JDEfcptBuild.Builders;

/// <summary>
/// Extension methods for fluent syntax with Efcpt builders.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Creates a new EfcptTargetBuilder for fluent target construction.
    /// </summary>
    /// <param name="targetsBuilder">The targets builder.</param>
    /// <param name="targetName">Name of the target to create.</param>
    /// <returns>An EfcptTargetBuilder for fluent configuration.</returns>
    /// <example>
    /// <code>
    /// t.AddEfcptTarget(EfcptTargets.MyTarget)
    ///     .ForEfCoreGeneration()
    ///     .DependsOn(EfcptTargets.EfcptStageInputs)
    ///     .LogInfo("Starting generation...")
    ///     .Build()
    ///     .Task(EfcptTasks.MyTask, task => { ... });
    /// </code>
    /// </example>
    public static EfcptTargetBuilder AddEfcptTarget(this TargetsBuilder targetsBuilder, string targetName)
    {
        TargetBuilder? targetBuilder = null;
        targetsBuilder.Target(targetName, t => targetBuilder = t);
        return new EfcptTargetBuilder(targetBuilder!);
    }

    /// <summary>
    /// Creates a TaskParameterMapper for fluent parameter mapping.
    /// </summary>
    /// <param name="taskBuilder">The task builder.</param>
    /// <returns>A TaskParameterMapper for fluent parameter configuration.</returns>
    /// <example>
    /// <code>
    /// target.Task(EfcptTasks.ApplyConfigOverrides, task =>
    ///     task.MapParameters()
    ///         .WithProjectContext()
    ///         .WithInputFiles()
    ///         .WithAllConfigOverrides()
    ///         .Build());
    /// </code>
    /// </example>
    public static TaskParameterMapper MapParameters(this TaskInvocationBuilder taskBuilder)
    {
        return new TaskParameterMapper(taskBuilder);
    }
}
