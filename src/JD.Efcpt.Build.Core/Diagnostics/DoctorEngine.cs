using System.Text.Json;
using JD.Efcpt.Build.Tasks.Extensions;

namespace JD.Efcpt.Build.Core.Diagnostics;

/// <summary>
/// The inputs to a doctor diagnosis - a snapshot of the MSBuild properties
/// <c>JD.Efcpt.Build.Tasks.EfcptDoctor</c> and <c>JD.Efcpt.Build.Tasks.RunEfcpt</c> both resolve
/// tool execution from.
/// </summary>
public readonly record struct DoctorInputs(
    string TargetFramework,
    string ToolMode,
    string ToolPackageId,
    string ToolVersion,
    string ToolCommand,
    string ToolPath,
    string DotNetExe,
    string WorkingDirectory,
    bool Offline,
    bool AutoAcquire,
    bool Strict);

/// <summary>
/// Pure diagnosis engine extracted from <c>JD.Efcpt.Build.Tasks.EfcptDoctor</c> (#181) so it can
/// be unit tested (and reused by the jd-efcpt CLI's <c>doctor</c> command) without any MSBuild
/// task infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately reuses the exact same manifest-discovery and mode-resolution logic
/// (<see cref="FindManifestDir"/>, <see cref="ManifestListsTool"/>, <see cref="ToolModeUsesManifest"/>,
/// <see cref="IsDotNet10OrLater"/>) that <c>RunEfcpt</c> uses for its own tool resolution - moved
/// here from <c>RunEfcpt</c> in #181 (which now delegates to these via thin wrappers) so the
/// reported verdict can never drift out of sync with the actual resolution logic.
/// </para>
/// <para>
/// The branch ladder and verdict message strings in <see cref="DetermineVerdict"/> are copied
/// verbatim from the pre-#181 <c>EfcptDoctor.DetermineVerdict</c> - this is an observable
/// contract (verdict strings are asserted in tests and read by users), so it must not change
/// during the extraction.
/// </para>
/// </remarks>
public static class DoctorEngine
{
    /// <summary>
    /// Runs a full doctor diagnosis: builds the same diagnostic message list
    /// <c>EfcptDoctor.Execute()</c> logs, then determines the final verdict.
    /// </summary>
    /// <param name="inputs">The resolved MSBuild property snapshot to diagnose.</param>
    /// <param name="probe">The SDK/dnx/global-tool capability probe to use.</param>
    /// <returns>
    /// The verdict line, whether a viable execution path was found, and the full ordered list of
    /// diagnostic messages (including the verdict line itself, as the last entry).
    /// </returns>
    public static (string Verdict, bool HasViablePath, IReadOnlyList<string> Messages) Diagnose(
        DoctorInputs inputs, ISdkProbe probe)
    {
        var messages = new List<string>();
        var workingDir = string.IsNullOrWhiteSpace(inputs.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(inputs.WorkingDirectory);

        var offline = inputs.Offline;
        var autoAcquireRequested = inputs.AutoAcquire;
        var autoAcquireEffective = autoAcquireRequested && !offline;

        var majorVersion = DotNetToolUtilities.ParseTargetFrameworkVersion(inputs.TargetFramework);
        messages.Add(
            $"TargetFramework: '{(string.IsNullOrWhiteSpace(inputs.TargetFramework) ? "(not specified)" : inputs.TargetFramework)}' " +
            $"(parsed major version: {(majorVersion?.ToString() ?? "unknown")})");

        var sdk10Installed = probe.IsDotNet10SdkInstalled(inputs.DotNetExe);
        messages.Add($"SDK 10+ installed: {sdk10Installed}");

        var dnxAvailable = probe.IsDnxAvailable(inputs.DotNetExe);
        messages.Add($"dnx available: {dnxAvailable}");

        var dnxUsable = !offline && IsDotNet10OrLater(inputs.TargetFramework) && sdk10Installed && dnxAvailable;
        messages.Add($"dnx usable for this build: {dnxUsable}");

#if NETFRAMEWORK
        var forceManifestOnNonWindows = !JD.Efcpt.Build.Tasks.Compatibility.OperatingSystemPolyfill.IsWindows() && !PathUtils.HasExplicitPath(inputs.ToolPath);
#else
        var forceManifestOnNonWindows = !OperatingSystem.IsWindows() && !PathUtils.HasExplicitPath(inputs.ToolPath);
#endif

        var manifestDir = FindManifestDir(workingDir);
        messages.Add($"Tool manifest discovered: {manifestDir ?? "(none found)"}");

        var manifestUsesMode = ToolModeUsesManifest(inputs.ToolMode, manifestDir, forceManifestOnNonWindows);
        var manifestListsTool = manifestDir is not null && ManifestListsTool(manifestDir, inputs.ToolPackageId, inputs.ToolCommand);
        if (manifestDir is not null)
            messages.Add($"Manifest lists '{inputs.ToolPackageId}' / command '{inputs.ToolCommand}': {manifestListsTool}");

        var globalToolResolvable = probe.IsGlobalToolInstalled(inputs.ToolCommand);
        messages.Add($"Global tool '{inputs.ToolCommand}' resolvable on PATH: {globalToolResolvable}");

        var hasExplicitToolPath = PathUtils.HasExplicitPath(inputs.ToolPath);
        var explicitToolPathExists = hasExplicitToolPath && File.Exists(PathUtils.FullPath(inputs.ToolPath, workingDir));
        messages.Add(hasExplicitToolPath
            ? $"Explicit ToolPath: '{inputs.ToolPath}' (exists: {explicitToolPathExists})"
            : "Explicit ToolPath: (not set)");

        messages.Add($"EfcptOfflineMode: {offline}");
        messages.Add($"EfcptAutoAcquireTool: {autoAcquireRequested} (effective, offline-adjusted: {autoAcquireEffective})");

        var (verdict, hasViablePath) = DetermineVerdict(
            inputs, workingDir, manifestDir, manifestUsesMode, manifestListsTool,
            globalToolResolvable, hasExplicitToolPath, explicitToolPathExists,
            dnxUsable, autoAcquireEffective);

        // Additive advisory (does not affect the verdict or HasViablePath): when no viable path
        // was found AND the 'dotnet' muxer can't even be located on PATH, the SDK/dnx probes
        // above returned false because they could not run at all (a launch failure collapses to
        // ProbeOutcome.Transient -> false), not because the SDK is genuinely absent. Surface that
        // so "SDK 10+ installed: False" isn't misread as a definitive answer. This is a pure
        // PATH/file lookup (SdkProbeCache.ResolveDotnetExecutable) - it spawns no process - and is
        // inserted before the verdict so the verdict remains the last message.
        if (!hasViablePath && SdkProbeCache.ResolveDotnetExecutable(inputs.DotNetExe) is null)
        {
            messages.Add(
                $"(note: the SDK/dnx probes were inconclusive - '{(string.IsNullOrWhiteSpace(inputs.DotNetExe) ? "dotnet" : inputs.DotNetExe)}' " +
                "could not be located on PATH, so it may not be launchable; verify the .NET SDK is installed and 'dotnet' is on PATH)");
        }

        messages.Add($"Verdict: {verdict}");

        return (verdict, hasViablePath, messages);
    }

    private static (string Verdict, bool HasViablePath) DetermineVerdict(
        DoctorInputs inputs,
        string workingDir,
        string? manifestDir,
        bool manifestUsesMode,
        bool manifestListsTool,
        bool globalToolResolvable,
        bool hasExplicitToolPath,
        bool explicitToolPathExists,
        bool dnxUsable,
        bool autoAcquireEffective)
    {
        if (hasExplicitToolPath && explicitToolPathExists)
            return ($"Explicit ToolPath will be used directly: '{inputs.ToolPath}'.", true);

        if (hasExplicitToolPath)
            return ($"ToolPath is set but the file does not exist: '{inputs.ToolPath}'. Fix ToolPath, " +
                     "or clear it to fall back to automatic resolution.", false);

        if (dnxUsable)
            return ($"dnx execution will be used: dotnet dnx {inputs.ToolPackageId} --yes -- ...", true);

        if (manifestUsesMode && manifestListsTool)
            return ($"Tool-manifest resolution will be used: dotnet tool run {inputs.ToolCommand} (manifest: '{manifestDir}').", true);

        if (manifestUsesMode && manifestDir is not null)
        {
            if (inputs.Offline)
                return ($"A tool manifest was found at '{manifestDir}' but does not list '{inputs.ToolPackageId}', and " +
                        "EfcptOfflineMode prevents restoring it. Pre-provision the tool or disable offline mode.", false);

            // Consistent with RunEfcpt.AcquireToolIfNeeded (#186 adversarial-review fix): a
            // manifest that doesn't list the tool is NOT auto-restorable via 'dotnet tool
            // restore' (restore only reinstalls tools already listed in the manifest) - it
            // requires EfcptAutoAcquireTool to install the missing entry, or it's a dead end.
            if (autoAcquireEffective)
                return ($"A tool manifest was found at '{manifestDir}' but does not list '{inputs.ToolPackageId}'; " +
                        $"EfcptAutoAcquireTool will install '{inputs.ToolPackageId}' into the existing manifest at " +
                        "build time.", true);

            return ($"A tool manifest was found at '{manifestDir}' but does not list '{inputs.ToolPackageId}', and " +
                    $"EfcptAutoAcquireTool is disabled. Run 'dotnet tool install {inputs.ToolPackageId}' in " +
                    $"'{manifestDir}', set EfcptAutoAcquireTool=true, or set an explicit ToolPath.", false);
        }

        if (manifestUsesMode)
        {
            if (autoAcquireEffective)
                return ($"No tool manifest found; EfcptAutoAcquireTool will bootstrap one at " +
                         $"'{workingDir}' and install '{inputs.ToolPackageId}' at build time.", true);

            return inputs.Offline
                ? ("No tool manifest found and EfcptOfflineMode prevents acquisition/restore. Pre-provision " +
                   "a manifest or global tool, set an explicit ToolPath, or disable offline mode.", false)
                : ($"No tool manifest found and EfcptAutoAcquireTool is disabled. Run 'dotnet new " +
                   $"tool-manifest && dotnet tool install {inputs.ToolPackageId}' in '{workingDir}', set " +
                   "EfcptAutoAcquireTool=true, or set an explicit ToolPath.", false);
        }

        if (globalToolResolvable)
            return ($"Global tool resolution will be used: '{inputs.ToolCommand}' is already resolvable on PATH.", true);

        if (autoAcquireEffective)
            return ($"No global tool found; EfcptAutoAcquireTool will bootstrap a local manifest at " +
                     $"'{workingDir}' and install '{inputs.ToolPackageId}' at build time.", true);

        var fix = inputs.Offline
            ? $"EfcptOfflineMode prevents acquisition. Pre-provision the tool (dotnet tool install --global " +
              $"{inputs.ToolPackageId}, or a committed tool manifest), or disable offline mode."
            : $"Install the tool globally (dotnet tool install --global {inputs.ToolPackageId}), commit a tool " +
              "manifest, set EfcptAutoAcquireTool=true, or set an explicit ToolPath.";

        return ($"No viable execution path found for TargetFramework='{inputs.TargetFramework}'. {fix}", false);
    }

    /// <summary>
    /// Checks if the target framework is .NET 10.0 or later.
    /// </summary>
    /// <param name="targetFramework">The target framework string (e.g., "net8.0", "net10.0").</param>
    /// <returns>True if the target framework is .NET 10.0 or later; otherwise false.</returns>
    /// <remarks>
    /// Moved verbatim from <c>JD.Efcpt.Build.Tasks.RunEfcpt.IsDotNet10OrLater</c> in #181, which
    /// now delegates here so its own <c>ToolResolutionStrategy</c>/<c>AcquireToolIfNeeded</c> and
    /// this engine's verdict can never drift on odd TFM shapes (originally called out in the #186
    /// adversarial review).
    /// </remarks>
    public static bool IsDotNet10OrLater(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            return false;

        try
        {
            // Parse target framework to get major version (e.g., "net8.0" -> 8, "net10.0" -> 10)
            if (!targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                return false;

            var versionPart = targetFramework[3..];

            // Trim at the first '.' or '-' after "net" to handle formats like:
            // - "net10.0"           -> "10"
            // - "net10.0-windows"   -> "10"
            // - "net10-windows"     -> "10"
            var dotIndex = versionPart.IndexOf('.');
            var hyphenIndex = versionPart.IndexOf('-');

            var cutIndex = (dotIndex >= 0, hyphenIndex >= 0) switch
            {
                (true, true) => Math.Min(dotIndex, hyphenIndex),
                (true, false) => dotIndex,
                (false, true) => hyphenIndex,
                _ => -1
            };

            if (cutIndex > 0)
                versionPart = versionPart[..cutIndex];

            if (int.TryParse(versionPart, out var version))
                return version >= 10;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Walks up from <paramref name="start"/> looking for a <c>.config/dotnet-tools.json</c>
    /// tool manifest.
    /// </summary>
    /// <param name="start">The directory to start searching from.</param>
    /// <returns>
    /// The directory containing the manifest, or <see langword="null"/> if none was found
    /// walking up to the filesystem root.
    /// </returns>
    /// <remarks>Moved verbatim from <c>JD.Efcpt.Build.Tasks.RunEfcpt.FindManifestDir</c> in #181.</remarks>
    public static string? FindManifestDir(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var manifest = Path.Combine(dir.FullName, ".config", "dotnet-tools.json");
            if (File.Exists(manifest)) return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Determines whether <paramref name="toolMode"/> would actually resolve to using a local
    /// tool manifest - i.e. <c>tool-manifest</c> mode, or <c>auto</c> mode with a discovered
    /// manifest directory or a forced manifest fallback (non-Windows, no explicit ToolPath).
    /// </summary>
    /// <remarks>
    /// Moved verbatim from <c>JD.Efcpt.Build.Tasks.RunEfcpt.ToolModeUsesManifest</c> in #181,
    /// which now delegates here so it stays in lockstep with its own tool-resolution strategy.
    /// </remarks>
    public static bool ToolModeUsesManifest(string toolMode, string? manifestDir, bool forceManifestOnNonWindows) =>
        toolMode.EqualsIgnoreCase("tool-manifest") ||
        (toolMode.EqualsIgnoreCase("auto") &&
        (manifestDir is not null || forceManifestOnNonWindows));

    /// <summary>
    /// Reads a discovered <c>.config/dotnet-tools.json</c> manifest and determines whether it
    /// lists an entry for the target efcpt tool - matched either by package id or by exposing a
    /// command name matching <paramref name="toolCommand"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a local file read only (no network access), so it is safe to call from an offline
    /// pre-flight path. Any parse failure - missing file, malformed JSON, or an unexpected shape
    /// - is tolerated by returning <see langword="false"/> (i.e. "does not prove runnability")
    /// rather than throwing, since a corrupt manifest is exactly the kind of situation a
    /// strengthened pre-flight check is meant to catch.
    /// </para>
    /// <para>
    /// Moved verbatim from <c>JD.Efcpt.Build.Tasks.RunEfcpt.ManifestListsTool</c> in #181.
    /// </para>
    /// </remarks>
    public static bool ManifestListsTool(string manifestDir, string toolPackageId, string toolCommand)
    {
        try
        {
            var manifestPath = Path.Combine(manifestDir, ".config", "dotnet-tools.json");
            if (!File.Exists(manifestPath)) return false;

            using var stream = File.OpenRead(manifestPath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var tool in tools.EnumerateObject())
            {
                if (tool.Name.EqualsIgnoreCase(toolPackageId))
                    return true;

                if (tool.Value.ValueKind == JsonValueKind.Object &&
                    tool.Value.TryGetProperty("commands", out var commands) &&
                    commands.ValueKind == JsonValueKind.Array)
                {
                    foreach (var command in commands.EnumerateArray())
                    {
                        if (command.ValueKind == JsonValueKind.String &&
                            command.GetString().EqualsIgnoreCase(toolCommand))
                            return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            // Malformed/unreadable manifest: don't treat it as proof of runnability.
            return false;
        }
    }
}
