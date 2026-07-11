using System.Text.Json;
using JD.Efcpt.Build.Core.Providers;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Schema;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that queries database schema metadata and computes a deterministic fingerprint.
/// </summary>
/// <remarks>
/// <para>
/// This task connects to a database using the provided connection string, reads the complete
/// schema metadata (tables, columns, indexes, constraints), and computes a fingerprint using
/// XxHash64 for change detection in incremental builds.
/// </para>
/// <para>
/// The task optionally writes a <c>schema-model.json</c> file to <see cref="OutputDir"/> for
/// diagnostics and debugging purposes.
/// </para>
/// </remarks>
public sealed class QuerySchemaMetadata : Task
{
    /// <summary>
    /// Full path to the MSBuild project file (used for profiling).
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Database connection string.
    /// </summary>
    [Required]
    [ProfileInput(Exclude = true)] // Excluded for security
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Redacted connection string for profiling (only included if ConnectionString is set).
    /// </summary>
    [ProfileInput(Name = "ConnectionString")]
    private string ConnectionStringRedacted => string.IsNullOrWhiteSpace(ConnectionString) ? "" : "<redacted>";

    /// <summary>
    /// Output directory for diagnostic files.
    /// </summary>
    [Required]
    [ProfileInput]
    public string OutputDir { get; set; } = "";

    /// <summary>
    /// Database provider type.
    /// </summary>
    /// <remarks>
    /// Supported providers: mssql, postgres, mysql, sqlite, oracle, firebird, snowflake.
    /// </remarks>
    [ProfileInput]
    public string Provider { get; set; } = "mssql";

    /// <summary>
    /// Logging verbosity level.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Additional directories to search for a satellite provider package's adapter assembly
    /// (e.g. <c>JD.Efcpt.Build.PostgreSQL.dll</c>), beyond the bundled <c>providers/{provider}/</c>
    /// folder next to the task assembly. Populated from the consuming project's
    /// <c>@(EfcptProviderSearchPath)</c> item group by <c>JD.Efcpt.Build.targets</c>; a satellite
    /// provider package's own <c>build/</c> props file appends its deployed provider directory to
    /// that item group when installed. Ignored for the bundled <c>mssql</c> provider.
    /// </summary>
    [ProfileInput]
    public string[] ProviderSearchPaths { get; set; } = [];

    /// <summary>
    /// Registers custom database providers (#184's <c>customProviders</c> plugin registry),
    /// loaded via the same satellite-package assembly-resolution machinery as built-in providers
    /// (see <see cref="ProviderSearchPaths"/> / #189).
    /// </summary>
    /// <remarks>
    /// Populated from the consuming project's <c>@(EfcptCustomProvider)</c> item group. Each
    /// item's identity is the custom provider key (the value <see cref="Provider"/> is set to, to
    /// select it); its <c>AssemblyName</c> metadata is the simple assembly name (without
    /// directory or <c>.dll</c> extension) containing the provider's <see cref="IProviderAdapter"/>;
    /// its optional <c>SearchPath</c> metadata is an additional directory to search for that
    /// assembly, appended to <see cref="ProviderSearchPaths"/>. A custom provider key that
    /// collides with a built-in provider key or alias fails the build with <c>JD0019</c>. Using a
    /// registered custom provider key as <see cref="Provider"/> requires
    /// <see cref="AllowCustomProviders"/> to be enabled - see the remarks there.
    /// </remarks>
    [ProfileInput]
    public ITaskItem[] EfcptCustomProviders { get; set; } = [];

    /// <summary>
    /// Security opt-in gate for custom database providers registered via
    /// <see cref="EfcptCustomProviders"/> (#184).
    /// </summary>
    /// <remarks>
    /// Custom providers load and execute third-party code at build time. This is fail-closed
    /// (disabled) by default: if <see cref="Provider"/> resolves to a registered custom provider
    /// key while this is not <see langword="true"/>, the task fails fast with <c>JD0017</c>
    /// before any custom provider assembly is loaded. When enabled and a custom provider is
    /// actually used, a build warning is logged noting that third-party code executes at build
    /// time. Has no effect on built-in providers, which are never gated.
    /// </remarks>
    [ProfileInput]
    public bool AllowCustomProviders { get; set; }

    /// <summary>
    /// Computed schema fingerprint (output).
    /// </summary>
    [Output]
    public string SchemaFingerprint { get; set; } = "";

    /// <inheritdoc/>
    public override bool Execute()
        => TaskExecutionDecorator.ExecuteWithProfiling(
            this, ExecuteCore, ProfilingHelper.GetProfiler(ProjectPath));

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        try
        {
            // #184: build the custom provider registry and enforce the security gate BEFORE any
            // assembly (built-in or custom) is loaded.
            var customProviderAssemblies = BuildCustomProviderAssemblyMap(EfcptCustomProviders);
            var effectiveSearchPaths = CombineSearchPaths(ProviderSearchPaths, EfcptCustomProviders);

            CheckForBuiltInCollisions(EfcptCustomProviders);

            var providerIsBuiltIn = TryNormalizeBuiltIn(Provider, out _);
            var isCustomProviderSelected = !providerIsBuiltIn
                && !string.IsNullOrWhiteSpace(Provider)
                && customProviderAssemblies.Keys.Any(k => string.Equals(k, Provider, StringComparison.OrdinalIgnoreCase));

            if (isCustomProviderSelected && !AllowCustomProviders)
            {
                throw new CustomProviderException(
                    CustomProviderException.NotAllowedCode,
                    $"Provider '{Provider}' is a custom provider. Custom providers load and execute " +
                    "third-party code at build time and are disabled by default. Set " +
                    "<EfcptAllowCustomProviders>true</EfcptAllowCustomProviders> to enable.");
            }

            if (isCustomProviderSelected)
            {
                log.Warn($"Provider '{Provider}' is a custom provider. Its assembly executes " +
                         "third-party code at build time - only enable custom providers you trust.");
            }

            // Normalize and validate provider
            var normalizedProvider = DatabaseProviderFactory.NormalizeProvider(Provider, customProviderAssemblies.Keys.ToArray());
            var providerDisplayName = providerIsBuiltIn
                ? DatabaseProviderFactory.GetProviderDisplayName(normalizedProvider)
                : $"custom provider '{normalizedProvider}'";

            // Validate connection using the appropriate provider
            ValidateConnection(normalizedProvider, ConnectionString, effectiveSearchPaths, customProviderAssemblies, log);

            // Create schema reader for the provider
            var reader = DatabaseProviderFactory.CreateSchemaReader(normalizedProvider, effectiveSearchPaths, customProviderAssemblies);

            log.Detail($"Reading schema metadata from {providerDisplayName} database...");
            var schema = reader.ReadSchema(ConnectionString);

            log.Detail($"Schema read: {schema.Tables.Count} tables");

            // Compute fingerprint
            SchemaFingerprint = SchemaFingerprinter.ComputeFingerprint(schema);
            log.Detail($"Schema fingerprint: {SchemaFingerprint}");

            if (ctx.Logger.HasLoggedErrors)
                return true;

            // Write schema model to disk for diagnostics
            Directory.CreateDirectory(OutputDir);
            var schemaPath = Path.Combine(OutputDir, "schema-model.json");
            var json = JsonSerializer.Serialize(schema, _jsonSerializerOptions);
            File.WriteAllText(schemaPath, json);
            log.Detail($"Schema model written to: {schemaPath}");

            return true;
        }
        catch (CustomProviderException ex)
        {
            log.Error(ex.Code, ex.Message);
            return false;
        }
        catch (NotSupportedException ex)
        {
            log.Error("JD0014", $"Failed to query database schema metadata: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            log.Error("JD0014", $"Failed to query database schema metadata: {ex.Message}");
            return false;
        }
    }

    private static void ValidateConnection(
        string provider,
        string connectionString,
        IReadOnlyList<string> providerSearchPaths,
        IReadOnlyDictionary<string, string> customProviderAssemblies,
        BuildLog log)
    {
        try
        {
            using var connection = DatabaseProviderFactory.CreateConnection(
                provider, connectionString, providerSearchPaths, customProviderAssemblies);
            connection.Open();
            log.Detail("Database connection validated successfully.");
        }
        catch (CustomProviderException)
        {
            // Let ExecuteCore's catch surface this with its own JD-coded (JD0018/JD0040) message
            // instead of being masked by the generic JD0013 connection-failure wording below.
            throw;
        }
        catch (Exception ex)
        {
            log.Error("JD0013",
                $"Failed to connect to database: {ex.Message}. Verify server accessibility and credentials.");
            throw;
        }
    }

    /// <summary>
    /// Builds the custom provider key -&gt; simple assembly name map from
    /// <see cref="EfcptCustomProviders"/>, keyed case-insensitively. Items with no
    /// <c>AssemblyName</c> metadata are skipped.
    /// </summary>
    private static IReadOnlyDictionary<string, string> BuildCustomProviderAssemblyMap(ITaskItem[] items)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var key = item.ItemSpec.Trim();
            if (key.Length == 0)
                continue;

            var assemblyName = item.GetMetadata("AssemblyName");
            if (!string.IsNullOrWhiteSpace(assemblyName))
                map[key] = assemblyName;
        }

        return map;
    }

    /// <summary>
    /// Combines <paramref name="providerSearchPaths"/> with each custom provider item's optional
    /// <c>SearchPath</c> metadata.
    /// </summary>
    private static string[] CombineSearchPaths(string[] providerSearchPaths, ITaskItem[] customProviders)
    {
        var extraSearchPaths = customProviders
            .Select(i => i.GetMetadata("SearchPath"))
            .Where(p => !string.IsNullOrWhiteSpace(p));

        return [.. providerSearchPaths, .. extraSearchPaths];
    }

    /// <summary>
    /// Throws <see cref="CustomProviderException"/> (<see cref="CustomProviderException.CollidesWithBuiltInCode"/>,
    /// <c>JD0019</c>) if any <see cref="EfcptCustomProviders"/> item's identity collides
    /// (case-insensitively) with a built-in provider key or alias.
    /// </summary>
    private static void CheckForBuiltInCollisions(ITaskItem[] customProviders)
    {
        foreach (var item in customProviders)
        {
            var key = item.ItemSpec.Trim();
            if (key.Length == 0)
                continue;

            if (TryNormalizeBuiltIn(key, out var canonical))
            {
                throw new CustomProviderException(
                    CustomProviderException.CollidesWithBuiltInCode,
                    $"Custom provider key '{key}' collides with the built-in provider '{canonical}'. " +
                    "Choose a different key for your custom provider.");
            }
        }
    }

    /// <summary>
    /// Attempts to normalize <paramref name="provider"/> as a built-in provider alias only -
    /// never admits a custom provider key. Returns <see langword="false"/> (without throwing) for
    /// both an unrecognized alias and a null/empty/whitespace input, so callers can use this as a
    /// pure "is this a built-in provider" check without having to special-case empty input
    /// themselves.
    /// </summary>
    private static bool TryNormalizeBuiltIn(string provider, out string canonical)
    {
        canonical = "";
        if (string.IsNullOrWhiteSpace(provider))
            return false;

        try
        {
            canonical = ProviderNames.Normalize(provider);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
