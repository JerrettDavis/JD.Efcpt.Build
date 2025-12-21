using System.Text.Json;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Schema;
using Microsoft.Build.Framework;
using Microsoft.Data.SqlClient;
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
    /// Database connection string.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Output directory for diagnostic files.
    /// </summary>
    [Required]
    public string OutputDir { get; set; } = "";

    /// <summary>
    /// Database provider type (mssql, postgresql, mysql, mariadb).
    /// </summary>
    /// <remarks>
    /// Phase 1 only supports mssql (SQL Server).
    /// </remarks>
    public string Provider { get; set; } = "mssql";

    /// <summary>
    /// Logging verbosity level.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Computed schema fingerprint (output).
    /// </summary>
    [Output]
    public string SchemaFingerprint { get; set; } = "";

    /// <inheritdoc/>
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(QuerySchemaMetadata));
        return decorator.Execute(in ctx);
    }

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        try
        {
            // Validate connection
            ValidateConnection(ConnectionString, log);

            // Select schema reader based on provider
            var reader = Provider.ToLowerInvariant() switch
            {
                "mssql" or "sqlserver" => new SqlServerSchemaReader(),
                _ => throw new NotSupportedException($"Database provider '{Provider}' is not supported. Phase 1 supports 'mssql' only.")
            };

            log.Detail($"Reading schema metadata from {Provider} database...");
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

    private static void ValidateConnection(string connectionString, BuildLog log)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            connection.OpenAsync(cts.Token).GetAwaiter().GetResult();
            log.Detail("Database connection validated successfully.");
        }
        catch (Exception ex)
        {
            log.Error("JD0013",
                $"Failed to connect to database: {ex.Message}. Verify server accessibility and credentials.");
            throw;
        }
    }
}
