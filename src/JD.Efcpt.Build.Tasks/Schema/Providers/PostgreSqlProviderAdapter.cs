using System.Data.Common;
using Npgsql;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for PostgreSQL, wrapping <see cref="NpgsqlConnection"/> and
/// <see cref="PostgreSqlSchemaReader"/>.
/// </summary>
internal sealed class PostgreSqlProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates a PostgreSQL connection.
    /// </summary>
    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    /// <summary>
    /// Creates a <see cref="PostgreSqlSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new PostgreSqlSchemaReader();
}
