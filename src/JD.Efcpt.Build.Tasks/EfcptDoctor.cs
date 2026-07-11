using JD.Efcpt.Build.Tasks.Extensions;
using JD.Efcpt.Build.Tasks.Utilities;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

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
/// Deliberately reuses <see cref="RunEfcpt"/>'s own manifest-discovery and mode-resolution
/// helpers (<see cref="RunEfcpt.FindManifestDir"/>, <see cref="RunEfcpt.ManifestListsTool"/>,
/// <see cref="RunEfcpt.ToolModeUsesManifest"/>) and the <see cref="ISdkProbe"/> seam introduced
/// in #185, so the reported verdict can never drift out of sync with the actual resolution logic
/// in <see cref="RunEfcpt"/>.
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
        var messages = new List<string>();
        var workingDir = string.IsNullOrWhiteSpace(WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(WorkingDirectory);

        var offline = OfflineMode.IsTrue() || Environment.GetEnvironmentVariable("EFCPT_OFFLINE").IsTrue();
        var autoAcquireRequested = AutoAcquireTool.IsTrue();
        var autoAcquireEffective = autoAcquireRequested && !offline;

        var majorVersion = DotNetToolUtilities.ParseTargetFrameworkVersion(TargetFramework);
        messages.Add(
            $"TargetFramework: '{(string.IsNullOrWhiteSpace(TargetFramework) ? "(not specified)" : TargetFramework)}' " +
            $"(parsed major version: {(majorVersion?.ToString() ?? "unknown")})");

        var sdk10Installed = Probe.IsDotNet10SdkInstalled(DotNetExe);
        messages.Add($"SDK 10+ installed: {sdk10Installed}");

        var dnxAvailable = Probe.IsDnxAvailable(DotNetExe);
        messages.Add($"dnx available: {dnxAvailable}");

        var dnxUsable = !offline && DotNetToolUtilities.IsDotNet10OrLater(TargetFramework) && sdk10Installed && dnxAvailable;
        messages.Add($"dnx usable for this build: {dnxUsable}");

#if NETFRAMEWORK
        var forceManifestOnNonWindows = !OperatingSystemPolyfill.IsWindows() && !PathUtils.HasExplicitPath(ToolPath);
#else
        var forceManifestOnNonWindows = !OperatingSystem.IsWindows() && !PathUtils.HasExplicitPath(ToolPath);
#endif

        var manifestDir = RunEfcpt.FindManifestDir(workingDir);
        messages.Add($"Tool manifest discovered: {manifestDir ?? "(none found)"}");

        var manifestUsesMode = RunEfcpt.ToolModeUsesManifest(ToolMode, manifestDir, forceManifestOnNonWindows);
        var manifestListsTool = manifestDir is not null && RunEfcpt.ManifestListsTool(manifestDir, ToolPackageId, ToolCommand);
        if (manifestDir is not null)
            messages.Add($"Manifest lists '{ToolPackageId}' / command '{ToolCommand}': {manifestListsTool}");

        var globalToolResolvable = Probe.IsGlobalToolInstalled(ToolCommand);
        messages.Add($"Global tool '{ToolCommand}' resolvable on PATH: {globalToolResolvable}");

        var hasExplicitToolPath = PathUtils.HasExplicitPath(ToolPath);
        var explicitToolPathExists = hasExplicitToolPath && File.Exists(PathUtils.FullPath(ToolPath, workingDir));
        messages.Add(hasExplicitToolPath
            ? $"Explicit ToolPath: '{ToolPath}' (exists: {explicitToolPathExists})"
            : "Explicit ToolPath: (not set)");

        messages.Add($"EfcptOfflineMode: {offline}");
        messages.Add($"EfcptAutoAcquireTool: {autoAcquireRequested} (effective, offline-adjusted: {autoAcquireEffective})");

        var (verdict, hasViablePath) = DetermineVerdict(
            workingDir, manifestDir, manifestUsesMode, manifestListsTool,
            globalToolResolvable, hasExplicitToolPath, explicitToolPathExists,
            dnxUsable, offline, autoAcquireEffective);

        messages.Add($"Verdict: {verdict}");

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

    private (string Verdict, bool HasViablePath) DetermineVerdict(
        string workingDir,
        string? manifestDir,
        bool manifestUsesMode,
        bool manifestListsTool,
        bool globalToolResolvable,
        bool hasExplicitToolPath,
        bool explicitToolPathExists,
        bool dnxUsable,
        bool offline,
        bool autoAcquireEffective)
    {
        if (hasExplicitToolPath && explicitToolPathExists)
            return ($"Explicit ToolPath will be used directly: '{ToolPath}'.", true);

        if (hasExplicitToolPath)
            return ($"ToolPath is set but the file does not exist: '{ToolPath}'. Fix ToolPath, " +
                     "or clear it to fall back to automatic resolution.", false);

        if (dnxUsable)
            return ($"dnx execution will be used: dotnet dnx {ToolPackageId} --yes -- ...", true);

        if (manifestUsesMode && manifestListsTool)
            return ($"Tool-manifest resolution will be used: dotnet tool run {ToolCommand} (manifest: '{manifestDir}').", true);

        if (manifestUsesMode && manifestDir is not null)
        {
            return offline
                ? ($"A tool manifest was found at '{manifestDir}' but does not list '{ToolPackageId}', and " +
                    "EfcptOfflineMode prevents restoring it. Pre-provision the tool or disable offline mode.", false)
                : ($"A tool manifest was found at '{manifestDir}' but does not list '{ToolPackageId}'; " +
                    "'dotnet tool restore' will be attempted at build time.", true);
        }

        if (manifestUsesMode)
        {
            if (autoAcquireEffective)
                return ($"No tool manifest found; EfcptAutoAcquireTool will bootstrap one at " +
                         $"'{workingDir}' and install '{ToolPackageId}' at build time.", true);

            return offline
                ? ("No tool manifest found and EfcptOfflineMode prevents acquisition/restore. Pre-provision " +
                   "a manifest or global tool, set an explicit ToolPath, or disable offline mode.", false)
                : ($"No tool manifest found and EfcptAutoAcquireTool is disabled. Run 'dotnet new " +
                   $"tool-manifest && dotnet tool install {ToolPackageId}' in '{workingDir}', set " +
                   "EfcptAutoAcquireTool=true, or set an explicit ToolPath.", false);
        }

        if (globalToolResolvable)
            return ($"Global tool resolution will be used: '{ToolCommand}' is already resolvable on PATH.", true);

        if (autoAcquireEffective)
            return ($"No global tool found; EfcptAutoAcquireTool will bootstrap a local manifest at " +
                     $"'{workingDir}' and install '{ToolPackageId}' at build time.", true);

        var fix = offline
            ? $"EfcptOfflineMode prevents acquisition. Pre-provision the tool (dotnet tool install --global " +
              $"{ToolPackageId}, or a committed tool manifest), or disable offline mode."
            : $"Install the tool globally (dotnet tool install --global {ToolPackageId}), commit a tool " +
              "manifest, set EfcptAutoAcquireTool=true, or set an explicit ToolPath.";

        return ($"No viable execution path found for TargetFramework='{TargetFramework}'. {fix}", false);
    }
}
