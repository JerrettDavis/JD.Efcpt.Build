using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for Firebird, wrapping <see cref="FbConnection"/> and
/// <see cref="FirebirdSchemaReader"/>.
/// </summary>
/// <remarks>
/// Public (rather than internal) so <c>ProviderAdapterResolver</c> in <c>JD.Efcpt.Build.Tasks</c>
/// can reflection-load and instantiate this type after locating this satellite package's
/// assembly - see that type's discovery contract for satellite provider assemblies.
/// </remarks>
public sealed class FirebirdProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates a Firebird connection.
    /// </summary>
    public DbConnection CreateConnection(string connectionString) => new FbConnection(connectionString);

    /// <summary>
    /// Creates a <see cref="FirebirdSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new FirebirdSchemaReader();
}
