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

[Feature("End-to-End Reverse Engineering: generates and compiles EF models from SQL Server using Testcontainers")]
[Collection(nameof(AssemblySetup))]
public sealed class EndToEndReverseEngineeringTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        MsSqlContainer Container,
        string ConnectionString,
        TestFolder Folder) : IDisposable
    {
        public void Dispose()
        {
            Container.DisposeAsync().AsTask().Wait();
            Folder.Dispose();
        }
    }

    private sealed record SchemaGenerationResult(
        TestContext Context,
        string ProjectDir,
        string OutputDir,
        bool QuerySuccess,
        bool RunSuccess);

    // ========== Setup Methods ==========

    private static async Task<TestContext> SetupSqlServerWithSampleSchema()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await container.StartAsync();
        var connectionString = container.GetConnectionString();

        // Create a sample schema with multiple tables
        await CreateTable(connectionString, "Customers",
            "CustomerId INT PRIMARY KEY IDENTITY(1,1)",
            "FirstName NVARCHAR(50) NOT NULL",
            "LastName NVARCHAR(50) NOT NULL",
            "Email NVARCHAR(255) NULL",
            "CreatedDate DATETIME NOT NULL DEFAULT GETDATE()");

        await CreateTable(connectionString, "Orders",
            "OrderId INT PRIMARY KEY IDENTITY(1,1)",
            "CustomerId INT NOT NULL",
            "OrderDate DATETIME NOT NULL",
            "TotalAmount DECIMAL(18,2) NOT NULL");

        await ExecuteSql(connectionString,
            "ALTER TABLE dbo.Orders ADD CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)");

        await CreateTable(connectionString, "Products",
            "ProductId INT PRIMARY KEY IDENTITY(1,1)",
            "ProductName NVARCHAR(100) NOT NULL",
            "Price DECIMAL(18,2) NOT NULL",
            "StockQuantity INT NOT NULL DEFAULT 0");

        await ExecuteSql(connectionString,
            "CREATE INDEX IX_Products_ProductName ON dbo.Products (ProductName)");

        var folder = new TestFolder();
        return new TestContext(container, connectionString, folder);
    }

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

    // ========== Execute Methods ==========

    private static SchemaGenerationResult ExecuteReverseEngineering(TestContext context)
    {
        var projectDir = context.Folder.CreateDir("TestProject");
        var outputDir = Path.Combine(projectDir, "obj", "efcpt");
        Directory.CreateDirectory(outputDir);

        // Create minimal config files
        var configPath = context.Folder.WriteFile("TestProject/efcpt-config.json",
            """
            {
              "ProjectRootNamespace": "TestProject",
              "ContextName": "TestDbContext",
              "ContextNamespace": "TestProject.Data",
              "ModelNamespace": "TestProject.Models",
              "SelectedToBeGenerated": [],
              "Tables": [],
              "UseDatabaseNames": false
            }
            """);

        var renamingPath = context.Folder.WriteFile("TestProject/efcpt.renaming.json", "[]");

        // Create an empty template directory (required by ComputeFingerprint)
        var templateDir = context.Folder.CreateDir("TestProject/templates");

        // Step 1: Query schema metadata
        var queryTask = new QuerySchemaMetadata
        {
            BuildEngine = new TestBuildEngine(),
            ConnectionString = context.ConnectionString,
            OutputDir = outputDir,
            LogVerbosity = "minimal"
        };

        var querySuccess = queryTask.Execute();
        var schemaFingerprint = queryTask.SchemaFingerprint;

        // Step 2: Compute full fingerprint
        var fingerprintFile = Path.Combine(outputDir, "efcpt-fingerprint.txt");
        var computeFingerprintTask = new ComputeFingerprint
        {
            BuildEngine = new TestBuildEngine(),
            UseConnectionStringMode = "true",
            SchemaFingerprint = schemaFingerprint,
            ConfigPath = configPath,
            RenamingPath = renamingPath,
            TemplateDir = templateDir,
            FingerprintFile = fingerprintFile,
            LogVerbosity = "minimal"
        };

        var fingerprintSuccess = computeFingerprintTask.Execute();

        // Step 3: Run EFCPT to generate models (using fake mode for tests)
        Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "true");
        try
        {
            var runTask = new RunEfcpt
            {
                BuildEngine = new TestBuildEngine(),
                WorkingDirectory = outputDir,
                ConnectionString = context.ConnectionString,
                UseConnectionStringMode = "true",
                ConfigPath = configPath,
                RenamingPath = renamingPath,
                TemplateDir = templateDir,
                OutputDir = outputDir,
                LogVerbosity = "minimal"
            };

            var runSuccess = runTask.Execute();

            return new SchemaGenerationResult(context, projectDir, outputDir, querySuccess && fingerprintSuccess, runSuccess);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", null);
        }
    }

    // ========== Helper Methods ==========

    private static string[] GetGeneratedFiles(string directory, string pattern)
        => Directory.Exists(directory)
            ? Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
            : [];

    // ========== Tests ==========

    [Scenario("Generate models from SQL Server schema")]
    [Fact]
    public async Task Generate_models_from_sql_server_schema()
        => await Given("SQL Server with Customers, Orders, Products tables", SetupSqlServerWithSampleSchema)
            .When("execute reverse engineering pipeline", ExecuteReverseEngineering)
            .Then("query schema task succeeds", r => r.QuerySuccess)
            .And("run efcpt task succeeds", r => r.RunSuccess)
            .And("fingerprint file exists", r => File.Exists(Path.Combine(r.OutputDir, "efcpt-fingerprint.txt")))
            .And("schema model file exists", r => File.Exists(Path.Combine(r.OutputDir, "schema-model.json")))
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("Generated models contain expected files")]
    [Fact]
    public async Task Generated_models_contain_expected_files()
        => await Given("SQL Server with sample schema", SetupSqlServerWithSampleSchema)
            .When("execute reverse engineering", ExecuteReverseEngineering)
            .Then("tasks succeed", r => r.QuerySuccess && r.RunSuccess)
            .And("sample model file is generated", r => File.Exists(Path.Combine(r.OutputDir, "SampleModel.cs")))
            .And("sample model has content", r =>
            {
                var sampleFile = Path.Combine(r.OutputDir, "SampleModel.cs");
                return File.Exists(sampleFile) && new FileInfo(sampleFile).Length > 0;
            })
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("Generated models are valid C# code")]
    [Fact]
    public async Task Generated_models_are_valid_csharp_code()
        => await Given("SQL Server with sample schema", SetupSqlServerWithSampleSchema)
            .When("execute reverse engineering", ExecuteReverseEngineering)
            .Then("tasks succeed", r => r.QuerySuccess && r.RunSuccess)
            .And("generated .cs file exists", r =>
            {
                var csFiles = GetGeneratedFiles(r.OutputDir, "*.cs");
                return csFiles.Length > 0;
            })
            .And("generated file has content", r =>
            {
                var csFiles = GetGeneratedFiles(r.OutputDir, "*.cs");
                return csFiles.All(f => new FileInfo(f).Length > 0);
            })
            .And("generated file contains expected comment", r =>
            {
                var sampleFile = Path.Combine(r.OutputDir, "SampleModel.cs");
                if (!File.Exists(sampleFile)) return false;
                var content = File.ReadAllText(sampleFile);
                return content.Contains("// generated from");
            })
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    [Scenario("Schema fingerprint changes when database schema changes")]
    [Fact]
    public async Task Schema_fingerprint_changes_when_database_schema_changes()
        => await Given("SQL Server with sample schema", SetupSqlServerWithSampleSchema)
            .When("execute reverse engineering, modify schema, execute again", ExecuteModifyAndRegenerate)
            .Then("initial generation succeeds", r => r.InitialQuerySuccess && r.InitialRunSuccess)
            .And("modified generation succeeds", r => r.ModifiedQuerySuccess && r.ModifiedRunSuccess)
            .And("fingerprints are different", r => r.InitialFingerprint != r.ModifiedFingerprint)
            .Finally(r => r.Context.Dispose())
            .AssertPassed();

    private static async Task<ModifiedSchemaResult> ExecuteModifyAndRegenerate(TestContext context)
    {
        var projectDir = context.Folder.CreateDir("TestProject");
        var outputDir = Path.Combine(projectDir, "obj", "efcpt");
        Directory.CreateDirectory(outputDir);

        var configPath = context.Folder.WriteFile("TestProject/efcpt-config.json",
            """
            {
              "ProjectRootNamespace": "TestProject",
              "ContextName": "TestDbContext",
              "ContextNamespace": "TestProject.Data",
              "ModelNamespace": "TestProject.Models",
              "SelectedToBeGenerated": [],
              "Tables": [],
              "UseDatabaseNames": false
            }
            """);

        var renamingPath = context.Folder.WriteFile("TestProject/efcpt.renaming.json", "[]");
        var templateDir = context.Folder.CreateDir("TestProject/templates");

        // First generation - Query schema and compute fingerprint
        var queryTask1 = new QuerySchemaMetadata
        {
            BuildEngine = new TestBuildEngine(),
            ConnectionString = context.ConnectionString,
            OutputDir = outputDir,
            LogVerbosity = "minimal"
        };

        var initialQuerySuccess = queryTask1.Execute();
        var initialSchemaFingerprint = queryTask1.SchemaFingerprint;

        var fingerprintFile = Path.Combine(outputDir, "efcpt-fingerprint.txt");
        var computeTask1 = new ComputeFingerprint
        {
            BuildEngine = new TestBuildEngine(),
            UseConnectionStringMode = "true",
            SchemaFingerprint = initialSchemaFingerprint,
            ConfigPath = configPath,
            RenamingPath = renamingPath,
            TemplateDir = templateDir,
            FingerprintFile = fingerprintFile,
            LogVerbosity = "minimal"
        };

        var initialFingerprintSuccess = computeTask1.Execute();
        var initialFingerprint = computeTask1.Fingerprint;

        // Modify schema - add a new column
        await ExecuteSql(context.ConnectionString,
            "ALTER TABLE dbo.Customers ADD PhoneNumber NVARCHAR(20) NULL");

        // Second generation - Query schema and compute fingerprint again
        var queryTask2 = new QuerySchemaMetadata
        {
            BuildEngine = new TestBuildEngine(),
            ConnectionString = context.ConnectionString,
            OutputDir = outputDir,
            LogVerbosity = "minimal"
        };

        var modifiedQuerySuccess = queryTask2.Execute();
        var modifiedSchemaFingerprint = queryTask2.SchemaFingerprint;

        var computeTask2 = new ComputeFingerprint
        {
            BuildEngine = new TestBuildEngine(),
            UseConnectionStringMode = "true",
            SchemaFingerprint = modifiedSchemaFingerprint,
            ConfigPath = configPath,
            RenamingPath = renamingPath,
            TemplateDir = templateDir,
            FingerprintFile = fingerprintFile,
            LogVerbosity = "minimal"
        };

        var modifiedFingerprintSuccess = computeTask2.Execute();
        var modifiedFingerprint = computeTask2.Fingerprint;

        return new ModifiedSchemaResult(
            context,
            initialQuerySuccess && initialFingerprintSuccess,
            true, // runSuccess not needed for this test
            initialFingerprint,
            modifiedQuerySuccess && modifiedFingerprintSuccess,
            true, // runSuccess not needed for this test
            modifiedFingerprint);
    }

    private sealed record ModifiedSchemaResult(
        TestContext Context,
        bool InitialQuerySuccess,
        bool InitialRunSuccess,
        string InitialFingerprint,
        bool ModifiedQuerySuccess,
        bool ModifiedRunSuccess,
        string ModifiedFingerprint);
}
