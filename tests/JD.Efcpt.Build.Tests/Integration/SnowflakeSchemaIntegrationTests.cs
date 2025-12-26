using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using JD.Efcpt.Build.Tests.Infrastructure;
using Snowflake.Data.Client;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.Integration;

/// <summary>
/// Integration tests for SnowflakeSchemaReader using LocalStack Snowflake emulator.
/// These tests verify that the reader correctly reads schema from a Snowflake-compatible database.
/// </summary>
/// <remarks>
/// <para>
/// LocalStack Snowflake requires a LOCALSTACK_AUTH_TOKEN environment variable.
/// Tests will be skipped if this token is not available.
/// </para>
/// <para>
/// To run these tests locally:
/// 1. Set LOCALSTACK_AUTH_TOKEN environment variable with a valid LocalStack Pro token
/// 2. Ensure Docker is running
/// 3. Run the tests
/// </para>
/// </remarks>
[Feature("SnowflakeSchemaReader: reads and fingerprints Snowflake schema using LocalStack")]
[Collection(nameof(AssemblySetup))]
public sealed class SnowflakeSchemaIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static readonly string? LocalStackAuthToken =
        Environment.GetEnvironmentVariable("LOCALSTACK_AUTH_TOKEN");

    private static bool HasLocalStackToken => !string.IsNullOrEmpty(LocalStackAuthToken);

    private sealed record TestContext(
        IContainer Container,
        string ConnectionString) : IDisposable
    {
        public void Dispose()
        {
            Container.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private sealed record SchemaResult(TestContext Context, SchemaModel Schema);
    private sealed record FingerprintResult(TestContext Context, string Fingerprint1, string Fingerprint2);

    // ========== Setup Methods ==========

    private static async Task<TestContext> SetupEmptyDatabase()
    {
        // LocalStack Snowflake uses port 4566 and requires auth token
        var container = new ContainerBuilder()
            .WithImage("localstack/snowflake:latest")
            .WithPortBinding(4566, true)
            .WithEnvironment("LOCALSTACK_AUTH_TOKEN", LocalStackAuthToken!)
            .WithEnvironment("SF_DEFAULT_USER", "test")
            .WithEnvironment("SF_DEFAULT_PASSWORD", "test")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort(4566)
                    .ForPath("/_localstack/health")))
            .Build();

        await container.StartAsync();

        var port = container.GetMappedPublicPort(4566);
        var host = container.Hostname;

        // LocalStack Snowflake connection string format
        // Note: LocalStack uses a special endpoint format
        var connectionString = $"account=test;host={host};port={port};user=test;password=test;db=TEST_DB;schema=PUBLIC;warehouse=TEST_WH;insecuremode=true";

        return new TestContext(container, connectionString);
    }

    private static async Task<TestContext> SetupDatabaseWithSchema()
    {
        var ctx = await SetupEmptyDatabase();

        // Wait for the container to be fully ready
        await Task.Delay(2000);

        await CreateTestSchema(ctx);
        return ctx;
    }

    private static async Task CreateTestSchema(TestContext ctx)
    {
        await using var connection = new SnowflakeDbConnection(ctx.ConnectionString);
        await connection.OpenAsync();

        // Create database and schema first
        var setupStatements = new[]
        {
            "CREATE DATABASE IF NOT EXISTS TEST_DB",
            "USE DATABASE TEST_DB",
            "CREATE SCHEMA IF NOT EXISTS PUBLIC",
            "USE SCHEMA PUBLIC",
            "CREATE WAREHOUSE IF NOT EXISTS TEST_WH WITH WAREHOUSE_SIZE = 'XSMALL'"
        };

        foreach (var sql in setupStatements)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch
            {
                // Some commands may fail in emulator, continue
            }
        }

        // Create test tables
        var tableStatements = new[]
        {
            """
            CREATE TABLE IF NOT EXISTS customers (
                id INTEGER AUTOINCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP_NTZ DEFAULT CURRENT_TIMESTAMP()
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS products (
                id INTEGER AUTOINCREMENT PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                price NUMBER(10, 2) NOT NULL,
                stock INTEGER DEFAULT 0
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS orders (
                id INTEGER AUTOINCREMENT PRIMARY KEY,
                customer_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL
            )
            """
        };

        foreach (var sql in tableStatements)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task AddColumn(TestContext ctx)
    {
        await using var connection = new SnowflakeDbConnection(ctx.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "ALTER TABLE customers ADD COLUMN phone VARCHAR(20)";
        await command.ExecuteNonQueryAsync();
    }

    // ========== Execute Methods ==========

    private static SchemaResult ExecuteReadSchema(TestContext ctx)
    {
        var reader = new SnowflakeSchemaReader();
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static SchemaResult ExecuteReadSchemaViaFactory(TestContext ctx)
    {
        var reader = DatabaseProviderFactory.CreateSchemaReader("snowflake");
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static SchemaResult ExecuteReadSchemaViaSfAlias(TestContext ctx)
    {
        var reader = DatabaseProviderFactory.CreateSchemaReader("sf");
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static FingerprintResult ExecuteComputeFingerprint(TestContext ctx)
    {
        var reader = new SnowflakeSchemaReader();
        var schema1 = reader.ReadSchema(ctx.ConnectionString);
        var schema2 = reader.ReadSchema(ctx.ConnectionString);
        var fp1 = SchemaFingerprinter.ComputeFingerprint(schema1);
        var fp2 = SchemaFingerprinter.ComputeFingerprint(schema2);
        return new FingerprintResult(ctx, fp1, fp2);
    }

    private static async Task<FingerprintResult> ExecuteComputeFingerprintWithChange(TestContext ctx)
    {
        var reader = new SnowflakeSchemaReader();
        var schema1 = reader.ReadSchema(ctx.ConnectionString);
        var fp1 = SchemaFingerprinter.ComputeFingerprint(schema1);

        await AddColumn(ctx);

        var schema2 = reader.ReadSchema(ctx.ConnectionString);
        var fp2 = SchemaFingerprinter.ComputeFingerprint(schema2);

        return new FingerprintResult(ctx, fp1, fp2);
    }

    // ========== Tests ==========

    [Scenario("Reads tables from Snowflake database")]
    [SkippableFact]
    public async Task Reads_tables_from_database()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("returns test tables", r => r.Schema.Tables.Count >= 3)
            .And("contains customers table", r => r.Schema.Tables.Any(t => t.Name.Equals("CUSTOMERS", StringComparison.OrdinalIgnoreCase)))
            .And("contains products table", r => r.Schema.Tables.Any(t => t.Name.Equals("PRODUCTS", StringComparison.OrdinalIgnoreCase)))
            .And("contains orders table", r => r.Schema.Tables.Any(t => t.Name.Equals("ORDERS", StringComparison.OrdinalIgnoreCase)))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Reads columns with correct metadata")]
    [SkippableFact]
    public async Task Reads_columns_with_metadata()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("customers table has correct column count", r =>
                r.Schema.Tables.First(t => t.Name.Equals("CUSTOMERS", StringComparison.OrdinalIgnoreCase)).Columns.Count() == 4)
            .And("products table has correct column count", r =>
                r.Schema.Tables.First(t => t.Name.Equals("PRODUCTS", StringComparison.OrdinalIgnoreCase)).Columns.Count() == 4)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Computes deterministic fingerprint")]
    [SkippableFact]
    public async Task Computes_deterministic_fingerprint()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("fingerprint computed twice", ExecuteComputeFingerprint)
            .Then("fingerprints are equal", r => string.Equals(r.Fingerprint1, r.Fingerprint2, StringComparison.Ordinal))
            .And("fingerprint is not empty", r => !string.IsNullOrEmpty(r.Fingerprint1))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint changes when schema changes")]
    [SkippableFact]
    public async Task Fingerprint_changes_when_schema_changes()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("schema is modified", ExecuteComputeFingerprintWithChange)
            .Then("fingerprints are different", r => !string.Equals(r.Fingerprint1, r.Fingerprint2, StringComparison.Ordinal))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses factory to create reader")]
    [SkippableFact]
    public async Task Factory_creates_correct_reader()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("schema read via factory", ExecuteReadSchemaViaFactory)
            .Then("returns valid schema", r => r.Schema.Tables.Count >= 3)
            .And("contains customers table", r => r.Schema.Tables.Any(t => t.Name.Equals("CUSTOMERS", StringComparison.OrdinalIgnoreCase)))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("sf alias works")]
    [SkippableFact]
    public async Task Sf_alias_works()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("schema read via sf alias", ExecuteReadSchemaViaSfAlias)
            .Then("returns valid schema", r => r.Schema.Tables.Count >= 3)
            .And("contains customers table", r => r.Schema.Tables.Any(t => t.Name.Equals("CUSTOMERS", StringComparison.OrdinalIgnoreCase)))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Excludes INFORMATION_SCHEMA")]
    [SkippableFact]
    public async Task Excludes_information_schema()
    {
        Skip.IfNot(HasLocalStackToken, "LOCALSTACK_AUTH_TOKEN not set - skipping Snowflake integration tests");

        await Given("a Snowflake container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("no INFORMATION_SCHEMA tables included", r =>
                !r.Schema.Tables.Any(t => t.Schema.Equals("INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase)))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }
}
