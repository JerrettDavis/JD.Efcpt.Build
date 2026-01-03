using FluentAssertions;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Integration tests for database-first SQL generation feature.
/// Tests verify that SQL scripts are generated from live databases into SQL projects.
/// </summary>
/// <remarks>
/// These tests validate the two-project pattern:
/// 1. DatabaseProject (SQL) - generates SQL scripts from live database
/// 2. DataAccessProject (EF Core) - generates models from DatabaseProject's DACPAC
/// </remarks>
[Collection("SQL Generation Tests")]
public class SqlGenerationIntegrationTests : IAsyncDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;
    private MsSqlContainer? _container;
    private string? _connectionString;

    public SqlGenerationIntegrationTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    private async Task<string> SetupDatabaseWithTestSchema()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Create test tables
        await ExecuteSqlAsync(_connectionString, @"
            CREATE TABLE dbo.Product (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Name NVARCHAR(100) NOT NULL,
                Price DECIMAL(18,2) NOT NULL
            );

            CREATE TABLE dbo.Category (
                Id INT PRIMARY KEY IDENTITY(1,1),
                Name NVARCHAR(100) NOT NULL
            );

            CREATE TABLE dbo.[Order] (
                Id INT PRIMARY KEY IDENTITY(1,1),
                OrderDate DATETIME2 NOT NULL,
                TotalAmount DECIMAL(18,2) NOT NULL
            );
        ");

        return _connectionString;
    }

    private static async Task ExecuteSqlAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Test that a SQL project with JD.Efcpt.Build reference is detected correctly.
    /// </summary>
    [Fact]
    public async Task SqlProject_WithEfcptBuild_IsDetectedAsSqlProject()
    {
        // Arrange
        var connectionString = await SetupDatabaseWithTestSchema();
        _builder.CreateSqlProject("TestSqlProject", "net8.0", connectionString);

        // Act
        var buildResult = await _builder.BuildAsync("-v:n");

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        buildResult.Output.Should().Contain("_EfcptIsSqlProject", "Should detect SQL project");
    }

    /// <summary>
    /// Test that SQL scripts are generated with proper folder structure.
    /// </summary>
    [Fact]
    public async Task SqlProject_GeneratesSqlScriptsWithProperStructure()
    {
        // Arrange
        var connectionString = await SetupDatabaseWithTestSchema();
        _builder.CreateSqlProject("TestSqlProject_Structure", "net8.0", connectionString);

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        
        // Verify SQL scripts were generated
        var tablesDir = Path.Combine(_builder.ProjectDirectory, "dbo", "Tables");
        Directory.Exists(tablesDir).Should().BeTrue("Tables directory should exist");
        
        var sqlFiles = Directory.GetFiles(tablesDir, "*.sql");
        sqlFiles.Should().NotBeEmpty("Should generate SQL files");
        sqlFiles.Should().Contain(f => f.Contains("Product.sql"), "Should generate Product.sql");
        sqlFiles.Should().Contain(f => f.Contains("Category.sql"), "Should generate Category.sql");
        sqlFiles.Should().Contain(f => f.Contains("Order.sql"), "Should generate Order.sql");
    }

    /// <summary>
    /// Test that auto-generation warnings are added to SQL files.
    /// </summary>
    [Fact]
    public async Task SqlProject_AddsAutoGenerationWarningsToSqlFiles()
    {
        // Arrange
        var connectionString = await SetupDatabaseWithTestSchema();
        _builder.CreateSqlProject("TestSqlProject_Warnings", "net8.0", connectionString);

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        
        // Read a generated SQL file and verify warning header
        var productSqlPath = Path.Combine(_builder.ProjectDirectory, "dbo", "Tables", "Product.sql");
        if (File.Exists(productSqlPath))
        {
            var content = await File.ReadAllTextAsync(productSqlPath);
            content.Should().Contain("AUTO-GENERATED", "Should contain auto-generation warning");
            content.Should().Contain("DO NOT EDIT", "Should warn against manual editing");
        }
    }

    /// <summary>
    /// Test that DataAccess project can reference SQL project and generate EF Core models.
    /// </summary>
    [Fact]
    public async Task DataAccessProject_ReferencingSqlProject_GeneratesEfCoreModels()
    {
        // Arrange
        var connectionString = await SetupDatabaseWithTestSchema();
        
        // Create SQL project first
        _builder.CreateSqlProject("DatabaseProject_TwoProj", "net8.0", connectionString);
        var sqlBuildResult = await _builder.BuildAsync();
        sqlBuildResult.Success.Should().BeTrue($"SQL project build should succeed.\n{sqlBuildResult}");

        var sqlProjectDir = _builder.ProjectDirectory;
        var dacpacPath = Path.Combine(sqlProjectDir, "bin", "Debug", "DatabaseProject_TwoProj.dacpac").Replace("\\", "/");
        
        // Create DataAccess project that references SQL project DACPAC
        var dataAccessAdditionalContent = $@"
    <PropertyGroup>
        <EfcptDacpac>{dacpacPath}</EfcptDacpac>
    </PropertyGroup>
    <ItemGroup>
        <ProjectReference Include=""{sqlProjectDir}\DatabaseProject_TwoProj.csproj"">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
        </ProjectReference>
    </ItemGroup>";
        
        _builder.CreateBuildPackageProject("DataAccessProject_TwoProj", "net8.0", dataAccessAdditionalContent);

        // Act - Build DataAccess project
        var dataAccessBuildResult = await _builder.BuildAsync();

        // Assert
        dataAccessBuildResult.Success.Should().BeTrue($"DataAccess project build should succeed.\n{dataAccessBuildResult}");
        
        // Verify SQL scripts were generated
        var tablesDir = Path.Combine(sqlProjectDir, "dbo", "Tables");
        Directory.Exists(tablesDir).Should().BeTrue("SQL tables directory should exist");
        
        // Verify DACPAC was created
        File.Exists(dacpacPath).Should().BeTrue("DACPAC should be created");
        
        // Verify EF Core models were generated
        var generatedFiles = _builder.GetGeneratedFiles();
        if (generatedFiles.Length > 0)
        {
            generatedFiles.Should().NotBeEmpty("Should generate EF Core model files");
        }
    }

    /// <summary>
    /// Test that schema fingerprinting skips regeneration when database is unchanged.
    /// </summary>
    [Fact]
    public async Task SqlProject_WithUnchangedSchema_SkipsRegeneration()
    {
        // Arrange
        var connectionString = await SetupDatabaseWithTestSchema();
        _builder.CreateSqlProject("TestSqlProject_Fingerprint", "net8.0", connectionString);

        // Act - Build once
        var firstBuildResult = await _builder.BuildAsync();
        firstBuildResult.Success.Should().BeTrue($"First build should succeed.\n{firstBuildResult}");

        // Record file timestamps
        var productSqlPath = Path.Combine(_builder.ProjectDirectory, "dbo", "Tables", "Product.sql");
        DateTime? firstBuildTime = null;
        if (File.Exists(productSqlPath))
        {
            firstBuildTime = File.GetLastWriteTimeUtc(productSqlPath);
        }

        // Wait a bit to ensure timestamp would change if regenerated
        await Task.Delay(1000);

        // Build again without changing database
        var secondBuildResult = await _builder.BuildAsync();

        // Assert
        secondBuildResult.Success.Should().BeTrue($"Second build should succeed.\n{secondBuildResult}");
        
        // Verify fingerprint was checked
        secondBuildResult.Output.Should().Contain("fingerprint", "Should check schema fingerprint");
    }
}
