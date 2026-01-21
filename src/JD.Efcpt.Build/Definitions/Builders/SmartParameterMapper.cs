using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;
using JDEfcptBuild.Constants;

namespace JD.Efcpt.Build.Definitions.Builders;

/// <summary>
/// Smart parameter mapper that auto-infers parameter names and wraps properties.
/// Eliminates task.Param(X, Property(Y)) boilerplate.
/// </summary>
public static class SmartParameterMapper
{
    /// <summary>
    /// Maps a property to a parameter, auto-wrapping with MsBuildExpressions.Property()
    /// </summary>
    public static TaskInvocationBuilder Map(this TaskInvocationBuilder task, string paramName, string propertyName)
    {
        task.Param(paramName, MsBuildExpressions.Property(propertyName));
        return task;
    }
    
    /// <summary>
    /// Maps multiple properties at once using params array
    /// </summary>
    public static TaskInvocationBuilder MapProps(this TaskInvocationBuilder task, params (string param, string prop)[] mappings)
    {
        foreach (var (param, prop) in mappings)
        {
            task.Param(param, MsBuildExpressions.Property(prop));
        }
        return task;
    }
}
