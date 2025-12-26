using System.Text.Json.Serialization;

namespace JD.Efcpt.Build.Tasks.Config;

/// <summary>
/// Represents overrides for efcpt-config.json. Null values mean "no override".
/// </summary>
/// <remarks>
/// <para>
/// This model is designed for use with MSBuild property overrides. Each section
/// corresponds to a section in the efcpt-config.json file. Properties use nullable
/// types where <c>null</c> indicates that the value should not be overridden.
/// </para>
/// <para>
/// The JSON property names are defined via <see cref="JsonPropertyNameAttribute"/>
/// to match the exact keys in the efcpt-config.json schema.
/// </para>
/// </remarks>
public sealed record EfcptConfigOverrides
{
    /// <summary>Custom class and namespace names.</summary>
    [JsonPropertyName("names")]
    public NamesOverrides? Names { get; init; }

    /// <summary>Custom file layout options.</summary>
    [JsonPropertyName("file-layout")]
    public FileLayoutOverrides? FileLayout { get; init; }

    /// <summary>Options for code generation.</summary>
    [JsonPropertyName("code-generation")]
    public CodeGenerationOverrides? CodeGeneration { get; init; }

    /// <summary>Optional type mappings.</summary>
    [JsonPropertyName("type-mappings")]
    public TypeMappingsOverrides? TypeMappings { get; init; }

    /// <summary>Custom naming options.</summary>
    [JsonPropertyName("replacements")]
    public ReplacementsOverrides? Replacements { get; init; }

    /// <summary>Returns true if any section has overrides.</summary>
    public bool HasAnyOverrides() =>
        Names is not null ||
        FileLayout is not null ||
        CodeGeneration is not null ||
        TypeMappings is not null ||
        Replacements is not null;
}

/// <summary>
/// Overrides for the "names" section of efcpt-config.json.
/// </summary>
public sealed record NamesOverrides
{
    /// <summary>Root namespace for generated code.</summary>
    [JsonPropertyName("root-namespace")]
    public string? RootNamespace { get; init; }

    /// <summary>Name of the DbContext class.</summary>
    [JsonPropertyName("dbcontext-name")]
    public string? DbContextName { get; init; }

    /// <summary>Namespace for the DbContext class.</summary>
    [JsonPropertyName("dbcontext-namespace")]
    public string? DbContextNamespace { get; init; }

    /// <summary>Namespace for entity model classes.</summary>
    [JsonPropertyName("model-namespace")]
    public string? ModelNamespace { get; init; }
}

/// <summary>
/// Overrides for the "file-layout" section of efcpt-config.json.
/// </summary>
public sealed record FileLayoutOverrides
{
    /// <summary>Output path for generated files.</summary>
    [JsonPropertyName("output-path")]
    public string? OutputPath { get; init; }

    /// <summary>Output path for the DbContext file.</summary>
    [JsonPropertyName("output-dbcontext-path")]
    public string? OutputDbContextPath { get; init; }

    /// <summary>Enable split DbContext generation (preview).</summary>
    [JsonPropertyName("split-dbcontext-preview")]
    public bool? SplitDbContextPreview { get; init; }

    /// <summary>Use schema-based folders for organization (preview).</summary>
    [JsonPropertyName("use-schema-folders-preview")]
    public bool? UseSchemaFoldersPreview { get; init; }

    /// <summary>Use schema-based namespaces (preview).</summary>
    [JsonPropertyName("use-schema-namespaces-preview")]
    public bool? UseSchemaNamespacesPreview { get; init; }
}

/// <summary>
/// Overrides for the "code-generation" section of efcpt-config.json.
/// </summary>
public sealed record CodeGenerationOverrides
{
    /// <summary>Add OnConfiguring method to the DbContext.</summary>
    [JsonPropertyName("enable-on-configuring")]
    public bool? EnableOnConfiguring { get; init; }

    /// <summary>Type of files to generate (all, dbcontext, entities).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Use table and column names from the database.</summary>
    [JsonPropertyName("use-database-names")]
    public bool? UseDatabaseNames { get; init; }

    /// <summary>Use DataAnnotation attributes rather than fluent API.</summary>
    [JsonPropertyName("use-data-annotations")]
    public bool? UseDataAnnotations { get; init; }

    /// <summary>Use nullable reference types.</summary>
    [JsonPropertyName("use-nullable-reference-types")]
    public bool? UseNullableReferenceTypes { get; init; }

    /// <summary>Pluralize or singularize generated names.</summary>
    [JsonPropertyName("use-inflector")]
    public bool? UseInflector { get; init; }

    /// <summary>Use EF6 Pluralizer instead of Humanizer.</summary>
    [JsonPropertyName("use-legacy-inflector")]
    public bool? UseLegacyInflector { get; init; }

    /// <summary>Preserve many-to-many entity instead of skipping.</summary>
    [JsonPropertyName("use-many-to-many-entity")]
    public bool? UseManyToManyEntity { get; init; }

    /// <summary>Customize code using T4 templates.</summary>
    [JsonPropertyName("use-t4")]
    public bool? UseT4 { get; init; }

    /// <summary>Customize code using T4 templates including EntityTypeConfiguration.t4.</summary>
    [JsonPropertyName("use-t4-split")]
    public bool? UseT4Split { get; init; }

    /// <summary>Remove SQL default from bool columns.</summary>
    [JsonPropertyName("remove-defaultsql-from-bool-properties")]
    public bool? RemoveDefaultSqlFromBoolProperties { get; init; }

    /// <summary>Run cleanup of obsolete files.</summary>
    [JsonPropertyName("soft-delete-obsolete-files")]
    public bool? SoftDeleteObsoleteFiles { get; init; }

    /// <summary>Discover multiple result sets from stored procedures (preview).</summary>
    [JsonPropertyName("discover-multiple-stored-procedure-resultsets-preview")]
    public bool? DiscoverMultipleStoredProcedureResultsetsPreview { get; init; }

    /// <summary>Use alternate result set discovery via sp_describe_first_result_set.</summary>
    [JsonPropertyName("use-alternate-stored-procedure-resultset-discovery")]
    public bool? UseAlternateStoredProcedureResultsetDiscovery { get; init; }

    /// <summary>Global path to T4 templates.</summary>
    [JsonPropertyName("t4-template-path")]
    public string? T4TemplatePath { get; init; }

    /// <summary>Remove all navigation properties (preview).</summary>
    [JsonPropertyName("use-no-navigations-preview")]
    public bool? UseNoNavigationsPreview { get; init; }

    /// <summary>Merge .dacpac files when using references.</summary>
    [JsonPropertyName("merge-dacpacs")]
    public bool? MergeDacpacs { get; init; }

    /// <summary>Refresh object lists from database during scaffolding.</summary>
    [JsonPropertyName("refresh-object-lists")]
    public bool? RefreshObjectLists { get; init; }

    /// <summary>Create a Mermaid ER diagram during scaffolding.</summary>
    [JsonPropertyName("generate-mermaid-diagram")]
    public bool? GenerateMermaidDiagram { get; init; }

    /// <summary>Use explicit decimal annotation for stored procedure results.</summary>
    [JsonPropertyName("use-decimal-data-annotation-for-sproc-results")]
    public bool? UseDecimalDataAnnotationForSprocResults { get; init; }

    /// <summary>Use prefix-based naming of navigations (EF Core 8+).</summary>
    [JsonPropertyName("use-prefix-navigation-naming")]
    public bool? UsePrefixNavigationNaming { get; init; }

    /// <summary>Use database names for stored procedures and functions.</summary>
    [JsonPropertyName("use-database-names-for-routines")]
    public bool? UseDatabaseNamesForRoutines { get; init; }

    /// <summary>Use internal access modifiers for stored procedures and functions.</summary>
    [JsonPropertyName("use-internal-access-modifiers-for-sprocs-and-functions")]
    public bool? UseInternalAccessModifiersForSprocsAndFunctions { get; init; }
}

/// <summary>
/// Overrides for the "type-mappings" section of efcpt-config.json.
/// </summary>
public sealed record TypeMappingsOverrides
{
    /// <summary>Map date and time to DateOnly/TimeOnly.</summary>
    [JsonPropertyName("use-DateOnly-TimeOnly")]
    public bool? UseDateOnlyTimeOnly { get; init; }

    /// <summary>Map hierarchyId type.</summary>
    [JsonPropertyName("use-HierarchyId")]
    public bool? UseHierarchyId { get; init; }

    /// <summary>Map spatial columns.</summary>
    [JsonPropertyName("use-spatial")]
    public bool? UseSpatial { get; init; }

    /// <summary>Use NodaTime types.</summary>
    [JsonPropertyName("use-NodaTime")]
    public bool? UseNodaTime { get; init; }
}

/// <summary>
/// Overrides for the "replacements" section of efcpt-config.json.
/// </summary>
/// <remarks>
/// Only scalar properties are exposed. Array properties (irregular-words,
/// uncountable-words, plural-rules, singular-rules) are not supported via MSBuild.
/// </remarks>
public sealed record ReplacementsOverrides
{
    /// <summary>Preserve casing with regex when custom naming.</summary>
    [JsonPropertyName("preserve-casing-with-regex")]
    public bool? PreserveCasingWithRegex { get; init; }
}
