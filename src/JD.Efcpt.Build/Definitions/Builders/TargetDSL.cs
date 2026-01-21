using JD.MSBuild.Fluent.Fluent;
using JDEfcptBuild.Builders;

namespace JD.Efcpt.Build.Definitions.Builders;

/// <summary>
/// Ultra-concise DSL for target creation
/// </summary>
public static class TargetDSL
{
    /// <summary>
    /// Creates a standard EF Core generation target in one line
    /// </summary>
    public static TargetBuilder EfCoreTarget(this TargetsBuilder t, string name, string dependencies, Action<TargetBuilder> configure)
    {
        return t.AddEfcptTarget(name)
            .ForEfCoreGeneration()
            .DependsOn(dependencies.Split(';'))
            .Build()
            .Apply(configure);
    }
    
    /// <summary>
    /// Creates a target with single task
    /// </summary>
    public static void SingleTask(this TargetsBuilder t, string targetName, string dependencies, string taskName, Action<TaskInvocationBuilder> configureTask)
    {
        t.AddEfcptTarget(targetName)
            .ForEfCoreGeneration()
            .DependsOn(dependencies.Split(';'))
            .Build()
            .Task(taskName, configureTask);
    }
    
    private static TargetBuilder Apply(this TargetBuilder target, Action<TargetBuilder> action)
    {
        action(target);
        return target;
    }
}
