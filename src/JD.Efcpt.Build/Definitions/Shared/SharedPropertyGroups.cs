using JD.Efcpt.Build.Definitions.Constants;
using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions.Shared;

/// <summary>
/// Shared property group configurations used across both Props and Targets.
/// Eliminates duplication and provides single source of truth.
/// </summary>
public static class SharedPropertyGroups
{
    /// <summary>
    /// Configures MSBuild property resolution for selecting the correct task assembly
    /// based on MSBuild runtime version and type.
    /// </summary>
    /// <remarks>
    /// <para><strong>Resolution Strategy:</strong></para>
    /// <list type="number">
    /// <item>net10.0 for MSBuild 18.0+ (Visual Studio 2026+)</item>
    /// <item>net10.0 for MSBuild 17.14+ (Visual Studio 2024 Update 14+)</item>
    /// <item>net9.0 for MSBuild 17.12+ (Visual Studio 2024 Update 12+)</item>
    /// <item>net8.0 for earlier .NET Core MSBuild versions</item>
    /// <item>net472 for .NET Framework MSBuild (Visual Studio 2017/2019)</item>
    /// </list>
    /// <para>
    /// The assembly path resolution follows this fallback order:
    /// 1. Packaged tasks folder (for NuGet consumption)
    /// 2. Local build output with $(Configuration)
    /// 3. Local Debug build output (for development)
    /// </para>
    /// </remarks>
    public static void ConfigureTaskAssemblyResolution(PropsGroupBuilder group)
    {
        // MSBuild 18.0+ (VS 2026+)
        group.Property(EfcptProperties._EfcptTasksFolder, PropertyValues.Net10_0, 
            MsBuildExpressions.Condition_RuntimeTypeAndVersion(PropertyValues.Core, PropertyValues.MsBuildVersion_18_0));
        
        // MSBuild 17.14+ (VS 2024 Update 14+)
        group.Property(EfcptProperties._EfcptTasksFolder, PropertyValues.Net10_0, 
            MsBuildExpressions.Condition_And(
                MsBuildExpressions.Condition_IsEmpty(EfcptProperties._EfcptTasksFolder),
                MsBuildExpressions.Condition_RuntimeTypeAndVersion(PropertyValues.Core, PropertyValues.MsBuildVersion_17_14)
            ));
        
        // MSBuild 17.12+ (VS 2024 Update 12+)
        group.Property(EfcptProperties._EfcptTasksFolder, PropertyValues.Net9_0, 
            MsBuildExpressions.Condition_And(
                MsBuildExpressions.Condition_IsEmpty(EfcptProperties._EfcptTasksFolder),
                MsBuildExpressions.Condition_RuntimeTypeAndVersion(PropertyValues.Core, PropertyValues.MsBuildVersion_17_12)
            ));
        
        // Earlier .NET Core MSBuild
        group.Property(EfcptProperties._EfcptTasksFolder, PropertyValues.Net8_0, 
            MsBuildExpressions.Condition_And(
                MsBuildExpressions.Condition_IsEmpty(EfcptProperties._EfcptTasksFolder),
                MsBuildExpressions.Condition_Equals(MsBuildProperties.MSBuildRuntimeType, PropertyValues.Core)
            ));
        
        // .NET Framework MSBuild (VS 2017/2019)
        group.Property(EfcptProperties._EfcptTasksFolder, PropertyValues.Net472, 
            MsBuildExpressions.Condition_IsEmpty(EfcptProperties._EfcptTasksFolder));
        
        // Assembly path resolution with fallbacks
        group.Property(EfcptProperties._EfcptTaskAssembly, 
            MsBuildExpressions.Path_Combine(
                MsBuildExpressions.Property(MsBuildProperties.MSBuildThisFileDirectory),
                PathPatterns.Tasks_RelativePath,
                MsBuildExpressions.Property(EfcptProperties._EfcptTasksFolder),
                PathPatterns.TaskAssembly_Name
            ));
        
        group.Property(EfcptProperties._EfcptTaskAssembly, 
            MsBuildExpressions.Path_Combine(
                MsBuildExpressions.Property(MsBuildProperties.MSBuildThisFileDirectory),
                PathPatterns.TaskAssembly_LocalBuild,
                MsBuildExpressions.Property(MsBuildProperties.Configuration),
                MsBuildExpressions.Property(EfcptProperties._EfcptTasksFolder),
                PathPatterns.TaskAssembly_Name
            ),
            MsBuildExpressions.Condition_NotExists(MsBuildExpressions.Property(EfcptProperties._EfcptTaskAssembly)));
        
        group.Property(EfcptProperties._EfcptTaskAssembly, 
            MsBuildExpressions.Path_Combine(
                MsBuildExpressions.Property(MsBuildProperties.MSBuildThisFileDirectory),
                PathPatterns.TaskAssembly_Debug,
                MsBuildExpressions.Property(EfcptProperties._EfcptTasksFolder),
                PathPatterns.TaskAssembly_Name
            ),
            MsBuildExpressions.Condition_And(
                MsBuildExpressions.Condition_NotExists(MsBuildExpressions.Property(EfcptProperties._EfcptTaskAssembly)),
                MsBuildExpressions.Condition_IsEmpty(MsBuildProperties.Configuration)
            ));
    }

    /// <summary>
    /// Configures EfcptConfigUseNullableReferenceTypes property based on project's Nullable setting.
    /// Provides zero-config experience by deriving EFCPT settings from standard project settings.
    /// </summary>
    /// <remarks>
    /// <para><strong>Logic:</strong></para>
    /// <list type="bullet">
    /// <item>If Nullable is "enable" or "Enable" → set to true</item>
    /// <item>If Nullable has any other value → set to false</item>
    /// <item>If Nullable is not set → leave EfcptConfigUseNullableReferenceTypes as-is (user override)</item>
    /// </list>
    /// </remarks>
    public static void ConfigureNullableReferenceTypes(PropsGroupBuilder group)
    {
        group.Property(EfcptProperties.EfcptConfigUseNullableReferenceTypes, PropertyValues.True, 
            MsBuildExpressions.Condition_And(
                MsBuildExpressions.Condition_IsEmpty(EfcptProperties.EfcptConfigUseNullableReferenceTypes),
                MsBuildExpressions.Condition_Or(
                    MsBuildExpressions.Condition_Equals(MsBuildProperties.Nullable, PropertyValues.Enable),
                    MsBuildExpressions.Condition_Equals(MsBuildProperties.Nullable, PropertyValues.Enable_Capitalized)
                )
            ));
        
        group.Property(EfcptProperties.EfcptConfigUseNullableReferenceTypes, PropertyValues.False, 
            MsBuildExpressions.Condition_And(
                MsBuildExpressions.Condition_IsEmpty(EfcptProperties.EfcptConfigUseNullableReferenceTypes),
                MsBuildExpressions.Condition_NotEmpty(MsBuildProperties.Nullable)
            ));
    }
}
