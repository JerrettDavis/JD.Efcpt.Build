using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace JD.Efcpt.Build.Tasks.Utilities;

/// <summary>
/// Process-wide, thread-safe memoization cache for expensive .NET SDK/dnx capability probes
/// (e.g. <c>dotnet --list-sdks</c>, <c>dotnet dnx --help</c>, <c>dotnet --list-runtimes</c>).
/// </summary>
/// <remarks>
/// <para>
/// Each probe shells out to an external process and blocks for up to a multi-second timeout.
/// Without caching, every MSBuild task invocation (e.g. once per project in a multi-project
/// <c>-m</c> build) re-runs the same probe against the same <c>dotnet</c> muxer, multiplying
/// the cost needlessly. This cache ensures a given probe only executes once per distinct
/// <c>(probeName, dotnetExe, muxer-last-write-time)</c> key for the lifetime of the process
/// (i.e. a single build session), while still invalidating automatically if the resolved
/// <c>dotnet</c> path or the muxer binary itself changes (e.g. an SDK upgrade mid-session).
/// </para>
/// <para>
/// Concurrency is handled via <see cref="Lazy{T}"/> with
/// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>, so even if multiple threads
/// race to probe the same key simultaneously, the underlying probe factory delegate is
/// guaranteed to execute exactly once; all callers observe the same result.
/// </para>
/// </remarks>
internal static class SdkProbeCache
{
    /// <summary>
    /// Backing store mapping a composite probe key to a lazily-evaluated, memoized result.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<bool>> Cache = new();

    /// <summary>
    /// Returns the cached result for the given probe, invoking <paramref name="probe"/> at most
    /// once per distinct key for the lifetime of the process.
    /// </summary>
    /// <param name="probeName">
    /// A short, stable identifier for the probe kind (e.g. <c>"list-sdks"</c>, <c>"dnx-help"</c>).
    /// Probes that run the same underlying command against the same <paramref name="dotnetExe"/>
    /// should use the same <paramref name="probeName"/> so results are shared across callers.
    /// </param>
    /// <param name="dotnetExe">
    /// Path to (or name of) the <c>dotnet</c> muxer executable used for the probe. May be
    /// <see langword="null"/> or empty; treated as part of the cache key regardless.
    /// </param>
    /// <param name="probe">
    /// The factory delegate that performs the actual (expensive) probe. Only invoked when no
    /// cached result exists yet for the resolved key.
    /// </param>
    /// <returns>The probe result, either freshly computed or retrieved from the cache.</returns>
    internal static bool GetOrProbe(string probeName, string? dotnetExe, Func<bool> probe)
    {
        var key = BuildKey(probeName, dotnetExe);
        return Cache.GetOrAdd(key, _ => new Lazy<bool>(probe, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    /// <summary>
    /// Clears all cached probe results. Intended for test isolation; production code should
    /// never need to call this since the cache is scoped to a single build session (process).
    /// </summary>
    internal static void Clear() => Cache.Clear();

    /// <summary>
    /// Builds the composite cache key for a probe: the probe name, the resolved dotnet
    /// executable path, and a "stamp" derived from the muxer binary's last-write-time. The
    /// stamp ensures the cache is automatically invalidated if the dotnet path is repointed at
    /// a different (or upgraded) binary within the same process, without requiring an extra
    /// process spawn to re-check versions explicitly.
    /// </summary>
    /// <param name="probeName">The probe kind identifier.</param>
    /// <param name="dotnetExe">Path to the dotnet muxer executable.</param>
    /// <returns>A composite string key suitable for use in <see cref="Cache"/>.</returns>
    private static string BuildKey(string probeName, string? dotnetExe) =>
        probeName + "|" + (dotnetExe ?? string.Empty) + "|" + GetMuxerStamp(dotnetExe);

    /// <summary>
    /// Reads the last-write-time (UTC ticks) of the muxer binary as a lightweight invalidation
    /// stamp. Returns <c>"0"</c> if the path is empty, the file doesn't exist, or the lookup
    /// throws for any reason (e.g. access denied) - in all such cases the probe should still be
    /// safely cacheable/re-runnable without crashing the build.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet muxer executable.</param>
    /// <returns>The last-write-time in UTC ticks as an invariant-culture string, or <c>"0"</c>.</returns>
    private static string GetMuxerStamp(string? dotnetExe)
    {
        if (string.IsNullOrEmpty(dotnetExe))
            return "0";

        try
        {
            return File.GetLastWriteTimeUtc(dotnetExe).Ticks.ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            return "0";
        }
    }
}
