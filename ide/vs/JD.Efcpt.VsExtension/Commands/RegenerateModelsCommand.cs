using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.Ide.Core;
using JD.Efcpt.VsExtension.Services;
using Microsoft.VisualStudio.Shell;

namespace JD.Efcpt.VsExtension.Commands;

/// <summary>
/// "Tools &gt; Entity Framework &gt; JD.Efcpt: Regenerate Models".
/// </summary>
/// <remarks>
/// Resolves the target JD.Efcpt.Build project - the active project if it references
/// JD.Efcpt.Build, otherwise the first matching project found anywhere in the solution (works for
/// both single- and multi-project solutions) - then shells out to <c>dotnet build</c> via
/// <see cref="RegenerateModelsService"/>, streaming redacted output into the "JD.Efcpt" Output
/// Window pane.
/// </remarks>
[Command(PackageGuids.JdEfcptCommandSetString, PackageIds.RegenerateModelsCommandId)]
internal sealed class RegenerateModelsCommand : BaseCommand<RegenerateModelsCommand>
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        // Belt-and-braces: BaseCommand.Execute already logs unhandled exceptions, but that is a
        // dev-only ActivityLog write with no user feedback. Wrap the whole flow so any failure is
        // surfaced to the user via the output pane + an error dialog (matching the StartFailed
        // path) rather than relying on the framework's undocumented unhandled-exception behavior.
        try
        {
            await ExecuteCoreAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await ReportUnexpectedFailureAsync(ex).ConfigureAwait(true);
        }
    }

    private async Task ExecuteCoreAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var resolution = await ProjectResolver.ResolveTargetProjectAsync().ConfigureAwait(true);
        var projectPath = resolution.ProjectPath;

        var pane = await JdEfcptOutputPane.GetAsync().ConfigureAwait(true);

        // Surface any candidate projects that couldn't be inspected, so a permissions/lock error
        // is not silently indistinguishable from "no JD.Efcpt project in the solution".
        foreach (var skipped in resolution.Skipped)
            await pane.WriteLineAsync($"Skipped '{skipped.Path}': {skipped.Reason}").ConfigureAwait(true);

        if (projectPath is null)
        {
            await VS.MessageBox.ShowWarningAsync(
                "JD.Efcpt: Regenerate Models",
                "No project in the solution references the JD.Efcpt.Build NuGet package.").ConfigureAwait(true);
            return;
        }

        await pane.ActivateAsync().ConfigureAwait(true);
        await pane.WriteLineAsync(
            $"Regenerating models for '{Path.GetFileNameWithoutExtension(projectPath)}'...").ConfigureAwait(true);

        // Capture the start instant BEFORE launching the build so the tool window can correlate the
        // on-disk profile against this attempt (a profile written by this build will postdate it).
        var startedUtc = DateTimeOffset.UtcNow;

        var result = await RegenerateModelsService
            .RunAsync(projectPath, pane, Package.DisposalToken)
            .ConfigureAwait(true);

        if (result.StartFailed)
        {
            // The build never ran, so there is no meaningful exit code / attempt to correlate.
            await VS.MessageBox.ShowErrorAsync(
                "JD.Efcpt: Regenerate Models",
                "Failed to start 'dotnet'. See the JD.Efcpt output pane for details.").ConfigureAwait(true);
            return;
        }

        var errorCount = result.Diagnostics.Count(d => d.Severity == JdDiagnosticSeverity.Error);
        var warningCount = result.Diagnostics.Count(d => d.Severity == JdDiagnosticSeverity.Warning);
        var succeeded = result.ExitCode == 0;

        await pane.WriteLineAsync(
            succeeded
                ? $"Regeneration succeeded ({warningCount} warning(s), {errorCount} error(s))."
                : $"Regeneration failed with exit code {result.ExitCode} ({warningCount} warning(s), {errorCount} error(s)).")
            .ConfigureAwait(true);

        // Push the outcome directly to any open build-status tool window. This is what keeps a
        // FAILED regenerate (which typically does not rewrite build-profile.json, so the watcher
        // never fires) from leaving the panel showing the previous green run.
        RegenerateNotifier.NotifyCompleted(new RegenerateAttempt(startedUtc, succeeded, result.ExitCode));

        await VS.StatusBar
            .ShowMessageAsync(succeeded ? "JD.Efcpt: Models regenerated." : "JD.Efcpt: Regeneration failed. See Output window.")
            .ConfigureAwait(true);
    }

    private static async Task ReportUnexpectedFailureAsync(Exception ex)
    {
        try
        {
            var pane = await JdEfcptOutputPane.GetAsync().ConfigureAwait(true);
            await pane.ActivateAsync().ConfigureAwait(true);
            await pane.WriteLineAsync($"Regenerate Models failed unexpectedly: {ex.Message}").ConfigureAwait(true);
        }
        catch (Exception paneEx)
        {
            // Even surfacing the failure failed - fall back to the ActivityLog so nothing is lost.
            await paneEx.LogAsync().ConfigureAwait(true);
        }

        await VS.MessageBox.ShowErrorAsync(
            "JD.Efcpt: Regenerate Models",
            $"Regenerate Models failed unexpectedly: {ex.Message}").ConfigureAwait(true);
    }
}
