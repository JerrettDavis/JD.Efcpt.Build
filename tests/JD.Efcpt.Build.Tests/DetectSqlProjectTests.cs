using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the DetectSqlProject MSBuild task.
/// This task detects whether a project is a SQL database project via SDK or properties.
/// </summary>
[Feature("DetectSqlProject: MSBuild task for SQL project detection")]
[Collection(nameof(AssemblySetup))]
public sealed class DetectSqlProjectTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(TestBuildEngine Engine, TestFolder Folder, DetectSqlProject Task);
    private sealed record ExecutionResult(SetupState Setup, bool Success, bool IsSqlProject);

    private static SetupState SetupTask(string projectFileName, string projectContent, string? sqlServerVersion = null, string? dsp = null)
    {
        var folder = new TestFolder();
        var projectPath = folder.WriteFile(projectFileName, projectContent);
        var engine = new TestBuildEngine();
        
        var task = new DetectSqlProject
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            SqlServerVersion = sqlServerVersion,
            DSP = dsp
        };
        
        return new SetupState(engine, folder, task);
    }

    private static ExecutionResult Execute(SetupState setup)
    {
        var success = setup.Task.Execute();
        return new ExecutionResult(setup, success, setup.Task.IsSqlProject);
    }

    [Scenario("Modern SQL SDK project is detected via SDK attribute")]
    [Fact]
    public async Task Modern_sql_sdk_detected()
    {
        await Given("a project with MSBuild.Sdk.SqlProj SDK", () =>
                SetupTask("Database.csproj", "<Project Sdk=\"MSBuild.Sdk.SqlProj/3.3.0\" />"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Modern SQL SDK project is detected via Sdk element")]
    [Fact]
    public async Task Modern_sql_sdk_via_element_detected()
    {
        await Given("a project with Microsoft.Build.Sql SDK element", () =>
                SetupTask("Database.csproj", 
                    "<Project><Sdk Name=\"Microsoft.Build.Sql\" Version=\"2.0.0\" /></Project>"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Legacy SSDT project detected via SqlServerVersion property")]
    [Fact]
    public async Task Legacy_ssdt_via_sqlserverversion_detected()
    {
        await Given("a project with SqlServerVersion property", () =>
                SetupTask("Database.sqlproj", 
                    "<Project><PropertyGroup><SqlServerVersion>Sql150</SqlServerVersion></PropertyGroup></Project>",
                    sqlServerVersion: "Sql150"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Legacy SSDT project detected via DSP property")]
    [Fact]
    public async Task Legacy_ssdt_via_dsp_detected()
    {
        await Given("a project with DSP property", () =>
                SetupTask("Database.sqlproj",
                    "<Project><PropertyGroup><DSP>Microsoft.Data.Tools.Schema.Sql.Sql150DatabaseSchemaProvider</DSP></PropertyGroup></Project>",
                    dsp: "Microsoft.Data.Tools.Schema.Sql.Sql150DatabaseSchemaProvider"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Legacy SSDT project detected with both properties")]
    [Fact]
    public async Task Legacy_ssdt_with_both_properties_detected()
    {
        await Given("a project with both SqlServerVersion and DSP", () =>
                SetupTask("Database.sqlproj",
                    "<Project />",
                    sqlServerVersion: "Sql150",
                    dsp: "Microsoft.Data.Tools.Schema.Sql.Sql150DatabaseSchemaProvider"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Non-SQL project returns false")]
    [Fact]
    public async Task Non_sql_project_returns_false()
    {
        await Given("a regular .NET project", () =>
                SetupTask("App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Null ProjectPath returns error")]
    [Fact]
    public async Task Null_project_path_returns_error()
    {
        await Given("a task with null ProjectPath", () =>
            {
                var engine = new TestBuildEngine();
                var task = new DetectSqlProject
                {
                    BuildEngine = engine,
                    ProjectPath = null!
                };
                return new SetupState(engine, new TestFolder(), task);
            })
            .When("detection runs", Execute)
            .Then("execution fails", r => !r.Success)
            .And("error is logged", r => r.Setup.Engine.Errors.Count > 0)
            .And("error mentions ProjectPath", r => r.Setup.Engine.Errors[0].Message?.Contains("ProjectPath") == true)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Empty ProjectPath returns error")]
    [Fact]
    public async Task Empty_project_path_returns_error()
    {
        await Given("a task with empty ProjectPath", () =>
            {
                var engine = new TestBuildEngine();
                var task = new DetectSqlProject
                {
                    BuildEngine = engine,
                    ProjectPath = "   "
                };
                return new SetupState(engine, new TestFolder(), task);
            })
            .When("detection runs", Execute)
            .Then("execution fails", r => !r.Success)
            .And("error is logged", r => r.Setup.Engine.Errors.Count > 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Missing project file returns false gracefully")]
    [Fact]
    public async Task Missing_project_file_returns_false()
    {
        await Given("a task with non-existent project path", () =>
            {
                var folder = new TestFolder();
                var engine = new TestBuildEngine();
                var task = new DetectSqlProject
                {
                    BuildEngine = engine,
                    ProjectPath = Path.Combine(folder.Root, "NotExists.csproj")
                };
                return new SetupState(engine, folder, task);
            })
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Project with no SQL indicators returns false")]
    [Fact]
    public async Task No_sql_indicators_returns_false()
    {
        await Given("a project with no SQL SDK or properties", () =>
                SetupTask("Library.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>",
                    sqlServerVersion: null,
                    dsp: null))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is false", r => !r.IsSqlProject)
            .And("low importance message logged", r => r.Setup.Engine.Messages.Exists(m => m.Message?.Contains("Not a SQL project") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Modern SDK takes precedence over properties")]
    [Fact]
    public async Task Modern_sdk_takes_precedence()
    {
        await Given("a project with modern SDK and legacy properties", () =>
                SetupTask("Database.csproj",
                    "<Project Sdk=\"MSBuild.Sdk.SqlProj/3.3.0\" />",
                    sqlServerVersion: "Sql150"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .And("SDK detection message logged", r => r.Setup.Engine.Messages.Exists(m => m.Message?.Contains("SDK attribute") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Invalid XML project file returns false gracefully")]
    [Fact]
    public async Task Invalid_xml_returns_false()
    {
        await Given("a project with invalid XML", () =>
                SetupTask("Broken.csproj", "<Project"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Multiple SDK values with SQL SDK detected")]
    [Fact]
    public async Task Multiple_sdks_with_sql_detected()
    {
        await Given("a project with multiple SDKs including SQL", () =>
                SetupTask("Database.csproj", 
                    "<Project Sdk=\"Microsoft.NET.Sdk;MSBuild.Sdk.SqlProj/3.3.0\" />"))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Whitespace-only SqlServerVersion is ignored")]
    [Fact]
    public async Task Whitespace_sqlserverversion_ignored()
    {
        await Given("a project with whitespace SqlServerVersion", () =>
                SetupTask("App.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\" />",
                    sqlServerVersion: "   "))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Whitespace-only DSP is ignored")]
    [Fact]
    public async Task Whitespace_dsp_ignored()
    {
        await Given("a project with whitespace DSP", () =>
                SetupTask("App.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\" />",
                    dsp: "   "))
            .When("detection runs", Execute)
            .Then("execution succeeds", r => r.Success)
            .And("IsSqlProject is false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
