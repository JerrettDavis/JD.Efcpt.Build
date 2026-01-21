using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions.Builders;

/// <summary>
/// Simplifies complex PropertyGroup patterns in MSBuild targets.
/// Reduces boilerplate for conditional property assignments.
/// </summary>
public static class PropertyGroupBuilder
{
    /// <summary>
    /// Adds conditional property assignment with user override and default fallback.
    /// First PropertyGroup sets the property if user provided a value.
    /// Second PropertyGroup sets the property to default if still empty.
    /// </summary>
    public static void AddConditionalDefaults(TargetBuilder target,
        string propertyName, 
        string userValue, 
        string defaultValue,
        string? userCondition = null,
        string? defaultCondition = null)
    {
        target.PropertyGroup(userCondition, group =>
        {
            group.Property(propertyName, userValue);
        });
        target.PropertyGroup(defaultCondition, group =>
        {
            group.Property(propertyName, defaultValue);
        });
    }
}
