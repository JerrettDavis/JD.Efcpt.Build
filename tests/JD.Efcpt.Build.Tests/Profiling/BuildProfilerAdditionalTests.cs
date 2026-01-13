using JD.Efcpt.Build.Tasks.Profiling;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Additional tests for BuildProfiler edge cases and complete coverage.
/// </summary>
[Feature("BuildProfiler: Additional coverage for edge cases")]
[Collection(nameof(AssemblySetup))]
public sealed class BuildProfilerAdditionalTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(BuildProfiler Profiler, string ProjectPath);

    private static SetupState Setup(bool enabled = true)
    {
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";
        var profiler = new BuildProfiler(enabled, projectPath, "TestProject", "net8.0", "Debug");
        return new SetupState(profiler, projectPath);
    }

    [Scenario("Task tracker SetOutputs with null is handled")]
    [Fact]
    public async Task Task_tracker_handles_null_outputs()
    {
        await Given("an enabled profiler", () => Setup())
            .When("a task completes without setting outputs", s =>
            {
                using (s.Profiler.BeginTask("TestTask"))
                {
                    // Don't set outputs
                }
                return s;
            })
            .Then("task has empty outputs dictionary", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Outputs != null && task.Outputs.Count == 0;
            })
            .AssertPassed();
    }

    [Scenario("Multiple metadata entries can be added")]
    [Fact]
    public async Task Multiple_metadata_entries_can_be_added()
    {
        await Given("an enabled profiler", () => Setup())
            .When("multiple metadata entries are added", s =>
            {
                s.Profiler.AddMetadata("key1", "value1");
                s.Profiler.AddMetadata("key2", 123);
                s.Profiler.AddMetadata("key3", true);
                return s;
            })
            .Then("all metadata is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Metadata.Count == 3 &&
                       output.Metadata["key1"]?.ToString() == "value1" &&
                       output.Metadata["key2"]?.ToString() == "123" &&
                       output.Metadata["key3"]?.ToString() == "True";
            })
            .AssertPassed();
    }

    [Scenario("Multiple artifacts can be added")]
    [Fact]
    public async Task Multiple_artifacts_can_be_added()
    {
        await Given("an enabled profiler", () => Setup())
            .When("multiple artifacts are added", s =>
            {
                s.Profiler.AddArtifact(new ArtifactInfo { Path = "/file1.cs", Type = "Model" });
                s.Profiler.AddArtifact(new ArtifactInfo { Path = "/file2.cs", Type = "Context" });
                return s;
            })
            .Then("all artifacts are captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Artifacts.Count == 2 &&
                       output.Artifacts.Any(a => a.Path == "/file1.cs") &&
                       output.Artifacts.Any(a => a.Path == "/file2.cs");
            })
            .AssertPassed();
    }

    [Scenario("Multiple diagnostics can be added")]
    [Fact]
    public async Task Multiple_diagnostics_can_be_added()
    {
        await Given("an enabled profiler", () => Setup())
            .When("multiple diagnostics are added", s =>
            {
                s.Profiler.AddDiagnostic(DiagnosticLevel.Info, "Info message", "INFO001");
                s.Profiler.AddDiagnostic(DiagnosticLevel.Warning, "Warning message", "WARN001");
                s.Profiler.AddDiagnostic(DiagnosticLevel.Error, "Error message", "ERR001");
                return s;
            })
            .Then("all diagnostics are captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.Diagnostics.Count == 3 &&
                       output.Diagnostics.Any(d => d.Level == DiagnosticLevel.Info) &&
                       output.Diagnostics.Any(d => d.Level == DiagnosticLevel.Warning) &&
                       output.Diagnostics.Any(d => d.Level == DiagnosticLevel.Error);
            })
            .AssertPassed();
    }

    [Scenario("Disabled profiler methods are safe to call")]
    [Fact]
    public async Task Disabled_profiler_methods_are_safe()
    {
        await Given("a disabled profiler", () => Setup(enabled: false))
            .When("various methods are called", s =>
            {
                s.Profiler.SetConfiguration(new BuildConfiguration { Provider = "test" });
                s.Profiler.AddMetadata("key", "value");
                s.Profiler.AddArtifact(new ArtifactInfo { Path = "/test" });
                s.Profiler.AddDiagnostic(DiagnosticLevel.Info, "message");
                using (var task = s.Profiler.BeginTask("TestTask"))
                {
                    task.SetOutputs(new Dictionary<string, object?> { ["out"] = "value" });
                }
                return s;
            })
            .Then("no data is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.BuildGraph.TotalTasks == 0 &&
                       output.Metadata.Count == 0 &&
                       output.Artifacts.Count == 0 &&
                       output.Diagnostics.Count == 0;
            })
            .AssertPassed();
    }

    [Scenario("Task with inputs but no outputs is tracked")]
    [Fact]
    public async Task Task_with_inputs_no_outputs_is_tracked()
    {
        await Given("an enabled profiler", () => Setup())
            .When("a task with only inputs is executed", s =>
            {
                var inputs = new Dictionary<string, object?> { ["input"] = "value" };
                using (s.Profiler.BeginTask("TestTask", "TestInitiator", inputs)) { }
                return s;
            })
            .Then("task has inputs", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Inputs.Count == 1 && task.Inputs["input"]?.ToString() == "value";
            })
            .And("task has empty outputs", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Outputs.Count == 0;
            })
            .AssertPassed();
    }

    [Scenario("Deeply nested tasks are tracked correctly")]
    [Fact]
    public async Task Deeply_nested_tasks_are_tracked()
    {
        await Given("an enabled profiler", () => Setup())
            .When("deeply nested tasks are executed", s =>
            {
                using (s.Profiler.BeginTask("Level1"))
                {
                    using (s.Profiler.BeginTask("Level2"))
                    {
                        using (s.Profiler.BeginTask("Level3"))
                        {
                            // Innermost task
                        }
                    }
                }
                return s;
            })
            .Then("all three levels are captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.BuildGraph.TotalTasks == 3;
            })
            .And("hierarchy is correct", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var level1 = output.BuildGraph.Nodes.First();
                var level2 = level1.Children.First();
                var level3 = level2.Children.First();
                return level1.Task.Name == "Level1" &&
                       level2.Task.Name == "Level2" &&
                       level3.Task.Name == "Level3";
            })
            .AssertPassed();
    }

    [Scenario("Complete writes file to disk")]
    [Fact]
    public async Task Complete_writes_file_to_disk()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.json");
        
        try
        {
            await Given("an enabled profiler with data", () =>
                {
                    var state = Setup();
                    using (state.Profiler.BeginTask("TestTask")) { }
                    return (state.Profiler, outputPath);
                })
                .When("Complete is called", t =>
                {
                    t.Profiler.Complete(outputPath);
                    return t;
                })
                .Then("file exists", _ => File.Exists(outputPath))
                .And("file contains valid JSON", _ =>
                {
                    var content = File.ReadAllText(outputPath);
                    return content.Contains("\"schemaVersion\"") && content.Contains("\"buildGraph\"");
                })
                .AssertPassed();
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Scenario("Complete creates output directory if needed")]
    [Fact]
    public async Task Complete_creates_output_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-dir-{Guid.NewGuid()}");
        var outputPath = Path.Combine(tempDir, "profile.json");
        
        try
        {
            await Given("an enabled profiler and non-existent directory", () =>
                {
                    var state = Setup();
                    using (state.Profiler.BeginTask("TestTask")) { }
                    return (state.Profiler, outputPath);
                })
                .When("Complete is called", t =>
                {
                    t.Profiler.Complete(outputPath);
                    return t;
                })
                .Then("directory is created", _ => Directory.Exists(tempDir))
                .And("file exists", _ => File.Exists(outputPath))
                .AssertPassed();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Scenario("GetRunOutput returns consistent data")]
    [Fact]
    public async Task GetRunOutput_returns_consistent_data()
    {
        await Given("an enabled profiler", () => Setup())
            .When("GetRunOutput is called multiple times", s =>
            {
                var output1 = s.Profiler.GetRunOutput();
                var output2 = s.Profiler.GetRunOutput();
                return (s, output1, output2);
            })
            .Then("same instance is returned", t =>
                !ReferenceEquals(t.output1, null) && ReferenceEquals(t.output1, t.output2))
            .AssertPassed();
    }

    [Scenario("Task with null initiator is handled")]
    [Fact]
    public async Task Task_with_null_initiator_is_handled()
    {
        await Given("an enabled profiler", () => Setup())
            .When("a task with null initiator is executed", s =>
            {
                using (s.Profiler.BeginTask("TestTask", initiator: null)) { }
                return s;
            })
            .Then("task is captured", s =>
            {
                var output = s.Profiler.GetRunOutput();
                return output.BuildGraph.TotalTasks == 1;
            })
            .And("initiator is null", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Initiator == null;
            })
            .AssertPassed();
    }

    [Scenario("Task with null inputs is handled")]
    [Fact]
    public async Task Task_with_null_inputs_is_handled()
    {
        await Given("an enabled profiler", () => Setup())
            .When("a task with null inputs is executed", s =>
            {
                using (s.Profiler.BeginTask("TestTask", inputs: null)) { }
                return s;
            })
            .Then("task has empty inputs dictionary", s =>
            {
                var output = s.Profiler.GetRunOutput();
                var task = output.BuildGraph.Nodes.First().Task;
                return task.Inputs != null && task.Inputs.Count == 0;
            })
            .AssertPassed();
    }
}
