using System.Data.Common;
using JD.Efcpt.Build.Tasks.Schema;

namespace Acme.Efcpt.Mongo;

/// <summary>
/// Sample custom database provider adapter (#184's <c>customProviders</c> plugin registry).
/// </summary>
/// <remarks>
/// <para>
/// This is the minimum a real custom provider needs: exactly one public, concrete class
/// implementing <see cref="IProviderAdapter"/> with a public parameterless constructor (see
/// <c>ProviderAdapterResolver.CreateAdapterInstance</c>, which discovers your adapter by
/// reflecting over this assembly for the first type assignable to <see cref="IProviderAdapter"/>
/// and instantiating it via <c>Activator.CreateInstance</c>).
/// </para>
/// <para>
/// This sample deliberately doesn't connect to a real database - <see cref="CreateSchemaReader"/>
/// returns a reader whose <see cref="ISchemaReader.ReadSchema"/> always returns
/// <see cref="SchemaModel.Empty"/>, so this project builds and can be exercised in CI with no
/// database and no network access. A real provider would connect to its target engine (MongoDB,
/// DynamoDB, ClickHouse, etc.) and translate its schema into a <see cref="SchemaModel"/>.
/// </para>
/// <para>
/// See <c>docs/user-guide/custom-providers.md</c> for the full authoring guide, and this sample's
/// <c>README.md</c> for the <c>@(EfcptCustomProvider)</c> registration snippet that would select
/// this adapter as provider key <c>acme-mongo</c>.
/// </para>
/// </remarks>
public sealed class MongoProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// A real implementation would parse <paramref name="connectionString"/> and return a
    /// <see cref="DbConnection"/>-derived wrapper around the target driver's connection type.
    /// This sample throws, since it is never exercised outside authoring documentation.
    /// </summary>
    /// <param name="connectionString">Ignored.</param>
    public DbConnection CreateConnection(string connectionString) =>
        throw new NotSupportedException(
            $"{nameof(MongoProviderAdapter)} is a documentation sample and does not support real connections. " +
            "See docs/user-guide/custom-providers.md for how to implement a real custom provider.");

    /// <inheritdoc/>
    public ISchemaReader CreateSchemaReader() => new EmptySchemaReader();

    private sealed class EmptySchemaReader : ISchemaReader
    {
        public SchemaModel ReadSchema(string connectionString) => SchemaModel.Empty;
    }
}
