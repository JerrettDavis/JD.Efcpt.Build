using JD.Efcpt.Build.Tasks.Schema;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace JD.Efcpt.Build.Tests.Integration;

/// <summary>
/// Integration tests for SQL Server schema reading and fingerprinting using Testcontainers.
/// </summary>
[Collection(nameof(AssemblySetup))]
public sealed class SqlServerSchemaIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        // Start SQL Server 2022 container
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("P@ssw0rd123!")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public void ReadEmptyDatabaseSchema()
    {
        // Act
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Assert
        Assert.NotNull(schema);
        Assert.Empty(schema.Tables); // Fresh database should have no user tables
    }

    [Fact]
    public async Task ReadSingleTableSchema()
    {
        // Arrange
        await CreateTable(_connectionString!, "Users",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL",
            "Email NVARCHAR(255) NULL");

        // Act
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Assert
        Assert.Single(schema.Tables);
        var table = schema.Tables[0];
        Assert.Equal("dbo", table.Schema);
        Assert.Equal("Users", table.Name);
        Assert.Equal(3, table.Columns.Count);

        // Verify columns
        var idColumn = table.Columns.First(c => c.Name == "Id");
        Assert.Equal("int", idColumn.DataType);
        Assert.False(idColumn.IsNullable);

        var nameColumn = table.Columns.First(c => c.Name == "Name");
        Assert.Equal("nvarchar", nameColumn.DataType);
        Assert.False(nameColumn.IsNullable);

        var emailColumn = table.Columns.First(c => c.Name == "Email");
        Assert.Equal("nvarchar", emailColumn.DataType);
        Assert.True(emailColumn.IsNullable);
    }

    [Fact]
    public async Task ReadSchemaWithIndexes()
    {
        // Arrange
        await CreateTable(_connectionString!, "Products",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        await ExecuteSql(_connectionString!,
            "CREATE INDEX IX_Products_Name ON dbo.Products (Name)");

        // Act
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Assert
        var table = schema.Tables[0];
        Assert.True(table.Indexes.Count >= 1); // At least the index we created (PK creates clustered index)

        var nameIndex = table.Indexes.FirstOrDefault(i => i.Name == "IX_Products_Name");
        Assert.NotNull(nameIndex);
        Assert.False(nameIndex!.IsUnique);
        Assert.False(nameIndex.IsPrimaryKey);
    }

    [Fact]
    public async Task ReadSchemaWithForeignKeys()
    {
        // Arrange
        await CreateTable(_connectionString!, "Customers",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        await CreateTable(_connectionString!, "Orders",
            "Id INT PRIMARY KEY",
            "CustomerId INT NOT NULL",
            "OrderDate DATETIME NOT NULL");

        await ExecuteSql(_connectionString!,
            "ALTER TABLE dbo.Orders ADD CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)");

        // Act
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Assert
        var ordersTable = schema.Tables.First(t => t.Name == "Orders");
        var fkConstraint = ordersTable.Constraints.FirstOrDefault(c => c.Type == ConstraintType.ForeignKey);
        Assert.NotNull(fkConstraint);
        Assert.Equal("FK_Orders_Customers", fkConstraint!.Name);
        Assert.NotNull(fkConstraint.ForeignKey);
        Assert.Equal("dbo", fkConstraint.ForeignKey!.ReferencedSchema);
        Assert.Equal("Customers", fkConstraint.ForeignKey.ReferencedTable);
    }

    [Fact]
    public async Task ReadSchemaWithCheckConstraints()
    {
        // Arrange
        await CreateTable(_connectionString!, "Employees",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL",
            "Age INT NOT NULL");

        await ExecuteSql(_connectionString!,
            "ALTER TABLE dbo.Employees ADD CONSTRAINT CK_Employees_Age CHECK (Age >= 18 AND Age <= 120)");

        // Act
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Assert
        var table = schema.Tables.First(t => t.Name == "Employees");
        var checkConstraint = table.Constraints.FirstOrDefault(c => c.Type == ConstraintType.Check);
        Assert.NotNull(checkConstraint);
        Assert.Equal("CK_Employees_Age", checkConstraint!.Name);
        Assert.NotNull(checkConstraint.CheckExpression);
        Assert.Contains("Age", checkConstraint.CheckExpression);
    }

    [Fact]
    public async Task SchemaFingerprintIsConsistent()
    {
        // Arrange
        await CreateTable(_connectionString!, "TestTable",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Act
        var fingerprint1 = SchemaFingerprinter.ComputeFingerprint(schema);
        var fingerprint2 = SchemaFingerprinter.ComputeFingerprint(schema);

        // Assert
        Assert.Equal(fingerprint1, fingerprint2);
        Assert.NotEmpty(fingerprint1);
    }

    [Fact]
    public async Task SchemaChangesProduceDifferentFingerprints()
    {
        // Arrange - Initial schema
        await CreateTable(_connectionString!, "VersionedTable",
            "Id INT PRIMARY KEY",
            "Name NVARCHAR(100) NOT NULL");

        var reader = new SqlServerSchemaReader();
        var schema1 = reader.ReadSchema(_connectionString!);
        var fingerprint1 = SchemaFingerprinter.ComputeFingerprint(schema1);

        // Act - Add a column
        await ExecuteSql(_connectionString!,
            "ALTER TABLE dbo.VersionedTable ADD Description NVARCHAR(500) NULL");

        var schema2 = reader.ReadSchema(_connectionString!);
        var fingerprint2 = SchemaFingerprinter.ComputeFingerprint(schema2);

        // Assert
        Assert.NotEqual(fingerprint1, fingerprint2);
    }

    [Fact]
    public async Task ReadMultipleTablesInDeterministicOrder()
    {
        // Arrange - Create tables in non-alphabetical order
        await CreateTable(_connectionString!, "Zebras", "Id INT PRIMARY KEY");
        await CreateTable(_connectionString!, "Apples", "Id INT PRIMARY KEY");
        await CreateTable(_connectionString!, "Monkeys", "Id INT PRIMARY KEY");

        // Act
        var reader = new SqlServerSchemaReader();
        var schema = reader.ReadSchema(_connectionString!);

        // Assert - SchemaModel.Create should sort tables
        Assert.Equal(3, schema.Tables.Count);
        Assert.Equal("Apples", schema.Tables[0].Name);
        Assert.Equal("Monkeys", schema.Tables[1].Name);
        Assert.Equal("Zebras", schema.Tables[2].Name);
    }

    // Helper methods

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
}
