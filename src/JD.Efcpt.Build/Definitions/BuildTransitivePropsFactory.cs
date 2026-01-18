using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build;

/// <summary>
/// MSBuild package definition scaffolded from JD.Efcpt.Build.xml
/// </summary>
public static class BuildTransitivePropsFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("JD.Efcpt.Build")
            .Props(p =>
            {
                p.PropertyGroup(null, group =>
                {
                    group.Property<EfcptEnabled>( "true", "'$(EfcptEnabled)'==''");
                    group.Property<EfcptOutput>( "$(BaseIntermediateOutputPath)efcpt\\", "'$(EfcptOutput)'==''");
                    group.Property<EfcptGeneratedDir>( "$(EfcptOutput)Generated\\", "'$(EfcptGeneratedDir)'==''");
                    group.Property<EfcptSqlProj>( "", "'$(EfcptSqlProj)'==''");
                    group.Property<EfcptDacpac>( "", "'$(EfcptDacpac)'==''");
                    group.Property<EfcptConfig>( "efcpt-config.json", "'$(EfcptConfig)'==''");
                    group.Property<EfcptRenaming>( "efcpt.renaming.json", "'$(EfcptRenaming)'==''");
                    group.Property<EfcptTemplateDir>( "Template", "'$(EfcptTemplateDir)'==''");
                    group.Property<EfcptConnectionString>( "", "'$(EfcptConnectionString)'==''");
                    group.Property<EfcptAppSettings>( "", "'$(EfcptAppSettings)'==''");
                    group.Property<EfcptAppConfig>( "", "'$(EfcptAppConfig)'==''");
                    group.Property<EfcptConnectionStringName>( "DefaultConnection", "'$(EfcptConnectionStringName)'==''");
                    group.Property<EfcptProvider>( "mssql", "'$(EfcptProvider)'==''");
                    group.Property<EfcptSolutionDir>( "$(SolutionDir)", "'$(EfcptSolutionDir)'==''");
                    group.Property<EfcptSolutionPath>( "$(SolutionPath)", "'$(EfcptSolutionPath)'==''");
                    group.Property<EfcptProbeSolutionDir>( "true", "'$(EfcptProbeSolutionDir)'==''");
                    group.Property<EfcptToolMode>( "auto", "'$(EfcptToolMode)'==''");
                    group.Property<EfcptToolPackageId>( "ErikEJ.EFCorePowerTools.Cli", "'$(EfcptToolPackageId)'==''");
                    group.Property<EfcptToolVersion>( "10.*", "'$(EfcptToolVersion)'==''");
                    group.Property<EfcptToolRestore>( "true", "'$(EfcptToolRestore)'==''");
                    group.Property<EfcptToolCommand>( "efcpt", "'$(EfcptToolCommand)'==''");
                    group.Property<EfcptToolPath>( "", "'$(EfcptToolPath)'==''");
                    group.Property<EfcptDotNetExe>( "dotnet", "'$(EfcptDotNetExe)'==''");
                    group.Property<EfcptFingerprintFile>( "$(EfcptOutput)fingerprint.txt", "'$(EfcptFingerprintFile)'==''");
                    group.Property<EfcptStampFile>( "$(EfcptOutput).efcpt.stamp", "'$(EfcptStampFile)'==''");
                    group.Property<EfcptDetectGeneratedFileChanges>( "false", "'$(EfcptDetectGeneratedFileChanges)'==''");
                    group.Property<EfcptLogVerbosity>( "minimal", "'$(EfcptLogVerbosity)'==''");
                    group.Property<EfcptDumpResolvedInputs>( "false", "'$(EfcptDumpResolvedInputs)'==''");
                    group.Property<EfcptAutoDetectWarningLevel>( "Info", "'$(EfcptAutoDetectWarningLevel)'==''");
                    group.Property<EfcptSdkVersionWarningLevel>( "Warn", "'$(EfcptSdkVersionWarningLevel)'==''");
                    group.Property<EfcptCheckForUpdates>( "false", "'$(EfcptCheckForUpdates)'==''");
                    group.Property<EfcptUpdateCheckCacheHours>( "24", "'$(EfcptUpdateCheckCacheHours)'==''");
                    group.Property<EfcptForceUpdateCheck>( "false", "'$(EfcptForceUpdateCheck)'==''");
                    group.Property<EfcptSplitOutputs>( "false", "'$(EfcptSplitOutputs)'==''");
                    group.Property<EfcptDataProject>( "", "'$(EfcptDataProject)'==''");
                    group.Property<EfcptDataProjectOutputSubdir>( "obj\\efcpt\\Generated\\", "'$(EfcptDataProjectOutputSubdir)'==''");
                    group.Property<EfcptExternalDataDir>( "", "'$(EfcptExternalDataDir)'==''");
                    group.Property<EfcptApplyMsBuildOverrides>( "true", "'$(EfcptApplyMsBuildOverrides)'==''");
                    group.Property<EfcptConfigRootNamespace>( "$(RootNamespace)", "'$(EfcptConfigRootNamespace)'=='' and '$(RootNamespace)'!=''");
                    group.Property<EfcptConfigRootNamespace>( "$(MSBuildProjectName)", "'$(EfcptConfigRootNamespace)'==''");
                    group.Property<EfcptConfigDbContextName>( "", "'$(EfcptConfigDbContextName)'==''");
                    group.Property<EfcptConfigDbContextNamespace>( "", "'$(EfcptConfigDbContextNamespace)'==''");
                    group.Property<EfcptConfigModelNamespace>( "", "'$(EfcptConfigModelNamespace)'==''");
                    group.Property<EfcptConfigOutputPath>( "", "'$(EfcptConfigOutputPath)'==''");
                    group.Property<EfcptConfigDbContextOutputPath>( "", "'$(EfcptConfigDbContextOutputPath)'==''");
                    group.Property<EfcptConfigSplitDbContext>( "", "'$(EfcptConfigSplitDbContext)'==''");
                    group.Property<EfcptConfigUseSchemaFolders>( "", "'$(EfcptConfigUseSchemaFolders)'==''");
                    group.Property<EfcptConfigUseSchemaNamespaces>( "", "'$(EfcptConfigUseSchemaNamespaces)'==''");
                    group.Property<EfcptConfigEnableOnConfiguring>( "", "'$(EfcptConfigEnableOnConfiguring)'==''");
                    group.Property<EfcptConfigGenerationType>( "", "'$(EfcptConfigGenerationType)'==''");
                    group.Property<EfcptConfigUseDatabaseNames>( "", "'$(EfcptConfigUseDatabaseNames)'==''");
                    group.Property<EfcptConfigUseDataAnnotations>( "", "'$(EfcptConfigUseDataAnnotations)'==''");
                    group.Property<EfcptConfigUseInflector>( "", "'$(EfcptConfigUseInflector)'==''");
                    group.Property<EfcptConfigUseLegacyInflector>( "", "'$(EfcptConfigUseLegacyInflector)'==''");
                    group.Property<EfcptConfigUseManyToManyEntity>( "", "'$(EfcptConfigUseManyToManyEntity)'==''");
                    group.Property<EfcptConfigUseT4>( "", "'$(EfcptConfigUseT4)'==''");
                    group.Property<EfcptConfigUseT4Split>( "", "'$(EfcptConfigUseT4Split)'==''");
                    group.Property<EfcptConfigRemoveDefaultSqlFromBool>( "", "'$(EfcptConfigRemoveDefaultSqlFromBool)'==''");
                    group.Property<EfcptConfigSoftDeleteObsoleteFiles>( "", "'$(EfcptConfigSoftDeleteObsoleteFiles)'==''");
                    group.Property<EfcptConfigDiscoverMultipleResultSets>( "", "'$(EfcptConfigDiscoverMultipleResultSets)'==''");
                    group.Property<EfcptConfigUseAlternateResultSetDiscovery>( "", "'$(EfcptConfigUseAlternateResultSetDiscovery)'==''");
                    group.Property<EfcptConfigT4TemplatePath>( "", "'$(EfcptConfigT4TemplatePath)'==''");
                    group.Property<EfcptConfigUseNoNavigations>( "", "'$(EfcptConfigUseNoNavigations)'==''");
                    group.Property<EfcptConfigMergeDacpacs>( "", "'$(EfcptConfigMergeDacpacs)'==''");
                    group.Property<EfcptConfigRefreshObjectLists>( "", "'$(EfcptConfigRefreshObjectLists)'==''");
                    group.Property<EfcptConfigGenerateMermaidDiagram>( "", "'$(EfcptConfigGenerateMermaidDiagram)'==''");
                    group.Property<EfcptConfigUseDecimalAnnotationForSprocs>( "", "'$(EfcptConfigUseDecimalAnnotationForSprocs)'==''");
                    group.Property<EfcptConfigUsePrefixNavigationNaming>( "", "'$(EfcptConfigUsePrefixNavigationNaming)'==''");
                    group.Property<EfcptConfigUseDatabaseNamesForRoutines>( "", "'$(EfcptConfigUseDatabaseNamesForRoutines)'==''");
                    group.Property<EfcptConfigUseInternalAccessForRoutines>( "", "'$(EfcptConfigUseInternalAccessForRoutines)'==''");
                    group.Property<EfcptConfigUseDateOnlyTimeOnly>( "", "'$(EfcptConfigUseDateOnlyTimeOnly)'==''");
                    group.Property<EfcptConfigUseHierarchyId>( "", "'$(EfcptConfigUseHierarchyId)'==''");
                    group.Property<EfcptConfigUseSpatial>( "", "'$(EfcptConfigUseSpatial)'==''");
                    group.Property<EfcptConfigUseNodaTime>( "", "'$(EfcptConfigUseNodaTime)'==''");
                    group.Property<EfcptConfigPreserveCasingWithRegex>( "", "'$(EfcptConfigPreserveCasingWithRegex)'==''");
                    group.Property<EfcptEnableProfiling>( "false", "'$(EfcptEnableProfiling)'==''");
                    group.Property<EfcptProfilingOutput>( "$(EfcptOutput)build-profile.json", "'$(EfcptProfilingOutput)'==''");
                    group.Property<EfcptProfilingVerbosity>( "minimal", "'$(EfcptProfilingVerbosity)'==''");
                });
                p.PropertyGroup(null, group =>
                {
                    group.Property<EfcptSqlProjType>( "microsoft-build-sql", "'$(EfcptSqlProjType)'==''");
                    group.Property<EfcptSqlProjLanguage>( "csharp", "'$(EfcptSqlProjLanguage)'==''");
                    group.Property<EfcptSqlProjOutputDir>( "$(MSBuildProjectDirectory)\\", "'$(EfcptSqlProjOutputDir)'==''");
                    group.Property<EfcptSqlScriptsDir>( "$(MSBuildProjectDirectory)\\", "'$(EfcptSqlScriptsDir)'==''");
                    group.Property<EfcptSqlServerVersion>( "Sql160", "'$(EfcptSqlServerVersion)'==''");
                    group.Property<EfcptSqlPackageToolVersion>( "", "'$(EfcptSqlPackageToolVersion)'==''");
                    group.Property<EfcptSqlPackageToolRestore>( "true", "'$(EfcptSqlPackageToolRestore)'==''");
                    group.Property<EfcptSqlPackageToolPath>( "", "'$(EfcptSqlPackageToolPath)'==''");
                });
            })
            .Targets(t =>
            {
                t.PropertyGroup(null, group =>
                {
                    group.Property<EfcptEnabled>( "true", "'$(EfcptEnabled)'==''");
                    group.Property<EfcptOutput>( "$(BaseIntermediateOutputPath)efcpt\\", "'$(EfcptOutput)'==''");
                    group.Property<EfcptGeneratedDir>( "$(EfcptOutput)Generated\\", "'$(EfcptGeneratedDir)'==''");
                    group.Property<EfcptSqlProj>( "", "'$(EfcptSqlProj)'==''");
                    group.Property<EfcptDacpac>( "", "'$(EfcptDacpac)'==''");
                    group.Property<EfcptConfig>( "efcpt-config.json", "'$(EfcptConfig)'==''");
                    group.Property<EfcptRenaming>( "efcpt.renaming.json", "'$(EfcptRenaming)'==''");
                    group.Property<EfcptTemplateDir>( "Template", "'$(EfcptTemplateDir)'==''");
                    group.Property<EfcptConnectionString>( "", "'$(EfcptConnectionString)'==''");
                    group.Property<EfcptAppSettings>( "", "'$(EfcptAppSettings)'==''");
                    group.Property<EfcptAppConfig>( "", "'$(EfcptAppConfig)'==''");
                    group.Property<EfcptConnectionStringName>( "DefaultConnection", "'$(EfcptConnectionStringName)'==''");
                    group.Property<EfcptProvider>( "mssql", "'$(EfcptProvider)'==''");
                    group.Property<EfcptSolutionDir>( "$(SolutionDir)", "'$(EfcptSolutionDir)'==''");
                    group.Property<EfcptSolutionPath>( "$(SolutionPath)", "'$(EfcptSolutionPath)'==''");
                    group.Property<EfcptProbeSolutionDir>( "true", "'$(EfcptProbeSolutionDir)'==''");
                    group.Property<EfcptToolMode>( "auto", "'$(EfcptToolMode)'==''");
                    group.Property<EfcptToolPackageId>( "ErikEJ.EFCorePowerTools.Cli", "'$(EfcptToolPackageId)'==''");
                    group.Property<EfcptToolVersion>( "10.*", "'$(EfcptToolVersion)'==''");
                    group.Property<EfcptToolRestore>( "true", "'$(EfcptToolRestore)'==''");
                    group.Property<EfcptToolCommand>( "efcpt", "'$(EfcptToolCommand)'==''");
                    group.Property<EfcptToolPath>( "", "'$(EfcptToolPath)'==''");
                    group.Property<EfcptDotNetExe>( "dotnet", "'$(EfcptDotNetExe)'==''");
                    group.Property<EfcptFingerprintFile>( "$(EfcptOutput)fingerprint.txt", "'$(EfcptFingerprintFile)'==''");
                    group.Property<EfcptStampFile>( "$(EfcptOutput).efcpt.stamp", "'$(EfcptStampFile)'==''");
                    group.Property<EfcptDetectGeneratedFileChanges>( "false", "'$(EfcptDetectGeneratedFileChanges)'==''");
                    group.Property<EfcptLogVerbosity>( "minimal", "'$(EfcptLogVerbosity)'==''");
                    group.Property<EfcptDumpResolvedInputs>( "false", "'$(EfcptDumpResolvedInputs)'==''");
                    group.Property<EfcptAutoDetectWarningLevel>( "Info", "'$(EfcptAutoDetectWarningLevel)'==''");
                    group.Property<EfcptSdkVersionWarningLevel>( "Warn", "'$(EfcptSdkVersionWarningLevel)'==''");
                    group.Property<EfcptCheckForUpdates>( "false", "'$(EfcptCheckForUpdates)'==''");
                    group.Property<EfcptUpdateCheckCacheHours>( "24", "'$(EfcptUpdateCheckCacheHours)'==''");
                    group.Property<EfcptForceUpdateCheck>( "false", "'$(EfcptForceUpdateCheck)'==''");
                    group.Property<EfcptSplitOutputs>( "false", "'$(EfcptSplitOutputs)'==''");
                    group.Property<EfcptDataProject>( "", "'$(EfcptDataProject)'==''");
                    group.Property<EfcptDataProjectOutputSubdir>( "obj\\efcpt\\Generated\\", "'$(EfcptDataProjectOutputSubdir)'==''");
                    group.Property<EfcptExternalDataDir>( "", "'$(EfcptExternalDataDir)'==''");
                    group.Property<EfcptApplyMsBuildOverrides>( "true", "'$(EfcptApplyMsBuildOverrides)'==''");
                    group.Property<EfcptConfigRootNamespace>( "$(RootNamespace)", "'$(EfcptConfigRootNamespace)'=='' and '$(RootNamespace)'!=''");
                    group.Property<EfcptConfigRootNamespace>( "$(MSBuildProjectName)", "'$(EfcptConfigRootNamespace)'==''");
                    group.Property<EfcptConfigDbContextName>( "", "'$(EfcptConfigDbContextName)'==''");
                    group.Property<EfcptConfigDbContextNamespace>( "", "'$(EfcptConfigDbContextNamespace)'==''");
                    group.Property<EfcptConfigModelNamespace>( "", "'$(EfcptConfigModelNamespace)'==''");
                    group.Property<EfcptConfigOutputPath>( "", "'$(EfcptConfigOutputPath)'==''");
                    group.Property<EfcptConfigDbContextOutputPath>( "", "'$(EfcptConfigDbContextOutputPath)'==''");
                    group.Property<EfcptConfigSplitDbContext>( "", "'$(EfcptConfigSplitDbContext)'==''");
                    group.Property<EfcptConfigUseSchemaFolders>( "", "'$(EfcptConfigUseSchemaFolders)'==''");
                    group.Property<EfcptConfigUseSchemaNamespaces>( "", "'$(EfcptConfigUseSchemaNamespaces)'==''");
                    group.Property<EfcptConfigEnableOnConfiguring>( "", "'$(EfcptConfigEnableOnConfiguring)'==''");
                    group.Property<EfcptConfigGenerationType>( "", "'$(EfcptConfigGenerationType)'==''");
                    group.Property<EfcptConfigUseDatabaseNames>( "", "'$(EfcptConfigUseDatabaseNames)'==''");
                    group.Property<EfcptConfigUseDataAnnotations>( "", "'$(EfcptConfigUseDataAnnotations)'==''");
                    group.Property<EfcptConfigUseInflector>( "", "'$(EfcptConfigUseInflector)'==''");
                    group.Property<EfcptConfigUseLegacyInflector>( "", "'$(EfcptConfigUseLegacyInflector)'==''");
                    group.Property<EfcptConfigUseManyToManyEntity>( "", "'$(EfcptConfigUseManyToManyEntity)'==''");
                    group.Property<EfcptConfigUseT4>( "", "'$(EfcptConfigUseT4)'==''");
                    group.Property<EfcptConfigUseT4Split>( "", "'$(EfcptConfigUseT4Split)'==''");
                    group.Property<EfcptConfigRemoveDefaultSqlFromBool>( "", "'$(EfcptConfigRemoveDefaultSqlFromBool)'==''");
                    group.Property<EfcptConfigSoftDeleteObsoleteFiles>( "", "'$(EfcptConfigSoftDeleteObsoleteFiles)'==''");
                    group.Property<EfcptConfigDiscoverMultipleResultSets>( "", "'$(EfcptConfigDiscoverMultipleResultSets)'==''");
                    group.Property<EfcptConfigUseAlternateResultSetDiscovery>( "", "'$(EfcptConfigUseAlternateResultSetDiscovery)'==''");
                    group.Property<EfcptConfigT4TemplatePath>( "", "'$(EfcptConfigT4TemplatePath)'==''");
                    group.Property<EfcptConfigUseNoNavigations>( "", "'$(EfcptConfigUseNoNavigations)'==''");
                    group.Property<EfcptConfigMergeDacpacs>( "", "'$(EfcptConfigMergeDacpacs)'==''");
                    group.Property<EfcptConfigRefreshObjectLists>( "", "'$(EfcptConfigRefreshObjectLists)'==''");
                    group.Property<EfcptConfigGenerateMermaidDiagram>( "", "'$(EfcptConfigGenerateMermaidDiagram)'==''");
                    group.Property<EfcptConfigUseDecimalAnnotationForSprocs>( "", "'$(EfcptConfigUseDecimalAnnotationForSprocs)'==''");
                    group.Property<EfcptConfigUsePrefixNavigationNaming>( "", "'$(EfcptConfigUsePrefixNavigationNaming)'==''");
                    group.Property<EfcptConfigUseDatabaseNamesForRoutines>( "", "'$(EfcptConfigUseDatabaseNamesForRoutines)'==''");
                    group.Property<EfcptConfigUseInternalAccessForRoutines>( "", "'$(EfcptConfigUseInternalAccessForRoutines)'==''");
                    group.Property<EfcptConfigUseDateOnlyTimeOnly>( "", "'$(EfcptConfigUseDateOnlyTimeOnly)'==''");
                    group.Property<EfcptConfigUseHierarchyId>( "", "'$(EfcptConfigUseHierarchyId)'==''");
                    group.Property<EfcptConfigUseSpatial>( "", "'$(EfcptConfigUseSpatial)'==''");
                    group.Property<EfcptConfigUseNodaTime>( "", "'$(EfcptConfigUseNodaTime)'==''");
                    group.Property<EfcptConfigPreserveCasingWithRegex>( "", "'$(EfcptConfigPreserveCasingWithRegex)'==''");
                    group.Property<EfcptEnableProfiling>( "false", "'$(EfcptEnableProfiling)'==''");
                    group.Property<EfcptProfilingOutput>( "$(EfcptOutput)build-profile.json", "'$(EfcptProfilingOutput)'==''");
                    group.Property<EfcptProfilingVerbosity>( "minimal", "'$(EfcptProfilingVerbosity)'==''");
                });
                t.PropertyGroup(null, group =>
                {
                    group.Property<EfcptSqlProjType>( "microsoft-build-sql", "'$(EfcptSqlProjType)'==''");
                    group.Property<EfcptSqlProjLanguage>( "csharp", "'$(EfcptSqlProjLanguage)'==''");
                    group.Property<EfcptSqlProjOutputDir>( "$(MSBuildProjectDirectory)\\", "'$(EfcptSqlProjOutputDir)'==''");
                    group.Property<EfcptSqlScriptsDir>( "$(MSBuildProjectDirectory)\\", "'$(EfcptSqlScriptsDir)'==''");
                    group.Property<EfcptSqlServerVersion>( "Sql160", "'$(EfcptSqlServerVersion)'==''");
                    group.Property<EfcptSqlPackageToolVersion>( "", "'$(EfcptSqlPackageToolVersion)'==''");
                    group.Property<EfcptSqlPackageToolRestore>( "true", "'$(EfcptSqlPackageToolRestore)'==''");
                    group.Property<EfcptSqlPackageToolPath>( "", "'$(EfcptSqlPackageToolPath)'==''");
                });
            })
            .Build();
    }

    // Strongly-typed property names

    
  public readonly struct EfcptAppConfig : IMsBuildPropertyName
  {
    public string Name => "EfcptAppConfig";
  }
  public readonly struct EfcptApplyMsBuildOverrides : IMsBuildPropertyName
  {
    public string Name => "EfcptApplyMsBuildOverrides";
  }
  public readonly struct EfcptAppSettings : IMsBuildPropertyName
  {
    public string Name => "EfcptAppSettings";
  }
  public readonly struct EfcptAutoDetectWarningLevel : IMsBuildPropertyName
  {
    public string Name => "EfcptAutoDetectWarningLevel";
  }
  public readonly struct EfcptCheckForUpdates : IMsBuildPropertyName
  {
    public string Name => "EfcptCheckForUpdates";
  }
  public readonly struct EfcptConfig : IMsBuildPropertyName
  {
    public string Name => "EfcptConfig";
  }
  public readonly struct EfcptConfigDbContextName : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigDbContextName";
  }
  public readonly struct EfcptConfigDbContextNamespace : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigDbContextNamespace";
  }
  public readonly struct EfcptConfigDbContextOutputPath : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigDbContextOutputPath";
  }
  public readonly struct EfcptConfigDiscoverMultipleResultSets : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigDiscoverMultipleResultSets";
  }
  public readonly struct EfcptConfigEnableOnConfiguring : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigEnableOnConfiguring";
  }
  public readonly struct EfcptConfigGenerateMermaidDiagram : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigGenerateMermaidDiagram";
  }
  public readonly struct EfcptConfigGenerationType : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigGenerationType";
  }
  public readonly struct EfcptConfigMergeDacpacs : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigMergeDacpacs";
  }
  public readonly struct EfcptConfigModelNamespace : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigModelNamespace";
  }
  public readonly struct EfcptConfigOutputPath : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigOutputPath";
  }
  public readonly struct EfcptConfigPreserveCasingWithRegex : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigPreserveCasingWithRegex";
  }
  public readonly struct EfcptConfigRefreshObjectLists : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigRefreshObjectLists";
  }
  public readonly struct EfcptConfigRemoveDefaultSqlFromBool : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigRemoveDefaultSqlFromBool";
  }
  public readonly struct EfcptConfigRootNamespace : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigRootNamespace";
  }
  public readonly struct EfcptConfigSoftDeleteObsoleteFiles : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigSoftDeleteObsoleteFiles";
  }
  public readonly struct EfcptConfigSplitDbContext : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigSplitDbContext";
  }
  public readonly struct EfcptConfigT4TemplatePath : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigT4TemplatePath";
  }
  public readonly struct EfcptConfigUseAlternateResultSetDiscovery : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseAlternateResultSetDiscovery";
  }
  public readonly struct EfcptConfigUseDataAnnotations : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseDataAnnotations";
  }
  public readonly struct EfcptConfigUseDatabaseNames : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseDatabaseNames";
  }
  public readonly struct EfcptConfigUseDatabaseNamesForRoutines : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseDatabaseNamesForRoutines";
  }
  public readonly struct EfcptConfigUseDateOnlyTimeOnly : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseDateOnlyTimeOnly";
  }
  public readonly struct EfcptConfigUseDecimalAnnotationForSprocs : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseDecimalAnnotationForSprocs";
  }
  public readonly struct EfcptConfigUseHierarchyId : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseHierarchyId";
  }
  public readonly struct EfcptConfigUseInflector : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseInflector";
  }
  public readonly struct EfcptConfigUseInternalAccessForRoutines : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseInternalAccessForRoutines";
  }
  public readonly struct EfcptConfigUseLegacyInflector : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseLegacyInflector";
  }
  public readonly struct EfcptConfigUseManyToManyEntity : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseManyToManyEntity";
  }
  public readonly struct EfcptConfigUseNodaTime : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseNodaTime";
  }
  public readonly struct EfcptConfigUseNoNavigations : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseNoNavigations";
  }
  public readonly struct EfcptConfigUsePrefixNavigationNaming : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUsePrefixNavigationNaming";
  }
  public readonly struct EfcptConfigUseSchemaFolders : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseSchemaFolders";
  }
  public readonly struct EfcptConfigUseSchemaNamespaces : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseSchemaNamespaces";
  }
  public readonly struct EfcptConfigUseSpatial : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseSpatial";
  }
  public readonly struct EfcptConfigUseT4 : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseT4";
  }
  public readonly struct EfcptConfigUseT4Split : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseT4Split";
  }
  public readonly struct EfcptConnectionString : IMsBuildPropertyName
  {
    public string Name => "EfcptConnectionString";
  }
  public readonly struct EfcptConnectionStringName : IMsBuildPropertyName
  {
    public string Name => "EfcptConnectionStringName";
  }
  public readonly struct EfcptDacpac : IMsBuildPropertyName
  {
    public string Name => "EfcptDacpac";
  }
  public readonly struct EfcptDataProject : IMsBuildPropertyName
  {
    public string Name => "EfcptDataProject";
  }
  public readonly struct EfcptDataProjectOutputSubdir : IMsBuildPropertyName
  {
    public string Name => "EfcptDataProjectOutputSubdir";
  }
  public readonly struct EfcptDetectGeneratedFileChanges : IMsBuildPropertyName
  {
    public string Name => "EfcptDetectGeneratedFileChanges";
  }
  public readonly struct EfcptDotNetExe : IMsBuildPropertyName
  {
    public string Name => "EfcptDotNetExe";
  }
  public readonly struct EfcptDumpResolvedInputs : IMsBuildPropertyName
  {
    public string Name => "EfcptDumpResolvedInputs";
  }
  public readonly struct EfcptEnabled : IMsBuildPropertyName
  {
    public string Name => "EfcptEnabled";
  }
  public readonly struct EfcptEnableProfiling : IMsBuildPropertyName
  {
    public string Name => "EfcptEnableProfiling";
  }
  public readonly struct EfcptExternalDataDir : IMsBuildPropertyName
  {
    public string Name => "EfcptExternalDataDir";
  }
  public readonly struct EfcptFingerprintFile : IMsBuildPropertyName
  {
    public string Name => "EfcptFingerprintFile";
  }
  public readonly struct EfcptForceUpdateCheck : IMsBuildPropertyName
  {
    public string Name => "EfcptForceUpdateCheck";
  }
  public readonly struct EfcptGeneratedDir : IMsBuildPropertyName
  {
    public string Name => "EfcptGeneratedDir";
  }
  public readonly struct EfcptLogVerbosity : IMsBuildPropertyName
  {
    public string Name => "EfcptLogVerbosity";
  }
  public readonly struct EfcptOutput : IMsBuildPropertyName
  {
    public string Name => "EfcptOutput";
  }
  public readonly struct EfcptProbeSolutionDir : IMsBuildPropertyName
  {
    public string Name => "EfcptProbeSolutionDir";
  }
  public readonly struct EfcptProfilingOutput : IMsBuildPropertyName
  {
    public string Name => "EfcptProfilingOutput";
  }
  public readonly struct EfcptProfilingVerbosity : IMsBuildPropertyName
  {
    public string Name => "EfcptProfilingVerbosity";
  }
  public readonly struct EfcptProvider : IMsBuildPropertyName
  {
    public string Name => "EfcptProvider";
  }
  public readonly struct EfcptRenaming : IMsBuildPropertyName
  {
    public string Name => "EfcptRenaming";
  }
  public readonly struct EfcptSdkVersionWarningLevel : IMsBuildPropertyName
  {
    public string Name => "EfcptSdkVersionWarningLevel";
  }
  public readonly struct EfcptSolutionDir : IMsBuildPropertyName
  {
    public string Name => "EfcptSolutionDir";
  }
  public readonly struct EfcptSolutionPath : IMsBuildPropertyName
  {
    public string Name => "EfcptSolutionPath";
  }
  public readonly struct EfcptSplitOutputs : IMsBuildPropertyName
  {
    public string Name => "EfcptSplitOutputs";
  }
  public readonly struct EfcptSqlPackageToolPath : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlPackageToolPath";
  }
  public readonly struct EfcptSqlPackageToolRestore : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlPackageToolRestore";
  }
  public readonly struct EfcptSqlPackageToolVersion : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlPackageToolVersion";
  }
  public readonly struct EfcptSqlProj : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlProj";
  }
  public readonly struct EfcptSqlProjLanguage : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlProjLanguage";
  }
  public readonly struct EfcptSqlProjOutputDir : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlProjOutputDir";
  }
  public readonly struct EfcptSqlProjType : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlProjType";
  }
  public readonly struct EfcptSqlScriptsDir : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlScriptsDir";
  }
  public readonly struct EfcptSqlServerVersion : IMsBuildPropertyName
  {
    public string Name => "EfcptSqlServerVersion";
  }
  public readonly struct EfcptStampFile : IMsBuildPropertyName
  {
    public string Name => "EfcptStampFile";
  }
  public readonly struct EfcptTemplateDir : IMsBuildPropertyName
  {
    public string Name => "EfcptTemplateDir";
  }
  public readonly struct EfcptToolCommand : IMsBuildPropertyName
  {
    public string Name => "EfcptToolCommand";
  }
  public readonly struct EfcptToolMode : IMsBuildPropertyName
  {
    public string Name => "EfcptToolMode";
  }
  public readonly struct EfcptToolPackageId : IMsBuildPropertyName
  {
    public string Name => "EfcptToolPackageId";
  }
  public readonly struct EfcptToolPath : IMsBuildPropertyName
  {
    public string Name => "EfcptToolPath";
  }
  public readonly struct EfcptToolRestore : IMsBuildPropertyName
  {
    public string Name => "EfcptToolRestore";
  }
  public readonly struct EfcptToolVersion : IMsBuildPropertyName
  {
    public string Name => "EfcptToolVersion";
  }
  public readonly struct EfcptUpdateCheckCacheHours : IMsBuildPropertyName
  {
    public string Name => "EfcptUpdateCheckCacheHours";
  }
}






