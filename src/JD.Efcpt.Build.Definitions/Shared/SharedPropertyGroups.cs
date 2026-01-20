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
    public static void ConfigureTaskAssemblyResolution(IPropertyGroupBuilder group)
    {
        // MSBuild 18.0+ (VS 2026+)
        group.Property("_EfcptTasksFolder", "net10.0", 
            "'$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))");
        
        // MSBuild 17.14+ (VS 2024 Update 14+)
        group.Property("_EfcptTasksFolder", "net10.0", 
            "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.14'))");
        
        // MSBuild 17.12+ (VS 2024 Update 12+)
        group.Property("_EfcptTasksFolder", "net9.0", 
            "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.12'))");
        
        // Earlier .NET Core MSBuild
        group.Property("_EfcptTasksFolder", "net8.0", 
            "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core'");
        
        // .NET Framework MSBuild (VS 2017/2019)
        group.Property("_EfcptTasksFolder", "net472", 
            "'$(_EfcptTasksFolder)' == ''");
        
        // Assembly path resolution with fallbacks
        group.Property("_EfcptTaskAssembly", 
            "$(MSBuildThisFileDirectory)..\\tasks\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll");
        
        group.Property("_EfcptTaskAssembly", 
            "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks\\bin\\$(Configuration)\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll", 
            "!Exists('$(_EfcptTaskAssembly)')");
        
        group.Property("_EfcptTaskAssembly", 
            "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks\\bin\\Debug\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll", 
            "!Exists('$(_EfcptTaskAssembly)') and '$(Configuration)' == ''");
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
    public static void ConfigureNullableReferenceTypes(IPropertyGroupBuilder group)
    {
        group.Property<EfcptConfigUseNullableReferenceTypes>("true", 
            "'$(EfcptConfigUseNullableReferenceTypes)'=='' and ('$(Nullable)'=='enable' or '$(Nullable)'=='Enable')");
        
        group.Property<EfcptConfigUseNullableReferenceTypes>("false", 
            "'$(EfcptConfigUseNullableReferenceTypes)'=='' and '$(Nullable)'!=''");
    }
}
