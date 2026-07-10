using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for Firebird, wrapping <see cref="FbConnection"/> and
/// <see cref="FirebirdSchemaReader"/>.
/// </summary>
internal sealed class FirebirdProviderAdapter : IProviderAdapter
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
