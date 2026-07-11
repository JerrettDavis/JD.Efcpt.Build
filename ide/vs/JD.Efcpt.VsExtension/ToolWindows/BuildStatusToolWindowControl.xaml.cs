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
/// loads <c>obj/efcpt/build-profile.json</c> via <see cref="BuildProfileReader"/>, and watches
/// that file for changes so the tool window refreshes automatically after any build - whether
/// triggered from "JD.Efcpt: Regenerate Models", a normal IDE build, or the CLI.
/// </summary>
public partial class BuildStatusToolWindowControl : UserControl
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);

    private readonly BuildStatusViewModel _viewModel = new();
    private FileSystemWatcher? _watcher;
    private string? _watchedProjectPath;
    private CancellationTokenSource? _debounceCts;

    /// <summary>Initializes the control and wires up the view model.</summary>
    public BuildStatusToolWindowControl()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // VSTHRD100: these are WPF event handlers, which must return void - there is no Task-returning
    // alternative signature to switch to. Each one wraps its whole body in try/catch and routes
    // unhandled exceptions through Exception.LogAsync() so a failure can never crash the process.
#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ex.LogAsync();
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
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
            await ex.LogAsync();
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

    private async Task ReloadAsync()
    {
        var projectPath = await ProjectResolver.ResolveTargetProjectAsync().ConfigureAwait(true);
        if (projectPath is null)
        {
            DisposeWatcher();
            _viewModel.SetUnavailable("No project in the solution references the JD.Efcpt.Build NuGet package.");
            return;
        }

        EnsureWatcher(projectPath);

        var profilePath = ProjectResolver.GetBuildProfilePath(projectPath);
        if (!File.Exists(profilePath))
        {
            _viewModel.SetUnavailable(
                "No build-profile.json found yet. Run \"JD.Efcpt: Regenerate Models\" with profiling enabled.");
            return;
        }

        try
        {
            var profile = BuildProfileReader.ReadFile(profilePath);
            _viewModel.LoadFromProfile(Path.GetFileNameWithoutExtension(projectPath), profile);
        }
        catch (BuildProfileParseException ex)
        {
            _viewModel.SetUnavailable($"Failed to parse build-profile.json: {ex.Message}");
        }
    }

    private void EnsureWatcher(string projectPath)
    {
        if (_watcher != null && string.Equals(_watchedProjectPath, projectPath, StringComparison.OrdinalIgnoreCase))
            return;

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

        _watcher = new FileSystemWatcher(watchDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };
        _watcher.Changed += OnBuildProfileChanged;
        _watcher.Created += OnBuildProfileChanged;
        _watcher.Renamed += OnBuildProfileChanged;
        _watcher.EnableRaisingEvents = true;
        _watchedProjectPath = projectPath;
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
            await ex.LogAsync();
        }
    }
#pragma warning restore VSTHRD100 // Avoid async void methods

    private void DisposeWatcher()
    {
        if (_watcher is null)
            return;

        _watcher.Changed -= OnBuildProfileChanged;
        _watcher.Created -= OnBuildProfileChanged;
        _watcher.Renamed -= OnBuildProfileChanged;
        _watcher.Dispose();
        _watcher = null;
        _watchedProjectPath = null;
    }
}
