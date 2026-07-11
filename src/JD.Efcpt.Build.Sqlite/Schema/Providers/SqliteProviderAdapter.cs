using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for SQLite, wrapping <see cref="SqliteConnection"/> and
/// <see cref="SqliteSchemaReader"/>.
/// </summary>
/// <remarks>
/// Public (rather than internal) so <c>ProviderAdapterResolver</c> in <c>JD.Efcpt.Build.Tasks</c>
/// can reflection-load and instantiate this type after locating this satellite package's
/// assembly - see that type's discovery contract for satellite provider assemblies.
/// </remarks>
public sealed class SqliteProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates a SQLite connection.
    /// </summary>
    public DbConnection CreateConnection(string connectionString) => new SqliteConnection(connectionString);

    /// <summary>
    /// Creates a <see cref="SqliteSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new SqliteSchemaReader();
}
