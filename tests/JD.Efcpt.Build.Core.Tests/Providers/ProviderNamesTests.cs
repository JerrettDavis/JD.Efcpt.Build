using JD.Efcpt.Build.Core.Providers;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Core.Tests.Providers;

/// <summary>
/// Tests for <see cref="ProviderNames"/>, extracted from
/// <c>JD.Efcpt.Build.Tasks.Schema.DatabaseProviderFactory</c> in #181. These are the "provider
/// normalize" cases that used to live alongside the adapter-resolution tests in
/// <c>DatabaseProviderFactoryTests</c> - relocated/duplicated here for the newly-extracted Core
/// type so it has direct, adapter-free coverage; <c>DatabaseProviderFactoryTests</c> in
/// JD.Efcpt.Build.Tests continues to cover <c>NormalizeProvider</c>/<c>GetProviderDisplayName</c>
/// as thin delegating wrappers, unchanged.
/// </summary>
[Feature("ProviderNames: normalizes provider aliases and maps display names")]
[Collection(nameof(AssemblySetup))]
public sealed partial class ProviderNamesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Normalize maps every recognized alias to its canonical name")]
    [Theory]
    [InlineData("mssql", "mssql")]
    [InlineData("sqlserver", "mssql")]
    [InlineData("sql-server", "mssql")]
    [InlineData("SqlServer", "mssql")]
    [InlineData("postgres", "postgres")]
    [InlineData("postgresql", "postgres")]
    [InlineData("pgsql", "postgres")]
    [InlineData("mysql", "mysql")]
    [InlineData("mariadb", "mysql")]
    [InlineData("sqlite", "sqlite")]
    [InlineData("sqlite3", "sqlite")]
    [InlineData("oracle", "oracle")]
    [InlineData("oracledb", "oracle")]
    [InlineData("firebird", "firebird")]
    [InlineData("fb", "firebird")]
    [InlineData("snowflake", "snowflake")]
    [InlineData("sf", "snowflake")]
    public async Task Normalize_maps_aliases_to_canonical_name(string input, string expected)
    {
        await Given($"provider alias '{input}'", () => input)
            .When("Normalize is called", ProviderNames.Normalize)
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalize throws NotSupportedException for an unrecognized provider")]
    [Fact]
    public async Task Normalize_throws_for_unrecognized_provider()
    {
        await Given("an unrecognized provider name", () => "cockroachdb")
            .When("Normalize is called", provider =>
            {
                try
                {
                    ProviderNames.Normalize(provider);
                    return (Threw: false, Message: "");
                }
                catch (NotSupportedException ex)
                {
                    return (Threw: true, Message: ex.Message);
                }
            })
            .Then("throws NotSupportedException", r => r.Threw)
            .And("message lists the supported providers", r => r.Message.Contains("mssql") && r.Message.Contains("snowflake"))
            .AssertPassed();
    }

    [Scenario("Normalize throws ArgumentException for null or whitespace")]
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Normalize_throws_for_null_or_whitespace(string input)
    {
        await Given($"a whitespace/empty provider name", () => input)
            .When("Normalize is called", provider =>
            {
                try
                {
                    ProviderNames.Normalize(provider);
                    return false;
                }
                catch (ArgumentException)
                {
                    return true;
                }
            })
            .Then("throws ArgumentException", threw => threw)
            .AssertPassed();
    }

    [Scenario("GetDisplayName maps every canonical provider to its human-readable name")]
    [Theory]
    [InlineData("mssql", "SQL Server")]
    [InlineData("postgres", "PostgreSQL")]
    [InlineData("mysql", "MySQL/MariaDB")]
    [InlineData("sqlite", "SQLite")]
    [InlineData("oracle", "Oracle")]
    [InlineData("firebird", "Firebird")]
    [InlineData("snowflake", "Snowflake")]
    public async Task GetDisplayName_maps_canonical_names(string provider, string expectedDisplayName)
    {
        await Given($"canonical provider '{provider}'", () => provider)
            .When("GetDisplayName is called", ProviderNames.GetDisplayName)
            .Then($"returns '{expectedDisplayName}'", result => result == expectedDisplayName)
            .AssertPassed();
    }

    [Scenario("GetDisplayName resolves an alias through normalization first")]
    [Fact]
    public async Task GetDisplayName_resolves_alias_through_normalization()
    {
        await Given("an alias, not a canonical name", () => "sql-server")
            .When("GetDisplayName is called", ProviderNames.GetDisplayName)
            .Then("returns the canonical display name", result => result == "SQL Server")
            .AssertPassed();
    }

    [Scenario("SupportedProviders lists exactly the seven canonical provider names")]
    [Fact]
    public async Task SupportedProviders_lists_all_canonical_names()
    {
        await Given("the SupportedProviders list", () => ProviderNames.SupportedProviders)
            .When("compared against the expected set", list => list.OrderBy(x => x).ToArray())
            .Then("matches exactly", result => result.SequenceEqual(
                new[] { "firebird", "mssql", "mysql", "oracle", "postgres", "snowflake", "sqlite" }.OrderBy(x => x)))
            .AssertPassed();
    }
}
