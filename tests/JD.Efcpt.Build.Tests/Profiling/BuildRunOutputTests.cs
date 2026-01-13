using JD.Efcpt.Build.Tasks.Profiling;
using System.Text.Json;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using ProfilingTaskStatus = JD.Efcpt.Build.Tasks.Profiling.TaskStatus;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the BuildRunOutput data model and related classes.
/// </summary>
[Feature("BuildRunOutput: Data model serialization and structure")]
[Collection(nameof(AssemblySetup))]
public sealed class BuildRunOutputTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("BuildRunOutput serializes to JSON")]
    [Fact]
    public async Task BuildRunOutput_serializes_to_json()
    {
        string? json = null;

        await Given("a BuildRunOutput with data", () =>
            {
                return new BuildRunOutput
                {
                    SchemaVersion = "1.0.0",
                    RunId = Guid.NewGuid().ToString(),
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow.AddMinutes(1),
                    Duration = TimeSpan.FromMinutes(1),
                    Status = BuildStatus.Success,
                    Project = new ProjectInfo
                    {
                        Path = "/test/project.csproj",
                        Name = "TestProject"
                    }
                };
            })
            .When("object is serialized", output =>
            {
                json = JsonSerializer.Serialize(output);
                return output;
            })
            .Then("JSON is not empty", _ => !string.IsNullOrWhiteSpace(json))
            .And("JSON contains schema version", _ => json!.Contains("\"schemaVersion\""))
            .And("JSON contains runId", _ => json!.Contains("\"runId\""))
            .AssertPassed();
    }

    [Scenario("BuildRunOutput deserializes from JSON")]
    [Fact]
    public async Task BuildRunOutput_deserializes_from_json()
    {
        BuildRunOutput? deserialized = null;

        await Given("valid JSON", () =>
            {
                var obj = new BuildRunOutput
                {
                    SchemaVersion = "1.0.0",
                    Project = new ProjectInfo { Name = "Test" }
                };
                return JsonSerializer.Serialize(obj);
            })
            .When("JSON is deserialized", json =>
            {
                deserialized = JsonSerializer.Deserialize<BuildRunOutput>(json);
                return json;
            })
            .Then("object is not null", _ => deserialized != null)
            .And("schema version is correct", _ => deserialized!.SchemaVersion == "1.0.0")
            .AssertPassed();
    }

    [Scenario("BuildStatus enum has all expected values")]
    [Theory]
    [InlineData(BuildStatus.Success)]
    [InlineData(BuildStatus.Failed)]
    [InlineData(BuildStatus.Skipped)]
    [InlineData(BuildStatus.Canceled)]
    public async Task BuildStatus_enum_has_expected_values(BuildStatus status)
    {
        await Given("a BuildStatus value", () => status)
            .When("value is checked", s => s)
            .Then("value is defined", s => Enum.IsDefined(typeof(BuildStatus), s))
            .AssertPassed();
    }

    [Scenario("TaskStatus enum has all expected values")]
    [Theory]
    [InlineData(ProfilingTaskStatus.Success)]
    [InlineData(ProfilingTaskStatus.Failed)]
    [InlineData(ProfilingTaskStatus.Skipped)]
    [InlineData(ProfilingTaskStatus.Canceled)]
    public async Task TaskStatus_enum_has_expected_values(ProfilingTaskStatus status)
    {
        await Given("a TaskStatus value", () => status)
            .When("value is checked", s => s)
            .Then("value is defined", s => Enum.IsDefined(typeof(ProfilingTaskStatus), s))
            .AssertPassed();
    }

    [Scenario("DiagnosticLevel enum has all expected values")]
    [Theory]
    [InlineData(DiagnosticLevel.Info)]
    [InlineData(DiagnosticLevel.Warning)]
    [InlineData(DiagnosticLevel.Error)]
    public async Task DiagnosticLevel_enum_has_expected_values(DiagnosticLevel level)
    {
        await Given("a DiagnosticLevel value", () => level)
            .When("value is checked", l => l)
            .Then("value is defined", l => Enum.IsDefined(typeof(DiagnosticLevel), l))
            .AssertPassed();
    }

    [Scenario("ProjectInfo serializes correctly")]
    [Fact]
    public async Task ProjectInfo_serializes_correctly()
    {
        string? json = null;

        await Given("a ProjectInfo object", () => new ProjectInfo
            {
                Path = "/test/project.csproj",
                Name = "TestProject",
                TargetFramework = "net8.0",
                Configuration = "Debug"
            })
            .When("object is serialized", info =>
            {
                json = JsonSerializer.Serialize(info);
                return info;
            })
            .Then("JSON contains path", _ => json!.Contains("\"path\""))
            .And("JSON contains name", _ => json!.Contains("\"name\""))
            .And("JSON contains targetFramework", _ => json!.Contains("\"targetFramework\""))
            .AssertPassed();
    }

    [Scenario("BuildConfiguration serializes correctly")]
    [Fact]
    public async Task BuildConfiguration_serializes_correctly()
    {
        string? json = null;

        await Given("a BuildConfiguration object", () => new BuildConfiguration
            {
                ConfigPath = "/test/config.json",
                DacpacPath = "/test/database.dacpac",
                Provider = "mssql"
            })
            .When("object is serialized", config =>
            {
                json = JsonSerializer.Serialize(config);
                return config;
            })
            .Then("JSON contains configPath", _ => json!.Contains("\"configPath\""))
            .And("JSON contains dacpacPath", _ => json!.Contains("\"dacpacPath\""))
            .And("JSON contains provider", _ => json!.Contains("\"provider\""))
            .AssertPassed();
    }

    [Scenario("ArtifactInfo serializes correctly")]
    [Fact]
    public async Task ArtifactInfo_serializes_correctly()
    {
        string? json = null;

        await Given("an ArtifactInfo object", () => new ArtifactInfo
            {
                Path = "/output/model.cs",
                Type = "GeneratedModel",
                Size = 1024,
                Hash = "abc123"
            })
            .When("object is serialized", artifact =>
            {
                json = JsonSerializer.Serialize(artifact);
                return artifact;
            })
            .Then("JSON contains path", _ => json!.Contains("\"path\""))
            .And("JSON contains type", _ => json!.Contains("\"type\""))
            .And("JSON contains size", _ => json!.Contains("\"size\""))
            .And("JSON contains hash", _ => json!.Contains("\"hash\""))
            .AssertPassed();
    }

    [Scenario("DiagnosticMessage serializes correctly")]
    [Fact]
    public async Task DiagnosticMessage_serializes_correctly()
    {
        string? json = null;

        await Given("a DiagnosticMessage object", () => new DiagnosticMessage
            {
                Level = DiagnosticLevel.Warning,
                Code = "WARN001",
                Message = "Test warning",
                Timestamp = DateTimeOffset.UtcNow
            })
            .When("object is serialized", diag =>
            {
                json = JsonSerializer.Serialize(diag);
                return diag;
            })
            .Then("JSON contains level", _ => json!.Contains("\"level\""))
            .And("JSON contains code", _ => json!.Contains("\"code\""))
            .And("JSON contains message", _ => json!.Contains("\"message\""))
            .And("JSON contains timestamp", _ => json!.Contains("\"timestamp\""))
            .AssertPassed();
    }

    [Scenario("BuildGraph serializes correctly")]
    [Fact]
    public async Task BuildGraph_serializes_correctly()
    {
        string? json = null;

        await Given("a BuildGraph object", () => new BuildGraph
            {
                TotalTasks = 5,
                SuccessfulTasks = 4,
                FailedTasks = 1,
                SkippedTasks = 0
            })
            .When("object is serialized", graph =>
            {
                json = JsonSerializer.Serialize(graph);
                return graph;
            })
            .Then("JSON contains totalTasks", _ => json!.Contains("\"totalTasks\""))
            .And("JSON contains successfulTasks", _ => json!.Contains("\"successfulTasks\""))
            .And("JSON contains failedTasks", _ => json!.Contains("\"failedTasks\""))
            .AssertPassed();
    }

    [Scenario("BuildGraphNode serializes with hierarchy")]
    [Fact]
    public async Task BuildGraphNode_serializes_with_hierarchy()
    {
        string? json = null;

        await Given("a BuildGraphNode with children", () =>
            {
                var parent = new BuildGraphNode
                {
                    Task = new TaskExecution { Name = "ParentTask" }
                };
                var child = new BuildGraphNode
                {
                    ParentId = parent.Id,
                    Task = new TaskExecution { Name = "ChildTask" }
                };
                parent.Children.Add(child);
                return parent;
            })
            .When("object is serialized", node =>
            {
                json = JsonSerializer.Serialize(node);
                return node;
            })
            .Then("JSON contains parent task", _ => json!.Contains("ParentTask"))
            .And("JSON contains child task", _ => json!.Contains("ChildTask"))
            .And("JSON contains children array", _ => json!.Contains("\"children\""))
            .AssertPassed();
    }

    [Scenario("TaskExecution serializes with all properties")]
    [Fact]
    public async Task TaskExecution_serializes_with_all_properties()
    {
        string? json = null;

        await Given("a TaskExecution with full data", () => new TaskExecution
            {
                Name = "TestTask",
                Version = "1.0.0",
                Type = "MSBuild",
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddSeconds(10),
                Duration = TimeSpan.FromSeconds(10),
                Status = ProfilingTaskStatus.Success,
                Initiator = "TestTarget",
                Inputs = new Dictionary<string, object?> { ["input1"] = "value1" },
                Outputs = new Dictionary<string, object?> { ["output1"] = "result1" }
            })
            .When("object is serialized", task =>
            {
                json = JsonSerializer.Serialize(task);
                return task;
            })
            .Then("JSON contains name", _ => json!.Contains("\"name\""))
            .And("JSON contains inputs", _ => json!.Contains("\"inputs\""))
            .And("JSON contains outputs", _ => json!.Contains("\"outputs\""))
            .And("JSON contains duration", _ => json!.Contains("\"duration\""))
            .And("JSON contains status", _ => json!.Contains("\"status\""))
            .AssertPassed();
    }

    [Scenario("Extensions dictionary is supported")]
    [Fact]
    public async Task Extensions_dictionary_is_supported()
    {
        string? json = null;

        await Given("a BuildRunOutput with extensions", () =>
            {
                var output = new BuildRunOutput
                {
                    SchemaVersion = "1.0.0"
                };
                output.Extensions = new Dictionary<string, object?>
                {
                    ["customField"] = "customValue",
                    ["numericField"] = 42
                };
                return output;
            })
            .When("object is serialized", output =>
            {
                json = JsonSerializer.Serialize(output);
                return output;
            })
            .Then("JSON contains custom fields", _ => 
                json!.Contains("\"customField\"") && json!.Contains("\"numericField\""))
            .AssertPassed();
    }
}
