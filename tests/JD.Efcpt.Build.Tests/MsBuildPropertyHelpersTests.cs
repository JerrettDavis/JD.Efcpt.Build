using JD.Efcpt.Build.Tasks;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the MsBuildPropertyHelpers utility class.
/// </summary>
[Feature("MsBuildPropertyHelpers: MSBuild property value utilities")]
[Collection(nameof(AssemblySetup))]
public sealed class MsBuildPropertyHelpersTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region NullIfEmpty Tests

    [Scenario("NullIfEmpty returns null for empty string")]
    [Fact]
    public async Task NullIfEmpty_empty_string()
    {
        await Given("an empty string", () => string.Empty)
            .When("NullIfEmpty is called", MsBuildPropertyHelpers.NullIfEmpty)
            .Then("result is null", r => r is null)
            .AssertPassed();
    }

    [Scenario("NullIfEmpty returns null for whitespace")]
    [Fact]
    public async Task NullIfEmpty_whitespace()
    {
        await Given("a whitespace string", () => "   ")
            .When("NullIfEmpty is called", MsBuildPropertyHelpers.NullIfEmpty)
            .Then("result is null", r => r is null)
            .AssertPassed();
    }

    [Scenario("NullIfEmpty returns value for non-empty string")]
    [Fact]
    public async Task NullIfEmpty_non_empty()
    {
        await Given("a non-empty string", () => "test value")
            .When("NullIfEmpty is called", MsBuildPropertyHelpers.NullIfEmpty)
            .Then("result is the original value", r => r == "test value")
            .AssertPassed();
    }

    #endregion

    #region ParseBoolOrNull Tests

    [Scenario("ParseBoolOrNull returns null for empty string")]
    [Fact]
    public async Task ParseBoolOrNull_empty()
    {
        await Given("an empty string", () => string.Empty)
            .When("ParseBoolOrNull is called", MsBuildPropertyHelpers.ParseBoolOrNull)
            .Then("result is null", r => r is null)
            .AssertPassed();
    }

    [Scenario("ParseBoolOrNull returns true for 'true'")]
    [Fact]
    public async Task ParseBoolOrNull_true()
    {
        await Given("the string 'true'", () => "true")
            .When("ParseBoolOrNull is called", MsBuildPropertyHelpers.ParseBoolOrNull)
            .Then("result is true", r => r is true)
            .AssertPassed();
    }

    [Scenario("ParseBoolOrNull returns true for 'True'")]
    [Fact]
    public async Task ParseBoolOrNull_True()
    {
        await Given("the string 'True'", () => "True")
            .When("ParseBoolOrNull is called", MsBuildPropertyHelpers.ParseBoolOrNull)
            .Then("result is true", r => r is true)
            .AssertPassed();
    }

    [Scenario("ParseBoolOrNull returns false for 'false'")]
    [Fact]
    public async Task ParseBoolOrNull_false()
    {
        await Given("the string 'false'", () => "false")
            .When("ParseBoolOrNull is called", MsBuildPropertyHelpers.ParseBoolOrNull)
            .Then("result is false", r => r is false)
            .AssertPassed();
    }

    #endregion

    #region HasAnyValue Tests

    [Scenario("HasAnyValue (strings) returns false when all null")]
    [Fact]
    public async Task HasAnyValue_strings_all_null()
    {
        await Given("an array of nulls", () => new string?[] { null, null, null })
            .When("HasAnyValue is called", MsBuildPropertyHelpers.HasAnyValue)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasAnyValue (strings) returns true when one has value")]
    [Fact]
    public async Task HasAnyValue_strings_one_value()
    {
        await Given("an array with one value", () => new[] { null, "value", null })
            .When("HasAnyValue is called", MsBuildPropertyHelpers.HasAnyValue)
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("HasAnyValue (bools) returns false when all null")]
    [Fact]
    public async Task HasAnyValue_bools_all_null()
    {
        await Given("an array of nulls", () => new bool?[] { null, null, null })
            .When("HasAnyValue is called", MsBuildPropertyHelpers.HasAnyValue)
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("HasAnyValue (bools) returns true when one has value")]
    [Fact]
    public async Task HasAnyValue_bools_one_value()
    {
        await Given("an array with one value", () => new bool?[] { null, true, null })
            .When("HasAnyValue is called", MsBuildPropertyHelpers.HasAnyValue)
            .Then("result is true", r => r)
            .AssertPassed();
    }

    #endregion

    #region AddIfNotEmpty Tests

    [Scenario("AddIfNotEmpty adds value when not empty")]
    [Fact]
    public async Task AddIfNotEmpty_adds_value()
    {
        await Given("an empty dictionary", () => new Dictionary<string, string>())
            .When("AddIfNotEmpty is called with a value", dict =>
            {
                MsBuildPropertyHelpers.AddIfNotEmpty(dict, "key", "value");
                return dict;
            })
            .Then("dictionary contains the key", dict => dict.ContainsKey("key") && dict["key"] == "value")
            .AssertPassed();
    }

    [Scenario("AddIfNotEmpty does not add when empty")]
    [Fact]
    public async Task AddIfNotEmpty_skips_empty()
    {
        await Given("an empty dictionary", () => new Dictionary<string, string>())
            .When("AddIfNotEmpty is called with empty string", dict =>
            {
                MsBuildPropertyHelpers.AddIfNotEmpty(dict, "key", "");
                return dict;
            })
            .Then("dictionary is still empty", dict => dict.Count == 0)
            .AssertPassed();
    }

    [Scenario("AddIfNotEmpty does not add when whitespace")]
    [Fact]
    public async Task AddIfNotEmpty_skips_whitespace()
    {
        await Given("an empty dictionary", () => new Dictionary<string, string>())
            .When("AddIfNotEmpty is called with whitespace", dict =>
            {
                MsBuildPropertyHelpers.AddIfNotEmpty(dict, "key", "   ");
                return dict;
            })
            .Then("dictionary is still empty", dict => dict.Count == 0)
            .AssertPassed();
    }

    #endregion
}
