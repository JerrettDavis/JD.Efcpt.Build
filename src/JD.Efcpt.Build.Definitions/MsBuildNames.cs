using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// Strongly-typed MSBuild task names specific to JD.Efcpt.Build.
/// For well-known MSBuild names, see <see cref="WellKnownMsBuild"/>.
/// </summary>
public static class EfcptTaskNames
{
    // ====================================================================================
    // JD.Efcpt.Build Tasks
    // ====================================================================================
    
    public readonly struct DetectSqlProjectTask : IMsBuildTaskName
    {
        public string Name => "DetectSqlProject";
    }
    
    public readonly struct ResolveSqlProjAndInputsTask : IMsBuildTaskName
    {
        public string Name => "ResolveSqlProjAndInputs";
    }
    
    public readonly struct EnsureDacpacBuiltTask : IMsBuildTaskName
    {
        public string Name => "EnsureDacpacBuilt";
    }
    
    public readonly struct StageEfcptInputsTask : IMsBuildTaskName
    {
        public string Name => "StageEfcptInputs";
    }
    
    public readonly struct ComputeFingerprintTask : IMsBuildTaskName
    {
        public string Name => "ComputeFingerprint";
    }
    
    public readonly struct RunEfcptTask : IMsBuildTaskName
    {
        public string Name => "RunEfcpt";
    }
    
    public readonly struct RenameGeneratedFilesTask : IMsBuildTaskName
    {
        public string Name => "RenameGeneratedFiles";
    }
    
    public readonly struct QuerySchemaMetadataTask : IMsBuildTaskName
    {
        public string Name => "QuerySchemaMetadata";
    }
    
    public readonly struct ApplyConfigOverridesTask : IMsBuildTaskName
    {
        public string Name => "ApplyConfigOverrides";
    }
    
    public readonly struct ResolveDbContextNameTask : IMsBuildTaskName
    {
        public string Name => "ResolveDbContextName";
    }
    
    public readonly struct SerializeConfigPropertiesTask : IMsBuildTaskName
    {
        public string Name => "SerializeConfigProperties";
    }
    
    public readonly struct CheckSdkVersionTask : IMsBuildTaskName
    {
        public string Name => "CheckSdkVersion";
    }
    
    public readonly struct RunSqlPackageTask : IMsBuildTaskName
    {
        public string Name => "RunSqlPackage";
    }
    
    public readonly struct AddSqlFileWarningsTask : IMsBuildTaskName
    {
        public string Name => "AddSqlFileWarnings";
    }
    
    public readonly struct InitializeBuildProfilingTask : IMsBuildTaskName
    {
        public string Name => "InitializeBuildProfiling";
    }
    
    public readonly struct FinalizeBuildProfilingTask : IMsBuildTaskName
    {
        public string Name => "FinalizeBuildProfiling";
    }
}
