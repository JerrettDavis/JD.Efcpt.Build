using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build.Definitions;

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
                    // Design-Time Build Guard: default for the opt-back-in override. The actual
                    // guard that forces EfcptEnabled=false during a design-time build lives in
                    // BuildTransitiveTargetsFactory (JD.Efcpt.Build.targets), not here, because
                    // this props file is imported before the consuming project's own
                    // PropertyGroup - too early to see a project-level override of this property.
                    group.Property<EfcptRunDuringDesignTimeBuild>( "false", "'$(EfcptRunDuringDesignTimeBuild)'==''");
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
                    // Pluggable connection-string sources (#188): EfcptConnectionStringSource
                    // selects a source key ("env", "azure-keyvault", "aws-secrets", ...); empty
                    // (the default) preserves today's file/.sqlproj resolution behavior exactly.
                    group.Property<EfcptConnectionStringSource>( "", "'$(EfcptConnectionStringSource)'==''");
                    group.Property<EfcptConnectionStringEnvVar>( "", "'$(EfcptConnectionStringEnvVar)'==''");
                    group.Property<EfcptKeyVaultUri>( "", "'$(EfcptKeyVaultUri)'==''");
                    group.Property<EfcptKeyVaultSecretName>( "", "'$(EfcptKeyVaultSecretName)'==''");
                    group.Property<EfcptKeyVaultSecretVersion>( "", "'$(EfcptKeyVaultSecretVersion)'==''");
                    group.Property<EfcptAwsSecretId>( "", "'$(EfcptAwsSecretId)'==''");
                    group.Property<EfcptAwsRegion>( "", "'$(EfcptAwsRegion)'==''");
                    group.Property<EfcptAwsSecretJsonKey>( "", "'$(EfcptAwsSecretJsonKey)'==''");
                    group.Property<EfcptProvider>( "mssql", "'$(EfcptProvider)'==''");
                    // customProviders plugin registry (#184): security opt-in gate. Custom
                    // providers (registered via @(EfcptCustomProvider)) load and execute
                    // third-party code at build time, so they are fail-closed (disabled) by
                    // default - this must stay "false" here to preserve that default.
                    group.Property<EfcptAllowCustomProviders>( "false", "'$(EfcptAllowCustomProviders)'==''");
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
                    // EFCPT_OFFLINE env-var bridge: MSBuild exposes environment variables as
                    // $(EFCPT_OFFLINE) automatically. When EfcptOfflineMode has not been set
                    // explicitly, honor a truthy EFCPT_OFFLINE value as the default so the
                    // documented "either is sufficient" contract holds even for targets/tasks
                    // that read $(EfcptOfflineMode) directly (e.g. _EfcptCheckForUpdates).
                    // Truthy tokens intentionally mirror StringExtensions.IsTrue() (true/yes/on/1/
                    // enable/enabled/y); MSBuild condition equality is case-insensitive, so no
                    // additional case variants are needed. EFCPT_OFFLINE=false/0/unset does NOT
                    // enable offline mode.
                    group.Property<EfcptOfflineMode>( "true", "'$(EfcptOfflineMode)'=='' and ('$(EFCPT_OFFLINE)'=='true' or '$(EFCPT_OFFLINE)'=='yes' or '$(EFCPT_OFFLINE)'=='on' or '$(EFCPT_OFFLINE)'=='1' or '$(EFCPT_OFFLINE)'=='enable' or '$(EFCPT_OFFLINE)'=='enabled' or '$(EFCPT_OFFLINE)'=='y')");
                    group.Property<EfcptOfflineMode>( "false", "'$(EfcptOfflineMode)'==''");
                    // .NET 8/9 automatic tool acquisition (#186): when no hermetic, network-free
                    // way to run the efcpt tool is otherwise available, RunEfcpt bootstraps an
                    // obj-local tool manifest and installs the tool into it. EfcptOfflineMode
                    // always takes precedence over this property - see RunEfcpt.AcquireToolIfNeeded.
                    group.Property<EfcptAutoAcquireTool>( "true", "'$(EfcptAutoAcquireTool)'==''");
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
                    group.Property<EfcptRunDuringDesignTimeBuild>( "false", "'$(EfcptRunDuringDesignTimeBuild)'==''");
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
                    // Pluggable connection-string sources (#188): EfcptConnectionStringSource
                    // selects a source key ("env", "azure-keyvault", "aws-secrets", ...); empty
                    // (the default) preserves today's file/.sqlproj resolution behavior exactly.
                    group.Property<EfcptConnectionStringSource>( "", "'$(EfcptConnectionStringSource)'==''");
                    group.Property<EfcptConnectionStringEnvVar>( "", "'$(EfcptConnectionStringEnvVar)'==''");
                    group.Property<EfcptKeyVaultUri>( "", "'$(EfcptKeyVaultUri)'==''");
                    group.Property<EfcptKeyVaultSecretName>( "", "'$(EfcptKeyVaultSecretName)'==''");
                    group.Property<EfcptKeyVaultSecretVersion>( "", "'$(EfcptKeyVaultSecretVersion)'==''");
                    group.Property<EfcptAwsSecretId>( "", "'$(EfcptAwsSecretId)'==''");
                    group.Property<EfcptAwsRegion>( "", "'$(EfcptAwsRegion)'==''");
                    group.Property<EfcptAwsSecretJsonKey>( "", "'$(EfcptAwsSecretJsonKey)'==''");
                    group.Property<EfcptProvider>( "mssql", "'$(EfcptProvider)'==''");
                    // customProviders plugin registry (#184): security opt-in gate. Custom
                    // providers (registered via @(EfcptCustomProvider)) load and execute
                    // third-party code at build time, so they are fail-closed (disabled) by
                    // default - this must stay "false" here to preserve that default.
                    group.Property<EfcptAllowCustomProviders>( "false", "'$(EfcptAllowCustomProviders)'==''");
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
                    // EFCPT_OFFLINE env-var bridge: MSBuild exposes environment variables as
                    // $(EFCPT_OFFLINE) automatically. When EfcptOfflineMode has not been set
                    // explicitly, honor a truthy EFCPT_OFFLINE value as the default so the
                    // documented "either is sufficient" contract holds even for targets/tasks
                    // that read $(EfcptOfflineMode) directly (e.g. _EfcptCheckForUpdates).
                    // Truthy tokens intentionally mirror StringExtensions.IsTrue() (true/yes/on/1/
                    // enable/enabled/y); MSBuild condition equality is case-insensitive, so no
                    // additional case variants are needed. EFCPT_OFFLINE=false/0/unset does NOT
                    // enable offline mode.
                    group.Property<EfcptOfflineMode>( "true", "'$(EfcptOfflineMode)'=='' and ('$(EFCPT_OFFLINE)'=='true' or '$(EFCPT_OFFLINE)'=='yes' or '$(EFCPT_OFFLINE)'=='on' or '$(EFCPT_OFFLINE)'=='1' or '$(EFCPT_OFFLINE)'=='enable' or '$(EFCPT_OFFLINE)'=='enabled' or '$(EFCPT_OFFLINE)'=='y')");
                    group.Property<EfcptOfflineMode>( "false", "'$(EfcptOfflineMode)'==''");
                    // .NET 8/9 automatic tool acquisition (#186): when no hermetic, network-free
                    // way to run the efcpt tool is otherwise available, RunEfcpt bootstraps an
                    // obj-local tool manifest and installs the tool into it. EfcptOfflineMode
                    // always takes precedence over this property - see RunEfcpt.AcquireToolIfNeeded.
                    group.Property<EfcptAutoAcquireTool>( "true", "'$(EfcptAutoAcquireTool)'==''");
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

    
  public readonly struct EfcptAllowCustomProviders : IMsBuildPropertyName
  {
    public string Name => "EfcptAllowCustomProviders";
  }
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
  public readonly struct EfcptAutoAcquireTool : IMsBuildPropertyName
  {
    public string Name => "EfcptAutoAcquireTool";
  }
  public readonly struct EfcptAutoDetectWarningLevel : IMsBuildPropertyName
  {
    public string Name => "EfcptAutoDetectWarningLevel";
  }
  public readonly struct EfcptAwsRegion : IMsBuildPropertyName
  {
    public string Name => "EfcptAwsRegion";
  }
  public readonly struct EfcptAwsSecretId : IMsBuildPropertyName
  {
    public string Name => "EfcptAwsSecretId";
  }
  public readonly struct EfcptAwsSecretJsonKey : IMsBuildPropertyName
  {
    public string Name => "EfcptAwsSecretJsonKey";
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
  public readonly struct EfcptConnectionStringEnvVar : IMsBuildPropertyName
  {
    public string Name => "EfcptConnectionStringEnvVar";
  }
  public readonly struct EfcptConnectionStringName : IMsBuildPropertyName
  {
    public string Name => "EfcptConnectionStringName";
  }
  public readonly struct EfcptConnectionStringSource : IMsBuildPropertyName
  {
    public string Name => "EfcptConnectionStringSource";
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
  public readonly struct EfcptKeyVaultSecretName : IMsBuildPropertyName
  {
    public string Name => "EfcptKeyVaultSecretName";
  }
  public readonly struct EfcptKeyVaultSecretVersion : IMsBuildPropertyName
  {
    public string Name => "EfcptKeyVaultSecretVersion";
  }
  public readonly struct EfcptKeyVaultUri : IMsBuildPropertyName
  {
    public string Name => "EfcptKeyVaultUri";
  }
  public readonly struct EfcptLogVerbosity : IMsBuildPropertyName
  {
    public string Name => "EfcptLogVerbosity";
  }
  public readonly struct EfcptOfflineMode : IMsBuildPropertyName
  {
    public string Name => "EfcptOfflineMode";
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
  public readonly struct EfcptRunDuringDesignTimeBuild : IMsBuildPropertyName
  {
    public string Name => "EfcptRunDuringDesignTimeBuild";
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





