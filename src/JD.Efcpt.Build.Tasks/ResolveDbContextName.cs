using JD.Efcpt.Build.Tasks.Decorators;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that generates a DbContext name from SQL project, DACPAC, or connection string.
/// </summary>
/// <remarks>
/// <para>
/// This task attempts to generate a meaningful DbContext name using available inputs:
/// <list type="number">
///   <item><description>SQL Project name: Extracts from project file path (e.g., "Database.csproj" → "DatabaseContext")</description></item>
///   <item><description>DACPAC filename: Humanizes the filename (e.g., "Our_Database20251225.dacpac" → "OurDatabaseContext")</description></item>
///   <item><description>Connection String: Extracts database name (e.g., "Database=myDb" → "MyDbContext")</description></item>
/// </list>
/// </para>
/// <para>
/// The task only sets <see cref="ResolvedDbContextName"/> if:
/// <list type="bullet">
///   <item><description><see cref="ExplicitDbContextName"/> is not provided (user override)</description></item>
///   <item><description>A name can be successfully resolved from available inputs</description></item>
/// </list>
/// Otherwise, it returns the fallback name "MyDbContext".
/// </para>
/// </remarks>
public sealed class ResolveDbContextName : Task
{
    /// <summary>
    /// Full path to the MSBuild project file (used for profiling).
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Explicit DbContext name provided by the user (highest priority).
    /// </summary>
    /// <remarks>
    /// When set, this value is returned directly without any generation logic.
    /// This allows users to explicitly override the auto-generated name.
    /// </remarks>
    [ProfileInput]
    public string ExplicitDbContextName { get; set; } = "";

    /// <summary>
    /// Full path to the SQL project file.
    /// </summary>
    /// <remarks>
    /// Used as the first source for name generation. The project filename
    /// (without extension) is humanized into a context name.
    /// </remarks>
    [ProfileInput]
    public string SqlProjPath { get; set; } = "";

    /// <summary>
    /// Full path to the DACPAC file.
    /// </summary>
    /// <remarks>
    /// Used as the second source for name generation. The DACPAC filename
    /// (without extension and special characters) is humanized into a context name.
    /// </remarks>
    [ProfileInput]
    public string DacpacPath { get; set; } = "";

    /// <summary>
    /// Database connection string.
    /// </summary>
    /// <remarks>
    /// Used as the third source for name generation. The database name is
    /// extracted from the connection string and humanized into a context name.
    /// </remarks>
    [ProfileInput(Exclude = true)] // Excluded for security
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Redacted connection string for profiling (only included if ConnectionString is set).
    /// </summary>
    [ProfileInput(Name = "ConnectionString")]
    private string ConnectionStringRedacted => string.IsNullOrWhiteSpace(ConnectionString) ? "" : "<redacted>";

    /// <summary>
    /// Controls whether to use connection string mode for generation.
    /// </summary>
    /// <remarks>
    /// When "true", the connection string is preferred over SQL project path.
    /// When "false", SQL project path takes precedence.
    /// </remarks>
    [ProfileInput]
    public string UseConnectionStringMode { get; set; } = "false";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// The resolved DbContext name.
    /// </summary>
    /// <remarks>
    /// Contains either:
    /// <list type="bullet">
    ///   <item><description>The <see cref="ExplicitDbContextName"/> if provided</description></item>
    ///   <item><description>A generated name from SQL project, DACPAC, or connection string</description></item>
    ///   <item><description>The default "MyDbContext" if unable to resolve</description></item>
    /// </list>
    /// </remarks>
    [Output]
    public string ResolvedDbContextName { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
        => TaskExecutionDecorator.ExecuteWithProfiling(
            this, ExecuteCore, ProfilingHelper.GetProfiler(ProjectPath));

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        // Priority 0: Use explicit override if provided
        if (!string.IsNullOrWhiteSpace(ExplicitDbContextName))
        {
            ResolvedDbContextName = ExplicitDbContextName;
            log.Detail($"Using explicit DbContext name: {ResolvedDbContextName}");
            return true;
        }

        // Generate name based on available inputs
        var useConnectionString = UseConnectionStringMode.Equals("true", StringComparison.OrdinalIgnoreCase);
        
        string? generatedName;
        if (useConnectionString)
        {
            // Connection string mode: prioritize connection string, then DACPAC
            generatedName = DbContextNameGenerator.Generate(
                sqlProjPath: null,
                dacpacPath: DacpacPath,
                connectionString: ConnectionString);
            
            log.Detail($"Generated DbContext name from connection string mode: {generatedName}");
        }
        else
        {
            // SQL Project mode: prioritize SQL project, then DACPAC, then connection string
            generatedName = DbContextNameGenerator.Generate(
                sqlProjPath: SqlProjPath,
                dacpacPath: DacpacPath,
                connectionString: ConnectionString);
            
            log.Detail($"Generated DbContext name from SQL project mode: {generatedName}");
        }

        ResolvedDbContextName = generatedName;
        
        if (generatedName != "MyDbContext")
        {
            log.Info($"Auto-generated DbContext name: {generatedName}");
        }
        else
        {
            log.Detail("Using default DbContext name: MyDbContext");
        }

        return true;
    }
}
