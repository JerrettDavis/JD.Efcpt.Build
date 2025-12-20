using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.Integration;

[Feature("QuerySchemaMetadata task: queries real SQL Server database schema")]
[Collection(nameof(AssemblySetup))]
public sealed class QuerySchemaMetadataIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        MsSqlContainer Container,
        string ConnectionString,
        TestBuildEngine Engine,
        string OutputDir) : IDisposable
    {
        public void Dispose()
        {
            Container.DisposeAsync().AsTask().Wait();
            if (Directory.Exists(OutputDir))
                Directory.Delete(OutputDir, true);
        }
    }

    private sealed record TaskResult(
        TestContext Context,
        QuerySchemaMetadata Task,
        bool Success);

    [Scenario("Queries schema from real SQL Server and produces deterministic fingerprint")]
    [Fact]
    public async Task Queries_schema_and_produces_deterministic_fingerprint()
    {
        await Given("SQL Server with test schema", SetupDatabaseWithSchema)
            .When("execute QuerySchemaMetadata task", ExecuteQuerySchemaMetadata)
            .Then("task succeeds", r => r.Success)
            .And("fingerprint is generated", r => !string.IsNullOrEmpty(r.Task.SchemaFingerprint))
            .And("schema model file exists", r => File.Exists(Path.Combine(r.Context.OutputDir, "schema-model.json")))
            .And(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Identical schema produces identical fingerprint")]
    [Fact]
    public async Task Identical_schema_produces_identical_fingerprint()
    {
        await Given("SQL Server with test schema", SetupDatabaseWithSchema)
            .When("execute task twice", ExecuteTaskTwice)
            .Then("both tasks succeed", r => r.Item1.Success && r.Item2.Success)
            .And("fingerprints are identical", r => r.Item1.Task.SchemaFingerprint == r.Item2.Task.SchemaFingerprint)
            .And(r => r.Item1.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Schema change produces different fingerprint")]
    [Fact]
    public async Task Schema_change_produces_different_fingerprint()
    {
        await Given("SQL Server with initial schema", SetupDatabaseWithSchema)
            .When("execute task, modify schema, execute again", ExecuteTaskModifySchemaExecuteAgain)
            .Then("both tasks succeed", r => r.Item1.Success && r.Item2.Success)
            .And("fingerprints are different", r => r.Item1.Task.SchemaFingerprint != r.Item2.Task.SchemaFingerprint)
            .And(r => r.Item1.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Captures all schema elements: tables, columns, indexes, foreign keys")]
    [Fact]
    public async Task Captures_complete_schema_elements()
    {
        await Given("SQL Server with comprehensive schema", SetupComprehensiveSchema)
            .When("execute QuerySchemaMetadata task", ExecuteQuerySchemaMetadata)
            .Then("task succeeds", r => r.Success)
            .And("schema model contains expected tables", r => VerifySchemaModelContainsTables(r))
            .And(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles empty database gracefully")]
    [Fact]
    public async Task Handles_empty_database_gracefully()
    {
        await Given("SQL Server with empty database", SetupEmptyDatabase)
            .When("execute QuerySchemaMetadata task", ExecuteQuerySchemaMetadata)
            .Then("task succeeds", r => r.Success)
            .And("fingerprint is generated for empty schema", r => !string.IsNullOrEmpty(r.Task.SchemaFingerprint))
            .And(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Fails gracefully with invalid connection string")]
    [Fact]
    public async Task Fails_gracefully_with_invalid_connection_string()
    {
        await Given("invalid connection string", SetupInvalidConnectionString)
            .When("execute QuerySchemaMetadata task", ExecuteQuerySchemaMetadata)
            .Then("task fails", r => !r.Success)
            .And("error is logged", r => r.Context.Engine.Errors.Count > 0)
            .And(r => r.Context.Dispose())
            .AssertPassed();
    }

    // ========== Setup Methods ==========

    private static async Task<TestContext> SetupDatabaseWithSchema()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await container.StartAsync();

        var connectionString = container.GetConnectionString();
        await CreateTestSchema(connectionString);

        var engine = new TestBuildEngine();
        var outputDir = Path.Combine(Path.GetTempPath(), $"efcpt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        return new TestContext(container, connectionString, engine, outputDir);
    }

    private static async Task<TestContext> SetupComprehensiveSchema()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await container.StartAsync();

        var connectionString = container.GetConnectionString();
        await CreateComprehensiveSchema(connectionString);

        var engine = new TestBuildEngine();
        var outputDir = Path.Combine(Path.GetTempPath(), $"efcpt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        return new TestContext(container, connectionString, engine, outputDir);
    }

    private static async Task<TestContext> SetupEmptyDatabase()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await container.StartAsync();

        var connectionString = container.GetConnectionString();
        // Don't create any schema - leave database empty

        var engine = new TestBuildEngine();
        var outputDir = Path.Combine(Path.GetTempPath(), $"efcpt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        return new TestContext(container, connectionString, engine, outputDir);
    }

    private static Task<TestContext> SetupInvalidConnectionString()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        // Don't start the container - connection will fail
        var invalidConnectionString = "Server=invalid;Database=test;User Id=sa;Password=invalid;TrustServerCertificate=true";

        var engine = new TestBuildEngine();
        var outputDir = Path.Combine(Path.GetTempPath(), $"efcpt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        return Task.FromResult(new TestContext(container, invalidConnectionString, engine, outputDir));
    }

    private static async Task CreateTestSchema(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Users (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Username NVARCHAR(100) NOT NULL,
                Email NVARCHAR(255) NOT NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateComprehensiveSchema(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            -- Users table with primary key and unique index
            CREATE TABLE Users (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Username NVARCHAR(100) NOT NULL,
                Email NVARCHAR(255) NOT NULL,
                Age INT NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                CONSTRAINT UQ_Users_Username UNIQUE (Username),
                CONSTRAINT CK_Users_Age CHECK (Age >= 18)
            );

            CREATE INDEX IX_Users_Email ON Users (Email);

            -- Orders table with foreign key
            CREATE TABLE Orders (
                Id INT PRIMARY KEY IDENTITY(1,1),
                UserId INT NOT NULL,
                OrderDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                TotalAmount DECIMAL(18,2) NOT NULL,
                CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
            );

            CREATE INDEX IX_Orders_UserId ON Orders (UserId);
            CREATE INDEX IX_Orders_OrderDate ON Orders (OrderDate DESC);

            -- Products table
            CREATE TABLE Products (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Name NVARCHAR(200) NOT NULL,
                Description NVARCHAR(MAX) NULL,
                Price DECIMAL(18,2) NOT NULL,
                Stock INT NOT NULL DEFAULT 0
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ModifySchema(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "ALTER TABLE Users ADD LastLoginAt DATETIME2 NULL;";
        await command.ExecuteNonQueryAsync();
    }

    // ========== Execute Methods ==========

    private static TaskResult ExecuteQuerySchemaMetadata(TestContext context)
    {
        var task = new QuerySchemaMetadata
        {
            BuildEngine = context.Engine,
            ConnectionString = context.ConnectionString,
            OutputDir = context.OutputDir,
            LogVerbosity = "minimal"
        };

        var success = task.Execute();
        return new TaskResult(context, task, success);
    }

    private static Task<(TaskResult, TaskResult)> ExecuteTaskTwice(TestContext context)
    {
        var result1 = ExecuteQuerySchemaMetadata(context);
        var result2 = ExecuteQuerySchemaMetadata(context);

        return Task.FromResult((result1, result2));
    }

    private static async Task<(TaskResult, TaskResult)> ExecuteTaskModifySchemaExecuteAgain(TestContext context)
    {
        var result1 = ExecuteQuerySchemaMetadata(context);

        // Modify the schema
        await ModifySchema(context.ConnectionString);

        var result2 = ExecuteQuerySchemaMetadata(context);

        return (result1, result2);
    }

    // ========== Verification Methods ==========

    private static bool VerifySchemaModelContainsTables(TaskResult result)
    {
        var schemaModelPath = Path.Combine(result.Context.OutputDir, "schema-model.json");
        if (!File.Exists(schemaModelPath))
            return false;

        var json = File.ReadAllText(schemaModelPath);

        // Verify the JSON contains expected table names
        return json.Contains("Users") &&
               json.Contains("Orders") &&
               json.Contains("Products") &&
               json.Contains("FK_Orders_Users") &&
               json.Contains("CK_Users_Age");
    }
}
