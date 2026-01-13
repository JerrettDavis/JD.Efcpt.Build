using System.Reflection;
using JD.Efcpt.Build.Tasks.Profiling;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MsBuildTask = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks.Decorators;

/// <summary>
/// Attribute to mark properties that should be captured as profiling inputs.
/// </summary>
/// <remarks>
/// By default, all properties with [Required] or [Output] attributes are automatically captured.
/// Use this attribute to:
/// <list type="bullet">
/// <item>Include additional properties not marked with MSBuild attributes</item>
/// <item>Exclude properties from automatic capture using Exclude=true</item>
/// <item>Provide a custom name for the profiling metadata</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ProfileInputAttribute : Attribute
{
    /// <summary>
    /// Whether to exclude this property from profiling.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    /// Custom name to use in profiling metadata. If null, uses property name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Attribute to mark properties that should be captured as profiling outputs.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ProfileOutputAttribute : Attribute
{
    /// <summary>
    /// Whether to exclude this property from profiling.
    /// </summary>
    public bool Exclude { get; set; }

    /// <summary>
    /// Custom name to use in profiling metadata. If null, uses property name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Provides automatic profiling behavior for MSBuild tasks.
/// </summary>
/// <remarks>
/// This behavior automatically:
/// <list type="bullet">
/// <item>Captures task execution timing</item>
/// <item>Records input properties (all [Required] properties by default)</item>
/// <item>Records output properties (all [Output] properties by default)</item>
/// <item>Handles profiler lifecycle (BeginTask/EndTask)</item>
/// </list>
///
/// <para><strong>Automatic Mode (Zero Code):</strong></para>
/// <code>
/// // Just use the base class - profiling is automatic
/// public class MyTask : MsBuildTask
/// {
///     [Required]
///     public string Input { get; set; }
///
///     [Output]
///     public string Output { get; set; }
///
///     public override bool Execute()
///     {
///         var decorator = TaskExecutionDecorator.Create(ExecuteCore);
///         var ctx = new TaskExecutionContext(Log, nameof(MyTask));
///         return decorator.Execute(in ctx);
///     }
///
///     private bool ExecuteCore(TaskExecutionContext ctx)
///     {
///         // Your logic here - profiling is automatic
///         return true;
///     }
/// }
/// </code>
///
/// <para><strong>Enhanced Mode (Custom Metadata):</strong></para>
/// <code>
/// public class MyTask : Task
/// {
///     [Required]
///     public string Input { get; set; }
///
///     [ProfileInput] // Include even without [Required]
///     public string OptionalInput { get; set; }
///
///     [ProfileInput(Exclude = true)] // Exclude sensitive data
///     public string Password { get; set; }
///
///     [Output]
///     [ProfileOutput(Name = "ResultPath")] // Custom name
///     public string Output { get; set; }
/// }
/// </code>
/// </remarks>
public static class ProfilingBehavior
{
    /// <summary>
    /// Adds profiling behavior to the decorator chain.
    /// </summary>
    /// <param name="task">The task instance to profile.</param>
    /// <param name="coreLogic">The task's core execution logic.</param>
    /// <param name="ctx">The execution context.</param>
    /// <returns>A decorator that includes automatic profiling.</returns>
    public static bool ExecuteWithProfiling<T>(
        T task,
        Func<TaskExecutionContext, bool> coreLogic,
        TaskExecutionContext ctx) where T : MsBuildTask
    {
        // If no profiler, just execute
        if (ctx.Profiler == null)
        {
            return coreLogic(ctx);
        }

        var taskType = task.GetType();
        var taskName = taskType.Name;

        // Capture inputs automatically
        var inputs = CaptureInputs(task, taskType);

        // Begin profiling
        using var tracker = ctx.Profiler.BeginTask(
            taskName,
            initiator: GetInitiator(task),
            inputs: inputs);

        // Execute core logic
        var success = coreLogic(ctx);

        // Capture outputs automatically
        var outputs = CaptureOutputs(task, taskType);
        tracker?.SetOutputs(outputs);

        return success;
    }

    /// <summary>
    /// Captures input properties from the task instance.
    /// </summary>
    /// <remarks>
    /// Automatically includes:
    /// <list type="bullet">
    /// <item>All properties marked with [Required]</item>
    /// <item>All properties marked with [ProfileInput] (unless Exclude=true)</item>
    /// </list>
    /// </remarks>
    private static Dictionary<string, object?> CaptureInputs<T>(T task, Type taskType) where T : MsBuildTask
    {
        var inputs = new Dictionary<string, object?>();

        foreach (var prop in taskType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            // Check for explicit profile attribute
            var profileAttr = prop.GetCustomAttribute<ProfileInputAttribute>();
            if (profileAttr?.Exclude == true)
                continue;

            // Include if: [Required], [ProfileInput], or has specific name patterns
            var shouldInclude =
                profileAttr != null ||
                prop.GetCustomAttribute<RequiredAttribute>() != null ||
                ShouldAutoIncludeAsInput(prop);

            if (shouldInclude)
            {
                var name = profileAttr?.Name ?? prop.Name;
                var value = prop.GetValue(task);

                // Don't include null or empty strings for cleaner output
                if (value != null && !(value is string s && string.IsNullOrEmpty(s)))
                {
                    inputs[name] = FormatValue(value);
                }
            }
        }

        return inputs;
    }

    /// <summary>
    /// Captures output properties from the task instance.
    /// </summary>
    /// <remarks>
    /// Automatically includes:
    /// <list type="bullet">
    /// <item>All properties marked with [Output]</item>
    /// <item>All properties marked with [ProfileOutput] (unless Exclude=true)</item>
    /// </list>
    /// </remarks>
    private static Dictionary<string, object?> CaptureOutputs<T>(T task, Type taskType) where T : MsBuildTask
    {
        var outputs = new Dictionary<string, object?>();

        foreach (var prop in taskType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            // Check for explicit profile attribute
            var profileAttr = prop.GetCustomAttribute<ProfileOutputAttribute>();
            if (profileAttr?.Exclude == true)
                continue;

            // Include if: [Output] or [ProfileOutput]
            var shouldInclude =
                profileAttr != null ||
                prop.GetCustomAttribute<OutputAttribute>() != null;

            if (shouldInclude)
            {
                var name = profileAttr?.Name ?? prop.Name;
                var value = prop.GetValue(task);

                // Don't include null or empty strings for cleaner output
                if (value != null && !(value is string s && string.IsNullOrEmpty(s)))
                {
                    outputs[name] = FormatValue(value);
                }
            }
        }

        return outputs;
    }

    /// <summary>
    /// Determines if a property should be auto-included as input based on naming conventions.
    /// </summary>
    private static bool ShouldAutoIncludeAsInput(PropertyInfo prop)
    {
        // Don't auto-include inherited Task properties
        if (prop.DeclaringType == typeof(MsBuildTask))
            return false;

        var name = prop.Name;

        // Include common input property patterns
        return name.EndsWith("Path", StringComparison.Ordinal) ||
               name.EndsWith("Dir", StringComparison.Ordinal) ||
               name.EndsWith("Directory", StringComparison.Ordinal) ||
               name == "Configuration" ||
               name == "ProjectPath" ||
               name == "ProjectFullPath";
    }

    /// <summary>
    /// Formats a value for JSON serialization, handling special types.
    /// </summary>
    private static object? FormatValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            ITaskItem item => item.ItemSpec,
            ITaskItem[] items => items.Select(i => i.ItemSpec).ToArray(),
            _ when value.GetType().IsArray => value,
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Gets the initiator name for profiling, typically from MSBuild target context.
    /// </summary>
    private static string? GetInitiator<T>(T task) where T : MsBuildTask
    {
        // Try to get from BuildEngine if available
        // For now, return null - could be enhanced with MSBuild context
        return null;
    }
}
