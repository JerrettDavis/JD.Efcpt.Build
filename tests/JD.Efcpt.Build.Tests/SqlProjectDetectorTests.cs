using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

[Feature("SqlProjectDetector: identifies supported SQL SDKs")]
[Collection(nameof(AssemblySetup))]
public sealed class SqlProjectDetectorTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(TestFolder Folder, string ProjectPath);
    private sealed record DetectionResult(SetupState Setup, bool IsSqlProject);

    private static SetupState SetupMissingProject()
    {
        var folder = new TestFolder();
        var path = Path.Combine(folder.Root, "Missing.csproj");
        return new SetupState(folder, path);
    }

    private static SetupState SetupProject(string contents)
    {
        var folder = new TestFolder();
        var path = folder.WriteFile("Db.csproj", contents);
        return new SetupState(folder, path);
    }

    private static DetectionResult ExecuteDetect(SetupState setup)
        => new(setup, SqlProjectDetector.IsSqlProjectReference(setup.ProjectPath));

    [Scenario("Missing project returns false")]
    [Fact]
    public async Task Missing_project_returns_false()
    {
        await Given("missing project path", SetupMissingProject)
            .When("detect", ExecuteDetect)
            .Then("returns false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Sdk attribute is detected")]
    [Fact]
    public async Task Sdk_attribute_is_detected()
    {
        await Given("project with supported SDK attribute", () => SetupProject("<Project Sdk=\"MSBuild.Sdk.SqlProj/3.0.0\" />"))
            .When("detect", ExecuteDetect)
            .Then("returns true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Multi Sdk attribute is detected")]
    [Fact]
    public async Task Multi_sdk_attribute_is_detected()
    {
        await Given("project with multiple SDKs", () => SetupProject("<Project Sdk=\"Microsoft.NET.Sdk;MSBuild.Sdk.SqlProj/3.0.0\" />"))
            .When("detect", ExecuteDetect)
            .Then("returns true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Sdk element is detected")]
    [Fact]
    public async Task Sdk_element_is_detected()
    {
        await Given("project with SDK element", () =>
                SetupProject("<Project><Sdk Name=\"Microsoft.Build.Sql\" Version=\"1.0.0\" /></Project>"))
            .When("detect", ExecuteDetect)
            .Then("returns true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Nested Project element is detected")]
    [Fact]
    public async Task Nested_project_element_is_detected()
    {
        await Given("project with nested Project element", () =>
                SetupProject("<Root><Project Sdk=\"MSBuild.Sdk.SqlProj/3.0.0\" /></Root>"))
            .When("detect", ExecuteDetect)
            .Then("returns true", r => r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Unknown SDK returns false")]
    [Fact]
    public async Task Unknown_sdk_returns_false()
    {
        await Given("project with unknown SDK", () => SetupProject("<Project Sdk=\"Microsoft.NET.Sdk\" />"))
            .When("detect", ExecuteDetect)
            .Then("returns false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Invalid XML returns false")]
    [Fact]
    public async Task Invalid_xml_returns_false()
    {
        await Given("project with invalid XML", () => SetupProject("<Project"))
            .When("detect", ExecuteDetect)
            .Then("returns false", r => !r.IsSqlProject)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
