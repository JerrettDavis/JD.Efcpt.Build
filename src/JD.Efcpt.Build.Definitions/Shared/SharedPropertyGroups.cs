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
            "_EfcptTasksFolder",
            "_EfcptTaskAssembly",
            "JD.Efcpt.Build.Tasks.dll",
            "JD.Efcpt.Build",
            ("net10.0", "18.0"),
            ("net9.0", "17.12"),
            ("net8.0", "15.0"),
            ("net472", "15.0"));
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
        group.Property("EfcptConfigUseNullableReferenceTypes", "true",
            "'$(EfcptConfigUseNullableReferenceTypes)'=='' and ('$(Nullable)'=='enable' or '$(Nullable)'=='Enable')");

        group.Property("EfcptConfigUseNullableReferenceTypes", "false",
            "'$(EfcptConfigUseNullableReferenceTypes)'=='' and '$(Nullable)'!=''");
    }

    /// <summary>
    /// Forces EfcptEnabled=false for the duration of an IDE design-time build (IntelliSense),
    /// unless the consuming project opted back in via EfcptRunDuringDesignTimeBuild=true.
    /// </summary>
    /// <remarks>
    /// This must run in the targets file (evaluated after the consuming project's own
    /// PropertyGroup), not in props (evaluated before it) - otherwise a project-level
    /// &lt;EfcptRunDuringDesignTimeBuild&gt;true&lt;/EfcptRunDuringDesignTimeBuild&gt; override
    /// would not be visible yet when this condition is checked, and the pipeline would stay
    /// disabled even when the user explicitly asked for it to run.
    /// </remarks>
    public static void ConfigureDesignTimeBuildGuard(PropsGroupBuilder group)
    {
        group.Property("EfcptEnabled", "false",
            "'$(EfcptEnabled)'=='true' and '$(DesignTimeBuild)'=='true' and '$(EfcptRunDuringDesignTimeBuild)'!='true'");
    }
}
