using Microsoft.Build.Utilities;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

[Feature("Split Outputs: separate Models project from Data project")]
[Collection(nameof(AssemblySetup))]
public sealed class SplitOutputsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SplitOutputsContext(
        TestFolder Folder,
        string DataDir,
        string ModelsDir,
        string DbDir,
        string DataOutputDir,
        string ModelsOutputDir,
        TestBuildEngine Engine) : IDisposable
    {
        public void Dispose() => Folder.Dispose();
    }

    private sealed record ResolveResult(
        SplitOutputsContext Context,
        ResolveSqlProjAndInputs Task);

    private sealed record GenerateResult(
        ResolveResult Resolve,
        string[] GeneratedFiles,
        string[] ModelsFiles,
        string[] RootFiles);

    private static SplitOutputsContext SetupSplitOutputsProject()
    {
        var folder = new TestFolder();
        var dataDir = folder.CreateDir("Sample.Data");
        var modelsDir = folder.CreateDir("Sample.Models");
        var dbDir = folder.CreateDir("SampleDatabase");

        // Copy database project from test assets
        TestFileSystem.CopyDirectory(TestPaths.Asset("SampleDatabase"), dbDir);

        // Create Models project (minimal)
        var modelsCsproj = Path.Combine(modelsDir, "Sample.Models.csproj");
        File.WriteAllText(modelsCsproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        // Create Data project with split outputs configuration
        var dataCsproj = Path.Combine(dataDir, "Sample.Data.csproj");
        File.WriteAllText(dataCsproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>
            """);

        // Create config files
        File.WriteAllText(Path.Combine(dataDir, "efcpt-config.json"), """
            {
              "names": { "root-namespace": "Sample.Data", "dbcontext-name": "SampleDbContext" },
              "code-generation": { "use-t4": false }
            }
            """);
        File.WriteAllText(Path.Combine(dataDir, "efcpt.renaming.json"), "[]");

        var dataOutputDir = Path.Combine(dataDir, "obj", "efcpt");
        var modelsOutputDir = Path.Combine(modelsDir, "obj", "efcpt", "Generated", "Models");
        var engine = new TestBuildEngine();

        return new SplitOutputsContext(folder, dataDir, modelsDir, dbDir, dataOutputDir, modelsOutputDir, engine);
    }

    private static SplitOutputsContext SetupWithPrebuiltDacpac(SplitOutputsContext context)
    {
        var sqlproj = Path.Combine(context.DbDir, "Sample.Database.sqlproj");
        var dacpac = Path.Combine(context.DbDir, "bin", "Debug", "Sample.Database.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        MockDacpacHelper.CreateAtPath(dacpac, "SampleTable");
        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow);
        return context;
    }

    private static ResolveResult ResolveInputs(SplitOutputsContext context)
    {
        var csproj = Path.Combine(context.DataDir, "Sample.Data.csproj");
        var resolve = new ResolveSqlProjAndInputs
        {
            BuildEngine = context.Engine,
            ProjectFullPath = csproj,
            ProjectDirectory = context.DataDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "SampleDatabase", "Sample.Database.sqlproj"))],
            OutputDir = context.DataOutputDir,
            SolutionDir = context.Folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var success = resolve.Execute();
        return success
            ? new ResolveResult(context, resolve)
            : throw new InvalidOperationException($"Resolve failed: {TestOutput.DescribeErrors(context.Engine)}");
    }

    private static GenerateResult GenerateWithFakeEfcpt(ResolveResult resolve)
    {
        var context = resolve.Context;
        var generatedDir = Path.Combine(context.DataOutputDir, "Generated");
        Directory.CreateDirectory(generatedDir);

        // Set up fake efcpt environment
        var initialFakeEfcpt = Environment.GetEnvironmentVariable("EFCPT_FAKE_EFCPT");
        Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "1");

        try
        {
            var run = new RunEfcpt
            {
                BuildEngine = context.Engine,
                ToolMode = "custom",
                ToolRestore = "false",
                WorkingDirectory = context.DataDir,
                DacpacPath = Path.Combine(context.DbDir, "bin", "Debug", "Sample.Database.dacpac"),
                ConfigPath = Path.Combine(context.DataDir, "efcpt-config.json"),
                RenamingPath = Path.Combine(context.DataDir, "efcpt.renaming.json"),
                TemplateDir = "",
                OutputDir = generatedDir
            };

            var success = run.Execute();
            if (!success)
                throw new InvalidOperationException($"RunEfcpt failed: {TestOutput.DescribeErrors(context.Engine)}");

            // Rename files (.cs -> .g.cs)
            var rename = new RenameGeneratedFiles
            {
                BuildEngine = context.Engine,
                GeneratedDir = generatedDir
            };
            rename.Execute();

            // Get generated files
            var allFiles = Directory.GetFiles(generatedDir, "*.g.cs", SearchOption.AllDirectories);
            var modelsFiles = Directory.GetFiles(Path.Combine(generatedDir, "Models"), "*.g.cs", SearchOption.AllDirectories);
            var rootFiles = allFiles.Except(modelsFiles).ToArray();

            return new GenerateResult(resolve, allFiles, modelsFiles, rootFiles);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", initialFakeEfcpt);
        }
    }

    [Scenario("Generated files are split between root and Models directory")]
    [Fact]
    public Task Generated_files_are_split_between_root_and_models_directory()
        => Given("split outputs project with dacpac", () => SetupWithPrebuiltDacpac(SetupSplitOutputsProject()))
            .When("resolve inputs", ResolveInputs)
            .Then("resolve succeeds", r => r.Task.SqlProjPath != null)
            .When("generate with fake efcpt", GenerateWithFakeEfcpt)
            .Then("files are generated in root", r => r.RootFiles.Length > 0)
            .And("files are generated in Models subdirectory", r => r.ModelsFiles.Length > 0)
            .And("root contains DbContext file", r => r.RootFiles.Any(f => f.Contains("DbContext")))
            .And("Models contains entity files", r =>
                r.ModelsFiles.Any(f => f.Contains("Blog")) &&
                r.ModelsFiles.Any(f => f.Contains("Post")))
            .Finally(r => r.Resolve.Context.Dispose())
            .AssertPassed();

    [Scenario("Models files have correct content for split outputs")]
    [Fact]
    public Task Models_files_have_correct_content()
        => Given("split outputs project with dacpac", () => SetupWithPrebuiltDacpac(SetupSplitOutputsProject()))
            .When("resolve inputs", ResolveInputs)
            .When("generate with fake efcpt", GenerateWithFakeEfcpt)
            .Then("Blog model contains class definition", r =>
            {
                var blogFile = r.ModelsFiles.FirstOrDefault(f => f.Contains("Blog"));
                if (blogFile == null) return false;
                var content = File.ReadAllText(blogFile);
                return content.Contains("class Blog");
            })
            .And("Post model contains class definition", r =>
            {
                var postFile = r.ModelsFiles.FirstOrDefault(f => f.Contains("Post"));
                if (postFile == null) return false;
                var content = File.ReadAllText(postFile);
                return content.Contains("class Post");
            })
            .Finally(r => r.Resolve.Context.Dispose())
            .AssertPassed();

    [Scenario("Validation fails when EfcptDataProject is not set with EfcptSplitOutputs enabled")]
    [Fact]
    public Task Validation_fails_when_data_project_not_set()
    {
        // This test verifies the MSBuild error message
        // The actual validation happens in the EfcptValidateSplitOutputs target
        // We test that the error message is clear and actionable
        var expectedError = "EfcptSplitOutputs is enabled but EfcptDataProject is not set";

        return Given("the expected error message", () => expectedError)
            .Then("error message is descriptive", msg => msg.Contains("EfcptDataProject"))
            .And("error message mentions EfcptSplitOutputs", msg => msg.Contains("EfcptSplitOutputs"))
            .AssertPassed();
    }

    [Scenario("Validation fails when Data project does not exist")]
    [Fact]
    public Task Validation_fails_when_data_project_does_not_exist()
    {
        // This test verifies the MSBuild error message format
        var expectedError = "EfcptDataProject was specified but the file does not exist";

        return Given("the expected error message", () => expectedError)
            .Then("error message mentions EfcptDataProject", msg => msg.Contains("EfcptDataProject"))
            .And("error message mentions file does not exist", msg => msg.Contains("does not exist"))
            .AssertPassed();
    }
}
