using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

[Feature("ProviderAdapterResolver: resolves provider adapters and caches per-instance")]
[Collection(nameof(AssemblySetup))]
public sealed class ProviderAdapterResolverTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Resolve Tests

    [Scenario("Resolves SQL Server adapter")]
    [Fact]
    public async Task Resolves_sql_server_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for mssql", r => r.Resolve("mssql"))
            .Then("returns a non-null SqlServerProviderAdapter", adapter => adapter is SqlServerProviderAdapter)
            .AssertPassed();
    }

    [Scenario("Resolves PostgreSQL adapter")]
    [Fact]
    public async Task Resolves_postgres_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for postgres", r => r.Resolve("postgres"))
            .Then("returns a non-null PostgreSqlProviderAdapter", adapter => adapter is PostgreSqlProviderAdapter)
            .AssertPassed();
    }

    [Scenario("Resolves MySQL adapter")]
    [Fact]
    public async Task Resolves_mysql_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for mysql", r => r.Resolve("mysql"))
            .Then("returns a non-null MySqlProviderAdapter", adapter => adapter is MySqlProviderAdapter)
            .AssertPassed();
    }

    [Scenario("Resolves SQLite adapter")]
    [Fact]
    public async Task Resolves_sqlite_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for sqlite", r => r.Resolve("sqlite"))
            .Then("returns a non-null SqliteProviderAdapter", adapter => adapter is SqliteProviderAdapter)
            .AssertPassed();
    }

    [Scenario("Resolves Firebird adapter")]
    [Fact]
    public async Task Resolves_firebird_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for firebird", r => r.Resolve("firebird"))
            .Then("returns a non-null FirebirdProviderAdapter", adapter => adapter is FirebirdProviderAdapter)
            .AssertPassed();
    }

    #endregion

    #region Satellite Provider: Snowflake

    /// <summary>
    /// Snowflake was the first provider extracted into a satellite package
    /// (JD.Efcpt.Build.Snowflake), so it now resolves exclusively through
    /// <see cref="ProviderAdapterResolver"/>'s dynamic-loading path rather than the in-assembly
    /// dictionary. The Tests project ProjectReferences JD.Efcpt.Build.Snowflake (test-only
    /// weight), so its built adapter DLL is already sitting in this test assembly's own output
    /// directory - pointing ProviderSearchPaths there exercises the exact same
    /// find-load-instantiate path a real satellite package install goes through.
    /// </summary>
    private static readonly string[] TestAssemblyDirectorySearchPath =
        [Path.GetDirectoryName(typeof(ProviderAdapterResolverTests).Assembly.Location)!];

    [Scenario("Resolves Snowflake adapter dynamically from a satellite provider search path")]
    [Fact]
    public async Task Resolves_snowflake_adapter_from_search_path()
    {
        await Given("a resolver and this test assembly's own directory as a provider search path",
                () => (Resolver: new ProviderAdapterResolver(), SearchPaths: TestAssemblyDirectorySearchPath))
            .When("resolved for snowflake", t => t.Resolver.Resolve("snowflake", t.SearchPaths))
            .Then("returns a non-null SnowflakeProviderAdapter", adapter => adapter is SnowflakeProviderAdapter)
            .AssertPassed();
    }

    [Scenario("Throws ProviderDriverNotFoundException for Snowflake when no search path contains its assembly")]
    [Fact]
    public async Task Throws_for_snowflake_without_matching_search_path()
    {
        await Given("a resolver with no search paths", () => new ProviderAdapterResolver())
            .When("resolved for snowflake", r =>
            {
                try
                {
                    r.Resolve("snowflake");
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .Then("throws ProviderDriverNotFoundException with the install command",
                ex => ex is ProviderDriverNotFoundException &&
                      ex.Message.Contains("dotnet add package JD.Efcpt.Build.Snowflake"))
            .AssertPassed();
    }

    #endregion

    #region Satellite Provider: Oracle

    /// <summary>
    /// Oracle is a satellite provider (JD.Efcpt.Build.Oracle); see the Snowflake region above
    /// for why a search path is required and how the Tests project makes this DLL available.
    /// </summary>
    private static readonly string[] OracleSearchPath =
        [Path.GetDirectoryName(typeof(ProviderAdapterResolverTests).Assembly.Location)!];

    [Scenario("Resolves Oracle adapter dynamically from a satellite provider search path")]
    [Fact]
    public async Task Resolves_oracle_adapter_from_search_path()
    {
        await Given("a resolver and this test assembly's own directory as a provider search path",
                () => (Resolver: new ProviderAdapterResolver(), SearchPaths: OracleSearchPath))
            .When("resolved for oracle", t => t.Resolver.Resolve("oracle", t.SearchPaths))
            .Then("returns a non-null OracleProviderAdapter", adapter => adapter is OracleProviderAdapter)
            .AssertPassed();
    }

    [Scenario("Throws ProviderDriverNotFoundException for Oracle when no search path contains its assembly")]
    [Fact]
    public async Task Throws_for_oracle_without_matching_search_path()
    {
        await Given("a resolver with no search paths", () => new ProviderAdapterResolver())
            .When("resolved for oracle", r =>
            {
                try
                {
                    r.Resolve("oracle");
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .Then("throws ProviderDriverNotFoundException with the install command",
                ex => ex is ProviderDriverNotFoundException &&
                      ex.Message.Contains("dotnet add package JD.Efcpt.Build.Oracle"))
            .AssertPassed();
    }

    #endregion

    #region Unknown Provider

    [Scenario("Throws ProviderDriverNotFoundException for an unresolvable provider")]
    [Fact]
    public async Task Throws_for_unknown_provider()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for an unregistered provider name", r =>
            {
                try
                {
                    r.Resolve("mongodb");
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .Then("throws ProviderDriverNotFoundException", ex => ex is ProviderDriverNotFoundException)
            .And("message contains the provider name", ex => ex!.Message.Contains("mongodb"))
            .AssertPassed();
    }

    #endregion

    #region Caching

    [Scenario("Caches resolved adapters per resolver instance")]
    [Fact]
    public async Task Caches_resolved_adapter_per_instance()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved twice for the same provider", r => (First: r.Resolve("postgres"), Second: r.Resolve("postgres")))
            .Then("both calls return the same instance", t => ReferenceEquals(t.First, t.Second))
            .AssertPassed();
    }

    [Scenario("Does not share cache across separate resolver instances")]
    [Fact]
    public async Task Does_not_share_cache_across_instances()
    {
        await Given("two independent resolvers", () => (First: new ProviderAdapterResolver(), Second: new ProviderAdapterResolver()))
            .When("each resolves the same provider", t => (FirstAdapter: t.First.Resolve("mysql"), SecondAdapter: t.Second.Resolve("mysql")))
            .Then("the adapters are distinct instances", t => !ReferenceEquals(t.FirstAdapter, t.SecondAdapter))
            .AssertPassed();
    }

    #endregion

    #region Cache Key Correctness (search-path-aware caching)

    [Scenario("Cache key is the provider name alone when no search paths are supplied")]
    [Fact]
    public async Task Cache_key_is_provider_name_alone_with_no_search_paths()
    {
        await Given("a provider with no search paths", () => "postgres")
            .When("cache key built", p => ProviderAdapterResolver.BuildCacheKey(p, []))
            .Then("the key equals the provider name", key => key == "postgres")
            .AssertPassed();
    }

    [Scenario("Cache key is unaffected by search-path order")]
    [Fact]
    public async Task Cache_key_is_order_independent()
    {
        await Given("the same two search paths in different orders",
                () => (A: new[] { @"C:\a", @"C:\b" }, B: new[] { @"C:\b", @"C:\a" }))
            .When("cache keys built for both orderings",
                t => (KeyA: ProviderAdapterResolver.BuildCacheKey("postgres", t.A), KeyB: ProviderAdapterResolver.BuildCacheKey("postgres", t.B)))
            .Then("both keys are identical", t => t.KeyA == t.KeyB)
            .AssertPassed();
    }

    [Scenario("Cache key de-duplicates repeated and differently-cased search paths")]
    [Fact]
    public async Task Cache_key_deduplicates_paths()
    {
        await Given("a search path repeated with different casing",
                () => (Once: new[] { @"C:\providers" }, Twice: new[] { @"C:\providers", @"C:\Providers", @"c:\providers" }))
            .When("cache keys built for both sets",
                t => (KeyOnce: ProviderAdapterResolver.BuildCacheKey("postgres", t.Once), KeyTwice: ProviderAdapterResolver.BuildCacheKey("postgres", t.Twice)))
            .Then("both keys are identical", t => t.KeyOnce == t.KeyTwice)
            .AssertPassed();
    }

    [Scenario("Cache key differs for different search-path sets on the same provider")]
    [Fact]
    public async Task Cache_key_differs_for_different_search_paths()
    {
        await Given("two distinct search-path sets for the same provider",
                () => (A: new[] { @"C:\projectA\providers" }, B: new[] { @"C:\projectB\providers" }))
            .When("cache keys built for both",
                t => (KeyA: ProviderAdapterResolver.BuildCacheKey("postgres", t.A), KeyB: ProviderAdapterResolver.BuildCacheKey("postgres", t.B)))
            .Then("the keys are different", t => t.KeyA != t.KeyB)
            .AssertPassed();
    }

    [Scenario("Cache key ignores null, empty, and whitespace-only search-path entries")]
    [Fact]
    public async Task Cache_key_ignores_blank_entries()
    {
        await Given("a search path set padded with blank entries",
                () => (Clean: new[] { @"C:\providers" }, Padded: new[] { "", "   ", @"C:\providers", null! }))
            .When("cache keys built for both",
                t => (KeyClean: ProviderAdapterResolver.BuildCacheKey("postgres", t.Clean), KeyPadded: ProviderAdapterResolver.BuildCacheKey("postgres", t.Padded)))
            .Then("both keys are identical", t => t.KeyClean == t.KeyPadded)
            .AssertPassed();
    }

    [Scenario("Cache key differs across provider names given the same search paths")]
    [Fact]
    public async Task Cache_key_differs_by_provider_name()
    {
        await Given("the same search paths for two different providers", () => new[] { @"C:\providers" })
            .When("cache keys built for both providers",
                paths => (Postgres: ProviderAdapterResolver.BuildCacheKey("postgres", paths), MySql: ProviderAdapterResolver.BuildCacheKey("mysql", paths)))
            .Then("the keys are different", t => t.Postgres != t.MySql)
            .AssertPassed();
    }

    #endregion
}
