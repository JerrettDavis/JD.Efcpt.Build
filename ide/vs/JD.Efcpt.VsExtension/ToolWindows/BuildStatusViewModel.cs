using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using JD.Efcpt.Ide.Core;

namespace JD.Efcpt.VsExtension.ToolWindows;

/// <summary>
/// View model backing <see cref="BuildStatusToolWindowControl"/>, populated from
/// <see cref="BuildProfileReader"/> and correlated against the last regenerate the extension
/// initiated via the unit-tested <see cref="BuildStatusEvaluator"/>.
/// </summary>
internal sealed class BuildStatusViewModel : INotifyPropertyChanged
{
    private const string NoProfilePlaceholder =
        "No build-profile.json found yet. Run \"JD.Efcpt: Regenerate Models\" with profiling enabled.";

    private string _projectName = "(no project)";
    private string _statusMessage = NoProfilePlaceholder;
    private string _banner = string.Empty;
    private bool _hasBanner;
    private bool _isProfileAvailable;
    private string _status = "Unknown";
    private int _modelCount;
    private string _lastRunTime = "-";
    private string _duration = "-";
    private int _warningCount;
    private int _errorCount;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The resolved project's name, or a placeholder when none is resolved.</summary>
    public string ProjectName { get => _projectName; private set => SetField(ref _projectName, value); }

    /// <summary>A human-readable status/error message shown above the details grid.</summary>
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }

    /// <summary>
    /// A staleness/failure banner (from <see cref="BuildStatusEvaluator"/>) shown prominently above
    /// the figures - e.g. "Last regenerate FAILED..." - or empty when the profile is current.
    /// </summary>
    public string Banner { get => _banner; private set => SetField(ref _banner, value); }

    /// <summary>Whether <see cref="Banner"/> has content (drives the banner element's visibility).</summary>
    public bool HasBanner { get => _hasBanner; private set => SetField(ref _hasBanner, value); }

    /// <summary>True once a build profile has been successfully loaded (drives figure visibility).</summary>
    public bool IsProfileAvailable { get => _isProfileAvailable; private set => SetField(ref _isProfileAvailable, value); }

    /// <summary>The raw build status (e.g. <c>"Success"</c>, <c>"Failed"</c>).</summary>
    public string Status { get => _status; private set => SetField(ref _status, value); }

    /// <summary>Count of generated model artifacts from the last profiled build.</summary>
    public int ModelCount { get => _modelCount; private set => SetField(ref _modelCount, value); }

    /// <summary>Formatted local end-time of the last profiled build.</summary>
    public string LastRunTime { get => _lastRunTime; private set => SetField(ref _lastRunTime, value); }

    /// <summary>Formatted duration of the last profiled build.</summary>
    public string Duration { get => _duration; private set => SetField(ref _duration, value); }

    /// <summary>Count of warning-severity diagnostics in the last profiled build.</summary>
    public int WarningCount { get => _warningCount; private set => SetField(ref _warningCount, value); }

    /// <summary>Count of error-severity diagnostics in the last profiled build.</summary>
    public int ErrorCount { get => _errorCount; private set => SetField(ref _errorCount, value); }

    /// <summary>All diagnostics from the last profiled build, for the details list.</summary>
    public ObservableCollection<BuildProfileDiagnostic> Diagnostics { get; } = new();

    /// <summary>
    /// Resets the view model to an "unavailable" state (no project resolved, a read/parse error, or
    /// live updates stopped), showing <paramref name="message"/> to the user and clearing figures.
    /// </summary>
    public void SetUnavailable(string message)
    {
        IsProfileAvailable = false;
        ProjectName = "(no project)";
        StatusMessage = message;
        Banner = string.Empty;
        HasBanner = false;
        ClearFigures();
    }

    /// <summary>
    /// Renders the tool window from the loaded profile (which may be <see langword="null"/> when no
    /// profile exists on disk) and the correlation <paramref name="evaluation"/> against the last
    /// regenerate the extension initiated.
    /// </summary>
    /// <param name="projectName">The resolved project's display name.</param>
    /// <param name="profile">The parsed build profile, or <see langword="null"/> when none exists.</param>
    /// <param name="evaluation">The freshness classification + optional banner from <see cref="BuildStatusEvaluator"/>.</param>
    public void Render(string projectName, BuildProfile? profile, BuildStatusEvaluation evaluation)
    {
        ProjectName = projectName;
        Banner = evaluation.BannerMessage ?? string.Empty;
        HasBanner = !string.IsNullOrEmpty(Banner);

        if (profile is null)
        {
            IsProfileAvailable = false;
            ClearFigures();
            // When a failed regenerate left no profile, the banner already carries the message; the
            // placeholder would be redundant, so suppress it in that case.
            StatusMessage = evaluation.Freshness == BuildStatusFreshness.StaleAfterFailedRegenerate
                ? string.Empty
                : NoProfilePlaceholder;
            return;
        }

        IsProfileAvailable = true;
        Status = profile.Status;
        ModelCount = profile.ModelCount;
        LastRunTime = profile.EndTime is { } endTime
            ? endTime.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
            : "-";
        Duration = profile.Duration is { } duration
            ? duration.ToString("g", CultureInfo.InvariantCulture)
            : "-";

        Diagnostics.Clear();
        var warningCount = 0;
        var errorCount = 0;
        foreach (var diagnostic in profile.Diagnostics)
        {
            Diagnostics.Add(diagnostic);
            if (string.Equals(diagnostic.Severity, "warning", StringComparison.OrdinalIgnoreCase))
                warningCount++;
            else if (string.Equals(diagnostic.Severity, "error", StringComparison.OrdinalIgnoreCase))
                errorCount++;
        }

        WarningCount = warningCount;
        ErrorCount = errorCount;

        StatusMessage = profile.SchemaSupported
            ? string.Empty
            : $"build-profile.json schema {profile.SchemaVersion} is newer than this extension understands; some fields may be missing.";
    }

    private void ClearFigures()
    {
        Status = "Unknown";
        ModelCount = 0;
        LastRunTime = "-";
        Duration = "-";
        WarningCount = 0;
        ErrorCount = 0;
        Diagnostics.Clear();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
