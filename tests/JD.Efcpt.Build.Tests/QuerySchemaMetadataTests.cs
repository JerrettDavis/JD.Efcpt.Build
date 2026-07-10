using JD.Efcpt.Build.Tasks;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Unit tests for <see cref="QuerySchemaMetadata"/>'s properties that don't require a real
/// database connection. Database-backed behavior is covered by
/// <c>Integration/QuerySchemaMetadataIntegrationTests.cs</c>.
/// </summary>
[Feature("QuerySchemaMetadata: provider search path plumbing")]
[Collection(nameof(AssemblySetup))]
public sealed class QuerySchemaMetadataTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("ProviderSearchPaths defaults to an empty array when not set")]
    [Fact]
    public async Task Provider_search_paths_defaults_to_empty()
    {
        await Given("a new QuerySchemaMetadata task", () => new QuerySchemaMetadata())
            .When("reading ProviderSearchPaths without setting it", task => task.ProviderSearchPaths)
            .Then("it is an empty array, not null", paths => paths is { Length: 0 })
            .AssertPassed();
    }

    [Scenario("ProviderSearchPaths round-trips whatever the caller sets")]
    [Fact]
    public async Task Provider_search_paths_round_trips()
    {
        await Given("a QuerySchemaMetadata task with search paths set",
                () => new QuerySchemaMetadata { ProviderSearchPaths = [@"C:\one", @"C:\two"] })
            .When("reading ProviderSearchPaths", task => task.ProviderSearchPaths)
            .Then("it returns exactly what was set", paths => paths.SequenceEqual([@"C:\one", @"C:\two"]))
            .AssertPassed();
    }
}
