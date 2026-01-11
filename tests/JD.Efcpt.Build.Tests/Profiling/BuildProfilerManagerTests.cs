using JD.Efcpt.Build.Tasks.Profiling;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the BuildProfilerManager class that manages profiler instances across tasks.
/// </summary>
[Feature("BuildProfilerManager: Cross-task profiler coordination")]
[Collection(nameof(AssemblySetup))]
public sealed class BuildProfilerManagerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Clear any existing profilers before each test in the Setup method
    private sealed record SetupState(string ProjectPath)
    {
        public SetupState() : this($"/test/project-{Guid.NewGuid()}.csproj")
        {
            BuildProfilerManager.Clear();
        }
    }

    private static SetupState Setup() => new();

    [Scenario("GetOrCreate returns new profiler for new project")]
    [Fact]
    public async Task GetOrCreate_returns_new_profiler()
    {
        BuildProfiler? profiler = null;

        await Given("a new project path", Setup)
            .When("GetOrCreate is called", s =>
            {
                profiler = BuildProfilerManager.GetOrCreate(
                    s.ProjectPath,
                    enabled: true,
                    "TestProject");
                return s;
            })
            .Then("profiler is created", _ => profiler != null)
            .And("profiler is enabled", _ => profiler!.Enabled)
            .AssertPassed();
    }

    [Scenario("GetOrCreate returns same profiler for same project")]
    [Fact]
    public async Task GetOrCreate_returns_same_profiler()
    {
        BuildProfiler? profiler1 = null;
        BuildProfiler? profiler2 = null;

        await Given("a project path", Setup)
            .When("GetOrCreate is called twice", s =>
            {
                profiler1 = BuildProfilerManager.GetOrCreate(
                    s.ProjectPath,
                    enabled: true,
                    "TestProject");
                
                profiler2 = BuildProfilerManager.GetOrCreate(
                    s.ProjectPath,
                    enabled: true,
                    "TestProject");
                return s;
            })
            .Then("same profiler instance is returned", _ =>
                profiler1 != null && profiler2 != null &&
                ReferenceEquals(profiler1, profiler2))
            .AssertPassed();
    }

    [Scenario("TryGet returns null for non-existent project")]
    [Fact]
    public async Task TryGet_returns_null_for_nonexistent()
    {
        BuildProfiler? profiler = null;

        await Given("a non-existent project path", () => "/nonexistent/project.csproj")
            .When("TryGet is called", path =>
            {
                profiler = BuildProfilerManager.TryGet(path);
                return path;
            })
            .Then("null is returned", _ => profiler == null)
            .AssertPassed();
    }

    [Scenario("TryGet returns profiler after GetOrCreate")]
    [Fact]
    public async Task TryGet_returns_profiler_after_create()
    {
        BuildProfiler? createdProfiler = null;
        BuildProfiler? retrievedProfiler = null;

        await Given("a project with profiler", Setup)
            .When("profiler is created", s =>
            {
                createdProfiler = BuildProfilerManager.GetOrCreate(
                    s.ProjectPath,
                    enabled: true,
                    "TestProject");
                return s;
            })
            .And("profiler is retrieved", s =>
            {
                retrievedProfiler = BuildProfilerManager.TryGet(s.ProjectPath);
                return s;
            })
            .Then("same profiler is returned", _ =>
                createdProfiler != null && retrievedProfiler != null &&
                ReferenceEquals(createdProfiler, retrievedProfiler))
            .AssertPassed();
    }

    [Scenario("Complete removes profiler and writes output")]
    [Fact]
    public async Task Complete_removes_profiler()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"test-profile-{Guid.NewGuid()}.json");
        BuildProfiler? profilerAfterComplete = null;

        try
        {
            await Given("a project with profiler", Setup)
                .When("profiler is created and completed", s =>
                {
                    BuildProfilerManager.GetOrCreate(
                        s.ProjectPath,
                        enabled: true,
                        "TestProject");
                    
                    BuildProfilerManager.Complete(s.ProjectPath, outputPath);
                    return s;
                })
                .And("profiler is retrieved after complete", s =>
                {
                    profilerAfterComplete = BuildProfilerManager.TryGet(s.ProjectPath);
                    return s;
                })
                .Then("profiler is removed", _ => profilerAfterComplete == null)
                .And("output file is created", _ => File.Exists(outputPath))
                .AssertPassed();
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Scenario("Multiple projects can have separate profilers")]
    [Fact]
    public async Task Multiple_projects_have_separate_profilers()
    {
        var project1 = $"/test/project1-{Guid.NewGuid()}.csproj";
        var project2 = $"/test/project2-{Guid.NewGuid()}.csproj";
        BuildProfiler? profiler1 = null;
        BuildProfiler? profiler2 = null;

        await Given("two project paths", () => (project1, project2))
            .When("profilers are created for both", p =>
            {
                profiler1 = BuildProfilerManager.GetOrCreate(p.project1, true, "Project1");
                profiler2 = BuildProfilerManager.GetOrCreate(p.project2, true, "Project2");
                return p;
            })
            .Then("different profiler instances are returned", _ =>
                profiler1 != null && profiler2 != null &&
                !ReferenceEquals(profiler1, profiler2))
            .AssertPassed();
    }
}
