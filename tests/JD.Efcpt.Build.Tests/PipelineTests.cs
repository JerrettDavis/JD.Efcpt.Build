using Microsoft.Build.Utilities;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using Xunit;

namespace JD.Efcpt.Build.Tests;

public class PipelineTests
{
    [Fact]
    public void Generates_and_renames_when_fingerprint_changes()
    {
        using var folder = new TestFolder();

        var appDir = folder.CreateDir("SampleApp");
        var dbDir = folder.CreateDir("SampleDatabase");
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleApp"), appDir);
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleDatabase"), dbDir);

        var sqlproj = Path.Combine(dbDir, "Sample.Database.sqlproj");
        var csproj = Path.Combine(appDir, "Sample.App.csproj");
        var dacpac = Path.Combine(dbDir, "bin", "Debug", "Sample.Database.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        File.WriteAllText(dacpac, "dacpac");
        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow);

        var outputDir = Path.Combine(appDir, "obj", "efcpt");
        var generatedDir = Path.Combine(outputDir, "Generated");
        var engine = new TestBuildEngine();

        var resolve = new ResolveSqlProjAndInputs
        {
            BuildEngine = engine,
            ProjectFullPath = csproj,
            ProjectDirectory = appDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "SampleDatabase", "Sample.Database.sqlproj"))],
            OutputDir = outputDir,
            SolutionDir = folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };
        Assert.True(resolve.Execute());

        var ensure = new EnsureDacpacBuilt
        {
            BuildEngine = engine,
            SqlProjPath = resolve.SqlProjPath,
            Configuration = "Debug",
            DotNetExe = "/bin/false"
        };
        Assert.True(ensure.Execute());

        var stage = new StageEfcptInputs
        {
            BuildEngine = engine,
            OutputDir = outputDir,
            ConfigPath = resolve.ResolvedConfigPath,
            RenamingPath = resolve.ResolvedRenamingPath,
            TemplateDir = resolve.ResolvedTemplateDir
        };
        Assert.True(stage.Execute());

        var fingerprintFile = Path.Combine(outputDir, "fingerprint.txt");
        var fingerprint = new ComputeFingerprint
        {
            BuildEngine = engine,
            DacpacPath = ensure.DacpacPath,
            ConfigPath = stage.StagedConfigPath,
            RenamingPath = stage.StagedRenamingPath,
            TemplateDir = stage.StagedTemplateDir,
            FingerprintFile = fingerprintFile
        };
        Assert.True(fingerprint.Execute());
        Assert.Equal("true", fingerprint.HasChanged);

        TestScripts.CreateFakeEfcpt(folder);

        Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "1");

        var run = new RunEfcpt
        {
            BuildEngine = engine,
            ToolMode = "custom",
            ToolRestore = "false",
            WorkingDirectory = appDir,
            DacpacPath = ensure.DacpacPath,
            ConfigPath = stage.StagedConfigPath,
            RenamingPath = stage.StagedRenamingPath,
            TemplateDir = stage.StagedTemplateDir,
            OutputDir = generatedDir
        };
        Assert.True(run.Execute(), TestOutput.DescribeErrors(engine));

        var rename = new RenameGeneratedFiles
        {
            BuildEngine = engine,
            GeneratedDir = generatedDir
        };
        Assert.True(rename.Execute());

        var generated = Directory.GetFiles(generatedDir, "*.g.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(generated);

        var combined = string.Join(Environment.NewLine, generated.Select(File.ReadAllText));
        Assert.Contains("generated from", combined);

        var fingerprint2 = new ComputeFingerprint
        {
            BuildEngine = engine,
            DacpacPath = ensure.DacpacPath,
            ConfigPath = stage.StagedConfigPath,
            RenamingPath = stage.StagedRenamingPath,
            TemplateDir = stage.StagedTemplateDir,
            FingerprintFile = fingerprintFile
        };
        Assert.True(fingerprint2.Execute());
        Assert.Equal("false", fingerprint2.HasChanged);
    }

    [Fact]
    public void End_to_end_generates_dacpac_and_runs_real_efcpt()
    {
        using var folder = new TestFolder();

        var appDir = folder.CreateDir("SampleApp");
        var dbDir = folder.CreateDir("SampleDatabase");
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleApp"), appDir);
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleDatabase"), dbDir);

        Path.Combine(dbDir, "Sample.Database.sqlproj");
        var csproj = Path.Combine(appDir, "Sample.App.csproj");

        var outputDir = Path.Combine(appDir, "obj", "efcpt");
        var generatedDir = Path.Combine(outputDir, "Generated");
        var engine = new TestBuildEngine();

        var resolve = new ResolveSqlProjAndInputs
        {
            BuildEngine = engine,
            ProjectFullPath = csproj,
            ProjectDirectory = appDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "SampleDatabase", "Sample.Database.sqlproj"))],
            OutputDir = outputDir,
            SolutionDir = folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };
        Assert.True(resolve.Execute(), TestOutput.DescribeErrors(engine));

        var ensure = new EnsureDacpacBuilt
        {
            BuildEngine = engine,
            SqlProjPath = resolve.SqlProjPath,
            Configuration = "Debug",
            DotNetExe = TestPaths.DotNetExe
        };
        Assert.True(ensure.Execute(), TestOutput.DescribeErrors(engine));

        var stage = new StageEfcptInputs
        {
            BuildEngine = engine,
            OutputDir = outputDir,
            ConfigPath = resolve.ResolvedConfigPath,
            RenamingPath = resolve.ResolvedRenamingPath,
            TemplateDir = resolve.ResolvedTemplateDir
        };
        Assert.True(stage.Execute(), TestOutput.DescribeErrors(engine));

        var fingerprintFile = Path.Combine(outputDir, "fingerprint.txt");
        var fingerprint = new ComputeFingerprint
        {
            BuildEngine = engine,
            DacpacPath = ensure.DacpacPath,
            ConfigPath = stage.StagedConfigPath,
            RenamingPath = stage.StagedRenamingPath,
            TemplateDir = stage.StagedTemplateDir,
            FingerprintFile = fingerprintFile
        };
        Assert.True(fingerprint.Execute(), TestOutput.DescribeErrors(engine));

        var run = new RunEfcpt
        {
            BuildEngine = engine,
            ToolMode = "dotnet",
            ToolRestore = "false",
            WorkingDirectory = appDir,
            DacpacPath = ensure.DacpacPath,
            ConfigPath = stage.StagedConfigPath,
            RenamingPath = stage.StagedRenamingPath,
            TemplateDir = stage.StagedTemplateDir,
            OutputDir = generatedDir
        };
        Assert.True(run.Execute(), TestOutput.DescribeErrors(engine));

        // Locate generated model files; efcpt writes into a Models subfolder by default
        var generatedRoot = Path.Combine(appDir, "obj", "efcpt", "Generated", "Models");
        if (!Directory.Exists(generatedRoot))
        {
            // fall back to the root Generated folder if Models does not exist
            generatedRoot = Path.Combine(appDir, "obj", "efcpt", "Generated");
        }

        Assert.True(Directory.Exists(generatedRoot), $"Expected generated output directory to exist: {generatedRoot}");

        var generatedFiles = Directory.GetFiles(generatedRoot, "*.cs", SearchOption.AllDirectories);
        if (generatedFiles.Length == 0)
        {
            var allFiles = Directory.GetFiles(Path.Combine(appDir, "obj", "efcpt"), "*.*", SearchOption.AllDirectories);
            var message = $"No generated .cs files found under '{generatedRoot}'. Files present under obj/efcpt: {string.Join(", ", allFiles)}";
            Assert.Fail(message);
        }

        var combined = string.Join(Environment.NewLine, generatedFiles.Select(File.ReadAllText));

        // Verify expected DbSets / entities from our sample schemas/tables
        Assert.Contains("DbSet<Blog>", combined);
        Assert.Contains("DbSet<Post>", combined);
        Assert.Contains("DbSet<Account>", combined);
        Assert.Contains("DbSet<Upload>", combined);
    }
}
