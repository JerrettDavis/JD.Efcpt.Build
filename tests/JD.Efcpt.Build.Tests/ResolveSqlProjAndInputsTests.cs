using Microsoft.Build.Utilities;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using Xunit;

namespace JD.Efcpt.Build.Tests;

public class ResolveSqlProjAndInputsTests
{
    [Fact]
    public void Discovers_sqlproj_and_project_level_inputs()
    {
        using var folder = new TestFolder();
        folder.CreateDir("db");
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");

        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");
        var config = folder.WriteFile("src/efcpt-config.json", "{}");
        var renaming = folder.WriteFile("src/efcpt.renaming.json", "[]");
        folder.WriteFile("src/Template/readme.txt", "template");

        var engine = new TestBuildEngine();
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = engine,
            ProjectFullPath = Path.Combine(projectDir, "App.csproj"),
            ProjectDirectory = projectDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "db", "Db.sqlproj"))],
            OutputDir = Path.Combine(projectDir, "obj", "efcpt"),
            SolutionDir = folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var ok = task.Execute();

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(sqlproj), task.SqlProjPath);
        Assert.Equal(Path.GetFullPath(config), task.ResolvedConfigPath);
        Assert.Equal(Path.GetFullPath(renaming), task.ResolvedRenamingPath);
        Assert.Equal(Path.GetFullPath(Path.Combine(projectDir, "Template")), task.ResolvedTemplateDir);
    }

    [Fact]
    public void Falls_back_to_solution_and_defaults()
    {
        using var folder = new TestFolder();
        folder.CreateDir("db");
        folder.WriteFile("db/Db.sqlproj", "<Project />");

        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");
        var solutionConfig = folder.WriteFile("efcpt-config.json", "{ \"level\": \"solution\" }");

        var engine = new TestBuildEngine();
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = engine,
            ProjectFullPath = Path.Combine(projectDir, "App.csproj"),
            ProjectDirectory = projectDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "db", "Db.sqlproj"))],
            OutputDir = Path.Combine(projectDir, "obj", "efcpt"),
            SolutionDir = folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot,
            ConfigOverride = "efcpt-config.json",
            RenamingOverride = "efcpt.renaming.json",
            TemplateDirOverride = "Template"
        };

        var ok = task.Execute();

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(solutionConfig), task.ResolvedConfigPath);
        Assert.Equal(Path.Combine(TestPaths.DefaultsRoot, "efcpt.renaming.json"), task.ResolvedRenamingPath);
        Assert.Equal(Path.Combine(TestPaths.DefaultsRoot, "Template"), task.ResolvedTemplateDir);
    }

    [Fact]
    public void Errors_when_multiple_sqlproj_references_present()
    {
        using var folder = new TestFolder();
        folder.WriteFile("db1/One.sqlproj", "<Project />");
        folder.WriteFile("db2/Two.sqlproj", "<Project />");
        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");

        var engine = new TestBuildEngine();
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = engine,
            ProjectFullPath = Path.Combine(projectDir, "App.csproj"),
            ProjectDirectory = projectDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "db1", "One.sqlproj")), new TaskItem(Path.Combine("..", "db2", "Two.sqlproj"))],
            OutputDir = Path.Combine(projectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var ok = task.Execute();

        Assert.False(ok);
        Assert.NotEmpty(engine.Errors);
    }
}
