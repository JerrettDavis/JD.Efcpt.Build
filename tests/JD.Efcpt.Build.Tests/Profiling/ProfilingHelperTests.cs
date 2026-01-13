using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.Profiling;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the ProfilingHelper class.
/// </summary>
[Feature("ProfilingHelper: Helper methods for profiling")]
[Collection(nameof(AssemblySetup))]
public sealed class ProfilingHelperTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("GetProfiler returns null for null project path")]
    [Fact]
    public async Task GetProfiler_returns_null_for_null_path()
    {
        BuildProfiler? profiler = null;

        await Given("a null project path", () => (string?)null)
            .When("GetProfiler is called", path =>
            {
                profiler = ProfilingHelper.GetProfiler(path!);
                return path;
            })
            .Then("null is returned", _ => profiler == null)
            .AssertPassed();
    }

    [Scenario("GetProfiler returns null for empty project path")]
    [Fact]
    public async Task GetProfiler_returns_null_for_empty_path()
    {
        BuildProfiler? profiler = null;

        await Given("an empty project path", () => string.Empty)
            .When("GetProfiler is called", path =>
            {
                profiler = ProfilingHelper.GetProfiler(path);
                return path;
            })
            .Then("null is returned", _ => profiler == null)
            .AssertPassed();
    }

    [Scenario("GetProfiler returns null for whitespace project path")]
    [Fact]
    public async Task GetProfiler_returns_null_for_whitespace_path()
    {
        BuildProfiler? profiler = null;

        await Given("a whitespace project path", () => "   ")
            .When("GetProfiler is called", path =>
            {
                profiler = ProfilingHelper.GetProfiler(path);
                return path;
            })
            .Then("null is returned", _ => profiler == null)
            .AssertPassed();
    }

    [Scenario("GetProfiler returns null when profiler not registered")]
    [Fact]
    public async Task GetProfiler_returns_null_when_not_registered()
    {
        BuildProfiler? profiler = null;

        await Given("a project path with no profiler", () =>
            {
                BuildProfilerManager.Clear();
                return "/test/project.csproj";
            })
            .When("GetProfiler is called", path =>
            {
                profiler = ProfilingHelper.GetProfiler(path);
                return path;
            })
            .Then("null is returned", _ => profiler == null)
            .AssertPassed();
    }

    [Scenario("GetProfiler returns profiler when registered")]
    [Fact]
    public async Task GetProfiler_returns_profiler_when_registered()
    {
        BuildProfiler? profiler = null;
        var projectPath = $"/test/project-{Guid.NewGuid()}.csproj";

        await Given("a project path with registered profiler", () =>
            {
                BuildProfilerManager.Clear();
                BuildProfilerManager.GetOrCreate(projectPath, true, "TestProject");
                return projectPath;
            })
            .When("GetProfiler is called", path =>
            {
                profiler = ProfilingHelper.GetProfiler(path);
                return path;
            })
            .Then("profiler is returned", _ => profiler != null)
            .And("profiler is the correct instance", _ =>
            {
                var expected = BuildProfilerManager.TryGet(projectPath);
                return ReferenceEquals(profiler, expected);
            })
            .AssertPassed();
    }
}
