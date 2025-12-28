using JD.Efcpt.Build.Tasks.Config;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that applies property overrides to the staged efcpt-config.json file.
/// </summary>
/// <remarks>
/// <para>
/// This task reads the staged configuration JSON, applies any non-empty MSBuild property
/// overrides, and writes the modified configuration back. It enables users to configure
/// efcpt settings via MSBuild properties without editing JSON files directly.
/// </para>
/// <para>
/// Override behavior:
/// <list type="bullet">
///   <item><description>When using the default config (library-provided): overrides are ALWAYS applied</description></item>
///   <item><description>When using a user-provided config: overrides are only applied if <see cref="ApplyOverrides"/> is true</description></item>
/// </list>
/// </para>
/// <para>
/// Empty or whitespace-only property values are treated as "no override" and the original
/// JSON value is preserved.
/// </para>
/// </remarks>
public sealed class ApplyConfigOverrides : Task
{
    #region Control Properties

    /// <summary>
    /// Path to the staged efcpt-config.json file to modify.
    /// </summary>
    [Required]
    public string StagedConfigPath { get; set; } = "";

    /// <summary>
    /// Whether to apply MSBuild property overrides to user-provided config files.
    /// </summary>
    /// <value>Default is "true". Set to "false" to skip overrides for user-provided configs.</value>
    public string ApplyOverrides { get; set; } = "true";

    /// <summary>
    /// Indicates whether the config file is the library default (not user-provided).
    /// </summary>
    /// <value>When "true", overrides are always applied regardless of <see cref="ApplyOverrides"/>.</value>
    public string IsUsingDefaultConfig { get; set; } = "false";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    #endregion

    #region Names Section Properties

    /// <summary>Root namespace for generated code.</summary>
    public string RootNamespace { get; set; } = "";

    /// <summary>Name of the DbContext class.</summary>
    public string DbContextName { get; set; } = "";

    /// <summary>Namespace for the DbContext class.</summary>
    public string DbContextNamespace { get; set; } = "";

    /// <summary>Namespace for entity model classes.</summary>
    public string ModelNamespace { get; set; } = "";

    #endregion

    #region File Layout Section Properties

    /// <summary>Output path for generated files.</summary>
    public string OutputPath { get; set; } = "";

    /// <summary>Output path for the DbContext file.</summary>
    public string DbContextOutputPath { get; set; } = "";

    /// <summary>Enable split DbContext generation (preview).</summary>
    public string SplitDbContext { get; set; } = "";

    /// <summary>Use schema-based folders for organization (preview).</summary>
    public string UseSchemaFolders { get; set; } = "";

    /// <summary>Use schema-based namespaces (preview).</summary>
    public string UseSchemaNamespaces { get; set; } = "";

    #endregion

    #region Code Generation Section Properties

    /// <summary>Add OnConfiguring method to the DbContext.</summary>
    public string EnableOnConfiguring { get; set; } = "";

    /// <summary>Type of files to generate (all, dbcontext, entities).</summary>
    public string GenerationType { get; set; } = "";

    /// <summary>Use table and column names from the database.</summary>
    public string UseDatabaseNames { get; set; } = "";

    /// <summary>Use DataAnnotation attributes rather than fluent API.</summary>
    public string UseDataAnnotations { get; set; } = "";

    /// <summary>Use nullable reference types.</summary>
    public string UseNullableReferenceTypes { get; set; } = "";

    /// <summary>Pluralize or singularize generated names.</summary>
    public string UseInflector { get; set; } = "";

    /// <summary>Use EF6 Pluralizer instead of Humanizer.</summary>
    public string UseLegacyInflector { get; set; } = "";

    /// <summary>Preserve many-to-many entity instead of skipping.</summary>
    public string UseManyToManyEntity { get; set; } = "";

    /// <summary>Customize code using T4 templates.</summary>
    public string UseT4 { get; set; } = "";

    /// <summary>Customize code using T4 templates including EntityTypeConfiguration.t4.</summary>
    public string UseT4Split { get; set; } = "";

    /// <summary>Remove SQL default from bool columns.</summary>
    public string RemoveDefaultSqlFromBool { get; set; } = "";

    /// <summary>Run cleanup of obsolete files.</summary>
    public string SoftDeleteObsoleteFiles { get; set; } = "";

    /// <summary>Discover multiple result sets from stored procedures (preview).</summary>
    public string DiscoverMultipleResultSets { get; set; } = "";

    /// <summary>Use alternate result set discovery via sp_describe_first_result_set.</summary>
    public string UseAlternateResultSetDiscovery { get; set; } = "";

    /// <summary>Global path to T4 templates.</summary>
    public string T4TemplatePath { get; set; } = "";

    /// <summary>Remove all navigation properties (preview).</summary>
    public string UseNoNavigations { get; set; } = "";

    /// <summary>Merge .dacpac files when using references.</summary>
    public string MergeDacpacs { get; set; } = "";

    /// <summary>Refresh object lists from database during scaffolding.</summary>
    public string RefreshObjectLists { get; set; } = "";

    /// <summary>Create a Mermaid ER diagram during scaffolding.</summary>
    public string GenerateMermaidDiagram { get; set; } = "";

    /// <summary>Use explicit decimal annotation for stored procedure results.</summary>
    public string UseDecimalAnnotationForSprocs { get; set; } = "";

    /// <summary>Use prefix-based naming of navigations (EF Core 8+).</summary>
    public string UsePrefixNavigationNaming { get; set; } = "";

    /// <summary>Use database names for stored procedures and functions.</summary>
    public string UseDatabaseNamesForRoutines { get; set; } = "";

    /// <summary>Use internal access modifiers for stored procedures and functions.</summary>
    public string UseInternalAccessForRoutines { get; set; } = "";

    #endregion

    #region Type Mappings Section Properties

    /// <summary>Map date and time to DateOnly/TimeOnly.</summary>
    public string UseDateOnlyTimeOnly { get; set; } = "";

    /// <summary>Map hierarchyId type.</summary>
    public string UseHierarchyId { get; set; } = "";

    /// <summary>Map spatial columns.</summary>
    public string UseSpatial { get; set; } = "";

    /// <summary>Use NodaTime types.</summary>
    public string UseNodaTime { get; set; } = "";

    #endregion

    #region Replacements Section Properties

    /// <summary>Preserve casing with regex when custom naming.</summary>
    public string PreserveCasingWithRegex { get; set; } = "";

    #endregion

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(ApplyConfigOverrides));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        // Determine if we should apply overrides
        var isDefault = IsUsingDefaultConfig.IsTrue();
        var shouldApply = isDefault || ApplyOverrides.IsTrue();

        if (!shouldApply)
        {
            log.Detail("Skipping config overrides (ApplyOverrides=false and not using default config)");
            return true;
        }

        // Build the override model from MSBuild properties
        var overrides = BuildOverridesModel();

        // Check if there are any overrides to apply
        if (!overrides.HasAnyOverrides())
        {
            log.Detail("No config overrides specified");
            return true;
        }

        // Apply overrides using the applicator
        EfcptConfigOverrideApplicator.Apply(StagedConfigPath, overrides, log);
        return true;
    }

    #region Model Building

    private EfcptConfigOverrides BuildOverridesModel() => new()
    {
        Names = BuildNamesOverrides(),
        FileLayout = BuildFileLayoutOverrides(),
        CodeGeneration = BuildCodeGenerationOverrides(),
        TypeMappings = BuildTypeMappingsOverrides(),
        Replacements = BuildReplacementsOverrides()
    };

    private NamesOverrides? BuildNamesOverrides()
    {
        var o = new NamesOverrides
        {
            RootNamespace = NullIfEmpty(RootNamespace),
            DbContextName = NullIfEmpty(DbContextName),
            DbContextNamespace = NullIfEmpty(DbContextNamespace),
            ModelNamespace = NullIfEmpty(ModelNamespace)
        };

        return HasAnyValue(o.RootNamespace, o.DbContextName, o.DbContextNamespace, o.ModelNamespace) ? o : null;
    }

    private FileLayoutOverrides? BuildFileLayoutOverrides()
    {
        var o = new FileLayoutOverrides
        {
            OutputPath = NullIfEmpty(OutputPath),
            OutputDbContextPath = NullIfEmpty(DbContextOutputPath),
            SplitDbContextPreview = ParseBoolOrNull(SplitDbContext),
            UseSchemaFoldersPreview = ParseBoolOrNull(UseSchemaFolders),
            UseSchemaNamespacesPreview = ParseBoolOrNull(UseSchemaNamespaces)
        };

        return HasAnyValue(o.OutputPath, o.OutputDbContextPath) ||
               HasAnyValue(o.SplitDbContextPreview, o.UseSchemaFoldersPreview, o.UseSchemaNamespacesPreview) ? o : null;
    }

    private CodeGenerationOverrides? BuildCodeGenerationOverrides()
    {
        var o = new CodeGenerationOverrides
        {
            EnableOnConfiguring = ParseBoolOrNull(EnableOnConfiguring),
            Type = NullIfEmpty(GenerationType),
            UseDatabaseNames = ParseBoolOrNull(UseDatabaseNames),
            UseDataAnnotations = ParseBoolOrNull(UseDataAnnotations),
            UseNullableReferenceTypes = ParseBoolOrNull(UseNullableReferenceTypes),
            UseInflector = ParseBoolOrNull(UseInflector),
            UseLegacyInflector = ParseBoolOrNull(UseLegacyInflector),
            UseManyToManyEntity = ParseBoolOrNull(UseManyToManyEntity),
            UseT4 = ParseBoolOrNull(UseT4),
            UseT4Split = ParseBoolOrNull(UseT4Split),
            RemoveDefaultSqlFromBoolProperties = ParseBoolOrNull(RemoveDefaultSqlFromBool),
            SoftDeleteObsoleteFiles = ParseBoolOrNull(SoftDeleteObsoleteFiles),
            DiscoverMultipleStoredProcedureResultsetsPreview = ParseBoolOrNull(DiscoverMultipleResultSets),
            UseAlternateStoredProcedureResultsetDiscovery = ParseBoolOrNull(UseAlternateResultSetDiscovery),
            T4TemplatePath = NullIfEmpty(T4TemplatePath),
            UseNoNavigationsPreview = ParseBoolOrNull(UseNoNavigations),
            MergeDacpacs = ParseBoolOrNull(MergeDacpacs),
            RefreshObjectLists = ParseBoolOrNull(RefreshObjectLists),
            GenerateMermaidDiagram = ParseBoolOrNull(GenerateMermaidDiagram),
            UseDecimalDataAnnotationForSprocResults = ParseBoolOrNull(UseDecimalAnnotationForSprocs),
            UsePrefixNavigationNaming = ParseBoolOrNull(UsePrefixNavigationNaming),
            UseDatabaseNamesForRoutines = ParseBoolOrNull(UseDatabaseNamesForRoutines),
            UseInternalAccessModifiersForSprocsAndFunctions = ParseBoolOrNull(UseInternalAccessForRoutines)
        };

        // Check if any property is set
        return o.EnableOnConfiguring.HasValue || o.Type is not null || o.UseDatabaseNames.HasValue ||
               o.UseDataAnnotations.HasValue || o.UseNullableReferenceTypes.HasValue ||
               o.UseInflector.HasValue || o.UseLegacyInflector.HasValue || o.UseManyToManyEntity.HasValue ||
               o.UseT4.HasValue || o.UseT4Split.HasValue || o.RemoveDefaultSqlFromBoolProperties.HasValue ||
               o.SoftDeleteObsoleteFiles.HasValue || o.DiscoverMultipleStoredProcedureResultsetsPreview.HasValue ||
               o.UseAlternateStoredProcedureResultsetDiscovery.HasValue || o.T4TemplatePath is not null ||
               o.UseNoNavigationsPreview.HasValue || o.MergeDacpacs.HasValue || o.RefreshObjectLists.HasValue ||
               o.GenerateMermaidDiagram.HasValue || o.UseDecimalDataAnnotationForSprocResults.HasValue ||
               o.UsePrefixNavigationNaming.HasValue || o.UseDatabaseNamesForRoutines.HasValue ||
               o.UseInternalAccessModifiersForSprocsAndFunctions.HasValue
            ? o : null;
    }

    private TypeMappingsOverrides? BuildTypeMappingsOverrides()
    {
        var o = new TypeMappingsOverrides
        {
            UseDateOnlyTimeOnly = ParseBoolOrNull(UseDateOnlyTimeOnly),
            UseHierarchyId = ParseBoolOrNull(UseHierarchyId),
            UseSpatial = ParseBoolOrNull(UseSpatial),
            UseNodaTime = ParseBoolOrNull(UseNodaTime)
        };

        return HasAnyValue(o.UseDateOnlyTimeOnly, o.UseHierarchyId, o.UseSpatial, o.UseNodaTime) ? o : null;
    }

    private ReplacementsOverrides? BuildReplacementsOverrides()
    {
        var o = new ReplacementsOverrides
        {
            PreserveCasingWithRegex = ParseBoolOrNull(PreserveCasingWithRegex)
        };

        return o.PreserveCasingWithRegex.HasValue ? o : null;
    }

    #endregion

    #region Helpers

    private static string? NullIfEmpty(string value) =>
        MsBuildPropertyHelpers.NullIfEmpty(value);

    private static bool? ParseBoolOrNull(string value) =>
        MsBuildPropertyHelpers.ParseBoolOrNull(value);

    private static bool HasAnyValue(params string?[] values) =>
        MsBuildPropertyHelpers.HasAnyValue(values);

    private static bool HasAnyValue(params bool?[] values) =>
        MsBuildPropertyHelpers.HasAnyValue(values);

    #endregion
}
