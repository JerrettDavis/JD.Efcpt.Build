namespace JD.Efcpt.Build.Core.Providers;

/// <summary>
/// Normalizes database provider identifiers/aliases to their canonical short names, and maps
/// canonical names to human-readable display names.
/// </summary>
/// <remarks>
/// Extracted from <c>JD.Efcpt.Build.Tasks.Schema.DatabaseProviderFactory</c> so the jd-efcpt CLI
/// (which needs provider normalization for <c>init --provider</c> but has no need for the
/// adapter-coupled <c>CreateConnection</c>/<c>CreateSchemaReader</c> machinery) can share the
/// exact same list of supported providers and aliases without depending on
/// <c>JD.Efcpt.Build.Tasks</c>.
/// </remarks>
public static class ProviderNames
{
    /// <summary>
    /// Normalizes a provider identifier (any recognized alias) to its canonical short name.
    /// </summary>
    /// <param name="provider">The provider name (any recognized alias).</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="provider"/> is not a recognized provider alias.
    /// </exception>
    public static string Normalize(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(provider));

        return provider.ToLowerInvariant() switch
        {
            "mssql" or "sqlserver" or "sql-server" => "mssql",
            "postgres" or "postgresql" or "pgsql" => "postgres",
            "mysql" or "mariadb" => "mysql",
            "sqlite" or "sqlite3" => "sqlite",
            "oracle" or "oracledb" => "oracle",
            "firebird" or "fb" => "firebird",
            "snowflake" or "sf" => "snowflake",
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported. " +
                "Supported providers: mssql, postgres, mysql, sqlite, oracle, firebird, snowflake")
        };
    }

    /// <summary>
    /// Gets the human-readable display name for a provider (any recognized alias).
    /// </summary>
    public static string GetDisplayName(string provider)
    {
        var normalized = Normalize(provider);

        return normalized switch
        {
            "mssql" => "SQL Server",
            "postgres" => "PostgreSQL",
            "mysql" => "MySQL/MariaDB",
            "sqlite" => "SQLite",
            "oracle" => "Oracle",
            "firebird" => "Firebird",
            "snowflake" => "Snowflake",
            _ => provider
        };
    }

    /// <summary>
    /// The canonical short names of every provider supported by efcpt.
    /// </summary>
    public static IReadOnlyList<string> SupportedProviders { get; } =
        ["mssql", "postgres", "mysql", "sqlite", "oracle", "firebird", "snowflake"];
}
