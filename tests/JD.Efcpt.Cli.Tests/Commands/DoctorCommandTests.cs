using JD.Efcpt.Build.Core.Diagnostics;
using JD.Efcpt.Cli.Commands;
using JD.Efcpt.Cli.Logging;
using JD.Efcpt.Cli.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="DoctorCommand"/>, driven directly via <see cref="DoctorCommand.Execute"/>
/// with a fake <see cref="ISdkProbe"/> (no process spawned) - verifying the exit-code contract:
/// 0 = viable path, 2 = no viable path (advisory), 1 = no viable path with --strict.
/// </summary>
[Feature("jd-efcpt doctor: exit-code contract (0 viable / 2 advisory / 1 strict)")]
[Collection(nameof(AssemblySetup))]
public sealed partial class DoctorCommandTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed class FakeSdkProbe : ISdkProbe
    {
        public bool GlobalToolInstalled { get; init; }

        public bool IsDotNet10SdkInstalled(string dotnetExe) => false;
        public bool IsDnxAvailable(string dotnetExe) => false;
        public bool IsGlobalToolInstalled(string toolCommand) => GlobalToolInstalled;
    }

    private sealed class ThrowingSdkProbe : ISdkProbe
    {
        public bool IsDotNet10SdkInstalled(string dotnetExe) => throw new InvalidOperationException("boom");
        public bool IsDnxAvailable(string dotnetExe) => throw new InvalidOperationException("boom");
        public bool IsGlobalToolInstalled(string toolCommand) => throw new InvalidOperationException("boom");
    }

    private sealed record SetupState(TestFolder Folder, DoctorInputs Inputs, FakeSdkProbe Probe);
    private sealed record RunResult(TestFolder Folder, int ExitCode);

    private static DoctorInputs Inputs(TestFolder folder, bool strict) => new(
        TargetFramework: "net8.0",
        ToolMode: "global",
        ToolPackageId: "ErikEJ.EFCorePowerTools.Cli",
        ToolVersion: "",
        ToolCommand: "efcpt",
        ToolPath: "",
        DotNetExe: "dotnet",
        WorkingDirectory: folder.Root,
        Offline: false,
        AutoAcquire: false,
        Strict: strict);

    private static RunResult RunDoctor(SetupState setup)
    {
        var exitCode = DoctorCommand.Execute(new ConsoleBuildLog(), setup.Inputs, setup.Probe);
        return new RunResult(setup.Folder, exitCode);
    }

    [Scenario("A viable path exits 0")]
    [Fact]
    public async Task Viable_path_exits_zero()
    {
        await Given("a global tool already resolvable on PATH", () =>
            {
                var folder = new TestFolder();
                return new SetupState(folder, Inputs(folder, strict: false), new FakeSdkProbe { GlobalToolInstalled = true });
            })
            .When("doctor runs", RunDoctor)
            .Then("exit code is 0", r => r.ExitCode == DoctorCommand.ExitViable)
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No viable path, not strict, exits 2 (advisory)")]
    [Fact]
    public async Task No_viable_path_not_strict_exits_two()
    {
        await Given("no global tool resolvable, auto-acquire disabled, not strict", () =>
            {
                var folder = new TestFolder();
                return new SetupState(folder, Inputs(folder, strict: false), new FakeSdkProbe { GlobalToolInstalled = false });
            })
            .When("doctor runs", RunDoctor)
            .Then("exit code is 2", r => r.ExitCode == DoctorCommand.ExitNoViablePathAdvisory)
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No viable path with --strict exits 1")]
    [Fact]
    public async Task No_viable_path_strict_exits_one()
    {
        await Given("no global tool resolvable, auto-acquire disabled, strict", () =>
            {
                var folder = new TestFolder();
                return new SetupState(folder, Inputs(folder, strict: true), new FakeSdkProbe { GlobalToolInstalled = false });
            })
            .When("doctor runs", RunDoctor)
            .Then("exit code is 1", r => r.ExitCode == DoctorCommand.ExitNoViablePathStrict)
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("An unexpected probe failure exits 3 (distinct from strict-no-path's 1)")]
    [Fact]
    public async Task Unexpected_failure_exits_three()
    {
        await Given("a probe that throws when invoked", () =>
            {
                var folder = new TestFolder();
                return new RunResult(folder,
                    DoctorCommand.Execute(new ConsoleBuildLog(), Inputs(folder, strict: true), new ThrowingSdkProbe()));
            })
            .When("the exit code is inspected", r => r)
            .Then("exit code is 3", r => r.ExitCode == DoctorCommand.ExitUnexpectedError)
            .And("3 is distinct from the strict-no-path exit code", r => DoctorCommand.ExitUnexpectedError != DoctorCommand.ExitNoViablePathStrict)
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }
}
