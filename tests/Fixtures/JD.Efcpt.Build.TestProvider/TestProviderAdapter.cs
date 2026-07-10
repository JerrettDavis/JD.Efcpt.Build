using System.Data.Common;
using JD.Efcpt.Build.Tasks.Schema;

namespace JD.Efcpt.Build.TestProvider;

/// <summary>
/// Minimal, real <see cref="IProviderAdapter"/> implementation used as a build-time test fixture
/// for <c>ProviderAdapterResolverDynamicLoadTests</c>. It stands in for a satellite provider
/// package (e.g. <c>JD.Efcpt.Build.PostgreSQL</c>) so the dynamic reflection-load path in
/// <c>ProviderAdapterResolver</c> can be exercised without a real ADO.NET driver dependency.
/// </summary>
/// <remarks>
/// Never referenced directly by test code - see the remarks in this project's <c>.csproj</c> for
/// why. Discovered purely by <see cref="IProviderAdapter"/> assignability, the same way
/// <c>ProviderAdapterResolver.CreateAdapterInstance</c> discovers a real satellite adapter.
/// </remarks>
public sealed class TestProviderAdapter : IProviderAdapter
{
    /// <summary>
    /// Not exercised by dynamic-load tests, which only assert that this type is discovered and
    /// instantiated. Throws so any accidental use is loud.
    /// </summary>
    /// <param name="connectionString">Ignored.</param>
    public DbConnection CreateConnection(string connectionString) =>
        throw new NotSupportedException(
            $"{nameof(TestProviderAdapter)} is a resolver test fixture and does not support real connections.");

    /// <summary>
    /// Returns a trivial <see cref="ISchemaReader"/> whose <see cref="ISchemaReader.ReadSchema"/>
    /// always returns <see cref="SchemaModel.Empty"/>, so tests can prove end-to-end that the
    /// reflection-loaded instance is genuinely usable through the shared abstraction types.
    /// </summary>
    public ISchemaReader CreateSchemaReader() => new EmptySchemaReader();

    private sealed class EmptySchemaReader : ISchemaReader
    {
        public SchemaModel ReadSchema(string connectionString) => SchemaModel.Empty;
    }
}
