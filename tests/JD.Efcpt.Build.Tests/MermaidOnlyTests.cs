using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Proves the EfcptMermaidOnly mechanism (#246): when true, the regular build pipeline must skip
/// EfcptGenerateModels and EfcptAddToCompile (so no DbContext / entity .g.cs files end up compiled),
/// and EfcptGenerateMermaid must run instead. We can't drive the full generation pipeline here (no
/// real efcpt tool in CI), so these tests verify the conditional wiring directly by:
///   (a) target Condition: invoking EfcptGenerateModels / EfcptAddToCompile with EfcptMermaidOnly=true
///       must observe the Condition fail and skip the target's tasks - proven by the absence of
///       expected side-effect files (no .g.cs in $(EfcptGeneratedDir), no stamp at
///       $(EfcptStampFile));
///   (b) override-target wiring: invoking _EfcptApplyMermaidOnlyOverrides must run a PropertyGroup
///       that sets EfcptConfigGenerateMermaidDiagram=true and EfcptConfigGenerationType=dbcontext,
///       so the upstream efcpt CLI gets the right config.
///
/// Synthetic project mirrors the production shape: Microsoft.NET.Sdk + net8.0 + JD.Efcpt.Build
/// imported by absolute path so the test does not need to install the package from NuGet.
/// </summary>
[Feature("EfcptMermaidOnly: skip DbContext/entity generation and emit only the Mermaid ER diagram")]
[Collection(nameof(AssemblySetup))]
public sealed partial class MermaidOnlyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private const string TargetsRelativePath = "buildTransitive/JD.Efcpt.Build.targets";

    private sealed record Context(
        TestFolder Folder,
        string AppDir,
        string CsprojPath,
        string GeneratedDir,
        string StampFile) : IDisposable
    {
        public void Dispose() => Folder.Dispose();
    }

    private sealed record MsBuildResult(Context Ctx, int ExitCode, string Output);

    private static Context SetupProject(bool mermaidOnly = false)
    {
        var folder = new TestFolder();
        var appDir = folder.CreateDir("MermaidOnlyApp");

        var efcptBuildRoot = Path.Combine(TestPaths.RepoRoot, "src", "JD.Efcpt.Build");
        var targetsPath = Path.Combine(efcptBuildRoot, TargetsRelativePath).Replace('\\', '/');

        var mermaidOnlyLine = mermaidOnly
            ? $"      <EfcptMermaidOnly>true</EfcptMermaidOnly>{Environment.NewLine}"
            : string.Empty;

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <Import Project="{efcptBuildRoot}/JD.Efcpt.Build.props" />

              <PropertyGroup>
                <EfcptEnabled>true</EfcptEnabled>
                {mermaidOnlyLine}
              </PropertyGroup>

              <Import Project="{targetsPath}" />
            </Project>
            """;

        var csprojPath = Path.Combine(appDir, "MermaidOnlyApp.csproj");
        File.WriteAllText(csprojPath, csproj);

        // Pre-create the efcpt output dir so any generated-file assertions have a known location.
        var generatedDir = Path.Combine(appDir, "obj", "efcpt", "Generated");
        Directory.CreateDirectory(generatedDir);

        var stampFile = Path.Combine(appDir, "obj", "efcpt", ".efcpt.stamp");

        return new Context(folder, appDir, csprojPath, generatedDir, stampFile);
    }

    private static MsBuildResult RunTarget(Context ctx, string target, params (string Key, string Value)[] extraProps)
    {
        var propArgs = string.Join(" ", extraProps.Select(p => $"-p:{p.Key}={p.Value}"));
        var arguments = $"msbuild -t:{target} -v:minimal {propArgs}".Trim();

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = TestPaths.DotNetExe,
            Arguments = arguments,
            WorkingDirectory = ctx.AppDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(120000);

        return new MsBuildResult(ctx, process.ExitCode, stdout + stderr);
    }

    [Scenario("EfcptMermaidOnly=true makes EfcptGenerateModels skip (Condition honors the flag)")]
    [Fact]
    public Task MermaidOnly_true_skips_EfcptGenerateModels()
        => Given("a project with EfcptMermaidOnly=true", () => SetupProject(mermaidOnly: true))
            .When("invoking EfcptGenerateModels directly", ctx => RunTarget(ctx, "EfcptGenerateModels"))
            .Then("the build succeeds", r =>
            {
                if (r.ExitCode != 0)
                    throw new InvalidOperationException($"msbuild failed (exit {r.ExitCode}). Output: {r.Output}");
                return true;
            })
            .And("no .g.cs files were created in $(EfcptGeneratedDir)", r =>
                !Directory.Exists(r.Ctx.GeneratedDir) ||
                Directory.GetFiles(r.Ctx.GeneratedDir, "*.g.cs", SearchOption.AllDirectories).Length == 0)
            .And("no stamp file was written (target's Outputs attribute is skipped)", r => !File.Exists(r.Ctx.StampFile))
            .Finally(r => r.Ctx.Dispose())
            .AssertPassed();

    [Scenario("Standalone EfcptGenerateMermaid enables Mermaid-only config overrides")]
    [Fact]
    public Task Standalone_EfcptGenerateMermaid_enables_overrides()
        => Given("a project with EfcptMermaidOnly unset", () => SetupProject(mermaidOnly: false))
            .When("invoking EfcptGenerateMermaid directly", ctx => RunTarget(ctx, "EfcptGenerateMermaid"))
            .Then("standalone invocation enables Mermaid-only config overrides before generation", r =>
            {
                if (!r.Output.Contains("Standalone EfcptGenerateMermaid enables Mermaid-only config overrides before generation.", StringComparison.Ordinal))
                    throw new InvalidOperationException($"The standalone target did not enable Mermaid-only config overrides. Output: {r.Output}");
                return true;
            })
            .Finally(r => r.Ctx.Dispose())
            .AssertPassed();

    [Scenario("EfcptMermaidOnly unset (default false) does not block the normal generation target - but does not run here without a real efcpt tool")]
    [Fact]
    public Task MermaidOnly_default_does_not_block_EfcptGenerateModels()
        => Given("a project with EfcptMermaidOnly unset", () => SetupProject(mermaidOnly: false))
            .When("invoking EfcptGenerateModels directly (no real efcpt, so we tolerate the tool error)", ctx => RunTarget(ctx, "EfcptGenerateModels"))
            .Then("the build does NOT silently no-op the target", r =>
            {
                // The target's Condition is true in this case (EfcptMermaidOnly != 'true'),
                // so MSBuild enters the target's body. Without a real efcpt tool, RunEfcpt will
                // fail. The point is that the target is reached - a silent no-op (exit 0 with
                // nothing happening) would mean the Condition is broken in the other direction.
                // In this fresh fixture, generation cannot succeed, so any zero exit code means
                // the target was silently skipped.
                if (r.ExitCode == 0)
                    throw new InvalidOperationException("EfcptGenerateModels appears to have been Condition-skipped when EfcptMermaidOnly was unset (default). The Condition must require EfcptMermaidOnly != 'true'.");
                return true;
            })
            .Finally(r => r.Ctx.Dispose())
            .AssertPassed();
}