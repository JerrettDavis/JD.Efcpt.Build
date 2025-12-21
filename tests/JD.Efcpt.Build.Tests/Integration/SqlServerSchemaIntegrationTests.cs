using JD.Efcpt.Build.Tasks.Schema;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.Integration;

[Feature("SqlServerSchemaReader: reads and fingerprints SQL Server schema using Testcontainers")]
[Collection(nameof(AssemblySetup))]
public sealed class SqlServerSchemaIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        MsSqlContainer Container,
        string ConnectionString) : IDisposable
    {
        public void Dispose()
        {
            Container.DisposeAsync().AsTask().Wait();
        }
    }

    private sealed record SchemaResult(
        TestContext Context,
        SchemaModel Schema);

    // ========== Setup Methods ==========

    private static async Task<TestContext> SetupEmptyDatabase()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await container.StartAsync();
        var connectionString = container.GetConnectionString();

        return new TestContext(container, connectionString);
    }

    private static async Task<TestContext> SetupSingleTableDatabase()
    {
        var context = await SetupEmptyDatabase();
        await CreateTable(context.ConnectionString, "Users",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL",
            "Email NVARCHAR(255) NULL");

        return context;
    }

    private static async Task<TestContext> SetupDatabaseWithIndexes()
    {
        var context = await SetupEmptyDatabase();
        await CreateTable(context.ConnectionString, "Products",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        await ExecuteSql(context.ConnectionString,
            "CREATE INDEX IX_Products_Name ON dbo.Products (Name)");

        return context;
    }

    private static async Task<TestContext> SetupDatabaseForFingerprinting()
    {
        var context = await SetupEmptyDatabase();
        await CreateTable(context.ConnectionString, "TestTable",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        return context;
    }

    private static async Task<TestContext> SetupDatabaseForChanges()
    {
        var context = await SetupEmptyDatabase();
        await CreateTable(context.ConnectionString, "VersionedTable",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        return context;
    }

    private static async Task<TestContext> SetupDatabaseWithMultipleTables()
    {
        var context = await SetupEmptyDatabase();
        // Create tables in non-alphabetical order
        await CreateTable(context.ConnectionString, "Zebras", "Id INT PRIMARY KEY");
        await CreateTable(context.ConnectionString, "Apples", "Id INT PRIMARY KEY");
        await CreateTable(context.ConnectionString, "Monkeys", "Id INT PRIMARY KEY");

        return context;
    }

    // ========== Execute Methods ==========

    private static SchemaResult ExecuteReadSchema(TestContext context)
    {
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(context.ConnectionString);
        return new SchemaResult(context, schema);
    }

    // ========== Helper Methods ==========

    private static async Task CreateTable(string connectionString, string tableName, params string[] columns)
    {
        var columnDefs = string.Join(", ", columns);
        var sql = $"CREATE TABLE dbo.{tableName} ({columnDefs})";
        await ExecuteSql(connectionString, sql);
    }

    private static async Task ExecuteSql(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    
    private static IEnumerable<TableModel> FilterDefaultTables(IReadOnlyList<TableModel> tables)
    {
        var tablePartials = new List<string>
        {
            "spt_",
            "MSreplication_options"
        };
        return tables.Where(v => tablePartials.All(t => !v.Name.StartsWith(t)));
    }

    // ========== Tests ==========

    [Scenario("Read empty database schema")]
    [Fact]
    public async Task Read_empty_database_schema()
    {
        await Given("SQL Server with empty database", SetupEmptyDatabase)
            .When("read schema", ExecuteReadSchema)
            .Then("schema is not null", r => r.Schema != null)
            .And("no user tables exist", r => !FilterDefaultTables(r.Schema.Tables).Any())
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Read single table schema")]
    [Fact]
    public async Task Read_single_table_schema()
    {
        await Given("SQL Server with Users table", SetupSingleTableDatabase)
            .When("read schema", ExecuteReadSchema)
            .Then("exactly one user table exists", r => FilterDefaultTables(r.Schema.Tables).Count() == 1)
            .And("table schema is dbo", r =>
            {
                var userTable = FilterDefaultTables(r.Schema.Tables).First();
                return userTable.Schema == "dbo";
            })
            .And("table name is Users", r =>
            {
                var userTable = FilterDefaultTables(r.Schema.Tables).First();
                return userTable.Name == "Users";
            })
            .And("has 3 columns", r =>
            {
                var userTable = FilterDefaultTables(r.Schema.Tables).First();
                return userTable.Columns.Count == 3;
            })
            .And("Id column is int and not nullable", r =>
            {
                var userTable = FilterDefaultTables(r.Schema.Tables).First();
                var idColumn = userTable.Columns.First(c => c.Name == "Id");
                return idColumn.DataType == "int" && !idColumn.IsNullable;
            })
            .And("Name column is nvarchar and not nullable", r =>
            {
                var userTable = FilterDefaultTables(r.Schema.Tables).First();
                var nameColumn = userTable.Columns.First(c => c.Name == "Name");
                return nameColumn.DataType == "nvarchar" && !nameColumn.IsNullable;
            })
            .And("Email column is nvarchar and nullable", r =>
            {
                var userTable = FilterDefaultTables(r.Schema.Tables).First();
                var emailColumn = userTable.Columns.First(c => c.Name == "Email");
                return emailColumn.DataType == "nvarchar" && emailColumn.IsNullable;
            })
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Read schema with indexes")]
    [Fact]
    public async Task Read_schema_with_indexes()
    {
        await Given("SQL Server with Products table and index", SetupDatabaseWithIndexes)
            .When("read schema", ExecuteReadSchema)
            .Then("Products table exists", r =>
            {
                var productsTable = FilterDefaultTables(r.Schema.Tables).FirstOrDefault(t => t.Name == "Products");
                return productsTable != null;
            })
            .And("table has at least one index", r =>
            {
                var productsTable = FilterDefaultTables(r.Schema.Tables).First(t => t.Name == "Products");
                return productsTable.Indexes.Count >= 1;
            })
            .And("name index exists", r =>
            {
                var productsTable = FilterDefaultTables(r.Schema.Tables).First(t => t.Name == "Products");
                var nameIndex = productsTable.Indexes.FirstOrDefault(i => i.Name == "IX_Products_Name");
                return nameIndex != null;
                // Note: IsUnique and IsPrimaryKey not available via GetSchema
            })
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    [Scenario("Schema fingerprint is consistent")]
    [Fact]
    public async Task Schema_fingerprint_is_consistent()
    {
        await Given("SQL Server with TestTable", SetupDatabaseForFingerprinting)
            .When("read schema and compute fingerprints twice", ExecuteComputeFingerprintTwice)
            .Then("fingerprints are identical", r => r.Fingerprint1 == r.Fingerprint2)
            .And("fingerprint is not empty", r => !string.IsNullOrEmpty(r.Fingerprint1))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    private static (TestContext Context, string Fingerprint1, string Fingerprint2) ExecuteComputeFingerprintTwice(TestContext context)
    {
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(context.ConnectionString);

        var fingerprint1 = SchemaFingerprinter.ComputeFingerprint(schema);
        var fingerprint2 = SchemaFingerprinter.ComputeFingerprint(schema);

        return (context, fingerprint1, fingerprint2);
    }

    [Scenario("Schema changes produce different fingerprints")]
    [Fact]
    public async Task Schema_changes_produce_different_fingerprints()
    {
        await Given("SQL Server with VersionedTable", SetupDatabaseForChanges)
            .When("read schema, add column, read schema again", ExecuteChangeAndCompare)
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }

    private static async Task<(TestContext Context, string Fingerprint1, string Fingerprint2)> ExecuteChangeAndCompare(TestContext context)
    {
        // Read schema before change
        var reader1 = new SqlServerSchemaReader();
        var schema1 = reader1.ReadSchema(context.ConnectionString);
        var fingerprint1 = SchemaFingerprinter.ComputeFingerprint(schema1);

        // Add a column - this creates a new connection and disposes it
        await ExecuteSql(context.ConnectionString,
            "ALTER TABLE dbo.VersionedTable ADD Description NVARCHAR(500) NULL");

        // Force a fresh connection by creating a new reader
        // This ensures GetSchema retrieves fresh metadata instead of cached data
        var reader2 = new SqlServerSchemaReader();
        var schema2 = reader2.ReadSchema(context.ConnectionString);
        var fingerprint2 = SchemaFingerprinter.ComputeFingerprint(schema2);

        return (context, fingerprint1, fingerprint2);
    }

    [Scenario("Read multiple tables in deterministic order")]
    [Fact]
    public async Task Read_multiple_tables_in_deterministic_order()
    {
        await Given("SQL Server with Zebras, Apples, Monkeys tables", SetupDatabaseWithMultipleTables)
            .When("read schema", ExecuteReadSchema)
            .Then("exactly 3 user tables exist", r => FilterDefaultTables(r.Schema.Tables).Count() == 3)
            .And("tables are sorted alphabetically", r =>
            {
                var userTables = FilterDefaultTables(r.Schema.Tables).ToList();
                return userTables[0].Name == "Apples" &&
                       userTables[1].Name == "Monkeys" &&
                       userTables[2].Name == "Zebras";
            })
            .Finally(r => r.Context.Dispose())
            .AssertPassed();
    }
}
