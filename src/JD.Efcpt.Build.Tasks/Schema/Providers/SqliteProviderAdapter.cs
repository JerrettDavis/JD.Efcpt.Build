using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for SQLite, wrapping <see cref="SqliteConnection"/> and
/// <see cref="SqliteSchemaReader"/>.
/// </summary>
internal sealed class SqliteProviderAdapter : IProviderAdapter
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
