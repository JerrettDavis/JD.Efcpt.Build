using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace JD.Efcpt.VsExtension.ToolWindows;

/// <summary>
/// The "JD.Efcpt Build Status" tool window, opened via
/// <see cref="Commands.ShowBuildStatusCommand"/>. Displays model count, last-run timestamp,
/// status, duration, and warnings/errors read from <c>obj/efcpt/build-profile.json</c>.
/// </summary>
public sealed class BuildStatusToolWindow : BaseToolWindow<BuildStatusToolWindow>
{
    /// <inheritdoc/>
    public override string GetTitle(int toolWindowId) => "JD.Efcpt Build Status";

    /// <inheritdoc/>
    public override Type PaneType => typeof(Pane);

    /// <inheritdoc/>
    public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return new BuildStatusToolWindowControl();
    }

    /// <summary>
    /// The <see cref="ToolWindowPane"/> hosting <see cref="BuildStatusToolWindowControl"/>. Uses
    /// its own GUID, distinct from any tool window registered by other extensions (e.g. EF Core
    /// Power Tools), so it can never collide with theirs.
    /// </summary>
    [Guid("6843c6d5-8b83-41d5-ae3e-24bb91f5438b")]
    internal sealed class Pane : ToolkitToolWindowPane
    {
        /// <summary>Initializes the pane's caption and icon.</summary>
        public Pane()
        {
            BitmapImageMoniker = KnownMonikers.StatusInformation;
            Caption = "JD.Efcpt Build Status";
        }
    }
}
