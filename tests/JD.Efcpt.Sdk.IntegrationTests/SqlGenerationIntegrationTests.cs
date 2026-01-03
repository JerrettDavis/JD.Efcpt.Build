using FluentAssertions;
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
public class SqlGenerationIntegrationTests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public SqlGenerationIntegrationTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    /// <summary>
    /// Test that a SQL project with JD.Efcpt.Build reference is detected correctly.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task SqlProject_WithEfcptBuild_IsDetectedAsSqlProject()
    {
        // Arrange
        // TODO: Create SQL project with MSBuild.Sdk.SqlProj SDK
        // TODO: Add JD.Efcpt.Build package reference
        // TODO: Configure connection string to test database

        // Act
        // TODO: Build the SQL project
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify _EfcptIsSqlProject property is set to true
        // TODO: Verify SQL scripts were generated in project directory
        // TODO: Verify auto-generation warnings are present in SQL files
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that SQL scripts are generated with proper folder structure.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task SqlProject_GeneratesSqlScriptsWithProperStructure()
    {
        // Arrange
        // TODO: Create SQL project with test database connection
        // TODO: Test database should have tables, views, stored procedures

        // Act
        // TODO: Build the SQL project
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify folder structure: dbo/Tables/, dbo/Views/, etc.
        // TODO: Verify SQL scripts exist for each database object
        // TODO: Verify SQL syntax is valid
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that auto-generation warnings are added to SQL files.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task SqlProject_AddsAutoGenerationWarningsToSqlFiles()
    {
        // Arrange
        // TODO: Create SQL project with test database connection

        // Act
        // TODO: Build the SQL project
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Read generated SQL files
        // TODO: Verify each file contains "AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY" header
        // TODO: Verify header includes database name and generation timestamp
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that DataAccess project can reference SQL project and generate EF Core models.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task DataAccessProject_ReferencingSqlProject_GeneratesEfCoreModels()
    {
        // Arrange
        // TODO: Create SQL project (DatabaseProject)
        // TODO: Create EF Core project (DataAccessProject)
        // TODO: Add ProjectReference from DataAccess to Database project
        // TODO: Add JD.Efcpt.Build to both projects

        // Act
        // TODO: Build both projects (SQL project first via MSBuild dependency)
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify SQL scripts were generated in DatabaseProject
        // TODO: Verify DACPAC was built from SQL project
        // TODO: Verify EF Core models were generated in DataAccessProject
        // TODO: Verify models match database schema
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that schema fingerprinting skips regeneration when database is unchanged.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task SqlProject_WithUnchangedSchema_SkipsRegeneration()
    {
        // Arrange
        // TODO: Create SQL project and build once
        // TODO: Record timestamp of generated files

        // Act
        // TODO: Build again without changing database
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify build succeeded
        // TODO: Verify generated files were not regenerated (timestamps unchanged)
        // TODO: Verify fingerprint was reused
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that lifecycle hooks are called in correct order.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task SqlProject_InvokesLifecycleHooksInCorrectOrder()
    {
        // Arrange
        // TODO: Create SQL project with custom BeforeSqlProjGeneration and AfterSqlProjGeneration targets
        // TODO: Targets should write marker files

        // Act
        // TODO: Build the SQL project
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify marker files exist in correct order
        // TODO: Verify BeforeSqlProjGeneration ran before extraction
        // TODO: Verify AfterSqlProjGeneration ran after SQL scripts generated
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that dnx is used when available for .NET 10+ projects.
    /// </summary>
    [Fact(Skip = "Requires .NET 10 SDK and dnx - placeholder for E2E test")]
    public async Task SqlProject_Net10_UsesDnxForSqlPackage()
    {
        // Arrange
        // TODO: Create SQL project targeting net10.0
        // TODO: Verify .NET 10 SDK is installed
        // TODO: Verify dnx is available

        // Act
        // TODO: Build with verbose logging
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify build output contains "Using dnx to execute microsoft.sqlpackage"
        // TODO: Verify sqlpackage was not installed globally
        // buildResult.Success.Should().BeTrue();
    }

    /// <summary>
    /// Test that global sqlpackage is used for .NET 8/9 projects.
    /// </summary>
    [Fact(Skip = "Requires database setup - placeholder for E2E test")]
    public async Task SqlProject_Net80_UsesGlobalSqlPackage()
    {
        // Arrange
        // TODO: Create SQL project targeting net8.0
        // TODO: Set EfcptSqlPackageToolRestore=true

        // Act
        // TODO: Build with verbose logging
        // var buildResult = await _builder.BuildAsync();

        // Assert
        // TODO: Verify build output contains tool restore messages
        // TODO: Verify global sqlpackage tool was used
        // buildResult.Success.Should().BeTrue();
    }
}
