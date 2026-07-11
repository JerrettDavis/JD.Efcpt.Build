using JD.Efcpt.Build.Core.Diagnostics;
using JD.Efcpt.Build.Core.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Core.Tests.Diagnostics;

/// <summary>
/// Table-drives every branch of <see cref="DoctorEngine.Diagnose"/>'s verdict ladder (the same
/// branch ladder <c>JD.Efcpt.Build.Tasks.EfcptDoctor</c> used pre-#181, moved here verbatim) using
/// a fake <see cref="ISdkProbe"/> - no process is ever spawned.
/// </summary>
[Feature("DoctorEngine: diagnoses efcpt tool resolution and reports a verdict")]
[Collection(nameof(AssemblySetup))]
public sealed partial class DoctorEngineTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed class FakeSdkProbe : ISdkProbe
    {
        public bool Sdk10Installed { get; init; }
        public bool DnxAvailable { get; init; }
        public bool GlobalToolInstalled { get; init; }

        public bool IsDotNet10SdkInstalled(string dotnetExe) => Sdk10Installed;
        public bool IsDnxAvailable(string dotnetExe) => DnxAvailable;
        public bool IsGlobalToolInstalled(string toolCommand) => GlobalToolInstalled;
    }

    private sealed record SetupState(TestFolder Folder, DoctorInputs Inputs, FakeSdkProbe Probe);

    private sealed record DiagnosisResult(
        SetupState Setup,
        string Verdict,
        bool HasViablePath,
        IReadOnlyList<string> Messages);

    private static DiagnosisResult Diagnose(SetupState setup)
    {
        var (verdict, hasViablePath, messages) = DoctorEngine.Diagnose(setup.Inputs, setup.Probe);
        return new DiagnosisResult(setup, verdict, hasViablePath, messages);
    }

    private static void WriteManifest(string workingDir, bool listsTool)
    {
        var configDir = Path.Combine(workingDir, ".config");
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

    private static DoctorInputs BaseInputs(
        TestFolder folder,
        string toolMode = "global",
        bool offline = false,
        bool autoAcquire = true,
        string toolPath = "",
        string targetFramework = "net8.0") =>
        new(
            TargetFramework: targetFramework,
            ToolMode: toolMode,
            ToolPackageId: "ErikEJ.EFCorePowerTools.Cli",
            ToolVersion: "",
            ToolCommand: "efcpt",
            ToolPath: toolPath,
            DotNetExe: "dotnet",
            WorkingDirectory: folder.Root,
            Offline: offline,
            AutoAcquire: autoAcquire,
            Strict: false);

    [Scenario("Explicit ToolPath that exists on disk wins outright, regardless of anything else")]
    [Fact]
    public async Task Explicit_tool_path_exists_wins()
    {
        await Given("a test folder with an explicit, existing ToolPath", () =>
            {
                var folder = new TestFolder();
                var toolPath = folder.WriteFile("tools/efcpt.exe", "fake");
                return new SetupState(folder, BaseInputs(folder, toolPath: toolPath), new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict names the explicit ToolPath", r => r.Verdict.Contains("Explicit ToolPath will be used directly"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Explicit ToolPath that does not exist is not viable")]
    [Fact]
    public async Task Explicit_tool_path_missing_is_not_viable()
    {
        await Given("a test folder with an explicit, non-existent ToolPath", () =>
            {
                var folder = new TestFolder();
                var toolPath = Path.Combine(folder.Root, "tools", "does-not-exist.exe");
                return new SetupState(folder, BaseInputs(folder, toolPath: toolPath), new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict says the file does not exist", r => r.Verdict.Contains("does not exist"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("dnx usable (.NET 10+, SDK installed, dnx available, not offline) wins before any manifest/global-tool check")]
    [Fact]
    public async Task Dnx_usable_wins()
    {
        await Given("a .NET 10 target with SDK 10 + dnx available", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "auto", targetFramework: "net10.0");
                var probe = new FakeSdkProbe { Sdk10Installed = true, DnxAvailable = true };
                return new SetupState(folder, inputs, probe);
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict names dnx execution", r => r.Verdict.Contains("dnx execution will be used"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Tool-manifest mode with a manifest that lists the tool is viable")]
    [Fact]
    public async Task Manifest_lists_tool_is_viable()
    {
        await Given("a tool-manifest that already lists the tool", () =>
            {
                var folder = new TestFolder();
                WriteManifest(folder.Root, listsTool: true);
                return new SetupState(folder, BaseInputs(folder, toolMode: "tool-manifest"), new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict names tool-manifest resolution", r => r.Verdict.Contains("Tool-manifest resolution will be used"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Tool-manifest found but missing the tool, offline, is not viable")]
    [Fact]
    public async Task Manifest_missing_tool_offline_is_not_viable()
    {
        await Given("a manifest that doesn't list the tool, offline mode enabled", () =>
            {
                var folder = new TestFolder();
                WriteManifest(folder.Root, listsTool: false);
                return new SetupState(folder, BaseInputs(folder, toolMode: "tool-manifest", offline: true), new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict cites offline mode preventing restore", r => r.Verdict.Contains("EfcptOfflineMode prevents restoring it"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Tool-manifest found but missing the tool, auto-acquire enabled, is viable")]
    [Fact]
    public async Task Manifest_missing_tool_auto_acquire_enabled_is_viable()
    {
        await Given("a manifest that doesn't list the tool, auto-acquire enabled", () =>
            {
                var folder = new TestFolder();
                WriteManifest(folder.Root, listsTool: false);
                var inputs = BaseInputs(folder, toolMode: "tool-manifest", offline: false, autoAcquire: true);
                return new SetupState(folder, inputs, new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict says auto-acquire will install into the existing manifest", r => r.Verdict.Contains("will install") && r.Verdict.Contains("existing manifest"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Tool-manifest found but missing the tool, auto-acquire disabled, is not viable")]
    [Fact]
    public async Task Manifest_missing_tool_auto_acquire_disabled_is_not_viable()
    {
        await Given("a manifest that doesn't list the tool, auto-acquire disabled", () =>
            {
                var folder = new TestFolder();
                WriteManifest(folder.Root, listsTool: false);
                var inputs = BaseInputs(folder, toolMode: "tool-manifest", offline: false, autoAcquire: false);
                return new SetupState(folder, inputs, new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict says auto-acquire is disabled", r => r.Verdict.Contains("EfcptAutoAcquireTool is disabled"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No tool manifest found, auto-acquire enabled, is viable (bootstraps one)")]
    [Fact]
    public async Task No_manifest_auto_acquire_enabled_is_viable()
    {
        await Given("tool-manifest mode with no manifest present, auto-acquire enabled", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "tool-manifest", offline: false, autoAcquire: true);
                return new SetupState(folder, inputs, new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict says auto-acquire will bootstrap a manifest", r => r.Verdict.Contains("will bootstrap one"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No tool manifest found, offline, is not viable")]
    [Fact]
    public async Task No_manifest_offline_is_not_viable()
    {
        await Given("tool-manifest mode with no manifest present, offline mode enabled", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "tool-manifest", offline: true, autoAcquire: true);
                return new SetupState(folder, inputs, new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict cites offline mode preventing acquisition", r => r.Verdict.Contains("EfcptOfflineMode prevents acquisition/restore"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No tool manifest found, auto-acquire disabled, not offline, is not viable")]
    [Fact]
    public async Task No_manifest_auto_acquire_disabled_is_not_viable()
    {
        await Given("tool-manifest mode with no manifest present, auto-acquire disabled", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "tool-manifest", offline: false, autoAcquire: false);
                return new SetupState(folder, inputs, new FakeSdkProbe());
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict says auto-acquire is disabled", r => r.Verdict.Contains("EfcptAutoAcquireTool is disabled"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Global tool already resolvable on PATH is viable")]
    [Fact]
    public async Task Global_tool_resolvable_is_viable()
    {
        await Given("global tool mode with the tool already resolvable", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "global");
                var probe = new FakeSdkProbe { GlobalToolInstalled = true };
                return new SetupState(folder, inputs, probe);
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict names global tool resolution", r => r.Verdict.Contains("Global tool resolution will be used"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No global tool, auto-acquire enabled, is viable (bootstraps a local manifest)")]
    [Fact]
    public async Task No_global_tool_auto_acquire_enabled_is_viable()
    {
        await Given("global tool mode with no tool resolvable, auto-acquire enabled", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "global", offline: false, autoAcquire: true);
                var probe = new FakeSdkProbe { GlobalToolInstalled = false };
                return new SetupState(folder, inputs, probe);
            })
            .When("Diagnose is called", Diagnose)
            .Then("a viable path is reported", r => r.HasViablePath)
            .And("the verdict says auto-acquire will bootstrap a local manifest", r => r.Verdict.Contains("will bootstrap a local manifest"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No global tool, offline, not viable")]
    [Fact]
    public async Task No_global_tool_offline_is_not_viable()
    {
        await Given("global tool mode with no tool resolvable, offline mode enabled", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "global", offline: true, autoAcquire: true);
                var probe = new FakeSdkProbe { GlobalToolInstalled = false };
                return new SetupState(folder, inputs, probe);
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict cites offline mode preventing acquisition", r => r.Verdict.Contains("EfcptOfflineMode prevents acquisition"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No global tool, auto-acquire disabled, not offline, not viable")]
    [Fact]
    public async Task No_global_tool_auto_acquire_disabled_is_not_viable()
    {
        await Given("global tool mode with no tool resolvable, auto-acquire disabled", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "global", offline: false, autoAcquire: false);
                var probe = new FakeSdkProbe { GlobalToolInstalled = false };
                return new SetupState(folder, inputs, probe);
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("the verdict recommends installing the tool globally", r => r.Verdict.Contains("Install the tool globally"))
            .And("the verdict names the unresolved target framework", r => r.Verdict.Contains("TargetFramework='net8.0'"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("When no viable path is found and 'dotnet' is unresolvable on PATH, an advisory is appended before the verdict")]
    [Fact]
    public async Task Unresolvable_dotnet_appends_probe_advisory()
    {
        await Given("global tool mode, nothing resolvable, and a bogus dotnet executable not on PATH", () =>
            {
                var folder = new TestFolder();
                var inputs = new DoctorInputs(
                    TargetFramework: "net8.0",
                    ToolMode: "global",
                    ToolPackageId: "ErikEJ.EFCorePowerTools.Cli",
                    ToolVersion: "",
                    ToolCommand: "efcpt",
                    ToolPath: "",
                    DotNetExe: "definitely-not-dotnet-xyz-12345",
                    WorkingDirectory: folder.Root,
                    Offline: false,
                    AutoAcquire: false,
                    Strict: false);
                return new SetupState(folder, inputs, new FakeSdkProbe { GlobalToolInstalled = false });
            })
            .When("Diagnose is called", Diagnose)
            .Then("no viable path is reported", r => !r.HasViablePath)
            .And("an inconclusive-probe advisory naming the bogus dotnet is present", r =>
                r.Messages.Any(m => m.Contains("SDK/dnx probes were inconclusive")
                                    && m.Contains("definitely-not-dotnet-xyz-12345")))
            .And("the advisory is placed immediately before the verdict (verdict stays last)", r =>
                r.Messages[^1] == $"Verdict: {r.Verdict}"
                && r.Messages[^2].Contains("SDK/dnx probes were inconclusive"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Diagnose reports every message in the same order EfcptDoctor logged them pre-#181")]
    [Fact]
    public async Task Diagnose_reports_messages_in_expected_order()
    {
        await Given("global tool mode with the tool resolvable", () =>
            {
                var folder = new TestFolder();
                var inputs = BaseInputs(folder, toolMode: "global");
                var probe = new FakeSdkProbe { GlobalToolInstalled = true };
                return new SetupState(folder, inputs, probe);
            })
            .When("Diagnose is called", Diagnose)
            .Then("the first message reports the target framework", r => r.Messages[0].StartsWith("TargetFramework:"))
            .And("the last message is the verdict", r => r.Messages[^1] == $"Verdict: {r.Verdict}")
            .And("SDK/dnx/manifest/global-tool/offline/auto-acquire lines are all present", r =>
                r.Messages.Any(m => m.StartsWith("SDK 10+ installed:")) &&
                r.Messages.Any(m => m.StartsWith("dnx available:")) &&
                r.Messages.Any(m => m.StartsWith("dnx usable for this build:")) &&
                r.Messages.Any(m => m.StartsWith("Tool manifest discovered:")) &&
                r.Messages.Any(m => m.StartsWith("Global tool")) &&
                r.Messages.Any(m => m.StartsWith("Explicit ToolPath:")) &&
                r.Messages.Any(m => m.StartsWith("EfcptOfflineMode:")) &&
                r.Messages.Any(m => m.StartsWith("EfcptAutoAcquireTool:")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
