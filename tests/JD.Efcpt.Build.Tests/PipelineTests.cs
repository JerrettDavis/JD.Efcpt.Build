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

[Feature("Full pipeline: resolve, dacpac, stage, fingerprint, generate, rename")]
[Collection(nameof(AssemblySetup))]
public sealed class PipelineTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record PipelineState(
        TestFolder Folder,
        string AppDir,
        string DbDir,
        string OutputDir,
        string GeneratedDir,
        TestBuildEngine Engine);

    private sealed record ResolveResult(
        PipelineState State,
        ResolveSqlProjAndInputs Task);

    private sealed record EnsureResult(
        ResolveResult Resolve,
        EnsureDacpacBuilt Task);

    private sealed record StageResult(
        EnsureResult Ensure,
        StageEfcptInputs Task);

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

    private static PipelineState SetupFolders()
    {
        var folder = new TestFolder();
        var appDir = folder.CreateDir("SampleApp");
        var dbDir = folder.CreateDir("SampleDatabase");
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleApp"), appDir);
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleDatabase"), dbDir);

        var outputDir = Path.Combine(appDir, "obj", "efcpt");
        var generatedDir = Path.Combine(outputDir, "Generated");
        var engine = new TestBuildEngine();

        return new PipelineState(folder, appDir, dbDir, outputDir, generatedDir, engine);
    }

    private static PipelineState SetupWithExistingDacpac(PipelineState state)
    {
        var sqlproj = Path.Combine(state.DbDir, "Sample.Database.sqlproj");
        var dacpac = Path.Combine(state.DbDir, "bin", "Debug", "Sample.Database.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        CreateMockDacpac(dacpac, "SampleTable");
        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow);
        return state;
    }

    /// <summary>
    /// Creates a mock DACPAC file (ZIP archive with model.xml).
    /// </summary>
    private static void CreateMockDacpac(string dacpacPath, string schemaContent)
    {
        var modelXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="C:\\builds\\{Path.GetFileName(dacpacPath)}" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[{schemaContent}]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        // Delete existing file if it exists (ZipArchiveMode.Create throws if file exists)
        if (File.Exists(dacpacPath))
            File.Delete(dacpacPath);

        using var archive = ZipFile.Open(dacpacPath, ZipArchiveMode.Create);
        var modelEntry = archive.CreateEntry("model.xml");
        using var stream = modelEntry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(modelXml);
    }

    private static ResolveResult ResolveInputs(PipelineState state)
    {
        var csproj = Path.Combine(state.AppDir, "Sample.App.csproj");
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

    private static EnsureResult EnsureDacpac(ResolveResult resolve, bool useFakeBuild = true)
    {
        var initialFakeBuild = Environment.GetEnvironmentVariable("EFCPT_FAKE_BUILD");
        if (useFakeBuild)
            Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", "1");

        var ensure = new EnsureDacpacBuilt
        {
            BuildEngine = resolve.State.Engine,
            SqlProjPath = resolve.Task.SqlProjPath,
            Configuration = "Debug",
            DotNetExe = useFakeBuild ? "/bin/false" : TestPaths.DotNetExe
        };

        Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", initialFakeBuild);

        var success = ensure.Execute();
        return success
            ? new EnsureResult(resolve, ensure)
            : throw new InvalidOperationException($"Ensure dacpac failed: {TestOutput.DescribeErrors(resolve.State.Engine)}");
    }

    private static StageResult StageInputs(EnsureResult ensure)
    {
        var stage = new StageEfcptInputs
        {
            BuildEngine = ensure.Resolve.State.Engine,
            OutputDir = ensure.Resolve.State.OutputDir,
            ConfigPath = ensure.Resolve.Task.ResolvedConfigPath,
            RenamingPath = ensure.Resolve.Task.ResolvedRenamingPath,
            TemplateDir = ensure.Resolve.Task.ResolvedTemplateDir
        };

        var success = stage.Execute();
        return success
            ? new StageResult(ensure, stage)
            : throw new InvalidOperationException($"Stage failed: {TestOutput.DescribeErrors(ensure.Resolve.State.Engine)}");
    }

    private static FingerprintResult ComputeFingerprintHash(StageResult stage)
    {
        var fingerprintFile = Path.Combine(stage.Ensure.Resolve.State.OutputDir, "fingerprint.txt");
        var fingerprint = new ComputeFingerprint
        {
            BuildEngine = stage.Ensure.Resolve.State.Engine,
            DacpacPath = stage.Ensure.Task.DacpacPath,
            ConfigPath = stage.Task.StagedConfigPath,
            RenamingPath = stage.Task.StagedRenamingPath,
            TemplateDir = stage.Task.StagedTemplateDir,
            FingerprintFile = fingerprintFile
        };

        var success = fingerprint.Execute();
        return success
            ? new FingerprintResult(stage, fingerprint)
            : throw new InvalidOperationException($"Fingerprint failed: {TestOutput.DescribeErrors(stage.Ensure.Resolve.State.Engine)}");
    }

    private static RunResult RunEfcptTool(FingerprintResult fingerprint, bool useFake = true)
    {
        var initialFakeEfcpt = Environment.GetEnvironmentVariable("EFCPT_FAKE_EFCPT");
        if (useFake)
            Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "1");

        var run = new RunEfcpt
        {
            BuildEngine = fingerprint.Stage.Ensure.Resolve.State.Engine,
            ToolMode = useFake ? "custom" : "dotnet",
            ToolRestore = "false",
            WorkingDirectory = fingerprint.Stage.Ensure.Resolve.State.AppDir,
            DacpacPath = fingerprint.Stage.Ensure.Task.DacpacPath,
            ConfigPath = fingerprint.Stage.Task.StagedConfigPath,
            RenamingPath = fingerprint.Stage.Task.StagedRenamingPath,
            TemplateDir = fingerprint.Stage.Task.StagedTemplateDir,
            OutputDir = fingerprint.Stage.Ensure.Resolve.State.GeneratedDir
        };

        var success = run.Execute();
        Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", initialFakeEfcpt);

        return success
            ? new RunResult(fingerprint, run)
            : throw new InvalidOperationException($"Run efcpt failed: {TestOutput.DescribeErrors(fingerprint.Stage.Ensure.Resolve.State.Engine)}");
    }

    private static RenameResult RenameFiles(RunResult run)
    {
        var rename = new RenameGeneratedFiles
        {
            BuildEngine = run.Fingerprint.Stage.Ensure.Resolve.State.Engine,
            GeneratedDir = run.Fingerprint.Stage.Ensure.Resolve.State.GeneratedDir
        };

        var success = rename.Execute();
        if (!success)
            throw new InvalidOperationException($"Rename failed: {TestOutput.DescribeErrors(run.Fingerprint.Stage.Ensure.Resolve.State.Engine)}");

        var generatedFiles = Directory.GetFiles(
            run.Fingerprint.Stage.Ensure.Resolve.State.GeneratedDir,
            "*.g.cs",
            SearchOption.AllDirectories);

        return new RenameResult(run, rename, generatedFiles);
    }
    
    [Scenario("Pipeline generates files when fingerprint changes and marks fingerprint unchanged on second run")]
    [Fact]
    public async Task Generates_and_renames_when_fingerprint_changes()
    {
        await Given("folders with existing dacpac", () => SetupWithExistingDacpac(SetupFolders()))
            .When("resolve inputs", ResolveInputs)
            .Then("resolve succeeds", r => r?.Task.SqlProjPath != null)
            .When("ensure dacpac", r => EnsureDacpac(r))
            .Then("dacpac exists", r => File.Exists(r.Task.DacpacPath))
            .When("stage inputs", StageInputs)
            .Then("staged files exist", r =>
                File.Exists(r.Task.StagedConfigPath) &&
                File.Exists(r.Task.StagedRenamingPath) &&
                Directory.Exists(r.Task.StagedTemplateDir))
            .When("compute fingerprint", ComputeFingerprintHash)
            .Then("fingerprint changed is true", r => r.Task.HasChanged == "true")
            .When("run efcpt (fake)", r => RunEfcptTool(r, useFake: true))
            .When("rename generated files", RenameFiles)
            .Then("generated files exist", r => r.GeneratedFiles.Length > 0)
            .And("files contain expected content", r =>
            {
                var combined = string.Join(Environment.NewLine, r.GeneratedFiles.Select(File.ReadAllText));
                return combined.Contains("generated from");
            })
            .When("compute fingerprint again", r =>
            {
                var fingerprintFile = Path.Combine(r.Run.Fingerprint.Stage.Ensure.Resolve.State.OutputDir, "fingerprint.txt");
                var fingerprint2 = new ComputeFingerprint
                {
                    BuildEngine = r.Run.Fingerprint.Stage.Ensure.Resolve.State.Engine,
                    DacpacPath = r.Run.Fingerprint.Stage.Ensure.Task.DacpacPath,
                    ConfigPath = r.Run.Fingerprint.Stage.Task.StagedConfigPath,
                    RenamingPath = r.Run.Fingerprint.Stage.Task.StagedRenamingPath,
                    TemplateDir = r.Run.Fingerprint.Stage.Task.StagedTemplateDir,
                    FingerprintFile = fingerprintFile
                };
                fingerprint2.Execute();
                return (r, fingerprint2);
            })
            .Then("fingerprint changed is false", t => t.Item2.HasChanged == "false")
            .Finally(t => t.r.Run.Fingerprint.Stage.Ensure.Resolve.State.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("End-to-end builds real dacpac and runs real efcpt CLI")]
    [Fact]
    public Task End_to_end_generates_dacpac_and_runs_real_efcpt()
        => Given("folders setup", SetupFolders)
            .When("resolve inputs", ResolveInputs)
            .Then("resolve succeeds", r => r.Task.SqlProjPath != null)
            .When("ensure dacpac (real build)", r => EnsureDacpac(r, useFakeBuild: false))
            .Then("dacpac file exists", r => File.Exists(r.Task.DacpacPath))
            .When("stage inputs", StageInputs)
            .Then("staged files exist", r =>
                File.Exists(r.Task.StagedConfigPath) &&
                File.Exists(r.Task.StagedRenamingPath) &&
                Directory.Exists(r.Task.StagedTemplateDir))
            .When("compute fingerprint", ComputeFingerprintHash)
            .Then("fingerprint file exists", r => File.Exists(Path.Combine(r.Stage.Ensure.Resolve.State.OutputDir, "fingerprint.txt")))
            .When("run efcpt (real)", r => RunEfcptTool(r, useFake: false))
            .Then("output directory exists", r =>
            {
                var generatedDir = r.Fingerprint.Stage.Ensure.Resolve.State.GeneratedDir;
                var modelsDir = Path.Combine(generatedDir, "Models");
                return Directory.Exists(modelsDir) || Directory.Exists(generatedDir);
            })
            .And("generated files contain expected DbSets", r =>
            {
                var generatedDir = r.Fingerprint.Stage.Ensure.Resolve.State.GeneratedDir;
                var generatedRoot = Path.Combine(generatedDir, "Models");
                if (!Directory.Exists(generatedRoot))
                    generatedRoot = generatedDir;

                var generatedFiles = Directory.GetFiles(generatedRoot, "*.cs", SearchOption.AllDirectories);
                if (generatedFiles.Length == 0)
                    return false;

                var combined = string.Join(Environment.NewLine, generatedFiles.Select(File.ReadAllText));
                return combined.Contains("DbSet<Blog>") &&
                       combined.Contains("DbSet<Post>") &&
                       combined.Contains("DbSet<Account>") &&
                       combined.Contains("DbSet<Upload>");
            })
            .Finally(r => r.Fingerprint.Stage.Ensure.Resolve.State.Folder.Dispose())
            .AssertPassed();
}
