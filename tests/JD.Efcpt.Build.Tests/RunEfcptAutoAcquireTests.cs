using System.Runtime.InteropServices;
using JD.Efcpt.Build.Core.Logging;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.Utilities;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for <c>EfcptAutoAcquireTool</c> (#186): verifies that on .NET 8/9 (where dnx is not
/// usable), with no explicit ToolPath and no already-usable tool manifest or global tool, the
/// task bootstraps an obj-local tool manifest and installs the efcpt tool into it before tool
/// resolution runs - and that this is correctly gated by TargetFramework, EfcptOfflineMode
/// precedence, and the EfcptAutoAcquireTool opt-out.
/// </summary>
[Feature("EfcptAutoAcquireTool: bootstrap an obj-local tool manifest on .NET 8/9 when nothing else is usable")]
[Collection(nameof(AssemblySetup))]
public sealed class RunEfcptAutoAcquireTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    /// <summary>
    /// An <see cref="ISdkProbe"/> that deterministically reports every capability as
    /// unavailable, without spawning any process.
    /// </summary>
    private sealed class AllUnavailableSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => false;
        public bool IsDnxAvailable(string dotnetExe) => false;
        public bool IsGlobalToolInstalled(string toolCommand) => false;
    }

    /// <summary>
    /// An <see cref="ISdkProbe"/> that reports dnx as fully usable (.NET 10+ SDK installed and
    /// dnx available) and throws if the global-tool probe is invoked - used to assert that the
    /// dnx branch never falls through to the global-tool/acquisition checks.
    /// </summary>
    private sealed class DnxUsableSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => true;
        public bool IsDnxAvailable(string dotnetExe) => true;

        public bool IsGlobalToolInstalled(string toolCommand) =>
            throw new InvalidOperationException("IsGlobalToolInstalled should not be called when dnx is usable.");
    }

    /// <summary>
    /// An <see cref="ISdkProbe"/> that reports dnx as fully unusable but a global tool as
    /// resolvable on PATH - used to exercise the "!wouldUseManifest &amp;&amp;
    /// Probe.IsGlobalToolInstalled" gating (#186 adversarial-review FIX 1 HIGH regression guard).
    /// </summary>
    private sealed class GlobalToolOnlySdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => false;
        public bool IsDnxAvailable(string dotnetExe) => false;
        public bool IsGlobalToolInstalled(string toolCommand) => true;
    }

    /// <summary>
    /// An <see cref="IToolAcquirer"/> that throws if invoked - used to assert that a given
    /// scenario genuinely never attempts acquisition.
    /// </summary>
    private sealed class ThrowingToolAcquirer : IToolAcquirer
    {
        public ToolAcquisitionOutcome Acquire(ToolAcquisitionRequest request, IBuildLog log) =>
            throw new InvalidOperationException("Acquire should not be called in this scenario.");
    }

    /// <summary>
    /// A fake <see cref="IToolAcquirer"/> that records every acquisition request it receives
    /// and, on success, writes a minimal real manifest listing the requested tool into
    /// <see cref="ToolAcquisitionRequest.ManifestDir"/> - simulating exactly what a real
    /// <c>dotnet new tool-manifest &amp;&amp; dotnet tool install</c> would produce, without
    /// spawning any process or touching the network - so that <c>FindManifestDir</c> discovers
    /// it afterward and resolution can proceed via <c>dotnet tool run</c>.
    /// </summary>
    private sealed class RecordingToolAcquirer(bool succeed = true, string? errorMessage = null) : IToolAcquirer
    {
        public List<ToolAcquisitionRequest> Requests { get; } = [];

        public ToolAcquisitionOutcome Acquire(ToolAcquisitionRequest request, IBuildLog log)
        {
            Requests.Add(request);

            if (!succeed)
                return ToolAcquisitionOutcome.Failed(errorMessage ?? "simulated acquisition failure");

            var configDir = Path.Combine(request.ManifestDir, ".config");
            Directory.CreateDirectory(configDir);
            File.WriteAllText(Path.Combine(configDir, "dotnet-tools.json"), $$"""
                {
                  "version": 1,
                  "isRoot": true,
                  "tools": {
                    "{{request.ToolPackageId.ToLowerInvariant()}}": {
                      "version": "10.0.0",
                      "commands": [ "efcpt" ]
                    }
                  }
                }
                """);

            return ToolAcquisitionOutcome.Ok();
        }
    }

    /// <summary>
    /// Writes a trivial fake executable into a scratch "tools" directory that appends
    /// <paramref name="label"/> and its invocation arguments to <paramref name="captureFile"/>
    /// and exits 0 - standing in for a real dotnet/global-tool invocation without spawning any
    /// real process or touching the network.
    /// </summary>
    /// <remarks>
    /// Cross-platform: on Windows this writes a <c>.cmd</c> script (invoked via
    /// <c>cmd.exe /c</c> by <c>CommandNormalizationStrategy</c>). <c>.cmd</c>/<c>.bat</c> files
    /// cannot be executed directly on Linux/macOS, so on non-Windows this instead writes a POSIX
    /// shell script (no extension, shebang line) and marks it executable via
    /// <see cref="File.SetUnixFileMode(string, UnixFileMode)"/> - <c>CommandNormalizationStrategy</c>
    /// runs such a file directly with no wrapper on non-Windows.
    /// </remarks>
    private static string WriteCaptureScript(TestFolder folder, string scriptName, string label, string captureFile)
    {
        var toolsDir = folder.CreateDir("tools");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var path = Path.Combine(toolsDir, scriptName);
            File.WriteAllText(path, $"@echo off\r\necho {label} %* >> \"{captureFile}\"\r\nexit /b 0\r\n");
            return path;
        }

        var baseName = Path.GetFileNameWithoutExtension(scriptName);
        var shPath = Path.Combine(toolsDir, baseName);
        File.WriteAllText(shPath, $"#!/bin/sh\necho {label} \"$@\" >> \"{captureFile}\"\nexit 0\n");
        TestFileSystem.MakeExecutable(shPath);
        return shPath;
    }

    /// <summary>
    /// Writes a real <c>.config/dotnet-tools.json</c> manifest into <paramref name="manifestDir"/>
    /// listing the given tool (or none, if <paramref name="listsTool"/> is <see langword="false"/>
    /// - a placeholder unrelated tool is listed instead, matching a manifest that exists but has
    /// never had the efcpt tool installed into it).
    /// </summary>
    private static void WriteManifest(string manifestDir, bool listsTool, string toolPackageId = "ErikEJ.EFCorePowerTools.Cli", string toolCommand = "efcpt")
    {
        var configDir = Path.Combine(manifestDir, ".config");
        Directory.CreateDirectory(configDir);

        var toolsEntry = listsTool
            ? $$"""
                "{{toolPackageId.ToLowerInvariant()}}": {
                      "version": "10.0.0",
                      "commands": [ "{{toolCommand}}" ]
                    }
                """
            : """
              "some.other.tool": {
                    "version": "1.0.0",
                    "commands": [ "othertool" ]
                  }
              """;

        File.WriteAllText(Path.Combine(configDir, "dotnet-tools.json"), $$"""
            {
              "version": 1,
              "isRoot": true,
              "tools": {
                {{toolsEntry}}
              }
            }
            """);
    }

    private sealed record SetupState(
        TestFolder Folder,
        string WorkingDir,
        string DacpacPath,
        string ConfigPath,
        string RenamingPath,
        string TemplateDir,
        string OutputDir,
        TestBuildEngine Engine);

    private sealed record TaskResult(SetupState Setup, RunEfcpt Task, bool Success);

    private sealed record CaptureResult(SetupState Setup, RunEfcpt Task, bool Success, string CaptureFile);

    private static SetupState SetupForDacpacMode()
    {
        var folder = new TestFolder();
        var workingDir = folder.CreateDir("obj");
        var dacpac = folder.WriteFile("db.dacpac", "DACPAC content");
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
        var templateDir = folder.CreateDir("Templates");
        var outputDir = Path.Combine(folder.Root, "Generated");

        var engine = new TestBuildEngine();
        return new SetupState(folder, workingDir, dacpac, config, renaming, templateDir, outputDir, engine);
    }

    // (a) .NET 8/9 + dnx-unavailable + AutoAcquire=true, no manifest -> bootstrap recorded and
    // resolution chooses `dotnet tool run`.
    [Scenario("On .NET 8/9 with dnx unusable and no manifest, AutoAcquireTool=true bootstraps a manifest and resolves via 'dotnet tool run'")]
    [Fact]
    public async Task AutoAcquire_bootstraps_manifest_and_resolves_via_tool_run()
    {
        await Given("inputs for DACPAC mode with a capturing fake dotnet and a recording tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                var acquirer = new RecordingToolAcquirer();
                return (setup, captureFile, fakeDotNet, acquirer);
            })
            .When("task executes with AutoAcquireTool=true, TFM net8.0, no ToolPath, no manifest", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolVersion = "10.*",
                    ToolCommand = "efcpt",
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = ctx.acquirer
                };

                var success = task.Execute();
                return (Result: new CaptureResult(ctx.setup, task, success, ctx.captureFile), ctx.acquirer);
            })
            .Then("task succeeds", r => r.Result.Success)
            .And("no error is logged", r => r.Result.Setup.Engine.Errors.Count == 0)
            .And("exactly one acquisition request was recorded", r => r.acquirer.Requests.Count == 1)
            .And("the request targets the obj-local working directory with the configured package/version", r =>
            {
                var req = r.acquirer.Requests[0];
                return req.ManifestDir.Equals(Path.GetFullPath(r.Result.Setup.WorkingDir), StringComparison.OrdinalIgnoreCase) &&
                       req.ToolPackageId == "ErikEJ.EFCorePowerTools.Cli" &&
                       req.ToolVersion == "10.*";
            })
            .And("resolution chose 'dotnet tool run efcpt'", r =>
            {
                var lines = File.ReadAllLines(r.Result.CaptureFile);
                return lines.Length > 0 &&
                       lines[^1].Contains("tool run", StringComparison.OrdinalIgnoreCase) &&
                       lines[^1].Contains("efcpt", StringComparison.OrdinalIgnoreCase);
            })
            .Finally(r => r.Result.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (b) .NET 8/9 + AutoAcquire=true but Offline=true -> NO acquisition attempted; JD0026 path
    // when nothing is pre-provisioned (offline precedence).
    [Scenario("Offline mode disables auto-acquisition even when AutoAcquireTool=true, and JD0026 fires when nothing is pre-provisioned")]
    [Fact]
    public async Task Offline_disables_auto_acquire_and_falls_back_to_jd0026()
    {
        await Given("inputs for DACPAC mode with a throwing tool acquirer, no manifest, no global tool", SetupForDacpacMode)
            .When("task executes with AutoAcquireTool=true, OfflineMode=true, TFM net8.0", s =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = s.Engine,
                    WorkingDirectory = s.WorkingDir,
                    DacpacPath = s.DacpacPath,
                    ConfigPath = s.ConfigPath,
                    RenamingPath = s.RenamingPath,
                    TemplateDir = s.TemplateDir,
                    OutputDir = s.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net8.0",
                    OfflineMode = "true",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = new ThrowingToolAcquirer()
                };

                var success = task.Execute();
                return new TaskResult(s, task, success);
            })
            .Then("task fails", r => !r.Success)
            .And("the error carries the JD0026 code (not a generic exception from a wrongly-invoked acquirer)", r =>
                r.Setup.Engine.Errors.Any(e => e.Code == "JD0026"))
            .And("no manifest was bootstrapped in the working directory", r =>
                !File.Exists(Path.Combine(r.Setup.WorkingDir, ".config", "dotnet-tools.json")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (c) Acquisition failure -> JD0027 coded error + return false (no throw).
    [Scenario("A failed acquisition attempt is translated into a coded JD0027 error and the task returns false without throwing")]
    [Fact]
    public async Task Acquisition_failure_reports_jd0027()
    {
        await Given("inputs for DACPAC mode with a failing recording tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                var acquirer = new RecordingToolAcquirer(succeed: false, errorMessage: "dotnet tool install exited with code 1: simulated NuGet failure");
                return (setup, acquirer);
            })
            .When("task executes with AutoAcquireTool=true, TFM net9.0, no manifest, no global tool", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net9.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = ctx.acquirer
                };

                var success = task.Execute();
                return (Result: new TaskResult(ctx.setup, task, success), ctx.acquirer);
            })
            .Then("task fails (returns false, not a thrown exception)", r => !r.Result.Success)
            .And("exactly one acquisition attempt was made", r => r.acquirer.Requests.Count == 1)
            .And("the error carries the JD0027 code", r =>
                r.Result.Setup.Engine.Errors.Any(e => e.Code == "JD0027"))
            .And("the error message includes fix options (global install)", r =>
                r.Result.Setup.Engine.Errors.Any(e =>
                    (e.Message?.Contains("dotnet tool install --global", StringComparison.OrdinalIgnoreCase) ?? false)))
            .And("the error message includes the captured failure detail", r =>
                r.Result.Setup.Engine.Errors.Any(e =>
                    (e.Message?.Contains("simulated NuGet failure", StringComparison.OrdinalIgnoreCase) ?? false)))
            .And("no stack-trace-only exception message replaced the coded error (decorator did not catch a throw)", r =>
                r.Result.Setup.Engine.Errors.All(e => e.Code != null))
            .Finally(r => r.Result.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (d) .NET 10 + SDK10 + dnx available -> dnx path chosen, NO acquisition (unaffected).
    [Scenario(".NET 10+ with dnx usable takes the dnx path and never attempts acquisition, even with AutoAcquireTool=true")]
    [Fact]
    public async Task Dnx_usable_skips_acquisition_entirely()
    {
        await Given("inputs for DACPAC mode with a capturing fake dotnet and a throwing tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                return (setup, captureFile, fakeDotNet);
            })
            .When("task executes with TFM net10.0, dnx usable, AutoAcquireTool=true", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net10.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new DnxUsableSdkProbe(),
                    ToolAcquirer = new ThrowingToolAcquirer()
                };

                var success = task.Execute();
                return new CaptureResult(ctx.setup, task, success, ctx.captureFile);
            })
            .Then("task succeeds", r => r.Success)
            .And("no error is logged", r => r.Setup.Engine.Errors.Count == 0)
            .And("dnx was invoked", r =>
                File.ReadAllText(r.CaptureFile).Contains("dnx", StringComparison.OrdinalIgnoreCase))
            .And("tool install/restore was never invoked", r =>
            {
                var text = File.ReadAllText(r.CaptureFile);
                return !text.Contains("tool install", StringComparison.OrdinalIgnoreCase) &&
                       !text.Contains("tool restore", StringComparison.OrdinalIgnoreCase);
            })
            .And("no manifest was bootstrapped", r =>
                !File.Exists(Path.Combine(r.Setup.WorkingDir, ".config", "dotnet-tools.json")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (e) AutoAcquireTool=false + 8/9 + no manifest -> legacy global-tool behavior (no bootstrap).
    [Scenario("AutoAcquireTool=false on .NET 8/9 with no manifest falls back to legacy global-tool resolution without bootstrapping anything")]
    [Fact]
    public async Task AutoAcquire_disabled_uses_legacy_global_tool_path()
    {
        await Given("inputs for DACPAC mode with a capturing fake dotnet, a fake global tool, and a throwing tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                var dotNetCaptureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var globalToolCaptureFile = Path.Combine(setup.Folder.Root, "global-tool-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", dotNetCaptureFile);
                var fakeGlobalTool = WriteCaptureScript(setup.Folder, "fake-efcpt-global.cmd", "GLOBALTOOL", globalToolCaptureFile);
                return (setup, dotNetCaptureFile, globalToolCaptureFile, fakeDotNet, fakeGlobalTool);
            })
            .When("task executes with AutoAcquireTool=false, TFM net8.0, no manifest", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    // Deliberately NOT "auto": RunEfcpt.ExecuteCore sets
                    // forceManifestOnNonWindows = !IsWindows && !HasExplicitPath(ToolPath), and
                    // ToolModeUsesManifest treats "auto" as manifest-mode whenever
                    // forceManifestOnNonWindows is true - regardless of whether a manifest
                    // actually exists. Since this scenario intentionally has no manifest and an
                    // empty ToolPath, "auto" would take the forced-manifest branch on Linux/macOS
                    // (and, with AutoAcquireTool=false and no manifest to restore, dead-end into
                    // the JD0028 "acquisition not configured" error instead of ever reaching the
                    // legacy global-tool path this scenario intends to exercise) while resolving
                    // via the Default/global-tool branch on Windows (where
                    // forceManifestOnNonWindows is always false). "global" is any
                    // non-"auto"/"tool-manifest" value, which unconditionally behaves like the
                    // global tool mode on every platform (see the analogous fix in
                    // OfflineModeTests.Offline_with_global_tool_on_path_succeeds_without_update),
                    // so this test deterministically exercises the legacy global-tool path
                    // identically on Windows and Linux.
                    ToolMode = "global",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    // Stands in for a real global tool resolvable on PATH: the Default branch of
                    // ToolResolutionStrategy invokes ToolCommand directly as the executable.
                    ToolCommand = ctx.fakeGlobalTool,
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "false",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = new ThrowingToolAcquirer()
                };

                var success = task.Execute();
                return (ctx.setup, task, success, ctx.dotNetCaptureFile, ctx.globalToolCaptureFile);
            })
            .Then("task succeeds", r => r.success)
            .And("no error is logged", r => r.setup.Engine.Errors.Count == 0)
            .And("legacy global tool update ran", r =>
                File.Exists(r.dotNetCaptureFile) &&
                File.ReadAllText(r.dotNetCaptureFile).Contains("tool update --global", StringComparison.OrdinalIgnoreCase))
            .And("the global tool executable was invoked directly", r =>
                File.Exists(r.globalToolCaptureFile) &&
                File.ReadAllText(r.globalToolCaptureFile).Contains("GLOBALTOOL", StringComparison.Ordinal))
            .And("no manifest was bootstrapped", r =>
                !File.Exists(Path.Combine(r.setup.WorkingDir, ".config", "dotnet-tools.json")))
            .Finally(r => r.setup.Folder.Dispose())
            .AssertPassed();
    }

    // Bonus: exercises the production DefaultToolAcquirer end to end (via a fake "dotnet.cmd"
    // capturing script standing in for the real dotnet muxer, so no real process/network call is
    // ever made) to confirm it forms the expected `dotnet new tool-manifest` /
    // `dotnet tool install <pkg> --version <ver>` command sequence.
    [Scenario("DefaultToolAcquirer issues 'dotnet new tool-manifest' then 'dotnet tool install <pkg> --version <ver>' via a fake dotnet, and reports success")]
    [Fact]
    public async Task DefaultToolAcquirer_issues_expected_command_sequence()
    {
        await Given("a folder with no existing manifest and a capturing fake dotnet", () =>
            {
                var folder = new TestFolder();
                var manifestDir = folder.CreateDir("obj-efcpt");
                var captureFile = Path.Combine(folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                return (folder, manifestDir, captureFile, fakeDotNet);
            })
            .When("DefaultToolAcquirer.Acquire is called", ctx =>
            {
                var acquirer = new DefaultToolAcquirer();
                var request = new ToolAcquisitionRequest(ctx.manifestDir, ctx.fakeDotNet, "ErikEJ.EFCorePowerTools.Cli", "10.*");
                var outcome = acquirer.Acquire(request, NullBuildLog.Instance);
                return (ctx.folder, ctx.captureFile, outcome);
            })
            .Then("acquisition reports success", r => r.outcome.Success)
            .And("'dotnet new tool-manifest' was invoked", r =>
                File.ReadAllText(r.captureFile).Contains("new tool-manifest", StringComparison.OrdinalIgnoreCase))
            .And("'dotnet tool install ErikEJ.EFCorePowerTools.Cli --version \"10.*\"' was invoked", r =>
                File.ReadAllText(r.captureFile).Contains("tool install ErikEJ.EFCorePowerTools.Cli --version", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.folder.Dispose())
            .AssertPassed();
    }

    // (f) #186 adversarial-review FIX 1: a manifest that already lists the tool is used exactly
    // as-is - acquisition must NEVER be attempted, even with AutoAcquireTool=true.
    [Scenario("A tool manifest that already lists the tool is used as-is: acquisition is never attempted")]
    [Fact]
    public async Task Manifest_listing_tool_skips_acquisition_entirely()
    {
        await Given("inputs for DACPAC mode with a pre-existing manifest listing the tool, a capturing fake dotnet, and a throwing tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                WriteManifest(setup.WorkingDir, listsTool: true);
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                return (setup, captureFile, fakeDotNet);
            })
            .When("task executes with AutoAcquireTool=true, ToolMode=auto, a throwing tool acquirer", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = new ThrowingToolAcquirer()
                };

                var success = task.Execute();
                return new CaptureResult(ctx.setup, task, success, ctx.captureFile);
            })
            .Then("task succeeds", r => r.Success)
            .And("no error is logged", r => r.Setup.Engine.Errors.Count == 0)
            .And("resolution chose 'dotnet tool run efcpt' against the pre-existing manifest", r =>
            {
                var lines = File.ReadAllLines(r.CaptureFile);
                return lines.Length > 0 &&
                       lines[^1].Contains("tool run", StringComparison.OrdinalIgnoreCase) &&
                       lines[^1].Contains("efcpt", StringComparison.OrdinalIgnoreCase);
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (g) #186 adversarial-review FIX 1 HIGH regression guard: a global tool being resolvable on
    // PATH must only skip acquisition when resolution would NOT use a manifest. When ToolMode
    // forces manifest resolution, a global tool on PATH is irrelevant - acquisition still runs.
    [Scenario("Global tool on PATH but ToolMode=tool-manifest forces manifest resolution: acquisition still runs (FIX 1 HIGH regression guard)")]
    [Fact]
    public async Task ToolManifestMode_with_global_tool_on_path_still_acquires()
    {
        await Given("inputs for DACPAC mode, ToolMode=tool-manifest, no manifest present, a global-tool-only probe, and a recording tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                var acquirer = new RecordingToolAcquirer();
                return (setup, captureFile, fakeDotNet, acquirer);
            })
            .When("task executes with ToolMode=tool-manifest, AutoAcquireTool=true, Probe reports a global tool installed", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "tool-manifest",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new GlobalToolOnlySdkProbe(),
                    ToolAcquirer = ctx.acquirer
                };

                var success = task.Execute();
                return (Result: new CaptureResult(ctx.setup, task, success, ctx.captureFile), ctx.acquirer);
            })
            .Then("task succeeds", r => r.Result.Success)
            .And("no error is logged", r => r.Result.Setup.Engine.Errors.Count == 0)
            .And("acquisition ran despite a global tool being reported installed", r => r.acquirer.Requests.Count == 1)
            .And("resolution chose 'dotnet tool run efcpt' (not the global tool)", r =>
            {
                var lines = File.ReadAllLines(r.Result.CaptureFile);
                return lines.Length > 0 &&
                       lines[^1].Contains("tool run", StringComparison.OrdinalIgnoreCase) &&
                       lines[^1].Contains("efcpt", StringComparison.OrdinalIgnoreCase);
            })
            .Finally(r => r.Result.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (h) Companion to (g): when resolution would NOT use a manifest, a global tool on PATH DOES
    // skip acquisition. ToolMode="auto" with no discovered manifest is used here - on Windows
    // that means "no force" (global tool wins, matching the literal #186 scenario); on
    // non-Windows RunEfcpt itself forces "auto" to manifest resolution with no manifest/ToolPath
    // (see ToolModeUsesManifest/forceManifestOnNonWindows), so acquisition correctly runs there
    // instead - both branches are asserted so this passes deterministically in Windows dev
    // environments AND on the ubuntu-latest CI runner.
    [Scenario("Global tool on PATH with ToolMode=auto and no discovered manifest: the global-tool skip and acquisition are mutually exclusive and agree with whether resolution would use a manifest")]
    [Fact]
    public async Task GlobalTool_on_path_with_auto_mode_matches_wouldUseManifest_gating()
    {
        await Given("inputs for DACPAC mode with a capturing fake dotnet + fake global tool, no manifest, and a recording tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                var dotNetCaptureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var globalToolCaptureFile = Path.Combine(setup.Folder.Root, "global-tool-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", dotNetCaptureFile);
                var fakeGlobalTool = WriteCaptureScript(setup.Folder, "fake-efcpt-global.cmd", "GLOBALTOOL", globalToolCaptureFile);
                var acquirer = new RecordingToolAcquirer();
                return (setup, dotNetCaptureFile, globalToolCaptureFile, fakeDotNet, fakeGlobalTool, acquirer);
            })
            .When("task executes with ToolMode=auto, AutoAcquireTool=true, no manifest, global tool on PATH", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolCommand = ctx.fakeGlobalTool,
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new GlobalToolOnlySdkProbe(),
                    ToolAcquirer = ctx.acquirer
                };

                var success = task.Execute();
                return (ctx.setup, task, success, ctx.globalToolCaptureFile, ctx.acquirer);
            })
            .Then("task succeeds", r => r.success)
            .And("no error is logged", r => r.setup.Engine.Errors.Count == 0)
            .And("global tool used directly on Windows (no force); manifest acquisition used on non-Windows (forced 'auto'->manifest)", r =>
            {
                var globalToolInvoked = File.Exists(r.globalToolCaptureFile) &&
                    File.ReadAllText(r.globalToolCaptureFile).Contains("GLOBALTOOL", StringComparison.Ordinal);
                var acquisitionRan = r.acquirer.Requests.Count == 1;

                return OperatingSystem.IsWindows()
                    ? globalToolInvoked && !acquisitionRan
                    : !globalToolInvoked && acquisitionRan;
            })
            .Finally(r => r.setup.Folder.Dispose())
            .AssertPassed();
    }

    // (i) #186 adversarial-review FIX 1: manifest present but doesn't list the tool,
    // AutoAcquireTool=true -> bootstraps/installs into the EXISTING manifest directory (not a
    // second, shadowing one).
    [Scenario("A tool manifest present but not listing the tool: AutoAcquireTool=true installs the tool into the existing manifest directory")]
    [Fact]
    public async Task Manifest_present_without_tool_and_autoacquire_enabled_installs_into_existing_manifest()
    {
        await Given("inputs for DACPAC mode with a pre-existing manifest that does not list the tool, a capturing fake dotnet, and a recording tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                WriteManifest(setup.WorkingDir, listsTool: false);
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                var acquirer = new RecordingToolAcquirer();
                return (setup, captureFile, fakeDotNet, acquirer);
            })
            .When("task executes with AutoAcquireTool=true, ToolMode=auto", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = ctx.acquirer
                };

                var success = task.Execute();
                return (Result: new CaptureResult(ctx.setup, task, success, ctx.captureFile), ctx.acquirer);
            })
            .Then("task succeeds", r => r.Result.Success)
            .And("no error is logged", r => r.Result.Setup.Engine.Errors.Count == 0)
            .And("exactly one acquisition request was recorded", r => r.acquirer.Requests.Count == 1)
            .And("the request targets the EXISTING manifest directory, not a new/shadowing one", r =>
                r.acquirer.Requests[0].ManifestDir.Equals(Path.GetFullPath(r.Result.Setup.WorkingDir), StringComparison.OrdinalIgnoreCase))
            .And("resolution chose 'dotnet tool run efcpt' against the freshly-updated manifest", r =>
            {
                var lines = File.ReadAllLines(r.Result.CaptureFile);
                return lines.Length > 0 &&
                       lines[^1].Contains("tool run", StringComparison.OrdinalIgnoreCase) &&
                       lines[^1].Contains("efcpt", StringComparison.OrdinalIgnoreCase);
            })
            .Finally(r => r.Result.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (j) #186 adversarial-review FIX 1: manifest present but doesn't list the tool,
    // AutoAcquireTool=false -> JD0028 coded error, task returns false, and 'dotnet tool run' is
    // NEVER invoked against the incomplete manifest (no doomed run).
    [Scenario("A tool manifest present but not listing the tool: AutoAcquireTool=false reports the actionable JD0028 error instead of a doomed 'dotnet tool run'")]
    [Fact]
    public async Task Manifest_present_without_tool_and_autoacquire_disabled_reports_jd0028()
    {
        await Given("inputs for DACPAC mode with a pre-existing manifest that does not list the tool, a capturing fake dotnet, and a throwing tool acquirer", () =>
            {
                var setup = SetupForDacpacMode();
                WriteManifest(setup.WorkingDir, listsTool: false);
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(setup.Folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                return (setup, captureFile, fakeDotNet);
            })
            .When("task executes with AutoAcquireTool=false, ToolMode=auto", ctx =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = ctx.setup.Engine,
                    WorkingDirectory = ctx.setup.WorkingDir,
                    DacpacPath = ctx.setup.DacpacPath,
                    ConfigPath = ctx.setup.ConfigPath,
                    RenamingPath = ctx.setup.RenamingPath,
                    TemplateDir = ctx.setup.TemplateDir,
                    OutputDir = ctx.setup.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    AutoAcquireTool = "false",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new AllUnavailableSdkProbe(),
                    ToolAcquirer = new ThrowingToolAcquirer()
                };

                var success = task.Execute();
                return new CaptureResult(ctx.setup, task, success, ctx.captureFile);
            })
            .Then("task fails", r => !r.Success)
            .And("the error carries the JD0028 code", r => r.Setup.Engine.Errors.Any(e => e.Code == "JD0028"))
            .And("the error message references remediation (tool install and EfcptAutoAcquireTool)", r =>
                r.Setup.Engine.Errors.Any(e =>
                    (e.Message?.Contains("dotnet tool install", StringComparison.OrdinalIgnoreCase) ?? false) &&
                    (e.Message?.Contains("EfcptAutoAcquireTool", StringComparison.OrdinalIgnoreCase) ?? false)))
            .And("'dotnet tool run' was never invoked (no doomed run against the incomplete manifest)", r =>
                !File.Exists(r.CaptureFile) || !File.ReadAllText(r.CaptureFile).Contains("tool run", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // (k) #186 adversarial-review FIX 1 (idempotent rebuild): DefaultToolAcquirer must not
    // re-issue 'dotnet new tool-manifest' when a manifest already exists at the target directory
    // - only 'dotnet tool install' should run.
    [Scenario("DefaultToolAcquirer skips 'dotnet new tool-manifest' when a manifest already exists at the target directory, issuing only 'dotnet tool install'")]
    [Fact]
    public async Task DefaultToolAcquirer_skips_new_manifest_when_one_already_exists()
    {
        await Given("a folder with an EXISTING manifest and a capturing fake dotnet", () =>
            {
                var folder = new TestFolder();
                var manifestDir = folder.CreateDir("obj-efcpt");
                WriteManifest(manifestDir, listsTool: false);
                var captureFile = Path.Combine(folder.Root, "dotnet-invocations.log");
                var fakeDotNet = WriteCaptureScript(folder, "fake-dotnet.cmd", "DOTNET", captureFile);
                return (folder, manifestDir, captureFile, fakeDotNet);
            })
            .When("DefaultToolAcquirer.Acquire is called against the existing manifest directory", ctx =>
            {
                var acquirer = new DefaultToolAcquirer();
                var request = new ToolAcquisitionRequest(ctx.manifestDir, ctx.fakeDotNet, "ErikEJ.EFCorePowerTools.Cli", "10.*");
                var outcome = acquirer.Acquire(request, NullBuildLog.Instance);
                return (ctx.folder, ctx.captureFile, outcome);
            })
            .Then("acquisition reports success", r => r.outcome.Success)
            .And("'dotnet new tool-manifest' was NOT invoked (idempotent rebuild - manifest already exists)", r =>
                !File.Exists(r.captureFile) || !File.ReadAllText(r.captureFile).Contains("new tool-manifest", StringComparison.OrdinalIgnoreCase))
            .And("'dotnet tool install' WAS invoked", r =>
                File.Exists(r.captureFile) &&
                File.ReadAllText(r.captureFile).Contains("tool install ErikEJ.EFCorePowerTools.Cli --version", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.folder.Dispose())
            .AssertPassed();
    }
}
