using JD.Efcpt.Build.Tasks.Schema.Providers;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Resolves a normalized provider name to its <see cref="IProviderAdapter"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Phased design:</b> in Phase 1, every provider driver still ships in this assembly,
/// so <see cref="Resolve"/> constructs the in-assembly adapter directly via
/// <see cref="AdapterFactoriesByProvider"/>. Once drivers move into satellite packages
/// (e.g. <c>JD.Efcpt.Build.PostgreSQL</c>), this lookup will be extended to search the
/// provider's directory for the package's assembly using the same dual-path mechanism as
/// <see cref="TaskAssemblyResolver"/> — <c>AssemblyLoadContext.Resolving</c> on net8+ and
/// <c>AppDomain.AssemblyResolve</c> on net472 — before falling back to
/// <see cref="ProviderDriverNotFoundException"/> if the package isn't installed. Callers of
/// <see cref="Resolve"/> do not need to change when that happens.
/// </para>
/// <para>
/// <b>Caching:</b> resolved adapters are cached on this instance, not in a process- or
/// type-static field. MSBuild loads task assemblies per <c>AssemblyLoadContext</c> (or per
/// <c>AppDomain</c> on .NET Framework), and later phases may resolve different satellite
/// packages for the same provider across different projects/load contexts in the same
/// process. Instance-scoped caching keeps that state correctly isolated.
/// </para>
/// </remarks>
internal sealed class ProviderAdapterResolver
{
    /// <summary>
    /// Factories for constructing the in-assembly adapter for each normalized provider.
    /// This is static because it holds only stateless factory delegates, not resolved
    /// adapter instances — see the caching remarks on this type.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Func<IProviderAdapter>> AdapterFactoriesByProvider =
        new Dictionary<string, Func<IProviderAdapter>>
        {
            ["mssql"] = () => new SqlServerProviderAdapter(),
            ["postgres"] = () => new PostgreSqlProviderAdapter(),
            ["mysql"] = () => new MySqlProviderAdapter(),
            ["sqlite"] = () => new SqliteProviderAdapter(),
            ["oracle"] = () => new OracleProviderAdapter(),
            ["firebird"] = () => new FirebirdProviderAdapter(),
            ["snowflake"] = () => new SnowflakeProviderAdapter()
        };

    private readonly Dictionary<string, IProviderAdapter> _cache = new();

    /// <summary>
    /// Resolves the <see cref="IProviderAdapter"/> for a normalized provider name, caching
    /// the result on this instance so repeat calls return the same adapter instance.
    /// </summary>
    /// <param name="normalizedProvider">
    /// A provider name already normalized by <see cref="DatabaseProviderFactory.NormalizeProvider"/>.
    /// </param>
    /// <returns>The resolved <see cref="IProviderAdapter"/> for the provider.</returns>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when no adapter is registered for <paramref name="normalizedProvider"/>.
    /// </exception>
    public IProviderAdapter Resolve(string normalizedProvider)
    {
        if (_cache.TryGetValue(normalizedProvider, out var cached))
            return cached;

        if (!AdapterFactoriesByProvider.TryGetValue(normalizedProvider, out var factory))
            throw new ProviderDriverNotFoundException(normalizedProvider);

        var adapter = factory();
        _cache[normalizedProvider] = adapter;
        return adapter;
    }
}
