using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.ConnectionStrings;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.ConnectionStrings;

[Feature("AppSettingsConnectionStringParser: parses connection strings from appsettings.json files")]
[Collection(nameof(AssemblySetup))]
public sealed class AppSettingsConnectionStringParserTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(TestFolder Folder, string FilePath, string KeyName);
    private sealed record ParseResult(SetupState Setup, ConnectionStringResult Result);

    private static BuildLog CreateTestLog()
    {
        var task = new DummyTask { BuildEngine = new TestBuildEngine() };
        return new BuildLog(task.Log, "minimal");
    }

    private static ParseResult ExecuteParse(SetupState setup)
    {
        var parser = new AppSettingsConnectionStringParser();
        var log = CreateTestLog();
        var result = AppSettingsConnectionStringParser.Parse(setup.FilePath, setup.KeyName, log);
        return new ParseResult(setup, result);
    }

    [Scenario("Valid appsettings with specified key")]
    [Fact]
    public async Task Valid_appsettings_with_specified_key()
    {
        await Given("appsettings.json with DefaultConnection", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("appsettings.json",
                    """
                    {
                      "ConnectionStrings": {
                        "DefaultConnection": "Server=localhost;Database=TestDb;",
                        "SecondaryConnection": "Server=remote;Database=OtherDb;"
                      }
                    }
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("succeeds", r => r.Result.Success)
            .And("connection string is correct", r => r.Result.ConnectionString == "Server=localhost;Database=TestDb;")
            .And("source is correct", r => r.Result.Source == r.Setup.FilePath)
            .And("key name is correct", r => r.Result.KeyName == "DefaultConnection")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Valid appsettings missing key falls back")]
    [Fact]
    public async Task Valid_appsettings_missing_key_falls_back()
    {
        await Given("appsettings.json without specified key", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("appsettings.json",
                    """
                    {
                      "ConnectionStrings": {
                        "ProductionDb": "Server=prod;Database=ProdDb;"
                      }
                    }
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("succeeds", r => r.Result.Success)
            .And("uses first available connection string", r => r.Result.ConnectionString == "Server=prod;Database=ProdDb;")
            .And("key name is first available", r => r.Result.KeyName == "ProductionDb")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No ConnectionStrings section")]
    [Fact]
    public async Task No_connection_strings_section()
    {
        await Given("appsettings.json without ConnectionStrings section", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("appsettings.json",
                    """
                    {
                      "Logging": {
                        "LogLevel": "Debug"
                      }
                    }
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Empty ConnectionStrings section")]
    [Fact]
    public async Task Empty_connection_strings_section()
    {
        await Given("appsettings.json with empty ConnectionStrings", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("appsettings.json",
                    """
                    {
                      "ConnectionStrings": {}
                    }
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Invalid JSON")]
    [Fact]
    public async Task Invalid_json()
    {
        await Given("invalid JSON file", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("appsettings.json", "{ invalid json }");
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Non-existent file")]
    [Fact]
    public async Task Non_existent_file()
    {
        await Given("non-existent file path", () =>
            {
                var folder = new TestFolder();
                var filePath = "C:\\nonexistent\\appsettings.json";
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Empty connection string value")]
    [Fact]
    public async Task Empty_connection_string_value()
    {
        await Given("appsettings.json with empty connection string", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("appsettings.json",
                    """
                    {
                      "ConnectionStrings": {
                        "DefaultConnection": ""
                      }
                    }
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    private sealed class DummyTask : Microsoft.Build.Utilities.Task
    {
        public override bool Execute() => true;
    }
}
