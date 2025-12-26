using JD.Efcpt.Build.Tasks;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the DbContextNameGenerator utility class.
/// </summary>
[Feature("DbContextNameGenerator: Generates context names from various sources")]
[Collection(nameof(AssemblySetup))]
public sealed class DbContextNameGeneratorTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Generates context name from SQL project path")]
    [Theory]
    [InlineData("/path/to/Database.csproj", "DatabaseContext")]
    [InlineData("/path/to/DatabaseProject.sqlproj", "DatabaseProjectContext")]
    [InlineData("C:\\Projects\\MyDatabase.csproj", "MyDatabaseContext")]
    [InlineData("/projects/Org.Unit.SystemData.sqlproj", "SystemDataContext")]
    [InlineData("/path/to/Sample.Database.sqlproj", "DatabaseContext")]
    public async Task Generates_context_name_from_sql_project(string projectPath, string expectedName)
    {
        await Given("a SQL project path", () => projectPath)
            .When("generating context name from SQL project", DbContextNameGenerator.FromSqlProject)
            .Then("returns expected context name", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Generates context name from DACPAC path")]
    [Theory]
    [InlineData("/path/to/MyDb.dacpac", "MyDbContext")]
    [InlineData("/path/to/Our_Database20251225.dacpac", "OurDatabaseContext")]
    [InlineData("C:\\DACPACs\\Database123.dacpac", "DatabaseContext")]
    [InlineData("/dacpacs/Test_Project_2024.dacpac", "TestProjectContext")]
    [InlineData("/path/sample-db_v2.dacpac", "SampleDbVContext")]
    public async Task Generates_context_name_from_dacpac(string dacpacPath, string expectedName)
    {
        await Given("a DACPAC path", () => dacpacPath)
            .When("generating context name from DACPAC", DbContextNameGenerator.FromDacpac)
            .Then("returns expected context name", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Generates context name from connection string with Database keyword")]
    [Theory]
    [InlineData("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;", "MyDataBaseContext")]
    [InlineData("Database=SampleDb;Server=localhost;", "SampleDbContext")]
    [InlineData("Server=.;Database=AdventureWorks;Integrated Security=true;", "AdventureWorksContext")]
    [InlineData("Db=TestDatabase;Host=localhost;", "TestDatabaseContext")]
    public async Task Generates_context_name_from_connection_string_with_database(string connectionString, string expectedName)
    {
        await Given("a connection string with Database keyword", () => connectionString)
            .When("generating context name from connection string", DbContextNameGenerator.FromConnectionString)
            .Then("returns expected context name", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Generates context name from connection string with Initial Catalog")]
    [Theory]
    [InlineData("Server=myServerAddress;Initial Catalog=myDataBase;User Id=myUsername;Password=myPassword;", "MyDataBaseContext")]
    [InlineData("Initial Catalog=SampleDb;Server=localhost;", "SampleDbContext")]
    public async Task Generates_context_name_from_connection_string_with_initial_catalog(string connectionString, string expectedName)
    {
        await Given("a connection string with Initial Catalog", () => connectionString)
            .When("generating context name from connection string", DbContextNameGenerator.FromConnectionString)
            .Then("returns expected context name", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Generates context name from SQLite connection string with Data Source")]
    [Theory]
    [InlineData("Data Source=sample.db", "SampleContext")]
    [InlineData("Data Source=/path/to/mydb.db", "MydbContext")]
    [InlineData("Data Source=C:\\databases\\test_database.db", "TestDatabaseContext")]
    public async Task Generates_context_name_from_sqlite_connection_string(string connectionString, string expectedName)
    {
        await Given("a SQLite connection string", () => connectionString)
            .When("generating context name from connection string", DbContextNameGenerator.FromConnectionString)
            .Then("returns expected context name", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Returns null for empty or null inputs")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Returns_null_for_empty_sql_project(string? input)
    {
        await Given("an empty or null SQL project path", () => input)
            .When("generating context name from SQL project", DbContextNameGenerator.FromSqlProject)
            .Then("returns null", result => result == null)
            .AssertPassed();
    }

    [Scenario("Returns null for empty or null DACPAC path")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Returns_null_for_empty_dacpac(string? input)
    {
        await Given("an empty or null DACPAC path", () => input)
            .When("generating context name from DACPAC", DbContextNameGenerator.FromDacpac)
            .Then("returns null", result => result == null)
            .AssertPassed();
    }

    [Scenario("Returns null for empty or null connection string")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Returns_null_for_empty_connection_string(string? input)
    {
        await Given("an empty or null connection string", () => input)
            .When("generating context name from connection string", DbContextNameGenerator.FromConnectionString)
            .Then("returns null", result => result == null)
            .AssertPassed();
    }

    [Scenario("Returns null for connection string without database name")]
    [Fact]
    public async Task Returns_null_for_connection_string_without_database_name()
    {
        await Given("a connection string without database name", () => "Server=localhost;User Id=root;Password=password;")
            .When("generating context name from connection string", DbContextNameGenerator.FromConnectionString)
            .Then("returns null", result => result == null)
            .AssertPassed();
    }

    [Scenario("Generate uses SQL project as priority")]
    [Fact]
    public async Task Generate_prioritizes_sql_project()
    {
        var sqlProj = "/path/to/DatabaseProject.sqlproj";
        var dacpac = "/path/to/OtherDatabase.dacpac";
        var connStr = "Database=ThirdDatabase;Server=localhost;";

        await Given("SQL project, DACPAC, and connection string", () => (sqlProj, dacpac, connStr))
            .When("generating context name", ctx =>
                DbContextNameGenerator.Generate(ctx.sqlProj, ctx.dacpac, ctx.connStr))
            .Then("uses SQL project name", result => result == "DatabaseProjectContext")
            .AssertPassed();
    }

    [Scenario("Generate uses DACPAC when SQL project is empty")]
    [Fact]
    public async Task Generate_uses_dacpac_when_no_sql_project()
    {
        var sqlProj = "";
        var dacpac = "/path/to/MyDatabase.dacpac";
        var connStr = "Database=OtherDatabase;Server=localhost;";

        await Given("no SQL project but DACPAC and connection string", () => (sqlProj, dacpac, connStr))
            .When("generating context name", ctx =>
                DbContextNameGenerator.Generate(ctx.sqlProj, ctx.dacpac, ctx.connStr))
            .Then("uses DACPAC name", result => result == "MyDatabaseContext")
            .AssertPassed();
    }

    [Scenario("Generate uses connection string when SQL project and DACPAC are empty")]
    [Fact]
    public async Task Generate_uses_connection_string_when_no_project_or_dacpac()
    {
        var sqlProj = "";
        var dacpac = "";
        var connStr = "Database=FinalDatabase;Server=localhost;";

        await Given("no SQL project or DACPAC but connection string", () => (sqlProj, dacpac, connStr))
            .When("generating context name", ctx =>
                DbContextNameGenerator.Generate(ctx.sqlProj, ctx.dacpac, ctx.connStr))
            .Then("uses connection string database name", result => result == "FinalDatabaseContext")
            .AssertPassed();
    }

    [Scenario("Generate returns default when all inputs are empty")]
    [Fact]
    public async Task Generate_returns_default_when_all_empty()
    {
        await Given("all empty inputs", () => ("", "", ""))
            .When("generating context name", ctx =>
                DbContextNameGenerator.Generate(ctx.Item1, ctx.Item2, ctx.Item3))
            .Then("returns default MyDbContext", result => result == "MyDbContext")
            .AssertPassed();
    }

    [Scenario("Removes trailing digits from DACPAC names")]
    [Theory]
    [InlineData("/path/to/Database20251225.dacpac", "DatabaseContext")]
    [InlineData("/path/to/MyDb123456.dacpac", "MyDbContext")]
    [InlineData("/path/to/Test_2024_v1.dacpac", "TestVContext")]
    public async Task Removes_trailing_digits(string dacpacPath, string expectedName)
    {
        await Given("a DACPAC with trailing digits", () => dacpacPath)
            .When("generating context name from DACPAC", DbContextNameGenerator.FromDacpac)
            .Then("removes trailing digits", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Handles names with underscores")]
    [Theory]
    [InlineData("/path/to/my_database.sqlproj", "MyDatabaseContext")]
    [InlineData("/path/to/test_project_name.csproj", "TestProjectNameContext")]
    [InlineData("/path/to/sample_db.dacpac", "SampleDbContext")]
    public async Task Handles_underscores(string path, string expectedName)
    {
        await Given("a path with underscores", () => path)
            .When("generating context name from SQL project", DbContextNameGenerator.FromSqlProject)
            .Then("converts underscores to PascalCase", result => result == expectedName)
            .AssertPassed();
    }

    [Scenario("Ensures Context suffix is present")]
    [Theory]
    [InlineData("/path/to/Database.sqlproj", "DatabaseContext")]
    [InlineData("/path/to/DatabaseContext.sqlproj", "DatabaseContext")] // Doesn't duplicate Context suffix
    public async Task Ensures_context_suffix(string projectPath, string expectedName)
    {
        await Given("a SQL project path", () => projectPath)
            .When("generating context name from SQL project", DbContextNameGenerator.FromSqlProject)
            .Then("ensures Context suffix", result => result == expectedName)
            .AssertPassed();
    }
}
