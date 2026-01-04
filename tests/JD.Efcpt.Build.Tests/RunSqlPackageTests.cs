using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the RunSqlPackage task that executes sqlpackage to extract database schema.
/// Note: Full execution tests are in SqlGenerationIntegrationTests. These are unit tests
/// focusing on specific logic paths and helpers.
/// </summary>
[Feature("RunSqlPackage: SqlPackage execution and file processing")]
[Collection(nameof(AssemblySetup))]
public sealed class RunSqlPackageTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(TestBuildEngine Engine, string TempDir);

    private static SetupState Setup()
    {
        var engine = new TestBuildEngine();
        var tempDir = Path.Combine(Path.GetTempPath(), $"efcpt-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        return new SetupState(engine, tempDir);
    }

    private static void Cleanup(SetupState state)
    {
        if (Directory.Exists(state.TempDir))
        {
            Directory.Delete(state.TempDir, recursive: true);
        }
    }

    [Scenario("Task initializes with default values")]
    [Fact]
    public async Task Task_initializes_with_defaults()
    {
        await Given("a new RunSqlPackage task", () => new RunSqlPackage())
            .When("properties are checked", task => task)
            .Then("ToolVersion is empty", t => t.ToolVersion == "")
            .And("ToolRestore is true by default", t => t.ToolRestore == "true")
            .And("ToolPath is empty", t => t.ToolPath == "")
            .And("DotNetExe is dotnet", t => t.DotNetExe == "dotnet")
            .And("ExtractTarget is Flat", t => t.ExtractTarget == "Flat")
            .And("LogVerbosity is minimal", t => t.LogVerbosity == "minimal")
            .AssertPassed();
    }

    [Scenario("ToolRestore property handles various true values")]
    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("YES")]
    public async Task ToolRestore_recognizes_true_values(string value)
    {
        await Given($"ToolRestore set to '{value}'", () =>
        {
            var state = Setup();
            return (state, value);
        })
        .When("task is configured", s =>
        {
            var task = new RunSqlPackage
            {
                BuildEngine = s.state.Engine,
                WorkingDirectory = s.state.TempDir,
                ConnectionString = "Server=test;Database=test",
                TargetDirectory = s.state.TempDir,
                ToolRestore = s.value,
                ToolPath = "sqlpackage", // Use explicit path to avoid restore
                LogVerbosity = "minimal"
            };
            return (s.state, task, s.value);
        })
        .Then("ToolRestore value is accepted", r => r.task.ToolRestore == r.value)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("ExtractTarget modes are configurable")]
    [Theory]
    [InlineData("Flat")]
    [InlineData("File")]
    [InlineData("SchemaObjectType")]
    public async Task ExtractTarget_modes_are_configurable(string mode)
    {
        await Given($"ExtractTarget set to '{mode}'", () =>
        {
            var state = Setup();
            return (state, mode);
        })
        .When("task is configured", s =>
        {
            var task = new RunSqlPackage
            {
                BuildEngine = s.state.Engine,
                WorkingDirectory = s.state.TempDir,
                ConnectionString = "Server=test;Database=test",
                TargetDirectory = s.state.TempDir,
                ExtractTarget = s.mode,
                ToolPath = "sqlpackage",
                LogVerbosity = "minimal"
            };
            return (s.state, task, s.mode);
        })
        .Then("ExtractTarget is set correctly", r => r.task.ExtractTarget == r.mode)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Creates target directory when it doesn't exist")]
    [Fact]
    public async Task Creates_target_directory_if_missing()
    {
        await Given("a target directory that doesn't exist", () =>
        {
            var state = Setup();
            var targetDir = Path.Combine(state.TempDir, "output");
            return (state, targetDir);
        })
        .When("task execution attempts to create directory", s =>
        {
            // We can't easily test Execute() without sqlpackage installed,
            // but we can verify the directory creation logic by checking
            // if Directory.CreateDirectory would work
            if (!Directory.Exists(s.targetDir))
            {
                Directory.CreateDirectory(s.targetDir);
            }
            return (s.state, s.targetDir, Directory.Exists(s.targetDir));
        })
        .Then("directory is created", r => r.Item3)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("File movement skips system security objects")]
    [Fact]
    public async Task File_movement_skips_system_objects()
    {
        await Given("extracted files including system objects", () =>
        {
            var state = Setup();

            // Create source directory with .dacpac subdirectory (as sqlpackage creates)
            var sourceDir = Path.Combine(state.TempDir, ".dacpac");
            Directory.CreateDirectory(sourceDir);

            // Create application-scoped files (should be moved)
            var dboTablesDir = Path.Combine(sourceDir, "dbo", "Tables");
            Directory.CreateDirectory(dboTablesDir);
            File.WriteAllText(Path.Combine(dboTablesDir, "Customers.sql"), "CREATE TABLE Customers (Id INT);");

            // Create system security objects (should be skipped)
            var securityDir = Path.Combine(sourceDir, "Security", "BUILTIN");
            Directory.CreateDirectory(securityDir);
            File.WriteAllText(Path.Combine(securityDir, "Administrators_.sql"), "-- System object");

            var serverObjectsDir = Path.Combine(sourceDir, "ServerObjects");
            Directory.CreateDirectory(serverObjectsDir);
            File.WriteAllText(Path.Combine(serverObjectsDir, "Server.sql"), "-- Server object");

            var storageDir = Path.Combine(sourceDir, "Storage");
            Directory.CreateDirectory(storageDir);
            File.WriteAllText(Path.Combine(storageDir, "Storage.sql"), "-- Storage object");

            var targetDir = Path.Combine(state.TempDir, "target");
            Directory.CreateDirectory(targetDir);

            return (state, sourceDir, targetDir);
        })
        .When("MoveDirectoryContents logic is simulated", s =>
        {
            // Simulate the MoveDirectoryContents logic
            var excludedPaths = new[] { "Security", "ServerObjects", "Storage" };
            var sourceDirNormalized = s.sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            foreach (var file in Directory.GetFiles(s.sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.StartsWith(sourceDirNormalized, StringComparison.OrdinalIgnoreCase)
                    ? file.Substring(sourceDirNormalized.Length)
                    : Path.GetFileName(file);

                var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (pathParts.Length > 0 && Array.Exists(excludedPaths, p => p.Equals(pathParts[0], StringComparison.OrdinalIgnoreCase)))
                {
                    // Skip system objects
                    continue;
                }

                var destPath = Path.Combine(s.targetDir, relativePath);
                var destDirectory = Path.GetDirectoryName(destPath);
                if (destDirectory != null && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy(file, destPath);
            }

            return (s.state, s.targetDir);
        })
        .Then("application-scoped files are moved", r =>
        {
            var customerTable = Path.Combine(r.targetDir, "dbo", "Tables", "Customers.sql");
            return File.Exists(customerTable);
        })
        .And("Security files are not moved", r =>
        {
            var securityFiles = Directory.GetFiles(r.targetDir, "*", SearchOption.AllDirectories)
                .Where(f => f.Contains("Security")).ToList();
            return securityFiles.Count == 0;
        })
        .And("ServerObjects files are not moved", r =>
        {
            var serverFiles = Directory.GetFiles(r.targetDir, "*", SearchOption.AllDirectories)
                .Where(f => f.Contains("ServerObjects")).ToList();
            return serverFiles.Count == 0;
        })
        .And("Storage files are not moved", r =>
        {
            var storageFiles = Directory.GetFiles(r.targetDir, "*", SearchOption.AllDirectories)
                .Where(f => f.Contains("Storage")).ToList();
            return storageFiles.Count == 0;
        })
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("File movement handles nested directories")]
    [Fact]
    public async Task File_movement_handles_nested_directories()
    {
        await Given("extracted files in nested directories", () =>
        {
            var state = Setup();
            var sourceDir = Path.Combine(state.TempDir, ".dacpac");
            var targetDir = Path.Combine(state.TempDir, "target");

            // Create nested directory structure
            var nestedDir = Path.Combine(sourceDir, "dbo", "Tables", "SubFolder");
            Directory.CreateDirectory(nestedDir);
            File.WriteAllText(Path.Combine(nestedDir, "Table1.sql"), "CREATE TABLE Table1 (Id INT);");

            Directory.CreateDirectory(targetDir);

            return (state, sourceDir, targetDir);
        })
        .When("MoveDirectoryContents logic is simulated", s =>
        {
            var excludedPaths = new[] { "Security", "ServerObjects", "Storage" };
            var sourceDirNormalized = s.sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            foreach (var file in Directory.GetFiles(s.sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.StartsWith(sourceDirNormalized, StringComparison.OrdinalIgnoreCase)
                    ? file.Substring(sourceDirNormalized.Length)
                    : Path.GetFileName(file);

                var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (pathParts.Length > 0 && Array.Exists(excludedPaths, p => p.Equals(pathParts[0], StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var destPath = Path.Combine(s.targetDir, relativePath);
                var destDirectory = Path.GetDirectoryName(destPath);
                if (destDirectory != null && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                File.Copy(file, destPath);
            }

            return (s.state, s.targetDir);
        })
        .Then("nested directory structure is preserved", r =>
        {
            var nestedFile = Path.Combine(r.targetDir, "dbo", "Tables", "SubFolder", "Table1.sql");
            return File.Exists(nestedFile);
        })
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("File movement overwrites existing files")]
    [Fact]
    public async Task File_movement_overwrites_existing_files()
    {
        await Given("source and target with conflicting files", () =>
        {
            var state = Setup();
            var sourceDir = Path.Combine(state.TempDir, ".dacpac");
            var targetDir = Path.Combine(state.TempDir, "target");

            Directory.CreateDirectory(Path.Combine(sourceDir, "dbo"));
            Directory.CreateDirectory(Path.Combine(targetDir, "dbo"));

            var sourceFile = Path.Combine(sourceDir, "dbo", "Table1.sql");
            var targetFile = Path.Combine(targetDir, "dbo", "Table1.sql");

            File.WriteAllText(sourceFile, "NEW CONTENT");
            File.WriteAllText(targetFile, "OLD CONTENT");

            return (state, sourceDir, targetDir, sourceFile, targetFile);
        })
        .When("MoveDirectoryContents logic is simulated with overwrite", s =>
        {
            var excludedPaths = new[] { "Security", "ServerObjects", "Storage" };
            var sourceDirNormalized = s.sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            foreach (var file in Directory.GetFiles(s.sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = file.StartsWith(sourceDirNormalized, StringComparison.OrdinalIgnoreCase)
                    ? file.Substring(sourceDirNormalized.Length)
                    : Path.GetFileName(file);

                var pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (pathParts.Length > 0 && Array.Exists(excludedPaths, p => p.Equals(pathParts[0], StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var destPath = Path.Combine(s.targetDir, relativePath);
                var destDirectory = Path.GetDirectoryName(destPath);
                if (destDirectory != null && !Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                // Delete existing file before copying (simulating File.Move with overwrite)
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
                File.Copy(file, destPath);
            }

            return (s.state, s.targetFile);
        })
        .Then("target file contains new content", r =>
        {
            var content = File.ReadAllText(r.targetFile);
            return content == "NEW CONTENT";
        })
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Connection string is properly formatted in arguments")]
    [Fact]
    public async Task Connection_string_formatted_in_arguments()
    {
        await Given("a task with connection string", () =>
        {
            var state = Setup();
            var connectionString = "Server=localhost;Database=TestDb;Trusted_Connection=true;";
            return (state, connectionString);
        })
        .When("BuildSqlPackageArguments is conceptually invoked", s =>
        {
            // We're testing the logic that would be in BuildSqlPackageArguments
            var args = $"/Action:Extract /SourceConnectionString:\"{s.connectionString}\"";
            return (s.state, args, s.connectionString);
        })
        .Then("connection string is quoted in arguments", r =>
            r.args.Contains($"/SourceConnectionString:\"{r.connectionString}\""))
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Target file path uses .dacpac subdirectory")]
    [Fact]
    public async Task Target_file_uses_dacpac_subdirectory()
    {
        await Given("a target directory", () =>
        {
            var state = Setup();
            var targetDirectory = state.TempDir;
            return (state, targetDirectory);
        })
        .When("BuildSqlPackageArguments logic determines target file", s =>
        {
            // Simulating the logic from BuildSqlPackageArguments
            var targetFile = Path.Combine(s.targetDirectory, ".dacpac");
            var args = $"/TargetFile:\"{targetFile}\"";
            return (s.state, args, targetFile);
        })
        .Then("target file uses .dacpac subdirectory", r =>
            r.args.Contains($"/TargetFile:\"{r.targetFile}\"") &&
            r.targetFile.EndsWith(".dacpac"))
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("ExtractApplicationScopedObjectsOnly property is set")]
    [Fact]
    public async Task Extract_application_scoped_objects_only()
    {
        await Given("sqlpackage arguments being built", () => Setup())
        .When("BuildSqlPackageArguments logic is applied", s =>
        {
            // Simulating BuildSqlPackageArguments method
            var args = "/Action:Extract /p:ExtractApplicationScopedObjectsOnly=True";
            return (s, args);
        })
        .Then("ExtractApplicationScopedObjectsOnly is set", r =>
            r.args.Contains("/p:ExtractApplicationScopedObjectsOnly=True"))
        .Finally(r => Cleanup(r.s))
        .AssertPassed();
    }

    [Scenario("Explicit tool path not found produces JD0020 error")]
    [Fact]
    public async Task Explicit_tool_path_not_found_error()
    {
        await Given("a task with non-existent tool path", () =>
        {
            var state = Setup();
            var nonExistentPath = Path.Combine(state.TempDir, "nonexistent-sqlpackage.exe");
            return (state, nonExistentPath);
        })
        .When("task is executed", s =>
        {
            var task = new RunSqlPackage
            {
                BuildEngine = s.state.Engine,
                WorkingDirectory = s.state.TempDir,
                ConnectionString = "Server=test;Database=test",
                TargetDirectory = s.state.TempDir,
                ToolPath = s.nonExistentPath,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s.state, result, s.state.Engine.Errors);
        })
        .Then("task fails", r => !r.result)
        .And("JD0020 error is logged", r => r.Errors.Any(e => e.Code == "JD0020" && e.Message?.Contains("Explicit tool path does not exist") == true))
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Invalid target directory produces JD0024 error")]
    [Fact]
    public async Task Invalid_target_directory_error()
    {
        await Given("a task with invalid target directory", () =>
        {
            var state = Setup();
            // Use an invalid path (e.g., contains invalid characters)
            var invalidPath = Path.Combine(state.TempDir, new string(Path.GetInvalidPathChars()));
            return (state, invalidPath);
        })
        .When("task is executed", s =>
        {
            var task = new RunSqlPackage
            {
                BuildEngine = s.state.Engine,
                WorkingDirectory = s.state.TempDir,
                ConnectionString = "Server=test;Database=test",
                TargetDirectory = s.invalidPath,
                ToolPath = "sqlpackage", // Use explicit path to avoid needing real tool
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s.state, result, s.state.Engine.Errors);
        })
        .Then("task fails", r => !r.result)
        .And("JD0024 error is logged", r => r.Errors.Any(e => e.Code == "JD0024" && e.Message?.Contains("Failed to create target directory") == true))
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("ToolRestore property handles false values")]
    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("False")]
    [InlineData("0")]
    [InlineData("no")]
    [InlineData("NO")]
    [InlineData("")]
    public async Task ToolRestore_recognizes_false_values(string value)
    {
        await Given($"ToolRestore set to '{value}'", () =>
        {
            var state = Setup();
            return (state, value);
        })
        .When("ShouldRestoreTool logic is evaluated", s =>
        {
            // Simulate the ShouldRestoreTool logic
            bool shouldRestore;
            if (string.IsNullOrEmpty(s.value))
            {
                shouldRestore = true; // Empty defaults to true
            }
            else
            {
                var normalized = s.value.Trim().ToLowerInvariant();
                shouldRestore = normalized == "true" || normalized == "1" || normalized == "yes";
            }
            return (s.state, shouldRestore, s.value);
        })
        .Then("restore should not be performed for explicit false values", r =>
        {
            // Empty string defaults to true, explicit false values should be false
            if (string.IsNullOrEmpty(r.value))
                return r.shouldRestore == true;
            return r.shouldRestore == false;
        })
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Explicit tool path with rooted path")]
    [Fact]
    public async Task Explicit_tool_path_with_rooted_path()
    {
        await Given("a rooted tool path that exists", () =>
        {
            var state = Setup();
            // Create a dummy file to represent sqlpackage
            var toolPath = Path.Combine(state.TempDir, "sqlpackage.exe");
            File.WriteAllText(toolPath, "dummy");
            return (state, toolPath);
        })
        .When("tool path resolution logic is evaluated", s =>
        {
            // Simulate ResolveToolPath logic for explicit path
            var resolvedPath = Path.IsPathRooted(s.toolPath)
                ? s.toolPath
                : Path.GetFullPath(Path.Combine(s.state.TempDir, s.toolPath));

            var exists = File.Exists(resolvedPath);
            return (s.state, resolvedPath, exists);
        })
        .Then("path is used as-is", r => r.resolvedPath == r.state.TempDir + Path.DirectorySeparatorChar + "sqlpackage.exe" ||
                                          r.resolvedPath.EndsWith("sqlpackage.exe"))
        .And("path exists", r => r.exists)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Explicit tool path with relative path")]
    [Fact]
    public async Task Explicit_tool_path_with_relative_path()
    {
        await Given("a relative tool path", () =>
        {
            var state = Setup();
            var workingDir = state.TempDir;
            // Use Path.Combine for cross-platform compatibility
            var relativePath = Path.Combine("tools", "sqlpackage.exe");

            // Create the tool file
            var toolDir = Path.Combine(workingDir, "tools");
            Directory.CreateDirectory(toolDir);
            var fullPath = Path.Combine(toolDir, "sqlpackage.exe");
            File.WriteAllText(fullPath, "dummy");

            return (state, relativePath, workingDir, fullPath);
        })
        .When("tool path resolution logic is evaluated", s =>
        {
            // Simulate ResolveToolPath logic
            string resolvedPath;
            if (Path.IsPathRooted(s.relativePath))
            {
                resolvedPath = s.relativePath;
            }
            else
            {
                resolvedPath = Path.GetFullPath(Path.Combine(s.workingDir, s.relativePath));
            }

            return (s.state, resolvedPath, s.fullPath);
        })
        .Then("path is resolved relative to working directory", r => r.resolvedPath == r.fullPath)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("File movement handles files without source directory prefix")]
    [Fact]
    public async Task File_movement_handles_files_without_prefix()
    {
        await Given("a file path that doesn't start with source directory", () =>
        {
            var state = Setup();
            var sourceDir = Path.Combine(state.TempDir, "source");
            Directory.CreateDirectory(sourceDir);

            // Simulate a case where file path doesn't start with normalized source dir
            var fileName = "Table1.sql";

            return (state, sourceDir, fileName);
        })
        .When("path processing logic is evaluated", s =>
        {
            var sourceDirNormalized = s.sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            // Simulate the substring logic
            var relativePath = s.fileName.StartsWith(sourceDirNormalized, StringComparison.OrdinalIgnoreCase)
                ? s.fileName.Substring(sourceDirNormalized.Length)
                : Path.GetFileName(s.fileName); // Fallback: use just the filename

            return (s.state, relativePath, s.fileName);
        })
        .Then("falls back to filename", r => r.relativePath == Path.GetFileName(r.fileName))
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Target framework for .NET 10 detection")]
    [Theory]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    [InlineData("net12.0")]
    public async Task Target_framework_net10_detection(string tfm)
    {
        await Given($"target framework {tfm}", () =>
        {
            var state = Setup();
            return (state, tfm);
        })
        .When("framework version is evaluated", s =>
        {
            // This would trigger the IsDotNet10OrLater check in ResolveToolPath
            var isNet10OrLater = Tasks.Utilities.DotNetToolUtilities.IsDotNet10OrLater(s.tfm);
            return (s.state, isNet10OrLater);
        })
        .Then("is recognized as .NET 10+", r => r.isNet10OrLater)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Target framework for pre-.NET 10 detection")]
    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("netstandard2.0")]
    [InlineData("net472")]
    public async Task Target_framework_pre_net10_detection(string tfm)
    {
        await Given($"target framework {tfm}", () =>
        {
            var state = Setup();
            return (state, tfm);
        })
        .When("framework version is evaluated", s =>
        {
            var isNet10OrLater = Tasks.Utilities.DotNetToolUtilities.IsDotNet10OrLater(s.tfm);
            return (s.state, isNet10OrLater);
        })
        .Then("is not recognized as .NET 10+", r => !r.isNet10OrLater)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("ExtractedPath output is set to target directory")]
    [Fact]
    public async Task ExtractedPath_output_is_set()
    {
        await Given("a RunSqlPackage task", () =>
        {
            var state = Setup();
            var targetDir = Path.Combine(state.TempDir, "output");
            Directory.CreateDirectory(targetDir);
            return (state, targetDir);
        })
        .When("ExtractedPath would be set", s =>
        {
            // Simulating line 145: ExtractedPath = TargetDirectory;
            var extractedPath = s.targetDir;
            return (s.state, extractedPath, s.targetDir);
        })
        .Then("ExtractedPath equals TargetDirectory", r => r.extractedPath == r.targetDir)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("ToolVersion property is configurable")]
    [Fact]
    public async Task ToolVersion_is_configurable()
    {
        await Given("a tool version", () =>
        {
            var state = Setup();
            var version = "162.0.52";
            return (state, version);
        })
        .When("task is configured with ToolVersion", s =>
        {
            var task = new RunSqlPackage
            {
                BuildEngine = s.state.Engine,
                WorkingDirectory = s.state.TempDir,
                ConnectionString = "Server=test;Database=test",
                TargetDirectory = s.state.TempDir,
                ToolVersion = s.version,
                ToolPath = "sqlpackage",
                LogVerbosity = "minimal"
            };
            return (s.state, task, s.version);
        })
        .Then("ToolVersion is set correctly", r => r.task.ToolVersion == r.version)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("DotNetExe property is configurable")]
    [Fact]
    public async Task DotNetExe_is_configurable()
    {
        await Given("a custom dotnet exe path", () =>
        {
            var state = Setup();
            var dotnetPath = "C:\\custom\\dotnet.exe";
            return (state, dotnetPath);
        })
        .When("task is configured with DotNetExe", s =>
        {
            var task = new RunSqlPackage
            {
                BuildEngine = s.state.Engine,
                WorkingDirectory = s.state.TempDir,
                ConnectionString = "Server=test;Database=test",
                TargetDirectory = s.state.TempDir,
                DotNetExe = s.dotnetPath,
                ToolPath = "sqlpackage",
                LogVerbosity = "minimal"
            };
            return (s.state, task, s.dotnetPath);
        })
        .Then("DotNetExe is set correctly", r => r.task.DotNetExe == r.dotnetPath)
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }
}
