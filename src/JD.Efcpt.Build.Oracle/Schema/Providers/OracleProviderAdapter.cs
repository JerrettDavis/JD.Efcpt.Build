using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for Oracle, wrapping <see cref="OracleConnection"/> and
/// <see cref="OracleSchemaReader"/>.
/// </summary>
/// <remarks>
/// Public (rather than internal) so <c>ProviderAdapterResolver</c> in <c>JD.Efcpt.Build.Tasks</c>
/// can reflection-load and instantiate this type after locating this satellite package's
/// assembly - see that type's discovery contract for satellite provider assemblies.
/// </remarks>
public sealed class OracleProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates an Oracle connection.
    /// </summary>
    public DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);

    /// <summary>
    /// Creates an <see cref="OracleSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new OracleSchemaReader();
}
