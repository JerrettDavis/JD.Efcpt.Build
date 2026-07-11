using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.VsExtension.ToolWindows;
using Microsoft.VisualStudio.Shell;

namespace JD.Efcpt.VsExtension.Commands;

/// <summary>
/// "Tools &gt; Entity Framework &gt; JD.Efcpt: Show Build Status". Opens (or brings to front) the
/// JD.Efcpt build-status tool window.
/// </summary>
[Command(PackageGuids.JdEfcptCommandSetString, PackageIds.ShowBuildStatusCommandId)]
internal sealed class ShowBuildStatusCommand : BaseCommand<ShowBuildStatusCommand>
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await BuildStatusToolWindow.ShowAsync().ConfigureAwait(true);
    }
}
