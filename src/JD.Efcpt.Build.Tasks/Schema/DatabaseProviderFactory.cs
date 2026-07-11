using System.Data.Common;
using JD.Efcpt.Build.Core.Providers;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Factory for creating database connections and schema readers based on provider type.
/// </summary>
/// <remarks>
/// Connection and schema-reader construction is delegated to <see cref="IProviderAdapter"/>
/// implementations resolved via <see cref="Resolver"/>; see <see cref="ProviderAdapterResolver"/>
/// for the phased design that will let later phases move drivers into satellite packages
/// without changing this factory's public surface. Provider name normalization itself is
/// delegated to <see cref="ProviderNames"/> (moved to <c>JD.Efcpt.Build.Core</c> in #181 so the
/// jd-efcpt CLI can share the exact same alias/display-name list).
/// </remarks>
internal static class DatabaseProviderFactory
{
    /// <summary>
    /// Resolves normalized provider names to their <see cref="IProviderAdapter"/>. A single
    /// instance is held for the lifetime of this static class's load context (which, under
    /// MSBuild, is scoped per task load — see <see cref="ProviderAdapterResolver"/> for why
    /// caching is instance-scoped rather than process-static).
    /// </summary>
    private static readonly ProviderAdapterResolver Resolver = new();

    /// <summary>
    /// Known provider identifiers mapped to their canonical names.
    /// </summary>
    public static string NormalizeProvider(string provider) => ProviderNames.Normalize(provider);

    /// <summary>
    /// Creates a DbConnection for the specified provider.
    /// </summary>
    /// <param name="provider">The provider name (any recognized alias).</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="providerSearchPaths">
    /// Additional directories to search for a satellite provider assembly, if the provider isn't
    /// bundled with the core package. See <see cref="ProviderAdapterResolver.Resolve"/>.
    /// </param>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when the driver for the normalized provider cannot be resolved.
    /// </exception>
    public static DbConnection CreateConnection(
        string provider,
        string connectionString,
        IReadOnlyList<string>? providerSearchPaths = null)
    {
        var normalized = NormalizeProvider(provider);
        return Resolver.Resolve(normalized, providerSearchPaths).CreateConnection(connectionString);
    }

    /// <summary>
    /// Creates an ISchemaReader for the specified provider.
    /// </summary>
    /// <param name="provider">The provider name (any recognized alias).</param>
    /// <param name="providerSearchPaths">
    /// Additional directories to search for a satellite provider assembly, if the provider isn't
    /// bundled with the core package. See <see cref="ProviderAdapterResolver.Resolve"/>.
    /// </param>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when the driver for the normalized provider cannot be resolved.
    /// </exception>
    public static ISchemaReader CreateSchemaReader(string provider, IReadOnlyList<string>? providerSearchPaths = null)
    {
        var normalized = NormalizeProvider(provider);
        return Resolver.Resolve(normalized, providerSearchPaths).CreateSchemaReader();
    }

    /// <summary>
    /// Gets the display name for a provider.
    /// </summary>
    public static string GetProviderDisplayName(string provider) => ProviderNames.GetDisplayName(provider);
}
