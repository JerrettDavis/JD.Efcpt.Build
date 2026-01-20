using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// MSBuild package definition scaffolded from JD.Efcpt.Build.xml
/// </summary>
public static class BuildTransitiveTargetsFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("JD.Efcpt.Build")
            .Props(p =>
            {
                p.PropertyGroup(null, group =>
                {
                    group.Property<EfcptConfigUseNullableReferenceTypes>( "true", "'$(EfcptConfigUseNullableReferenceTypes)'=='' and ('$(Nullable)'=='enable' or '$(Nullable)'=='Enable')");
                    group.Property<EfcptConfigUseNullableReferenceTypes>( "false", "'$(EfcptConfigUseNullableReferenceTypes)'=='' and '$(Nullable)'!=''");
                });
                p.PropertyGroup(null, group =>
                {
                    group.Property("_EfcptTasksFolder", "net10.0", "'$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))");
                    group.Property("_EfcptTasksFolder", "net10.0", "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.14'))");
                    group.Property("_EfcptTasksFolder", "net9.0", "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.12'))");
                    group.Property("_EfcptTasksFolder", "net8.0", "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core'");
                    group.Property("_EfcptTasksFolder", "net472", "'$(_EfcptTasksFolder)' == ''");
                    group.Property("_EfcptTaskAssembly", "$(MSBuildThisFileDirectory)..\\tasks\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll");
                    group.Property("_EfcptTaskAssembly", "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks\\bin\\$(Configuration)\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll", "!Exists('$(_EfcptTaskAssembly)')");
                    group.Property("_EfcptTaskAssembly", "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks\\bin\\Debug\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll", "!Exists('$(_EfcptTaskAssembly)') and '$(Configuration)' == ''");
                });
            })
            .Targets(t =>
            {
                t.PropertyGroup(null, group =>
                {
                    group.Property<EfcptConfigUseNullableReferenceTypes>( "true", "'$(EfcptConfigUseNullableReferenceTypes)'=='' and ('$(Nullable)'=='enable' or '$(Nullable)'=='Enable')");
                    group.Property<EfcptConfigUseNullableReferenceTypes>( "false", "'$(EfcptConfigUseNullableReferenceTypes)'=='' and '$(Nullable)'!=''");
                });
                t.Target("_EfcptDetectSqlProject", target =>
                {
                    target.BeforeTargets("BeforeBuild", "BeforeRebuild");
                    target.Task("DetectSqlProject", task =>
                    {
                        task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                        task.Param("SqlServerVersion", "$(SqlServerVersion)");
                        task.Param("DSP", "$(DSP)");
                        task.OutputProperty<IsSqlProject, EfcptIsSqlProject>();
                    });
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptIsSqlProject", "false");
                    });
                });
                t.PropertyGroup(null, group =>
                {
                    group.Property("_EfcptTasksFolder", "net10.0", "'$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))");
                    group.Property("_EfcptTasksFolder", "net10.0", "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.14'))");
                    group.Property("_EfcptTasksFolder", "net9.0", "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.12'))");
                    group.Property("_EfcptTasksFolder", "net8.0", "'$(_EfcptTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core'");
                    group.Property("_EfcptTasksFolder", "net472", "'$(_EfcptTasksFolder)' == ''");
                    group.Property("_EfcptTaskAssembly", "$(MSBuildThisFileDirectory)..\\tasks\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll");
                    group.Property("_EfcptTaskAssembly", "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks\\bin\\$(Configuration)\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll", "!Exists('$(_EfcptTaskAssembly)')");
                    group.Property("_EfcptTaskAssembly", "$(MSBuildThisFileDirectory)..\\..\\JD.Efcpt.Build.Tasks\\bin\\Debug\\$(_EfcptTasksFolder)\\JD.Efcpt.Build.Tasks.dll", "!Exists('$(_EfcptTaskAssembly)') and '$(Configuration)' == ''");
                });
                t.Target("_EfcptLogTaskAssemblyInfo", target =>
                {
                    target.BeforeTargets(new EfcptResolveInputsTarget(), new EfcptResolveInputsForDirectDacpacTarget());
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(EfcptLogVerbosity)' == 'detailed'");
                    target.Message("EFCPT Task Assembly Selection:", "high");
                    target.Message("  MSBuildRuntimeType: $(MSBuildRuntimeType)", "high");
                    target.Message("  MSBuildVersion: $(MSBuildVersion)", "high");
                    target.Message("  Selected TasksFolder: $(_EfcptTasksFolder)", "high");
                    target.Message("  TaskAssembly Path: $(_EfcptTaskAssembly)", "high");
                    target.Message("  TaskAssembly Exists: $([System.IO.File]::Exists('$(_EfcptTaskAssembly)'))", "high");
                });
                t.UsingTask("JD.Efcpt.Build.Tasks.ResolveSqlProjAndInputs", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.EnsureDacpacBuilt", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.StageEfcptInputs", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.ComputeFingerprint", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.RunEfcpt", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.RenameGeneratedFiles", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.QuerySchemaMetadata", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.ApplyConfigOverrides", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.ResolveDbContextName", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.SerializeConfigProperties", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.CheckSdkVersion", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.RunSqlPackage", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.AddSqlFileWarnings", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.DetectSqlProject", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.InitializeBuildProfiling", "$(_EfcptTaskAssembly)");
                t.UsingTask("JD.Efcpt.Build.Tasks.FinalizeBuildProfiling", "$(_EfcptTaskAssembly)");
                t.Target("_EfcptInitializeProfiling", target =>
                {
                    target.BeforeTargets("_EfcptDetectSqlProject");
                    target.Condition("'$(EfcptEnabled)' == 'true'");
                    target.Task("InitializeBuildProfiling", task =>
                    {
                        task.Param("EnableProfiling", "$(EfcptEnableProfiling)");
                        task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                        task.Param("ProjectName", "$(MSBuildProjectName)");
                        task.Param("TargetFramework", "$(TargetFramework)");
                        task.Param("Configuration", "$(Configuration)");
                        task.Param("ConfigPath", "$(_EfcptResolvedConfig)");
                        task.Param("RenamingPath", "$(_EfcptResolvedRenaming)");
                        task.Param("TemplateDir", "$(_EfcptResolvedTemplateDir)");
                        task.Param("SqlProjectPath", "$(_EfcptSqlProj)");
                        task.Param("DacpacPath", "$(_EfcptDacpacPath)");
                        task.Param("Provider", "$(EfcptProvider)");
                    });
                });
                t.Target("_EfcptCheckForUpdates", target =>
                {
                    target.BeforeTargets("Build");
                    target.Condition("'$(EfcptCheckForUpdates)' == 'true' and '$(EfcptSdkVersion)' != ''");
                    target.Task("CheckSdkVersion", task =>
                    {
                        task.Param("CurrentVersion", "$(EfcptSdkVersion)");
                        task.Param("PackageId", "JD.Efcpt.Sdk");
                        task.Param("CacheHours", "$(EfcptUpdateCheckCacheHours)");
                        task.Param("ForceCheck", "$(EfcptForceUpdateCheck)");
                        task.Param("WarningLevel", "$(EfcptSdkVersionWarningLevel)");
                        task.OutputProperty<LatestVersion, EfcptLatestVersion>();
                        task.OutputProperty<UpdateAvailable, EfcptUpdateAvailable>();
                    });
                });
                t.Target<BeforeSqlProjGenerationTarget>( target =>
                {
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' == 'true'");
                });
                t.Target<EfcptQueryDatabaseSchemaForSqlProjTarget>( target =>
                {
                    target.DependsOnTargets("BeforeSqlProjGeneration");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' == 'true'");
                    target.Error("SqlProj generation requires a connection string. Set EfcptConnectionString, EfcptAppSettings, or EfcptAppConfig.", "'$(EfcptConnectionString)' == '' and '$(EfcptAppSettings)' == '' and '$(EfcptAppConfig)' == ''");
                    target.Message("Querying database schema for fingerprinting...", "high");
                    target.Task("QuerySchemaMetadata", task =>
                    {
                        task.Param("ConnectionString", "$(EfcptConnectionString)");
                        task.Param("OutputDir", "$(EfcptOutput)");
                        task.Param("Provider", "$(EfcptProvider)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<SchemaFingerprint, EfcptSchemaFingerprint>();
                    });
                    target.Message("Database schema fingerprint: $(_EfcptSchemaFingerprint)", "normal");
                });
                t.Target<EfcptExtractDatabaseSchemaToScriptsTarget>( target =>
                {
                    target.DependsOnTargets("EfcptQueryDatabaseSchemaForSqlProj");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' == 'true'");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptScriptsDir", "$(EfcptSqlScriptsDir)");
                    });
                    target.Message("Extracting database schema to SQL scripts in SQL project: $(_EfcptScriptsDir)", "high");
                    target.ItemGroup(null, group =>
                    {
                        group.Include("_EfcptGeneratedScripts", "$(_EfcptScriptsDir)**\\*.sql");
                    });
                    target.Task("Delete", task =>
                    {
                        task.Param("Files", "@(_EfcptGeneratedScripts)");
                    }, "'@(_EfcptGeneratedScripts)' != ''");
                    target.Task("RunSqlPackage", task =>
                    {
                        task.Param("ToolVersion", "$(EfcptSqlPackageToolVersion)");
                        task.Param("ToolRestore", "$(EfcptSqlPackageToolRestore)");
                        task.Param("ToolPath", "$(EfcptSqlPackageToolPath)");
                        task.Param("DotNetExe", "$(EfcptDotNetExe)");
                        task.Param("WorkingDirectory", "$(EfcptOutput)");
                        task.Param("ConnectionString", "$(EfcptConnectionString)");
                        task.Param("TargetDirectory", "$(_EfcptScriptsDir)");
                        task.Param("ExtractTarget", "SchemaObjectType");
                        task.Param("TargetFramework", "$(TargetFramework)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<ExtractedPath, EfcptExtractedScriptsPath>();
                    });
                    target.Message("Extracted SQL scripts to: $(_EfcptExtractedScriptsPath)", "high");
                });
                t.Target<EfcptAddSqlFileWarningsTarget>( target =>
                {
                    target.DependsOnTargets("EfcptExtractDatabaseSchemaToScripts");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' == 'true'");
                    target.Message("Adding auto-generation warnings to SQL files...", "high");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptDatabaseName", "$([System.Text.RegularExpressions.Regex]::Match($(EfcptConnectionString), 'Database\\s*=\\s*\\\"?([^;\"]+)\\\"?').Groups[1].Value)");
                        group.Property("_EfcptDatabaseName", "$([System.Text.RegularExpressions.Regex]::Match($(EfcptConnectionString), 'Initial Catalog\\s*=\\s*\\\"?([^;\"]+)\\\"?').Groups[1].Value)");
                    });
                    target.Task("AddSqlFileWarnings", task =>
                    {
                        task.Param("ScriptsDirectory", "$(_EfcptScriptsDir)");
                        task.Param("DatabaseName", "$(_EfcptDatabaseName)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                    });
                });
                t.Target<AfterSqlProjGenerationTarget>( target =>
                {
                    target.BeforeTargets("Build");
                    target.DependsOnTargets("EfcptAddSqlFileWarnings");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' == 'true'");
                    target.Message("_EfcptIsSqlProject: $(_EfcptIsSqlProject)", "high");
                    target.Message("SQL script generation complete. SQL project will build to DACPAC.", "high");
                });
                t.Target<EfcptResolveInputsTarget>( target =>
                {
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true' and '$(EfcptDacpac)' == ''");
                    target.Task("ResolveSqlProjAndInputs", task =>
                    {
                        task.Param("ProjectFullPath", "$(MSBuildProjectFullPath)");
                        task.Param("ProjectDirectory", "$(MSBuildProjectDirectory)");
                        task.Param("Configuration", "$(Configuration)");
                        task.Param("ProjectReferences", "@(ProjectReference)");
                        task.Param("SqlProjOverride", "$(EfcptSqlProj)");
                        task.Param("ConfigOverride", "$(EfcptConfig)");
                        task.Param("RenamingOverride", "$(EfcptRenaming)");
                        task.Param("TemplateDirOverride", "$(EfcptTemplateDir)");
                        task.Param("SolutionDir", "$(EfcptSolutionDir)");
                        task.Param("SolutionPath", "$(EfcptSolutionPath)");
                        task.Param("ProbeSolutionDir", "$(EfcptProbeSolutionDir)");
                        task.Param("OutputDir", "$(EfcptOutput)");
                        task.Param("DefaultsRoot", "$(MSBuildThisFileDirectory)Defaults");
                        task.Param("DumpResolvedInputs", "$(EfcptDumpResolvedInputs)");
                        task.Param("EfcptConnectionString", "$(EfcptConnectionString)");
                        task.Param("EfcptAppSettings", "$(EfcptAppSettings)");
                        task.Param("EfcptAppConfig", "$(EfcptAppConfig)");
                        task.Param("EfcptConnectionStringName", "$(EfcptConnectionStringName)");
                        task.Param("AutoDetectWarningLevel", "$(EfcptAutoDetectWarningLevel)");
                        task.OutputProperty<SqlProjPath, EfcptSqlProj>();
                        task.OutputProperty<ResolvedConfigPath, EfcptResolvedConfig>();
                        task.OutputProperty<ResolvedRenamingPath, EfcptResolvedRenaming>();
                        task.OutputProperty<ResolvedTemplateDir, EfcptResolvedTemplateDir>();
                        task.OutputProperty<ResolvedConnectionString, EfcptResolvedConnectionString>();
                        task.OutputProperty<UseConnectionString, EfcptUseConnectionString>();
                        task.OutputProperty<IsUsingDefaultConfig, EfcptIsUsingDefaultConfig>();
                    });
                });
                t.Target<EfcptResolveInputsForDirectDacpacTarget>( target =>
                {
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(EfcptDacpac)' != ''");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptResolvedConfig", "$(MSBuildProjectDirectory)\\$(EfcptConfig)");
                        group.Property("_EfcptResolvedConfig", "$(MSBuildThisFileDirectory)Defaults\\efcpt-config.json");
                        group.Property("_EfcptResolvedRenaming", "$(MSBuildProjectDirectory)\\$(EfcptRenaming)");
                        group.Property("_EfcptResolvedRenaming", "$(MSBuildThisFileDirectory)Defaults\\efcpt.renaming.json");
                        group.Property("_EfcptResolvedTemplateDir", "$(MSBuildProjectDirectory)\\$(EfcptTemplateDir)");
                        group.Property("_EfcptResolvedTemplateDir", "$(MSBuildThisFileDirectory)Defaults\\Template");
                        group.Property("_EfcptIsUsingDefaultConfig", "true");
                        group.Property("_EfcptUseConnectionString", "false");
                    });
                    target.Task("MakeDir", task =>
                    {
                        task.Param("Directories", "$(EfcptOutput)");
                    });
                });
                t.Target<EfcptQuerySchemaMetadataTarget>( target =>
                {
                    target.BeforeTargets(new EfcptStageInputsTarget());
                    target.AfterTargets(new EfcptResolveInputsTarget());
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptUseConnectionString)' == 'true'");
                    target.Task("QuerySchemaMetadata", task =>
                    {
                        task.Param("ConnectionString", "$(_EfcptResolvedConnectionString)");
                        task.Param("OutputDir", "$(EfcptOutput)");
                        task.Param("Provider", "$(EfcptProvider)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<SchemaFingerprint, EfcptSchemaFingerprint>();
                    });
                });
                t.Target<EfcptUseDirectDacpacTarget>( target =>
                {
                    target.DependsOnTargets("EfcptResolveInputs;EfcptResolveInputsForDirectDacpac");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptUseConnectionString)' != 'true' and '$(EfcptDacpac)' != ''");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptDacpacPath", "$(EfcptDacpac)");
                        group.Property("_EfcptDacpacPath", "$([System.IO.Path]::GetFullPath($([System.IO.Path]::Combine('$(MSBuildProjectDirectory)', '$(EfcptDacpac)'))))");
                        group.Property("_EfcptUseDirectDacpac", "true");
                    });
                    target.Error("EfcptDacpac was specified but the file does not exist: $(_EfcptDacpacPath)", "!Exists('$(_EfcptDacpacPath)')");
                    target.Message("Using pre-built DACPAC: $(_EfcptDacpacPath)", "high");
                });
                t.Target<EfcptBuildSqlProjTarget>( target =>
                {
                    target.DependsOnTargets("EfcptResolveInputs;EfcptUseDirectDacpac");
                    target.Condition("'$(EfcptEnabled)' == 'true'");
                    target.Message("Building SQL project: $(_EfcptSqlProj)", "normal", "'$(_EfcptUseConnectionString)' != 'true' and '$(_EfcptUseDirectDacpac)' != 'true' and '$(_EfcptSqlProj)' != ''");
                    target.Task("MSBuild", task =>
                    {
                        task.Param("Projects", "$(_EfcptSqlProj)");
                        task.Param("Targets", "Build");
                        task.Param("Properties", "Configuration=$(Configuration)");
                        task.Param("BuildInParallel", "false");
                    }, "'$(_EfcptUseConnectionString)' != 'true' and '$(_EfcptUseDirectDacpac)' != 'true' and '$(_EfcptSqlProj)' != ''");
                });
                t.Target<EfcptEnsureDacpacTarget>( target =>
                {
                    target.DependsOnTargets("EfcptResolveInputs;EfcptUseDirectDacpac;EfcptBuildSqlProj");
                    target.Condition("'$(EfcptEnabled)' == 'true'");
                    target.Task("EnsureDacpacBuilt", task =>
                    {
                        task.Param("SqlProjPath", "$(_EfcptSqlProj)");
                        task.Param("Configuration", "$(Configuration)");
                        task.Param("MsBuildExe", "$(MSBuildBinPath)msbuild.exe");
                        task.Param("DotNetExe", "$(EfcptDotNetExe)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<DacpacPath, EfcptDacpacPath>();
                    }, "'$(_EfcptUseConnectionString)' != 'true' and '$(_EfcptUseDirectDacpac)' != 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                });
                t.Target<EfcptResolveDbContextNameTarget>( target =>
                {
                    target.DependsOnTargets("EfcptResolveInputs;EfcptEnsureDacpac;EfcptUseDirectDacpac");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                    target.Task("ResolveDbContextName", task =>
                    {
                        task.Param("ExplicitDbContextName", "$(EfcptConfigDbContextName)");
                        task.Param("SqlProjPath", "$(_EfcptSqlProj)");
                        task.Param("DacpacPath", "$(_EfcptDacpacPath)");
                        task.Param("ConnectionString", "$(_EfcptResolvedConnectionString)");
                        task.Param("UseConnectionStringMode", "$(_EfcptUseConnectionString)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<ResolvedDbContextName, EfcptResolvedDbContextName>();
                    });
                    target.PropertyGroup(null, group =>
                    {
                        group.Property<EfcptConfigDbContextName>( "$(_EfcptResolvedDbContextName)");
                    });
                });
                t.Target<EfcptStageInputsTarget>( target =>
                {
                    target.DependsOnTargets("EfcptResolveInputs;EfcptEnsureDacpac;EfcptUseDirectDacpac;EfcptResolveDbContextName");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                    target.Task("StageEfcptInputs", task =>
                    {
                        task.Param("OutputDir", "$(EfcptOutput)");
                        task.Param("ProjectDirectory", "$(MSBuildProjectDirectory)");
                        task.Param("ConfigPath", "$(_EfcptResolvedConfig)");
                        task.Param("RenamingPath", "$(_EfcptResolvedRenaming)");
                        task.Param("TemplateDir", "$(_EfcptResolvedTemplateDir)");
                        task.Param("TemplateOutputDir", "$(EfcptGeneratedDir)");
                        task.Param("TargetFramework", "$(TargetFramework)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<StagedConfigPath, EfcptStagedConfig>();
                        task.OutputProperty<StagedRenamingPath, EfcptStagedRenaming>();
                        task.OutputProperty<StagedTemplateDir, EfcptStagedTemplateDir>();
                    });
                });
                t.Target<EfcptApplyConfigOverridesTarget>( target =>
                {
                    target.DependsOnTargets("EfcptStageInputs");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                    target.Task("ApplyConfigOverrides", task =>
                    {
                        task.Param("StagedConfigPath", "$(_EfcptStagedConfig)");
                        task.Param("ApplyOverrides", "$(EfcptApplyMsBuildOverrides)");
                        task.Param("IsUsingDefaultConfig", "$(_EfcptIsUsingDefaultConfig)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.Param("RootNamespace", "$(EfcptConfigRootNamespace)");
                        task.Param("DbContextName", "$(EfcptConfigDbContextName)");
                        task.Param("DbContextNamespace", "$(EfcptConfigDbContextNamespace)");
                        task.Param("ModelNamespace", "$(EfcptConfigModelNamespace)");
                        task.Param("OutputPath", "$(EfcptConfigOutputPath)");
                        task.Param("DbContextOutputPath", "$(EfcptConfigDbContextOutputPath)");
                        task.Param("SplitDbContext", "$(EfcptConfigSplitDbContext)");
                        task.Param("UseSchemaFolders", "$(EfcptConfigUseSchemaFolders)");
                        task.Param("UseSchemaNamespaces", "$(EfcptConfigUseSchemaNamespaces)");
                        task.Param("EnableOnConfiguring", "$(EfcptConfigEnableOnConfiguring)");
                        task.Param("GenerationType", "$(EfcptConfigGenerationType)");
                        task.Param("UseDatabaseNames", "$(EfcptConfigUseDatabaseNames)");
                        task.Param("UseDataAnnotations", "$(EfcptConfigUseDataAnnotations)");
                        task.Param("UseNullableReferenceTypes", "$(EfcptConfigUseNullableReferenceTypes)");
                        task.Param("UseInflector", "$(EfcptConfigUseInflector)");
                        task.Param("UseLegacyInflector", "$(EfcptConfigUseLegacyInflector)");
                        task.Param("UseManyToManyEntity", "$(EfcptConfigUseManyToManyEntity)");
                        task.Param("UseT4", "$(EfcptConfigUseT4)");
                        task.Param("UseT4Split", "$(EfcptConfigUseT4Split)");
                        task.Param("RemoveDefaultSqlFromBool", "$(EfcptConfigRemoveDefaultSqlFromBool)");
                        task.Param("SoftDeleteObsoleteFiles", "$(EfcptConfigSoftDeleteObsoleteFiles)");
                        task.Param("DiscoverMultipleResultSets", "$(EfcptConfigDiscoverMultipleResultSets)");
                        task.Param("UseAlternateResultSetDiscovery", "$(EfcptConfigUseAlternateResultSetDiscovery)");
                        task.Param("T4TemplatePath", "$(EfcptConfigT4TemplatePath)");
                        task.Param("UseNoNavigations", "$(EfcptConfigUseNoNavigations)");
                        task.Param("MergeDacpacs", "$(EfcptConfigMergeDacpacs)");
                        task.Param("RefreshObjectLists", "$(EfcptConfigRefreshObjectLists)");
                        task.Param("GenerateMermaidDiagram", "$(EfcptConfigGenerateMermaidDiagram)");
                        task.Param("UseDecimalAnnotationForSprocs", "$(EfcptConfigUseDecimalAnnotationForSprocs)");
                        task.Param("UsePrefixNavigationNaming", "$(EfcptConfigUsePrefixNavigationNaming)");
                        task.Param("UseDatabaseNamesForRoutines", "$(EfcptConfigUseDatabaseNamesForRoutines)");
                        task.Param("UseInternalAccessForRoutines", "$(EfcptConfigUseInternalAccessForRoutines)");
                        task.Param("UseDateOnlyTimeOnly", "$(EfcptConfigUseDateOnlyTimeOnly)");
                        task.Param("UseHierarchyId", "$(EfcptConfigUseHierarchyId)");
                        task.Param("UseSpatial", "$(EfcptConfigUseSpatial)");
                        task.Param("UseNodaTime", "$(EfcptConfigUseNodaTime)");
                        task.Param("PreserveCasingWithRegex", "$(EfcptConfigPreserveCasingWithRegex)");
                    });
                });
                t.Target<EfcptSerializeConfigPropertiesTarget>( target =>
                {
                    target.DependsOnTargets("EfcptApplyConfigOverrides");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                    target.Task("SerializeConfigProperties", task =>
                    {
                        task.Param("RootNamespace", "$(EfcptConfigRootNamespace)");
                        task.Param("DbContextName", "$(EfcptConfigDbContextName)");
                        task.Param("DbContextNamespace", "$(EfcptConfigDbContextNamespace)");
                        task.Param("ModelNamespace", "$(EfcptConfigModelNamespace)");
                        task.Param("OutputPath", "$(EfcptConfigOutputPath)");
                        task.Param("DbContextOutputPath", "$(EfcptConfigDbContextOutputPath)");
                        task.Param("SplitDbContext", "$(EfcptConfigSplitDbContext)");
                        task.Param("UseSchemaFolders", "$(EfcptConfigUseSchemaFolders)");
                        task.Param("UseSchemaNamespaces", "$(EfcptConfigUseSchemaNamespaces)");
                        task.Param("EnableOnConfiguring", "$(EfcptConfigEnableOnConfiguring)");
                        task.Param("GenerationType", "$(EfcptConfigGenerationType)");
                        task.Param("UseDatabaseNames", "$(EfcptConfigUseDatabaseNames)");
                        task.Param("UseDataAnnotations", "$(EfcptConfigUseDataAnnotations)");
                        task.Param("UseNullableReferenceTypes", "$(EfcptConfigUseNullableReferenceTypes)");
                        task.Param("UseInflector", "$(EfcptConfigUseInflector)");
                        task.Param("UseLegacyInflector", "$(EfcptConfigUseLegacyInflector)");
                        task.Param("UseManyToManyEntity", "$(EfcptConfigUseManyToManyEntity)");
                        task.Param("UseT4", "$(EfcptConfigUseT4)");
                        task.Param("UseT4Split", "$(EfcptConfigUseT4Split)");
                        task.Param("RemoveDefaultSqlFromBool", "$(EfcptConfigRemoveDefaultSqlFromBool)");
                        task.Param("SoftDeleteObsoleteFiles", "$(EfcptConfigSoftDeleteObsoleteFiles)");
                        task.Param("DiscoverMultipleResultSets", "$(EfcptConfigDiscoverMultipleResultSets)");
                        task.Param("UseAlternateResultSetDiscovery", "$(EfcptConfigUseAlternateResultSetDiscovery)");
                        task.Param("T4TemplatePath", "$(EfcptConfigT4TemplatePath)");
                        task.Param("UseNoNavigations", "$(EfcptConfigUseNoNavigations)");
                        task.Param("MergeDacpacs", "$(EfcptConfigMergeDacpacs)");
                        task.Param("RefreshObjectLists", "$(EfcptConfigRefreshObjectLists)");
                        task.Param("GenerateMermaidDiagram", "$(EfcptConfigGenerateMermaidDiagram)");
                        task.Param("UseDecimalAnnotationForSprocs", "$(EfcptConfigUseDecimalAnnotationForSprocs)");
                        task.Param("UsePrefixNavigationNaming", "$(EfcptConfigUsePrefixNavigationNaming)");
                        task.Param("UseDatabaseNamesForRoutines", "$(EfcptConfigUseDatabaseNamesForRoutines)");
                        task.Param("UseInternalAccessForRoutines", "$(EfcptConfigUseInternalAccessForRoutines)");
                        task.Param("UseDateOnlyTimeOnly", "$(EfcptConfigUseDateOnlyTimeOnly)");
                        task.Param("UseHierarchyId", "$(EfcptConfigUseHierarchyId)");
                        task.Param("UseSpatial", "$(EfcptConfigUseSpatial)");
                        task.Param("UseNodaTime", "$(EfcptConfigUseNodaTime)");
                        task.Param("PreserveCasingWithRegex", "$(EfcptConfigPreserveCasingWithRegex)");
                        task.OutputProperty<SerializedProperties, EfcptSerializedConfigProperties>();
                    });
                });
                t.Target<EfcptComputeFingerprintTarget>( target =>
                {
                    target.DependsOnTargets("EfcptSerializeConfigProperties");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                    target.Task("ComputeFingerprint", task =>
                    {
                        task.Param("DacpacPath", "$(_EfcptDacpacPath)");
                        task.Param("SchemaFingerprint", "$(_EfcptSchemaFingerprint)");
                        task.Param("UseConnectionStringMode", "$(_EfcptUseConnectionString)");
                        task.Param("ConfigPath", "$(_EfcptStagedConfig)");
                        task.Param("RenamingPath", "$(_EfcptStagedRenaming)");
                        task.Param("TemplateDir", "$(_EfcptStagedTemplateDir)");
                        task.Param("FingerprintFile", "$(EfcptFingerprintFile)");
                        task.Param("ToolVersion", "$(EfcptToolVersion)");
                        task.Param("GeneratedDir", "$(EfcptGeneratedDir)");
                        task.Param("DetectGeneratedFileChanges", "$(EfcptDetectGeneratedFileChanges)");
                        task.Param("ConfigPropertyOverrides", "$(_EfcptSerializedConfigProperties)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                        task.OutputProperty<Fingerprint, EfcptFingerprint>();
                        task.OutputProperty<HasChanged, EfcptFingerprintChanged>();
                    });
                });
                t.Target<BeforeEfcptGenerationTarget>( target =>
                {
                    target.DependsOnTargets("EfcptComputeFingerprint");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                });
                t.Target<EfcptGenerateModelsTarget>( target =>
                {
                    target.BeforeTargets("CoreCompile");
                    target.DependsOnTargets("BeforeEfcptGeneration");
                    target.Inputs("$(_EfcptDacpacPath);$(_EfcptStagedConfig);$(_EfcptStagedRenaming)");
                    target.Outputs("$(EfcptStampFile)");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true' and ('$(_EfcptFingerprintChanged)' == 'true' or !Exists('$(EfcptStampFile)'))");
                    target.Task("MakeDir", task =>
                    {
                        task.Param("Directories", "$(EfcptGeneratedDir)");
                    });
                    target.Task("RunEfcpt", task =>
                    {
                        task.Param("ToolMode", "$(EfcptToolMode)");
                        task.Param("ToolPackageId", "$(EfcptToolPackageId)");
                        task.Param("ToolVersion", "$(EfcptToolVersion)");
                        task.Param("ToolRestore", "$(EfcptToolRestore)");
                        task.Param("ToolCommand", "$(EfcptToolCommand)");
                        task.Param("ToolPath", "$(EfcptToolPath)");
                        task.Param("DotNetExe", "$(EfcptDotNetExe)");
                        task.Param("WorkingDirectory", "$(EfcptOutput)");
                        task.Param("DacpacPath", "$(_EfcptDacpacPath)");
                        task.Param("ConnectionString", "$(_EfcptResolvedConnectionString)");
                        task.Param("UseConnectionStringMode", "$(_EfcptUseConnectionString)");
                        task.Param("Provider", "$(EfcptProvider)");
                        task.Param("ConfigPath", "$(_EfcptStagedConfig)");
                        task.Param("RenamingPath", "$(_EfcptStagedRenaming)");
                        task.Param("TemplateDir", "$(_EfcptStagedTemplateDir)");
                        task.Param("OutputDir", "$(EfcptGeneratedDir)");
                        task.Param("TargetFramework", "$(TargetFramework)");
                        task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                    });
                    target.Task("RenameGeneratedFiles", task =>
                    {
                        task.Param("GeneratedDir", "$(EfcptGeneratedDir)");
                        task.Param("LogVerbosity", "$(EfcptLogVerbosity)");
                    });
                    target.Task("WriteLinesToFile", task =>
                    {
                        task.Param("File", "$(EfcptStampFile)");
                        task.Param("Lines", "$(_EfcptFingerprint)");
                        task.Param("Overwrite", "true");
                    });
                });
                t.Target<AfterEfcptGenerationTarget>( target =>
                {
                    target.AfterTargets(new EfcptGenerateModelsTarget());
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                });
                t.Target<EfcptValidateSplitOutputsTarget>( target =>
                {
                    target.DependsOnTargets("EfcptGenerateModels");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true' and '$(EfcptSplitOutputs)' == 'true'");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptDataProjectPath", "$(EfcptDataProject)");
                        group.Property("_EfcptDataProjectPath", "$([System.IO.Path]::GetFullPath($([System.IO.Path]::Combine('$(MSBuildProjectDirectory)', '$(EfcptDataProject)'))))");
                    });
                    target.Error("EfcptSplitOutputs is enabled but EfcptDataProject is not set. Please specify the path to your Data project: <EfcptDataProject>..\\MyProject.Data\\MyProject.Data.csproj</EfcptDataProject>", "'$(_EfcptDataProjectPath)' == ''");
                    target.Error("EfcptDataProject was specified but the file does not exist: $(_EfcptDataProjectPath)", "!Exists('$(_EfcptDataProjectPath)')");
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptDataProjectDir", "$([System.IO.Path]::GetDirectoryName('$(_EfcptDataProjectPath)'))\\");
                        group.Property("_EfcptDataDestDir", "$(_EfcptDataProjectDir)$(EfcptDataProjectOutputSubdir)");
                    });
                    target.Message("Split outputs enabled. DbContext and configurations will be copied to: $(_EfcptDataDestDir)", "high");
                });
                t.Target<EfcptCopyDataToDataProjectTarget>( target =>
                {
                    target.DependsOnTargets("EfcptValidateSplitOutputs");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true' and '$(EfcptSplitOutputs)' == 'true'");
                    target.ItemGroup(null, group =>
                    {
                        group.Include("_EfcptDbContextFiles", "$(EfcptGeneratedDir)*.g.cs");
                    });
                    target.ItemGroup(null, group =>
                    {
                        group.Include("_EfcptConfigurationFiles", "$(EfcptGeneratedDir)*Configuration.g.cs");
                        group.Include("_EfcptConfigurationFiles", "$(EfcptGeneratedDir)Configurations\\**\\*.g.cs");
                    });
                    target.PropertyGroup(null, group =>
                    {
                        group.Property("_EfcptHasFilesToCopy", "true");
                    });
                    target.Task("RemoveDir", task =>
                    {
                        task.Param("Directories", "$(_EfcptDataDestDir)");
                    }, "'$(_EfcptHasFilesToCopy)' == 'true' and Exists('$(_EfcptDataDestDir)')");
                    target.Task("MakeDir", task =>
                    {
                        task.Param("Directories", "$(_EfcptDataDestDir)");
                    }, "'$(_EfcptHasFilesToCopy)' == 'true'");
                    target.Task("MakeDir", task =>
                    {
                        task.Param("Directories", "$(_EfcptDataDestDir)Configurations");
                    }, "'@(_EfcptConfigurationFiles)' != ''");
                    target.Task("Copy", task =>
                    {
                        task.Param("SourceFiles", "@(_EfcptDbContextFiles)");
                        task.Param("DestinationFolder", "$(_EfcptDataDestDir)");
                        task.Param("SkipUnchangedFiles", "true");
                        task.OutputItem("CopiedFiles", "_EfcptCopiedDataFiles");
                    }, "'@(_EfcptDbContextFiles)' != ''");
                    target.Task("Copy", task =>
                    {
                        task.Param("SourceFiles", "@(_EfcptConfigurationFiles)");
                        task.Param("DestinationFolder", "$(_EfcptDataDestDir)Configurations");
                        task.Param("SkipUnchangedFiles", "true");
                        task.OutputItem("CopiedFiles", "_EfcptCopiedDataFiles");
                    }, "'@(_EfcptConfigurationFiles)' != ''");
                    target.Message("Copied @(_EfcptCopiedDataFiles->Count()) data files to Data project: $(_EfcptDataDestDir)", "high", "'@(_EfcptCopiedDataFiles)' != ''");
                    target.Message("Split outputs: No new files to copy (generation was skipped)", "normal", "'$(_EfcptHasFilesToCopy)' != 'true'");
                    target.Task("Delete", task =>
                    {
                        task.Param("Files", "@(_EfcptDbContextFiles)");
                    }, "'@(_EfcptDbContextFiles)' != ''");
                    target.Task("Delete", task =>
                    {
                        task.Param("Files", "@(_EfcptConfigurationFiles)");
                    }, "'@(_EfcptConfigurationFiles)' != ''");
                    target.Message("Removed DbContext and configuration files from Models project", "normal", "'$(_EfcptHasFilesToCopy)' == 'true'");
                });
                t.Target<EfcptAddToCompileTarget>( target =>
                {
                    target.BeforeTargets("CoreCompile");
                    target.DependsOnTargets("EfcptResolveInputs;EfcptUseDirectDacpac;EfcptEnsureDacpac;EfcptStageInputs;EfcptComputeFingerprint;EfcptGenerateModels;EfcptCopyDataToDataProject");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true'");
                    target.ItemGroup(null, group =>
                    {
                        group.Include("Compile", "$(EfcptGeneratedDir)Models\\**\\*.g.cs", null, "'$(EfcptSplitOutputs)' == 'true'");
                        group.Include("Compile", "$(EfcptGeneratedDir)**\\*.g.cs", null, "'$(EfcptSplitOutputs)' != 'true'");
                    });
                });
                t.Target<EfcptIncludeExternalDataTarget>( target =>
                {
                    target.BeforeTargets("CoreCompile");
                    target.Condition("'$(EfcptExternalDataDir)' != '' and Exists('$(EfcptExternalDataDir)')");
                    target.ItemGroup(null, group =>
                    {
                        group.Include("Compile", "$(EfcptExternalDataDir)**\\*.g.cs");
                    });
                    target.Message("Including external data files from: $(EfcptExternalDataDir)", "normal");
                });
                t.Target<EfcptCleanTarget>( target =>
                {
                    target.AfterTargets("Clean");
                    target.Condition("'$(EfcptEnabled)' == 'true'");
                    target.Message("Cleaning efcpt output: $(EfcptOutput)", "normal");
                    target.Task("RemoveDir", task =>
                    {
                        task.Param("Directories", "$(EfcptOutput)");
                    }, "Exists('$(EfcptOutput)')");
                });
                t.Target("_EfcptFinalizeProfiling", target =>
                {
                    target.AfterTargets("Build");
                    target.Condition("'$(EfcptEnabled)' == 'true' and '$(EfcptEnableProfiling)' == 'true'");
                    target.Task("FinalizeBuildProfiling", task =>
                    {
                        task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                        task.Param("OutputPath", "$(EfcptProfilingOutput)");
                        task.Param("BuildSucceeded", "true");
                    });
                });
            })
            .Build();
    }

    // Strongly-typed names
  public readonly struct EfcptDacpacPath : IMsBuildPropertyName
  {
    public string Name => "_EfcptDacpacPath";
  }
  public readonly struct EfcptDatabaseName : IMsBuildPropertyName
  {
    public string Name => "_EfcptDatabaseName";
  }
  public readonly struct EfcptDataDestDir : IMsBuildPropertyName
  {
    public string Name => "_EfcptDataDestDir";
  }
  public readonly struct EfcptDataProjectDir : IMsBuildPropertyName
  {
    public string Name => "_EfcptDataProjectDir";
  }
  public readonly struct EfcptDataProjectPath : IMsBuildPropertyName
  {
    public string Name => "_EfcptDataProjectPath";
  }
  public readonly struct EfcptHasFilesToCopy : IMsBuildPropertyName
  {
    public string Name => "_EfcptHasFilesToCopy";
  }
  public readonly struct EfcptIsSqlProject : IMsBuildPropertyName
  {
    public string Name => "_EfcptIsSqlProject";
  }
  public readonly struct EfcptIsUsingDefaultConfig : IMsBuildPropertyName
  {
    public string Name => "_EfcptIsUsingDefaultConfig";
  }
  public readonly struct EfcptResolvedConfig : IMsBuildPropertyName
  {
    public string Name => "_EfcptResolvedConfig";
  }
  public readonly struct EfcptResolvedRenaming : IMsBuildPropertyName
  {
    public string Name => "_EfcptResolvedRenaming";
  }
  public readonly struct EfcptResolvedTemplateDir : IMsBuildPropertyName
  {
    public string Name => "_EfcptResolvedTemplateDir";
  }
  public readonly struct EfcptScriptsDir : IMsBuildPropertyName
  {
    public string Name => "_EfcptScriptsDir";
  }
  public readonly struct EfcptTaskAssembly : IMsBuildPropertyName
  {
    public string Name => "_EfcptTaskAssembly";
  }
  public readonly struct EfcptTasksFolder : IMsBuildPropertyName
  {
    public string Name => "_EfcptTasksFolder";
  }
  public readonly struct EfcptUseConnectionString : IMsBuildPropertyName
  {
    public string Name => "_EfcptUseConnectionString";
  }
  public readonly struct EfcptUseDirectDacpac : IMsBuildPropertyName
  {
    public string Name => "_EfcptUseDirectDacpac";
  }
  public readonly struct EfcptConfigDbContextName : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigDbContextName";
  }
  public readonly struct EfcptConfigUseNullableReferenceTypes : IMsBuildPropertyName
  {
    public string Name => "EfcptConfigUseNullableReferenceTypes";
  }
    // Item types:
  public readonly struct EfcptConfigurationFilesItem : IMsBuildItemTypeName
  {
    public string Name => "_EfcptConfigurationFiles";
  }
  public readonly struct EfcptDbContextFilesItem : IMsBuildItemTypeName
  {
    public string Name => "_EfcptDbContextFiles";
  }
  public readonly struct EfcptGeneratedScriptsItem : IMsBuildItemTypeName
  {
    public string Name => "_EfcptGeneratedScripts";
  }
  public readonly struct CompileItem : IMsBuildItemTypeName
  {
    public string Name => "Compile";
  }
  public readonly struct EfcptCheckForUpdatesTarget : IMsBuildTargetName
  {
    public string Name => "_EfcptCheckForUpdates";
  }
  public readonly struct EfcptDetectSqlProjectTarget : IMsBuildTargetName
  {
    public string Name => "_EfcptDetectSqlProject";
  }
  public readonly struct EfcptFinalizeProfilingTarget : IMsBuildTargetName
  {
    public string Name => "_EfcptFinalizeProfiling";
  }
  public readonly struct EfcptInitializeProfilingTarget : IMsBuildTargetName
  {
    public string Name => "_EfcptInitializeProfiling";
  }
  public readonly struct EfcptLogTaskAssemblyInfoTarget : IMsBuildTargetName
  {
    public string Name => "_EfcptLogTaskAssemblyInfo";
  }
  public readonly struct AfterEfcptGenerationTarget : IMsBuildTargetName
  {
    public string Name => "AfterEfcptGeneration";
  }
  public readonly struct AfterSqlProjGenerationTarget : IMsBuildTargetName
  {
    public string Name => "AfterSqlProjGeneration";
  }
  public readonly struct BeforeEfcptGenerationTarget : IMsBuildTargetName
  {
    public string Name => "BeforeEfcptGeneration";
  }
  public readonly struct BeforeSqlProjGenerationTarget : IMsBuildTargetName
  {
    public string Name => "BeforeSqlProjGeneration";
  }
  public readonly struct EfcptAddSqlFileWarningsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptAddSqlFileWarnings";
  }
  public readonly struct EfcptAddToCompileTarget : IMsBuildTargetName
  {
    public string Name => "EfcptAddToCompile";
  }
  public readonly struct EfcptApplyConfigOverridesTarget : IMsBuildTargetName
  {
    public string Name => "EfcptApplyConfigOverrides";
  }
  public readonly struct EfcptBuildSqlProjTarget : IMsBuildTargetName
  {
    public string Name => "EfcptBuildSqlProj";
  }
  public readonly struct EfcptCleanTarget : IMsBuildTargetName
  {
    public string Name => "EfcptClean";
  }
  public readonly struct EfcptComputeFingerprintTarget : IMsBuildTargetName
  {
    public string Name => "EfcptComputeFingerprint";
  }
  public readonly struct EfcptCopyDataToDataProjectTarget : IMsBuildTargetName
  {
    public string Name => "EfcptCopyDataToDataProject";
  }
  public readonly struct EfcptEnsureDacpacTarget : IMsBuildTargetName
  {
    public string Name => "EfcptEnsureDacpac";
  }
  public readonly struct EfcptExtractDatabaseSchemaToScriptsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptExtractDatabaseSchemaToScripts";
  }
  public readonly struct EfcptGenerateModelsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptGenerateModels";
  }
  public readonly struct EfcptIncludeExternalDataTarget : IMsBuildTargetName
  {
    public string Name => "EfcptIncludeExternalData";
  }
  public readonly struct EfcptQueryDatabaseSchemaForSqlProjTarget : IMsBuildTargetName
  {
    public string Name => "EfcptQueryDatabaseSchemaForSqlProj";
  }
  public readonly struct EfcptQuerySchemaMetadataTarget : IMsBuildTargetName
  {
    public string Name => "EfcptQuerySchemaMetadata";
  }
  public readonly struct EfcptResolveDbContextNameTarget : IMsBuildTargetName
  {
    public string Name => "EfcptResolveDbContextName";
  }
  public readonly struct EfcptResolveInputsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptResolveInputs";
  }
  public readonly struct EfcptResolveInputsForDirectDacpacTarget : IMsBuildTargetName
  {
    public string Name => "EfcptResolveInputsForDirectDacpac";
  }
  public readonly struct EfcptSerializeConfigPropertiesTarget : IMsBuildTargetName
  {
    public string Name => "EfcptSerializeConfigProperties";
  }
  public readonly struct EfcptStageInputsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptStageInputs";
  }
  public readonly struct EfcptUseDirectDacpacTarget : IMsBuildTargetName
  {
    public string Name => "EfcptUseDirectDacpac";
  }
  public readonly struct EfcptValidateSplitOutputsTarget : IMsBuildTargetName
  {
    public string Name => "EfcptValidateSplitOutputs";
  }
}






