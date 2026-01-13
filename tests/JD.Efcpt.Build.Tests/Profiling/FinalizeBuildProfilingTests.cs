using JD.Efcpt.Build.Tests.Infrastructure;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.Profiling;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the FinalizeBuildProfiling task that finalizes build profiling.
/// </summary>
[Feature("FinalizeBuildProfiling: Build profiling finalization")]
[Collection(nameof(AssemblySetup))]
public sealed class FinalizeBuildProfilingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestBuildEngine Engine,
        FinalizeBuildProfiling Task,
        string ProjectPath,
        string OutputPath);

    private static SetupState Setup()
    {
        BuildProfilerManager.Clear();
        var engine = new TestBuildEngine();
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";
        var outputPath = Path.Combine(Path.GetTempPath(), $"test-profile-{Guid.NewGuid()}.json");
        var task = new FinalizeBuildProfiling
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            OutputPath = outputPath
        };
        return new SetupState(engine, task, projectPath, outputPath);
    }

    [Scenario("Task returns true when no profiler exists")]
    [Fact]
    public async Task Task_returns_true_when_no_profiler()
    {
        var result = false;

        await Given("a task with no profiler initialized", Setup)
            .When("task is executed", s =>
            {
                result = s.Task.Execute();
                return s;
            })
            .Then("result is true", _ => result)
            .AssertPassed();
    }

    [Scenario("Task returns true when profiler is disabled")]
    [Fact]
    public async Task Task_returns_true_when_profiler_disabled()
    {
        var result = false;

        await Given("a task with disabled profiler", () =>
            {
                var state = Setup();
                // Create a disabled profiler
                BuildProfilerManager.GetOrCreate(state.ProjectPath, false, "TestProject");
                return state;
            })
            .When("task is executed", s =>
            {
                result = s.Task.Execute();
                return s;
            })
            .Then("result is true", _ => result)
            .And("no output file is created", s => !File.Exists(s.OutputPath))
            .AssertPassed();
    }

    [Scenario("Profile is written when profiler is enabled")]
    [Fact]
    public async Task Profile_written_when_profiler_enabled()
    {
        try
        {
            await Given("a task with enabled profiler", () =>
                {
                    var state = Setup();
                    // Create an enabled profiler with some tasks
                    var profiler = BuildProfilerManager.GetOrCreate(state.ProjectPath, true, "TestProject");
                    using var task = profiler.BeginTask("TestTask");
                    return state;
                })
                .When("task is executed", s =>
                {
                    s.Task.Execute();
                    return s;
                })
                .Then("output file is created", s => File.Exists(s.OutputPath))
                .And("high importance message is logged", s =>
                    s.Engine.Messages.Any(m =>
                        m.Message != null && m.Message.Contains("Build profile written to") &&
                        m.Importance == Microsoft.Build.Framework.MessageImportance.High))
                .AssertPassed();
        }
        finally
        {
            // Cleanup
            var state = Setup();
            if (File.Exists(state.OutputPath))
                File.Delete(state.OutputPath);
        }
    }

    [Scenario("Task handles exceptions gracefully")]
    [Fact]
    public async Task Task_handles_exceptions_gracefully()
    {
        var result = false;

        await Given("a task with invalid output path", () =>
            {
                var state = Setup();
                // Create an enabled profiler
                BuildProfilerManager.GetOrCreate(state.ProjectPath, true, "TestProject");
                // Set an invalid output path with illegal characters
                state.Task.OutputPath = "C:\\invalid<>path\\profile.json";
                return state;
            })
            .When("task is executed", s =>
            {
                result = s.Task.Execute();
                return s;
            })
            .Then("result is still true", _ => result)
            .And("warning is logged", s =>
                s.Engine.Warnings.Any(w => w.Message != null && w.Message.Contains("Failed to write build profile")))
            .AssertPassed();
    }

    [Scenario("BuildSucceeded property is accepted")]
    [Fact]
    public async Task BuildSucceeded_property_is_accepted()
    {
        var result = false;

        await Given("a task with BuildSucceeded set", () =>
            {
                var state = Setup();
                state.Task.BuildSucceeded = false;
                return state;
            })
            .When("task is executed", s =>
            {
                result = s.Task.Execute();
                return s;
            })
            .Then("result is true", _ => result)
            .AssertPassed();
    }

    [Scenario("Profiler is removed after completion")]
    [Fact]
    public async Task Profiler_removed_after_completion()
    {
        try
        {
            await Given("a task with enabled profiler", () =>
                {
                    var state = Setup();
                    BuildProfilerManager.GetOrCreate(state.ProjectPath, true, "TestProject");
                    return state;
                })
                .When("task is executed", s =>
                {
                    s.Task.Execute();
                    return s;
                })
                .Then("profiler is removed from manager", s =>
                    BuildProfilerManager.TryGet(s.ProjectPath) == null)
                .AssertPassed();
        }
        finally
        {
            var state = Setup();
            if (File.Exists(state.OutputPath))
                File.Delete(state.OutputPath);
        }
    }
}
