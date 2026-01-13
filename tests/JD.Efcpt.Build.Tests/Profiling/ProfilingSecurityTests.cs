using JD.Efcpt.Build.Tasks.Profiling;
using JD.Efcpt.Build.Tasks.Decorators;
using Microsoft.Build.Framework;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for security and sensitive data handling in profiling.
/// </summary>
[Feature("Profiling Security: Sensitive data exclusion")]
[Collection(nameof(AssemblySetup))]
public sealed class ProfilingSecurityTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Test task with sensitive data
    private sealed class TestTaskWithSensitiveData : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string PublicInput { get; set; } = "";

        [Required]
        [ProfileInput(Exclude = true)]
        public string Password { get; set; } = "";

        [Output]
        public string PublicOutput { get; set; } = "";

        [Output]
        [ProfileOutput(Exclude = true)]
        public string SecretToken { get; set; } = "";

        public override bool Execute()
        {
            PublicOutput = "public result";
            SecretToken = "secret-token-12345";
            return true;
        }
    }

    [Scenario("Sensitive inputs are excluded from profiling")]
    [Fact]
    public async Task Sensitive_inputs_excluded_from_profiling()
    {
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";
        BuildProfiler? profiler = null;

        try
        {
            await Given("a profiler and task with sensitive data", () =>
                {
                    BuildProfilerManager.Clear();
                    profiler = BuildProfilerManager.GetOrCreate(projectPath, true, "TestProject");
                    
                    var task = new TestTaskWithSensitiveData
                    {
                        PublicInput = "public value",
                        Password = "super-secret-password"
                    };
                    
                    return (profiler, task);
                })
                .When("task is executed with profiling", t =>
                {
                    var ctx = new TaskExecutionContext(null!, "TestTask", t.profiler);
                    ProfilingBehavior.ExecuteWithProfiling(t.task, _ =>
                    {
                        return t.task.Execute();
                    }, ctx);
                    return t;
                })
                .Then("public input is captured", t =>
                {
                    var output = t.profiler.GetRunOutput();
                    var taskExec = output.BuildGraph.Nodes.First().Task;
                    return taskExec.Inputs.ContainsKey("PublicInput") &&
                           taskExec.Inputs["PublicInput"]?.ToString() == "public value";
                })
                .And("sensitive input is NOT captured", t =>
                {
                    var output = t.profiler.GetRunOutput();
                    var taskExec = output.BuildGraph.Nodes.First().Task;
                    return !taskExec.Inputs.ContainsKey("Password");
                })
                .AssertPassed();
        }
        finally
        {
            BuildProfilerManager.Clear();
        }
    }

    [Scenario("Sensitive outputs are excluded from profiling")]
    [Fact]
    public async Task Sensitive_outputs_excluded_from_profiling()
    {
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";
        BuildProfiler? profiler = null;

        try
        {
            await Given("a profiler and task with sensitive outputs", () =>
                {
                    BuildProfilerManager.Clear();
                    profiler = BuildProfilerManager.GetOrCreate(projectPath, true, "TestProject");
                    
                    var task = new TestTaskWithSensitiveData
                    {
                        PublicInput = "public value"
                    };
                    
                    return (profiler, task);
                })
                .When("task is executed with profiling", t =>
                {
                    var ctx = new TaskExecutionContext(null!, "TestTask", t.profiler);
                    ProfilingBehavior.ExecuteWithProfiling(t.task, _ =>
                    {
                        return t.task.Execute();
                    }, ctx);
                    return t;
                })
                .Then("public output is captured", t =>
                {
                    var output = t.profiler.GetRunOutput();
                    var taskExec = output.BuildGraph.Nodes.First().Task;
                    return taskExec.Outputs.ContainsKey("PublicOutput") &&
                           taskExec.Outputs["PublicOutput"]?.ToString() == "public result";
                })
                .And("sensitive output is NOT captured", t =>
                {
                    var output = t.profiler.GetRunOutput();
                    var taskExec = output.BuildGraph.Nodes.First().Task;
                    return !taskExec.Outputs.ContainsKey("SecretToken");
                })
                .AssertPassed();
        }
        finally
        {
            BuildProfilerManager.Clear();
        }
    }

    [Scenario("Connection string redaction is verified")]
    [Fact]
    public async Task Connection_string_is_redacted()
    {
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";
        BuildProfiler? profiler = null;

        try
        {
            await Given("a profiler", () =>
                {
                    BuildProfilerManager.Clear();
                    profiler = BuildProfilerManager.GetOrCreate(projectPath, true, "TestProject");
                    return profiler;
                })
                .When("a task with connection string pattern is tracked", p =>
                {
                    var inputs = new Dictionary<string, object?>
                    {
                        ["ConnectionString"] = "<redacted>",
                        ["Database"] = "MyDatabase"
                    };
                    
                    using (p.BeginTask("TestTask", inputs: inputs)) { }
                    return p;
                })
                .Then("connection string is redacted in output", p =>
                {
                    var output = p.GetRunOutput();
                    var taskExec = output.BuildGraph.Nodes.First().Task;
                    return taskExec.Inputs["ConnectionString"]?.ToString() == "<redacted>";
                })
                .And("other inputs are preserved", p =>
                {
                    var output = p.GetRunOutput();
                    var taskExec = output.BuildGraph.Nodes.First().Task;
                    return taskExec.Inputs["Database"]?.ToString() == "MyDatabase";
                })
                .AssertPassed();
        }
        finally
        {
            BuildProfilerManager.Clear();
        }
    }
}
