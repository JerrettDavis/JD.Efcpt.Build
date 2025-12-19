using Microsoft.Build.Utilities;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

[Feature("ResolveSqlProjAndInputs task: discovers sqlproj and configuration files")]
[Collection(nameof(AssemblySetup))]
public sealed class ResolveSqlProjAndInputsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestFolder Folder,
        string ProjectDir,
        string SqlProj,
        TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        ResolveSqlProjAndInputs Task,
        bool Success);

    private static SetupState SetupProjectLevelInputs()
    {
        var folder = new TestFolder();
        folder.CreateDir("db");
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");

        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");
        folder.WriteFile("src/efcpt-config.json", "{}");
        folder.WriteFile("src/efcpt.renaming.json", "[]");
        folder.WriteFile("src/Template/readme.txt", "template");

        var engine = new TestBuildEngine();
        return new SetupState(folder, projectDir, sqlproj, engine);
    }

    private static SetupState SetupSolutionLevelInputs()
    {
        var folder = new TestFolder();
        folder.CreateDir("db");
        folder.WriteFile("db/Db.sqlproj", "<Project />");

        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");
        folder.WriteFile("efcpt-config.json", "{ \"level\": \"solution\" }");

        var engine = new TestBuildEngine();
        return new SetupState(folder, projectDir, folder.WriteFile("db/Db.sqlproj", "<Project />"), engine);
    }

    private static SetupState SetupMultipleSqlProj()
    {
        var folder = new TestFolder();
        folder.WriteFile("db1/One.sqlproj", "<Project />");
        folder.WriteFile("db2/Two.sqlproj", "<Project />");
        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");

        var engine = new TestBuildEngine();
        return new SetupState(folder, projectDir, "", engine);
    }

    private static TaskResult ExecuteTaskProjectLevel(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = Path.Combine(setup.ProjectDir, "App.csproj"),
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "db", "Db.sqlproj"))],
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            SolutionDir = setup.Folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskSolutionLevel(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = Path.Combine(setup.ProjectDir, "App.csproj"),
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "db", "Db.sqlproj"))],
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            SolutionDir = setup.Folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot,
            ConfigOverride = "efcpt-config.json",
            RenamingOverride = "efcpt.renaming.json",
            TemplateDirOverride = "Template"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskMultipleSqlProj(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = Path.Combine(setup.ProjectDir, "App.csproj"),
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = [
                new TaskItem(Path.Combine("..", "db1", "One.sqlproj")),
                new TaskItem(Path.Combine("..", "db2", "Two.sqlproj"))
            ],
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }
    [Scenario("Discovers sqlproj and project-level config files")]
    [Fact]
    public async Task Discovers_sqlproj_and_project_level_inputs()
    {
        await Given("project with local config files", SetupProjectLevelInputs)
            .When("execute task", ExecuteTaskProjectLevel)
            .Then("task succeeds", r => r.Success)
            .And("sqlproj path resolved", r => r.Task.SqlProjPath == Path.GetFullPath(r.Setup.SqlProj))
            .And("config path resolved", r => r.Task.ResolvedConfigPath == Path.GetFullPath(Path.Combine(r.Setup.ProjectDir, "efcpt-config.json")))
            .And("renaming path resolved", r => r.Task.ResolvedRenamingPath == Path.GetFullPath(Path.Combine(r.Setup.ProjectDir, "efcpt.renaming.json")))
            .And("template dir resolved", r => r.Task.ResolvedTemplateDir == Path.GetFullPath(Path.Combine(r.Setup.ProjectDir, "Template")))
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Falls back to solution-level config and defaults")]
    [Fact]
    public async Task Falls_back_to_solution_and_defaults()
    {
        await Given("project with solution-level config", SetupSolutionLevelInputs)
            .When("execute task with overrides", ExecuteTaskSolutionLevel)
            .Then("task succeeds", r => r.Success)
            .And("solution config resolved", r => r.Task.ResolvedConfigPath == Path.GetFullPath(Path.Combine(r.Setup.Folder.Root, "efcpt-config.json")))
            .And("default renaming path used", r => r.Task.ResolvedRenamingPath == Path.Combine(TestPaths.DefaultsRoot, "efcpt.renaming.json"))
            .And("default template dir used", r => r.Task.ResolvedTemplateDir == Path.Combine(TestPaths.DefaultsRoot, "Template"))
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Errors when multiple sqlproj references are present")]
    [Fact]
    public async Task Errors_when_multiple_sqlproj_references_present()
    {
        await Given("project with multiple sqlproj references", SetupMultipleSqlProj)
            .When("execute task", ExecuteTaskMultipleSqlProj)
            .Then("task fails", r => !r.Success)
            .And("errors are logged", r => r.Setup.Engine.Errors.Count > 0)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
