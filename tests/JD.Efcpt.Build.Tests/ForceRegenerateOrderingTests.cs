using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// End-to-end proof of the crux claim behind #191 (and therefore the VS #182 / VS Code #183
/// integrations): a target hooked with <c>BeforeTargets="Gen"</c> runs BEFORE <c>Gen</c>'s own
/// MSBuild <c>Inputs</c>/<c>Outputs</c> up-to-date evaluation, so deleting <c>Gen</c>'s declared
/// <c>Outputs</c> from that hook genuinely forces <c>Gen</c> to RE-RUN even when it would otherwise
/// be skipped as up-to-date.
///
/// The direct-invocation <see cref="ForceRegenerateTests"/> only prove the invalidation target's
/// Delete/Condition in isolation - they can't prove the ORDERING relative to the incremental gate.
/// This test reproduces exactly that pattern (Inputs/Outputs incremental target + a BeforeTargets
/// stamp-deleting hook gated on a Force property) in a minimal, self-contained synthetic MSBuild
/// project and drives it with real `dotnet msbuild` invocations - no efcpt tool or database
/// required. The three-build flow deliberately includes an unforced middle build whose SKIP must be
/// observed; if that skip does not happen, the whole test is meaningless, so it is asserted
/// explicitly (run count must NOT increment on the unforced rebuild).
/// </summary>
[Feature("Force-regenerate ordering: BeforeTargets stamp-deletion defeats MSBuild Inputs/Outputs incremental gating")]
[Collection(nameof(AssemblySetup))]
public sealed partial class ForceRegenerateOrderingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record OrderingContext(
        TestFolder Folder,
        string ProjectDir,
        string ProjectPath,
        string StampFile,
        string RunCountFile) : IDisposable
    {
        public void Dispose() => Folder.Dispose();
    }

    private sealed record OrderingResult(
        OrderingContext Context,
        int RunCountAfterFirst,
        int RunCountAfterSecondNoForce,
        int RunCountAfterThirdForce,
        string StampAfterSecond,
        string StampAfterThird,
        string CombinedOutput,
        bool AllBuildsSucceeded);

    // Minimal synthetic project mirroring the real EfcptGenerateModels + _EfcptForceRegenerateInvalidateStamp
    // shape: an Inputs/Outputs-gated target plus a BeforeTargets hook that deletes the output stamp
    // only when Force=true. No SDK, no tasks assembly - pure inbox MSBuild so it runs anywhere.
    private const string SyntheticProject = """
        <Project>
          <PropertyGroup>
            <InFile Condition="'$(InFile)'==''">$(MSBuildProjectDirectory)/input.txt</InFile>
            <StampFile Condition="'$(StampFile)'==''">$(MSBuildProjectDirectory)/stamp.txt</StampFile>
            <RunCountFile Condition="'$(RunCountFile)'==''">$(MSBuildProjectDirectory)/runcount.txt</RunCountFile>
            <Force Condition="'$(Force)'==''">false</Force>
          </PropertyGroup>

          <!-- Mirrors _EfcptForceRegenerateInvalidateStamp: runs before Gen's own up-to-date check,
               deletes Gen's Outputs so the incremental gate cannot skip it. Only when forcing. -->
          <Target Name="_Invalidate" BeforeTargets="Gen" Condition="'$(Force)' == 'true'">
            <Delete Condition="Exists('$(StampFile)')" Files="$(StampFile)" />
          </Target>

          <!-- Mirrors EfcptGenerateModels: Inputs/Outputs-gated. Each real run appends a marker line
               (so we can count runs) and rewrites the stamp with a fresh value. -->
          <Target Name="Gen" Inputs="$(InFile)" Outputs="$(StampFile)">
            <WriteLinesToFile File="$(RunCountFile)" Lines="ran" Overwrite="false" />
            <WriteLinesToFile File="$(StampFile)" Lines="$([System.DateTime]::UtcNow.Ticks)" Overwrite="true" />
          </Target>
        </Project>
        """;

    private static OrderingContext SetupSyntheticProject()
    {
        var folder = new TestFolder();
        var projectDir = folder.CreateDir("SyntheticOrdering");

        var projectPath = Path.Combine(projectDir, "ordering.proj");
        File.WriteAllText(projectPath, SyntheticProject);

        // The single Input must exist and be OLDER than the Output stamp for MSBuild to ever
        // consider Gen up to date. Backdate it so the first Gen run - which writes the stamp "now" -
        // reliably produces stamp-newer-than-input, independent of filesystem timestamp resolution.
        var inFile = Path.Combine(projectDir, "input.txt");
        File.WriteAllText(inFile, "schema-input");
        File.SetLastWriteTimeUtc(inFile, DateTime.UtcNow.AddHours(-1));

        var stampFile = Path.Combine(projectDir, "stamp.txt");
        var runCountFile = Path.Combine(projectDir, "runcount.txt");

        return new OrderingContext(folder, projectDir, projectPath, stampFile, runCountFile);
    }

    private static (int ExitCode, string Output) RunGen(OrderingContext context, bool force)
    {
        var arguments = $"msbuild \"{context.ProjectPath}\" -t:Gen -v:normal"
            + (force ? " -p:Force=true" : string.Empty);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = TestPaths.DotNetExe,
            Arguments = arguments,
            WorkingDirectory = context.ProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(120000);

        return (process.ExitCode, stdout + stderr);
    }

    private static int RunCount(OrderingContext context)
        => File.Exists(context.RunCountFile)
            ? File.ReadAllLines(context.RunCountFile).Count(l => !string.IsNullOrWhiteSpace(l))
            : 0;

    private static string StampContent(OrderingContext context)
        => File.Exists(context.StampFile) ? File.ReadAllText(context.StampFile).Trim() : "";

    private static OrderingResult RunThreeBuildFlow(OrderingContext context)
    {
        var allSucceeded = true;

        // (a) First build - Gen must run (no stamp yet), creating the stamp and marker #1.
        var (exit1, out1) = RunGen(context, force: false);
        allSucceeded &= exit1 == 0;
        var runCountAfterFirst = RunCount(context);

        // (b) Second build, unforced, inputs unchanged - Gen MUST be skipped as up-to-date.
        //     This is the load-bearing observation: if it is NOT skipped, the test is tautological.
        var (exit2, out2) = RunGen(context, force: false);
        allSucceeded &= exit2 == 0;
        var runCountAfterSecond = RunCount(context);
        var stampAfterSecond = StampContent(context);

        // (c) Third build with Force=true, inputs STILL unchanged - the BeforeTargets hook deletes
        //     the stamp before Gen's up-to-date check, so Gen must re-run despite being "up to date".
        var (exit3, out3) = RunGen(context, force: true);
        allSucceeded &= exit3 == 0;
        var runCountAfterThird = RunCount(context);
        var stampAfterThird = StampContent(context);

        return new OrderingResult(
            context,
            runCountAfterFirst,
            runCountAfterSecond,
            runCountAfterThird,
            stampAfterSecond,
            stampAfterThird,
            string.Join("\n----\n", out1, out2, out3),
            allSucceeded);
    }

    [Scenario("Force=true re-runs an Inputs/Outputs-gated target that an unforced rebuild skips as up-to-date")]
    [Fact]
    public Task Force_defeats_inputs_outputs_incremental_gate_via_before_targets_ordering()
        => Given("a synthetic Inputs/Outputs-gated project with a Force-gated BeforeTargets stamp deleter", SetupSyntheticProject)
            .When("building three times: initial, unforced rebuild, then forced rebuild (inputs never change)", RunThreeBuildFlow)
            .Then("all three builds succeed", r =>
            {
                if (!r.AllBuildsSucceeded)
                    throw new InvalidOperationException($"One or more msbuild invocations failed. Output:\n{r.CombinedOutput}");
                return true;
            })
            .And("(a) the first build ran Gen (run count = 1)", r => r.RunCountAfterFirst == 1)
            .And("(b) the unforced rebuild SKIPPED Gen as up-to-date (run count still 1)", r => r.RunCountAfterSecondNoForce == 1)
            .And("(c) the forced rebuild RE-RAN Gen despite unchanged inputs (run count = 2)", r => r.RunCountAfterThirdForce == 2)
            .And("(c) the forced rebuild rewrote the stamp with a fresh value", r =>
                r.StampAfterThird.Length > 0 && r.StampAfterThird != r.StampAfterSecond)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("Sanity: without ever forcing, an unchanged rebuild never re-runs the gated target")]
    [Fact]
    public Task Unforced_rebuilds_never_rerun_the_gated_target()
        => Given("a synthetic Inputs/Outputs-gated project", SetupSyntheticProject)
            .When("building twice, unforced, with unchanged inputs", ctx =>
            {
                var (exit1, out1) = RunGen(ctx, force: false);
                var countAfterFirst = RunCount(ctx);
                var (exit2, out2) = RunGen(ctx, force: false);
                var countAfterSecond = RunCount(ctx);
                return (ctx, exit1, exit2, countAfterFirst, countAfterSecond, output: out1 + "\n----\n" + out2);
            })
            .Then("both builds succeed", r => r.exit1 == 0 && r.exit2 == 0)
            .And("the first build ran the target once", r => r.countAfterFirst == 1)
            .And("the second build was skipped (up-to-date), proving the incremental gate is real", r => r.countAfterSecond == 1)
            .Finally(r => r.ctx.Dispose())
            .AssertPassed();
}
