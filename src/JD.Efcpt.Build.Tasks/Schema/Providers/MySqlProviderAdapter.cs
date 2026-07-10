using System.Data.Common;
using MySqlConnector;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for MySQL/MariaDB, wrapping <see cref="MySqlConnection"/> and
/// <see cref="MySqlSchemaReader"/>.
/// </summary>
internal sealed class MySqlProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates a MySQL connection.
    /// </summary>
    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    /// <summary>
    /// Creates a <see cref="MySqlSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new MySqlSchemaReader();
}
