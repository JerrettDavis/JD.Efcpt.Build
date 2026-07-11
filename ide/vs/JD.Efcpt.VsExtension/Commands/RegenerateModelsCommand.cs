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
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var projectPath = await ProjectResolver.ResolveTargetProjectAsync().ConfigureAwait(true);
        if (projectPath is null)
        {
            await VS.MessageBox.ShowWarningAsync(
                "JD.Efcpt: Regenerate Models",
                "No project in the solution references the JD.Efcpt.Build NuGet package.").ConfigureAwait(true);
            return;
        }

        var pane = await JdEfcptOutputPane.GetAsync().ConfigureAwait(true);
        await pane.ActivateAsync().ConfigureAwait(true);
        await pane.WriteLineAsync(
            $"Regenerating models for '{Path.GetFileNameWithoutExtension(projectPath)}'...").ConfigureAwait(true);

        var result = await RegenerateModelsService
            .RunAsync(projectPath, pane, Package.DisposalToken)
            .ConfigureAwait(true);

        if (result.StartFailed)
        {
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

        await VS.StatusBar
            .ShowMessageAsync(succeeded ? "JD.Efcpt: Models regenerated." : "JD.Efcpt: Regeneration failed. See Output window.")
            .ConfigureAwait(true);
    }
}
