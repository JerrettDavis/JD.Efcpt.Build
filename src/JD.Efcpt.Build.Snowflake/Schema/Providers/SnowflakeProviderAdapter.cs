using System.Data.Common;
using Snowflake.Data.Client;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// <see cref="IProviderAdapter"/> for Snowflake, wrapping <see cref="SnowflakeDbConnection"/> and
/// <see cref="SnowflakeSchemaReader"/>.
/// </summary>
/// <remarks>
/// Public (rather than internal) so <c>ProviderAdapterResolver</c> in <c>JD.Efcpt.Build.Tasks</c>
/// can reflection-load and instantiate this type after locating this satellite package's
/// assembly - see that type's discovery contract for satellite provider assemblies.
/// </remarks>
public sealed class SnowflakeProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Creates a Snowflake connection.
    /// </summary>
    public DbConnection CreateConnection(string connectionString) => new SnowflakeDbConnection(connectionString);

    /// <summary>
    /// Creates a <see cref="SnowflakeSchemaReader"/>.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new SnowflakeSchemaReader();
}
