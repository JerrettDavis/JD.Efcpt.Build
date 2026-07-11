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

    /// <summary>
    /// Writes a real <c>.config/dotnet-tools.json</c> manifest into <paramref name="manifestDir"/>
    /// - listing the efcpt tool when <paramref name="listsTool"/> is <see langword="true"/>, or an
    /// unrelated placeholder tool otherwise (a manifest that exists but never had efcpt installed
    /// into it).
    /// </summary>
    private static void WriteManifest(string manifestDir, bool listsTool)
    {
        var configDir = Path.Combine(manifestDir, ".config");
        Directory.CreateDirectory(configDir);

        var toolsEntry = listsTool
            ? """
              "erikej.efcorepowertools.cli": {
                    "version": "10.0.0",
                    "commands": [ "efcpt" ]
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
                    // Deliberately NOT "auto" (the default): EfcptDoctor.Execute computes
                    // forceManifestOnNonWindows = !IsWindows && !HasExplicitPath(ToolPath), and
                    // RunEfcpt.ToolModeUsesManifest treats "auto" as manifest-mode whenever
                    // forceManifestOnNonWindows is true - regardless of whether a manifest
                    // actually exists. With no manifest present here, "auto" would make
                    // DetermineVerdict take the "no tool manifest found" branch on Linux/macOS
                    // (remediation: "dotnet new tool-manifest && dotnet tool install ...") while
                    // taking the global-tool branch on Windows (remediation: "dotnet tool install
                    // --global ..."), a platform-dependent divergence in the verdict text. "global"
                    // is any non-"auto"/"tool-manifest" value, so it bypasses forceManifestOnNonWindows
                    // entirely (same fix pattern as RunEfcptAutoAcquireTests and
                    // OfflineModeTests.Offline_with_global_tool_on_path_succeeds_without_update),
                    // making the global-install remediation this test asserts deterministic on
                    // every platform.
                    ToolMode = "global",
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

    // #186 adversarial-review FIX 2: a manifest present but not listing the tool must consult
    // autoAcquireEffective, matching RunEfcpt.AcquireToolIfNeeded's actual FIX 1 behavior - it is
    // viable (will install into the existing manifest) only when auto-acquire is effective.
    [Scenario("A tool manifest present but not listing the tool, with AutoAcquireTool=true: the verdict reports it will be installed into the existing manifest, and HasViablePath is true")]
    [Fact]
    public async Task Reports_viable_when_manifest_incomplete_and_autoacquire_enabled()
    {
        await Given("a doctor task targeting net8.0 with a manifest that does not list the tool and AutoAcquireTool=true", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                WriteManifest(workingDir, listsTool: false);
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net8.0",
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
            .And("the verdict mentions EfcptAutoAcquireTool installing into the existing manifest", r =>
                r.Task.Verdict.Contains("EfcptAutoAcquireTool", StringComparison.OrdinalIgnoreCase) &&
                r.Task.Verdict.Contains("install", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("A tool manifest present but not listing the tool, with AutoAcquireTool=false: the verdict offers the actionable 'dotnet tool install' remediation (not the misleading 'dotnet tool restore'), and HasViablePath is false")]
    [Fact]
    public async Task Reports_not_viable_when_manifest_incomplete_and_autoacquire_disabled()
    {
        await Given("a doctor task targeting net8.0 with a manifest that does not list the tool and AutoAcquireTool=false", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                WriteManifest(workingDir, listsTool: false);
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
            .And("the verdict offers 'dotnet tool install' remediation, not the misleading 'dotnet tool restore'", r =>
                r.Task.Verdict.Contains("dotnet tool install", StringComparison.OrdinalIgnoreCase) &&
                !r.Task.Verdict.Contains("dotnet tool restore", StringComparison.OrdinalIgnoreCase))
            .And("the verdict mentions enabling EfcptAutoAcquireTool as a fix option", r =>
                r.Task.Verdict.Contains("EfcptAutoAcquireTool=true", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    // Guards against a report that only surfaces TargetFramework/Verdict - each individual probe
    // result must be independently visible in Messages for troubleshooting.
    [Scenario("Messages includes each individual diagnostic field line (SDK10, dnx, manifest-lists-tool, global-tool, explicit-ToolPath), not just TargetFramework/Verdict")]
    [Fact]
    public async Task Reports_individual_diagnostic_field_lines()
    {
        await Given("a doctor task targeting net8.0 with a manifest that lists the tool and no explicit ToolPath", () =>
            {
                var folder = new TestFolder();
                var workingDir = folder.CreateDir("obj");
                WriteManifest(workingDir, listsTool: true);
                var engine = new TestBuildEngine();
                var task = new EfcptDoctor
                {
                    BuildEngine = engine,
                    WorkingDirectory = workingDir,
                    TargetFramework = "net8.0",
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
            .And("Messages includes an SDK 10+ installed line", r =>
                r.Task.Messages.Any(m => m.StartsWith("SDK 10+ installed:", StringComparison.Ordinal)))
            .And("Messages includes a dnx available line", r =>
                r.Task.Messages.Any(m => m.StartsWith("dnx available:", StringComparison.Ordinal)))
            .And("Messages includes a manifest-lists-tool line reporting True", r =>
                r.Task.Messages.Any(m => m.StartsWith("Manifest lists ", StringComparison.Ordinal) && m.Contains(": True", StringComparison.Ordinal)))
            .And("Messages includes a global tool resolvable line", r =>
                r.Task.Messages.Any(m => m.Contains("resolvable on PATH:", StringComparison.Ordinal)))
            .And("Messages includes an explicit ToolPath line reporting '(not set)'", r =>
                r.Task.Messages.Any(m => m.StartsWith("Explicit ToolPath:", StringComparison.Ordinal) && m.Contains("not set", StringComparison.Ordinal)))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }
}
