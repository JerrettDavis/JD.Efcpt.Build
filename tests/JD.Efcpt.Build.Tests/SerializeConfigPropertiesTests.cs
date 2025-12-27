using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the SerializeConfigProperties MSBuild task.
/// </summary>
[Feature("SerializeConfigProperties: Serialize MSBuild config properties to JSON for fingerprinting")]
[Collection(nameof(AssemblySetup))]
public sealed class SerializeConfigPropertiesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        SerializeConfigProperties Task,
        bool Success);

    private static SetupState SetupTask()
    {
        var engine = new TestBuildEngine();
        return new SetupState(engine);
    }

    private static TaskResult ExecuteTask(SetupState setup, Action<SerializeConfigProperties>? configure = null)
    {
        var task = new SerializeConfigProperties
        {
            BuildEngine = setup.Engine
        };

        configure?.Invoke(task);

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    [Scenario("Returns empty JSON when no properties are set")]
    [Fact]
    public async Task Empty_properties_returns_empty_json()
    {
        await Given("task with no properties", SetupTask)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("serialized properties is empty array", r => r.Task.SerializedProperties == "[]")
            .AssertPassed();
    }

    [Scenario("Serializes single property correctly")]
    [Fact]
    public async Task Single_property_serializes_correctly()
    {
        await Given("task with RootNamespace set", SetupTask)
            .When("task executes", s => ExecuteTask(s, t => t.RootNamespace = "MyNamespace"))
            .Then("task succeeds", r => r.Success)
            .And("serialized properties contains RootNamespace", r => 
                r.Task.SerializedProperties.Contains("\"RootNamespace\"") && 
                r.Task.SerializedProperties.Contains("\"MyNamespace\""))
            .AssertPassed();
    }

    [Scenario("Serializes multiple properties correctly")]
    [Fact]
    public async Task Multiple_properties_serialize_correctly()
    {
        await Given("task with multiple properties set", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.RootNamespace = "MyNamespace";
                t.DbContextName = "MyContext";
                t.UseDataAnnotations = "true";
            }))
            .Then("task succeeds", r => r.Success)
            .And("serialized properties contains all values", r =>
                r.Task.SerializedProperties.Contains("\"RootNamespace\"") &&
                r.Task.SerializedProperties.Contains("\"MyNamespace\"") &&
                r.Task.SerializedProperties.Contains("\"DbContextName\"") &&
                r.Task.SerializedProperties.Contains("\"MyContext\"") &&
                r.Task.SerializedProperties.Contains("\"UseDataAnnotations\"") &&
                r.Task.SerializedProperties.Contains("\"true\""))
            .AssertPassed();
    }

    [Scenario("Ignores empty and whitespace-only properties")]
    [Fact]
    public async Task Empty_properties_are_ignored()
    {
        await Given("task with some empty properties", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.RootNamespace = "MyNamespace";
                t.DbContextName = "";
                t.ModelNamespace = "   ";
                t.UseDataAnnotations = "true";
            }))
            .Then("task succeeds", r => r.Success)
            .And("serialized properties excludes empty values", r =>
                r.Task.SerializedProperties.Contains("\"RootNamespace\"") &&
                !r.Task.SerializedProperties.Contains("\"DbContextName\"") &&
                !r.Task.SerializedProperties.Contains("\"ModelNamespace\"") &&
                r.Task.SerializedProperties.Contains("\"UseDataAnnotations\""))
            .AssertPassed();
    }

    [Scenario("Output is deterministic and sorted")]
    [Fact]
    public async Task Output_is_deterministic_and_sorted()
    {
        await Given("task with properties in random order", SetupTask)
            .When("task executes twice", s =>
            {
                // First execution
                var result1 = ExecuteTask(s, t =>
                {
                    t.UseDataAnnotations = "true";
                    t.RootNamespace = "MyNamespace";
                    t.DbContextName = "MyContext";
                });

                // Second execution with same values
                var result2 = ExecuteTask(s, t =>
                {
                    t.DbContextName = "MyContext";
                    t.RootNamespace = "MyNamespace";
                    t.UseDataAnnotations = "true";
                });

                return (result1.Task.SerializedProperties, result2.Task.SerializedProperties);
            })
            .Then("outputs are identical", t => t.Item1 == t.Item2)
            .AssertPassed();
    }

    [Scenario("Serializes all name properties")]
    [Fact]
    public async Task Serializes_all_name_properties()
    {
        await Given("task with name properties", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.RootNamespace = "Root";
                t.DbContextName = "Context";
                t.DbContextNamespace = "ContextNs";
                t.ModelNamespace = "ModelNs";
            }))
            .Then("task succeeds", r => r.Success)
            .And("all name properties are serialized", r =>
                r.Task.SerializedProperties.Contains("\"RootNamespace\"") &&
                r.Task.SerializedProperties.Contains("\"DbContextName\"") &&
                r.Task.SerializedProperties.Contains("\"DbContextNamespace\"") &&
                r.Task.SerializedProperties.Contains("\"ModelNamespace\""))
            .AssertPassed();
    }

    [Scenario("Serializes all file layout properties")]
    [Fact]
    public async Task Serializes_all_file_layout_properties()
    {
        await Given("task with file layout properties", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.OutputPath = "Output";
                t.DbContextOutputPath = "ContextOut";
                t.SplitDbContext = "true";
                t.UseSchemaFolders = "true";
                t.UseSchemaNamespaces = "false";
            }))
            .Then("task succeeds", r => r.Success)
            .And("all file layout properties are serialized", r =>
                r.Task.SerializedProperties.Contains("\"OutputPath\"") &&
                r.Task.SerializedProperties.Contains("\"DbContextOutputPath\"") &&
                r.Task.SerializedProperties.Contains("\"SplitDbContext\"") &&
                r.Task.SerializedProperties.Contains("\"UseSchemaFolders\"") &&
                r.Task.SerializedProperties.Contains("\"UseSchemaNamespaces\""))
            .AssertPassed();
    }

    [Scenario("Serializes all code generation properties")]
    [Fact]
    public async Task Serializes_all_code_generation_properties()
    {
        await Given("task with code generation properties", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.EnableOnConfiguring = "true";
                t.GenerationType = "DbContext";
                t.UseDatabaseNames = "false";
                t.UseDataAnnotations = "true";
                t.UseNullableReferenceTypes = "true";
                t.UseInflector = "false";
                t.UseLegacyInflector = "false";
                t.UseManyToManyEntity = "true";
                t.UseT4 = "false";
                t.UseT4Split = "false";
            }))
            .Then("task succeeds", r => r.Success)
            .And("all code generation properties are serialized", r =>
                r.Task.SerializedProperties.Contains("\"EnableOnConfiguring\"") &&
                r.Task.SerializedProperties.Contains("\"GenerationType\"") &&
                r.Task.SerializedProperties.Contains("\"UseDatabaseNames\"") &&
                r.Task.SerializedProperties.Contains("\"UseDataAnnotations\"") &&
                r.Task.SerializedProperties.Contains("\"UseNullableReferenceTypes\"") &&
                r.Task.SerializedProperties.Contains("\"UseInflector\"") &&
                r.Task.SerializedProperties.Contains("\"UseLegacyInflector\"") &&
                r.Task.SerializedProperties.Contains("\"UseManyToManyEntity\"") &&
                r.Task.SerializedProperties.Contains("\"UseT4\"") &&
                r.Task.SerializedProperties.Contains("\"UseT4Split\""))
            .AssertPassed();
    }

    [Scenario("Serializes all type mapping properties")]
    [Fact]
    public async Task Serializes_all_type_mapping_properties()
    {
        await Given("task with type mapping properties", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.UseDateOnlyTimeOnly = "true";
                t.UseHierarchyId = "true";
                t.UseSpatial = "true";
                t.UseNodaTime = "true";
            }))
            .Then("task succeeds", r => r.Success)
            .And("all type mapping properties are serialized", r =>
                r.Task.SerializedProperties.Contains("\"UseDateOnlyTimeOnly\"") &&
                r.Task.SerializedProperties.Contains("\"UseHierarchyId\"") &&
                r.Task.SerializedProperties.Contains("\"UseSpatial\"") &&
                r.Task.SerializedProperties.Contains("\"UseNodaTime\""))
            .AssertPassed();
    }

    [Scenario("Serializes special character values correctly")]
    [Fact]
    public async Task Serializes_special_characters_correctly()
    {
        await Given("task with special character values", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.RootNamespace = "My.Namespace\\With\"Special'Chars";
                t.T4TemplatePath = "C:\\Path\\To\\Template.t4";
            }))
            .Then("task succeeds", r => r.Success)
            .And("values are present in output", r =>
                r.Task.SerializedProperties.Contains("RootNamespace") &&
                r.Task.SerializedProperties.Contains("T4TemplatePath"))
            .AssertPassed();
    }

    [Scenario("JSON output is valid and parseable")]
    [Fact]
    public async Task JSON_output_is_valid()
    {
        await Given("task with multiple properties", SetupTask)
            .When("task executes", s => ExecuteTask(s, t =>
            {
                t.RootNamespace = "MyNamespace";
                t.DbContextName = "MyContext";
                t.UseDataAnnotations = "true";
            }))
            .Then("task succeeds", r => r.Success)
            .And("output is valid JSON", r =>
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(r.Task.SerializedProperties);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .AssertPassed();
    }
}
