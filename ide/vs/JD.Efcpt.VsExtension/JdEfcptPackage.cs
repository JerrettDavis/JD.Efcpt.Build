using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.VsExtension.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.VsExtension;

/// <summary>
/// The JD.Efcpt.Build Visual Studio extension package. Background-loaded when a solution exists
/// (rather than eagerly on VS startup), and never touches the MSBuild task pipeline directly -
/// see <see cref="Commands.RegenerateModelsCommand"/>, which shells out to <c>dotnet build</c>.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuids.JdEfcptPackageString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
[ProvideToolWindow(typeof(BuildStatusToolWindow.Pane))]
public sealed class JdEfcptPackage : ToolkitPackage
{
    /// <inheritdoc/>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        // Tool windows must be registered before any command tries to show one.
        this.RegisterToolWindows();

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Scans this assembly for [Command]-attributed BaseCommand<T> types (RegenerateModelsCommand,
        // ShowBuildStatusCommand) and wires each one up to the OleMenuCommandService.
        await this.RegisterCommandsAsync();
    }
}
