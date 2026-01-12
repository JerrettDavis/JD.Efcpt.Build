using JD.Efcpt.Build.Tests.Infrastructure;
using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.Profiling;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the InitializeBuildProfiling task that initializes build profiling.
/// </summary>
[Feature("InitializeBuildProfiling: Build profiling initialization")]
[Collection(nameof(AssemblySetup))]
public sealed class InitializeBuildProfilingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestBuildEngine Engine,
        InitializeBuildProfiling Task,
        string ProjectPath);

    private static SetupState Setup()
    {
        BuildProfilerManager.Clear();
        var engine = new TestBuildEngine();
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";
        var task = new InitializeBuildProfiling
        {
            BuildEngine = engine,
            ProjectPath = projectPath,
            ProjectName = "TestProject"
        };
        return new SetupState(engine, task, projectPath);
    }

    [Scenario("Profiling is disabled when EnableProfiling is false")]
    [Fact]
    public async Task Profiling_disabled_when_EnableProfiling_false()
    {
        await Given("a task with profiling disabled", () =>
            {
                var state = Setup();
                state.Task.EnableProfiling = "false";
                return state;
            })
            .When("task is executed", s =>
            {
                s.Task.Execute();
                return s;
            })
            .Then("profiler is created but disabled", s =>
            {
                var profiler = BuildProfilerManager.TryGet(s.ProjectPath);
                return profiler != null && !profiler.Enabled;
            })
            .AssertPassed();
    }

    [Scenario("Profiling is enabled when EnableProfiling is true")]
    [Fact]
    public async Task Profiling_enabled_when_EnableProfiling_true()
    {
        await Given("a task with profiling enabled", () =>
            {
                var state = Setup();
                state.Task.EnableProfiling = "true";
                state.Task.TargetFramework = "net8.0";
                state.Task.Configuration = "Debug";
                return state;
            })
            .When("task is executed", s =>
            {
                s.Task.Execute();
                return s;
            })
            .Then("profiler is created and enabled", s =>
            {
                var profiler = BuildProfilerManager.TryGet(s.ProjectPath);
                return profiler != null && profiler.Enabled;
            })
            .AssertPassed();
    }

    [Scenario("Configuration is set when profiling is enabled")]
    [Fact]
    public async Task Configuration_set_when_profiling_enabled()
    {
        await Given("a task with profiling enabled and configuration", () =>
            {
                var state = Setup();
                state.Task.EnableProfiling = "true";
                state.Task.ConfigPath = "/test/config.json";
                state.Task.DacpacPath = "/test/database.dacpac";
                state.Task.Provider = "mssql";
                return state;
            })
            .When("task is executed", s =>
            {
                s.Task.Execute();
                return s;
            })
            .Then("profiler configuration is set", s =>
            {
                var profiler = BuildProfilerManager.TryGet(s.ProjectPath);
                var output = profiler?.GetRunOutput();
                return output?.Configuration.ConfigPath == "/test/config.json" &&
                       output?.Configuration.DacpacPath == "/test/database.dacpac" &&
                       output?.Configuration.Provider == "mssql";
            })
            .AssertPassed();
    }

    [Scenario("EnableProfiling is case-insensitive")]
    [Theory]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("true")]
    public async Task EnableProfiling_is_case_insensitive(string value)
    {
        await Given("a task with various EnableProfiling values", () =>
            {
                var state = Setup();
                state.Task.EnableProfiling = value;
                return state;
            })
            .When("task is executed", s =>
            {
                s.Task.Execute();
                return s;
            })
            .Then("profiler is enabled", s =>
            {
                var profiler = BuildProfilerManager.TryGet(s.ProjectPath);
                return profiler != null && profiler.Enabled;
            })
            .AssertPassed();
    }

    [Scenario("Task returns true on success")]
    [Fact]
    public async Task Task_returns_true_on_success()
    {
        var result = false;

        await Given("a task configured correctly", Setup)
            .When("task is executed", s =>
            {
                result = s.Task.Execute();
                return s;
            })
            .Then("result is true", _ => result)
            .AssertPassed();
    }

    [Scenario("Log message is written when profiling is enabled")]
    [Fact]
    public async Task Log_message_written_when_profiling_enabled()
    {
        await Given("a task with profiling enabled", () =>
            {
                var state = Setup();
                state.Task.EnableProfiling = "true";
                return state;
            })
            .When("task is executed", s =>
            {
                s.Task.Execute();
                return s;
            })
            .Then("high importance message is logged", s =>
                s.Engine.Messages.Any(m => 
                    m.Message != null && m.Message.Contains("Build profiling enabled") &&
                    m.Importance == Microsoft.Build.Framework.MessageImportance.High))
            .AssertPassed();
    }
}
