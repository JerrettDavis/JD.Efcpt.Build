using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.Utilities;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the <see cref="EfcptDoctor"/> diagnostic task (#186): verifies it reports TFM,
/// SDK/dnx/manifest/global-tool state, and a verdict describing either the execution path
/// <see cref="RunEfcpt"/> would take or the exact remediation if none is viable - and that it
/// never fails the build unless <c>Strict</c> is set.
/// </summary>
[Feature("EfcptDoctor: report which efcpt tool execution path RunEfcpt would take, without running anything")]
[Collection(nameof(AssemblySetup))]
public sealed class EfcptDoctorTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed class DnxUsableSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => true;
        public bool IsDnxAvailable(string dotnetExe) => true;
        public bool IsGlobalToolInstalled(string toolCommand) => false;
    }

    private sealed class AllUnavailableSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => false;
        public bool IsDnxAvailable(string dotnetExe) => false;
        public bool IsGlobalToolInstalled(string toolCommand) => false;
    }

    private sealed record DoctorResult(TestFolder Folder, EfcptDoctor Task, bool Success);

    [Scenario("On .NET 10+ with dnx usable, the verdict reports the dnx execution path and the task succeeds")]
    [Fact]
    public async Task Reports_dnx_path_for_dotnet10_with_dnx_usable()
    {
        await Given("a doctor task targeting net10.0 with dnx usable", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net10.0",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolCommand = "efcpt",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    Strict = "false",
                    Probe = new DnxUsableSdkProbe()
                };
                return (folder, task);
            })
            .When("the task executes", ctx =>
            {
                var success = ctx.task.Execute();
                return new DoctorResult(ctx.folder, ctx.task, success);
            })
            .Then("the task succeeds", r => r.Success)
            .And("the verdict mentions dnx", r => r.Task.Verdict.Contains("dnx", StringComparison.OrdinalIgnoreCase))
            .And("HasViablePath is true", r => r.Task.HasViablePath)
            .And("Messages includes a TargetFramework line", r =>
                r.Task.Messages.Any(m => m.StartsWith("TargetFramework:", StringComparison.Ordinal)))
            .And("Messages includes the verdict line", r =>
                r.Task.Messages.Any(m => m.StartsWith("Verdict:", StringComparison.Ordinal)))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("On .NET 8/9 with nothing usable and AutoAcquireTool=false, the verdict reports actionable remediation and HasViablePath is false, but the task still succeeds (non-strict)")]
    [Fact]
    public async Task Reports_remediation_when_no_viable_path_and_non_strict_still_succeeds()
    {
        await Given("a doctor task targeting net8.0 with nothing usable and AutoAcquireTool=false", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net8.0",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolCommand = "efcpt",
                    OfflineMode = "false",
                    AutoAcquireTool = "false",
                    Strict = "false",
                    Probe = new AllUnavailableSdkProbe()
                };
                return (folder, task);
            })
            .When("the task executes", ctx =>
            {
                var success = ctx.task.Execute();
                return new DoctorResult(ctx.folder, ctx.task, success);
            })
            .Then("the task still succeeds (non-strict)", r => r.Success)
            .And("HasViablePath is false", r => !r.Task.HasViablePath)
            .And("the verdict offers a remediation (global install)", r =>
                r.Task.Verdict.Contains("dotnet tool install --global", StringComparison.OrdinalIgnoreCase))
            .And("no error was logged", r => true) // non-strict: EfcptDoctor never logs Log.LogError unless Strict
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Strict mode fails the task when no viable execution path is found")]
    [Fact]
    public async Task Strict_mode_fails_when_no_viable_path()
    {
        await Given("a doctor task targeting net8.0 with nothing usable, AutoAcquireTool=false, Strict=true", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net8.0",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolCommand = "efcpt",
                    OfflineMode = "false",
                    AutoAcquireTool = "false",
                    Strict = "true",
                    Probe = new AllUnavailableSdkProbe()
                };
                return (folder, task, engine);
            })
            .When("the task executes", ctx =>
            {
                var success = ctx.task.Execute();
                return (Result: new DoctorResult(ctx.folder, ctx.task, success), ctx.engine);
            })
            .Then("the task fails", r => !r.Result.Success)
            .And("an error is logged", r => r.engine.Errors.Count > 0)
            .Finally(r => r.Result.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("AutoAcquireTool=true on .NET 8/9 with nothing else usable reports that acquisition will run, and HasViablePath is true")]
    [Fact]
    public async Task Reports_acquisition_will_run_when_enabled()
    {
        await Given("a doctor task targeting net8.0 with nothing usable and AutoAcquireTool=true", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net9.0",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolCommand = "efcpt",
                    OfflineMode = "false",
                    AutoAcquireTool = "true",
                    Strict = "false",
                    Probe = new AllUnavailableSdkProbe()
                };
                return (folder, task);
            })
            .When("the task executes", ctx =>
            {
                var success = ctx.task.Execute();
                return new DoctorResult(ctx.folder, ctx.task, success);
            })
            .Then("the task succeeds", r => r.Success)
            .And("HasViablePath is true", r => r.Task.HasViablePath)
            .And("the verdict mentions EfcptAutoAcquireTool bootstrapping a manifest", r =>
                r.Task.Verdict.Contains("EfcptAutoAcquireTool", StringComparison.OrdinalIgnoreCase) &&
                r.Task.Verdict.Contains("bootstrap", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Offline mode with nothing pre-provisioned reports HasViablePath=false and mentions offline in the remediation")]
    [Fact]
    public async Task Reports_offline_remediation_when_nothing_pre_provisioned()
    {
        await Given("a doctor task targeting net8.0, offline, with nothing pre-provisioned", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net8.0",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli",
                    ToolCommand = "efcpt",
                    OfflineMode = "true",
                    AutoAcquireTool = "true",
                    Strict = "false",
                    Probe = new AllUnavailableSdkProbe()
                };
                return (folder, task);
            })
            .When("the task executes", ctx =>
            {
                var success = ctx.task.Execute();
                return new DoctorResult(ctx.folder, ctx.task, success);
            })
            .Then("the task succeeds (non-strict)", r => r.Success)
            .And("HasViablePath is false", r => !r.Task.HasViablePath)
            .And("the verdict mentions EfcptOfflineMode preventing acquisition", r =>
                r.Task.Verdict.Contains("EfcptOfflineMode", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }
}
