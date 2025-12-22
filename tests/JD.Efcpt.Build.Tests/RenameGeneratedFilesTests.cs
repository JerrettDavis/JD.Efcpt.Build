using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the RenameGeneratedFiles MSBuild task.
/// </summary>
[Feature("RenameGeneratedFiles: rename .cs files to .g.cs convention")]
[Collection(nameof(AssemblySetup))]
public sealed class RenameGeneratedFilesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestFolder Folder,
        string GeneratedDir,
        TestBuildEngine Engine
    );

    private sealed record TaskResult(
        SetupState Setup,
        RenameGeneratedFiles Task,
        bool Success
    );

    private static SetupState SetupWithCsFiles()
    {
        var folder = new TestFolder();
        var generatedDir = folder.CreateDir("Generated");

        // Create some .cs files
        File.WriteAllText(Path.Combine(generatedDir, "Model1.cs"), "// Model1");
        File.WriteAllText(Path.Combine(generatedDir, "Model2.cs"), "// Model2");
        File.WriteAllText(Path.Combine(generatedDir, "DbContext.cs"), "// DbContext");

        var engine = new TestBuildEngine();
        return new SetupState(folder, generatedDir, engine);
    }

    private static SetupState SetupWithMixedFiles()
    {
        var folder = new TestFolder();
        var generatedDir = folder.CreateDir("Generated");

        // Create mix of .cs and .g.cs files
        File.WriteAllText(Path.Combine(generatedDir, "Model1.cs"), "// Model1");
        File.WriteAllText(Path.Combine(generatedDir, "Model2.g.cs"), "// Already renamed");
        File.WriteAllText(Path.Combine(generatedDir, "Model3.cs"), "// Model3");

        var engine = new TestBuildEngine();
        return new SetupState(folder, generatedDir, engine);
    }

    private static SetupState SetupWithNestedDirs()
    {
        var folder = new TestFolder();
        var generatedDir = folder.CreateDir("Generated");
        var modelsDir = folder.CreateDir("Generated/Models");

        File.WriteAllText(Path.Combine(generatedDir, "DbContext.cs"), "// DbContext");
        File.WriteAllText(Path.Combine(modelsDir, "Entity1.cs"), "// Entity1");
        File.WriteAllText(Path.Combine(modelsDir, "Entity2.cs"), "// Entity2");

        var engine = new TestBuildEngine();
        return new SetupState(folder, generatedDir, engine);
    }

    private static SetupState SetupWithNoFiles()
    {
        var folder = new TestFolder();
        var generatedDir = folder.CreateDir("Generated");
        var engine = new TestBuildEngine();
        return new SetupState(folder, generatedDir, engine);
    }

    private static SetupState SetupWithMissingDir()
    {
        var folder = new TestFolder();
        var generatedDir = Path.Combine(folder.Root, "NonExistent");
        var engine = new TestBuildEngine();
        return new SetupState(folder, generatedDir, engine);
    }

    private static SetupState SetupWithExistingGcsFiles()
    {
        var folder = new TestFolder();
        var generatedDir = folder.CreateDir("Generated");

        // Create a .cs file and a pre-existing .g.cs with the same base name
        File.WriteAllText(Path.Combine(generatedDir, "Model.cs"), "// New version");
        File.WriteAllText(Path.Combine(generatedDir, "Model.g.cs"), "// Old version");

        var engine = new TestBuildEngine();
        return new SetupState(folder, generatedDir, engine);
    }

    private static TaskResult ExecuteTask(SetupState setup, string logVerbosity = "minimal")
    {
        var task = new RenameGeneratedFiles
        {
            BuildEngine = setup.Engine,
            GeneratedDir = setup.GeneratedDir,
            LogVerbosity = logVerbosity
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    [Scenario("Renames all .cs files to .g.cs")]
    [Fact]
    public async Task Renames_cs_files_to_gcs()
    {
        await Given("directory with .cs files", SetupWithCsFiles)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("all files renamed to .g.cs", r =>
            {
                var files = Directory.GetFiles(r.Setup.GeneratedDir, "*.cs");
                return files.All(f => f.EndsWith(".g.cs"));
            })
            .And("original .cs files no longer exist",
                r => !File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model1.cs")) &&
                     !File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model2.cs")) &&
                     !File.Exists(Path.Combine(r.Setup.GeneratedDir, "DbContext.cs")))
            .And("renamed files exist",
                r => File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model1.g.cs")) &&
                     File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model2.g.cs")) &&
                     File.Exists(Path.Combine(r.Setup.GeneratedDir, "DbContext.g.cs")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Skips files already ending with .g.cs")]
    [Fact]
    public async Task Skips_already_renamed_files()
    {
        await Given("directory with mixed .cs and .g.cs files", SetupWithMixedFiles)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("original .g.cs file preserved", r =>
            {
                var content = File.ReadAllText(Path.Combine(r.Setup.GeneratedDir, "Model2.g.cs"));
                return content.Contains("Already renamed");
            })
            .And("other files renamed", r =>
            {
                return File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model1.g.cs")) &&
                       File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model3.g.cs"));
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Renames files in subdirectories")]
    [Fact]
    public async Task Renames_files_in_subdirectories()
    {
        await Given("directory with nested subdirectories", SetupWithNestedDirs)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("root files renamed", r => File.Exists(Path.Combine(r.Setup.GeneratedDir, "DbContext.g.cs")))
            .And("nested files renamed", r =>
            {
                var modelsDir = Path.Combine(r.Setup.GeneratedDir, "Models");
                return File.Exists(Path.Combine(modelsDir, "Entity1.g.cs")) &&
                       File.Exists(Path.Combine(modelsDir, "Entity2.g.cs"));
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Succeeds with empty directory")]
    [Fact]
    public async Task Succeeds_with_empty_directory()
    {
        await Given("empty generated directory", SetupWithNoFiles)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("no errors logged", r => r.Setup.Engine.Errors.Count == 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Succeeds when directory does not exist")]
    [Fact]
    public async Task Succeeds_when_directory_missing()
    {
        await Given("non-existent directory", SetupWithMissingDir)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("no errors logged", r => r.Setup.Engine.Errors.Count == 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Overwrites existing .g.cs file when renaming")]
    [Fact]
    public async Task Overwrites_existing_gcs_file()
    {
        await Given("directory with conflicting file names", SetupWithExistingGcsFiles)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("renamed file has new content", r =>
            {
                var content = File.ReadAllText(Path.Combine(r.Setup.GeneratedDir, "Model.g.cs"));
                return content.Contains("New version");
            })
            .And("only one file exists", r =>
            {
                var files = Directory.GetFiles(r.Setup.GeneratedDir, "Model*");
                return files.Length == 1;
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Logs rename operations with detailed verbosity")]
    [Fact]
    public async Task Logs_with_detailed_verbosity()
    {
        await Given("directory with .cs files", SetupWithCsFiles)
            .When("task executes with detailed verbosity", s => ExecuteTask(s, "detailed"))
            .Then("task succeeds", r => r.Success)
            .And("messages contain rename info", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("Renamed") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Preserves file content during rename")]
    [Fact]
    public async Task Preserves_file_content()
    {
        await Given("directory with .cs files", SetupWithCsFiles)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("file content preserved", r =>
            {
                var content = File.ReadAllText(Path.Combine(r.Setup.GeneratedDir, "Model1.g.cs"));
                return content == "// Model1";
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles files with multiple extensions")]
    [Fact]
    public async Task Handles_multiple_extensions()
    {
        await Given("file with multiple extensions", () =>
            {
                var folder = new TestFolder();
                var generatedDir = folder.CreateDir("Generated");
                File.WriteAllText(Path.Combine(generatedDir, "Model.test.cs"), "// content");
                var engine = new TestBuildEngine();
                return new SetupState(folder, generatedDir, engine);
            })
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("file renamed correctly", r =>
                File.Exists(Path.Combine(r.Setup.GeneratedDir, "Model.test.g.cs")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}