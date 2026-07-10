using JD.Efcpt.Build.Tasks.Schema;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

[Feature("ProviderDriverNotFoundException: builds an actionable, provider-specific message")]
[Collection(nameof(AssemblySetup))]
public sealed class ProviderDriverNotFoundExceptionTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Install Instructions

    [Scenario("Message includes provider name and exact install command for a satellite provider")]
    [Theory]
    [InlineData("postgres", "PostgreSQL")]
    [InlineData("mysql", "MySQL")]
    [InlineData("sqlite", "Sqlite")]
    [InlineData("oracle", "Oracle")]
    [InlineData("firebird", "Firebird")]
    [InlineData("snowflake", "Snowflake")]
    public async Task Message_includes_install_command(string provider, string packageSuffix)
    {
        await Given($"provider '{provider}'", () => provider)
            .When("exception constructed", p => new ProviderDriverNotFoundException(p))
            .Then("message contains the provider name", ex => ex.Message.Contains(provider))
            .And("message contains the exact dotnet add package command",
                ex => ex.Message.Contains($"dotnet add package JD.Efcpt.Build.{packageSuffix}"))
            .AssertPassed();
    }

    [Scenario("Message for the bundled mssql provider has no install command")]
    [Fact]
    public async Task Message_for_bundled_provider_has_no_install_command()
    {
        await Given("the mssql provider", () => "mssql")
            .When("exception constructed", p => new ProviderDriverNotFoundException(p))
            .Then("message contains the provider name", ex => ex.Message.Contains("mssql"))
            .And("message does not contain a dotnet add package command", ex => !ex.Message.Contains("dotnet add package"))
            .AssertPassed();
    }

    #endregion

    #region Provider Property

    [Scenario("Exposes the offending provider name")]
    [Fact]
    public async Task Exposes_provider_property()
    {
        await Given("provider 'postgres'", () => "postgres")
            .When("exception constructed", p => new ProviderDriverNotFoundException(p))
            .Then("Provider property matches", ex => ex.Provider == "postgres")
            .AssertPassed();
    }

    #endregion

    #region Inner Exception Constructor

    [Scenario("Inner-exception constructor preserves the original failure and still surfaces install guidance")]
    [Fact]
    public async Task Inner_exception_constructor_preserves_original_failure()
    {
        var original = new BadImageFormatException("bad image");

        await Given("a load failure for provider 'postgres'", () => original)
            .When("exception constructed with the inner exception", inner => new ProviderDriverNotFoundException("postgres", inner))
            .Then("InnerException is the exact instance passed in", ex => ReferenceEquals(ex.InnerException, original))
            .And("message still contains the dotnet add package command", ex => ex.Message.Contains("dotnet add package JD.Efcpt.Build.PostgreSQL"))
            .And("Provider property matches", ex => ex.Provider == "postgres")
            .AssertPassed();
    }

    #endregion

    #region Package Suffix Table

    [Scenario("Package suffix table maps every known provider")]
    [Fact]
    public async Task Package_suffix_table_maps_every_known_provider()
    {
        await Given("the package suffix table", () => ProviderDriverNotFoundException.PackageSuffixesByProvider)
            .When("checking known providers", table => new[]
            {
                "mssql", "postgres", "mysql", "sqlite", "oracle", "firebird", "snowflake"
            }.All(table.ContainsKey))
            .Then("all seven providers are present", allPresent => allPresent)
            .AssertPassed();
    }

    #endregion
}
