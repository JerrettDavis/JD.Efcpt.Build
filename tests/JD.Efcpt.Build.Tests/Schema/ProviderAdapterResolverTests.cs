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

    [Scenario("Resolves Oracle adapter")]
    [Fact]
    public async Task Resolves_oracle_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for oracle", r => r.Resolve("oracle"))
            .Then("returns a non-null OracleProviderAdapter", adapter => adapter is OracleProviderAdapter)
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

    [Scenario("Resolves Snowflake adapter")]
    [Fact]
    public async Task Resolves_snowflake_adapter()
    {
        await Given("a resolver", () => new ProviderAdapterResolver())
            .When("resolved for snowflake", r => r.Resolve("snowflake"))
            .Then("returns a non-null SnowflakeProviderAdapter", adapter => adapter is SnowflakeProviderAdapter)
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
}
