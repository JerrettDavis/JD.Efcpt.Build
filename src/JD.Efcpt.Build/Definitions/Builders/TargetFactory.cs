using System;
using JD.MSBuild.Fluent.Fluent;
using JDEfcptBuild.Constants;

namespace JDEfcptBuild.Builders;

/// <summary>
/// Factory for creating common target patterns in the Efcpt build pipeline.
/// </summary>
public static class TargetFactory
{
    /// <summary>
    /// Creates a standard pipeline target with a task and parameter mapper.
    /// This is the most common pattern: a target that depends on other targets, has a condition,
    /// and executes a single task with mapped parameters.
    /// </summary>
    /// <param name="targetsBuilder">The targets builder.</param>
    /// <param name="targetName">Name of the target.</param>
    /// <param name="condition">Condition expression for when the target should run.</param>
    /// <param name="dependencies">Target dependencies (DependsOnTargets).</param>
    /// <param name="taskName">Name of the task to execute.</param>
    /// <param name="configureParams">Action to configure task parameters using the mapper.</param>
    public static void CreatePipelineTarget(
        TargetsBuilder targetsBuilder,
        string targetName,
        string condition,
        string[] dependencies,
        string taskName,
        Action<TaskParameterMapper> configureParams)
    {
        targetsBuilder.Target(targetName, target =>
        {
            if (dependencies.Length > 0)
            {
                target.DependsOnTargets(string.Join(";", dependencies));
            }
            target.Condition(condition);
            target.Task(taskName, task =>
            {
                var mapper = new TaskParameterMapper(task);
                configureParams(mapper);
            });
        });
    }

    /// <summary>
    /// Creates an empty lifecycle hook target for extensibility.
    /// These targets allow users to inject custom behavior before/after key operations.
    /// </summary>
    /// <param name="targetsBuilder">The targets builder.</param>
    /// <param name="targetName">Name of the hook target.</param>
    /// <param name="condition">Optional condition for when the hook should be available.</param>
    public static void CreateLifecycleHook(
        TargetsBuilder targetsBuilder,
        string targetName,
        string? condition = null)
    {
        targetsBuilder.Target(targetName, target =>
        {
            if (condition != null)
            {
                target.Condition(condition);
            }
            // Empty target - extensibility point
        });
    }

    /// <summary>
    /// Creates a target that conditionally sets a property.
    /// Common pattern for late-evaluated property overrides in targets files.
    /// </summary>
    /// <param name="targetsBuilder">The targets builder.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <param name="value">Value to assign to the property.</param>
    /// <param name="condition">Condition under which to set the property.</param>
    public static void CreateConditionalPropertySetter(
        TargetsBuilder targetsBuilder,
        string propertyName,
        string value,
        string condition)
    {
        targetsBuilder.PropertyGroup(null, group =>
        {
            group.Property(propertyName, value, condition);
        });
    }
}
