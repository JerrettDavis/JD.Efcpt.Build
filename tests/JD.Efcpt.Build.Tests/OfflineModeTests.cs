using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.Utilities;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for <c>EfcptOfflineMode</c>: verifies that offline mode never spawns any of the three
/// network-dependent tool resolution/restore branches (dnx, tool-manifest restore, global tool
/// update), and that it fails actionably with <c>JD0026</c> when the tool cannot be guaranteed
/// to run without a network call.
/// </summary>
[Feature("EfcptOfflineMode: skip network-dependent tool resolution when offline")]
[Collection(nameof(AssemblySetup))]
public sealed class OfflineModeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    /// <summary>
    /// An <see cref="ISdkProbe"/> that throws if any probe method is invoked - used to assert
    /// that a given code path genuinely never calls into SDK/dnx/global-tool detection.
    /// </summary>
    private sealed class ThrowingSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) =>
            throw new InvalidOperationException("IsDotNet10SdkInstalled should not be called in this scenario.");

        public bool IsDnxAvailable(string dotnetExe) =>
            throw new InvalidOperationException("IsDnxAvailable should not be called in this scenario.");

        public bool IsGlobalToolInstalled(string toolCommand) =>
            throw new InvalidOperationException("IsGlobalToolInstalled should not be called in this scenario.");
    }

    /// <summary>
    /// An <see cref="ISdkProbe"/> that deterministically reports every capability as
    /// unavailable, without spawning any process - used for the negative (JD0026) scenario so
    /// the outcome doesn't depend on what's actually installed on the test machine.
    /// </summary>
    private sealed class AllUnavailableSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => false;
        public bool IsDnxAvailable(string dotnetExe) => false;
        public bool IsGlobalToolInstalled(string toolCommand) => false;
    }

    /// <summary>
    /// An <see cref="ISdkProbe"/> that reports the global tool as installed and throws if the
    /// dnx-related probe methods are invoked - used for the global-tool-on-PATH offline scenario,
    /// where only <see cref="IsGlobalToolInstalled"/> should ever be consulted.
    /// </summary>
    private sealed class GlobalToolOnlySdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) =>
            throw new InvalidOperationException("IsDotNet10SdkInstalled should not be called in this scenario.");

        public bool IsDnxAvailable(string dotnetExe) =>
            throw new InvalidOperationException("IsDnxAvailable should not be called in this scenario.");

        public bool IsGlobalToolInstalled(string toolCommand) => true;
    }

    /// <summary>
    /// An <see cref="ISdkProbe"/> that records how many times each probe method is invoked,
    /// while deterministically reporting every capability as unavailable (so it never itself
    /// steers tool resolution toward dnx or the global tool) - used for the offline=false
    /// gate-regression scenario, where we want to observe what actually got called rather than
    /// assert nothing was.
    /// </summary>
    private sealed class CountingSdkProbe : ISdkProbe
    {
        public int IsDotNet10SdkInstalledCalls { get; private set; }
        public int IsDnxAvailableCalls { get; private set; }
        public int IsGlobalToolInstalledCalls { get; private set; }

        public bool IsDotNet10SdkInstalled(string dotnetExe)
        {
            IsDotNet10SdkInstalledCalls++;
            return false;
        }

        public bool IsDnxAvailable(string dotnetExe)
        {
            IsDnxAvailableCalls++;
            return false;
        }

        public bool IsGlobalToolInstalled(string toolCommand)
        {
            IsGlobalToolInstalledCalls++;
            return false;
        }
    }

    /// <summary>
    /// Writes a minimal, valid <c>.config/dotnet-tools.json</c> manifest under
    /// <paramref name="folder"/>'s root that lists an entry for the efcpt tool - matching the
    /// default <c>ToolPackageId</c> (<c>ErikEJ.EFCorePowerTools.Cli</c>) and <c>ToolCommand</c>
    /// (<c>efcpt</c>) used throughout these tests.
    /// </summary>
    private static void WriteEfcptToolManifest(TestFolder folder) =>
        folder.WriteFile(
            ".config/dotnet-tools.json",
            """
            {
              "version": 1,
              "isRoot": true,
              "tools": {
                "erikej.efcorepowertools.cli": {
                  "version": "10.0.0",
                  "commands": [ "efcpt" ]
                }
              }
            }
            """);

    private sealed record SetupState(
        TestFolder Folder,
        string WorkingDir,
        string DacpacPath,
        string ConfigPath,
        string RenamingPath,
        string TemplateDir,
        string OutputDir,
        TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        RunEfcpt Task,
        bool Success);

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

    [Scenario("Offline mode with a pre-provisioned explicit ToolPath succeeds without touching any SDK probe")]
    [Fact]
    public async Task Offline_with_explicit_tool_path_skips_all_probes()
    {
        await Given("inputs for DACPAC mode with a pre-provisioned explicit tool path", () =>
            {
                var setup = SetupForDacpacMode();
                // A trivial, real, always-succeeding script stands in for the efcpt CLI. On
                // Windows this is a .cmd file (CommandNormalizationStrategy wraps it via
                // `cmd.exe /c`); on Linux/macOS it's a shell script with a shebang and the
                // executable bit set, launched directly. Either way it genuinely spawns and
                // exits 0 regardless of the args RunEfcpt appends - letting this test exercise
                // real tool resolution/restore (not the EFCPT_FAKE_EFCPT short-circuit, which
                // returns before any of that runs) while still asserting the (throwing) probe is
                // never invoked.
                var toolPath = TestScripts.CreateAlwaysSucceedsScript(setup.Folder, "fake-efcpt");
                return (setup, toolPath);
            })
            .When("task executes offline with a throwing probe", ctx =>
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
                    OfflineMode = "true",
                    ToolPath = ctx.toolPath,
                    Probe = new ThrowingSdkProbe()
                };

                var success = task.Execute();
                return new TaskResult(ctx.setup, task, success);
            })
            .Then("task succeeds", r => r.Success)
            .And("no error is logged", r => r.Setup.Engine.Errors.Count == 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Offline mode with no explicit tool path, no manifest, and no global tool fails actionably with JD0026")]
    [Fact]
    public async Task Offline_with_no_runnable_tool_fails_with_jd0026()
    {
        await Given("inputs for DACPAC mode with no pre-provisioned tool", SetupForDacpacMode)
            .When("task executes offline without fake mode, TFM net8.0, no manifest, no ToolPath", s =>
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
                    ToolPath = "",
                    Probe = new AllUnavailableSdkProbe()
                };

                var success = task.Execute();
                return new TaskResult(s, task, success);
            })
            .Then("task fails", r => !r.Success)
            .And("an error is logged", r => r.Setup.Engine.Errors.Count > 0)
            .And("the error carries the JD0026 code", r =>
                r.Setup.Engine.Errors.Any(e => e.Code == "JD0026"))
            .And("the error message includes a dotnet tool install command", r =>
                r.Setup.Engine.Errors.Any(e =>
                    (e.Message?.Contains("dotnet tool install", StringComparison.OrdinalIgnoreCase) ?? false)))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Offline mode is a no-op when disabled (default)")]
    [Fact]
    public async Task Offline_mode_disabled_by_default()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes in fake mode without setting OfflineMode", s =>
            {
                Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "true");
                try
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
                        ToolPackageId = "ErikEJ.EFCorePowerTools.Cli"
                    };
                    var success = task.Execute();
                    return new TaskResult(s, task, success);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", null);
                }
            })
            .Then("task succeeds", r => r.Success)
            .And("OfflineMode defaults to false", r => r.Task.OfflineMode == "false")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // The four scenarios above all short-circuit before the `!ctx.Offline` STRATEGY gates:
    // Offline_with_explicit_tool_path_skips_all_probes resolves via the explicit-ToolPath leg
    // (the very first `.When` in ToolResolutionStrategy, unconditional on Offline), and
    // Offline_mode_disabled_by_default uses EFCPT_FAKE_EFCPT, which returns before tool
    // resolution runs at all. The scenarios below exercise the `!ctx.Offline`-gated branches
    // directly - tool-manifest resolution/restore and the global-tool/update-global path - to
    // prove offline mode actually gates them (and that the gate isn't inverted or always-on).

    private sealed record CaptureResult(SetupState Setup, RunEfcpt Task, bool Success, string CaptureFile);

    [Scenario("Offline mode with a present, tool-listing manifest resolves via 'dotnet tool run' and never probes dnx or the global tool, and never restores")]
    [Fact]
    public async Task Offline_with_manifest_listing_tool_resolves_via_tool_run_without_restore()
    {
        await Given("inputs for DACPAC mode with a real tool manifest listing the efcpt tool, and a capturing fake dotnet", () =>
            {
                var setup = SetupForDacpacMode();
                WriteEfcptToolManifest(setup.Folder);
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = TestScripts.CreateCaptureScript(setup.Folder, "fake-dotnet", "DOTNET", captureFile);
                return (setup, captureFile, fakeDotNet);
            })
            .When("task executes offline, TFM net10.0, no explicit ToolPath, with a throwing probe", ctx =>
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
                    ToolCommand = "efcpt",
                    TargetFramework = "net10.0",
                    OfflineMode = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new ThrowingSdkProbe()
                };

                var success = task.Execute();
                return new CaptureResult(ctx.setup, task, success, ctx.captureFile);
            })
            .Then("task succeeds", r => r.Success)
            .And("no error is logged", r => r.Setup.Engine.Errors.Count == 0)
            .And("dotnet was invoked exactly once, via 'tool run efcpt'", r =>
            {
                var lines = File.ReadAllLines(r.CaptureFile);
                return lines.Length == 1 &&
                       lines[0].Contains("tool run", StringComparison.OrdinalIgnoreCase) &&
                       lines[0].Contains("efcpt", StringComparison.OrdinalIgnoreCase);
            })
            .And("tool restore was never invoked", r =>
                !File.ReadAllText(r.CaptureFile).Contains("tool restore", StringComparison.OrdinalIgnoreCase))
            .And("dnx was never invoked", r =>
                !File.ReadAllText(r.CaptureFile).Contains("dnx", StringComparison.OrdinalIgnoreCase))
            .And("global tool update was never invoked", r =>
                !File.ReadAllText(r.CaptureFile).Contains("tool update", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Offline mode with no manifest but a global tool on PATH succeeds directly and never updates the global tool")]
    [Fact]
    public async Task Offline_with_global_tool_on_path_succeeds_without_update()
    {
        await Given("inputs for DACPAC mode with no manifest, a fake global-tool executable, and a capturing fake dotnet", () =>
            {
                var setup = SetupForDacpacMode();
                var globalToolCaptureFile = Path.Combine(setup.Folder.Root, "global-tool-invocations.log");
                var dotNetCaptureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeGlobalTool = TestScripts.CreateCaptureScript(setup.Folder, "fake-efcpt-global", "GLOBALTOOL", globalToolCaptureFile);
                var fakeDotNet = TestScripts.CreateCaptureScript(setup.Folder, "fake-dotnet", "DOTNET", dotNetCaptureFile);
                return (setup, globalToolCaptureFile, dotNetCaptureFile, fakeGlobalTool, fakeDotNet);
            })
            .When("task executes offline with no manifest, no ToolPath, and IsGlobalToolInstalled=true", ctx =>
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
                    // ToolIsAutoOrManifest treats "auto" as manifest-mode whenever
                    // forceManifestOnNonWindows is true - regardless of whether a manifest
                    // actually exists. Since this scenario intentionally has no manifest and an
                    // empty ToolPath, "auto" would resolve via the dotnet-tool-run branch on
                    // Linux/macOS (invoking fakeDotNet, never fakeGlobalTool) while resolving via
                    // the Default/global-tool branch on Windows (where forceManifestOnNonWindows
                    // is always false) - a platform-dependent divergence baked into the product's
                    // "auto" mode by design (see the ToolMode doc comment: "use a local tool
                    // manifest if one is discovered ... otherwise fall back to the global tool",
                    // combined with the non-Windows fragile-PATH guard). "global" is any
                    // non-"auto"/"tool-manifest" value, which per that same doc comment "behaves
                    // like the global tool mode" unconditionally on every platform, so this test
                    // exercises the Default branch (ctx.ToolCommand invoked directly) identically
                    // on Windows and Linux.
                    ToolMode = "global",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    // Stands in for a real global tool resolvable on PATH: the Default branch of
                    // ToolResolutionStrategy invokes ctx.ToolCommand directly as the executable.
                    ToolCommand = ctx.fakeGlobalTool,
                    TargetFramework = "net8.0",
                    OfflineMode = "true",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = new GlobalToolOnlySdkProbe()
                };

                var success = task.Execute();
                return (Result: new CaptureResult(ctx.setup, task, success, ctx.globalToolCaptureFile), ctx.dotNetCaptureFile);
            })
            .Then("task succeeds", r => r.Result.Success)
            .And("no error is logged", r => r.Result.Setup.Engine.Errors.Count == 0)
            .And("the global tool executable was invoked directly, exactly once", r =>
            {
                var lines = File.ReadAllLines(r.Result.CaptureFile);
                return lines.Length == 1 && lines[0].StartsWith("GLOBALTOOL", StringComparison.Ordinal);
            })
            .And("dotnet (and therefore 'tool update --global') was never invoked", r =>
                !File.Exists(r.dotNetCaptureFile))
            .Finally(r => r.Result.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("EFCPT_OFFLINE environment variable alone (no EfcptOfflineMode property) reproduces offline pre-flight behavior")]
    [Fact]
    public async Task Offline_env_var_alone_triggers_jd0026()
    {
        await Given("inputs for DACPAC mode with no pre-provisioned tool", SetupForDacpacMode)
            .When("task executes with EFCPT_OFFLINE set but OfflineMode left at its default", s =>
            {
                Environment.SetEnvironmentVariable("EFCPT_OFFLINE", "true");
                try
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
                        // OfflineMode intentionally left unset (defaults to "false") - only the
                        // EFCPT_OFFLINE environment variable signals offline. This exercises the
                        // task-level OR-in at RunEfcpt.cs (`OfflineMode.IsTrue() ||
                        // Environment.GetEnvironmentVariable("EFCPT_OFFLINE").IsTrue()`), which is
                        // independent of the MSBuild-property-level bridge added to
                        // BuildTransitivePropsFactory (verified separately via the generated
                        // buildTransitive/*.props XML, since exercising MSBuild property
                        // evaluation itself would require a full MSBuild target harness rather
                        // than this task-level unit test).
                        ToolPath = "",
                        Probe = new AllUnavailableSdkProbe()
                    };

                    var success = task.Execute();
                    return new TaskResult(s, task, success);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("EFCPT_OFFLINE", null);
                }
            })
            .Then("task fails", r => !r.Success)
            .And("OfflineMode property itself is still the default 'false'", r => r.Task.OfflineMode == "false")
            .And("the error carries the JD0026 code", r =>
                r.Setup.Engine.Errors.Any(e => e.Code == "JD0026"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Offline=false gate-regression: a present manifest is restored normally, proving the !ctx.Offline gate is not inverted or always-on")]
    [Fact]
    public async Task Offline_disabled_gate_regression_manifest_restore_runs()
    {
        await Given("inputs for DACPAC mode with a real tool manifest, offline explicitly disabled, and a capturing fake dotnet", () =>
            {
                var setup = SetupForDacpacMode();
                WriteEfcptToolManifest(setup.Folder);
                var captureFile = Path.Combine(setup.Folder.Root, "dotnet-invocations.log");
                var fakeDotNet = TestScripts.CreateCaptureScript(setup.Folder, "fake-dotnet", "DOTNET", captureFile);
                return (setup, captureFile, fakeDotNet);
            })
            .When("task executes with OfflineMode=false, TFM net8.0, manifest present, counting probe", ctx =>
            {
                var probe = new CountingSdkProbe();
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
                    ToolCommand = "efcpt",
                    TargetFramework = "net8.0",
                    OfflineMode = "false",
                    ToolPath = "",
                    DotNetExe = ctx.fakeDotNet,
                    Probe = probe
                };

                var success = task.Execute();
                return (Result: new CaptureResult(ctx.setup, task, success, ctx.captureFile), Probe: probe);
            })
            .Then("task succeeds", r => r.Result.Success)
            .And("no error is logged", r => r.Result.Setup.Engine.Errors.Count == 0)
            .And("tool restore DID run (the normal, non-offline path)", r =>
                File.ReadAllText(r.Result.CaptureFile).Contains("tool restore", StringComparison.OrdinalIgnoreCase))
            .And("tool run also ran, after restore", r =>
                File.ReadAllText(r.Result.CaptureFile).Contains("tool run", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Result.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
