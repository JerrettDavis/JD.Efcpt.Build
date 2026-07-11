using JD.Efcpt.Build.Core.Diagnostics;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild diagnostic task that reports how <see cref="RunEfcpt"/> would resolve the efcpt tool
/// for the current project, without actually running (or installing) anything.
/// </summary>
/// <remarks>
/// <para>
/// Invoke directly via <c>dotnet build -t:EfcptDoctor</c> (the <c>EfcptDoctor</c> target has no
/// <c>BeforeTargets</c>, so it never runs as part of a normal build). Reports: the target
/// framework and its parsed major version; whether the .NET 10+ SDK and <c>dnx</c> are
/// available; whether a tool manifest was discovered (and whether it lists the tool); whether a
/// global tool is resolvable on <c>PATH</c>; the explicit <see cref="ToolPath"/> state; the
/// effective <see cref="OfflineMode"/>/<see cref="AutoAcquireTool"/> settings; and a verdict -
/// either which execution path <see cref="RunEfcpt"/> would take, or the exact remediation if
/// none is viable.
/// </para>
/// <para>
/// This task never fails the build unless <see cref="Strict"/> is <c>true</c> (wired to
/// <c>-p:EfcptDoctorStrict=true</c>) and no viable execution path was found.
/// </para>
/// <para>
/// The actual diagnosis (message-building and verdict branch ladder) lives in
/// <see cref="DoctorEngine.Diagnose"/> (extracted in #181 alongside the
/// <c>JD.Efcpt.Build.Core</c> library extraction) so it can be unit tested and reused by the
/// jd-efcpt CLI's <c>doctor</c> command without any MSBuild task infrastructure. This task's
/// <see cref="Execute"/> only builds a <see cref="DoctorInputs"/> snapshot from its MSBuild
/// properties, calls the engine, and copies the result to its output properties/log - the
/// verdict strings and branch logic are unchanged from before the extraction.
/// </para>
/// </remarks>
public sealed class EfcptDoctor : Task
{
    /// <summary>Target framework of the project being diagnosed (e.g. "net8.0", "net10.0").</summary>
    public string TargetFramework { get; set; } = "";

    /// <summary>Mirrors <see cref="RunEfcpt.ToolMode"/>.</summary>
    public string ToolMode { get; set; } = "auto";

    /// <summary>Mirrors <see cref="RunEfcpt.ToolPackageId"/>.</summary>
    public string ToolPackageId { get; set; } = "ErikEJ.EFCorePowerTools.Cli";

    /// <summary>Mirrors <see cref="RunEfcpt.ToolVersion"/>.</summary>
    public string ToolVersion { get; set; } = "";

    /// <summary>Mirrors <see cref="RunEfcpt.ToolCommand"/>.</summary>
    public string ToolCommand { get; set; } = "efcpt";

    /// <summary>Mirrors <see cref="RunEfcpt.ToolPath"/>.</summary>
    public string ToolPath { get; set; } = "";

    /// <summary>Mirrors <see cref="RunEfcpt.DotNetExe"/>.</summary>
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>
    /// Working directory used for manifest discovery - mirrors <see cref="RunEfcpt.WorkingDirectory"/>.
    /// </summary>
    [Required]
    public string WorkingDirectory { get; set; } = "";

    /// <summary>Mirrors <see cref="RunEfcpt.OfflineMode"/>.</summary>
    public string OfflineMode { get; set; } = "false";

    /// <summary>Mirrors <see cref="RunEfcpt.AutoAcquireTool"/>.</summary>
    public string AutoAcquireTool { get; set; } = "true";

    /// <summary>
    /// When truthy (see <see cref="StringExtensions.IsTrue"/>), the task fails the build
    /// (returns <see langword="false"/> and logs an error) if no viable execution path was
    /// found. Wired to <c>-p:EfcptDoctorStrict=true</c>. Defaults to <c>"false"</c>: by default
    /// this task only reports, never fails.
    /// </summary>
    public string Strict { get; set; } = "false";

    /// <summary>
    /// The individual diagnostic lines gathered during the run, in report order (output).
    /// </summary>
    [Output]
    public string[] Messages { get; set; } = [];

    /// <summary>
    /// The final verdict line: either the execution path that will be used, or the exact
    /// remediation needed if none is viable (output).
    /// </summary>
    [Output]
    public string Verdict { get; set; } = "";

    /// <summary>
    /// Whether at least one viable, network-free-or-acquirable execution path was found (output).
    /// </summary>
    [Output]
    public bool HasViablePath { get; set; }

    /// <summary>
    /// Testability seam for the SDK/dnx/global-tool capability probes - same interface used by
    /// <see cref="RunEfcpt"/>. Defaults to <see cref="DefaultSdkProbe"/>; tests may substitute a
    /// fake implementation.
    /// </summary>
    internal ISdkProbe Probe { get; set; } = new DefaultSdkProbe();

    /// <inheritdoc />
    public override bool Execute()
    {
        var offline = OfflineMode.IsTrue() || Environment.GetEnvironmentVariable("EFCPT_OFFLINE").IsTrue();

        var inputs = new DoctorInputs(
            TargetFramework: TargetFramework,
            ToolMode: ToolMode,
            ToolPackageId: ToolPackageId,
            ToolVersion: ToolVersion,
            ToolCommand: ToolCommand,
            ToolPath: ToolPath,
            DotNetExe: DotNetExe,
            WorkingDirectory: WorkingDirectory,
            Offline: offline,
            AutoAcquire: AutoAcquireTool.IsTrue(),
            Strict: Strict.IsTrue());

        var (verdict, hasViablePath, messages) = DoctorEngine.Diagnose(inputs, Probe);

        foreach (var message in messages)
            Log.LogMessage(MessageImportance.High, $"[EfcptDoctor] {message}");

        Messages = [.. messages];
        Verdict = verdict;
        HasViablePath = hasViablePath;

        if (!hasViablePath && Strict.IsTrue())
        {
            // No dedicated error code: this is a diagnostic-only task summarizing conditions
            // RunEfcpt itself will separately report (with codes, e.g. JD0026/JD0027) if the
            // build actually reaches that point. EfcptDoctorStrict is an opt-in CI gate, not a
            // new error surface.
            Log.LogError(verdict);
            return false;
        }

        return true;
    }
}
