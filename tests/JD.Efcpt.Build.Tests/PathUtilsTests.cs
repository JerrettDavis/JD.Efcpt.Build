using JD.Efcpt.Build.Tasks;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the PathUtils utility class.
/// </summary>
[Feature("PathUtils: path resolution and validation utilities")]
[Collection(nameof(AssemblySetup))]
public sealed class PathUtilsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region FullPath Tests

    [Scenario("FullPath returns rooted path unchanged")]
    [Fact]
    public async Task FullPath_rooted_path_unchanged()
    {
        var rootedPath = OperatingSystem.IsWindows()
            ? @"C:\absolute\path\file.txt"
            : "/absolute/path/file.txt";

        await Given("a rooted path and a base directory", () => (rootedPath, "/some/base"))
            .When("FullPath is called", t => PathUtils.FullPath(t.rootedPath, t.Item2))
            .Then("result equals the rooted path", r =>
                Path.GetFullPath(r) == Path.GetFullPath(rootedPath))
            .AssertPassed();
    }

    [Scenario("FullPath combines relative path with base directory")]
    [Fact]
    public async Task FullPath_relative_path_combined()
    {
        await Given("a relative path and base directory", () =>
            {
                var baseDir = Path.GetTempPath();
                return ("relative/file.txt", baseDir);
            })
            .When("FullPath is called", t => PathUtils.FullPath(t.Item1, t.baseDir))
            .Then("result is combined path", r =>
            {
                var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "relative/file.txt"));
                return Path.GetFullPath(r) == expected;
            })
            .AssertPassed();
    }

    [Scenario("FullPath returns empty/whitespace path unchanged")]
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task FullPath_empty_returns_unchanged(string? path)
    {
        await Given("empty or whitespace path", () => (path!, "/base"))
            .When("FullPath is called", t => PathUtils.FullPath(t.Item1, t.Item2))
            .Then("result equals input", r => r == path)
            .AssertPassed();
    }

    [Scenario("FullPath handles parent directory references")]
    [Fact]
    public async Task FullPath_handles_parent_references()
    {
        await Given("a path with parent directory reference", () =>
            {
                var baseDir = Path.Combine(Path.GetTempPath(), "sub", "folder");
                return ("../sibling/file.txt", baseDir);
            })
            .When("FullPath is called", t => PathUtils.FullPath(t.Item1, t.baseDir))
            .Then("result resolves parent correctly", r =>
            {
                var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "sub", "sibling", "file.txt"));
                return Path.GetFullPath(r) == expected;
            })
            .AssertPassed();
    }

    #endregion

    #region HasValue Tests

    [Scenario("HasValue returns true for non-empty string")]
    [Fact]
    public async Task HasValue_non_empty()
    {
        await Given("a non-empty string", () => "value")
            .When("HasValue is called", PathUtils.HasValue)
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("HasValue returns false for null")]
    [Fact]
    public async Task HasValue_null()
    {
        await Given("a null string", string? () => null)
            .When("HasValue is called", PathUtils.HasValue)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasValue returns false for empty string")]
    [Fact]
    public async Task HasValue_empty()
    {
        await Given("an empty string", () => "")
            .When("HasValue is called", PathUtils.HasValue)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasValue returns false for whitespace")]
    [Fact]
    public async Task HasValue_whitespace()
    {
        await Given("a whitespace string", () => "   ")
            .When("HasValue is called", PathUtils.HasValue)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    #endregion

    #region HasExplicitPath Tests

    [Scenario("HasExplicitPath returns true for rooted path")]
    [Fact]
    public async Task HasExplicitPath_rooted()
    {
        var path = OperatingSystem.IsWindows() ? @"C:\path\to\file.txt" : "/path/to/file.txt";

        await Given("a rooted path", () => path)
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns true for path with directory separator")]
    [Fact]
    public async Task HasExplicitPath_with_separator()
    {
        await Given("a relative path with separator", () => $"folder{Path.DirectorySeparatorChar}file.txt")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns true for path with alt directory separator")]
    [Fact]
    public async Task HasExplicitPath_with_alt_separator()
    {
        await Given("a relative path with alt separator", () => $"folder{Path.AltDirectorySeparatorChar}file.txt")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns false for simple filename")]
    [Fact]
    public async Task HasExplicitPath_simple_filename()
    {
        await Given("a simple filename", () => "file.txt")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns false for null")]
    [Fact]
    public async Task HasExplicitPath_null()
    {
        await Given("a null string", () => (string?)null)
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns false for empty string")]
    [Fact]
    public async Task HasExplicitPath_empty()
    {
        await Given("an empty string", () => "")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns false for whitespace")]
    [Fact]
    public async Task HasExplicitPath_whitespace()
    {
        await Given("a whitespace string", () => "   ")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns true for parent path reference")]
    [Fact]
    public async Task HasExplicitPath_parent_reference()
    {
        await Given("a parent path reference", () => "../file.txt")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is true (contains separator)", r => r)
            .AssertPassed();
    }

    [Scenario("HasExplicitPath returns true for current directory reference")]
    [Fact]
    public async Task HasExplicitPath_current_directory_reference()
    {
        await Given("a current directory reference", () => "./file.txt")
            .When("HasExplicitPath is called", PathUtils.HasExplicitPath)
            .Then("result is true (contains separator)", r => r)
            .AssertPassed();
    }

    #endregion
}
