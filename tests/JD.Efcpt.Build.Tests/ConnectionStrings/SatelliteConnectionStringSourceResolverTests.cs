using JD.Efcpt.Build.Core.ConnectionStrings;
using JD.Efcpt.Build.Tasks.ConnectionStrings;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.ConnectionStrings;

[Feature("SatelliteConnectionStringSourceResolver: resolves the env source in-assembly and discovers satellite source assemblies")]
[Collection(nameof(AssemblySetup))]
public sealed class SatelliteConnectionStringSourceResolverTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Resolves the 'env' source in-assembly, without any search paths")]
    [Fact]
    public async Task Resolves_env_source_in_assembly()
    {
        await Given("a resolver with no search paths", () => new SatelliteConnectionStringSourceResolver())
            .When("resolve 'env'", r => r.Resolve("env"))
            .Then("returns an EnvironmentVariableConnectionStringSource", source => source is EnvironmentVariableConnectionStringSource)
            .AssertPassed();
    }

    [Scenario("Resolving 'env' is case-insensitive")]
    [Fact]
    public async Task Resolves_env_source_case_insensitively()
    {
        await Given("a resolver", () => new SatelliteConnectionStringSourceResolver())
            .When("resolve 'ENV'", r => r.Resolve("ENV"))
            .Then("returns an EnvironmentVariableConnectionStringSource", source => source is EnvironmentVariableConnectionStringSource)
            .AssertPassed();
    }

    [Scenario("Returns null for an empty or whitespace source key")]
    [Fact]
    public async Task Returns_null_for_blank_key()
    {
        await Given("a resolver", () => new SatelliteConnectionStringSourceResolver())
            .When("resolve an empty key", r => r.Resolve(""))
            .Then("returns null", source => source is null)
            .AssertPassed();
    }

    [Scenario("Returns null for an unregistered/unknown source key with no matching assembly")]
    [Fact]
    public async Task Returns_null_for_unknown_key()
    {
        await Given("a resolver with no search paths", () => new SatelliteConnectionStringSourceResolver())
            .When("resolve an unknown key", r => r.Resolve("totally-unknown-source"))
            .Then("returns null", source => source is null)
            .AssertPassed();
    }

    [Scenario("Returns null for a known satellite key when no assembly is found in any search path")]
    [Fact]
    public async Task Returns_null_for_known_satellite_key_without_assembly()
    {
        await Given("a resolver with no search paths", () => new SatelliteConnectionStringSourceResolver())
            .When("resolve 'azure-keyvault'", r => r.Resolve("azure-keyvault"))
            .Then("returns null (chain maps this to JD0033)", source => source is null)
            .AssertPassed();
    }

    [Scenario("Caches resolved sources per-instance")]
    [Fact]
    public async Task Caches_resolved_source_per_instance()
    {
        await Given("a resolver", () => new SatelliteConnectionStringSourceResolver())
            .When("resolved twice for 'env'", r => (First: r.Resolve("env"), Second: r.Resolve("env")))
            .Then("both calls return the same instance", t => ReferenceEquals(t.First, t.Second))
            .AssertPassed();
    }

    [Scenario("Candidate directories include the bundled connstr-sources folder and caller search paths")]
    [Fact]
    public async Task Candidate_directories_include_bundled_and_caller_paths()
    {
        await Given("a resolver with a caller-supplied search path", () =>
                new SatelliteConnectionStringSourceResolver([AppContext.BaseDirectory]))
            .When("enumerate candidate directories for 'azure-keyvault'",
                r => r.EnumerateCandidateDirectories("azure-keyvault").ToList())
            .Then("includes the bundled connstr-sources/azure-keyvault directory",
                dirs => dirs.Any(d => d.Replace('\\', '/').EndsWith("connstr-sources/azure-keyvault", StringComparison.OrdinalIgnoreCase)))
            .And("includes the caller-supplied search path", dirs => dirs.Contains(AppContext.BaseDirectory))
            .AssertPassed();
    }

    [Scenario("Candidate directories skip null, empty, whitespace, and non-existent caller search paths")]
    [Fact]
    public async Task Candidate_directories_skip_invalid_caller_paths()
    {
        var bogus = Path.Combine(AppContext.BaseDirectory, "definitely-does-not-exist-" + Guid.NewGuid().ToString("N"));
        await Given("a resolver with a mix of invalid and one valid search path", () =>
                new SatelliteConnectionStringSourceResolver([null!, "", "   ", bogus, AppContext.BaseDirectory]))
            .When("enumerate candidate directories for 'aws-secrets'",
                r => r.EnumerateCandidateDirectories("aws-secrets").ToList())
            .Then("does not include the bogus non-existent path", dirs => !dirs.Contains(bogus))
            .And("does include the valid caller path", dirs => dirs.Contains(AppContext.BaseDirectory))
            .AssertPassed();
    }
}
