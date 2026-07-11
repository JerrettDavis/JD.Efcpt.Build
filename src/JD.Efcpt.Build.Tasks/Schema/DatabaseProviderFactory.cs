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
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="provider"/> is not a recognized built-in provider alias.
    /// </exception>
    public static string NormalizeProvider(string provider) => NormalizeProvider(provider, null);

    /// <summary>
    /// Normalizes a provider identifier, additionally admitting keys registered in
    /// <paramref name="customProviderKeys"/> (see #184's <c>customProviders</c> plugin registry).
    /// </summary>
    /// <remarks>
    /// Built-in aliases are always matched first via <see cref="ProviderNames.Normalize"/> - a
    /// custom provider key can never shadow a built-in one, since the built-in switch is
    /// evaluated before <paramref name="customProviderKeys"/> is even consulted. Only when the
    /// built-in normalization throws <see cref="NotSupportedException"/> is the (lowercased,
    /// ordinal-ignore-case) custom registry checked; a match there is returned as-is (custom
    /// provider keys are their own canonical form - there are no aliases for them).
    /// </remarks>
    /// <param name="provider">The provider name (a built-in alias, or a registered custom key).</param>
    /// <param name="customProviderKeys">
    /// The set of registered custom provider keys (see <c>QuerySchemaMetadata.EfcptCustomProviders</c>),
    /// or <see langword="null"/>/empty when no custom providers are registered.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="provider"/> is neither a recognized built-in provider alias
    /// nor a key present in <paramref name="customProviderKeys"/>.
    /// </exception>
    public static string NormalizeProvider(string provider, IReadOnlyCollection<string>? customProviderKeys)
    {
        try
        {
            return ProviderNames.Normalize(provider);
        }
        catch (NotSupportedException)
        {
            if (!string.IsNullOrWhiteSpace(provider) && customProviderKeys is { Count: > 0 })
            {
                var lowered = provider.ToLowerInvariant();
                if (customProviderKeys.Any(k => string.Equals(k, provider, StringComparison.OrdinalIgnoreCase)))
                    return lowered;
            }

            throw;
        }
    }

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
        => CreateConnection(provider, connectionString, providerSearchPaths, null);

    /// <summary>
    /// Creates a DbConnection for the specified provider, additionally admitting a registered
    /// custom provider (see #184's <c>customProviders</c> plugin registry).
    /// </summary>
    /// <param name="provider">The provider name (a built-in alias, or a registered custom key).</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="providerSearchPaths">
    /// Additional directories to search for a satellite provider assembly, if the provider isn't
    /// bundled with the core package. See <see cref="ProviderAdapterResolver.Resolve"/>.
    /// </param>
    /// <param name="customProviderAssemblies">
    /// Maps a registered custom provider key to the simple assembly name that contains its
    /// <see cref="IProviderAdapter"/>, or <see langword="null"/>/empty when no custom providers
    /// are registered. See <see cref="ProviderAdapterResolver.Resolve"/>.
    /// </param>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when the driver for the normalized provider cannot be resolved.
    /// </exception>
    /// <exception cref="CustomProviderException">
    /// Thrown when <paramref name="provider"/> resolves to a registered custom provider whose
    /// assembly cannot be found, loaded, or instantiated.
    /// </exception>
    public static DbConnection CreateConnection(
        string provider,
        string connectionString,
        IReadOnlyList<string>? providerSearchPaths,
        IReadOnlyDictionary<string, string>? customProviderAssemblies)
    {
        var normalized = NormalizeProvider(provider, customProviderAssemblies?.Keys.ToArray());
        return Resolver.Resolve(normalized, providerSearchPaths, customProviderAssemblies).CreateConnection(connectionString);
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
        => CreateSchemaReader(provider, providerSearchPaths, null);

    /// <summary>
    /// Creates an ISchemaReader for the specified provider, additionally admitting a registered
    /// custom provider (see #184's <c>customProviders</c> plugin registry).
    /// </summary>
    /// <param name="provider">The provider name (a built-in alias, or a registered custom key).</param>
    /// <param name="providerSearchPaths">
    /// Additional directories to search for a satellite provider assembly, if the provider isn't
    /// bundled with the core package. See <see cref="ProviderAdapterResolver.Resolve"/>.
    /// </param>
    /// <param name="customProviderAssemblies">
    /// Maps a registered custom provider key to the simple assembly name that contains its
    /// <see cref="IProviderAdapter"/>, or <see langword="null"/>/empty when no custom providers
    /// are registered. See <see cref="ProviderAdapterResolver.Resolve"/>.
    /// </param>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when the driver for the normalized provider cannot be resolved.
    /// </exception>
    /// <exception cref="CustomProviderException">
    /// Thrown when <paramref name="provider"/> resolves to a registered custom provider whose
    /// assembly cannot be found, loaded, or instantiated.
    /// </exception>
    public static ISchemaReader CreateSchemaReader(
        string provider,
        IReadOnlyList<string>? providerSearchPaths,
        IReadOnlyDictionary<string, string>? customProviderAssemblies)
    {
        var normalized = NormalizeProvider(provider, customProviderAssemblies?.Keys.ToArray());
        return Resolver.Resolve(normalized, providerSearchPaths, customProviderAssemblies).CreateSchemaReader();
    }

    /// <summary>
    /// Gets the display name for a provider.
    /// </summary>
    public static string GetProviderDisplayName(string provider) => ProviderNames.GetDisplayName(provider);
}
