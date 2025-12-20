using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.ConnectionStrings;
using JD.Efcpt.Build.Tests.Infrastructure;
using Microsoft.Build.Utilities;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.ConnectionStrings;

[Feature("AppConfigConnectionStringParser: parses connection strings from app.config/web.config files")]
[Collection(nameof(AssemblySetup))]
public sealed class AppConfigConnectionStringParserTests(ITestOutputHelper output) : TinyBddXunitBase(output)
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
        var parser = new AppConfigConnectionStringParser();
        var log = CreateTestLog();
        var result = parser.Parse(setup.FilePath, setup.KeyName, log);
        return new ParseResult(setup, result);
    }

    [Scenario("Valid app.config with specified key")]
    [Fact]
    public async Task Valid_app_config_with_specified_key()
    {
        await Given("app.config with DefaultConnection", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <connectionStrings>
                        <add name="DefaultConnection" connectionString="Server=localhost;Database=TestDb;" providerName="System.Data.SqlClient" />
                        <add name="SecondaryConnection" connectionString="Server=remote;Database=OtherDb;" providerName="System.Data.SqlClient" />
                      </connectionStrings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("succeeds", r => r.Result.Success)
            .And("connection string is correct", r => r.Result.ConnectionString == "Server=localhost;Database=TestDb;")
            .And("source is correct", r => r.Result.Source == r.Setup.FilePath)
            .And("key name is correct", r => r.Result.KeyName == "DefaultConnection")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Valid web.config with specified key")]
    [Fact]
    public async Task Valid_web_config_with_specified_key()
    {
        await Given("web.config with ApplicationDb", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("web.config",
                    """
                    <?xml version="1.0"?>
                    <configuration>
                      <connectionStrings>
                        <add name="ApplicationDb" connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=MyApp;Integrated Security=True" />
                      </connectionStrings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "ApplicationDb");
            })
            .When("parse", ExecuteParse)
            .Then("succeeds", r => r.Result.Success)
            .And("connection string is correct", r => r.Result.ConnectionString == "Data Source=.\\SQLEXPRESS;Initial Catalog=MyApp;Integrated Security=True")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("App.config missing key falls back")]
    [Fact]
    public async Task App_config_missing_key_falls_back()
    {
        await Given("app.config without specified key", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <connectionStrings>
                        <add name="ProductionDb" connectionString="Server=prod;Database=ProdDb;" />
                      </connectionStrings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("succeeds", r => r.Result.Success)
            .And("uses first available connection string", r => r.Result.ConnectionString == "Server=prod;Database=ProdDb;")
            .And("key name is first available", r => r.Result.KeyName == "ProductionDb")
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("No connectionStrings section")]
    [Fact]
    public async Task No_connection_strings_section()
    {
        await Given("app.config without connectionStrings section", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <appSettings>
                        <add key="Setting1" value="Value1" />
                      </appSettings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Empty connectionStrings section")]
    [Fact]
    public async Task Empty_connection_strings_section()
    {
        await Given("app.config with empty connectionStrings", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <connectionStrings>
                      </connectionStrings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Invalid XML")]
    [Fact]
    public async Task Invalid_xml()
    {
        await Given("invalid XML file", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config", "<configuration><unclosed>");
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Non-existent file")]
    [Fact]
    public async Task Non_existent_file()
    {
        await Given("non-existent file path", () =>
            {
                var folder = new TestFolder();
                var filePath = "C:\\nonexistent\\app.config";
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Empty connection string value")]
    [Fact]
    public async Task Empty_connection_string_value()
    {
        await Given("app.config with empty connection string", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <connectionStrings>
                        <add name="DefaultConnection" connectionString="" />
                      </connectionStrings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Missing connectionString attribute")]
    [Fact]
    public async Task Missing_connection_string_attribute()
    {
        await Given("app.config missing connectionString attribute", () =>
            {
                var folder = new TestFolder();
                var filePath = folder.WriteFile("app.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <connectionStrings>
                        <add name="DefaultConnection" />
                      </connectionStrings>
                    </configuration>
                    """);
                return new SetupState(folder, filePath, "DefaultConnection");
            })
            .When("parse", ExecuteParse)
            .Then("fails", r => !r.Result.Success)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    private sealed class DummyTask : Microsoft.Build.Utilities.Task
    {
        public override bool Execute() => true;
    }
}
