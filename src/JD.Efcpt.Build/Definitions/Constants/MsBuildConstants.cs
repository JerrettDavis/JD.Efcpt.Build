namespace JDEfcptBuild.Constants;

/// <summary>
/// Well-known MSBuild property names.
/// </summary>
public static class MsBuildProperties
{
    // MSBuild built-in properties
    public const string MSBuildProjectFullPath = nameof(MSBuildProjectFullPath);
    public const string MSBuildProjectName = nameof(MSBuildProjectName);
    public const string MSBuildProjectDirectory = nameof(MSBuildProjectDirectory);
    public const string MSBuildRuntimeType = nameof(MSBuildRuntimeType);
    public const string MSBuildVersion = nameof(MSBuildVersion);
    public const string Configuration = nameof(Configuration);
    public const string TargetFramework = nameof(TargetFramework);
    public const string OutputPath = nameof(OutputPath);
    public const string IntermediateOutputPath = nameof(IntermediateOutputPath);
    
    // SQL Project properties
    public const string SqlServerVersion = nameof(SqlServerVersion);
    public const string DSP = nameof(DSP);
    public const string DacVersion = nameof(DacVersion);
    
    // .NET properties
    public const string Nullable = nameof(Nullable);
}

/// <summary>
/// Efcpt-specific MSBuild property names.
/// </summary>
public static class EfcptProperties
{
    // Control properties
    public const string EfcptEnabled = nameof(EfcptEnabled);
    public const string EfcptLogVerbosity = nameof(EfcptLogVerbosity);
    public const string EfcptCheckForUpdates = nameof(EfcptCheckForUpdates);
    public const string EfcptEnableProfiling = nameof(EfcptEnableProfiling);
    
    // Configuration properties
    public const string EfcptConfigPath = nameof(EfcptConfigPath);
    public const string EfcptRenamingPath = nameof(EfcptRenamingPath);
    public const string EfcptTemplateDir = nameof(EfcptTemplateDir);
    public const string EfcptOutputDir = nameof(EfcptOutputDir);
    public const string EfcptProvider = nameof(EfcptProvider);
    public const string EfcptConnectionString = nameof(EfcptConnectionString);
    public const string EfcptDbContextName = nameof(EfcptDbContextName);
    
    // Config option properties
    public const string EfcptConfigUseNullableReferenceTypes = nameof(EfcptConfigUseNullableReferenceTypes);
    
    // SQL Project properties
    public const string EfcptSqlProj = nameof(EfcptSqlProj);
    public const string EfcptSqlProjOutputDir = nameof(EfcptSqlProjOutputDir);
    public const string EfcptBuildSqlProj = nameof(EfcptBuildSqlProj);
    
    // Direct DACPAC properties
    public const string EfcptDacpacPath = nameof(EfcptDacpacPath);
    
    // Internal resolved properties (prefixed with _)
    public const string _EfcptIsSqlProject = nameof(_EfcptIsSqlProject);
    public const string _EfcptResolvedConfig = nameof(_EfcptResolvedConfig);
    public const string _EfcptResolvedRenaming = nameof(_EfcptResolvedRenaming);
    public const string _EfcptResolvedTemplateDir = nameof(_EfcptResolvedTemplateDir);
    public const string _EfcptSqlProj = nameof(_EfcptSqlProj);
    public const string _EfcptSqlProjInputs = nameof(_EfcptSqlProjInputs);
    public const string _EfcptDacpacPath = nameof(_EfcptDacpacPath);
    public const string _EfcptTasksFolder = nameof(_EfcptTasksFolder);
    public const string _EfcptTaskAssembly = nameof(_EfcptTaskAssembly);
    public const string _EfcptSqlProjOutputDir = nameof(_EfcptSqlProjOutputDir);
    public const string _EfcptFingerprint = nameof(_EfcptFingerprint);
    public const string _EfcptDbContextName = nameof(_EfcptDbContextName);
}

/// <summary>
/// Well-known MSBuild target names.
/// </summary>
public static class MsBuildTargets
{
    // Standard .NET SDK targets
    public const string BeforeBuild = nameof(BeforeBuild);
    public const string AfterBuild = nameof(AfterBuild);
    public const string Build = nameof(Build);
    public const string BeforeRebuild = nameof(BeforeRebuild);
    public const string CoreBuild = nameof(CoreBuild);
    public const string Clean = nameof(Clean);
    public const string BeforeClean = nameof(BeforeClean);
    public const string CoreCompile = nameof(CoreCompile);
}

/// <summary>
/// Efcpt-specific MSBuild target names.
/// </summary>
public static class EfcptTargets
{
    // Public extensibility targets
    public const string EfcptGenerateModels = nameof(EfcptGenerateModels);
    public const string BeforeEfcptGeneration = nameof(BeforeEfcptGeneration);
    public const string AfterEfcptGeneration = nameof(AfterEfcptGeneration);
    public const string BeforeSqlProjGeneration = nameof(BeforeSqlProjGeneration);
    public const string AfterSqlProjGeneration = nameof(AfterSqlProjGeneration);
    
    // Internal targets (prefixed with _Efcpt)
    public const string _EfcptDetectSqlProject = nameof(_EfcptDetectSqlProject);
    public const string _EfcptLogTaskAssemblyInfo = nameof(_EfcptLogTaskAssemblyInfo);
    public const string _EfcptInitializeProfiling = nameof(_EfcptInitializeProfiling);
    public const string _EfcptCheckForUpdates = nameof(_EfcptCheckForUpdates);
    
    // Pipeline targets
    public const string EfcptResolveInputs = nameof(EfcptResolveInputs);
    public const string EfcptResolveInputsForDirectDacpac = nameof(EfcptResolveInputsForDirectDacpac);
    public const string EfcptStageInputs = nameof(EfcptStageInputs);
    public const string EfcptSerializeConfigProperties = nameof(EfcptSerializeConfigProperties);
    public const string EfcptApplyConfigOverrides = nameof(EfcptApplyConfigOverrides);
    public const string EfcptResolveSqlProjAndInputs = nameof(EfcptResolveSqlProjAndInputs);
    public const string EfcptEnsureDacpacBuilt = nameof(EfcptEnsureDacpacBuilt);
    public const string EfcptRunEfcpt = nameof(EfcptRunEfcpt);
    public const string EfcptRenameGeneratedFiles = nameof(EfcptRenameGeneratedFiles);
    public const string EfcptAddSqlFileWarnings = nameof(EfcptAddSqlFileWarnings);
    public const string EfcptQueryDatabaseSchemaForSqlProj = nameof(EfcptQueryDatabaseSchemaForSqlProj);
    public const string EfcptGenerateSqlFilesFromMetadata = nameof(EfcptGenerateSqlFilesFromMetadata);
    public const string EfcptRunSqlPackageToGenerateSqlFiles = nameof(EfcptRunSqlPackageToGenerateSqlFiles);
    public const string _EfcptFinalizeProfiling = nameof(_EfcptFinalizeProfiling);
}

/// <summary>
/// Well-known MSBuild item names.
/// </summary>
public static class MsBuildItems
{
    public const string Compile = nameof(Compile);
    public const string None = nameof(None);
    public const string Content = nameof(Content);
    public const string Reference = nameof(Reference);
    public const string ProjectReference = nameof(ProjectReference);
    public const string PackageReference = nameof(PackageReference);
}

/// <summary>
/// Efcpt-specific MSBuild item names.
/// </summary>
public static class EfcptItems
{
    public const string EfcptInputs = nameof(EfcptInputs);
    public const string EfcptGeneratedFiles = nameof(EfcptGeneratedFiles);
    public const string EfcptSqlFiles = nameof(EfcptSqlFiles);
}

/// <summary>
/// Well-known MSBuild item metadata names.
/// </summary>
public static class ItemMetadata
{
    public const string Link = nameof(Link);
    public const string CopyToOutputDirectory = nameof(CopyToOutputDirectory);
    public const string Visible = nameof(Visible);
    public const string DependentUpon = nameof(DependentUpon);
}

/// <summary>
/// Well-known MSBuild task names.
/// </summary>
public static class MsBuildTasks
{
    public const string Message = nameof(Message);
    public const string Warning = nameof(Warning);
    public const string Error = nameof(Error);
    public const string MakeDir = nameof(MakeDir);
    public const string Copy = nameof(Copy);
    public const string Delete = nameof(Delete);
    public const string Touch = nameof(Touch);
    public const string Exec = nameof(Exec);
}

/// <summary>
/// Task parameter names used across multiple tasks.
/// </summary>
public static class TaskParameters
{
    // Common input parameters
    public const string ProjectPath = nameof(ProjectPath);
    public const string ConfigPath = nameof(ConfigPath);
    public const string RenamingPath = nameof(RenamingPath);
    public const string TemplateDir = nameof(TemplateDir);
    public const string OutputDir = nameof(OutputDir);
    public const string Provider = nameof(Provider);
    public const string ConnectionString = nameof(ConnectionString);
    public const string DacpacPath = nameof(DacpacPath);
    public const string SqlProjPath = nameof(SqlProjPath);
    public const string LogVerbosity = nameof(LogVerbosity);
    
    // Output parameters
    public const string IsSqlProject = nameof(IsSqlProject);
    public const string ResolvedSqlProjPath = nameof(ResolvedSqlProjPath);
    public const string SqlProjInputs = nameof(SqlProjInputs);
    public const string ResolvedDacpacPath = nameof(ResolvedDacpacPath);
    public const string Fingerprint = nameof(Fingerprint);
    public const string DbContextName = nameof(DbContextName);
    
    // Other common parameters
    public const string Directories = nameof(Directories);
    public const string Files = nameof(Files);
    public const string SourceDir = nameof(SourceDir);
    public const string DestDir = nameof(DestDir);
}

/// <summary>
/// Property values and literals.
/// </summary>
public static class PropertyValues
{
    public const string True = "true";
    public const string False = "false";
    public const string Enable = "enable";
    public const string Enable_Capitalized = "Enable";
    public const string High = "high";
    public const string Normal = "normal";
    public const string Detailed = "detailed";
    public const string Core = "Core";
    public const string PreserveNewest = "PreserveNewest";
}
