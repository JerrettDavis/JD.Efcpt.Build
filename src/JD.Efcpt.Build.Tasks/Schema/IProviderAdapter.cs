using System.Data.Common;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Defines the contract a database provider must fulfill to plug into the schema
/// query pipeline: creating ADO.NET connections and schema readers for that provider.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam between <see cref="DatabaseProviderFactory"/> and the concrete,
/// provider-specific ADO.NET driver. Today every implementation lives in this assembly
/// (see <c>Schema/Providers/*ProviderAdapter.cs</c>) and simply wraps the existing
/// connection type and <see cref="ISchemaReader"/> for its provider.
/// </para>
/// <para>
/// In a later phase, drivers will be extracted into satellite packages (e.g.
/// <c>JD.Efcpt.Build.PostgreSQL</c>) and <see cref="ProviderAdapterResolver"/> will
/// locate implementations of this interface from those packages instead of
/// constructing them directly. The interface is intentionally minimal so that swap
/// can happen without touching <see cref="DatabaseProviderFactory"/> again.
/// </para>
/// </remarks>
internal interface IProviderAdapter
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
