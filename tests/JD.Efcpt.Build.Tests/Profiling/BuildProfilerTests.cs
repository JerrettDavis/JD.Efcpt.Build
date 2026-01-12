using JD.Efcpt.Build.Tasks.Profiling;
using System.Text.Json;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the BuildProfiler class that captures task execution telemetry.
/// </summary>
[Feature("BuildProfiler: Task execution profiling and telemetry capture")]
[Collection(nameof(AssemblySetup))]
public sealed class BuildProfilerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        BuildProfiler Profiler,
        string ProjectPath,
        string ProjectName);

    private static SetupState Setup()
    {
        var projectPath = "/test/project/TestProject.csproj";
        var projectName = "TestProject";
        var profiler = new BuildProfiler(
            enabled: true,
            projectPath,
            projectName,
            targetFramework: "net8.0",
            configuration: "Debug");

        return new SetupState(profiler, projectPath, projectName);
    }

    [Scenario("Disabled profiler has zero overhead")]
    [Fact]
    public async Task Disabled_profiler_has_zero_overhead()
    {
        BuildProfiler? profiler = null;

        await Given("a disabled profiler", () =>
            {
                profiler = new BuildProfiler(
                    enabled: false,
                    "/test/project.csproj",
                    "TestProject");
                return profiler;
            })
            .When("tasks are tracked", p =>
            {
                using var task = p.BeginTask("TestTask");
                return p;
            })
            .Then("profiler is disabled", p => !p.Enabled)
            .And("no overhead is incurred", p =>
            {
                var output = p.GetRunOutput();
                return output.BuildGraph.TotalTasks == 0;
            })
            .AssertPassed();
    }

    [Scenario("Profiler captures task execution")]
    [Fact]
    public async Task Profiler_captures_task_execution()
    {
        await Given("an enabled profiler", Setup)
            .When("a task is executed", s =>
            {
                var inputs = new Dictionary<string, object?> { ["Input1"] = "value1" };
                using var task = s.Profiler.BeginTask("TestTask", "TestInitiator", inputs);
                // Task completes here
                return s;
            })
            .Then("task is captured in build graph", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.BuildGraph.TotalTasks == 1 &&
                       output.BuildGraph.SuccessfulTasks == 1;
            })
            .And("task has correct name", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.BuildGraph.Nodes.Any(n => n.Task.Name == "TestTask");
            })
            .And("task has inputs", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Inputs.ContainsKey("Input1") &&
                       task.Inputs["Input1"]?.ToString() == "value1";
            })
            .AssertPassed();
    }

    [Scenario("Profiler captures nested tasks")]
    [Fact]
    public async Task Profiler_captures_nested_tasks()
    {
        await Given("an enabled profiler", Setup)
            .When("nested tasks are executed", s =>
            {
                using var parent = s.Profiler.BeginTask("ParentTask");
                using var child = s.Profiler.BeginTask("ChildTask");
                return s;
            })
            .Then("both tasks are captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.BuildGraph.TotalTasks == 2;
            })
            .And("child is nested under parent", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var parent = output.BuildGraph.Nodes.First();
                return parent.Children.Count == 1 &&
                       parent.Children[0].Task.Name == "ChildTask";
            })
            .AssertPassed();
    }

    [Scenario("Profiler captures build configuration")]
    [Fact]
    public async Task Profiler_captures_build_configuration()
    {
        await Given("an enabled profiler", Setup)
            .When("configuration is set", s =>
            {
                s.Profiler.SetConfiguration(new BuildConfiguration
                {
                    ConfigPath = "/test/config.json",
                    DacpacPath = "/test/database.dacpac",
                    Provider = "mssql"
                });
                return s;
            })
            .Then("configuration is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Configuration.ConfigPath == "/test/config.json" &&
                       output.Configuration.DacpacPath == "/test/database.dacpac" &&
                       output.Configuration.Provider == "mssql";
            })
            .AssertPassed();
    }

    [Scenario("Profiler captures artifacts")]
    [Fact]
    public async Task Profiler_captures_artifacts()
    {
        await Given("an enabled profiler", Setup)
            .When("artifacts are added", s =>
            {
                s.Profiler.AddArtifact(new ArtifactInfo
                {
                    Path = "/output/Model.g.cs",
                    Type = "GeneratedModel",
                    Size = 1024
                });
                return s;
            })
            .Then("artifact is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Artifacts.Count == 1 &&
                       output.Artifacts[0].Path == "/output/Model.g.cs" &&
                       output.Artifacts[0].Type == "GeneratedModel";
            })
            .AssertPassed();
    }

    [Scenario("Profiler captures diagnostics")]
    [Fact]
    public async Task Profiler_captures_diagnostics()
    {
        await Given("an enabled profiler", Setup)
            .When("diagnostics are added", s =>
            {
                s.Profiler.AddDiagnostic(DiagnosticLevel.Warning, "Test warning", "WARN001");
                return s;
            })
            .Then("diagnostic is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Diagnostics.Count == 1 &&
                       output.Diagnostics[0].Level == DiagnosticLevel.Warning &&
                       output.Diagnostics[0].Message == "Test warning" &&
                       output.Diagnostics[0].Code == "WARN001";
            })
            .AssertPassed();
    }

    [Scenario("Profiler captures metadata")]
    [Fact]
    public async Task Profiler_captures_metadata()
    {
        await Given("an enabled profiler", Setup)
            .When("metadata is added", s =>
            {
                s.Profiler.AddMetadata("key1", "value1");
                s.Profiler.AddMetadata("key2", 42);
                return s;
            })
            .Then("metadata is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Metadata.Count == 2 &&
                       output.Metadata["key1"]?.ToString() == "value1" &&
                       output.Metadata["key2"]?.ToString() == "42";
            })
            .AssertPassed();
    }

    [Scenario("Profiler writes JSON output")]
    [Fact]
    public async Task Profiler_writes_json_output()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"test-profile-{Guid.NewGuid()}.json");

        try
        {
            await Given("an enabled profiler with tasks", Setup)
                .When("tasks are executed", s =>
                {
                    using var task = s.Profiler.BeginTask("TestTask");
                    return s;
                })
                .And("profile is completed", s =>
                {
                    s.Profiler.Complete(outputPath);
                    return s;
                })
                .Then("output file exists", _ => File.Exists(outputPath))
                .And("output is valid JSON", _ =>
                {
                    var json = File.ReadAllText(outputPath);
                    var output = JsonSerializer.Deserialize<BuildRunOutput>(json);
                    return output != null;
                })
                .And("output has schema version", _ =>
                {
                    var json = File.ReadAllText(outputPath);
                    var output = JsonSerializer.Deserialize<BuildRunOutput>(json);
                    return output!.SchemaVersion == "1.0.0";
                })
                .AssertPassed();
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Scenario("Profiler captures timing information")]
    [Fact]
    public async Task Profiler_captures_timing_information()
    {
        await Given("an enabled profiler", Setup)
            .When("a task with delay is executed", (Func<SetupState, Task<SetupState>>)(async s =>
            {
                using var task = s.Profiler.BeginTask("SlowTask");
                await Task.Delay(100); // Simulate work
                return s;
            }))
            .Then("task duration is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Duration.TotalMilliseconds >= 100;
            })
            .And("task has start and end times", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.EndTime.HasValue &&
                       task.EndTime.Value > task.StartTime;
            })
            .AssertPassed();
    }
}
