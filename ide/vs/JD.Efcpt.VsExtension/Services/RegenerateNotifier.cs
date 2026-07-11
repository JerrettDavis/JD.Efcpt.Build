using System;
using JD.Efcpt.Ide.Core;

namespace JD.Efcpt.VsExtension.Services;

/// <summary>
/// A lightweight process-wide notifier that lets <see cref="Commands.RegenerateModelsCommand"/>
/// push the outcome of a regenerate directly to any open build-status tool window, independent of
/// the <see cref="System.IO.FileSystemWatcher"/> on <c>build-profile.json</c>.
/// </summary>
/// <remarks>
/// This closes a correctness gap: a FAILED regenerate typically does not rewrite
/// <c>build-profile.json</c>, so the watcher never fires and the tool window would otherwise keep
/// showing the previous (green) run. By raising <see cref="RegenerateCompleted"/> the moment the
/// build finishes - success or failure - the tool window can immediately reflect what actually
/// happened. The correlation of that outcome against the on-disk profile is done by the
/// unit-tested <see cref="BuildStatusEvaluator"/> in <c>JD.Efcpt.Ide.Core</c>; this class is only
/// the (windows-only, correctness-by-construction) event plumbing.
/// </remarks>
internal static class RegenerateNotifier
{
    /// <summary>
    /// Raised when a regenerate the extension initiated completes (whether it succeeded or failed).
    /// Handlers are invoked on whatever thread raised the event; subscribers that touch UI must
    /// marshal to the UI thread themselves.
    /// </summary>
    public static event EventHandler<RegenerateAttempt>? RegenerateCompleted;

    /// <summary>Raises <see cref="RegenerateCompleted"/> with the given attempt outcome.</summary>
    /// <param name="attempt">The completed regenerate attempt (start time, success, exit code).</param>
    public static void NotifyCompleted(RegenerateAttempt attempt)
    {
        RegenerateCompleted?.Invoke(null, attempt);
    }
}
