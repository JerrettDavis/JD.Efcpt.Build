using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// Strongly-typed MSBuild property, target, task, and item names.
/// Eliminates magic strings and provides compile-time safety.
/// </summary>
public static class MsBuildNames
{
    // ====================================================================================
    // Well-Known MSBuild Targets (from Microsoft.Common.targets, etc.)
    // ====================================================================================
    
    public readonly struct BeforeBuildTarget : IMsBuildTargetName
    {
        public string Name => "BeforeBuild";
    }
    
    public readonly struct BeforeRebuildTarget : IMsBuildTargetName
    {
        public string Name => "BeforeRebuild";
    }
    
    public readonly struct BuildTarget : IMsBuildTargetName
    {
        public string Name => "Build";
    }
    
    public readonly struct CoreCompileTarget : IMsBuildTargetName
    {
        public string Name => "CoreCompile";
    }
    
    public readonly struct CleanTarget : IMsBuildTargetName
    {
        public string Name => "Clean";
    }
    
    // ====================================================================================
    // Well-Known MSBuild Properties (from Microsoft.Common.targets, etc.)
    // ====================================================================================
    
    public readonly struct ConfigurationProperty : IMsBuildPropertyName
    {
        public string Name => "Configuration";
    }
    
    public readonly struct MSBuildProjectFullPathProperty : IMsBuildPropertyName
    {
        public string Name => "MSBuildProjectFullPath";
    }
    
    public readonly struct MSBuildRuntimeTypeProperty : IMsBuildPropertyName
    {
        public string Name => "MSBuildRuntimeType";
    }
    
    public readonly struct MSBuildVersionProperty : IMsBuildPropertyName
    {
        public string Name => "MSBuildVersion";
    }
    
    public readonly struct MSBuildThisFileDirectoryProperty : IMsBuildPropertyName
    {
        public string Name => "MSBuildThisFileDirectory";
    }
    
    public readonly struct NullableProperty : IMsBuildPropertyName
    {
        public string Name => "Nullable";
    }
    
    // ====================================================================================
    // SQL Project Properties (MSBuild.Sdk.SqlProj, Microsoft.Build.Sql)
    // ====================================================================================
    
    public readonly struct SqlServerVersionProperty : IMsBuildPropertyName
    {
        public string Name => "SqlServerVersion";
    }
    
    public readonly struct DSPProperty : IMsBuildPropertyName
    {
        public string Name => "DSP";
    }
    
    // ====================================================================================
    // Common MSBuild Tasks
    // ====================================================================================
    
    public readonly struct MessageTask : IMsBuildTaskName
    {
        public string Name => "Message";
    }
    
    public readonly struct ErrorTask : IMsBuildTaskName
    {
        public string Name => "Error";
    }
    
    public readonly struct WarningTask : IMsBuildTaskName
    {
        public string Name => "Warning";
    }
    
    public readonly struct CopyTask : IMsBuildTaskName
    {
        public string Name => "Copy";
    }
    
    public readonly struct MakeDirTask : IMsBuildTaskName
    {
        public string Name => "MakeDir";
    }
    
    public readonly struct DeleteTask : IMsBuildTaskName
    {
        public string Name => "Delete";
    }
    
    public readonly struct TouchTask : IMsBuildTaskName
    {
        public string Name => "Touch";
    }
    
    public readonly struct ExecTask : IMsBuildTaskName
    {
        public string Name => "Exec";
    }
    
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
