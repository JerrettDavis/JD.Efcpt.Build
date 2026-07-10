using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Loads satellite provider adapter assemblies (e.g. <c>JD.Efcpt.Build.PostgreSQL.dll</c>) from
/// disk and ensures their own driver dependencies (e.g. <c>Npgsql.dll</c>) resolve correctly even
/// though they live outside the main task assembly's directory.
/// </summary>
/// <remarks>
/// <para>
/// This mirrors <see cref="TaskAssemblyResolver"/>'s dual-path loading strategy -
/// <c>AssemblyLoadContext.Default.Resolving</c> on net8+ and <c>AppDomain.AssemblyResolve</c> on
/// net472 - but probes a dynamic, growing set of provider directories instead of the single fixed
/// task directory. Every directory that a provider adapter assembly was loaded from is registered
/// here so that the CLR can find that provider's transitive dependencies when it JITs the adapter's
/// methods, without requiring those dependencies to be copied next to <c>JD.Efcpt.Build.Tasks.dll</c>.
/// </para>
/// <para>
/// This class is excluded from code coverage for the same reason as <see cref="TaskAssemblyResolver"/>:
/// it is MSBuild/CLR loader infrastructure that only activates during assembly resolution, which
/// requires real on-disk satellite packages to exercise meaningfully.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class ProviderAssemblyLoader
{
    private static readonly List<string> ProviderDirectories = [];
    private static readonly object Lock = new();
    private static bool _initialized;

    /// <summary>
    /// Loads the assembly at <paramref name="assemblyPath"/> and registers its containing
    /// directory so that the assembly's own dependencies can be resolved later.
    /// </summary>
    /// <param name="assemblyPath">Full path to the provider adapter assembly.</param>
    /// <returns>The loaded <see cref="Assembly"/>.</returns>
    public static Assembly LoadFromPath(string assemblyPath)
    {
        var directory = Path.GetDirectoryName(assemblyPath);
        if (!string.IsNullOrEmpty(directory))
            RegisterDirectory(directory);

#if NETFRAMEWORK
        return Assembly.LoadFrom(assemblyPath);
#else
        return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#endif
    }

    /// <summary>
    /// Registers a directory to be probed when a not-yet-resolved assembly is requested,
    /// and ensures the resolver hook is armed. Safe to call multiple times with the same
    /// directory or from multiple threads.
    /// </summary>
    private static void RegisterDirectory(string directory)
    {
        lock (Lock)
        {
            if (!ProviderDirectories.Contains(directory, StringComparer.OrdinalIgnoreCase))
                ProviderDirectories.Add(directory);

            if (_initialized)
                return;

            _initialized = true;
#if NETFRAMEWORK
            AppDomain.CurrentDomain.AssemblyResolve += OnResolvingFramework;
#else
            AssemblyLoadContext.Default.Resolving += OnResolving;
#endif
        }
    }

    private static string? TryFindAssembly(string simpleName)
    {
        string[] directoriesSnapshot;
        lock (Lock)
        {
            directoriesSnapshot = ProviderDirectories.ToArray();
        }

        return directoriesSnapshot
            .Select(dir => Path.Combine(dir, $"{simpleName}.dll"))
            .FirstOrDefault(File.Exists);
    }

#if NETFRAMEWORK
    private static Assembly? OnResolvingFramework(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);
        var path = TryFindAssembly(assemblyName.Name ?? "");
        if (path is null)
            return null;

        try
        {
            return Assembly.LoadFrom(path);
        }
        catch
        {
            // If loading fails, let other resolvers try.
            return null;
        }
    }
#else
    private static Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        var path = TryFindAssembly(name.Name ?? "");
        if (path is null)
            return null;

        try
        {
            return context.LoadFromAssemblyPath(path);
        }
        catch
        {
            // If loading fails, let other resolvers try.
            return null;
        }
    }
#endif
}
