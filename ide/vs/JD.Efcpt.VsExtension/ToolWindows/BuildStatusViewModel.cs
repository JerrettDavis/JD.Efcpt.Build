using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using JD.Efcpt.Ide.Core;

namespace JD.Efcpt.VsExtension.ToolWindows;

/// <summary>
/// View model backing <see cref="BuildStatusToolWindowControl"/>, populated from
/// <see cref="BuildProfileReader"/>.
/// </summary>
internal sealed class BuildStatusViewModel : INotifyPropertyChanged
{
    private string _projectName = "(no project)";
    private string _statusMessage =
        "No build-profile.json found yet. Run \"JD.Efcpt: Regenerate Models\" with profiling enabled.";
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

    /// <summary>True once a build profile has been successfully loaded.</summary>
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
    /// Resets the view model to an "unavailable" state (no project resolved, no profile found, or
    /// the profile failed to parse), showing <paramref name="message"/> to the user.
    /// </summary>
    public void SetUnavailable(string message)
    {
        IsProfileAvailable = false;
        ProjectName = "(no project)";
        StatusMessage = message;
        Status = "Unknown";
        ModelCount = 0;
        LastRunTime = "-";
        Duration = "-";
        WarningCount = 0;
        ErrorCount = 0;
        Diagnostics.Clear();
    }

    /// <summary>Populates the view model from a successfully parsed <see cref="BuildProfile"/>.</summary>
    /// <param name="projectName">The resolved project's display name.</param>
    /// <param name="profile">The parsed build profile.</param>
    public void LoadFromProfile(string projectName, BuildProfile profile)
    {
        IsProfileAvailable = true;
        ProjectName = projectName;
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

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
