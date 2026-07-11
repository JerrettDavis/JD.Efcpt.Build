using JD.Efcpt.Build.Core.ConnectionStrings;
using JD.Efcpt.Build.Core.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Core.Tests.ConnectionStrings;

[Feature("ConnectionStringResolutionChain: Branch 0 - pluggable connection-string sources are fail-closed")]
[Collection(nameof(AssemblySetup))]
public sealed class ConnectionStringResolutionChainSourceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    /// <summary>A source whose result/exception is fully controlled by the test.</summary>
    private sealed class FakeConnectionStringSource(
        string key,
        ConnectionStringSourceResult? result = null,
        Exception? throwException = null) : IConnectionStringSource
    {
        public string Key { get; } = key;
        public int Priority => 0;
        public bool? ReceivedOffline { get; private set; }
        public IReadOnlyDictionary<string, string>? ReceivedSettings { get; private set; }

        public ConnectionStringSourceResult Resolve(in ConnectionStringSourceContext context)
        {
            ReceivedOffline = context.Offline;
            ReceivedSettings = context.Settings;

            if (throwException is not null)
                throw throwException;

            return result ?? ConnectionStringSourceResult.Found(Key, "unused");
        }
    }

    /// <summary>Resolves only the keys explicitly registered with it; everything else is "not installed".</summary>
    private sealed class FakeConnectionStringSourceResolver : IConnectionStringSourceResolver
    {
        private readonly Dictionary<string, IConnectionStringSource> _sources = new(StringComparer.OrdinalIgnoreCase);

        public FakeConnectionStringSourceResolver Register(IConnectionStringSource source)
        {
            _sources[source.Key] = source;
            return this;
        }

        public IConnectionStringSource? Resolve(string sourceKey)
            => _sources.TryGetValue(sourceKey, out var source) ? source : null;
    }

    private static readonly ResultChainThrowsHelper Chain = new();

    /// <summary>Small helper so scenarios can capture "did it throw, and with what" as ordinary data.</summary>
    private sealed class ResultChainThrowsHelper
    {
        public (bool Threw, ConnectionStringSourceException? Exception, string? Result) Execute(ConnectionStringResolutionContext ctx)
        {
            try
            {
                var chain = ConnectionStringResolutionChain.Build();
                var success = chain.Execute(in ctx, out var result);
                return (false, null, success ? result : null);
            }
            catch (ConnectionStringSourceException ex)
            {
                return (true, ex, null);
            }
        }
    }

    private static ConnectionStringResolutionContext BuildContext(
        string connectionStringSource,
        IConnectionStringSourceResolver? resolver,
        string explicitConnectionString = "",
        bool offline = false,
        Dictionary<string, string>? settings = null)
        => new(
            ExplicitConnectionString: explicitConnectionString,
            EfcptAppSettings: "",
            EfcptAppConfig: "",
            ConnectionStringName: "DefaultConnection",
            ProjectDirectory: Path.GetTempPath(),
            Log: new RecordingBuildLog(),
            ConnectionStringSource: connectionStringSource,
            SourceSettings: settings,
            Offline: offline,
            SourceResolver: resolver);

    #region Found

    [Scenario("Found outcome returns the connection string and does not throw")]
    [Fact]
    public async Task Found_returns_connection_string()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.Found("fake", "Server=fake-host;"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context selecting a source that resolves Found", () =>
                BuildContext("fake", resolver))
            .When("execute the chain", Chain.Execute)
            .Then("does not throw", r => !r.Threw)
            .And("returns the resolved connection string", r => r.Result == "Server=fake-host;")
            .AssertPassed();
    }

    #endregion

    #region Fail-closed outcome -> JD code mapping

    [Scenario("NotFound outcome throws with JD0031")]
    [Fact]
    public async Task Not_found_throws_jd0031()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.NotFound("fake", "secret absent"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context selecting a source that resolves NotFound", () =>
                BuildContext("fake", resolver))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0031", r => r.Exception!.Code == "JD0031")
            .And("source key is 'fake'", r => r.Exception!.SourceKey == "fake")
            .AssertPassed();
    }

    [Scenario("Failed outcome throws with JD0030")]
    [Fact]
    public async Task Failed_throws_jd0030()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.Failed("fake", "boom"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context selecting a source that resolves Failed", () =>
                BuildContext("fake", resolver))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0030", r => r.Exception!.Code == "JD0030")
            .AssertPassed();
    }

    [Scenario("A source that throws an unexpected exception is mapped to JD0030")]
    [Fact]
    public async Task Unexpected_exception_throws_jd0030()
    {
        var source = new FakeConnectionStringSource("fake", throwException: new InvalidOperationException("kaboom"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context selecting a source that throws", () =>
                BuildContext("fake", resolver))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0030", r => r.Exception!.Code == "JD0030")
            .And("inner exception is preserved", r => r.Exception!.InnerException is InvalidOperationException)
            .AssertPassed();
    }

    [Scenario("OfflineBlocked outcome throws with JD0032")]
    [Fact]
    public async Task Offline_blocked_throws_jd0032()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.OfflineBlocked("fake", "network blocked"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context selecting a source that resolves OfflineBlocked", () =>
                BuildContext("fake", resolver, offline: true))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0032", r => r.Exception!.Code == "JD0032")
            .AssertPassed();
    }

    [Scenario("Misconfigured outcome throws with JD0034")]
    [Fact]
    public async Task Misconfigured_throws_jd0034()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.Misconfigured("fake", "missing setting"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context selecting a source that resolves Misconfigured", () =>
                BuildContext("fake", resolver))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0034", r => r.Exception!.Code == "JD0034")
            .AssertPassed();
    }

    #endregion

    #region Satellite not installed

    [Scenario("Selecting an unregistered source key throws with JD0033")]
    [Fact]
    public async Task Unregistered_source_key_throws_jd0033()
    {
        var resolver = new FakeConnectionStringSourceResolver(); // nothing registered

        await Given("a context selecting a source the resolver doesn't know", () =>
                BuildContext("azure-keyvault", resolver))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0033", r => r.Exception!.Code == "JD0033")
            .And("message includes install guidance", r => r.Exception!.Message.Contains("dotnet add package JD.Efcpt.Build.ConnectionStrings.AzureKeyVault"))
            .AssertPassed();
    }

    [Scenario("A null SourceResolver with a selected source throws with JD0033")]
    [Fact]
    public async Task Null_resolver_throws_jd0033()
    {
        await Given("a context selecting a source with no resolver supplied", () =>
                BuildContext("env", resolver: null))
            .When("execute the chain", Chain.Execute)
            .Then("throws", r => r.Threw)
            .And("with code JD0033", r => r.Exception!.Code == "JD0033")
            .AssertPassed();
    }

    #endregion

    #region No fall-through

    [Scenario("A failing source does not fall through to the explicit connection string branch")]
    [Fact]
    public async Task Failing_source_does_not_fall_through_to_explicit_connection_string()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.NotFound("fake"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);

        await Given("a context with both a failing source and a valid explicit connection string", () =>
                BuildContext("fake", resolver, explicitConnectionString: "Server=should-not-be-used;"))
            .When("execute the chain", Chain.Execute)
            .Then("throws instead of returning the explicit connection string", r => r.Threw)
            .And("with code JD0031", r => r.Exception!.Code == "JD0031")
            .AssertPassed();
    }

    #endregion

    #region Offline and settings are threaded through to the source

    [Scenario("Offline flag and settings are passed through to the selected source")]
    [Fact]
    public async Task Offline_and_settings_are_threaded_through()
    {
        var source = new FakeConnectionStringSource("fake", ConnectionStringSourceResult.Found("fake", "Server=x;"));
        var resolver = new FakeConnectionStringSourceResolver().Register(source);
        var settings = new Dictionary<string, string> { ["keyVaultUri"] = "https://example.vault.azure.net/" };

        await Given("a context with offline=true and custom settings", () =>
                (Ctx: BuildContext("fake", resolver, offline: true, settings: settings), Source: source))
            .When("execute the chain", t => (Chain.Execute(t.Ctx), t.Source))
            .Then("the source observed offline=true", t => t.Item2.ReceivedOffline == true)
            .And("the source observed the settings", t => t.Item2.ReceivedSettings!["keyVaultUri"] == "https://example.vault.azure.net/")
            .AssertPassed();
    }

    #endregion

    #region Backward compatibility: empty ConnectionStringSource behaves exactly like today

    [Scenario("Empty ConnectionStringSource does not consult the resolver at all")]
    [Fact]
    public async Task Empty_source_does_not_consult_resolver()
    {
        await Given("a context with no source selected but an explicit connection string", () =>
                BuildContext("", resolver: null, explicitConnectionString: "Server=classic;"))
            .When("execute the chain", Chain.Execute)
            .Then("does not throw", r => !r.Threw)
            .And("falls through to the explicit connection string branch as before", r => r.Result == "Server=classic;")
            .AssertPassed();
    }

    [Scenario("Empty ConnectionStringSource with nothing configured returns null (fallback to .sqlproj)")]
    [Fact]
    public async Task Empty_source_with_nothing_configured_returns_null()
    {
        await Given("a context with nothing configured", () =>
                BuildContext("", resolver: null, explicitConnectionString: ""))
            .When("execute the chain", Chain.Execute)
            .Then("does not throw", r => !r.Threw)
            .And("result is null", r => r.Result is null)
            .AssertPassed();
    }

    #endregion
}
