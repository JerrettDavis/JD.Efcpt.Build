using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

[Feature("Clean target: dotnet clean removes efcpt output directory")]
[Collection(nameof(AssemblySetup))]
public sealed class CleanTargetTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record CleanTestContext(
        TestFolder Folder,
        string AppDir,
        string EfcptOutputDir) : IDisposable
    {
        public void Dispose() => Folder.Dispose();
    }

    private sealed record CleanResult(
        CleanTestContext Context,
        int ExitCode,
        string Output,
        bool EfcptDirExistedBefore,
        bool EfcptDirExistsAfter);

    private static CleanTestContext SetupProjectWithEfcptOutput()
    {
        var folder = new TestFolder();
        var appDir = folder.CreateDir("TestApp");

        // Get the absolute path to the JD.Efcpt.Build source directory
        var efcptBuildRoot = Path.Combine(TestPaths.RepoRoot, "src", "JD.Efcpt.Build");

        // Create a minimal project file that imports our targets with absolute paths
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

        // Create the efcpt output directory with sample content (simulating a previous build)
        var efcptOutputDir = Path.Combine(appDir, "obj", "efcpt");
        Directory.CreateDirectory(efcptOutputDir);

        // Add sample files that would exist after a build
        File.WriteAllText(Path.Combine(efcptOutputDir, "fingerprint.txt"), "sample-fingerprint-hash");
        File.WriteAllText(Path.Combine(efcptOutputDir, "efcpt.stamp"), "stamp");

        var generatedDir = Path.Combine(efcptOutputDir, "Generated");
        Directory.CreateDirectory(generatedDir);
        File.WriteAllText(Path.Combine(generatedDir, "TestContext.g.cs"), "// generated file");
        File.WriteAllText(Path.Combine(generatedDir, "TestModel.g.cs"), "// generated model");

        return new CleanTestContext(folder, appDir, efcptOutputDir);
    }

    private static CleanResult ExecuteDotNetClean(CleanTestContext context)
    {
        var efcptDirExistedBefore = Directory.Exists(context.EfcptOutputDir);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = TestPaths.DotNetExe,
            Arguments = "clean",
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
        var efcptDirExistsAfter = Directory.Exists(context.EfcptOutputDir);

        return new CleanResult(context, process.ExitCode, output, efcptDirExistedBefore, efcptDirExistsAfter);
    }

    [Scenario("dotnet clean removes efcpt output directory")]
    [Fact]
    public Task Dotnet_clean_removes_efcpt_output_directory()
        => Given("project with efcpt output directory", SetupProjectWithEfcptOutput)
            .Then("efcpt directory exists before clean", ctx => Directory.Exists(ctx.EfcptOutputDir))
            .And("efcpt directory contains files", ctx =>
                Directory.GetFiles(ctx.EfcptOutputDir, "*", SearchOption.AllDirectories).Length > 0)
            .When("run dotnet clean", ExecuteDotNetClean)
            .Then("clean command succeeds", r =>
            {
                if (r.ExitCode != 0)
                    throw new InvalidOperationException($"dotnet clean failed with exit code {r.ExitCode}. Output: {r.Output}");
                return true;
            })
            .And("efcpt directory existed before clean", r => r.EfcptDirExistedBefore)
            .And("efcpt directory is removed after clean", r => !r.EfcptDirExistsAfter)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("dotnet clean succeeds when efcpt directory does not exist")]
    [Fact]
    public Task Dotnet_clean_succeeds_when_efcpt_directory_does_not_exist()
        => Given("project without efcpt output directory", () =>
            {
                var ctx = SetupProjectWithEfcptOutput();
                // Remove the efcpt directory to simulate a fresh state
                if (Directory.Exists(ctx.EfcptOutputDir))
                    Directory.Delete(ctx.EfcptOutputDir, recursive: true);
                return ctx;
            })
            .Then("efcpt directory does not exist", ctx => !Directory.Exists(ctx.EfcptOutputDir))
            .When("run dotnet clean", ExecuteDotNetClean)
            .Then("clean command succeeds", r => r.ExitCode == 0)
            .And("efcpt directory still does not exist", r => !r.EfcptDirExistsAfter)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("dotnet clean outputs message about cleaning efcpt")]
    [Fact]
    public Task Dotnet_clean_outputs_message_about_cleaning_efcpt()
        => Given("project with efcpt output directory", SetupProjectWithEfcptOutput)
            .When("run dotnet clean with normal verbosity", ctx =>
            {
                var efcptDirExistedBefore = Directory.Exists(ctx.EfcptOutputDir);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = TestPaths.DotNetExe,
                    Arguments = "clean -v normal",
                    WorkingDirectory = ctx.AppDir,
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
                var efcptDirExistsAfter = Directory.Exists(ctx.EfcptOutputDir);

                return new CleanResult(ctx, process.ExitCode, output, efcptDirExistedBefore, efcptDirExistsAfter);
            })
            .Then("clean command succeeds", r => r.ExitCode == 0)
            .And("output contains efcpt cleaning message", r =>
                r.Output.Contains("Cleaning efcpt output", StringComparison.OrdinalIgnoreCase) ||
                r.Output.Contains("efcpt", StringComparison.OrdinalIgnoreCase))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
}
