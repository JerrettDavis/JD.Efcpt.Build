using System.Text;
using System.Text.Json;
using JD.Efcpt.Build.Tasks.Decorators;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that serializes EfcptConfig* property overrides to a JSON string for fingerprinting.
/// </summary>
/// <remarks>
/// This task collects all MSBuild property overrides (EfcptConfig*) and serializes them to a
/// deterministic JSON string. This allows the fingerprinting system to detect when configuration
/// properties change in the .csproj file, triggering regeneration.
/// </remarks>
public sealed class SerializeConfigProperties : Task
{
    /// <summary>
    /// Root namespace override.
    /// </summary>
    public string RootNamespace { get; set; } = "";

    /// <summary>
    /// DbContext name override.
    /// </summary>
    public string DbContextName { get; set; } = "";

    /// <summary>
    /// DbContext namespace override.
    /// </summary>
    public string DbContextNamespace { get; set; } = "";

    /// <summary>
    /// Model namespace override.
    /// </summary>
    public string ModelNamespace { get; set; } = "";

    /// <summary>
    /// Output path override.
    /// </summary>
    public string OutputPath { get; set; } = "";

    /// <summary>
    /// DbContext output path override.
    /// </summary>
    public string DbContextOutputPath { get; set; } = "";

    /// <summary>
    /// Split DbContext override.
    /// </summary>
    public string SplitDbContext { get; set; } = "";

    /// <summary>
    /// Use schema folders override.
    /// </summary>
    public string UseSchemaFolders { get; set; } = "";

    /// <summary>
    /// Use schema namespaces override.
    /// </summary>
    public string UseSchemaNamespaces { get; set; } = "";

    /// <summary>
    /// Enable OnConfiguring override.
    /// </summary>
    public string EnableOnConfiguring { get; set; } = "";

    /// <summary>
    /// Generation type override.
    /// </summary>
    public string GenerationType { get; set; } = "";

    /// <summary>
    /// Use database names override.
    /// </summary>
    public string UseDatabaseNames { get; set; } = "";

    /// <summary>
    /// Use data annotations override.
    /// </summary>
    public string UseDataAnnotations { get; set; } = "";

    /// <summary>
    /// Use nullable reference types override.
    /// </summary>
    public string UseNullableReferenceTypes { get; set; } = "";

    /// <summary>
    /// Use inflector override.
    /// </summary>
    public string UseInflector { get; set; } = "";

    /// <summary>
    /// Use legacy inflector override.
    /// </summary>
    public string UseLegacyInflector { get; set; } = "";

    /// <summary>
    /// Use many-to-many entity override.
    /// </summary>
    public string UseManyToManyEntity { get; set; } = "";

    /// <summary>
    /// Use T4 override.
    /// </summary>
    public string UseT4 { get; set; } = "";

    /// <summary>
    /// Use T4 split override.
    /// </summary>
    public string UseT4Split { get; set; } = "";

    /// <summary>
    /// Remove default SQL from bool override.
    /// </summary>
    public string RemoveDefaultSqlFromBool { get; set; } = "";

    /// <summary>
    /// Soft delete obsolete files override.
    /// </summary>
    public string SoftDeleteObsoleteFiles { get; set; } = "";

    /// <summary>
    /// Discover multiple result sets override.
    /// </summary>
    public string DiscoverMultipleResultSets { get; set; } = "";

    /// <summary>
    /// Use alternate result set discovery override.
    /// </summary>
    public string UseAlternateResultSetDiscovery { get; set; } = "";

    /// <summary>
    /// T4 template path override.
    /// </summary>
    public string T4TemplatePath { get; set; } = "";

    /// <summary>
    /// Use no navigations override.
    /// </summary>
    public string UseNoNavigations { get; set; } = "";

    /// <summary>
    /// Merge dacpacs override.
    /// </summary>
    public string MergeDacpacs { get; set; } = "";

    /// <summary>
    /// Refresh object lists override.
    /// </summary>
    public string RefreshObjectLists { get; set; } = "";

    /// <summary>
    /// Generate Mermaid diagram override.
    /// </summary>
    public string GenerateMermaidDiagram { get; set; } = "";

    /// <summary>
    /// Use decimal annotation for sprocs override.
    /// </summary>
    public string UseDecimalAnnotationForSprocs { get; set; } = "";

    /// <summary>
    /// Use prefix navigation naming override.
    /// </summary>
    public string UsePrefixNavigationNaming { get; set; } = "";

    /// <summary>
    /// Use database names for routines override.
    /// </summary>
    public string UseDatabaseNamesForRoutines { get; set; } = "";

    /// <summary>
    /// Use internal access for routines override.
    /// </summary>
    public string UseInternalAccessForRoutines { get; set; } = "";

    /// <summary>
    /// Use DateOnly/TimeOnly override.
    /// </summary>
    public string UseDateOnlyTimeOnly { get; set; } = "";

    /// <summary>
    /// Use HierarchyId override.
    /// </summary>
    public string UseHierarchyId { get; set; } = "";

    /// <summary>
    /// Use spatial override.
    /// </summary>
    public string UseSpatial { get; set; } = "";

    /// <summary>
    /// Use NodaTime override.
    /// </summary>
    public string UseNodaTime { get; set; } = "";

    /// <summary>
    /// Preserve casing with regex override.
    /// </summary>
    public string PreserveCasingWithRegex { get; set; } = "";

    /// <summary>
    /// Serialized JSON string containing all non-empty property values.
    /// </summary>
    [Output]
    public string SerializedProperties { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(SerializeConfigProperties));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var properties = new Dictionary<string, string>(35, StringComparer.Ordinal);

        // Only include properties that have non-empty values
        AddIfNotEmpty(properties, nameof(RootNamespace), RootNamespace);
        AddIfNotEmpty(properties, nameof(DbContextName), DbContextName);
        AddIfNotEmpty(properties, nameof(DbContextNamespace), DbContextNamespace);
        AddIfNotEmpty(properties, nameof(ModelNamespace), ModelNamespace);
        AddIfNotEmpty(properties, nameof(OutputPath), OutputPath);
        AddIfNotEmpty(properties, nameof(DbContextOutputPath), DbContextOutputPath);
        AddIfNotEmpty(properties, nameof(SplitDbContext), SplitDbContext);
        AddIfNotEmpty(properties, nameof(UseSchemaFolders), UseSchemaFolders);
        AddIfNotEmpty(properties, nameof(UseSchemaNamespaces), UseSchemaNamespaces);
        AddIfNotEmpty(properties, nameof(EnableOnConfiguring), EnableOnConfiguring);
        AddIfNotEmpty(properties, nameof(GenerationType), GenerationType);
        AddIfNotEmpty(properties, nameof(UseDatabaseNames), UseDatabaseNames);
        AddIfNotEmpty(properties, nameof(UseDataAnnotations), UseDataAnnotations);
        AddIfNotEmpty(properties, nameof(UseNullableReferenceTypes), UseNullableReferenceTypes);
        AddIfNotEmpty(properties, nameof(UseInflector), UseInflector);
        AddIfNotEmpty(properties, nameof(UseLegacyInflector), UseLegacyInflector);
        AddIfNotEmpty(properties, nameof(UseManyToManyEntity), UseManyToManyEntity);
        AddIfNotEmpty(properties, nameof(UseT4), UseT4);
        AddIfNotEmpty(properties, nameof(UseT4Split), UseT4Split);
        AddIfNotEmpty(properties, nameof(RemoveDefaultSqlFromBool), RemoveDefaultSqlFromBool);
        AddIfNotEmpty(properties, nameof(SoftDeleteObsoleteFiles), SoftDeleteObsoleteFiles);
        AddIfNotEmpty(properties, nameof(DiscoverMultipleResultSets), DiscoverMultipleResultSets);
        AddIfNotEmpty(properties, nameof(UseAlternateResultSetDiscovery), UseAlternateResultSetDiscovery);
        AddIfNotEmpty(properties, nameof(T4TemplatePath), T4TemplatePath);
        AddIfNotEmpty(properties, nameof(UseNoNavigations), UseNoNavigations);
        AddIfNotEmpty(properties, nameof(MergeDacpacs), MergeDacpacs);
        AddIfNotEmpty(properties, nameof(RefreshObjectLists), RefreshObjectLists);
        AddIfNotEmpty(properties, nameof(GenerateMermaidDiagram), GenerateMermaidDiagram);
        AddIfNotEmpty(properties, nameof(UseDecimalAnnotationForSprocs), UseDecimalAnnotationForSprocs);
        AddIfNotEmpty(properties, nameof(UsePrefixNavigationNaming), UsePrefixNavigationNaming);
        AddIfNotEmpty(properties, nameof(UseDatabaseNamesForRoutines), UseDatabaseNamesForRoutines);
        AddIfNotEmpty(properties, nameof(UseInternalAccessForRoutines), UseInternalAccessForRoutines);
        AddIfNotEmpty(properties, nameof(UseDateOnlyTimeOnly), UseDateOnlyTimeOnly);
        AddIfNotEmpty(properties, nameof(UseHierarchyId), UseHierarchyId);
        AddIfNotEmpty(properties, nameof(UseSpatial), UseSpatial);
        AddIfNotEmpty(properties, nameof(UseNodaTime), UseNodaTime);
        AddIfNotEmpty(properties, nameof(PreserveCasingWithRegex), PreserveCasingWithRegex);

        // Serialize to JSON with sorted keys for deterministic output
        SerializedProperties = JsonSerializer.Serialize(properties.OrderBy(kvp => kvp.Key, StringComparer.Ordinal), JsonOptions);

        return true;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private static void AddIfNotEmpty(Dictionary<string, string> dict, string key, string value) =>
        MsBuildPropertyHelpers.AddIfNotEmpty(dict, key, value);
}
