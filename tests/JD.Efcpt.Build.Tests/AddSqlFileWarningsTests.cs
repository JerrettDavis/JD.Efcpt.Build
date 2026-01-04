using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the AddSqlFileWarnings task that adds auto-generation warnings to SQL files.
/// </summary>
[Feature("AddSqlFileWarnings: Adding auto-generation warnings to SQL files")]
[Collection(nameof(AssemblySetup))]
public sealed class AddSqlFileWarningsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
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

    [Scenario("Adds warning header to SQL file without existing warning")]
    [Fact]
    public async Task Adds_warning_to_sql_file_without_warning()
    {
        await Given("a SQL file without warning header", () =>
        {
            var state = Setup();
            var sqlFile = Path.Combine(state.TempDir, "test.sql");
            File.WriteAllText(sqlFile, "CREATE TABLE Test (Id INT);");
            return state;
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.Engine,
                ScriptsDirectory = s.TempDir,
                DatabaseName = "TestDb",
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s, result, task.FilesProcessed);
        })
        .Then("task succeeds", r => r.result)
        .And("one file is processed", r => r.FilesProcessed == 1)
        .And("file contains warning header", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.s.TempDir, "test.sql"));
            return content.Contains("AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY");
        })
        .And("file contains database name", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.s.TempDir, "test.sql"));
            return content.Contains("database: TestDb");
        })
        .And("file contains original content", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.s.TempDir, "test.sql"));
            return content.Contains("CREATE TABLE Test (Id INT);");
        })
        .Finally(r => Cleanup(r.s))
        .AssertPassed();
    }

    [Scenario("Skips SQL file that already has warning header")]
    [Fact]
    public async Task Skips_sql_file_with_existing_warning()
    {
        await Given("a SQL file with existing warning header", () =>
        {
            var state = Setup();
            var sqlFile = Path.Combine(state.TempDir, "test.sql");
            var content = "/* AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY */\nCREATE TABLE Test (Id INT);";
            File.WriteAllText(sqlFile, content);
            return (state, originalContent: content);
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.state.Engine,
                ScriptsDirectory = s.state.TempDir,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s.state, s.originalContent, result, task.FilesProcessed);
        })
        .Then("task succeeds", r => r.result)
        .And("one file is processed", r => r.FilesProcessed == 1)
        .And("file content is unchanged", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.state.TempDir, "test.sql"));
            return content == r.originalContent;
        })
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Processes multiple SQL files")]
    [Fact]
    public async Task Processes_multiple_sql_files()
    {
        await Given("multiple SQL files without warnings", () =>
        {
            var state = Setup();
            File.WriteAllText(Path.Combine(state.TempDir, "file1.sql"), "CREATE TABLE Test1 (Id INT);");
            File.WriteAllText(Path.Combine(state.TempDir, "file2.sql"), "CREATE TABLE Test2 (Id INT);");
            File.WriteAllText(Path.Combine(state.TempDir, "file3.sql"), "CREATE TABLE Test3 (Id INT);");
            return state;
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.Engine,
                ScriptsDirectory = s.TempDir,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s, result, task.FilesProcessed);
        })
        .Then("task succeeds", r => r.result)
        .And("three files are processed", r => r.FilesProcessed == 3)
        .And("all files contain warning header", r =>
        {
            var file1 = File.ReadAllText(Path.Combine(r.s.TempDir, "file1.sql"));
            var file2 = File.ReadAllText(Path.Combine(r.s.TempDir, "file2.sql"));
            var file3 = File.ReadAllText(Path.Combine(r.s.TempDir, "file3.sql"));
            return file1.Contains("AUTO-GENERATED FILE") &&
                   file2.Contains("AUTO-GENERATED FILE") &&
                   file3.Contains("AUTO-GENERATED FILE");
        })
        .Finally(r => Cleanup(r.s))
        .AssertPassed();
    }

    [Scenario("Processes SQL files in subdirectories")]
    [Fact]
    public async Task Processes_sql_files_in_subdirectories()
    {
        await Given("SQL files in subdirectories", () =>
        {
            var state = Setup();
            var subDir1 = Path.Combine(state.TempDir, "dbo", "Tables");
            var subDir2 = Path.Combine(state.TempDir, "dbo", "Views");
            Directory.CreateDirectory(subDir1);
            Directory.CreateDirectory(subDir2);
            File.WriteAllText(Path.Combine(subDir1, "Table1.sql"), "CREATE TABLE Table1 (Id INT);");
            File.WriteAllText(Path.Combine(subDir2, "View1.sql"), "CREATE VIEW View1 AS SELECT 1;");
            return state;
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.Engine,
                ScriptsDirectory = s.TempDir,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s, result, task.FilesProcessed);
        })
        .Then("task succeeds", r => r.result)
        .And("two files are processed", r => r.FilesProcessed == 2)
        .Finally(r => Cleanup(r.s))
        .AssertPassed();
    }

    [Scenario("Succeeds when scripts directory doesn't exist")]
    [Fact]
    public async Task Succeeds_when_directory_not_found()
    {
        await Given("a non-existent directory", () =>
        {
            var state = Setup();
            var nonExistentDir = Path.Combine(state.TempDir, "nonexistent");
            return (state, nonExistentDir);
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.state.Engine,
                ScriptsDirectory = s.nonExistentDir,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s.state, result, task.FilesProcessed, s.state.Engine.Warnings);
        })
        .Then("task succeeds", r => r.result)
        .And("no files are processed", r => r.FilesProcessed == 0)
        .And("warning is logged", r => r.Warnings.Any(w => w.Message?.Contains("Scripts directory not found") is true))
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    [Scenario("Adds warning header without database name when not provided")]
    [Fact]
    public async Task Adds_warning_without_database_name()
    {
        await Given("a SQL file and no database name", () =>
        {
            var state = Setup();
            var sqlFile = Path.Combine(state.TempDir, "test.sql");
            File.WriteAllText(sqlFile, "CREATE TABLE Test (Id INT);");
            return state;
        })
        .When("AddSqlFileWarnings task is executed without database name", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.Engine,
                ScriptsDirectory = s.TempDir,
                DatabaseName = "", // No database name
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s, result);
        })
        .Then("task succeeds", r => r.result)
        .And("file contains warning header", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.s.TempDir, "test.sql"));
            return content.Contains("AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY");
        })
        .And("file does not mention specific database", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.s.TempDir, "test.sql"));
            return !content.Contains("database:");
        })
        .Finally(r => Cleanup(r.s))
        .AssertPassed();
    }

    [Scenario("Continues processing when individual file fails")]
    [Fact]
    public async Task Continues_when_individual_file_fails()
    {
        await Given("multiple SQL files with one read-only", () =>
        {
            var state = Setup();
            var file1 = Path.Combine(state.TempDir, "file1.sql");
            var file2 = Path.Combine(state.TempDir, "file2.sql");
            var file3 = Path.Combine(state.TempDir, "file3.sql");

            File.WriteAllText(file1, "CREATE TABLE Test1 (Id INT);");
            File.WriteAllText(file2, "CREATE TABLE Test2 (Id INT);");
            File.WriteAllText(file3, "CREATE TABLE Test3 (Id INT);");

            // Make file2 read-only to cause a failure
            File.SetAttributes(file2, FileAttributes.ReadOnly);

            return (state, file2);
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.state.Engine,
                ScriptsDirectory = s.state.TempDir,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s.state, s.file2, result, task.FilesProcessed, s.state.Engine.Warnings);
        })
        .Then("task succeeds", r => r.result)
        .And("processes two files successfully", r => r.FilesProcessed == 2)
        .And("warning is logged for failed file", r => r.Warnings.Any(w => w.Message?.Contains("Failed to process") == true))
        .Finally(r =>
        {
            // Remove read-only attribute before cleanup
            if (File.Exists(r.file2))
            {
                File.SetAttributes(r.file2, FileAttributes.Normal);
            }
            Cleanup(r.state);
        })
        .AssertPassed();
    }

    [Scenario("Handles UTF-8 encoded SQL files correctly")]
    [Fact]
    public async Task Handles_utf8_encoding_correctly()
    {
        await Given("a SQL file with UTF-8 content", () =>
        {
            var state = Setup();
            var sqlFile = Path.Combine(state.TempDir, "test.sql");
            var content = "-- Comment with special chars: é, ñ, 中文\nCREATE TABLE Test (Id INT);";
            File.WriteAllText(sqlFile, content, System.Text.Encoding.UTF8);
            return (state, originalContent: content);
        })
        .When("AddSqlFileWarnings task is executed", s =>
        {
            var task = new AddSqlFileWarnings
            {
                BuildEngine = s.state.Engine,
                ScriptsDirectory = s.state.TempDir,
                LogVerbosity = "minimal"
            };
            var result = task.Execute();
            return (s.state, s.originalContent, result);
        })
        .Then("task succeeds", r => r.result)
        .And("file preserves UTF-8 content", r =>
        {
            var content = File.ReadAllText(Path.Combine(r.state.TempDir, "test.sql"));
            return content.Contains("é, ñ, 中文") && content.Contains(r.originalContent);
        })
        .Finally(r => Cleanup(r.state))
        .AssertPassed();
    }

    // Note: JD0025 error path (top-level exception) is difficult to test in a unit test
    // as it requires triggering an unhandled exception during Directory.GetFiles or file processing.
    // This error path exists for unexpected failures and is covered by the error handling
    // implementation in AddSqlFileWarnings.cs:79-84
}
