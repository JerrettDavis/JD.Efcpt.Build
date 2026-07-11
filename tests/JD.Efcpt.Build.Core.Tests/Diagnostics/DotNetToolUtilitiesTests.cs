using JD.Efcpt.Build.Core.Diagnostics;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Core.Tests.Diagnostics;

/// <summary>
/// Tests for the DotNetToolUtilities class that handles .NET SDK and runtime detection.
/// </summary>
[Feature("DotNetToolUtilities: .NET SDK and runtime detection")]
[Collection(nameof(AssemblySetup))]
public sealed partial class DotNetToolUtilitiesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
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
            .Then("returns false", result => !result)
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

    [Scenario("IsDotNet10OrLater handles frameworks with modifiers")]
    [Theory]
    [InlineData("net10.0-windows", true)]
    [InlineData("net10.0-macos", true)]
    [InlineData("net10.0-android", true)]
    [InlineData("net9.0-android", false)]
    [InlineData("net8.0-ios", false)]
    public async Task IsDotNet10OrLater_handles_framework_modifiers(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}' with modifier", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles single-digit major version")]
    [Theory]
    [InlineData("net5", false)]
    [InlineData("net6", false)]
    [InlineData("net7", false)]
    [InlineData("net8", false)]
    [InlineData("net9", false)]
    public async Task IsDotNet10OrLater_handles_single_digit_versions(string tfm, bool expected)
    {
        await Given($"target framework '{tfm}' without minor version", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    [Scenario("IsDotNet10OrLater handles whitespace")]
    [Theory]
    [InlineData("  net10.0  ", true)]
    [InlineData("\tnet10.0\t", true)]
    [InlineData("  net9.0  ", false)]
    public async Task IsDotNet10OrLater_handles_whitespace(string tfm, bool expected)
    {
        await Given($"target framework with whitespace", () => tfm)
            .When("IsDotNet10OrLater is called", t => DotNetToolUtilities.IsDotNet10OrLater(t))
            .Then($"returns {expected}", result => result == expected)
            .AssertPassed();
    }

    // MapListSdksOutcome / MapListRuntimesOutcome are the pure decision-logic seams extracted
    // from ProbeDotNet10SdkInstalled / ProbeDnxAvailable so the exit-code/output parsing can be
    // unit tested directly, without spawning a real `dotnet` process.

    [Scenario("MapListSdksOutcome reports Unavailable for a non-zero exit code regardless of output")]
    [Fact]
    public async Task MapListSdksOutcome_reports_unavailable_for_nonzero_exit_code()
    {
        await Given("a non-zero exit code and output that would otherwise qualify", () => (ExitCode: 1, Output: "10.0.100 [C:\\dotnet\\sdk]"))
            .When("MapListSdksOutcome is called", args => DotNetToolUtilities.MapListSdksOutcome(args.ExitCode, args.Output))
            .Then("returns Unavailable", result => result == ProbeOutcome.Unavailable)
            .AssertPassed();
    }

    [Scenario("MapListSdksOutcome reports Available when a qualifying SDK version is listed")]
    [Theory]
    [InlineData("10.0.100 [C:\\Program Files\\dotnet\\sdk]", true)]
    [InlineData("9.0.100 [C:\\Program Files\\dotnet\\sdk]\n10.0.100 [C:\\Program Files\\dotnet\\sdk]", true)]
    [InlineData("11.0.100 [C:\\Program Files\\dotnet\\sdk]", true)]
    [InlineData("9.0.100 [C:\\Program Files\\dotnet\\sdk]\n8.0.404 [C:\\Program Files\\dotnet\\sdk]", false)]
    [InlineData("", false)]
    [InlineData("not-a-version-line", false)]
    public async Task MapListSdksOutcome_maps_output_to_expected_outcome(string output, bool expectAvailable)
    {
        await Given("a zero exit code and captured `dotnet --list-sdks` output", () => output)
            .When("MapListSdksOutcome is called", o => DotNetToolUtilities.MapListSdksOutcome(0, o))
            .Then($"returns {(expectAvailable ? "Available" : "Unavailable")}", result =>
                result == (expectAvailable ? ProbeOutcome.Available : ProbeOutcome.Unavailable))
            .AssertPassed();
    }

    [Scenario("MapListRuntimesOutcome reports Unavailable for a non-zero exit code regardless of output")]
    [Fact]
    public async Task MapListRuntimesOutcome_reports_unavailable_for_nonzero_exit_code()
    {
        await Given("a non-zero exit code and output that would otherwise qualify", () => (ExitCode: 1, Output: "Microsoft.NETCore.App 10.0.0 [C:\\dotnet\\shared]"))
            .When("MapListRuntimesOutcome is called", args => DotNetToolUtilities.MapListRuntimesOutcome(args.ExitCode, args.Output))
            .Then("returns Unavailable", result => result == ProbeOutcome.Unavailable)
            .AssertPassed();
    }

    [Scenario("MapListRuntimesOutcome reports Available when a qualifying runtime is listed")]
    [Theory]
    [InlineData("Microsoft.NETCore.App 10.0.0 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]", true)]
    [InlineData("Microsoft.NETCore.App 9.0.0 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]", false)]
    [InlineData("malformed-line-with-no-version", false)]
    [InlineData("", false)]
    [InlineData("   \nMicrosoft.NETCore.App 10.0.0 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]", true)] // blank line is skipped before the qualifying line
    public async Task MapListRuntimesOutcome_maps_output_to_expected_outcome(string output, bool expectAvailable)
    {
        await Given("a zero exit code and captured `dotnet --list-runtimes` output", () => output)
            .When("MapListRuntimesOutcome is called", o => DotNetToolUtilities.MapListRuntimesOutcome(0, o))
            .Then($"returns {(expectAvailable ? "Available" : "Unavailable")}", result =>
                result == (expectAvailable ? ProbeOutcome.Available : ProbeOutcome.Unavailable))
            .AssertPassed();
    }
}

