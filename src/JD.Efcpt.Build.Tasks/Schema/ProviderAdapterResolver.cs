using JD.Efcpt.Build.Tasks.Schema.Providers;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Resolves a normalized provider name to its <see cref="IProviderAdapter"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Bundled vs. satellite providers:</b> <c>mssql</c> is always resolved in-assembly via
/// <see cref="AdapterFactoriesByProvider"/>, since SQL Server ships bundled with the core
/// package. Every other provider is resolved dynamically: <see cref="Resolve"/> searches
/// provider directories - the bundled <c>providers/{provider}/</c> folder next to this
/// assembly, then any caller-supplied <c>providerSearchPaths</c> -
/// for a <c>JD.Efcpt.Build.{Suffix}.dll</c> assembly (suffix from
/// <see cref="ProviderDriverNotFoundException.PackageSuffixesByProvider"/>), loads it using the
/// same dual-path mechanism as <see cref="TaskAssemblyResolver"/> (see
/// <see cref="ProviderAssemblyLoader"/>), and instantiates the single concrete
/// <see cref="IProviderAdapter"/> implementation it contains. If no matching assembly is found
/// anywhere, <see cref="ProviderDriverNotFoundException"/> is thrown with the exact
/// <c>dotnet add package</c> command needed.
/// </para>
/// <para>
/// <b>Caching:</b> resolved adapters are cached on this instance, not in a process- or
/// type-static field. MSBuild loads task assemblies per <c>AssemblyLoadContext</c> (or per
/// <c>AppDomain</c> on .NET Framework), and different projects in the same build may supply
/// different <c>providerSearchPaths</c> for the same provider name across different load
/// contexts in the same process. Instance-scoped caching keeps that state correctly isolated.
/// Within a single instance, the first successful resolution for a provider wins even if a
/// later call passes different search paths - this matches the existing MSBuild task lifecycle,
/// where search paths are constant for the duration of a single project build.
/// </para>
/// </remarks>
internal sealed class ProviderAdapterResolver
{
    /// <summary>
    /// Factories for constructing the in-assembly adapter for providers still bundled with
    /// <c>JD.Efcpt.Build.Tasks</c>. This is static because it holds only stateless factory
    /// delegates, not resolved adapter instances - see the caching remarks on this type.
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
    /// <param name="providerSearchPaths">
    /// Additional directories to search for a satellite provider assembly, beyond the bundled
    /// <c>providers/{provider}/</c> folder next to this assembly. Typically populated from the
    /// consuming MSBuild project's <c>EfcptProviderSearchPath</c> item group (see
    /// <c>QuerySchemaMetadata.ProviderSearchPaths</c>). Ignored for providers resolved in-assembly.
    /// </param>
    /// <returns>The resolved <see cref="IProviderAdapter"/> for the provider.</returns>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when no adapter is registered for <paramref name="normalizedProvider"/> and no
    /// matching satellite assembly can be found in any searched directory.
    /// </exception>
    public IProviderAdapter Resolve(string normalizedProvider, IReadOnlyList<string>? providerSearchPaths = null)
    {
        if (_cache.TryGetValue(normalizedProvider, out var cached))
            return cached;

        var adapter = AdapterFactoriesByProvider.TryGetValue(normalizedProvider, out var factory)
            ? factory()
            : ResolveFromSatellitePackage(normalizedProvider, providerSearchPaths ?? [])
                ?? throw new ProviderDriverNotFoundException(normalizedProvider);

        _cache[normalizedProvider] = adapter;
        return adapter;
    }

    /// <summary>
    /// Attempts to locate and load a satellite provider package's adapter assembly for
    /// <paramref name="normalizedProvider"/>, returning its <see cref="IProviderAdapter"/>
    /// instance, or <see langword="null"/> if no matching assembly is found.
    /// </summary>
    private static IProviderAdapter? ResolveFromSatellitePackage(
        string normalizedProvider,
        IReadOnlyList<string> providerSearchPaths)
    {
        if (!ProviderDriverNotFoundException.PackageSuffixesByProvider.TryGetValue(normalizedProvider, out var suffix)
            || suffix is null)
            return null;

        var assemblyFileName = $"JD.Efcpt.Build.{suffix}.dll";
        var assemblyPath = EnumerateCandidateDirectories(normalizedProvider, providerSearchPaths)
            .Select(dir => Path.Combine(dir, assemblyFileName))
            .FirstOrDefault(File.Exists);

        if (assemblyPath is null)
            return null;

        var assembly = ProviderAssemblyLoader.LoadFromPath(assemblyPath);
        return CreateAdapterInstance(assembly);
    }

    /// <summary>
    /// Enumerates, in search order, the directories that may contain a satellite provider's
    /// adapter assembly: first the bundled <c>providers/{provider}/</c> folder next to this
    /// assembly, then each caller-supplied search path in order.
    /// </summary>
    internal static IEnumerable<string> EnumerateCandidateDirectories(
        string normalizedProvider,
        IReadOnlyList<string> providerSearchPaths)
    {
        var taskDirectory = Path.GetDirectoryName(typeof(ProviderAdapterResolver).Assembly.Location);
        if (!string.IsNullOrEmpty(taskDirectory))
            yield return Path.Combine(taskDirectory, "providers", normalizedProvider);

        foreach (var path in providerSearchPaths)
        {
            if (!string.IsNullOrWhiteSpace(path))
                yield return path;
        }
    }

    /// <summary>
    /// Finds the single concrete <see cref="IProviderAdapter"/> implementation in
    /// <paramref name="assembly"/> and instantiates it via its parameterless constructor.
    /// </summary>
    /// <remarks>
    /// Every satellite provider assembly is expected to contain exactly one concrete
    /// <see cref="IProviderAdapter"/> implementation - this is the discovery contract satellite
    /// provider projects must follow. The first one found (by declaration order) is used.
    /// </remarks>
    internal static IProviderAdapter? CreateAdapterInstance(System.Reflection.Assembly assembly)
    {
        var adapterType = assembly.GetTypes()
            .FirstOrDefault(t => t is { IsClass: true, IsAbstract: false } && typeof(IProviderAdapter).IsAssignableFrom(t));

        return adapterType is null
            ? null
            : (IProviderAdapter?)Activator.CreateInstance(adapterType);
    }
}
