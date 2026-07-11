using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;

namespace JD.Efcpt.VsExtension.Services;

/// <summary>
/// Provides the single, shared "JD.Efcpt" Output Window pane used by
/// <see cref="Commands.RegenerateModelsCommand"/>. Caches the <see cref="OutputWindowPane"/>
/// instance (rather than requesting a pane by a caller-supplied GUID, which the currently
/// referenced Community.VisualStudio.Toolkit.17 version does not expose) so repeated command
/// invocations write to the same pane instead of creating a new one each time.
/// </summary>
internal static class JdEfcptOutputPane
{
    private static OutputWindowPane? s_pane;

    /// <summary>Gets the shared "JD.Efcpt" Output Window pane, creating it on first use.</summary>
    public static async Task<OutputWindowPane> GetAsync()
    {
        return s_pane ??= await VS.Windows.CreateOutputWindowPaneAsync("JD.Efcpt").ConfigureAwait(true);
    }
}
