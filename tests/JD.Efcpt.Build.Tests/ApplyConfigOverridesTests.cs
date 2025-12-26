using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the ApplyConfigOverrides MSBuild task.
/// </summary>
[Feature("ApplyConfigOverrides: MSBuild property overrides for efcpt-config.json")]
[Collection(nameof(AssemblySetup))]
public sealed class ApplyConfigOverridesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestFolder Folder,
        string ConfigPath,
        TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        ApplyConfigOverrides Task,
        bool Success);

    private static SetupState SetupWithDefaultConfig()
    {
        var folder = new TestFolder();
        var config = folder.WriteFile("efcpt-config.json", """
            {
              "names": {
                "root-namespace": "OriginalNamespace"
              },
              "code-generation": {
                "use-database-names": false
              }
            }
            """);
        var engine = new TestBuildEngine();
        return new SetupState(folder, config, engine);
    }

    private static SetupState SetupWithMinimalConfig()
    {
        var folder = new TestFolder();
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var engine = new TestBuildEngine();
        return new SetupState(folder, config, engine);
    }

    private static TaskResult ExecuteTask(
        SetupState setup,
        bool isUsingDefaultConfig = true,
        bool applyOverrides = true,
        string rootNamespace = "",
        string dbContextName = "",
        string useDatabaseNames = "",
        string useNullableReferenceTypes = "",
        string generationType = "",
        string preserveCasingWithRegex = "")
    {
        var task = new ApplyConfigOverrides
        {
            BuildEngine = setup.Engine,
            StagedConfigPath = setup.ConfigPath,
            IsUsingDefaultConfig = isUsingDefaultConfig ? "true" : "false",
            ApplyOverrides = applyOverrides ? "true" : "false",
            RootNamespace = rootNamespace,
            DbContextName = dbContextName,
            UseDatabaseNames = useDatabaseNames,
            UseNullableReferenceTypes = useNullableReferenceTypes,
            GenerationType = generationType,
            PreserveCasingWithRegex = preserveCasingWithRegex
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static string ReadConfig(SetupState setup) => File.ReadAllText(setup.ConfigPath);

    [Scenario("Applies string override to names section")]
    [Fact]
    public async Task Applies_root_namespace_override()
    {
        await Given("a config file with existing root-namespace", SetupWithDefaultConfig)
            .When("task executes with RootNamespace override", s =>
                ExecuteTask(s, rootNamespace: "MyNewNamespace"))
            .Then("task succeeds", r => r.Success)
            .And("config contains new root-namespace", r =>
                ReadConfig(r.Setup).Contains("\"root-namespace\": \"MyNewNamespace\""))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Applies boolean override to code-generation section")]
    [Fact]
    public async Task Applies_use_database_names_override()
    {
        await Given("a config file with use-database-names false", SetupWithDefaultConfig)
            .When("task executes with UseDatabaseNames=true", s =>
                ExecuteTask(s, useDatabaseNames: "true"))
            .Then("task succeeds", r => r.Success)
            .And("config contains use-database-names true", r =>
                ReadConfig(r.Setup).Contains("\"use-database-names\": true"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates section if it doesn't exist")]
    [Fact]
    public async Task Creates_names_section_if_missing()
    {
        await Given("a minimal config file without names section", SetupWithMinimalConfig)
            .When("task executes with DbContextName override", s =>
                ExecuteTask(s, dbContextName: "MyDbContext"))
            .Then("task succeeds", r => r.Success)
            .And("config contains names section", r =>
                ReadConfig(r.Setup).Contains("\"names\""))
            .And("config contains dbcontext-name", r =>
                ReadConfig(r.Setup).Contains("\"dbcontext-name\": \"MyDbContext\""))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Skips overrides when ApplyOverrides is false and not default config")]
    [Fact]
    public async Task Skips_overrides_when_disabled()
    {
        await Given("a config file with existing root-namespace", SetupWithDefaultConfig)
            .When("task executes with ApplyOverrides=false on user config", s =>
                ExecuteTask(s, isUsingDefaultConfig: false, applyOverrides: false, rootNamespace: "ShouldNotApply"))
            .Then("task succeeds", r => r.Success)
            .And("config still contains original root-namespace", r =>
                ReadConfig(r.Setup).Contains("\"root-namespace\": \"OriginalNamespace\""))
            .And("config does not contain the override value", r =>
                !ReadConfig(r.Setup).Contains("ShouldNotApply"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Always applies overrides when using default config")]
    [Fact]
    public async Task Always_applies_when_default_config()
    {
        await Given("a config file", SetupWithDefaultConfig)
            .When("task executes with ApplyOverrides=false but IsUsingDefaultConfig=true", s =>
                ExecuteTask(s, isUsingDefaultConfig: true, applyOverrides: false, rootNamespace: "ShouldApply"))
            .Then("task succeeds", r => r.Success)
            .And("config contains override despite ApplyOverrides=false", r =>
                ReadConfig(r.Setup).Contains("\"root-namespace\": \"ShouldApply\""))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Does not modify config when no overrides specified")]
    [Fact]
    public async Task No_modification_when_no_overrides()
    {
        await Given("a config file", SetupWithDefaultConfig)
            .When("task executes with no override properties set", s =>
            {
                var originalContent = ReadConfig(s);
                var result = ExecuteTask(s);
                return (result, originalContent);
            })
            .Then("task succeeds", r => r.result.Success)
            .And("config content is unchanged", r =>
                ReadConfig(r.result.Setup) == r.originalContent)
            .Finally(r => r.result.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Applies multiple overrides in a single execution")]
    [Fact]
    public async Task Applies_multiple_overrides()
    {
        await Given("a minimal config file", SetupWithMinimalConfig)
            .When("task executes with multiple overrides", s =>
                ExecuteTask(s,
                    rootNamespace: "MultiNamespace",
                    dbContextName: "MultiContext",
                    useDatabaseNames: "true",
                    useNullableReferenceTypes: "true"))
            .Then("task succeeds", r => r.Success)
            .And("config contains root-namespace", r =>
                ReadConfig(r.Setup).Contains("\"root-namespace\": \"MultiNamespace\""))
            .And("config contains dbcontext-name", r =>
                ReadConfig(r.Setup).Contains("\"dbcontext-name\": \"MultiContext\""))
            .And("config contains use-database-names", r =>
                ReadConfig(r.Setup).Contains("\"use-database-names\": true"))
            .And("config contains use-nullable-reference-types", r =>
                ReadConfig(r.Setup).Contains("\"use-nullable-reference-types\": true"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles false boolean value correctly")]
    [Fact]
    public async Task Handles_false_boolean_value()
    {
        await Given("a minimal config file", SetupWithMinimalConfig)
            .When("task executes with UseDatabaseNames=false", s =>
                ExecuteTask(s, useDatabaseNames: "false"))
            .Then("task succeeds", r => r.Success)
            .And("config contains use-database-names false", r =>
                ReadConfig(r.Setup).Contains("\"use-database-names\": false"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Applies string override to code-generation section")]
    [Fact]
    public async Task Applies_generation_type_override()
    {
        await Given("a minimal config file", SetupWithMinimalConfig)
            .When("task executes with GenerationType override", s =>
                ExecuteTask(s, generationType: "dbcontext"))
            .Then("task succeeds", r => r.Success)
            .And("config contains type property", r =>
                ReadConfig(r.Setup).Contains("\"type\": \"dbcontext\""))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Empty string properties are not applied")]
    [Fact]
    public async Task Empty_properties_not_applied()
    {
        await Given("a minimal config file", SetupWithMinimalConfig)
            .When("task executes with empty RootNamespace but valid DbContextName", s =>
                ExecuteTask(s, rootNamespace: "", dbContextName: "ValidContext"))
            .Then("task succeeds", r => r.Success)
            .And("config contains dbcontext-name", r =>
                ReadConfig(r.Setup).Contains("\"dbcontext-name\": \"ValidContext\""))
            .And("config does not contain root-namespace", r =>
                !ReadConfig(r.Setup).Contains("root-namespace"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Preserves existing properties not being overridden")]
    [Fact]
    public async Task Preserves_existing_properties()
    {
        await Given("a config file with use-database-names", SetupWithDefaultConfig)
            .When("task executes with only RootNamespace override", s =>
                ExecuteTask(s, rootNamespace: "NewNamespace"))
            .Then("task succeeds", r => r.Success)
            .And("config contains new root-namespace", r =>
                ReadConfig(r.Setup).Contains("\"root-namespace\": \"NewNamespace\""))
            .And("config still contains original use-database-names", r =>
                ReadConfig(r.Setup).Contains("\"use-database-names\": false"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Applies boolean override to replacements section")]
    [Fact]
    public async Task Applies_preserve_casing_with_regex_override()
    {
        await Given("a minimal config file", SetupWithMinimalConfig)
            .When("task executes with PreserveCasingWithRegex=true", s =>
                ExecuteTask(s, preserveCasingWithRegex: "true"))
            .Then("task succeeds", r => r.Success)
            .And("config contains replacements section", r =>
                ReadConfig(r.Setup).Contains("\"replacements\""))
            .And("config contains preserve-casing-with-regex true", r =>
                ReadConfig(r.Setup).Contains("\"preserve-casing-with-regex\": true"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
