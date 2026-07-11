namespace JD.Efcpt.Build.Tasks.Utilities;

/// <summary>
/// The result of an SDK/dnx capability probe, distinguishing a genuine (stable) answer from a
/// transient failure that must not be treated as a permanent negative result.
/// </summary>
/// <remarks>
/// Before probe results were cached (see <see cref="SdkProbeCache"/>), every call re-spawned
/// the probe process, so a one-off failure (a launch hiccup, a timeout under load, etc.) could
/// always "self-heal" on the next call. Once results are memoized for the build session, a
/// transient failure must be distinguishable from a real, determinate answer - otherwise it
/// gets cached as a permanent negative and never retried.
/// </remarks>
internal enum ProbeOutcome
{
    /// <summary>
    /// The probe ran to completion and the probed capability is present. This is a real,
    /// stable answer and is safe to memoize for the remainder of the build session.
    /// </summary>
    Available,

    /// <summary>
    /// The probe ran to completion and the probed capability is confirmed absent. This is a
    /// real, stable answer and is safe to memoize for the remainder of the build session.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The probe could not produce a determinate answer - e.g. the process failed to launch,
    /// threw an unexpected exception, or timed out. This is <em>not</em> a genuine
    /// "capability absent" result and must not be memoized; the caller should be given the
    /// negative result for this one call only, so the next call retries the probe from scratch.
    /// </summary>
    Transient
}
