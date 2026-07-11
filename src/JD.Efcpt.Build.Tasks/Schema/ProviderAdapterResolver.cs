using System.Collections.Concurrent;
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
/// </para>
/// <para>
/// <b>Thread safety and cache keys:</b> the cache is a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// because a single <see cref="DatabaseProviderFactory"/>-held instance can be reached
/// concurrently under MSBuild node reuse. For providers resolved dynamically from a satellite
/// package, the cache key folds in the effective, order-independent set of
/// <c>providerSearchPaths</c> (see <see cref="BuildCacheKey"/>) - not just the provider name -
/// so that two callers supplying different search paths for the same provider (e.g. two
/// projects with different <c>EfcptProviderSearchPath</c> items reusing the same task node)
/// never collide on the same cache entry and silently receive the wrong adapter. Providers
/// resolved in-assembly via <see cref="AdapterFactoriesByProvider"/> are cached by name alone
/// since search paths are irrelevant to them.
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
            ["mssql"] = () => new SqlServerProviderAdapter()
            // "snowflake", "oracle", "postgres", "mysql", "firebird", and "sqlite" are
            // intentionally absent: they were extracted into satellite packages
            // (JD.Efcpt.Build.Snowflake, .Oracle, .PostgreSQL, .MySqlConnector, .Firebird,
            // .Sqlite) and now resolve exclusively via ResolveFromSatellitePackage below.
            // "mssql" is the only provider that remains bundled with the core package - see #189.
        };

    private static readonly IReadOnlyDictionary<string, string> EmptyCustomProviderAssemblies =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, IProviderAdapter> _cache = new();

    /// <summary>
    /// Resolves the <see cref="IProviderAdapter"/> for a normalized provider name, caching
    /// the result on this instance so repeat calls return the same adapter instance. Safe to
    /// call concurrently from multiple threads.
    /// </summary>
    /// <param name="normalizedProvider">
    /// A provider name already normalized by <see cref="DatabaseProviderFactory.NormalizeProvider(string)"/>.
    /// </param>
    /// <param name="providerSearchPaths">
    /// Additional directories to search for a satellite provider assembly, beyond the bundled
    /// <c>providers/{provider}/</c> folder next to this assembly. Typically populated from the
    /// consuming MSBuild project's <c>EfcptProviderSearchPath</c> item group (see
    /// <c>QuerySchemaMetadata.ProviderSearchPaths</c>). Ignored for providers resolved in-assembly.
    /// </param>
    /// <param name="customProviderAssemblies">
    /// Maps a registered custom provider key (see #184's <c>customProviders</c> plugin registry)
    /// to the simple assembly name (without directory or <c>.dll</c> extension) that contains its
    /// <see cref="IProviderAdapter"/>. Only consulted when <paramref name="normalizedProvider"/>
    /// does not match a built-in provider - see <see cref="ResolveFromSatellitePackage"/>.
    /// Ignored for providers resolved in-assembly.
    /// </param>
    /// <returns>The resolved <see cref="IProviderAdapter"/> for the provider.</returns>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when no adapter is registered for <paramref name="normalizedProvider"/> and no
    /// matching satellite assembly can be found in any searched directory, or when a matching
    /// built-in satellite assembly was found but could not be loaded or instantiated (see
    /// <see cref="Exception.InnerException"/> for the underlying cause in that case).
    /// </exception>
    /// <exception cref="CustomProviderException">
    /// Thrown when <paramref name="normalizedProvider"/> resolves via
    /// <paramref name="customProviderAssemblies"/> and its assembly could not be found, could not
    /// be loaded or instantiated (<see cref="CustomProviderException.AssemblyLoadFailedCode"/>),
    /// or was loaded but contains no concrete <see cref="IProviderAdapter"/>
    /// (<see cref="CustomProviderException.NoAdapterFoundCode"/>).
    /// </exception>
    public IProviderAdapter Resolve(
        string normalizedProvider,
        IReadOnlyList<string>? providerSearchPaths = null,
        IReadOnlyDictionary<string, string>? customProviderAssemblies = null)
    {
        if (AdapterFactoriesByProvider.TryGetValue(normalizedProvider, out var factory))
            return _cache.GetOrAdd(normalizedProvider, _ => factory());

        var searchPaths = providerSearchPaths ?? [];
        var cacheKey = BuildCacheKey(normalizedProvider, searchPaths);

        return _cache.GetOrAdd(cacheKey, _ =>
            ResolveFromSatellitePackage(normalizedProvider, searchPaths, customProviderAssemblies ?? EmptyCustomProviderAssemblies)
                ?? throw new ProviderDriverNotFoundException(normalizedProvider));
    }

    /// <summary>
    /// Builds the cache key for a satellite-resolved provider, folding in the effective set of
    /// <paramref name="providerSearchPaths"/> so that two callers supplying different search
    /// paths for the same provider name never collide on the same cache entry - see the
    /// thread-safety and cache-key remarks on this type.
    /// </summary>
    /// <param name="normalizedProvider">The normalized provider name.</param>
    /// <param name="providerSearchPaths">The caller-supplied satellite search paths.</param>
    /// <returns>
    /// A key that is identical for two calls with the same provider and the same effective set
    /// of search paths, regardless of order, duplicates, or surrounding whitespace.
    /// </returns>
    internal static string BuildCacheKey(string normalizedProvider, IReadOnlyList<string> providerSearchPaths)
    {
        var distinctSortedPaths = providerSearchPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctSortedPaths.Count == 0
            ? normalizedProvider
            : normalizedProvider + "|" + string.Join("|", distinctSortedPaths);
    }

    /// <summary>
    /// Attempts to locate and load a satellite provider package's adapter assembly for
    /// <paramref name="normalizedProvider"/>, returning its <see cref="IProviderAdapter"/>
    /// instance, or <see langword="null"/> if no matching assembly is found in any searched
    /// directory and <paramref name="normalizedProvider"/> is not a registered custom provider
    /// key either.
    /// </summary>
    /// <remarks>
    /// DLL-name resolution tries, in order: (1) the built-in
    /// <see cref="ProviderDriverNotFoundException.PackageSuffixesByProvider"/> suffix map -
    /// unchanged, always matched first; (2) <paramref name="customProviderAssemblies"/> (#184) -
    /// only consulted when <paramref name="normalizedProvider"/> doesn't match a built-in suffix,
    /// so a custom key can never shadow a built-in provider's assembly name. If neither matches,
    /// this returns <see langword="null"/> (unknown provider - <see cref="Resolve"/> turns that
    /// into <see cref="ProviderDriverNotFoundException"/>).
    /// </remarks>
    /// <exception cref="ProviderDriverNotFoundException">
    /// Thrown when a matching <i>built-in</i> satellite assembly file was found but loading it or
    /// instantiating its <see cref="IProviderAdapter"/> implementation failed for any reason
    /// (corrupt DLL, wrong architecture, missing transitive dependency, no accessible
    /// parameterless constructor, a constructor that throws, etc.). The original exception is
    /// preserved as <see cref="Exception.InnerException"/>. This ensures a raw CLR loader
    /// exception never reaches the MSBuild log in place of the actionable install guidance.
    /// </exception>
    /// <exception cref="CustomProviderException">
    /// Thrown when <paramref name="normalizedProvider"/> matches a key in
    /// <paramref name="customProviderAssemblies"/> and its assembly could not be found on any
    /// searched directory, or was found but failed to load or instantiate
    /// (<see cref="CustomProviderException.AssemblyLoadFailedCode"/>), or loaded successfully but
    /// contains no concrete <see cref="IProviderAdapter"/>
    /// (<see cref="CustomProviderException.NoAdapterFoundCode"/>).
    /// </exception>
    internal static IProviderAdapter? ResolveFromSatellitePackage(
        string normalizedProvider,
        IReadOnlyList<string> providerSearchPaths,
        IReadOnlyDictionary<string, string>? customProviderAssemblies = null)
    {
        bool isCustom;
        string assemblyFileName;

        if (ProviderDriverNotFoundException.PackageSuffixesByProvider.TryGetValue(normalizedProvider, out var suffix)
            && suffix is not null)
        {
            isCustom = false;
            assemblyFileName = $"JD.Efcpt.Build.{suffix}.dll";
        }
        else if (customProviderAssemblies is not null
                 && customProviderAssemblies.TryGetValue(normalizedProvider, out var customAssemblyName)
                 && !string.IsNullOrWhiteSpace(customAssemblyName))
        {
            isCustom = true;
            assemblyFileName = $"{customAssemblyName}.dll";
        }
        else
        {
            return null;
        }

        var assemblyPath = EnumerateCandidateDirectories(normalizedProvider, providerSearchPaths)
            .Select(dir => Path.Combine(dir, assemblyFileName))
            .FirstOrDefault(File.Exists);

        if (assemblyPath is null)
        {
            if (isCustom)
                throw new CustomProviderException(
                    CustomProviderException.AssemblyLoadFailedCode,
                    $"Custom provider '{normalizedProvider}' is registered, but its assembly " +
                    $"'{assemblyFileName}' was not found on any provider search path. Verify the " +
                    "AssemblyName metadata on the matching @(EfcptCustomProvider) item and, if the " +
                    "assembly isn't next to the task assembly, that its SearchPath metadata (or " +
                    "@(EfcptProviderSearchPath)) points at the directory containing it.");

            return null;
        }

        try
        {
            var assembly = ProviderAssemblyLoader.LoadFromPath(assemblyPath);
            var adapter = CreateAdapterInstance(assembly);

            if (adapter is null && isCustom)
                throw new CustomProviderException(
                    CustomProviderException.NoAdapterFoundCode,
                    $"Custom provider '{normalizedProvider}' assembly '{assemblyPath}' was loaded " +
                    "successfully, but does not contain a concrete implementation of " +
                    "IProviderAdapter (JD.Efcpt.Build.Tasks.Schema.IProviderAdapter, from the " +
                    "JD.Efcpt.Build.Providers.Abstractions package). Every custom provider assembly " +
                    "must contain exactly one such type.");

            return adapter;
        }
        catch (CustomProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (isCustom)
                throw new CustomProviderException(
                    CustomProviderException.AssemblyLoadFailedCode,
                    $"Custom provider '{normalizedProvider}' assembly '{assemblyPath}' was found, " +
                    $"but failed to load or instantiate: {ex.Message}", ex);

            throw new ProviderDriverNotFoundException(normalizedProvider, ex);
        }
    }

    /// <summary>
    /// Enumerates, in search order, the directories that may contain a satellite provider's
    /// adapter assembly: first the bundled <c>providers/{provider}/</c> folder next to this
    /// assembly, then each caller-supplied search path in order. Caller-supplied paths that are
    /// null, empty, whitespace, or don't exist on disk are skipped defensively - an
    /// <c>EfcptProviderSearchPath</c> item pointing at a stale or misconfigured directory should
    /// be silently ignored rather than risk a downstream <see cref="Path.Combine(string, string)"/>
    /// failure on a malformed entry.
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
            if (string.IsNullOrWhiteSpace(path))
                continue;

            // Directory.Exists tolerates invalid path characters by returning false instead of
            // throwing, so this also guards against a malformed search-path entry crashing
            // resolution outright.
            if (!Directory.Exists(path))
                continue;

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
