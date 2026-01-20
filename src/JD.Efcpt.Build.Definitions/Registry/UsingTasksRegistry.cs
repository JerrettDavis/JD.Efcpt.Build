using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions.Registry;

/// <summary>
/// Centralized registry for all JD.Efcpt.Build custom MSBuild tasks.
/// Automatically registers all task assemblies with MSBuild using a data-driven approach.
/// </summary>
public static class UsingTasksRegistry
{
    /// <summary>
    /// All custom task names in the JD.Efcpt.Build.Tasks assembly.
    /// Adding a new task only requires adding its name to this array.
    /// </summary>
    private static readonly string[] TaskNames =
    [
        "AddSqlFileWarnings",
        "ApplyConfigOverrides",
        "CheckSdkVersion",
        "ComputeFingerprint",
        "DetectSqlProject",
        "EnsureDacpacBuilt",
        "FinalizeBuildProfiling",
        "InitializeBuildProfiling",
        "QuerySchemaMetadata",
        "RenameGeneratedFiles",
        "ResolveDbContextName",
        "ResolveSqlProjAndInputs",
        "RunEfcpt",
        "RunSqlPackage",
        "SerializeConfigProperties",
        "StageEfcptInputs"
    ];
    
    /// <summary>
    /// Registers all EFCPT custom tasks with MSBuild.
    /// Uses the resolved task assembly path from SharedPropertyGroups.
    /// </summary>
    /// <param name="t">The targets builder to register tasks with.</param>
    public static void RegisterAll(TargetsBuilder t)
    {
        const string assemblyPath = "$(_EfcptTaskAssembly)";
        
        foreach (var taskName in TaskNames)
        {
            t.UsingTask($"JD.Efcpt.Build.Tasks.{taskName}", assemblyPath);
        }
    }
}
