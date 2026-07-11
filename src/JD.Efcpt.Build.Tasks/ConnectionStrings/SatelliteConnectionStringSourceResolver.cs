using System.Collections.Concurrent;
using System.Reflection;
using JD.Efcpt.Build.Core.ConnectionStrings;
using JD.Efcpt.Build.Tasks.Schema;

namespace JD.Efcpt.Build.Tasks.ConnectionStrings;

/// <summary>
/// Resolves a connection-string source key to its <see cref="IConnectionStringSource"/>,
/// dynamically loading satellite connection-string-source packages (Azure Key Vault, AWS
/// Secrets Manager, etc.) the same way <see cref="ProviderAdapterResolver"/> loads satellite
/// provider drivers - see #189.
/// </summary>
/// <remarks>
/// <para>
/// The <c>env</c> source is always resolved in-assembly via
/// <see cref="EnvironmentVariableConnectionStringSource"/> since it ships bundled with
/// <c>JD.Efcpt.Build.Core</c> and never requires a satellite package.
/// </para>
/// <para>
/// Every other source key is resolved dynamically: <see cref="Resolve"/> searches the bundled
/// <c>connstr-sources/{key}/</c> folder next to this assembly, then any caller-supplied
/// search paths (from the <c>EfcptConnectionStringSourceSearchPath</c> MSBuild item, threaded
/// through the constructor), for a <c>JD.Efcpt.Build.ConnectionStrings.{Suffix}.dll</c>
/// assembly, loads it via <see cref="ProviderAssemblyLoader.LoadFromPath"/> - the same dual-path
/// loader used for provider drivers, reused here rather than duplicated - and instantiates the
/// single concrete <see cref="IConnectionStringSource"/> implementation it contains.
/// </para>
/// <para>
/// If no matching assembly is found anywhere, <see cref="Resolve"/> returns
/// <see langword="null"/>; <see cref="ConnectionStringResolutionChain"/>'s Branch 0 turns that
/// into a <see cref="ConnectionStringSourceException"/> with <c>JD0033</c> and the exact
/// <c>dotnet add package</c> command needed. If a matching assembly is found but fails to load
/// or instantiate, <see cref="Resolve"/> throws instead of returning <see langword="null"/>, so
/// Branch 0 maps that to <c>JD0030</c> (a resolution failure) rather than the misleading
/// "not installed" JD0033.
/// </para>
/// <para>
/// Resolved sources are cached per-instance, mirroring <see cref="ProviderAdapterResolver"/>'s
/// caching rationale: MSBuild loads task assemblies per <c>AssemblyLoadContext</c>/<c>AppDomain</c>,
/// and different projects in the same build may supply different search paths for the same
/// source key.
/// </para>
/// </remarks>
internal sealed class SatelliteConnectionStringSourceResolver : IConnectionStringSourceResolver
{
    private static readonly EnvironmentVariableConnectionStringSource EnvSource = new();

    /// <summary>
    /// Maps each known first-party satellite connection-string source key to the assembly file
    /// name (without directory) that contains its <see cref="IConnectionStringSource"/>
    /// implementation. Reuses <see cref="ConnectionStringSourceException.KnownSatellitePackagesByKey"/>
    /// since the NuGet package id and assembly name are identical for these satellites, matching
    /// the provider-driver satellite convention (e.g. <c>JD.Efcpt.Build.PostgreSQL</c>).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> AssemblyFileNamesByKey =
        ConnectionStringSourceException.KnownSatellitePackagesByKey
            .ToDictionary(kv => kv.Key, kv => kv.Value + ".dll", StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyList<string> _searchPaths;
    private readonly ConcurrentDictionary<string, IConnectionStringSource?> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of <see cref="SatelliteConnectionStringSourceResolver"/>.
    /// </summary>
    /// <param name="searchPaths">
    /// Additional directories to search for a satellite source assembly, beyond the bundled
    /// <c>connstr-sources/{key}/</c> folder next to this assembly. Typically populated from the
    /// consuming MSBuild project's <c>EfcptConnectionStringSourceSearchPath</c> item group.
    /// </param>
    public SatelliteConnectionStringSourceResolver(IReadOnlyList<string>? searchPaths = null)
    {
        _searchPaths = searchPaths ?? [];
    }

    /// <inheritdoc />
    public IConnectionStringSource? Resolve(string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return null;

        if (string.Equals(sourceKey, EnvSource.Key, StringComparison.OrdinalIgnoreCase))
            return EnvSource;

        return _cache.GetOrAdd(sourceKey, ResolveFromSatellitePackage);
    }

    private IConnectionStringSource? ResolveFromSatellitePackage(string sourceKey)
    {
        // Only known first-party source keys are resolved dynamically - mirrors
        // ProviderDriverNotFoundException.PackageSuffixesByProvider's closed-set convention.
        // An unrecognized key returns null (JD0033) rather than guessing an assembly name.
        if (!AssemblyFileNamesByKey.TryGetValue(sourceKey, out var assemblyFileName))
            return null;

        var assemblyPath = EnumerateCandidateDirectories(sourceKey)
            .Select(dir => Path.Combine(dir, assemblyFileName))
            .FirstOrDefault(File.Exists);

        if (assemblyPath is null)
            return null;

        try
        {
            var assembly = ProviderAssemblyLoader.LoadFromPath(assemblyPath);
            return CreateSourceInstance(assembly)
                ?? throw new InvalidOperationException(
                    $"Connection-string source assembly '{assemblyPath}' for key '{sourceKey}' does not contain a concrete IConnectionStringSource implementation.");
        }
        catch (ReflectionTypeLoadException ex)
        {
            // A missing transitive dependency surfaces as ReflectionTypeLoadException from
            // Assembly.GetTypes() (inside CreateSourceInstance) with the real cause buried in
            // LoaderExceptions - flatten those into the message so the user sees the actual
            // missing dependency instead of the generic "Unable to load one or more of the
            // requested types" text.
            throw new InvalidOperationException(
                $"Found connection-string source assembly '{assemblyPath}' for key '{sourceKey}' but failed to load its types: {FlattenLoaderExceptions(ex)}", ex);
        }
        catch (InvalidOperationException)
        {
            // Already a clear, actionable message (e.g. no IConnectionStringSource implementation);
            // rethrow as-is rather than double-wrapping it below.
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Found connection-string source assembly '{assemblyPath}' for key '{sourceKey}' but failed to load or instantiate it: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Flattens the <see cref="ReflectionTypeLoadException.LoaderExceptions"/> of a
    /// <paramref name="ex"/> into a single human-readable string, joining the distinct underlying
    /// loader-failure messages (typically a missing transitive dependency) so they surface in the
    /// thrown error instead of being silently dropped.
    /// </summary>
    internal static string FlattenLoaderExceptions(ReflectionTypeLoadException ex)
    {
        var messages = (ex.LoaderExceptions ?? [])
            .Where(e => e is not null)
            .Select(e => e!.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return messages.Length == 0
            ? ex.Message
            : string.Join(" | ", messages);
    }

    /// <summary>
    /// Enumerates, in search order, the directories that may contain a satellite connection-
    /// string source's assembly: first the bundled <c>connstr-sources/{key}/</c> folder next to
    /// this assembly, then each caller-supplied search path in order. Caller-supplied paths that
    /// are null, empty, whitespace, or don't exist on disk are skipped defensively.
    /// </summary>
    internal IEnumerable<string> EnumerateCandidateDirectories(string sourceKey)
    {
        var taskDirectory = Path.GetDirectoryName(typeof(SatelliteConnectionStringSourceResolver).Assembly.Location);
        if (!string.IsNullOrEmpty(taskDirectory))
            yield return Path.Combine(taskDirectory, "connstr-sources", sourceKey);

        foreach (var path in _searchPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (!Directory.Exists(path))
                continue;

            yield return path;
        }
    }

    /// <summary>
    /// Finds the single concrete <see cref="IConnectionStringSource"/> implementation in
    /// <paramref name="assembly"/> and instantiates it via its parameterless constructor.
    /// </summary>
    internal static IConnectionStringSource? CreateSourceInstance(Assembly assembly)
    {
        var sourceType = assembly.GetTypes()
            .FirstOrDefault(t => t is { IsClass: true, IsAbstract: false } && typeof(IConnectionStringSource).IsAssignableFrom(t));

        return sourceType is null
            ? null
            : (IConnectionStringSource?)Activator.CreateInstance(sourceType);
    }
}
