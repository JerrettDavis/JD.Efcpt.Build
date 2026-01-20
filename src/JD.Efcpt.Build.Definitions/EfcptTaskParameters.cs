using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// Task-specific parameter names for JD.Efcpt.Build tasks.
/// These are the input/output parameter names defined on the custom tasks.
/// </summary>
public static class EfcptTaskParameters
{
    // DetectSqlProject task parameters
    public readonly struct ProjectPathParameter : IMsBuildTaskParameterName
    {
        public string Name => "ProjectPath";
    }
    
    public readonly struct SqlServerVersionParameter : IMsBuildTaskParameterName
    {
        public string Name => "SqlServerVersion";
    }
    
    public readonly struct DSPParameter : IMsBuildTaskParameterName
    {
        public string Name => "DSP";
    }
    
    public readonly struct IsSqlProjectParameter : IMsBuildTaskParameterName
    {
        public string Name => "IsSqlProject";
    }
    
    // ResolveSqlProjAndInputs task parameters
    public readonly struct SolutionDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "SolutionDir";
    }
    
    public readonly struct SolutionPathParameter : IMsBuildTaskParameterName
    {
        public string Name => "SolutionPath";
    }
    
    public readonly struct ProbeSolutionDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "ProbeSolutionDir";
    }
    
    public readonly struct ProjectDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "ProjectDir";
    }
    
    public readonly struct SqlProjParameter : IMsBuildTaskParameterName
    {
        public string Name => "SqlProj";
    }
    
    public readonly struct ConnectionStringParameter : IMsBuildTaskParameterName
    {
        public string Name => "ConnectionString";
    }
    
    public readonly struct DacpacParameter : IMsBuildTaskParameterName
    {
        public string Name => "Dacpac";
    }
    
    public readonly struct UseConnectionStringParameter : IMsBuildTaskParameterName
    {
        public string Name => "UseConnectionString";
    }
    
    public readonly struct UseDirectDacpacParameter : IMsBuildTaskParameterName
    {
        public string Name => "UseDirectDacpac";
    }
    
    // StageEfcptInputs task parameters
    public readonly struct ConfigParameter : IMsBuildTaskParameterName
    {
        public string Name => "Config";
    }
    
    public readonly struct RenamingParameter : IMsBuildTaskParameterName
    {
        public string Name => "Renaming";
    }
    
    public readonly struct TemplateDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "TemplateDir";
    }
    
    public readonly struct StagingDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "StagingDir";
    }
    
    public readonly struct ResolvedConfigParameter : IMsBuildTaskParameterName
    {
        public string Name => "ResolvedConfig";
    }
    
    public readonly struct ResolvedRenamingParameter : IMsBuildTaskParameterName
    {
        public string Name => "ResolvedRenaming";
    }
    
    public readonly struct ResolvedTemplateDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "ResolvedTemplateDir";
    }
    
    public readonly struct IsUsingDefaultConfigParameter : IMsBuildTaskParameterName
    {
        public string Name => "IsUsingDefaultConfig";
    }
    
    // ComputeFingerprint task parameters
    public readonly struct DacpacPathParameter : IMsBuildTaskParameterName
    {
        public string Name => "DacpacPath";
    }
    
    public readonly struct FingerprintFileParameter : IMsBuildTaskParameterName
    {
        public string Name => "FingerprintFile";
    }
    
    public readonly struct FingerprintChangedParameter : IMsBuildTaskParameterName
    {
        public string Name => "FingerprintChanged";
    }
    
    // RunEfcpt task parameters
    public readonly struct DotNetExeParameter : IMsBuildTaskParameterName
    {
        public string Name => "DotNetExe";
    }
    
    public readonly struct ToolModeParameter : IMsBuildTaskParameterName
    {
        public string Name => "ToolMode";
    }
    
    public readonly struct ToolPathParameter : IMsBuildTaskParameterName
    {
        public string Name => "ToolPath";
    }
    
    public readonly struct ToolCommandParameter : IMsBuildTaskParameterName
    {
        public string Name => "ToolCommand";
    }
    
    public readonly struct ToolRestoreParameter : IMsBuildTaskParameterName
    {
        public string Name => "ToolRestore";
    }
    
    public readonly struct ToolPackageIdParameter : IMsBuildTaskParameterName
    {
        public string Name => "ToolPackageId";
    }
    
    public readonly struct ToolVersionParameter : IMsBuildTaskParameterName
    {
        public string Name => "ToolVersion";
    }
    
    public readonly struct ProviderParameter : IMsBuildTaskParameterName
    {
        public string Name => "Provider";
    }
    
    public readonly struct WorkingDirectoryParameter : IMsBuildTaskParameterName
    {
        public string Name => "WorkingDirectory";
    }
    
    public readonly struct OutputDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "OutputDir";
    }
    
    public readonly struct DatabaseNameParameter : IMsBuildTaskParameterName
    {
        public string Name => "DatabaseName";
    }
    
    public readonly struct LogVerbosityParameter : IMsBuildTaskParameterName
    {
        public string Name => "LogVerbosity";
    }
    
    // RenameGeneratedFiles task parameters
    public readonly struct DataProjectParameter : IMsBuildTaskParameterName
    {
        public string Name => "DataProject";
    }
    
    public readonly struct DataProjectDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "DataProjectDir";
    }
    
    public readonly struct DataProjectOutputSubdirParameter : IMsBuildTaskParameterName
    {
        public string Name => "DataProjectOutputSubdir";
    }
    
    public readonly struct HasFilesToCopyParameter : IMsBuildTaskParameterName
    {
        public string Name => "HasFilesToCopy";
    }
    
    public readonly struct DataDestDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "DataDestDir";
    }
    
    public readonly struct DataProjectPathParameter : IMsBuildTaskParameterName
    {
        public string Name => "DataProjectPath";
    }
    
    // ApplyConfigOverrides task parameters
    public readonly struct ConfigFileParameter : IMsBuildTaskParameterName
    {
        public string Name => "ConfigFile";
    }
    
    public readonly struct OverrideCountParameter : IMsBuildTaskParameterName
    {
        public string Name => "OverrideCount";
    }
    
    // ResolveDbContextName task parameters
    public readonly struct DbContextNameParameter : IMsBuildTaskParameterName
    {
        public string Name => "DbContextName";
    }
    
    // SerializeConfigProperties task parameters
    public readonly struct OutputFileParameter : IMsBuildTaskParameterName
    {
        public string Name => "OutputFile";
    }
    
    // QuerySchemaMetadata task parameters
    public readonly struct ScriptsDirParameter : IMsBuildTaskParameterName
    {
        public string Name => "ScriptsDir";
    }
    
    // InitializeBuildProfiling task parameters
    public readonly struct EnableProfilingParameter : IMsBuildTaskParameterName
    {
        public string Name => "EnableProfiling";
    }
    
    public readonly struct ProfilingVerbosityParameter : IMsBuildTaskParameterName
    {
        public string Name => "ProfilingVerbosity";
    }
    
    // Common task parameters (Message, Error, Warning)
    public readonly struct TextParameter : IMsBuildTaskParameterName
    {
        public string Name => "Text";
    }
    
    public readonly struct ImportanceParameter : IMsBuildTaskParameterName
    {
        public string Name => "Importance";
    }
    
    public readonly struct ConditionParameter : IMsBuildTaskParameterName
    {
        public string Name => "Condition";
    }
    
    public readonly struct CodeParameter : IMsBuildTaskParameterName
    {
        public string Name => "Code";
    }
    
    public readonly struct FileParameter : IMsBuildTaskParameterName
    {
        public string Name => "File";
    }
    
    public readonly struct HelpKeywordParameter : IMsBuildTaskParameterName
    {
        public string Name => "HelpKeyword";
    }
}
