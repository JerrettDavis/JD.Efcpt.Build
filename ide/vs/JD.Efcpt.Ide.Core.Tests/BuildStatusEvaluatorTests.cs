using System;
using JD.Efcpt.Ide.Core;
using Xunit;

namespace JD.Efcpt.Ide.Core.Tests;

/// <summary>
/// Exercises <see cref="BuildStatusEvaluator"/> - the correlation logic that decides whether the
/// on-disk build profile is current, stale, or superseded by a failed regenerate. This is the
/// ubuntu-CI-testable core of the fix for "the tool window shows stale success after a failed
/// regenerate".
/// </summary>
public sealed class BuildStatusEvaluatorTests
{
    private static readonly DateTimeOffset AttemptStart = new(2026, 7, 11, 12, 0, 0, TimeSpan.Zero);

    private static BuildProfile ProfileEndingAt(DateTimeOffset? endTime, string status = "Success") => new()
    {
        SchemaVersion = "1.0.0",
        SchemaSupported = true,
        Status = status,
        StatusValue = BuildProfileStatus.Success,
        EndTime = endTime,
        ModelCount = 2
    };

    [Fact]
    public void No_attempt_and_no_profile_is_NoProfile_without_a_banner()
    {
        var result = BuildStatusEvaluator.Evaluate(lastAttempt: null, profile: null);

        Assert.Equal(BuildStatusFreshness.NoProfile, result.Freshness);
        Assert.Null(result.BannerMessage);
    }

    [Fact]
    public void No_attempt_with_a_profile_is_Current_without_a_banner()
    {
        // A profile that came from a CLI/IDE build (no extension-initiated regenerate to compare
        // against) is presented as current with no caveat.
        var result = BuildStatusEvaluator.Evaluate(lastAttempt: null, ProfileEndingAt(AttemptStart));

        Assert.Equal(BuildStatusFreshness.Current, result.Freshness);
        Assert.Null(result.BannerMessage);
    }

    [Fact]
    public void Profile_written_after_the_attempt_start_is_Current()
    {
        var attempt = new RegenerateAttempt(AttemptStart, succeeded: true, exitCode: 0);
        var profile = ProfileEndingAt(AttemptStart.AddSeconds(5));

        var result = BuildStatusEvaluator.Evaluate(attempt, profile);

        Assert.Equal(BuildStatusFreshness.Current, result.Freshness);
        Assert.Null(result.BannerMessage);
    }

    [Fact]
    public void Failed_attempt_with_a_profile_predating_it_reports_StaleAfterFailedRegenerate()
    {
        var attempt = new RegenerateAttempt(AttemptStart, succeeded: false, exitCode: 1);
        var stale = ProfileEndingAt(AttemptStart.AddMinutes(-10));

        var result = BuildStatusEvaluator.Evaluate(attempt, stale);

        Assert.Equal(BuildStatusFreshness.StaleAfterFailedRegenerate, result.Freshness);
        Assert.NotNull(result.BannerMessage);
        Assert.Contains("FAILED", result.BannerMessage!);
        Assert.Contains("exit code 1", result.BannerMessage!);
        Assert.Contains("earlier run", result.BannerMessage!);
    }

    [Fact]
    public void Failed_attempt_with_no_profile_reports_StaleAfterFailedRegenerate()
    {
        var attempt = new RegenerateAttempt(AttemptStart, succeeded: false, exitCode: 7);

        var result = BuildStatusEvaluator.Evaluate(attempt, profile: null);

        Assert.Equal(BuildStatusFreshness.StaleAfterFailedRegenerate, result.Freshness);
        Assert.NotNull(result.BannerMessage);
        Assert.Contains("exit code 7", result.BannerMessage!);
    }

    [Fact]
    public void Succeeded_attempt_with_a_profile_predating_it_reports_StaleFromEarlierRun()
    {
        // The regenerate succeeded but didn't update the profile (e.g. profiling disabled): the
        // figures shown are from an earlier run and must be flagged as such, not presented as new.
        var attempt = new RegenerateAttempt(AttemptStart, succeeded: true, exitCode: 0);
        var stale = ProfileEndingAt(AttemptStart.AddMinutes(-1));

        var result = BuildStatusEvaluator.Evaluate(attempt, stale);

        Assert.Equal(BuildStatusFreshness.StaleFromEarlierRun, result.Freshness);
        Assert.NotNull(result.BannerMessage);
        Assert.Contains("earlier run", result.BannerMessage!);
    }

    [Fact]
    public void Profile_with_no_EndTime_and_an_attempt_is_treated_as_stale_conservatively()
    {
        // A missing EndTime can't be confirmed as current, so it must NOT be presented as the
        // fresh result of the attempt.
        var attempt = new RegenerateAttempt(AttemptStart, succeeded: true, exitCode: 0);
        var profile = ProfileEndingAt(endTime: null);

        var result = BuildStatusEvaluator.Evaluate(attempt, profile);

        Assert.Equal(BuildStatusFreshness.StaleFromEarlierRun, result.Freshness);
    }

    [Fact]
    public void Profile_EndTime_in_a_different_offset_is_compared_in_UTC()
    {
        // EndTime one hour ahead in +01:00 == AttemptStart in UTC; anything at/after the attempt
        // start counts as current, so this must resolve to Current, not stale.
        var attempt = new RegenerateAttempt(AttemptStart, succeeded: true, exitCode: 0);
        var sameInstantDifferentOffset = new DateTimeOffset(2026, 7, 11, 13, 0, 5, TimeSpan.FromHours(1));
        var profile = ProfileEndingAt(sameInstantDifferentOffset);

        var result = BuildStatusEvaluator.Evaluate(attempt, profile);

        Assert.Equal(BuildStatusFreshness.Current, result.Freshness);
    }
}
