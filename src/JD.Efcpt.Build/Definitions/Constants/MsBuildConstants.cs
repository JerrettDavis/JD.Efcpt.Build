namespace JD.Efcpt.Build.Definitions.Constants;

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
    public const string MSBuildThisFileDirectory = nameof(MSBuildThisFileDirectory);
    public const string MSBuildBinPath = nameof(MSBuildBinPath);
    public const string Configuration = nameof(Configuration);
    public const string TargetFramework = nameof(TargetFramework);
    public const string OutputPath = nameof(OutputPath);
    public const string IntermediateOutputPath = nameof(IntermediateOutputPath);
    public const string RootNamespace = nameof(RootNamespace);
    public const string SolutionDir = nameof(SolutionDir);
    public const string SolutionPath = nameof(SolutionPath);
    public const string BaseIntermediateOutputPath = nameof(BaseIntermediateOutputPath);
    
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
    
    // Output and path properties
    public const string EfcptOutput = nameof(EfcptOutput);
    public const string EfcptGeneratedDir = nameof(EfcptGeneratedDir);
    public const string EfcptStampFile = nameof(EfcptStampFile);
    public const string EfcptSqlScriptsDir = nameof(EfcptSqlScriptsDir);
    public const string EfcptAppSettings = nameof(EfcptAppSettings);
    public const string EfcptAppConfig = nameof(EfcptAppConfig);
    public const string EfcptDacpac = nameof(EfcptDacpac);
    public const string EfcptDataProject = nameof(EfcptDataProject);
    public const string EfcptSplitOutputs = nameof(EfcptSplitOutputs);
    public const string EfcptExternalDataDir = nameof(EfcptExternalDataDir);
    public const string EfcptDataProjectOutputSubdir = nameof(EfcptDataProjectOutputSubdir);
    
    // Tool configuration properties
    public const string EfcptToolCommand = nameof(EfcptToolCommand);
    public const string EfcptToolPath = nameof(EfcptToolPath);
    public const string EfcptToolRestore = nameof(EfcptToolRestore);
    public const string EfcptToolPackageId = nameof(EfcptToolPackageId);
    public const string EfcptToolMode = nameof(EfcptToolMode);
    public const string EfcptToolVersion = nameof(EfcptToolVersion);
    public const string EfcptSqlPackageToolVersion = nameof(EfcptSqlPackageToolVersion);
    public const string EfcptSqlPackageToolPath = nameof(EfcptSqlPackageToolPath);
    public const string EfcptSqlPackageToolRestore = nameof(EfcptSqlPackageToolRestore);
    
    // Path override properties
    public const string EfcptConfig = nameof(EfcptConfig);
    public const string EfcptRenaming = nameof(EfcptRenaming);
    public const string EfcptSolutionDir = nameof(EfcptSolutionDir);
    public const string EfcptSolutionPath = nameof(EfcptSolutionPath);
    public const string EfcptProbeSolutionDir = nameof(EfcptProbeSolutionDir);
    
    // Update check properties
    public const string EfcptUpdateCheckCacheHours = nameof(EfcptUpdateCheckCacheHours);
    public const string EfcptForceUpdateCheck = nameof(EfcptForceUpdateCheck);
    public const string EfcptSdkVersion = nameof(EfcptSdkVersion);
    public const string EfcptSdkVersionWarningLevel = nameof(EfcptSdkVersionWarningLevel);
    public const string EfcptAutoDetectWarningLevel = nameof(EfcptAutoDetectWarningLevel);
    
    // Execution environment properties
    public const string EfcptDotNetExe = nameof(EfcptDotNetExe);
    public const string EfcptDetectGeneratedFileChanges = nameof(EfcptDetectGeneratedFileChanges);
    public const string EfcptDumpResolvedInputs = nameof(EfcptDumpResolvedInputs);
    public const string EfcptApplyMsBuildOverrides = nameof(EfcptApplyMsBuildOverrides);
    public const string EfcptConnectionStringName = nameof(EfcptConnectionStringName);
    public const string EfcptProfilingOutput = nameof(EfcptProfilingOutput);
    public const string EfcptFingerprintFile = nameof(EfcptFingerprintFile);
    
    // Config option properties
    public const string EfcptConfigRootNamespace = nameof(EfcptConfigRootNamespace);
    public const string EfcptConfigDbContextName = nameof(EfcptConfigDbContextName);
    public const string EfcptConfigDbContextNamespace = nameof(EfcptConfigDbContextNamespace);
    public const string EfcptConfigDbContextOutputPath = nameof(EfcptConfigDbContextOutputPath);
    public const string EfcptConfigOutputPath = nameof(EfcptConfigOutputPath);
    public const string EfcptConfigModelNamespace = nameof(EfcptConfigModelNamespace);
    public const string EfcptConfigGenerationType = nameof(EfcptConfigGenerationType);
    public const string EfcptConfigSplitDbContext = nameof(EfcptConfigSplitDbContext);
    public const string EfcptConfigT4TemplatePath = nameof(EfcptConfigT4TemplatePath);
    public const string EfcptConfigUseDatabaseNames = nameof(EfcptConfigUseDatabaseNames);
    public const string EfcptConfigUseDataAnnotations = nameof(EfcptConfigUseDataAnnotations);
    public const string EfcptConfigUseNullableReferenceTypes = nameof(EfcptConfigUseNullableReferenceTypes);
    public const string EfcptConfigUseDateOnlyTimeOnly = nameof(EfcptConfigUseDateOnlyTimeOnly);
    public const string EfcptConfigUseDecimalAnnotationForSprocs = nameof(EfcptConfigUseDecimalAnnotationForSprocs);
    public const string EfcptConfigUseHierarchyId = nameof(EfcptConfigUseHierarchyId);
    public const string EfcptConfigUseInflector = nameof(EfcptConfigUseInflector);
    public const string EfcptConfigUseLegacyInflector = nameof(EfcptConfigUseLegacyInflector);
    public const string EfcptConfigUseNodaTime = nameof(EfcptConfigUseNodaTime);
    public const string EfcptConfigUseNoNavigations = nameof(EfcptConfigUseNoNavigations);
    public const string EfcptConfigUsePrefixNavigationNaming = nameof(EfcptConfigUsePrefixNavigationNaming);
    public const string EfcptConfigUseSpatial = nameof(EfcptConfigUseSpatial);
    public const string EfcptConfigUseSchemaFolders = nameof(EfcptConfigUseSchemaFolders);
    public const string EfcptConfigUseSchemaNamespaces = nameof(EfcptConfigUseSchemaNamespaces);
    public const string EfcptConfigUseManyToManyEntity = nameof(EfcptConfigUseManyToManyEntity);
    public const string EfcptConfigUseT4 = nameof(EfcptConfigUseT4);
    public const string EfcptConfigUseT4Split = nameof(EfcptConfigUseT4Split);
    public const string EfcptConfigRemoveDefaultSqlFromBool = nameof(EfcptConfigRemoveDefaultSqlFromBool);
    public const string EfcptConfigUseDatabaseNamesForRoutines = nameof(EfcptConfigUseDatabaseNamesForRoutines);
    public const string EfcptConfigUseInternalAccessForRoutines = nameof(EfcptConfigUseInternalAccessForRoutines);
    public const string EfcptConfigUseAlternateResultSetDiscovery = nameof(EfcptConfigUseAlternateResultSetDiscovery);
    public const string EfcptConfigDiscoverMultipleResultSets = nameof(EfcptConfigDiscoverMultipleResultSets);
    public const string EfcptConfigRefreshObjectLists = nameof(EfcptConfigRefreshObjectLists);
    public const string EfcptConfigMergeDacpacs = nameof(EfcptConfigMergeDacpacs);
    public const string EfcptConfigEnableOnConfiguring = nameof(EfcptConfigEnableOnConfiguring);
    public const string EfcptConfigPreserveCasingWithRegex = nameof(EfcptConfigPreserveCasingWithRegex);
    public const string EfcptConfigGenerateMermaidDiagram = nameof(EfcptConfigGenerateMermaidDiagram);
    public const string EfcptConfigSoftDeleteObsoleteFiles = nameof(EfcptConfigSoftDeleteObsoleteFiles);
    
    // SQL Project properties
    public const string EfcptSqlProj = nameof(EfcptSqlProj);
    public const string EfcptSqlProjOutputDir = nameof(EfcptSqlProjOutputDir);
    public const string EfcptBuildSqlProj = nameof(EfcptBuildSqlProj);
    public const string EfcptSqlProjType = nameof(EfcptSqlProjType);
    public const string EfcptSqlProjLanguage = nameof(EfcptSqlProjLanguage);
    public const string EfcptSqlServerVersion = nameof(EfcptSqlServerVersion);
    public const string EfcptProfilingVerbosity = nameof(EfcptProfilingVerbosity);
    
    // Direct DACPAC properties
    public const string EfcptDacpacPath = nameof(EfcptDacpacPath);
    
    // Internal resolved properties (prefixed with _)
    public const string _EfcptIsSqlProject = nameof(_EfcptIsSqlProject);
    public const string _EfcptIsDirectReference = nameof(_EfcptIsDirectReference);
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
    public const string _EfcptFingerprintChanged = nameof(_EfcptFingerprintChanged);
    public const string _EfcptSerializedConfigProperties = nameof(_EfcptSerializedConfigProperties);
    public const string _EfcptExtractedScriptsPath = nameof(_EfcptExtractedScriptsPath);
    public const string _EfcptGeneratedScripts = nameof(_EfcptGeneratedScripts);
    public const string _EfcptLatestVersion = nameof(_EfcptLatestVersion);
    public const string _EfcptUpdateAvailable = nameof(_EfcptUpdateAvailable);
    public const string _EfcptCopiedDataFiles = nameof(_EfcptCopiedDataFiles);
    public const string _EfcptHasFilesToCopy = nameof(_EfcptHasFilesToCopy);
    public const string _EfcptUseConnectionString = nameof(_EfcptUseConnectionString);
    public const string _EfcptUseDirectDacpac = nameof(_EfcptUseDirectDacpac);
    public const string _EfcptDataProjectPath = nameof(_EfcptDataProjectPath);
    public const string _EfcptDataProjectDir = nameof(_EfcptDataProjectDir);
    public const string _EfcptDataDestDir = nameof(_EfcptDataDestDir);
    public const string _EfcptScriptsDir = nameof(_EfcptScriptsDir);
    public const string _EfcptSchemaFingerprint = nameof(_EfcptSchemaFingerprint);
    public const string _EfcptIsUsingDefaultConfig = nameof(_EfcptIsUsingDefaultConfig);
    public const string _EfcptConfigurationFiles = nameof(_EfcptConfigurationFiles);
    public const string _EfcptDbContextFiles = nameof(_EfcptDbContextFiles);
    public const string _EfcptStagedConfig = nameof(_EfcptStagedConfig);
    public const string _EfcptStagedRenaming = nameof(_EfcptStagedRenaming);
    public const string _EfcptStagedTemplateDir = nameof(_EfcptStagedTemplateDir);
    public const string _EfcptResolvedConnectionString = nameof(_EfcptResolvedConnectionString);
    public const string _EfcptResolvedDbContextName = nameof(_EfcptResolvedDbContextName);
    public const string _EfcptDatabaseName = nameof(_EfcptDatabaseName);
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
    public const string EfcptExtractDatabaseSchemaToScripts = nameof(EfcptExtractDatabaseSchemaToScripts);
    public const string EfcptCopyFilesToDataProject = nameof(EfcptCopyFilesToDataProject);
    public const string EfcptQuerySchemaMetadataForDb = nameof(EfcptQuerySchemaMetadataForDb);
    public const string EfcptUseDirectDacpac = nameof(EfcptUseDirectDacpac);
    public const string EfcptBuildSqlProj = nameof(EfcptBuildSqlProj);
    public const string EfcptResolveDbContextName = nameof(EfcptResolveDbContextName);
    public const string EfcptComputeFingerprint = nameof(EfcptComputeFingerprint);
    public const string EfcptEnsureDacpac = nameof(EfcptEnsureDacpac);
    public const string EfcptAddToCompile = nameof(EfcptAddToCompile);
    public const string EfcptCopyDataToDataProject = nameof(EfcptCopyDataToDataProject);
    public const string EfcptValidateSplitOutputs = nameof(EfcptValidateSplitOutputs);
    public const string EfcptIncludeExternalData = nameof(EfcptIncludeExternalData);
    public const string EfcptClean = nameof(EfcptClean);
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
    public const string WriteLinesToFile = nameof(WriteLinesToFile);
    public const string RemoveDir = nameof(RemoveDir);
    public const string MSBuild = nameof(MSBuild);
}

/// <summary>
/// Efcpt-specific task names.
/// </summary>
public static class EfcptTasks
{
    public const string DetectSqlProject = nameof(DetectSqlProject);
    public const string InitializeBuildProfiling = nameof(InitializeBuildProfiling);
    public const string CheckSdkVersion = nameof(CheckSdkVersion);
    public const string QuerySchemaMetadata = nameof(QuerySchemaMetadata);
    public const string GenerateSqlScripts = nameof(GenerateSqlScripts);
    public const string ExtractSqlScriptsFromDacpac = nameof(ExtractSqlScriptsFromDacpac);
    public const string ResolveInputPaths = nameof(ResolveInputPaths);
    public const string StageInputFiles = nameof(StageInputFiles);
    public const string SerializeConfigProperties = nameof(SerializeConfigProperties);
    public const string ApplyConfigOverrides = nameof(ApplyConfigOverrides);
    public const string ResolveSqlProjPath = nameof(ResolveSqlProjPath);
    public const string BuildSqlProject = nameof(BuildSqlProject);
    public const string RunEfcpt = nameof(RunEfcpt);
    public const string RenameFilesFromJson = nameof(RenameFilesFromJson);
    public const string GenerateCompileWarnings = nameof(GenerateCompileWarnings);
    public const string DetectFileChanges = nameof(DetectFileChanges);
    public const string ComputeFingerprint = nameof(ComputeFingerprint);
    public const string CopyFilesToProject = nameof(CopyFilesToProject);
    public const string FinalizeBuildProfiling = nameof(FinalizeBuildProfiling);
    public const string RunSqlPackage = nameof(RunSqlPackage);
    public const string AddSqlFileWarnings = nameof(AddSqlFileWarnings);
    public const string ResolveSqlProjAndInputs = nameof(ResolveSqlProjAndInputs);
    public const string EnsureDacpacBuilt = nameof(EnsureDacpacBuilt);
    public const string StageEfcptInputs = nameof(StageEfcptInputs);
    public const string RenameGeneratedFiles = nameof(RenameGeneratedFiles);
    public const string ResolveDbContextName = nameof(ResolveDbContextName);
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
    
    // Profiling and diagnostic parameters
    public const string EnableProfiling = nameof(EnableProfiling);
    public const string ProfilingOutput = nameof(ProfilingOutput);
    
    // Staging parameters
    public const string StagedConfigPath = nameof(StagedConfigPath);
    public const string StagedRenamingPath = nameof(StagedRenamingPath);
    public const string StagedTemplateDir = nameof(StagedTemplateDir);
    public const string SerializedProperties = nameof(SerializedProperties);
    
    // Configuration override parameters
    public const string ApplyOverrides = nameof(ApplyOverrides);
    public const string ConfigPropertyOverrides = nameof(ConfigPropertyOverrides);
    public const string ConfigOverride = nameof(ConfigOverride);
    
    // Fingerprint parameters
    public const string HasChanged = nameof(HasChanged);
    public const string FingerprintFile = nameof(FingerprintFile);
    public const string SchemaFingerprint = nameof(SchemaFingerprint);
    
    // Connection parameters
    public const string UseConnectionStringMode = nameof(UseConnectionStringMode);
    
    // Tool parameters
    public const string ToolCommand = nameof(ToolCommand);
    public const string ToolPath = nameof(ToolPath);
    public const string ToolRestore = nameof(ToolRestore);
    public const string ToolPackageId = nameof(ToolPackageId);
    public const string ToolMode = nameof(ToolMode);
    public const string ToolVersion = nameof(ToolVersion);
    public const string DotNetExe = nameof(DotNetExe);
    public const string MsBuildExe = nameof(MsBuildExe);
    
    // Update check parameters
    public const string CurrentVersion = nameof(CurrentVersion);
    public const string LatestVersion = nameof(LatestVersion);
    public const string CacheHours = nameof(CacheHours);
    public const string ForceCheck = nameof(ForceCheck);
    public const string UpdateAvailable = nameof(UpdateAvailable);
    public const string WarningLevel = nameof(WarningLevel);
    public const string PackageId = nameof(PackageId);
    public const string AutoDetectWarningLevel = nameof(AutoDetectWarningLevel);
    
    // Detection and analysis parameters
    public const string DetectGeneratedFileChanges = nameof(DetectGeneratedFileChanges);
    public const string DumpResolvedInputs = nameof(DumpResolvedInputs);
    
    // Resolution parameters
    public const string ResolvedConnectionString = nameof(ResolvedConnectionString);
    public const string ResolvedConfigPath = nameof(ResolvedConfigPath);
    public const string ResolvedRenamingPath = nameof(ResolvedRenamingPath);
    public const string ResolvedTemplateDir = nameof(ResolvedTemplateDir);
    public const string ResolvedDbContextName = nameof(ResolvedDbContextName);
    public const string ExplicitDbContextName = nameof(ExplicitDbContextName);
    
    // Database schema parameters
    public const string DatabaseName = nameof(DatabaseName);
    public const string SchemaObjectType = nameof(SchemaObjectType);
    public const string ScriptsDirectory = nameof(ScriptsDirectory);
    
    // SQL extraction parameters
    public const string ExtractedPath = nameof(ExtractedPath);
    public const string ExtractTarget = nameof(ExtractTarget);
    public const string SqlServerVersion = nameof(SqlServerVersion);
    public const string DSP = nameof(DSP);
    
    // File operation parameters
    public const string Lines = nameof(Lines);
    public const string Overwrite = nameof(Overwrite);
    
    // Path resolution parameters
    public const string ProbeSolutionDir = nameof(ProbeSolutionDir);
    public const string SolutionDir = nameof(SolutionDir);
    public const string SolutionPath = nameof(SolutionPath);
    public const string DefaultsRoot = nameof(DefaultsRoot);
    public const string TemplateOutputDir = nameof(TemplateOutputDir);
    public const string DestinationFolder = nameof(DestinationFolder);
    public const string SourceFiles = nameof(SourceFiles);
    public const string CopiedFiles = nameof(CopiedFiles);
    
    // Configuration metadata parameters
    public const string IsUsingDefaultConfig = nameof(IsUsingDefaultConfig);
    
    // Build parameters
    public const string BuildSucceeded = nameof(BuildSucceeded);
    public const string BuildInParallel = nameof(BuildInParallel);
    public const string WorkingDirectory = nameof(WorkingDirectory);
    public const string TargetDirectory = nameof(TargetDirectory);
    
    // Common parameter names
    public const string ProjectName = nameof(ProjectName);
    public const string TargetFramework = nameof(TargetFramework);
    public const string Configuration = nameof(Configuration);
    public const string ProjectFullPath = nameof(ProjectFullPath);
    public const string ProjectDirectory = nameof(ProjectDirectory);
    public const string ProjectReferences = nameof(ProjectReferences);
    public const string SqlProjOverride = nameof(SqlProjOverride);
    public const string RenamingOverride = nameof(RenamingOverride);
    public const string TemplateDirOverride = nameof(TemplateDirOverride);
    public const string EfcptConnectionString = nameof(EfcptConnectionString);
    public const string EfcptAppSettings = nameof(EfcptAppSettings);
    public const string EfcptAppConfig = nameof(EfcptAppConfig);
    public const string EfcptConnectionStringName = nameof(EfcptConnectionStringName);
    public const string SqlProjectPath = nameof(SqlProjectPath);
    public const string GeneratedDir = nameof(GeneratedDir);
    public const string OutputPath = nameof(OutputPath);
    
    // Config property override parameters
    public const string RootNamespace = nameof(RootNamespace);
    public const string DbContextName = nameof(DbContextName);
    public const string DbContextNamespace = nameof(DbContextNamespace);
    public const string ModelNamespace = nameof(ModelNamespace);
    public const string DbContextOutputPath = nameof(DbContextOutputPath);
    public const string SplitDbContext = nameof(SplitDbContext);
    public const string UseSchemaFolders = nameof(UseSchemaFolders);
    public const string UseSchemaNamespaces = nameof(UseSchemaNamespaces);
    public const string EnableOnConfiguring = nameof(EnableOnConfiguring);
    public const string GenerationType = nameof(GenerationType);
    public const string UseDatabaseNames = nameof(UseDatabaseNames);
    public const string UseDataAnnotations = nameof(UseDataAnnotations);
    public const string UseNullableReferenceTypes = nameof(UseNullableReferenceTypes);
    public const string UseInflector = nameof(UseInflector);
    public const string UseLegacyInflector = nameof(UseLegacyInflector);
    public const string UseManyToManyEntity = nameof(UseManyToManyEntity);
    public const string UseT4 = nameof(UseT4);
    public const string UseT4Split = nameof(UseT4Split);
    public const string RemoveDefaultSqlFromBool = nameof(RemoveDefaultSqlFromBool);
    public const string SoftDeleteObsoleteFiles = nameof(SoftDeleteObsoleteFiles);
    public const string DiscoverMultipleResultSets = nameof(DiscoverMultipleResultSets);
    public const string UseAlternateResultSetDiscovery = nameof(UseAlternateResultSetDiscovery);
    public const string T4TemplatePath = nameof(T4TemplatePath);
    public const string UseNoNavigations = nameof(UseNoNavigations);
    public const string MergeDacpacs = nameof(MergeDacpacs);
    public const string RefreshObjectLists = nameof(RefreshObjectLists);
    public const string GenerateMermaidDiagram = nameof(GenerateMermaidDiagram);
    public const string UseDecimalAnnotationForSprocs = nameof(UseDecimalAnnotationForSprocs);
    public const string UsePrefixNavigationNaming = nameof(UsePrefixNavigationNaming);
    public const string UseDatabaseNamesForRoutines = nameof(UseDatabaseNamesForRoutines);
    public const string UseInternalAccessForRoutines = nameof(UseInternalAccessForRoutines);
    public const string UseDateOnlyTimeOnly = nameof(UseDateOnlyTimeOnly);
    public const string UseHierarchyId = nameof(UseHierarchyId);
    public const string UseSpatial = nameof(UseSpatial);
    public const string UseNodaTime = nameof(UseNodaTime);
    public const string PreserveCasingWithRegex = nameof(PreserveCasingWithRegex);
    
    // Output parameters
    public const string IsSqlProject = nameof(IsSqlProject);
    public const string ResolvedSqlProjPath = nameof(ResolvedSqlProjPath);
    public const string SqlProjInputs = nameof(SqlProjInputs);
    public const string ResolvedDacpacPath = nameof(ResolvedDacpacPath);
    public const string Fingerprint = nameof(Fingerprint);
    
    // Other common parameters
    public const string Directories = nameof(Directories);
    public const string Files = nameof(Files);
    public const string SourceDir = nameof(SourceDir);
    public const string DestDir = nameof(DestDir);
    public const string File = nameof(File);
    public const string Projects = nameof(Projects);
}

/// <summary>
/// Property values and literals.
/// </summary>
public static class PropertyValues
{
    // Boolean values
    public const string True = "true";
    public const string False = "false";
    
    // Enable values
    public const string Enable = "enable";
    public const string Enable_Capitalized = "Enable";
    
    // Importance/Verbosity levels
    public const string High = "high";
    public const string Normal = "normal";
    public const string Detailed = "detailed";
    public const string Minimal = "minimal";
    
    // Runtime types
    public const string Core = "Core";
    
    // Copy behavior
    public const string PreserveNewest = "PreserveNewest";
    
    // Build configuration
    public const string Configuration = "Configuration=$(Configuration)";
    
    // Package identifiers
    public const string JD_Efcpt_Sdk = "JD.Efcpt.Sdk";
    public const string ErikEJ_EFCorePowerTools_Cli = "ErikEJ.EFCorePowerTools.Cli";
    
    // Folder/File names
    public const string Targets = "Targets";
    public const string MSBuild = "MSBuild";
    public const string Defaults = "Defaults";
    public const string EfcptConfigJson = "efcpt-config.json";
    public const string EfcptRenamingJson = "efcpt.renaming.json";
    public const string Template = "Template";
    public const string MsBuildExe = "msbuild.exe";
    public const string Tasks = "tasks";
    
    // Framework versions
    public const string Net10_0 = "net10.0";
    public const string Net9_0 = "net9.0";
    public const string Net8_0 = "net8.0";
    public const string Net472 = "net472";
    
    // MSBuild version numbers
    public const string MsBuildVersion_18_0 = "18.0";
    public const string MsBuildVersion_17_14 = "17.14";
    public const string MsBuildVersion_17_12 = "17.12";
    
    // Provider types
    public const string Mssql = "mssql";
    
    // SQL Project types
    public const string MicrosoftBuildSql = "microsoft-build-sql";
    public const string CSharp = "csharp";
    
    // SQL Server versions
    public const string Sql160 = "Sql160";
    
    // Tool modes
    public const string Auto = "auto";
    
    // Tool commands
    public const string Efcpt = "efcpt";
    public const string Dotnet = "dotnet";
    
    // Extract target types
    public const string SchemaObjectType = "SchemaObjectType";
    
    // Version patterns
    public const string Version_10_Wildcard = "10.*";
    
    // Connection string names
    public const string DefaultConnection = "DefaultConnection";
    
    // Warning levels
    public const string Info = "Info";
    public const string Warn = "Warn";
    
    // Default cache hours
    public const string CacheHours_24 = "24";
    
    // Empty string
    public const string Empty = "";
}

/// <summary>
/// Path patterns and relative paths used in MSBuild files.
/// </summary>
public static class PathPatterns
{
    // BuildTransitive imports
    public const string BuildTransitive_Props = "buildTransitive\\JD.Efcpt.Build.props";
    public const string BuildTransitive_Props_Fallback = "..\\buildTransitive\\JD.Efcpt.Build.props";
    public const string BuildTransitive_Targets = "buildTransitive\\JD.Efcpt.Build.targets";
    public const string BuildTransitive_Targets_Fallback = "..\\buildTransitive\\JD.Efcpt.Build.targets";
    
    // Task assembly paths
    public const string Tasks_RelativePath = "..\\tasks";
    public const string TaskAssembly_Name = "JD.Efcpt.Build.Tasks.dll";
    public const string TaskAssembly_LocalBuild = "..\\..\\JD.Efcpt.Build.Tasks\\bin";
    public const string TaskAssembly_Debug = "..\\..\\JD.Efcpt.Build.Tasks\\bin\\Debug";
    
    // Output paths
    public const string Output_Efcpt = "$(BaseIntermediateOutputPath)efcpt\\";
    public const string Output_Generated = "$(EfcptOutput)Generated\\";
    public const string Output_ObjEfcptGenerated = "obj\\efcpt\\Generated\\";
    public const string Output_Fingerprint = "$(EfcptOutput)fingerprint.txt";
    public const string Output_Stamp = "$(EfcptOutput).efcpt.stamp";
    public const string Output_BuildProfile = "$(EfcptOutput)build-profile.json";
    
    // SQL Project paths
    public const string SqlProj_OutputDir = "$(MSBuildProjectDirectory)\\";
    public const string SqlScripts_Dir = "$(MSBuildProjectDirectory)\\";
}

/// <summary>
/// Helper methods for constructing MSBuild expressions and conditions.
/// </summary>
public static class MsBuildExpressions
{
    /// <summary>
    /// Returns a property reference expression: $(propertyName)
    /// </summary>
    public static string Property(string name) => $"$({name})";
    
    /// <summary>
    /// Returns a condition that checks if a property is 'true': '$(propName)' == 'true'
    /// </summary>
    public static string Condition_IsTrue(string propName) => $"'$({propName})' == 'true'";
    
    /// <summary>
    /// Returns a condition that checks if a property is not 'true': '$(propName)' != 'true'
    /// </summary>
    public static string Condition_IsFalse(string propName) => $"'$({propName})' != 'true'";
    
    /// <summary>
    /// Returns a condition that checks if a property is not empty: '$(propName)' != ''
    /// </summary>
    public static string Condition_NotEmpty(string propName) => $"'$({propName})' != ''";
    
    /// <summary>
    /// Returns a condition that checks if a property is empty: '$(propName)' == ''
    /// </summary>
    public static string Condition_IsEmpty(string propName) => $"'$({propName})' == ''";
    
    /// <summary>
    /// Returns a condition that checks if a property equals a specific value: '$(propName)' == 'value'
    /// </summary>
    public static string Condition_Equals(string propName, string value) => $"'$({propName})' == '{value}'";
    
    /// <summary>
    /// Returns a condition that checks if a property does not equal a specific value: '$(propName)' != 'value'
    /// </summary>
    public static string Condition_NotEquals(string propName, string value) => $"'$({propName})' != '{value}'";
    
    /// <summary>
    /// Returns a condition that checks if a path exists: Exists('path')
    /// </summary>
    public static string Condition_Exists(string path) => $"Exists('{path}')";
    
    /// <summary>
    /// Returns a condition that checks if a path does not exist: !Exists('path')
    /// </summary>
    public static string Condition_NotExists(string path) => $"!Exists('{path}')";
    
    /// <summary>
    /// Returns an MSBuild version comparison: $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', 'version'))
    /// </summary>
    public static string Condition_VersionGreaterThanOrEquals(string version) => 
        $"$([MSBuild]::VersionGreaterThanOrEquals('$({MsBuildProperties.MSBuildVersion})', '{version}'))";
    
    /// <summary>
    /// Returns a complex condition combining MSBuildRuntimeType and version check
    /// </summary>
    public static string Condition_RuntimeTypeAndVersion(string runtimeType, string minVersion) =>
        $"'$({MsBuildProperties.MSBuildRuntimeType})' == '{runtimeType}' and {Condition_VersionGreaterThanOrEquals(minVersion)}";
    
    /// <summary>
    /// Returns an item list reference expression: @(itemName)
    /// </summary>
    public static string ItemList(string itemName) => $"@({itemName})";
    
    /// <summary>
    /// Returns a condition that checks if an item list is not empty: '@(itemName)' != ''
    /// </summary>
    public static string ItemList_NotEmpty(string itemName) => $"'@({itemName})' != ''";
    
    /// <summary>
    /// Returns a condition that checks if an item list is empty: '@(itemName)' == ''
    /// </summary>
    public static string ItemList_IsEmpty(string itemName) => $"'@({itemName})' == ''";
    
    /// <summary>
    /// Combines two conditions with AND: (condition1) and (condition2)
    /// </summary>
    public static string Condition_And(string condition1, string condition2) => $"({condition1}) and ({condition2})";
    
    /// <summary>
    /// Combines two conditions with OR: (condition1) or (condition2)
    /// </summary>
    public static string Condition_Or(string condition1, string condition2) => $"({condition1}) or ({condition2})";
    
    /// <summary>
    /// Returns a file existence check using System.IO.File::Exists
    /// </summary>
    public static string FileExists(string path) => $"$([System.IO.File]::Exists('{path}'))";
    
    /// <summary>
    /// Builds a path by combining components with MSBuild property references
    /// </summary>
    public static string Path_Combine(params string[] parts) => string.Join("\\", parts);
}
