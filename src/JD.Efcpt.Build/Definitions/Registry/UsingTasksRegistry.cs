using JD.Efcpt.Build.Tasks;
using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions.Registry;

/// <summary>
/// Centralized registry for all JD.Efcpt.Build custom MSBuild tasks.
/// Automatically registers all task assemblies with MSBuild using compile-time type safety via nameof().
/// </summary>
public static class UsingTasksRegistry
{
    /// <summary>
    /// All custom task types in the JD.Efcpt.Build.Tasks assembly.
    /// Using nameof() provides compile-time safety and refactoring support.
    /// </summary>
    private static readonly string[] TaskNames =
    [
        nameof(AddSqlFileWarnings),
        nameof(ApplyConfigOverrides),
        nameof(CheckSdkVersion),
        nameof(ComputeFingerprint),
        nameof(DetectSqlProject),
        nameof(EnsureDacpacBuilt),
        nameof(FinalizeBuildProfiling),
        nameof(InitializeBuildProfiling),
        nameof(QuerySchemaMetadata),
        nameof(RenameGeneratedFiles),
        nameof(ResolveDbContextName),
        nameof(ResolveSqlProjAndInputs),
        nameof(RunEfcpt),
        nameof(RunSqlPackage),
        nameof(SerializeConfigProperties),
        nameof(StageEfcptInputs)
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
