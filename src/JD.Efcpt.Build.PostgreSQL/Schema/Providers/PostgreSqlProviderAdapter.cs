using System.Data.Common;
using Npgsql;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for PostgreSQL, wrapping <see cref="NpgsqlConnection"/> and
/// <see cref="PostgreSqlSchemaReader"/>.
/// </summary>
/// <remarks>
/// Public (rather than internal) so <c>ProviderAdapterResolver</c> in <c>JD.Efcpt.Build.Tasks</c>
/// can reflection-load and instantiate this type after locating this satellite package's
/// assembly - see that type's discovery contract for satellite provider assemblies.
/// </remarks>
public sealed class PostgreSqlProviderAdapter : IProviderAdapter
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
