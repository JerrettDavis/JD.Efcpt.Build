using System.Data.Common;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Factory for creating database connections and schema readers based on provider type.
/// </summary>
/// <remarks>
/// Connection and schema-reader construction is delegated to <see cref="IProviderAdapter"/>
/// implementations resolved via <see cref="Resolver"/>; see <see cref="ProviderAdapterResolver"/>
/// for the phased design that will let later phases move drivers into satellite packages
/// without changing this factory's public surface.
/// </remarks>
internal static class DatabaseProviderFactory
{
    /// <summary>
    /// Resolves normalized provider names to their <see cref="IProviderAdapter"/>. A single
    /// instance is held for the lifetime of this static class's load context (which, under
    /// MSBuild, is scoped per task load — see <see cref="ProviderAdapterResolver"/> for why
    /// caching is instance-scoped rather than process-static).
    /// </summary>
    private static readonly ProviderAdapterResolver Resolver = new();

    /// <summary>
    /// Known provider identifiers mapped to their canonical names.
    /// </summary>
    public static string NormalizeProvider(string provider)
    {
#if NETFRAMEWORK
        NetFrameworkPolyfills.ThrowIfNullOrWhiteSpace(provider, nameof(provider));
#else
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
#endif

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
    /// Creates a DbConnection for the specified provider.
    /// </summary>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when the driver for the normalized provider cannot be resolved.
    /// </exception>
    public static DbConnection CreateConnection(string provider, string connectionString)
    {
        var normalized = NormalizeProvider(provider);
        return Resolver.Resolve(normalized).CreateConnection(connectionString);
    }

    /// <summary>
    /// Creates an ISchemaReader for the specified provider.
    /// </summary>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when the driver for the normalized provider cannot be resolved.
    /// </exception>
    public static ISchemaReader CreateSchemaReader(string provider)
    {
        var normalized = NormalizeProvider(provider);
        return Resolver.Resolve(normalized).CreateSchemaReader();
    }

    /// <summary>
    /// Gets the display name for a provider.
    /// </summary>
    public static string GetProviderDisplayName(string provider)
    {
        var normalized = NormalizeProvider(provider);

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
}
