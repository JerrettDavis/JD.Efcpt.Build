using System.Collections.Concurrent;
using System.Globalization;

namespace JD.Efcpt.Build.Core.Diagnostics;

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
/// <c>dotnet</c> path or the muxer binary itself changes (e.g. the muxer is replaced in place
/// mid-session). Note this mtime check does not reliably detect a side-by-side SDK install that
/// leaves the muxer binary itself untouched.
/// </para>
/// <para>
/// Callers typically pass a bare command name (e.g. <c>"dotnet"</c>) rather than a full path.
/// Because a bare name resolves relative to the current working directory (not <c>PATH</c>)
/// when used directly for a file-time lookup, the key is built from a <c>PATH</c>-resolved
/// full path (see <see cref="ResolveDotnetExecutable"/>) so the mtime stamp is actually
/// meaningful. If resolution genuinely fails (the muxer can't be found anywhere on
/// <c>PATH</c>), invalidation degrades from mtime-based to session-only: the probe is still
/// cached (and still cleared on <see cref="Clear"/> / process exit), it just won't
/// automatically detect an in-place binary replacement while the session is running.
/// </para>
/// <para>
/// Only a <em>determinate</em> probe result (see <see cref="ProbeOutcome.Available"/> /
/// <see cref="ProbeOutcome.Unavailable"/>) is memoized. A <see cref="ProbeOutcome.Transient"/>
/// result (process launch failure, timeout, unexpected exception) is deliberately not cached,
/// so a one-off hiccup doesn't get "poisoned" into a permanent negative for the rest of the
/// build session - the next call retries the probe from scratch.
/// </para>
/// <para>
/// Concurrency is handled via <see cref="Lazy{T}"/> with
/// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>, so even if multiple threads
/// race to probe the same key simultaneously, the underlying probe factory delegate is
/// guaranteed to execute exactly once; all callers observe the same result.
/// </para>
/// <para>
/// This cache is a single static class shared by every caller loaded into the process -
/// including <c>JD.Efcpt.Build.Tasks.RunEfcpt</c>'s own SDK/dnx probes and this assembly's
/// <c>DotNetToolUtilities</c> and <c>DefaultSdkProbe</c> - so probe results (and their cache
/// keys) are genuinely shared across both assemblies as long as callers use the same
/// probe-name/<c>dotnetExe</c> pair.
/// </para>
/// </remarks>
public static class SdkProbeCache
{
    /// <summary>
    /// Sentinel stamp value used when the muxer path cannot be resolved to an existing file
    /// (empty input, unresolvable bare command, or a lookup failure). See the type-level
    /// remarks for the invalidation trade-off this implies.
    /// </summary>
    private const string UnresolvedStamp = "unresolved";

    /// <summary>
    /// Backing store mapping a composite probe key to a lazily-evaluated, memoized result.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<ProbeOutcome>> Cache = new();

    /// <summary>
    /// Returns the cached result for the given probe, invoking <paramref name="probe"/> at most
    /// once per distinct key for the lifetime of the process - unless the probe reports a
    /// <see cref="ProbeOutcome.Transient"/> failure, in which case nothing is cached and the
    /// next call retries from scratch.
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
    /// cached determinate result exists yet for the resolved key. If <paramref name="probe"/>
    /// throws, the cache itself coerces the failure to <see cref="ProbeOutcome.Transient"/>
    /// rather than memoizing (and forever rethrowing) the exception via <see cref="Lazy{T}"/> -
    /// callers do not need to guard against exceptions from <see cref="GetOrProbe"/> themselves.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the probe result (fresh or cached) is
    /// <see cref="ProbeOutcome.Available"/>; otherwise <see langword="false"/> (this includes
    /// <see cref="ProbeOutcome.Transient"/>, which is reported as a negative result for this
    /// call only and is never cached).
    /// </returns>
    public static bool GetOrProbe(string probeName, string? dotnetExe, Func<ProbeOutcome> probe)
    {
        var key = BuildKey(probeName, dotnetExe);
        var lazy = Cache.GetOrAdd(key, _ => new Lazy<ProbeOutcome>(
            () => { try { return probe(); } catch { return ProbeOutcome.Transient; } },
            LazyThreadSafetyMode.ExecutionAndPublication));
        var outcome = lazy.Value;

        if (outcome == ProbeOutcome.Transient)
        {
            // Don't let a transient failure poison the cache for the rest of the build session.
            // Atomically remove this exact (still-transient) entry - if another thread already
            // replaced it with a fresh attempt, this is a no-op - so the next caller gets a
            // clean retry. This call itself reports the negative result without caching it.
            ((ICollection<KeyValuePair<string, Lazy<ProbeOutcome>>>)Cache).Remove(
                new KeyValuePair<string, Lazy<ProbeOutcome>>(key, lazy));
            return false;
        }

        return outcome == ProbeOutcome.Available;
    }

    /// <summary>
    /// Clears all cached probe results. Intended for test isolation; production code should
    /// never need to call this since the cache is scoped to a single build session (process).
    /// </summary>
    public static void Clear() => Cache.Clear();

    /// <summary>
    /// Builds the composite cache key for a probe: the probe name, the caller-supplied dotnet
    /// executable string, and a "stamp" derived from the resolved muxer binary's
    /// last-write-time. The stamp ensures the cache is automatically invalidated if the dotnet
    /// path is repointed at a different (or upgraded) binary within the same process, without
    /// requiring an extra process spawn to re-check versions explicitly.
    /// </summary>
    /// <param name="probeName">The probe kind identifier.</param>
    /// <param name="dotnetExe">Path to (or bare name of) the dotnet muxer executable.</param>
    /// <returns>A composite string key suitable for use in <see cref="Cache"/>.</returns>
    private static string BuildKey(string probeName, string? dotnetExe) =>
        probeName + "|" + (dotnetExe ?? string.Empty) + "|" + GetMuxerStamp(ResolveDotnetExecutable(dotnetExe));

    /// <summary>
    /// Resolves <paramref name="dotnetExe"/> to a fully-qualified path on disk, so its
    /// last-write-time can be used as a reliable cache-invalidation stamp.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers commonly pass a bare command name such as <c>"dotnet"</c> rather than a full
    /// path. <see cref="File.GetLastWriteTimeUtc(string)"/> resolves a bare name relative to
    /// the current working directory (not <c>PATH</c>), so without this step the mtime lookup
    /// would almost always fail and the cache key's stamp component would be constant -
    /// meaning the cache would never invalidate when the SDK/muxer is upgraded.
    /// </para>
    /// <para>Resolution rules:</para>
    /// <list type="bullet">
    /// <item><description>
    /// If <paramref name="dotnetExe"/> already contains a directory separator, it is treated as
    /// already qualified and resolved via <see cref="Path.GetFullPath(string)"/> (provided the
    /// file exists).
    /// </description></item>
    /// <item><description>
    /// Otherwise, it is treated as a bare command and each directory in the <c>PATH</c>
    /// environment variable is searched (split on <see cref="Path.PathSeparator"/> - <c>;</c>
    /// on Windows, <c>:</c> on Unix-like platforms), trying both the bare name and, if not
    /// already present, a <c>.exe</c>-suffixed variant.
    /// </description></item>
    /// </list>
    /// <para>
    /// Returns <see langword="null"/> if <paramref name="dotnetExe"/> is empty or cannot be
    /// resolved to an existing file anywhere on <c>PATH</c>.
    /// </para>
    /// </remarks>
    /// <param name="dotnetExe">The bare command or path supplied by the caller (e.g. <c>"dotnet"</c>).</param>
    /// <returns>The fully-qualified path if resolved; otherwise <see langword="null"/>.</returns>
    public static string? ResolveDotnetExecutable(string? dotnetExe)
    {
        if (string.IsNullOrEmpty(dotnetExe))
            return null;

        if (dotnetExe.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
            dotnetExe.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
        {
            try
            {
                return File.Exists(dotnetExe) ? Path.GetFullPath(dotnetExe) : null;
            }
            catch
            {
                return null;
            }
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var candidateNames = dotnetExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? new[] { dotnetExe }
            : new[] { dotnetExe, dotnetExe + ".exe" };

        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            foreach (var name in candidateNames)
            {
                string candidate;
                try
                {
                    candidate = Path.Combine(dir, name);
                }
                catch
                {
                    continue;
                }

                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the last-write-time (UTC ticks) of the resolved muxer binary as a lightweight
    /// invalidation stamp. Returns <see cref="UnresolvedStamp"/> if the path could not be
    /// resolved (see <see cref="ResolveDotnetExecutable"/>) or the lookup throws for any reason
    /// (e.g. access denied) - in all such cases the probe should still be safely
    /// cacheable/re-runnable without crashing the build, but invalidation degrades to
    /// session-only rather than mtime-based (see the type-level remarks).
    /// </summary>
    /// <param name="resolvedDotnetExe">
    /// The fully-qualified muxer path, as returned by <see cref="ResolveDotnetExecutable"/>, or
    /// <see langword="null"/> if resolution failed.
    /// </param>
    /// <returns>The last-write-time in UTC ticks as an invariant-culture string, or <see cref="UnresolvedStamp"/>.</returns>
    private static string GetMuxerStamp(string? resolvedDotnetExe)
    {
        if (string.IsNullOrEmpty(resolvedDotnetExe))
            return UnresolvedStamp;

        try
        {
            return File.GetLastWriteTimeUtc(resolvedDotnetExe).Ticks.ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            return UnresolvedStamp;
        }
    }
}
