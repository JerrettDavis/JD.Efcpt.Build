using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for SQL Server, wrapping <see cref="SqlConnection"/> and
/// <see cref="SqlServerSchemaReader"/>. The SQL Server driver is bundled with the core
/// package, so this adapter is always available.
/// </summary>
internal sealed class SqlServerProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates a SQL Server connection, ensuring the native SNI library resolver is
    /// initialized first.
    /// </summary>
    public DbConnection CreateConnection(string connectionString)
    {
        // Ensure native library resolver is set up before creating SqlConnection
        NativeLibraryLoader.EnsureInitialized();
        return new SqlConnection(connectionString);
    }

    /// <summary>
    /// Creates a <see cref="SqlServerSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new SqlServerSchemaReader();
}
