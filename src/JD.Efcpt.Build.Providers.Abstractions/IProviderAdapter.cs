using System.Data.Common;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Defines the contract a database provider must fulfill to plug into the schema
/// query pipeline: creating ADO.NET connections and schema readers for that provider.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam between <c>DatabaseProviderFactory</c> (in <c>JD.Efcpt.Build.Tasks</c>)
/// and the concrete, provider-specific ADO.NET driver. SQL Server's implementation lives
/// directly in <c>JD.Efcpt.Build.Tasks</c> (see <c>Schema/Providers/SqlServerProviderAdapter</c>)
/// since it's bundled with the core package. Every other provider's implementation lives in
/// its own satellite project (e.g. <c>JD.Efcpt.Build.PostgreSQL</c>) and simply wraps that
/// provider's ADO.NET connection type and <see cref="ISchemaReader"/>.
/// </para>
/// <para>
/// This interface lives in a shared, dependency-light assembly (referenced by both
/// <c>JD.Efcpt.Build.Tasks</c> and every satellite provider project) so that
/// <c>ProviderAdapterResolver</c> can reflection-load a satellite provider's adapter type and
/// cast it back to this exact interface type, regardless of which project produced it.
/// </para>
/// </remarks>
public interface IProviderAdapter
{
    /// <summary>
    /// Creates a provider-specific <see cref="DbConnection"/> for the given connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>An unopened <see cref="DbConnection"/> for the provider's database engine.</returns>
    DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Creates the <see cref="ISchemaReader"/> used to read schema metadata for this provider.
    /// </summary>
    /// <returns>A new <see cref="ISchemaReader"/> instance for the provider.</returns>
    ISchemaReader CreateSchemaReader();
}
