using System.Data.Common;
using MySqlConnector;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for MySQL/MariaDB, wrapping <see cref="MySqlConnection"/> and
/// <see cref="MySqlSchemaReader"/>.
/// </summary>
/// <remarks>
/// Public (rather than internal) so <c>ProviderAdapterResolver</c> in <c>JD.Efcpt.Build.Tasks</c>
/// can reflection-load and instantiate this type after locating this satellite package's
/// assembly - see that type's discovery contract for satellite provider assemblies.
/// </remarks>
public sealed class MySqlProviderAdapter : IProviderAdapter
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
