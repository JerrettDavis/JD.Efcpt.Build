using System;

namespace JD.Efcpt.Ide.Core;

/// <summary>
/// A record of the most recent regenerate the extension itself initiated (via
/// "JD.Efcpt: Regenerate Models"). Used to correlate the on-disk build profile against what the
/// user just asked for, so the build-status tool window never presents a stale prior-run profile
/// as if it were the result of a regenerate that actually failed (or did not update the profile).
/// </summary>
public sealed class RegenerateAttempt
{
    /// <summary>
    /// Initializes a new <see cref="RegenerateAttempt"/>.
    /// </summary>
    /// <param name="startedUtc">
    /// UTC timestamp captured immediately before the <c>dotnet build</c> was launched. A build
    /// profile written by this attempt will have an <see cref="BuildProfile.EndTime"/> at or after
    /// this instant; a profile from an earlier run will predate it.
    /// </param>
    /// <param name="succeeded">Whether the regenerate build exited with code 0.</param>
    /// <param name="exitCode">The <c>dotnet build</c> exit code.</param>
    public RegenerateAttempt(DateTimeOffset startedUtc, bool succeeded, int exitCode)
    {
        StartedUtc = startedUtc;
        Succeeded = succeeded;
        ExitCode = exitCode;
    }

    /// <summary>UTC timestamp captured immediately before the regenerate build was launched.</summary>
    public DateTimeOffset StartedUtc { get; }

    /// <summary>Whether the regenerate build exited with code 0.</summary>
    public bool Succeeded { get; }

    /// <summary>The <c>dotnet build</c> exit code.</summary>
    public int ExitCode { get; }
}

/// <summary>
/// How the on-disk build profile relates to the last regenerate the extension initiated.
/// </summary>
public enum BuildStatusFreshness
{
    /// <summary>No build profile exists on disk yet.</summary>
    NoProfile,

    /// <summary>
    /// The profile is current: it was written at/after the last regenerate the extension
    /// initiated, or there is no initiated regenerate to compare it against (e.g. a CLI build).
    /// </summary>
    Current,

    /// <summary>
    /// The profile predates the last regenerate the extension initiated, and that regenerate
    /// succeeded but did not update the profile (e.g. profiling was disabled) — the displayed
    /// figures are from an earlier run.
    /// </summary>
    StaleFromEarlierRun,

    /// <summary>
    /// The most recent regenerate the extension initiated FAILED, so any displayed figures are
    /// from an earlier (successful) run and must not be presented as the current result.
    /// </summary>
    StaleAfterFailedRegenerate
}

/// <summary>
/// The result of correlating a loaded <see cref="BuildProfile"/> with the last
/// <see cref="RegenerateAttempt"/>.
/// </summary>
public sealed class BuildStatusEvaluation
{
    /// <summary>Initializes a new <see cref="BuildStatusEvaluation"/>.</summary>
    /// <param name="freshness">How the profile relates to the last regenerate attempt.</param>
    /// <param name="bannerMessage">
    /// A user-facing banner to show above the profile figures, or <see langword="null"/> when the
    /// profile can be presented as current with no caveat.
    /// </param>
    public BuildStatusEvaluation(BuildStatusFreshness freshness, string? bannerMessage)
    {
        Freshness = freshness;
        BannerMessage = bannerMessage;
    }

    /// <summary>How the on-disk profile relates to the last regenerate the extension initiated.</summary>
    public BuildStatusFreshness Freshness { get; }

    /// <summary>
    /// A user-facing banner explaining any staleness/failure, or <see langword="null"/> when the
    /// profile is current (<see cref="BuildStatusFreshness.Current"/>) or absent
    /// (<see cref="BuildStatusFreshness.NoProfile"/>, which the UI handles with its own placeholder).
    /// </summary>
    public string? BannerMessage { get; }
}

/// <summary>
/// Pure, unit-testable correlation logic that decides whether a loaded build profile is current,
/// stale, or superseded by a failed regenerate. Kept in <c>JD.Efcpt.Ide.Core</c> (not the VSIX)
/// so it runs on ubuntu CI; the Visual Studio tool window is a thin renderer over its result.
/// </summary>
public static class BuildStatusEvaluator
{
    /// <summary>
    /// Correlates the last regenerate the extension initiated with the profile currently loaded
    /// from <c>obj/efcpt/build-profile.json</c>.
    /// </summary>
    /// <param name="lastAttempt">
    /// The most recent regenerate the extension initiated, or <see langword="null"/> when the
    /// extension has not initiated one this session (e.g. the profile came from a CLI/IDE build).
    /// </param>
    /// <param name="profile">
    /// The loaded build profile, or <see langword="null"/> when no profile exists on disk.
    /// </param>
    /// <returns>The freshness classification and an optional user-facing banner.</returns>
    public static BuildStatusEvaluation Evaluate(RegenerateAttempt? lastAttempt, BuildProfile? profile)
    {
        if (profile is null)
        {
            // No profile on disk. If the last regenerate failed, surface that instead of the
            // neutral "no profile yet" placeholder the UI would otherwise show.
            if (lastAttempt is { Succeeded: false })
            {
                return new BuildStatusEvaluation(
                    BuildStatusFreshness.StaleAfterFailedRegenerate,
                    $"Last regenerate FAILED (exit code {lastAttempt.ExitCode}). No build profile is available.");
            }

            return new BuildStatusEvaluation(BuildStatusFreshness.NoProfile, null);
        }

        // A profile written by `lastAttempt` has EndTime >= the attempt's start; a profile from an
        // earlier run predates it. A missing EndTime can't be confirmed as current, so treat it as
        // predating (conservative) whenever there is an attempt to compare against.
        var profilePredatesAttempt =
            lastAttempt is not null &&
            (profile.EndTime is null || profile.EndTime.Value.ToUniversalTime() < lastAttempt.StartedUtc);

        if (!profilePredatesAttempt)
            return new BuildStatusEvaluation(BuildStatusFreshness.Current, null);

        if (lastAttempt!.Succeeded)
        {
            return new BuildStatusEvaluation(
                BuildStatusFreshness.StaleFromEarlierRun,
                "The status below is from an earlier run; the latest regenerate did not update the build profile.");
        }

        return new BuildStatusEvaluation(
            BuildStatusFreshness.StaleAfterFailedRegenerate,
            $"Last regenerate FAILED (exit code {lastAttempt.ExitCode}) — the status below is from an earlier run.");
    }
}
