using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using MySqlConnector;
using Testcontainers.MySql;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.Integration;

[Feature("MySqlSchemaReader: reads and fingerprints MySQL schema using Testcontainers")]
[Collection(nameof(AssemblySetup))]
public sealed class MySqlSchemaIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        MySqlContainer Container,
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
        var container = new MySqlBuilder()
            .WithImage("mysql:8.0")
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
        await using var connection = new MySqlConnection(ctx.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE customers (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE products (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                price DECIMAL(10, 2) NOT NULL,
                stock INT DEFAULT 0,
                INDEX idx_products_name (name)
            );

            CREATE TABLE order_items (
                id INT AUTO_INCREMENT PRIMARY KEY,
                customer_id INT NOT NULL,
                product_id INT NOT NULL,
                quantity INT NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id),
                FOREIGN KEY (product_id) REFERENCES products(id),
                INDEX idx_order_items_customer (customer_id)
            );
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AddColumn(TestContext ctx)
    {
        await using var connection = new MySqlConnection(ctx.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "ALTER TABLE customers ADD COLUMN phone VARCHAR(20)";
        await command.ExecuteNonQueryAsync();
    }

    // ========== Execute Methods ==========

    private static SchemaResult ExecuteReadSchema(TestContext ctx)
    {
        var reader = new MySqlSchemaReader();
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static SchemaResult ExecuteReadSchemaViaFactory(TestContext ctx)
    {
        var reader = DatabaseProviderFactory.CreateSchemaReader("mysql");
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static SchemaResult ExecuteReadSchemaViaMariaDbAlias(TestContext ctx)
    {
        var reader = DatabaseProviderFactory.CreateSchemaReader("mariadb");
        var schema = reader.ReadSchema(ctx.ConnectionString);
        return new SchemaResult(ctx, schema);
    }

    private static FingerprintResult ExecuteComputeFingerprint(TestContext ctx)
    {
        var reader = new MySqlSchemaReader();
        var schema1 = reader.ReadSchema(ctx.ConnectionString);
        var schema2 = reader.ReadSchema(ctx.ConnectionString);
        var fp1 = SchemaFingerprinter.ComputeFingerprint(schema1);
        var fp2 = SchemaFingerprinter.ComputeFingerprint(schema2);
        return new FingerprintResult(ctx, fp1, fp2);
    }

    private static async Task<FingerprintResult> ExecuteComputeFingerprintWithChange(TestContext ctx)
    {
        var reader = new MySqlSchemaReader();
        var schema1 = reader.ReadSchema(ctx.ConnectionString);
        var fp1 = SchemaFingerprinter.ComputeFingerprint(schema1);

        await AddColumn(ctx);

        var schema2 = reader.ReadSchema(ctx.ConnectionString);
        var fp2 = SchemaFingerprinter.ComputeFingerprint(schema2);

        return new FingerprintResult(ctx, fp1, fp2);
    }

    // ========== Tests ==========

    [Scenario("Reads tables from MySQL database")]
    [Fact]
    public async Task Reads_tables_from_database()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("returns all tables", r => r.Schema.Tables.Count == 3)
            .And("contains customers table", r => r.Schema.Tables.Any(t => t.Name == "customers"))
            .And("contains products table", r => r.Schema.Tables.Any(t => t.Name == "products"))
            .And("contains order_items table", r => r.Schema.Tables.Any(t => t.Name == "order_items"))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Reads columns with correct metadata")]
    [Fact]
    public async Task Reads_columns_with_metadata()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("customers table has correct column count", r =>
                r.Schema.Tables.First(t => t.Name == "customers").Columns.Count == 4)
            .And("name column has correct type", r =>
                r.Schema.Tables.First(t => t.Name == "customers").Columns
                    .Any(c => c.Name == "name" && c.DataType.Contains("varchar", StringComparison.OrdinalIgnoreCase)))
            .And("price column is decimal", r =>
                r.Schema.Tables.First(t => t.Name == "products").Columns
                    .Any(c => c.Name == "price" && c.DataType.Contains("decimal", StringComparison.OrdinalIgnoreCase)))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Reads indexes from MySQL database")]
    [Fact]
    public async Task Reads_indexes_from_database()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("products table has indexes", r =>
                r.Schema.Tables.First(t => t.Name == "products").Indexes.Count > 0)
            .And("order_items table has indexes", r =>
                r.Schema.Tables.First(t => t.Name == "order_items").Indexes.Count > 0)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Identifies primary key indexes")]
    [Fact]
    public async Task Identifies_primary_key_indexes()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is read", ExecuteReadSchema)
            .Then("customers table has PRIMARY index", r =>
                r.Schema.Tables.First(t => t.Name == "customers").Indexes
                    .Any(i => i.Name.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase) && i.IsPrimaryKey))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Computes deterministic fingerprint")]
    [Fact]
    public async Task Computes_deterministic_fingerprint()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("fingerprint computed twice", ExecuteComputeFingerprint)
            .Then("fingerprints are equal", r => r.Fingerprint1 == r.Fingerprint2)
            .And("fingerprint is not empty", r => !string.IsNullOrEmpty(r.Fingerprint1))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint changes when schema changes")]
    [Fact]
    public async Task Fingerprint_changes_when_schema_changes()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema is modified", ExecuteComputeFingerprintWithChange)
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses factory to create reader")]
    [Fact]
    public async Task Factory_creates_correct_reader()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema read via factory", ExecuteReadSchemaViaFactory)
            .Then("returns valid schema", r => r.Schema.Tables.Count == 3)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("MariaDB alias works")]
    [Fact]
    public async Task Mariadb_alias_works()
    {
        await Given("a MySQL container with test schema", SetupDatabaseWithSchema)
            .When("schema read via mariadb alias", ExecuteReadSchemaViaMariaDbAlias)
            .Then("returns valid schema", r => r.Schema.Tables.Count == 3)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }
}
