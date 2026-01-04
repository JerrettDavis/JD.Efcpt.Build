using JD.Efcpt.Build.Tasks.Utilities;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the DotNetToolUtilities class that handles .NET SDK and runtime detection.
/// </summary>
[Feature("DotNetToolUtilities: .NET SDK and runtime detection")]
[Collection(nameof(AssemblySetup))]
public sealed class DotNetToolUtilitiesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("IsDotNet10OrLater recognizes .NET 10+ frameworks")]
    [Theory]
    [InlineData("net10.0", true)]
    [InlineData("net10", true)]
    [InlineData("net11.0", true)]
    [InlineData("NET10.0", true)] // Case insensitive
    [InlineData("Net10.0", true)]
    public async Task IsDotNet10OrLater_recognizes_net10_and_later(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater recognizes older .NET frameworks")]
    [Theory]
    [InlineData("net9.0", false)]
    [InlineData("net8.0", false)]
    [InlineData("net7.0", false)]
    [InlineData("net6.0", false)]
    [InlineData("net5.0", false)]
    public async Task IsDotNet10OrLater_recognizes_older_net_frameworks(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles .NET Framework")]
    [Theory]
    [InlineData("net48", false)]
    [InlineData("net472", false)]
    [InlineData("net471", false)]
    [InlineData("net47", false)]
    [InlineData("net462", false)]
    [InlineData("net461", false)]
    [InlineData("net46", false)]
    public async Task IsDotNet10OrLater_handles_net_framework(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles .NET Standard")]
    [Theory]
    [InlineData("netstandard2.0", false)]
    [InlineData("netstandard2.1", false)]
    [InlineData("netstandard1.6", false)]
    public async Task IsDotNet10OrLater_handles_netstandard(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles .NET Core")]
    [Theory]
    [InlineData("netcoreapp3.1", false)]
    [InlineData("netcoreapp3.0", false)]
    [InlineData("netcoreapp2.1", false)]
    public async Task IsDotNet10OrLater_handles_netcoreapp(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles invalid input")]
    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("invalid", false)]
    [InlineData("netX.Y", false)]
    public async Task IsDotNet10OrLater_handles_invalid_input(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles null input")]
    [Fact]
    public async Task IsDotNet10OrLater_handles_null_input()
    {
        await Given("null target framework", () => (string?)null)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t!))
            .Then("returns false", result => result == false)
            .AssertPassed();
    }

    [Scenario("ParseTargetFrameworkVersion parses .NET 5+ versions")]
    [Theory]
    [InlineData("net10.0", 10)]
    [InlineData("net10", 10)]
    [InlineData("net9.0", 9)]
    [InlineData("net8.0", 8)]
    [InlineData("net7.0", 7)]
    [InlineData("net6.0", 6)]
    [InlineData("net5.0", 5)]
    [InlineData("NET10.0", 10)] // Case insensitive
    public async Task ParseTargetFrameworkVersion_parses_net_versions(string tfm, int? expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("ParseTargetFrameworkVersion is called", t => DotNetToolUtilities.ParseTargetFrameworkVersion(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("ParseTargetFrameworkVersion parses .NET Core versions")]
    [Theory]
    [InlineData("netcoreapp3.1", 3)]
    [InlineData("netcoreapp3.0", 3)]
    [InlineData("netcoreapp2.1", 2)]
    [InlineData("netcoreapp2.0", 2)]
    public async Task ParseTargetFrameworkVersion_parses_netcoreapp_versions(string tfm, int? expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("ParseTargetFrameworkVersion is called", t => DotNetToolUtilities.ParseTargetFrameworkVersion(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("ParseTargetFrameworkVersion parses .NET Framework versions")]
    [Theory]
    [InlineData("net48", 48)]
    [InlineData("net472", 472)]
    [InlineData("net471", 471)]
    [InlineData("net47", 47)]
    [InlineData("net462", 462)]
    [InlineData("net461", 461)]
    [InlineData("net46", 46)]
    public async Task ParseTargetFrameworkVersion_parses_net_framework_versions(string tfm, int? expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("ParseTargetFrameworkVersion is called", t => DotNetToolUtilities.ParseTargetFrameworkVersion(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("ParseTargetFrameworkVersion returns null for .NET Standard")]
    [Theory]
    [InlineData("netstandard2.0", null)]
    [InlineData("netstandard2.1", null)]
    [InlineData("netstandard1.6", null)]
    public async Task ParseTargetFrameworkVersion_returns_null_for_netstandard(string tfm, int? expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("ParseTargetFrameworkVersion is called", t => DotNetToolUtilities.ParseTargetFrameworkVersion(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("ParseTargetFrameworkVersion handles invalid input")]
    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("invalid", null)]
    [InlineData("netX.Y", null)]
    public async Task ParseTargetFrameworkVersion_handles_invalid_input(string tfm, int? expected)
    {
        await Given($"target framework '{tfm}'", () => tfm)
            .When("ParseTargetFrameworkVersion is called", t => DotNetToolUtilities.ParseTargetFrameworkVersion(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("ParseTargetFrameworkVersion handles null input")]
    [Fact]
    public async Task ParseTargetFrameworkVersion_handles_null_input()
    {
        await Given("null target framework", () => (string?)null)
            .When("ParseTargetFrameworkVersion is called", t => DotNetToolUtilities.ParseTargetFrameworkVersion(t!))
            .Then("returns null", result => result == null)
            .AssertPassed();
    }

    [Scenario("IsDotNet10SdkInstalled returns false when dotnet command doesn't exist")]
    [Fact]
    public async Task IsDotNet10SdkInstalled_returns_false_for_nonexistent_dotnet()
    {
        await Given("a non-existent dotnet command", () => "nonexistent-dotnet-command-12345")
            .When("IsDotNet10SdkInstalled is called", cmd => DotNetToolUtilities.IsDotNet10SdkInstalled(cmd))
            .Then("returns false", result => result == false)
            .AssertPassed();
    }

    [Scenario("IsDnxAvailable returns false when dotnet command doesn't exist")]
    [Fact]
    public async Task IsDnxAvailable_returns_false_for_nonexistent_dotnet()
    {
        await Given("a non-existent dotnet command", () => "nonexistent-dotnet-command-12345")
            .When("IsDnxAvailable is called", cmd => DotNetToolUtilities.IsDnxAvailable(cmd))
            .Then("returns false", result => result == false)
            .AssertPassed();
    }

    // Note: Testing IsDotNet10SdkInstalled and IsDnxAvailable with actual dotnet executable
    // would require the .NET SDK to be installed, which is environment-dependent.
    // These tests would be better suited for integration tests.
    // The current tests verify error handling and invalid input scenarios.
}
