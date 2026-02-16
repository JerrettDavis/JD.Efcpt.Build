using JD.Efcpt.Build.Tasks.Extensions;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the EnumerableExtensions utility class.
/// </summary>
[Feature("EnumerableExtensions: collection manipulation utilities")]
[Collection(nameof(AssemblySetup))]
public sealed class EnumerableExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("BuildCandidateNames returns fallback names when no override")]
    [Fact]
    public async Task BuildCandidateNames_fallback_only()
    {
        var setup = new[] { "file1.json", "file2.json" };
        await Given("no override and two fallback names", () => ((string?)null, setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("result contains both fallbacks", r => r.Count == 2 && r[0] == "file1.json" && r[1] == "file2.json")
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames places override first")]
    [Fact]
    public async Task BuildCandidateNames_override_first()
    {
        var setup = new[] { "file1.json", "file2.json" };
        await Given("an override and fallback names", () => ("custom.json", setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("override is first", r => r[0] == "custom.json")
            .And("result contains all names", r => r.Count == 3)
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames extracts filename from path override")]
    [Fact]
    public async Task BuildCandidateNames_extracts_filename_from_path()
    {
        var setup = new[] { "default.json" };
        await Given("an override path and fallback", () => ("path/to/custom.json", setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("extracted filename is first", r => r[0] == "custom.json")
            .And("result contains default", r => r.Contains("default.json"))
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames deduplicates case-insensitively")]
    [Fact]
    public async Task BuildCandidateNames_deduplicates()
    {
        var setup = new[] { "file.json", "other.json" };
        await Given("override matching a fallback with different case", () => ("FILE.JSON", setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("result is deduplicated", r => r.Count == 2)
            .And("first is override version", r => r[0] == "FILE.JSON")
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames handles empty fallbacks")]
    [Fact]
    public async Task BuildCandidateNames_empty_fallbacks()
    {
        await Given("override only", () => ("custom.json", Array.Empty<string>()))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("result contains only override", r => r.Count == 1 && r[0] == "custom.json")
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames filters null and empty fallbacks")]
    [Fact]
    public async Task BuildCandidateNames_filters_invalid_fallbacks()
    {
        var setup = new[] { "valid.json", "", "  ", "also-valid.json" };
        await Given("fallbacks with nulls and empties", () => ((string?)null, setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("only valid names included", r => r.Count == 2)
            .And("contains valid.json", r => r.Contains("valid.json"))
            .And("contains also-valid.json", r => r.Contains("also-valid.json"))
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames handles whitespace-only override")]
    [Fact]
    public async Task BuildCandidateNames_whitespace_override()
    {
        var setup = new[] { "file.json" };
        await Given("whitespace override and fallbacks", () => ("   ", setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("override is ignored", r => r.Count == 1 && r[0] == "file.json")
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames preserves order of fallbacks")]
    [Fact]
    public async Task BuildCandidateNames_preserves_fallback_order()
    {
        var setup = new[] { "first.json", "second.json", "third.json" };
        await Given("multiple fallbacks", () => ((string?)null, setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("order is preserved", r =>
                r.Count == 3 && r[0] == "first.json" && r[1] == "second.json" && r[2] == "third.json")
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames handles Windows-style path in override")]
    [Fact]
    public async Task BuildCandidateNames_windows_path_override()
    {
        // Windows-style paths with backslashes are only correctly parsed on Windows.
        // On Linux/macOS, Path.GetFileName treats backslashes as literal characters.
        if (!OperatingSystem.IsWindows())
        {
            return; // Skip on non-Windows platforms
        }

        var setup = new[] { "default.json" };
        await Given("Windows-style path override", () => (@"C:\path\to\custom.json", setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("extracted filename is first", r => r[0] == "custom.json")
            .AssertPassed();
    }

    [Scenario("BuildCandidateNames handles Unix-style path in override")]
    [Fact]
    public async Task BuildCandidateNames_unix_path_override()
    {
        var setup = new[] { "default.json" };
        await Given("Unix-style path override", () => ("/path/to/custom.json", setup))
            .When("BuildCandidateNames is called", t => EnumerableExtensions.BuildCandidateNames(t.Item1, t.Item2))
            .Then("extracted filename is first", r => r[0] == "custom.json")
            .AssertPassed();
    }
}
