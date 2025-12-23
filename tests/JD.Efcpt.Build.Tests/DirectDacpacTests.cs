using System.IO.Compression;
using System.Text;
using Microsoft.Build.Utilities;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for direct DACPAC loading functionality.
/// When EfcptDacpac is set in MSBuild, the pipeline should use that DACPAC directly
/// without building the .sqlproj file.
/// </summary>
/// <remarks>
/// <para>
/// The direct DACPAC feature works as follows in the MSBuild targets:
/// <list type="number">
///   <item>EfcptResolveInputs runs normally (resolves config, renaming, templates)</item>
///   <item>EfcptUseDirectDacpac sets _EfcptDacpacPath from EfcptDacpac property</item>
///   <item>EfcptEnsureDacpac is skipped (condition: _EfcptUseDirectDacpac != true)</item>
///   <item>Pipeline continues using the direct DACPAC path</item>
/// </list>
/// </para>
/// <para>
/// These tests simulate this behavior by:
/// <list type="bullet">
///   <item>Setting up a test environment with both a .sqlproj (for resolve) and a pre-built DACPAC</item>
///   <item>Resolving inputs normally</item>
///   <item>Skipping EnsureDacpacBuilt task</item>
///   <item>Using the pre-built DACPAC path directly in subsequent pipeline steps</item>
/// </list>
/// </para>
/// </remarks>
[Feature("Direct DACPAC loading: use pre-built DACPAC without building .sqlproj")]
[Collection(nameof(AssemblySetup))]
public sealed class DirectDacpacTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record DirectDacpacState(
        TestFolder Folder,
        string AppDir,
        string DbDir,
        string DirectDacpacPath,
        string OutputDir,
        string GeneratedDir,
        TestBuildEngine Engine);

    private sealed record ResolveResult(
        DirectDacpacState State,
        ResolveSqlProjAndInputs Task);

    private sealed record StageResult(
        ResolveResult Resolve,
        StageEfcptInputs Task,
        string DirectDacpacPath);

    private sealed record FingerprintResult(
        StageResult Stage,
        ComputeFingerprint Task);

    private sealed record RunResult(
        FingerprintResult Fingerprint,
        RunEfcpt Task);

    private sealed record RenameResult(
        RunResult Run,
        RenameGeneratedFiles Task,
        string[] GeneratedFiles);

    /// <summary>
    /// Creates a mock DACPAC file (ZIP archive with model.xml) containing a modified schema.
    /// Used to simulate DACPAC changes without rebuilding the actual project.
    /// </summary>
    private static void CreateModifiedMockDacpac(string dacpacPath, string schemaContent)
    {
        var modelXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="C:\\builds\\modified.dacpac" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[{schemaContent}]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        // Delete existing file and create new one
        if (File.Exists(dacpacPath))
            File.Delete(dacpacPath);

        using var archive = ZipFile.Open(dacpacPath, ZipArchiveMode.Create);
        var modelEntry = archive.CreateEntry("model.xml");
        using var stream = modelEntry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(modelXml);
    }

    /// <summary>
    /// Sets up a test folder with both a .sqlproj reference (for resolve to succeed)
    /// and a pre-built DACPAC file that will be used directly instead of building.
    /// This simulates the scenario where a user has EfcptDacpac set to a pre-built DACPAC.
    /// </summary>
    private static DirectDacpacState SetupWithPrebuiltDacpac()
    {
        var folder = new TestFolder();
        var appDir = folder.CreateDir("SampleApp");
        var dbDir = folder.CreateDir("SampleDatabase");
        var dacpacDir = folder.CreateDir("PrebuiltDacpacs");

        // Copy sample app and database project (needed for resolve to succeed)
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleApp"), appDir);
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleDatabase"), dbDir);

        // Create a pre-built DACPAC file (this is what EfcptDacpac would point to)
        var directDacpacPath = Path.Combine(dacpacDir, "MyPrebuiltDatabase.dacpac");

        // Build the sample database to get a valid DACPAC to copy
        var sqlproj = Directory.GetFiles(dbDir, "*.sqlproj").First();
        BuildDacpacFromProject(sqlproj, directDacpacPath);

        var outputDir = Path.Combine(appDir, "obj", "efcpt");
        var generatedDir = Path.Combine(outputDir, "Generated");
        var engine = new TestBuildEngine();

        return new DirectDacpacState(folder, appDir, dbDir, directDacpacPath, outputDir, generatedDir, engine);
    }

    private static void BuildDacpacFromProject(string sqlprojPath, string targetDacpacPath)
    {
        var dbProjectDir = Path.GetDirectoryName(sqlprojPath)!;

        // Build the database project
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{sqlprojPath}\" -c Debug",
            WorkingDirectory = dbProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        var process = System.Diagnostics.Process.Start(psi)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Failed to build DACPAC: {stderr}");
        }

        // Find and copy the built DACPAC
        var builtDacpac = Directory.GetFiles(dbProjectDir, "*.dacpac", SearchOption.AllDirectories).FirstOrDefault();
        if (builtDacpac == null)
            throw new InvalidOperationException("DACPAC was not created");

        Directory.CreateDirectory(Path.GetDirectoryName(targetDacpacPath)!);
        File.Copy(builtDacpac, targetDacpacPath, overwrite: true);
    }

    private static ResolveResult ResolveInputs(DirectDacpacState state)
    {
        var csproj = Path.Combine(state.AppDir, "Sample.App.csproj");

        // Provide a SqlProj reference so resolve succeeds (simulating normal project setup)
        // Even when using direct DACPAC mode, the resolve step still needs to find config/renaming/templates
        var resolve = new ResolveSqlProjAndInputs
        {
            BuildEngine = state.Engine,
            ProjectFullPath = csproj,
            ProjectDirectory = state.AppDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "SampleDatabase", "Sample.Database.sqlproj"))],
            OutputDir = state.OutputDir,
            SolutionDir = state.Folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var success = resolve.Execute();
        return success
            ? new ResolveResult(state, resolve)
            : throw new InvalidOperationException($"Resolve failed: {TestOutput.DescribeErrors(state.Engine)}");
    }

    /// <summary>
    /// Stage inputs using the direct DACPAC path (bypassing EnsureDacpacBuilt).
    /// This simulates the MSBuild target behavior where EfcptUseDirectDacpac sets
    /// _EfcptDacpacPath directly from EfcptDacpac property.
    /// </summary>
    private static StageResult StageInputsWithDirectDacpac(ResolveResult resolve)
    {
        var stage = new StageEfcptInputs
        {
            BuildEngine = resolve.State.Engine,
            OutputDir = resolve.State.OutputDir,
            ProjectDirectory = resolve.State.AppDir,
            ConfigPath = resolve.Task.ResolvedConfigPath,
            RenamingPath = resolve.Task.ResolvedRenamingPath,
            TemplateDir = resolve.Task.ResolvedTemplateDir
        };

        var success = stage.Execute();
        return success
            ? new StageResult(resolve, stage, resolve.State.DirectDacpacPath)
            : throw new InvalidOperationException($"Stage failed: {TestOutput.DescribeErrors(resolve.State.Engine)}");
    }

    private static FingerprintResult ComputeFingerprintWithDirectDacpac(StageResult stage)
    {
        var fingerprintFile = Path.Combine(stage.Resolve.State.OutputDir, "fingerprint.txt");

        // Use the direct DACPAC path instead of a built one
        var fingerprint = new ComputeFingerprint
        {
            BuildEngine = stage.Resolve.State.Engine,
            DacpacPath = stage.DirectDacpacPath, // Using direct DACPAC path
            ConfigPath = stage.Task.StagedConfigPath,
            RenamingPath = stage.Task.StagedRenamingPath,
            TemplateDir = stage.Task.StagedTemplateDir,
            FingerprintFile = fingerprintFile
        };

        var success = fingerprint.Execute();
        return success
            ? new FingerprintResult(stage, fingerprint)
            : throw new InvalidOperationException($"Fingerprint failed: {TestOutput.DescribeErrors(stage.Resolve.State.Engine)}");
    }

    private static RunResult RunEfcptWithDirectDacpac(FingerprintResult fingerprint, bool useFake = true)
    {
        var initialFakeEfcpt = Environment.GetEnvironmentVariable("EFCPT_FAKE_EFCPT");
        if (useFake)
            Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "1");

        var run = new RunEfcpt
        {
            BuildEngine = fingerprint.Stage.Resolve.State.Engine,
            ToolMode = useFake ? "custom" : "dotnet",
            ToolRestore = "false",
            WorkingDirectory = fingerprint.Stage.Resolve.State.AppDir,
            DacpacPath = fingerprint.Stage.DirectDacpacPath, // Using direct DACPAC path
            ConfigPath = fingerprint.Stage.Task.StagedConfigPath,
            RenamingPath = fingerprint.Stage.Task.StagedRenamingPath,
            TemplateDir = fingerprint.Stage.Task.StagedTemplateDir,
            OutputDir = fingerprint.Stage.Resolve.State.GeneratedDir
        };

        var success = run.Execute();
        Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", initialFakeEfcpt);

        return success
            ? new RunResult(fingerprint, run)
            : throw new InvalidOperationException($"Run efcpt failed: {TestOutput.DescribeErrors(fingerprint.Stage.Resolve.State.Engine)}");
    }

    private static RenameResult RenameFiles(RunResult run)
    {
        var rename = new RenameGeneratedFiles
        {
            BuildEngine = run.Fingerprint.Stage.Resolve.State.Engine,
            GeneratedDir = run.Fingerprint.Stage.Resolve.State.GeneratedDir
        };

        var success = rename.Execute();
        if (!success)
            throw new InvalidOperationException($"Rename failed: {TestOutput.DescribeErrors(run.Fingerprint.Stage.Resolve.State.Engine)}");

        var generatedFiles = Directory.GetFiles(
            run.Fingerprint.Stage.Resolve.State.GeneratedDir,
            "*.g.cs",
            SearchOption.AllDirectories);

        return new RenameResult(run, rename, generatedFiles);
    }

    [Scenario("Pipeline succeeds when using a pre-built DACPAC directly (fake efcpt)")]
    [Fact]
    public async Task Pipeline_succeeds_with_direct_dacpac_fake_efcpt()
    {
        await Given("pre-built DACPAC file", SetupWithPrebuiltDacpac)
            .When("resolve inputs", ResolveInputs)
            .Then("resolve succeeds", r => r.Task != null)
            // Note: SqlProjPath may or may not be set - in direct DACPAC mode it's not required
            .When("stage inputs with direct DACPAC", StageInputsWithDirectDacpac)
            .Then("staged files exist", r =>
                File.Exists(r.Task.StagedConfigPath) &&
                File.Exists(r.Task.StagedRenamingPath) &&
                Directory.Exists(r.Task.StagedTemplateDir))
            .And("direct DACPAC path is valid", r => File.Exists(r.DirectDacpacPath))
            .When("compute fingerprint with direct DACPAC", ComputeFingerprintWithDirectDacpac)
            .Then("fingerprint is computed", r => !string.IsNullOrEmpty(r.Task.Fingerprint))
            .And("fingerprint has changed on first run", r => r.Task.HasChanged == "true")
            .When("run efcpt with direct DACPAC (fake)", r => RunEfcptWithDirectDacpac(r, useFake: true))
            .When("rename generated files", RenameFiles)
            .Then("generated files exist", r => r.GeneratedFiles.Length > 0)
            .And("files contain expected content", r =>
            {
                var combined = string.Join(Environment.NewLine, r.GeneratedFiles.Select(File.ReadAllText));
                return combined.Contains("generated from");
            })
            .Finally(r => r.Run.Fingerprint.Stage.Resolve.State.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Pipeline succeeds with real efcpt using direct DACPAC")]
    [Fact]
    public async Task Pipeline_succeeds_with_direct_dacpac_real_efcpt()
    {
        await Given("pre-built DACPAC file", SetupWithPrebuiltDacpac)
            .When("resolve inputs", ResolveInputs)
            .Then("resolve succeeds", r => r.Task != null)
            .When("stage inputs with direct DACPAC", StageInputsWithDirectDacpac)
            .Then("staged files exist", r =>
                File.Exists(r.Task.StagedConfigPath) &&
                File.Exists(r.Task.StagedRenamingPath) &&
                Directory.Exists(r.Task.StagedTemplateDir))
            .When("compute fingerprint with direct DACPAC", ComputeFingerprintWithDirectDacpac)
            .Then("fingerprint file exists", r =>
                File.Exists(Path.Combine(r.Stage.Resolve.State.OutputDir, "fingerprint.txt")))
            .When("run efcpt with direct DACPAC (real)", r => RunEfcptWithDirectDacpac(r, useFake: false))
            .Then("output directory exists", r =>
            {
                var generatedDir = r.Fingerprint.Stage.Resolve.State.GeneratedDir;
                var modelsDir = Path.Combine(generatedDir, "Models");
                return Directory.Exists(modelsDir) || Directory.Exists(generatedDir);
            })
            .And("generated files contain expected DbSets", r =>
            {
                var generatedDir = r.Fingerprint.Stage.Resolve.State.GeneratedDir;
                var generatedRoot = Path.Combine(generatedDir, "Models");
                if (!Directory.Exists(generatedRoot))
                    generatedRoot = generatedDir;

                var generatedFiles = Directory.GetFiles(generatedRoot, "*.cs", SearchOption.AllDirectories);
                if (generatedFiles.Length == 0)
                    return false;

                var combined = string.Join(Environment.NewLine, generatedFiles.Select(File.ReadAllText));
                // Sample database should have Blog, Post, Account, Upload tables
                return combined.Contains("DbSet<Blog>") &&
                       combined.Contains("DbSet<Post>") &&
                       combined.Contains("DbSet<Account>") &&
                       combined.Contains("DbSet<Upload>");
            })
            .Finally(r => r.Fingerprint.Stage.Resolve.State.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint changes when direct DACPAC content changes")]
    [Fact]
    public async Task Fingerprint_changes_when_direct_dacpac_changes()
    {
        await Given("pre-built DACPAC file", SetupWithPrebuiltDacpac)
            .When("resolve inputs", ResolveInputs)
            .When("stage inputs with direct DACPAC", StageInputsWithDirectDacpac)
            .When("compute fingerprint", ComputeFingerprintWithDirectDacpac)
            .Then("fingerprint is computed", r => !string.IsNullOrEmpty(r.Task.Fingerprint))
            .When("modify DACPAC and recompute fingerprint", r =>
            {
                // Write the first fingerprint
                var firstFingerprint = r.Task.Fingerprint;

                // Replace the DACPAC with a mock containing different schema
                // (simulates rebuilding with schema changes)
                CreateModifiedMockDacpac(r.Stage.DirectDacpacPath, "ModifiedTable");

                // Recompute fingerprint
                var fingerprintFile = Path.Combine(r.Stage.Resolve.State.OutputDir, "fingerprint.txt");
                var fingerprint2 = new ComputeFingerprint
                {
                    BuildEngine = r.Stage.Resolve.State.Engine,
                    DacpacPath = r.Stage.DirectDacpacPath,
                    ConfigPath = r.Stage.Task.StagedConfigPath,
                    RenamingPath = r.Stage.Task.StagedRenamingPath,
                    TemplateDir = r.Stage.Task.StagedTemplateDir,
                    FingerprintFile = fingerprintFile
                };
                fingerprint2.Execute();

                return (FirstFingerprint: firstFingerprint, SecondFingerprint: fingerprint2.Fingerprint,
                        HasChanged: fingerprint2.HasChanged, Folder: r.Stage.Resolve.State.Folder);
            })
            .Then("fingerprints are different", t => t.FirstFingerprint != t.SecondFingerprint)
            .And("has changed is true", t => t.HasChanged == "true")
            .Finally(t => t.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint unchanged when direct DACPAC is unchanged")]
    [Fact]
    public async Task Fingerprint_unchanged_when_direct_dacpac_unchanged()
    {
        await Given("pre-built DACPAC file", SetupWithPrebuiltDacpac)
            .When("resolve inputs", ResolveInputs)
            .When("stage inputs with direct DACPAC", StageInputsWithDirectDacpac)
            .When("compute fingerprint", ComputeFingerprintWithDirectDacpac)
            .Then("fingerprint has changed is true (first run)", r => r.Task.HasChanged == "true")
            .When("compute fingerprint again without changes", r =>
            {
                var firstFingerprint = r.Task.Fingerprint;
                var fingerprintFile = Path.Combine(r.Stage.Resolve.State.OutputDir, "fingerprint.txt");

                // Write fingerprint to cache file to simulate completed generation
                File.WriteAllText(fingerprintFile, firstFingerprint);

                var fingerprint2 = new ComputeFingerprint
                {
                    BuildEngine = r.Stage.Resolve.State.Engine,
                    DacpacPath = r.Stage.DirectDacpacPath,
                    ConfigPath = r.Stage.Task.StagedConfigPath,
                    RenamingPath = r.Stage.Task.StagedRenamingPath,
                    TemplateDir = r.Stage.Task.StagedTemplateDir,
                    FingerprintFile = fingerprintFile
                };
                fingerprint2.Execute();

                return (r, fingerprint2);
            })
            .Then("fingerprint has changed is false", t => t.Item2.HasChanged == "false")
            .Finally(t => t.r.Stage.Resolve.State.Folder.Dispose())
            .AssertPassed();
    }
}
