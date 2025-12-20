using Microsoft.Build.Framework;
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
        TestBuildEngine Engine,
        string ProjectDir,
        string Csproj,
        string SqlProj,
        string Config,
        string Renaming,
        string AppSettings,
        string AppConfig = "");

    private sealed record TaskResult(
        SetupState Setup,
        ResolveSqlProjAndInputs Task,
        bool Success);

    private sealed record SolutionScanSetup(
        TestFolder Folder,
        string ProjectDir,
        string SqlProj,
        string SolutionPath,
        TestBuildEngine Engine);

    private sealed record SolutionScanResult(
        SolutionScanSetup Setup,
        ResolveSqlProjAndInputs Task,
        bool Success);

    private static SetupState SetupProjectLevelInputs()
    {
        var folder = new TestFolder();
        folder.CreateDir("db");
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");

        var projectDir = folder.CreateDir("src");
        var csproj = folder.WriteFile("src/App.csproj", "<Project />");
        var config = folder.WriteFile("src/efcpt-config.json", "{}");
        var renaming = folder.WriteFile("src/efcpt.renaming.json", "[]");
        folder.WriteFile("src/Template/readme.txt", "template");

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, sqlproj, config, renaming, "", "");
    }

    private static SetupState SetupSdkProjectLevelInputs()
    {
        var folder = new TestFolder();
        folder.CreateDir("db");
        var sqlproj = folder.WriteFile("db/Db.csproj", "<Project Sdk=\"MSBuild.Sdk.SqlProj/3.0.0\" />");

        var projectDir = folder.CreateDir("src");
        var csproj = folder.WriteFile("src/App.csproj", "<Project />");
        var config = folder.WriteFile("src/efcpt-config.json", "{}");
        var renaming = folder.WriteFile("src/efcpt.renaming.json", "[]");
        folder.WriteFile("src/Template/readme.txt", "template");

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, sqlproj, config, renaming, "", "");
    }

    private static SolutionScanSetup SetupSolutionScanInputs()
    {
        var folder = new TestFolder();
        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");

        var sqlproj = folder.WriteFile("db/Db.csproj", "<Project Sdk=\"MSBuild.Sdk.SqlProj/3.0.0\" />");
        var solutionPath = folder.WriteFile("Sample.sln",
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{11111111-1111-1111-1111-111111111111}") = "App", "src\App.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Project("{11111111-1111-1111-1111-111111111111}") = "Db", "db\Db.csproj", "{33333333-3333-3333-3333-333333333333}"
            EndProject
            """);

        var engine = new TestBuildEngine();
        return new SolutionScanSetup(folder, projectDir, sqlproj, solutionPath, engine);
    }

    private static SolutionScanSetup SetupSlnxScanInputs()
    {
        var folder = new TestFolder();
        var projectDir = folder.CreateDir("src");
        folder.WriteFile("src/App.csproj", "<Project />");

        var sqlproj = folder.WriteFile("db/Db.csproj", "<Project Sdk=\"MSBuild.Sdk.SqlProj/3.0.0\" />");
        var solutionPath = folder.WriteFile("Sample.slnx",
            """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/App.csproj" />
              </Folder>
              <Folder Name="/db/">
                <Project Path="db/Db.csproj" />
              </Folder>
            </Solution>
            """);

        var engine = new TestBuildEngine();
        return new SolutionScanSetup(folder, projectDir, sqlproj, solutionPath, engine);
    }

    private static SetupState SetupSolutionLevelInputs()
    {
        var folder = new TestFolder();
        folder.CreateDir("db");
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");

        var projectDir = folder.CreateDir("src");
        var csproj = folder.WriteFile("src/App.csproj", "<Project />");
        var config = folder.WriteFile("efcpt-config.json", "{ \"level\": \"solution\" }");

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, sqlproj, config, "", "", "");
    }

    private static SetupState SetupMultipleSqlProj()
    {
        var folder = new TestFolder();
        folder.WriteFile("db1/One.sqlproj", "<Project />");
        folder.WriteFile("db2/Two.sqlproj", "<Project />");
        var projectDir = folder.CreateDir("src");
        var csproj = folder.WriteFile("src/App.csproj", "<Project />");

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, "", "", "", "", "");
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

    private static TaskResult ExecuteTaskProjectLevelSdk(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = Path.Combine(setup.ProjectDir, "App.csproj"),
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = [new TaskItem(Path.Combine("..", "db", "Db.csproj"))],
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            SolutionDir = setup.Folder.Root,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static SolutionScanResult ExecuteTaskSolutionScan(SolutionScanSetup setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = Path.Combine(setup.ProjectDir, "App.csproj"),
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = [],
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            SolutionDir = setup.Folder.Root,
            SolutionPath = setup.SolutionPath,
            ProbeSolutionDir = "true",
            DefaultsRoot = TestPaths.DefaultsRoot
        };

        var success = task.Execute();
        return new SolutionScanResult(setup, task, success);
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

    [Scenario("Discovers MSBuild.Sdk.SqlProj project references")]
    [Fact]
    public async Task Discovers_sdk_sqlproj_reference()
    {
        await Given("project with SDK sql project", SetupSdkProjectLevelInputs)
            .When("execute task", ExecuteTaskProjectLevelSdk)
            .Then("task succeeds", r => r.Success)
            .And("sql project path resolved", r => r.Task.SqlProjPath == Path.GetFullPath(r.Setup.SqlProj))
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Scans solution for SQL project when no references exist")]
    [Fact]
    public async Task Scans_solution_for_sql_project()
    {
        await Given("project with solution-level SQL project", SetupSolutionScanInputs)
            .When("execute task with solution scan", ExecuteTaskSolutionScan)
            .Then("task succeeds", r => r.Success)
            .And("sql project path resolved", r => r.Task.SqlProjPath == Path.GetFullPath(r.Setup.SqlProj))
            .And("warning logged", r => r.Setup.Engine.Warnings.Count == 1)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Scans slnx solution for SQL project when no references exist")]
    [Fact]
    public async Task Scans_slnx_solution_for_sql_project()
    {
        await Given("project with slnx SQL project", SetupSlnxScanInputs)
            .When("execute task with solution scan", ExecuteTaskSolutionScan)
            .Then("task succeeds", r => r.Success)
            .And("sql project path resolved", r => r.Task.SqlProjPath == Path.GetFullPath(r.Setup.SqlProj))
            .And("warning logged", r => r.Setup.Engine.Warnings.Count == 1)
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

    // ========== Connection String Discovery Tests ==========

    [Scenario("Uses explicit EfcptConnectionString property as highest priority")]
    [Fact]
    public async Task Uses_explicit_connection_string()
    {
        await Given("project with explicit connection string", SetupExplicitConnectionString)
            .When("execute task with explicit connection string", ExecuteTaskExplicitConnectionString)
            .Then("task succeeds", r => r.Success)
            .And("connection string resolved", r => r.Task.ResolvedConnectionString == "Server=localhost;Database=ExplicitDb;")
            .And("uses connection string mode", r => r.Task.UseConnectionString == "true")
            .And("sql project not resolved", r => string.IsNullOrEmpty(r.Task.SqlProjPath))
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Discovers connection string from appsettings.json with specified key")]
    [Fact]
    public async Task Discovers_connection_string_from_appsettings()
    {
        await Given("project with appsettings.json", SetupAppSettingsConnectionString)
            .When("execute task with appsettings", ExecuteTaskAppSettingsConnectionString)
            .Then("task succeeds", r => r.Success)
            .And("connection string resolved", r => r.Task.ResolvedConnectionString == "Server=localhost;Database=AppSettingsDb;")
            .And("uses connection string mode", r => r.Task.UseConnectionString == "true")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Discovers connection string from app.config with specified key")]
    [Fact]
    public async Task Discovers_connection_string_from_appconfig()
    {
        await Given("project with app.config", SetupAppConfigConnectionString)
            .When("execute task with app.config", ExecuteTaskAppConfigConnectionString)
            .Then("task succeeds", r => r.Success)
            .And("connection string resolved", r => r.Task.ResolvedConnectionString == "Server=localhost;Database=AppConfigDb;")
            .And("uses connection string mode", r => r.Task.UseConnectionString == "true")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Auto-discovers appsettings.json in project directory")]
    [Fact]
    public async Task Auto_discovers_appsettings_json()
    {
        await Given("project with auto-discovered appsettings.json", SetupAutoDiscoverAppSettings)
            .When("execute task without overrides", ExecuteTaskAutoDiscoverAppSettings)
            .Then("task succeeds", r => r.Success)
            .And("connection string resolved", r => r.Task.ResolvedConnectionString == "Server=localhost;Database=AutoDb;")
            .And("uses connection string mode", r => r.Task.UseConnectionString == "true")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Auto-discovers app.config in project directory")]
    [Fact]
    public async Task Auto_discovers_app_config()
    {
        await Given("project with auto-discovered app.config", SetupAutoDiscoverAppConfig)
            .When("execute task without overrides", ExecuteTaskAutoDiscoverAppConfig)
            .Then("task succeeds", r => r.Success)
            .And("connection string resolved", r => r.Task.ResolvedConnectionString == "Server=localhost;Database=AutoAppConfigDb;")
            .And("uses connection string mode", r => r.Task.UseConnectionString == "true")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Falls back to sqlproj when no connection string found")]
    [Fact]
    public async Task Falls_back_to_sqlproj_when_no_connection_string()
    {
        await Given("project with sqlproj but no connection string", SetupSqlProjNoConnectionString)
            .When("execute task", ExecuteTaskSqlProjNoConnectionString)
            .Then("task succeeds", r => r.Success)
            .And("uses dacpac mode", r => r.Task.UseConnectionString == "false")
            .And("sql project resolved", r => !string.IsNullOrEmpty(r.Task.SqlProjPath))
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // ========== Setup Methods for Connection String Tests ==========

    private static SetupState SetupExplicitConnectionString()
    {
        var folder = new TestFolder();
        var projectDir = folder.Root;
        var csproj = folder.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, "", "", "", "");
    }

    private static SetupState SetupAppSettingsConnectionString()
    {
        var folder = new TestFolder();
        var projectDir = folder.Root;
        var csproj = folder.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var appsettings = folder.WriteFile("appsettings.json",
            """
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Database=AppSettingsDb;"
              }
            }
            """);

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, "", "", "", appsettings);
    }

    private static SetupState SetupAppConfigConnectionString()
    {
        var folder = new TestFolder();
        var projectDir = folder.Root;
        var csproj = folder.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var appConfig = folder.WriteFile("app.config",
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <connectionStrings>
                <add name="DefaultConnection" connectionString="Server=localhost;Database=AppConfigDb;" />
              </connectionStrings>
            </configuration>
            """);

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, "", "", "", "", appConfig);
    }

    private static SetupState SetupAutoDiscoverAppSettings()
    {
        var folder = new TestFolder();
        var projectDir = folder.Root;
        var csproj = folder.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        // Place appsettings.json in project directory (will be auto-discovered)
        folder.WriteFile("appsettings.json",
            """
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Database=AutoDb;"
              }
            }
            """);

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, "", "", "", "");
    }

    private static SetupState SetupAutoDiscoverAppConfig()
    {
        var folder = new TestFolder();
        var projectDir = folder.Root;
        var csproj = folder.WriteFile("MyApp.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        // Place app.config in project directory (will be auto-discovered)
        folder.WriteFile("app.config",
            """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <connectionStrings>
                <add name="DefaultConnection" connectionString="Server=localhost;Database=AutoAppConfigDb;" />
              </connectionStrings>
            </configuration>
            """);

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, "", "", "", "");
    }

    private static SetupState SetupSqlProjNoConnectionString()
    {
        var folder = new TestFolder();
        var projectDir = folder.Root;
        var sqlproj = folder.WriteFile("Database.sqlproj", "<Project Sdk=\"MSBuild.Sdk.SqlProj/2.0.0\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup></Project>");
        var csproj = folder.WriteFile("MyApp.csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{sqlproj}" />
              </ItemGroup>
            </Project>
            """);

        var engine = new TestBuildEngine();
        return new SetupState(folder, engine, projectDir, csproj, sqlproj, "", "", "");
    }

    // ========== Execute Methods for Connection String Tests ==========

    private static TaskResult ExecuteTaskExplicitConnectionString(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = setup.Csproj,
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = Array.Empty<ITaskItem>(),
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot,
            EfcptConnectionString = "Server=localhost;Database=ExplicitDb;",
            EfcptConnectionStringName = "DefaultConnection"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskAppSettingsConnectionString(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = setup.Csproj,
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = Array.Empty<ITaskItem>(),
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot,
            EfcptAppSettings = setup.AppSettings,
            EfcptConnectionStringName = "DefaultConnection"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskAppConfigConnectionString(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = setup.Csproj,
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = Array.Empty<ITaskItem>(),
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot,
            EfcptAppConfig = setup.AppConfig,
            EfcptConnectionStringName = "DefaultConnection"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskAutoDiscoverAppSettings(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = setup.Csproj,
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = Array.Empty<ITaskItem>(),
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot,
            EfcptConnectionStringName = "DefaultConnection"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskAutoDiscoverAppConfig(SetupState setup)
    {
        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = setup.Csproj,
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = Array.Empty<ITaskItem>(),
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot,
            EfcptConnectionStringName = "DefaultConnection"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskSqlProjNoConnectionString(SetupState setup)
    {
        var projectReferences = new[]
        {
            new TaskItem(setup.SqlProj, new Dictionary<string, string> { ["ReferenceOutputAssembly"] = "false" })
        };

        var task = new ResolveSqlProjAndInputs
        {
            BuildEngine = setup.Engine,
            ProjectFullPath = setup.Csproj,
            ProjectDirectory = setup.ProjectDir,
            Configuration = "Debug",
            ProjectReferences = projectReferences,
            OutputDir = Path.Combine(setup.ProjectDir, "obj", "efcpt"),
            DefaultsRoot = TestPaths.DefaultsRoot,
            EfcptConnectionStringName = "DefaultConnection"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }
}
