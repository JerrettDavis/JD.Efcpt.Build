using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Snowflake.Data.Client;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Factory for creating database connections and schema readers based on provider type.
/// </summary>
internal static class DatabaseProviderFactory
{
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
    public static DbConnection CreateConnection(string provider, string connectionString)
    {
        var normalized = NormalizeProvider(provider);

        return normalized switch
        {
            "mssql" => CreateSqlServerConnection(connectionString),
            "postgres" => new NpgsqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            "sqlite" => new SqliteConnection(connectionString),
            "oracle" => new OracleConnection(connectionString),
            "firebird" => new FbConnection(connectionString),
            "snowflake" => new SnowflakeDbConnection(connectionString),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported.")
        };
    }

    /// <summary>
    /// Creates an ISchemaReader for the specified provider.
    /// </summary>
    public static ISchemaReader CreateSchemaReader(string provider)
    {
        var normalized = NormalizeProvider(provider);

        return normalized switch
        {
            "mssql" => new Providers.SqlServerSchemaReader(),
            "postgres" => new Providers.PostgreSqlSchemaReader(),
            "mysql" => new Providers.MySqlSchemaReader(),
            "sqlite" => new Providers.SqliteSchemaReader(),
            "oracle" => new Providers.OracleSchemaReader(),
            "firebird" => new Providers.FirebirdSchemaReader(),
            "snowflake" => new Providers.SnowflakeSchemaReader(),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported.")
        };
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

    /// <summary>
    /// Creates a SQL Server connection with native library initialization.
    /// </summary>
    private static SqlConnection CreateSqlServerConnection(string connectionString)
    {
        // Ensure native library resolver is set up before creating SqlConnection
        NativeLibraryLoader.EnsureInitialized();
        return new SqlConnection(connectionString);
    }
}
