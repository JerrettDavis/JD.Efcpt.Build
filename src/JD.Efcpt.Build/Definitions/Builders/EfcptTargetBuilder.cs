using JD.Efcpt.Build.Definitions.Constants;
using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions.Builders;

/// <summary>
/// Fluent builder for creating Efcpt targets with common condition patterns.
/// </summary>
public class EfcptTargetBuilder
{
    private readonly TargetBuilder _target;
    private string? _condition;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfcptTargetBuilder"/> class.
    /// </summary>
    /// <param name="target">The underlying target builder.</param>
    public EfcptTargetBuilder(TargetBuilder target)
    {
        _target = target;
    }

    /// <summary>
    /// Sets the condition to: Efcpt enabled AND NOT SQL project.
    /// Use this for EF Core code generation targets.
    /// </summary>
    public EfcptTargetBuilder ForEfCoreGeneration()
    {
        _condition = MsBuildExpressions.Condition_And(
            MsBuildExpressions.Condition_IsTrue(EfcptProperties.EfcptEnabled),
            MsBuildExpressions.Condition_IsFalse(EfcptProperties._EfcptIsSqlProject));
        return this;
    }

    /// <summary>
    /// Sets the condition to: Efcpt enabled AND SQL project.
    /// Use this for SQL project generation targets.
    /// </summary>
    public EfcptTargetBuilder ForSqlProjectGeneration()
    {
        _condition = MsBuildExpressions.Condition_And(
            MsBuildExpressions.Condition_IsTrue(EfcptProperties.EfcptEnabled),
            MsBuildExpressions.Condition_IsTrue(EfcptProperties._EfcptIsSqlProject));
        return this;
    }

    /// <summary>
    /// Sets the condition to: Efcpt enabled.
    /// Use this for targets that run regardless of project type.
    /// </summary>
    public EfcptTargetBuilder WhenEnabled()
    {
        _condition = MsBuildExpressions.Condition_IsTrue(EfcptProperties.EfcptEnabled);
        return this;
    }

    /// <summary>
    /// Sets the target dependencies (DependsOnTargets).
    /// </summary>
    public EfcptTargetBuilder DependsOn(params string[] targetNames)
    {
        if (targetNames.Length > 0)
        {
            _target.DependsOnTargets(string.Join(";", targetNames));
        }
        return this;
    }

    /// <summary>
    /// Sets the BeforeTargets attribute.
    /// </summary>
    public EfcptTargetBuilder Before(params string[] targetNames)
    {
        if (targetNames.Length > 0)
        {
            _target.BeforeTargets(string.Join(";", targetNames));
        }
        return this;
    }

    /// <summary>
    /// Sets the AfterTargets attribute.
    /// </summary>
    public EfcptTargetBuilder After(params string[] targetNames)
    {
        if (targetNames.Length > 0)
        {
            _target.AfterTargets(string.Join(";", targetNames));
        }
        return this;
    }

    /// <summary>
    /// Adds an informational message (High importance) to the target.
    /// </summary>
    public EfcptTargetBuilder LogInfo(string message)
    {
        _target.Task(MsBuildTasks.Message, task =>
        {
            task.Param("Text", message);
            task.Param("Importance", PropertyValues.High);
        });
        return this;
    }

    /// <summary>
    /// Adds a normal message (Normal importance) to the target.
    /// </summary>
    public EfcptTargetBuilder LogNormal(string message)
    {
        _target.Task(MsBuildTasks.Message, task =>
        {
            task.Param("Text", message);
            task.Param("Importance", PropertyValues.Normal);
        });
        return this;
    }

    /// <summary>
    /// Builds and returns the underlying target builder.
    /// Applies the accumulated condition if set.
    /// </summary>
    public TargetBuilder Build()
    {
        if (_condition != null)
        {
            _target.Condition(_condition);
        }
        return _target;
    }
}
