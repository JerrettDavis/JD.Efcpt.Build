using JD.Efcpt.Build.Definitions.Constants;
using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions.Builders;

/// <summary>
/// Eliminates repetitive task.Param calls by mapping common parameter patterns.
/// </summary>
public class TaskParameterMapper
{
    private readonly TaskInvocationBuilder _task;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskParameterMapper"/> class.
    /// </summary>
    /// <param name="task">The underlying task builder.</param>
    public TaskParameterMapper(TaskInvocationBuilder task)
    {
        _task = task;
    }

    /// <summary>
    /// Maps all 38 EfcptConfig* properties to their corresponding task parameters.
    /// This eliminates the repetitive pattern found in ApplyConfigOverrides and SerializeConfigProperties targets.
    /// </summary>
    public TaskParameterMapper WithAllConfigOverrides()
    {
        _task.Param(TaskParameters.RootNamespace, MsBuildExpressions.Property(EfcptProperties.EfcptConfigRootNamespace));
        _task.Param(TaskParameters.DbContextName, MsBuildExpressions.Property(EfcptProperties.EfcptConfigDbContextName));
        _task.Param(TaskParameters.DbContextNamespace, MsBuildExpressions.Property(EfcptProperties.EfcptConfigDbContextNamespace));
        _task.Param(TaskParameters.ModelNamespace, MsBuildExpressions.Property(EfcptProperties.EfcptConfigModelNamespace));
        _task.Param(TaskParameters.OutputPath, MsBuildExpressions.Property(EfcptProperties.EfcptConfigOutputPath));
        _task.Param(TaskParameters.DbContextOutputPath, MsBuildExpressions.Property(EfcptProperties.EfcptConfigDbContextOutputPath));
        _task.Param(TaskParameters.SplitDbContext, MsBuildExpressions.Property(EfcptProperties.EfcptConfigSplitDbContext));
        _task.Param(TaskParameters.UseSchemaFolders, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseSchemaFolders));
        _task.Param(TaskParameters.UseSchemaNamespaces, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseSchemaNamespaces));
        _task.Param(TaskParameters.EnableOnConfiguring, MsBuildExpressions.Property(EfcptProperties.EfcptConfigEnableOnConfiguring));
        _task.Param(TaskParameters.GenerationType, MsBuildExpressions.Property(EfcptProperties.EfcptConfigGenerationType));
        _task.Param(TaskParameters.UseDatabaseNames, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseDatabaseNames));
        _task.Param(TaskParameters.UseDataAnnotations, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseDataAnnotations));
        _task.Param(TaskParameters.UseNullableReferenceTypes, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseNullableReferenceTypes));
        _task.Param(TaskParameters.UseInflector, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseInflector));
        _task.Param(TaskParameters.UseLegacyInflector, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseLegacyInflector));
        _task.Param(TaskParameters.UseManyToManyEntity, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseManyToManyEntity));
        _task.Param(TaskParameters.UseT4, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseT4));
        _task.Param(TaskParameters.UseT4Split, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseT4Split));
        _task.Param(TaskParameters.RemoveDefaultSqlFromBool, MsBuildExpressions.Property(EfcptProperties.EfcptConfigRemoveDefaultSqlFromBool));
        _task.Param(TaskParameters.SoftDeleteObsoleteFiles, MsBuildExpressions.Property(EfcptProperties.EfcptConfigSoftDeleteObsoleteFiles));
        _task.Param(TaskParameters.DiscoverMultipleResultSets, MsBuildExpressions.Property(EfcptProperties.EfcptConfigDiscoverMultipleResultSets));
        _task.Param(TaskParameters.UseAlternateResultSetDiscovery, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseAlternateResultSetDiscovery));
        _task.Param(TaskParameters.T4TemplatePath, MsBuildExpressions.Property(EfcptProperties.EfcptConfigT4TemplatePath));
        _task.Param(TaskParameters.UseNoNavigations, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseNoNavigations));
        _task.Param(TaskParameters.MergeDacpacs, MsBuildExpressions.Property(EfcptProperties.EfcptConfigMergeDacpacs));
        _task.Param(TaskParameters.RefreshObjectLists, MsBuildExpressions.Property(EfcptProperties.EfcptConfigRefreshObjectLists));
        _task.Param(TaskParameters.GenerateMermaidDiagram, MsBuildExpressions.Property(EfcptProperties.EfcptConfigGenerateMermaidDiagram));
        _task.Param(TaskParameters.UseDecimalAnnotationForSprocs, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseDecimalAnnotationForSprocs));
        _task.Param(TaskParameters.UsePrefixNavigationNaming, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUsePrefixNavigationNaming));
        _task.Param(TaskParameters.UseDatabaseNamesForRoutines, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseDatabaseNamesForRoutines));
        _task.Param(TaskParameters.UseInternalAccessForRoutines, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseInternalAccessForRoutines));
        _task.Param(TaskParameters.UseDateOnlyTimeOnly, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseDateOnlyTimeOnly));
        _task.Param(TaskParameters.UseHierarchyId, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseHierarchyId));
        _task.Param(TaskParameters.UseSpatial, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseSpatial));
        _task.Param(TaskParameters.UseNodaTime, MsBuildExpressions.Property(EfcptProperties.EfcptConfigUseNodaTime));
        _task.Param(TaskParameters.PreserveCasingWithRegex, MsBuildExpressions.Property(EfcptProperties.EfcptConfigPreserveCasingWithRegex));
        return this;
    }

    /// <summary>
    /// Maps common project context parameters: MSBuildProjectFullPath, MSBuildProjectName, Configuration, TargetFramework.
    /// </summary>
    public TaskParameterMapper WithProjectContext()
    {
        _task.Param(TaskParameters.ProjectPath, MsBuildExpressions.Property(MsBuildProperties.MSBuildProjectFullPath));
        _task.Param(TaskParameters.ProjectName, MsBuildExpressions.Property(MsBuildProperties.MSBuildProjectName));
        _task.Param(TaskParameters.Configuration, MsBuildExpressions.Property(MsBuildProperties.Configuration));
        _task.Param(TaskParameters.TargetFramework, MsBuildExpressions.Property(MsBuildProperties.TargetFramework));
        return this;
    }

    /// <summary>
    /// Maps input file parameters: _EfcptResolvedConfig, _EfcptResolvedRenaming, _EfcptResolvedTemplateDir.
    /// </summary>
    public TaskParameterMapper WithInputFiles()
    {
        _task.Param(TaskParameters.ConfigPath, MsBuildExpressions.Property(EfcptProperties._EfcptResolvedConfig));
        _task.Param(TaskParameters.RenamingPath, MsBuildExpressions.Property(EfcptProperties._EfcptResolvedRenaming));
        _task.Param(TaskParameters.TemplateDir, MsBuildExpressions.Property(EfcptProperties._EfcptResolvedTemplateDir));
        return this;
    }

    /// <summary>
    /// Maps output parameters: EfcptOutput, EfcptLogVerbosity.
    /// </summary>
    public TaskParameterMapper WithOutput()
    {
        _task.Param(TaskParameters.OutputDir, MsBuildExpressions.Property(EfcptProperties.EfcptOutput));
        _task.Param(TaskParameters.LogVerbosity, MsBuildExpressions.Property(EfcptProperties.EfcptLogVerbosity));
        return this;
    }

    /// <summary>
    /// Maps database connection parameters: EfcptConnectionString, EfcptProvider.
    /// </summary>
    public TaskParameterMapper WithDatabaseConnection()
    {
        _task.Param(TaskParameters.ConnectionString, MsBuildExpressions.Property(EfcptProperties.EfcptConnectionString));
        _task.Param(TaskParameters.Provider, MsBuildExpressions.Property(EfcptProperties.EfcptProvider));
        return this;
    }

    /// <summary>
    /// Maps DACPAC parameters: _EfcptDacpacPath, _EfcptSqlProj.
    /// </summary>
    public TaskParameterMapper WithDacpac()
    {
        _task.Param(TaskParameters.DacpacPath, MsBuildExpressions.Property(EfcptProperties._EfcptDacpacPath));
        _task.Param(TaskParameters.SqlProjectPath, MsBuildExpressions.Property(EfcptProperties._EfcptSqlProj));
        return this;
    }

    /// <summary>
    /// Maps staged file parameters: _EfcptStagedConfig, _EfcptStagedRenaming, _EfcptStagedTemplateDir.
    /// Used when tasks need to reference files that have been copied to the output directory.
    /// </summary>
    public TaskParameterMapper WithStagedFiles()
    {
        _task.Param(TaskParameters.ConfigPath, MsBuildExpressions.Property(EfcptProperties._EfcptStagedConfig));
        _task.Param(TaskParameters.RenamingPath, MsBuildExpressions.Property(EfcptProperties._EfcptStagedRenaming));
        _task.Param(TaskParameters.TemplateDir, MsBuildExpressions.Property(EfcptProperties._EfcptStagedTemplateDir));
        return this;
    }

    /// <summary>
    /// Maps tool execution parameters: ToolMode, ToolPackageId, ToolVersion, ToolRestore, ToolCommand, ToolPath, DotNetExe.
    /// </summary>
    public TaskParameterMapper WithToolConfiguration()
    {
        _task.Param(TaskParameters.ToolMode, MsBuildExpressions.Property(EfcptProperties.EfcptToolMode));
        _task.Param(TaskParameters.ToolPackageId, MsBuildExpressions.Property(EfcptProperties.EfcptToolPackageId));
        _task.Param(TaskParameters.ToolVersion, MsBuildExpressions.Property(EfcptProperties.EfcptToolVersion));
        _task.Param(TaskParameters.ToolRestore, MsBuildExpressions.Property(EfcptProperties.EfcptToolRestore));
        _task.Param(TaskParameters.ToolCommand, MsBuildExpressions.Property(EfcptProperties.EfcptToolCommand));
        _task.Param(TaskParameters.ToolPath, MsBuildExpressions.Property(EfcptProperties.EfcptToolPath));
        _task.Param(TaskParameters.DotNetExe, MsBuildExpressions.Property(EfcptProperties.EfcptDotNetExe));
        return this;
    }

    /// <summary>
    /// Maps resolved connection string and mode: _EfcptResolvedConnectionString, _EfcptUseConnectionString.
    /// Used when tasks need the connection string that was resolved during input resolution.
    /// </summary>
    public TaskParameterMapper WithResolvedConnection()
    {
        _task.Param(TaskParameters.ConnectionString, MsBuildExpressions.Property(EfcptProperties._EfcptResolvedConnectionString));
        _task.Param(TaskParameters.UseConnectionStringMode, MsBuildExpressions.Property(EfcptProperties._EfcptUseConnectionString));
        _task.Param(TaskParameters.Provider, MsBuildExpressions.Property(EfcptProperties.EfcptProvider));
        return this;
    }

    /// <summary>
    /// Maps parameters for MSBuild task invocation.
    /// </summary>
    public TaskParameterMapper WithMsBuildInvocation()
    {
        _task.Param(TaskParameters.Projects, MsBuildExpressions.Property(EfcptProperties._EfcptSqlProj));
        _task.Param(TaskParameters.Targets, MsBuildTargets.Build);
        _task.Param(TaskParameters.Properties, PropertyValues.Configuration);
        _task.Param(TaskParameters.BuildInParallel, PropertyValues.False);
        return this;
    }

    /// <summary>
    /// Maps parameters for file operations.
    /// </summary>
    public TaskParameterMapper WithFileOperation(string sourceProperty, string destProperty)
    {
        _task.Param(TaskParameters.SkipUnchangedFiles, PropertyValues.True);
        return this;
    }

    /// <summary>
    /// Maps parameters for directory operations.
    /// </summary>
    public TaskParameterMapper WithDirectoryOperation(string dirProperty)
    {
        _task.Param(TaskParameters.Directories, MsBuildExpressions.Property(dirProperty));
        return this;
    }

    /// <summary>
    /// Returns the underlying task builder.
    /// </summary>
    public TaskInvocationBuilder Build()
    {
        return _task;
    }
}
