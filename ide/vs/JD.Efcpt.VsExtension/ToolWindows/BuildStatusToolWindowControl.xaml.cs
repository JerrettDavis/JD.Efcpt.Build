using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.Ide.Core;
using JD.Efcpt.VsExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace JD.Efcpt.VsExtension.ToolWindows;

/// <summary>
/// Code-behind for the "JD.Efcpt Build Status" tool window content. Resolves the target project,
/// loads <c>obj/efcpt/build-profile.json</c> via <see cref="BuildProfileReader"/>, correlates it
/// against the last regenerate the extension initiated via <see cref="BuildStatusEvaluator"/>, and
/// watches that file for changes so the tool window refreshes automatically after any build -
/// whether triggered from "JD.Efcpt: Regenerate Models", a normal IDE build, or the CLI.
/// </summary>
public partial class BuildStatusToolWindowControl : UserControl
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly BuildStatusViewModel _viewModel = new();
    private FileSystemWatcher? _watcher;
    private string? _watchedProjectPath;
    private CancellationTokenSource? _debounceCts;

    // The most recent regenerate THIS extension initiated (pushed by RegenerateNotifier), used to
    // correlate the on-disk profile against what the user just asked for.
    private RegenerateAttempt? _lastAttempt;

    // Set when the FileSystemWatcher reports an error (e.g. obj/ deleted by `dotnet clean`, or an
    // internal buffer overflow). Live updates are dead until the watcher is successfully re-armed
    // on a later reload, so we tell the user to click Refresh instead of silently freezing.
    private bool _liveUpdatesStopped;

    /// <summary>Initializes the control and wires up the view model.</summary>
    public BuildStatusToolWindowControl()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // VSTHRD100: these are WPF/event-source handlers, which must return void - there is no
    // Task-returning alternative signature to switch to. Each one wraps its whole body in
    // try/catch and, on failure, both routes the exception through Exception.LogAsync() AND sets
    // the view model to a visible error state, so a failure can never crash the process nor leave
    // the UI silently showing stale/placeholder text.
#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Idempotent (re)subscription: the control may be Loaded more than once over its lifetime,
        // and constructor-time subscription would leak if Unloaded fired without a matching Loaded.
        RegenerateNotifier.RegenerateCompleted -= OnRegenerateCompleted;
        RegenerateNotifier.RegenerateCompleted += OnRegenerateCompleted;

        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        RegenerateNotifier.RegenerateCompleted -= OnRegenerateCompleted;
        _debounceCts?.Cancel();
        DisposeWatcher();
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnRegenerateCompleted(object? sender, RegenerateAttempt attempt)
    {
        // Record the outcome and re-render immediately. This is the key fix for a FAILED regenerate
        // that never rewrote build-profile.json (so the watcher never fires): the evaluator now
        // surfaces the failure/staleness instead of leaving the previous green run showing.
        _lastAttempt = attempt;

        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

    private async Task ReloadAsync()
    {
        var resolution = await ProjectResolver.ResolveTargetProjectAsync().ConfigureAwait(true);
        var projectPath = resolution.ProjectPath;

        if (projectPath is null)
        {
            DisposeWatcher();
            var message = "No project in the solution references the JD.Efcpt.Build NuGet package.";
            if (resolution.Skipped.Count > 0)
                message += $" ({resolution.Skipped.Count} project(s) could not be read.)";
            _viewModel.SetUnavailable(message);
            return;
        }

        EnsureWatcher(projectPath);

        var profilePath = ProjectResolver.GetBuildProfilePath(projectPath);
        BuildProfile? profile = null;

        if (File.Exists(profilePath))
        {
            try
            {
                profile = BuildProfileReader.ReadFile(profilePath);
            }
            catch (BuildProfileParseException ex)
            {
                _viewModel.SetUnavailable($"Failed to parse build-profile.json: {ex.Message}");
                return;
            }
        }

        var evaluation = BuildStatusEvaluator.Evaluate(_lastAttempt, profile);
        _viewModel.Render(Path.GetFileNameWithoutExtension(projectPath), profile, evaluation);
    }

    private void EnsureWatcher(string projectPath)
    {
        // Re-arm when the path changed OR when a prior watcher error left live updates stopped.
        if (_watcher != null &&
            !_liveUpdatesStopped &&
            string.Equals(_watchedProjectPath, projectPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        DisposeWatcher();

        var profileDirectory = Path.GetDirectoryName(ProjectResolver.GetBuildProfilePath(projectPath));
        if (string.IsNullOrEmpty(profileDirectory))
            return;

        // obj/efcpt may not exist until the first profiled build runs; watch the nearest existing
        // ancestor directory (recursively) so the watcher survives and picks up that first build
        // too, since FileSystemWatcher requires its target directory to exist at construction time.
        var watchDirectory = profileDirectory!;
        while (!Directory.Exists(watchDirectory))
        {
            var parent = Path.GetDirectoryName(watchDirectory);
            if (string.IsNullOrEmpty(parent) || string.Equals(parent, watchDirectory, StringComparison.OrdinalIgnoreCase))
                return;
            watchDirectory = parent!;
        }

        try
        {
            _watcher = new FileSystemWatcher(watchDirectory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            _watcher.Changed += OnBuildProfileChanged;
            _watcher.Created += OnBuildProfileChanged;
            _watcher.Renamed += OnBuildProfileChanged;
            _watcher.Error += OnWatcherError;
            _watcher.EnableRaisingEvents = true;
            _watchedProjectPath = projectPath;
            _liveUpdatesStopped = false;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            // Constructing/arming the watcher can fail (directory vanished mid-setup, permissions,
            // an invalid path). Fall back to manual refresh rather than throwing out of a reload;
            // the next reload will attempt to re-arm.
            DisposeWatcher();
            _liveUpdatesStopped = true;
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnBuildProfileChanged(object sender, FileSystemEventArgs e)
    {
        if (!string.Equals(Path.GetFileName(e.FullPath), "build-profile.json", StringComparison.OrdinalIgnoreCase))
            return;

        // Debounce: a single profiled build can touch the file more than once in quick succession.
        _debounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        try
        {
            await Task.Delay(DebounceDelay, cts.Token).ConfigureAwait(true);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cts.Token);
            await ReloadAsync();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer change event - ignore.
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // The watcher is dead after an internal error (common causes: the watched directory was
        // deleted by `dotnet clean`, or the internal event buffer overflowed under a heavy build).
        // Mark live updates as stopped, tell the user to Refresh, and re-arm on the next reload
        // rather than freezing the panel permanently.
        _liveUpdatesStopped = true;

        try
        {
            await e.GetException().LogAsync();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _viewModel.SetUnavailable(
                "Live updates stopped (the watched directory was removed or changed) - click Refresh to reload.");
        }
        catch (Exception ex)
        {
            await ex.LogAsync();
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

    private async Task ReportErrorAsync(Exception ex)
    {
        await ex.LogAsync();

        // Also reflect the failure in the UI so it doesn't sit on stale/placeholder text.
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _viewModel.SetUnavailable($"Error loading build status: {ex.Message}");
        }
        catch (Exception uiEx)
        {
            await uiEx.LogAsync();
        }
    }

    private void DisposeWatcher()
    {
        if (_watcher is null)
            return;

        _watcher.Changed -= OnBuildProfileChanged;
        _watcher.Created -= OnBuildProfileChanged;
        _watcher.Renamed -= OnBuildProfileChanged;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        _watcher = null;
        _watchedProjectPath = null;
    }
}
