using JD.Efcpt.Build.Tasks;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the MessageLevelHelpers utility class.
/// </summary>
[Feature("MessageLevelHelpers: parse and validate message severity levels")]
[Collection(nameof(AssemblySetup))]
public sealed class MessageLevelHelpersTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Parse valid level names (case-insensitive)")]
    [Theory]
    [InlineData("None", MessageLevel.None)]
    [InlineData("none", MessageLevel.None)]
    [InlineData("NONE", MessageLevel.None)]
    [InlineData("Info", MessageLevel.Info)]
    [InlineData("info", MessageLevel.Info)]
    [InlineData("INFO", MessageLevel.Info)]
    [InlineData("Warn", MessageLevel.Warn)]
    [InlineData("warn", MessageLevel.Warn)]
    [InlineData("Warning", MessageLevel.Warn)]
    [InlineData("warning", MessageLevel.Warn)]
    [InlineData("Error", MessageLevel.Error)]
    [InlineData("error", MessageLevel.Error)]
    [InlineData("ERROR", MessageLevel.Error)]
    public async Task Parse_valid_level_names(string input, MessageLevel expected)
    {
        await Given($"input string '{input}'", () => input)
            .When("parsing with default fallback", s => MessageLevelHelpers.Parse(s, MessageLevel.Info))
            .Then("returns expected level", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Parse invalid level returns default")]
    [Theory]
    [InlineData("invalid", MessageLevel.Info)]
    [InlineData("unknown", MessageLevel.Warn)]
    [InlineData("", MessageLevel.Error)]
    [InlineData(null, MessageLevel.None)]
    [InlineData("   ", MessageLevel.Info)]
    public async Task Parse_invalid_level_returns_default(string? input, MessageLevel defaultValue)
    {
        await Given($"input string '{input ?? "null"}'", () => input)
            .When("parsing with default value", s => MessageLevelHelpers.Parse(s, defaultValue))
            .Then("returns default value", result => result == defaultValue)
            .AssertPassed();
    }

    [Scenario("TryParse valid level names")]
    [Theory]
    [InlineData("None", MessageLevel.None)]
    [InlineData("Info", MessageLevel.Info)]
    [InlineData("Warn", MessageLevel.Warn)]
    [InlineData("Warning", MessageLevel.Warn)]
    [InlineData("Error", MessageLevel.Error)]
    public async Task TryParse_valid_level_names(string input, MessageLevel expected)
    {
        await Given($"input string '{input}'", () => input)
            .When("trying to parse", s => MessageLevelHelpers.TryParse(s, out var result) ? (true, result) : (false, MessageLevel.None))
            .Then("succeeds", r => r.Item1)
            .And("returns expected level", r => r.Item2 == expected)
            .AssertPassed();
    }

    [Scenario("TryParse invalid level fails")]
    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task TryParse_invalid_level_fails(string? input)
    {
        await Given($"input string '{input ?? "null"}'", () => input)
            .When("trying to parse", s => MessageLevelHelpers.TryParse(s, out var result) ? (true, result) : (false, MessageLevel.None))
            .Then("fails", r => !r.Item1)
            .AssertPassed();
    }

    [Scenario("Parse handles whitespace")]
    [Theory]
    [InlineData("  None  ", MessageLevel.None)]
    [InlineData("\tInfo\t", MessageLevel.Info)]
    [InlineData(" Warn ", MessageLevel.Warn)]
    public async Task Parse_handles_whitespace(string input, MessageLevel expected)
    {
        await Given($"input with whitespace '{input}'", () => input)
            .When("parsing", s => MessageLevelHelpers.Parse(s, MessageLevel.Info))
            .Then("returns expected level", result => result == expected)
            .AssertPassed();
    }
}
