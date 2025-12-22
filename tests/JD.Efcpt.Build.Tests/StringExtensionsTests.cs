using JD.Efcpt.Build.Tasks.Extensions;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the StringExtensions utility class.
/// </summary>
[Feature("StringExtensions: string comparison and parsing utilities")]
[Collection(nameof(AssemblySetup))]
public sealed class StringExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region EqualsIgnoreCase Tests

    [Scenario("EqualsIgnoreCase returns true for identical strings")]
    [Fact]
    public async Task EqualsIgnoreCase_identical_strings()
    {
        await Given("two identical strings", () => ("hello", "hello"))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("EqualsIgnoreCase returns true for same string with different case")]
    [Fact]
    public async Task EqualsIgnoreCase_different_case()
    {
        await Given("strings with different case", () => ("Hello", "hELLO"))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("EqualsIgnoreCase returns false for different strings")]
    [Fact]
    public async Task EqualsIgnoreCase_different_strings()
    {
        await Given("two different strings", () => ("hello", "world"))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("EqualsIgnoreCase handles null on left side")]
    [Fact]
    public async Task EqualsIgnoreCase_null_left()
    {
        await Given("null and a string", () => ((string?)null, "hello"))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("EqualsIgnoreCase handles null on right side")]
    [Fact]
    public async Task EqualsIgnoreCase_null_right()
    {
        await Given("a string and null", () => ("hello", (string?)null))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("EqualsIgnoreCase returns true for two nulls")]
    [Fact]
    public async Task EqualsIgnoreCase_both_null()
    {
        await Given("two nulls", () => ((string?)null, (string?)null))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("EqualsIgnoreCase handles empty strings")]
    [Fact]
    public async Task EqualsIgnoreCase_empty_strings()
    {
        await Given("two empty strings", () => ("", ""))
            .When("compared case-insensitively", t => t.Item1.EqualsIgnoreCase(t.Item2))
            .Then("result is true", r => r)
            .AssertPassed();
    }

    #endregion

    #region IsTrue Tests

    [Scenario("IsTrue returns true for 'true'")]
    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    [InlineData("TrUe")]
    public async Task IsTrue_true_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns true for 'yes'")]
    [Theory]
    [InlineData("yes")]
    [InlineData("YES")]
    [InlineData("Yes")]
    public async Task IsTrue_yes_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns true for 'on'")]
    [Theory]
    [InlineData("on")]
    [InlineData("ON")]
    [InlineData("On")]
    public async Task IsTrue_on_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns true for '1'")]
    [Fact]
    public async Task IsTrue_one()
    {
        await Given("the string '1'", () => "1")
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns true for 'enable'")]
    [Theory]
    [InlineData("enable")]
    [InlineData("ENABLE")]
    [InlineData("Enable")]
    public async Task IsTrue_enable_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns true for 'enabled'")]
    [Theory]
    [InlineData("enabled")]
    [InlineData("ENABLED")]
    [InlineData("Enabled")]
    public async Task IsTrue_enabled_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns true for 'y'")]
    [Theory]
    [InlineData("y")]
    [InlineData("Y")]
    public async Task IsTrue_y_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is true", r => r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for 'false'")]
    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("False")]
    public async Task IsTrue_false_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for 'no'")]
    [Theory]
    [InlineData("no")]
    [InlineData("NO")]
    [InlineData("No")]
    public async Task IsTrue_no_variations(string value)
    {
        await Given("the string", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for '0'")]
    [Fact]
    public async Task IsTrue_zero()
    {
        await Given("the string '0'", () => "0")
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for null")]
    [Fact]
    public async Task IsTrue_null()
    {
        await Given("a null string", () => (string?)null)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for empty string")]
    [Fact]
    public async Task IsTrue_empty()
    {
        await Given("an empty string", () => "")
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for whitespace")]
    [Fact]
    public async Task IsTrue_whitespace()
    {
        await Given("a whitespace string", () => "   ")
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    [Scenario("IsTrue returns false for arbitrary text")]
    [Theory]
    [InlineData("maybe")]
    [InlineData("sure")]
    [InlineData("2")]
    [InlineData("yep")]
    public async Task IsTrue_arbitrary_text(string value)
    {
        await Given("arbitrary text", () => value)
            .When("IsTrue is called", s => s.IsTrue())
            .Then("result is false", r => !r)
            .AssertPassed();
    }

    #endregion
}
