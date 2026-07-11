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
                var toolDir = setup.Folder.CreateDir("tools");
                // A trivial, real, always-succeeding script stands in for the efcpt CLI. On
                // Windows, CommandNormalizationStrategy wraps .cmd files via `cmd.exe /c`, so
                // this genuinely spawns and exits 0 regardless of the args RunEfcpt appends -
                // letting this test exercise real tool resolution/restore (not the
                // EFCPT_FAKE_EFCPT short-circuit, which returns before any of that runs) while
                // still asserting the (throwing) probe is never invoked.
                var toolPath = Path.Combine(toolDir, "fake-efcpt.cmd");
                File.WriteAllText(toolPath, "@echo off\r\nexit /b 0\r\n");
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
}
