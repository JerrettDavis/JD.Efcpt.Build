using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using Npgsql;
using Testcontainers.PostgreSql;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.Integration;

[Feature("PostgreSqlSchemaReader: reads and fingerprints PostgreSQL schema using Testcontainers")]
[Collection(nameof(AssemblySetup))]
public sealed class PostgreSqlSchemaIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        PostgreSqlContainer Container,
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
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await container.StartAsync();
        return new TestContext(container, container.GetConnectionString());
    }

    private static async Task<TestContext> SetupDatabaseWithSchema()
    {
        var ctx = await SetupEmptyDatabase();
        await CreateTestSchema(ctx);
        return ctx;
    }

    private static async Task CreateTestSchema(TestContext ctx)
    {
        await using var connection = new NpgsqlConnection(ctx.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE orders (
                id SERIAL PRIMARY KEY,
                user_id INTEGER NOT NULL REFERENCES users(id),
                total DECIMAL(10, 2) NOT NULL,
                status VARCHAR(50) DEFAULT 'pending',
                order_date DATE NOT NULL
            );

            CREATE INDEX idx_orders_user_id ON orders(user_id);
            CREATE INDEX idx_orders_status ON orders(status);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AddColumn(TestContext ctx)
    {
        await using var connection = new NpgsqlConnection(ctx.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "ALTER TABLE users ADD COLUMN phone VARCHAR(20)";
        await command.ExecuteNonQueryAsync();
    }

    // ========== Execute Methods ==========

    private static SchemaResult ExecuteReadSchema(TestContext ctx)
    {
        var reader = new PostgreSqlSchemaReader();
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static SchemaResult ExecuteReadSchemaViaFactory(TestContext ctx)
    {
        var reader = DatabaseProviderFactory.CreateSchemaReader("postgres");
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static FingerprintResult ExecuteComputeFingerprint(TestContext ctx)
    {
        var reader = new PostgreSqlSchemaReader();
        var schema1 = reader.ReadSchema(ctx.ConnectionString);
        var schema2 = reader.ReadSchema(ctx.ConnectionString);
        var fp1 = SchemaFingerprinter.ComputeFingerprint(schema1);
        var fp2 = SchemaFingerprinter.ComputeFingerprint(schema2);
        return new FingerprintResult(ctx, fp1, fp2);
    }

    private static async Task<FingerprintResult> ExecuteComputeFingerprintWithChange(TestContext ctx)
    {
        var reader = new PostgreSqlSchemaReader();
        var schema1 = reader.ReadSchema(ctx.ConnectionString);
        var fp1 = SchemaFingerprinter.ComputeFingerprint(schema1);

        await AddColumn(ctx);

        var schema2 = reader.ReadSchema(ctx.ConnectionString);
        var fp2 = SchemaFingerprinter.ComputeFingerprint(schema2);

        return new FingerprintResult(ctx, fp1, fp2);
    }

    // ========== Tests ==========

    [Scenario("Reads tables from PostgreSQL database")]
    [Fact]
    public async Task Reads_tables_from_database()
    {
        await Given("a PostgreSQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("returns both tables", r => r.Schema.Tables.Count == 2)
            .And("contains users table", r => r.Schema.Tables.Any(t => t.Name == "users"))
            .And("contains orders table", r => r.Schema.Tables.Any(t => t.Name == "orders"))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Reads columns with correct metadata")]
    [Fact]
    public async Task Reads_columns_with_metadata()
    {
        await Given("a PostgreSQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("users table has correct column count", r =>
                r.Schema.Tables.First(t => t.Name == "users").Columns.Count == 4)
            .And("username column has correct type", r =>
                r.Schema.Tables.First(t => t.Name == "users").Columns
                    .Any(c => c.Name == "username" && c.DataType.Contains("character varying")))
            .And("email column is present", r =>
                r.Schema.Tables.First(t => t.Name == "users").Columns.Any(c => c.Name == "email"))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Reads indexes from PostgreSQL database")]
    [Fact]
    public async Task Reads_indexes_from_database()
    {
        await Given("a PostgreSQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("orders table has indexes", r =>
                r.Schema.Tables.First(t => t.Name == "orders").Indexes.Count > 0)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Computes deterministic fingerprint")]
    [Fact]
    public async Task Computes_deterministic_fingerprint()
    {
        await Given("a PostgreSQL container with test schema", SetupDatabaseWithSchema)
            .When("fingerprint computed twice", ExecuteComputeFingerprint)
            .Then("fingerprints are equal", r => string.Equals(r.Fingerprint1, r.Fingerprint2, StringComparison.Ordinal))
            .And("fingerprint is not empty", r => !string.IsNullOrEmpty(r.Fingerprint1))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint changes when schema changes")]
    [Fact]
    public async Task Fingerprint_changes_when_schema_changes()
    {
        await Given("a PostgreSQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is modified", ExecuteComputeFingerprintWithChange)
            .Then("fingerprints are different", r => !string.Equals(r.Fingerprint1, r.Fingerprint2, StringComparison.Ordinal))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses factory to create reader")]
    [Fact]
    public async Task Factory_creates_correct_reader()
    {
        await Given("a PostgreSQL container with test schema", SetupDatabaseWithSchema)
            .When("schema read via factory", ExecuteReadSchemaViaFactory)
            .Then("returns valid schema", r => r.Schema.Tables.Count == 2)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }
}
