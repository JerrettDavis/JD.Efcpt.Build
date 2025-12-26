using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the ResolveDbContextName MSBuild task.
/// </summary>
[Feature("ResolveDbContextName: MSBuild task for resolving DbContext names")]
[Collection(nameof(AssemblySetup))]
public sealed class ResolveDbContextNameTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TaskResult(
        ResolveDbContextName Task,
        bool Success,
        string ResolvedName);

    private static TaskResult ExecuteTask(
        string explicitName = "",
        string sqlProjPath = "",
        string dacpacPath = "",
        string connectionString = "",
        string useConnectionStringMode = "false")
    {
        var engine = new TestBuildEngine();
        var task = new ResolveDbContextName
        {
            BuildEngine = engine,
            ExplicitDbContextName = explicitName,
            SqlProjPath = sqlProjPath,
            DacpacPath = dacpacPath,
            ConnectionString = connectionString,
            UseConnectionStringMode = useConnectionStringMode,
            LogVerbosity = "minimal"
        };

        var success = task.Execute();
        return new TaskResult(task, success, task.ResolvedDbContextName);
    }

    [Scenario("Uses explicit name when provided")]
    [Fact]
    public async Task Uses_explicit_name_when_provided()
    {
        await Given("an explicit DbContext name", () => "MyExplicitContext")
            .When("task executes with explicit name", name =>
                ExecuteTask(explicitName: name, sqlProjPath: "/path/Database.sqlproj"))
            .Then("task succeeds", r => r.Success)
            .And("returns explicit name", r => r.ResolvedName == "MyExplicitContext")
            .AssertPassed();
    }

    [Scenario("Generates name from SQL project path")]
    [Fact]
    public async Task Generates_name_from_sql_project()
    {
        await Given("a SQL project path", () => "/path/to/DatabaseProject.sqlproj")
            .When("task executes with SQL project", path =>
                ExecuteTask(sqlProjPath: path))
            .Then("task succeeds", r => r.Success)
            .And("returns generated name from project", r => r.ResolvedName == "DatabaseProjectContext")
            .AssertPassed();
    }

    [Scenario("Generates name from DACPAC path")]
    [Fact]
    public async Task Generates_name_from_dacpac()
    {
        await Given("a DACPAC path", () => "/path/to/MyDatabase.dacpac")
            .When("task executes with DACPAC", path =>
                ExecuteTask(dacpacPath: path))
            .Then("task succeeds", r => r.Success)
            .And("returns generated name from DACPAC", r => r.ResolvedName == "MyDatabaseContext")
            .AssertPassed();
    }

    [Scenario("Generates name from connection string")]
    [Fact]
    public async Task Generates_name_from_connection_string()
    {
        await Given("a connection string", () => "Server=localhost;Database=SampleDb;")
            .When("task executes with connection string", connStr =>
                ExecuteTask(connectionString: connStr))
            .Then("task succeeds", r => r.Success)
            .And("returns generated name from database", r => r.ResolvedName == "SampleDbContext")
            .AssertPassed();
    }

    [Scenario("Prioritizes SQL project over DACPAC and connection string")]
    [Fact]
    public async Task Prioritizes_sql_project()
    {
        await Given("SQL project, DACPAC, and connection string", () => 
                ("/path/Project.sqlproj", "/path/Database.dacpac", "Database=Other;"))
            .When("task executes with all inputs", ctx =>
                ExecuteTask(
                    sqlProjPath: ctx.Item1,
                    dacpacPath: ctx.Item2,
                    connectionString: ctx.Item3))
            .Then("task succeeds", r => r.Success)
            .And("uses SQL project name", r => r.ResolvedName == "ProjectContext")
            .AssertPassed();
    }

    [Scenario("Uses DACPAC when SQL project is empty")]
    [Fact]
    public async Task Uses_dacpac_when_no_sql_project()
    {
        await Given("DACPAC and connection string but no SQL project", () =>
                ("/path/MyDatabase.dacpac", "Database=Other;"))
            .When("task executes without SQL project", ctx =>
                ExecuteTask(
                    dacpacPath: ctx.Item1,
                    connectionString: ctx.Item2))
            .Then("task succeeds", r => r.Success)
            .And("uses DACPAC name", r => r.ResolvedName == "MyDatabaseContext")
            .AssertPassed();
    }

    [Scenario("Uses connection string when SQL project and DACPAC are empty")]
    [Fact]
    public async Task Uses_connection_string_when_no_project_or_dacpac()
    {
        await Given("connection string only", () => "Database=FinalDb;Server=localhost;")
            .When("task executes with only connection string", connStr =>
                ExecuteTask(connectionString: connStr))
            .Then("task succeeds", r => r.Success)
            .And("uses database name from connection string", r => r.ResolvedName == "FinalDbContext")
            .AssertPassed();
    }

    [Scenario("Returns default MyDbContext when all inputs are empty")]
    [Fact]
    public async Task Returns_default_when_all_empty()
    {
        await Given("no inputs provided", () => (object?)null)
            .When("task executes with no inputs", _ => ExecuteTask())
            .Then("task succeeds", r => r.Success)
            .And("returns default name", r => r.ResolvedName == "MyDbContext")
            .AssertPassed();
    }

    [Scenario("Connection string mode prioritizes connection string over SQL project")]
    [Fact]
    public async Task Connection_string_mode_prioritizes_connection_string()
    {
        await Given("SQL project and connection string", () =>
                ("/path/Project.sqlproj", "Database=ConnectionDb;"))
            .When("task executes in connection string mode", ctx =>
                ExecuteTask(
                    sqlProjPath: ctx.Item1,
                    connectionString: ctx.Item2,
                    useConnectionStringMode: "true"))
            .Then("task succeeds", r => r.Success)
            .And("uses connection string database name", r => r.ResolvedName == "ConnectionDbContext")
            .AssertPassed();
    }

    [Scenario("Connection string mode falls back to DACPAC when connection string is empty")]
    [Fact]
    public async Task Connection_string_mode_falls_back_to_dacpac()
    {
        await Given("DACPAC but no connection string", () => "/path/MyDatabase.dacpac")
            .When("task executes in connection string mode", dacpac =>
                ExecuteTask(
                    dacpacPath: dacpac,
                    useConnectionStringMode: "true"))
            .Then("task succeeds", r => r.Success)
            .And("uses DACPAC name", r => r.ResolvedName == "MyDatabaseContext")
            .AssertPassed();
    }

    [Scenario("Handles SQL project with complex namespace")]
    [Fact]
    public async Task Handles_complex_namespace_project()
    {
        await Given("SQL project with complex namespace", () => "/path/Org.Unit.SystemData.sqlproj")
            .When("task executes with complex project path", path =>
                ExecuteTask(sqlProjPath: path))
            .Then("task succeeds", r => r.Success)
            .And("uses last part of namespace", r => r.ResolvedName == "SystemDataContext")
            .AssertPassed();
    }

    [Scenario("Handles DACPAC with underscores and numbers")]
    [Fact]
    public async Task Handles_dacpac_with_special_chars()
    {
        await Given("DACPAC with underscores and numbers", () => "/path/Our_Database20251225.dacpac")
            .When("task executes with DACPAC", path =>
                ExecuteTask(dacpacPath: path))
            .Then("task succeeds", r => r.Success)
            .And("humanizes the name", r => r.ResolvedName == "OurDatabaseContext")
            .AssertPassed();
    }

    [Scenario("Handles SQLite connection string")]
    [Fact]
    public async Task Handles_sqlite_connection_string()
    {
        await Given("SQLite connection string", () => "Data Source=/path/to/sample.db")
            .When("task executes with SQLite connection string", connStr =>
                ExecuteTask(connectionString: connStr))
            .Then("task succeeds", r => r.Success)
            .And("extracts filename as database name", r => r.ResolvedName == "SampleContext")
            .AssertPassed();
    }

    [Scenario("Explicit name overrides all other sources")]
    [Fact]
    public async Task Explicit_name_overrides_all()
    {
        await Given("explicit name and all other sources", () =>
                ("MyContext", "/path/Project.sqlproj", "/path/Database.dacpac", "Database=Other;"))
            .When("task executes with all inputs", ctx =>
                ExecuteTask(
                    explicitName: ctx.Item1,
                    sqlProjPath: ctx.Item2,
                    dacpacPath: ctx.Item3,
                    connectionString: ctx.Item4))
            .Then("task succeeds", r => r.Success)
            .And("uses explicit name", r => r.ResolvedName == "MyContext")
            .AssertPassed();
    }
}
