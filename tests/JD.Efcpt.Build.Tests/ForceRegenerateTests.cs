using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Proves the EfcptForceRegenerate mechanism (#191): setting EfcptForceRegenerate=true must
/// genuinely defeat EfcptGenerateModels's MSBuild Inputs/Outputs incremental gate, not just its
/// Condition. We can't drive the full generation pipeline here (no real efcpt tool / database in
/// CI), so these tests target the internal _EfcptForceRegenerateInvalidateStamp target directly -
/// it has no DependsOnTargets, so it can be invoked standalone via `-t:` - and assert on the one
/// observable side effect that actually defeats the incremental gate: deletion of EfcptStampFile
/// (EfcptGenerateModels's declared Outputs).
/// </summary>
[Feature("EfcptForceRegenerate: force a full model regeneration, bypassing the fingerprint/incremental cache")]
[Collection(nameof(AssemblySetup))]
public sealed partial class ForceRegenerateTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record ForceRegenContext(
        TestFolder Folder,
        string AppDir,
        string StampFile) : IDisposable
    {
        public void Dispose() => Folder.Dispose();
    }

    private sealed record MsBuildResult(
        ForceRegenContext Context,
        int ExitCode,
        string Output,
        bool StampFileExistedBefore,
        bool StampFileExistsAfter);

    private static ForceRegenContext SetupProjectWithExistingStamp()
    {
        var folder = new TestFolder();
        var appDir = folder.CreateDir("TestApp");

        var efcptBuildRoot = Path.Combine(TestPaths.RepoRoot, "src", "JD.Efcpt.Build");

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <Import Project="{efcptBuildRoot}/JD.Efcpt.Build.props" />

              <PropertyGroup>
                <EfcptEnabled>true</EfcptEnabled>
              </PropertyGroup>

              <Import Project="{efcptBuildRoot}/JD.Efcpt.Build.targets" />
            </Project>
            """;

        File.WriteAllText(Path.Combine(appDir, "TestApp.csproj"), csproj);

        // Simulate a previous successful generation: stamp file already up to date.
        var efcptOutputDir = Path.Combine(appDir, "obj", "efcpt");
        Directory.CreateDirectory(efcptOutputDir);
        var stampFile = Path.Combine(efcptOutputDir, ".efcpt.stamp");
        File.WriteAllText(stampFile, "previous-fingerprint-hash");

        return new ForceRegenContext(folder, appDir, stampFile);
    }

    private static MsBuildResult RunInvalidationTarget(ForceRegenContext context, bool forceRegenerate)
    {
        var stampExistedBefore = File.Exists(context.StampFile);

        var arguments = "msbuild -t:_EfcptForceRegenerateInvalidateStamp -v:normal"
            + (forceRegenerate ? " -p:EfcptForceRegenerate=true" : string.Empty);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = TestPaths.DotNetExe,
            Arguments = arguments,
            WorkingDirectory = context.AppDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(60000);

        var output = stdout + stderr;
        var stampExistsAfter = File.Exists(context.StampFile);

        return new MsBuildResult(context, process.ExitCode, output, stampExistedBefore, stampExistsAfter);
    }

    [Scenario("EfcptForceRegenerate=true invalidates the stamp file, defeating the Inputs/Outputs incremental gate")]
    [Fact]
    public Task ForceRegenerate_true_deletes_existing_stamp_file()
        => Given("a project with an up-to-date efcpt stamp file", SetupProjectWithExistingStamp)
            .Then("the stamp file exists before the build", ctx => File.Exists(ctx.StampFile))
            .When("running the invalidation target with EfcptForceRegenerate=true", ctx => RunInvalidationTarget(ctx, forceRegenerate: true))
            .Then("the target succeeds", r =>
            {
                if (r.ExitCode != 0)
                    throw new InvalidOperationException($"msbuild failed with exit code {r.ExitCode}. Output: {r.Output}");
                return true;
            })
            .And("the stamp file existed beforehand", r => r.StampFileExistedBefore)
            .And("the stamp file (EfcptGenerateModels' Outputs) is deleted", r => !r.StampFileExistsAfter)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("EfcptForceRegenerate unset (default false) leaves the stamp file untouched")]
    [Fact]
    public Task ForceRegenerate_default_false_preserves_existing_stamp_file()
        => Given("a project with an up-to-date efcpt stamp file", SetupProjectWithExistingStamp)
            .Then("the stamp file exists before the build", ctx => File.Exists(ctx.StampFile))
            .When("running the invalidation target without EfcptForceRegenerate set", ctx => RunInvalidationTarget(ctx, forceRegenerate: false))
            .Then("the target succeeds (Condition is false, target is a no-op)", r =>
            {
                if (r.ExitCode != 0)
                    throw new InvalidOperationException($"msbuild failed with exit code {r.ExitCode}. Output: {r.Output}");
                return true;
            })
            .And("the stamp file existed beforehand", r => r.StampFileExistedBefore)
            .And("the stamp file is left untouched - default behavior is unchanged", r => r.StampFileExistsAfter)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
}
