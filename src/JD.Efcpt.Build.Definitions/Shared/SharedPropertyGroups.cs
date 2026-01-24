using JD.MSBuild.Fluent.Common;
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
    public static void ConfigureTaskAssemblyResolution(PropsGroupBuilder group)
    {
        group.ResolveMultiTargetedTaskAssembly(
            folderProperty: "_EfcptTasksFolder",
            assemblyProperty: "_EfcptTaskAssembly",
            assemblyFileName: "JD.Efcpt.Build.Tasks.dll",
            nugetTasksPath: "$(MSBuildThisFileDirectory)..\\tasks",
            localProjectPath: "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks");
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
        group.Property<EfcptConfigUseNullableReferenceTypes>("true", 
            "'$(EfcptConfigUseNullableReferenceTypes)'=='' and ('$(Nullable)'=='enable' or '$(Nullable)'=='Enable')");
        
        group.Property<EfcptConfigUseNullableReferenceTypes>("false", 
            "'$(EfcptConfigUseNullableReferenceTypes)'=='' and '$(Nullable)'!=''");
    }
}
