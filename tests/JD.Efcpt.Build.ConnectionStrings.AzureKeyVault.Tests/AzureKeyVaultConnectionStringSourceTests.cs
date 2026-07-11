using Azure;
using JD.Efcpt.Build.ConnectionStrings.AzureKeyVault;
using JD.Efcpt.Build.Core.ConnectionStrings;
using JD.Efcpt.Build.Core.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.ConnectionStrings.AzureKeyVault.Tests;

[Feature("AzureKeyVaultConnectionStringSource: resolves connection strings from Azure Key Vault secrets")]
public sealed class AzureKeyVaultConnectionStringSourceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    /// <summary>A fake <see cref="ISecretClient"/> that records the request and returns a scripted response/exception.</summary>
    private sealed class FakeSecretClient(string? value = null, Exception? throwException = null) : ISecretClient
    {
        public bool Called { get; private set; }
        public string? RequestedSecretName { get; private set; }
        public string? RequestedVersion { get; private set; }

        public string GetSecretValue(string secretName, string? version, CancellationToken cancellationToken)
        {
            Called = true;
            RequestedSecretName = secretName;
            RequestedVersion = version;

            if (throwException is not null)
                throw throwException;

            return value ?? "";
        }
    }

    private static ConnectionStringSourceContext BuildContext(Dictionary<string, string> settings, bool offline = false)
        => new(SourceKey: "azure-keyvault", Settings: settings, Offline: offline, Log: NullBuildLog.Instance);

    #region Key / Priority

    [Scenario("Key and priority are stable")]
    [Fact]
    public async Task Key_and_priority_are_stable()
    {
        await Given("a source", () => new AzureKeyVaultConnectionStringSource())
            .When("read Key/Priority", s => (s.Key, s.Priority))
            .Then("key is 'azure-keyvault'", r => r.Key == "azure-keyvault")
            .AssertPassed();
    }

    #endregion

    #region Settings -> Request Mapping

    [Scenario("Maps keyVaultUri/secretName/secretVersion settings to the client request")]
    [Fact]
    public async Task Maps_settings_to_request()
    {
        var client = new FakeSecretClient("Server=vault-host;Database=VaultDb;");
        var source = new AzureKeyVaultConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "MyConnectionString",
            ["secretVersion"] = "abc123"
        };

        await Given("a context with full settings", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Found", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .And("connection string is returned", r => r.ConnectionString == "Server=vault-host;Database=VaultDb;")
            .And("client received the secret name", _ => client.RequestedSecretName == "MyConnectionString")
            .And("client received the secret version", _ => client.RequestedVersion == "abc123")
            .AssertPassed();
    }

    [Scenario("Omits secretVersion when not configured")]
    [Fact]
    public async Task Omits_secret_version_when_not_configured()
    {
        var client = new FakeSecretClient("Server=x;");
        var source = new AzureKeyVaultConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "MyConnectionString"
        };

        await Given("a context without secretVersion", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Found", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .And("client received a null version", _ => client.RequestedVersion is null)
            .AssertPassed();
    }

    #endregion

    #region Misconfigured

    [Scenario("Missing keyVaultUri is Misconfigured")]
    [Fact]
    public async Task Missing_key_vault_uri_is_misconfigured()
    {
        var source = new AzureKeyVaultConnectionStringSource(_ => new FakeSecretClient());
        var settings = new Dictionary<string, string> { ["secretName"] = "MyConnectionString" };

        await Given("a context missing keyVaultUri", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .And("diagnostic mentions keyVaultUri", r => r.Diagnostic != null && r.Diagnostic.Contains("keyVaultUri"))
            .AssertPassed();
    }

    [Scenario("Missing secretName is Misconfigured")]
    [Fact]
    public async Task Missing_secret_name_is_misconfigured()
    {
        var source = new AzureKeyVaultConnectionStringSource(_ => new FakeSecretClient());
        var settings = new Dictionary<string, string> { ["keyVaultUri"] = "https://example.vault.azure.net/" };

        await Given("a context missing secretName", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .And("diagnostic mentions secretName", r => r.Diagnostic != null && r.Diagnostic.Contains("secretName"))
            .AssertPassed();
    }

    [Scenario("Invalid keyVaultUri is Misconfigured")]
    [Fact]
    public async Task Invalid_key_vault_uri_is_misconfigured()
    {
        var source = new AzureKeyVaultConnectionStringSource(_ => new FakeSecretClient());
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "not a uri",
            ["secretName"] = "MyConnectionString"
        };

        await Given("a context with an invalid uri", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .AssertPassed();
    }

    #endregion

    #region Offline

    [Scenario("Offline mode returns OfflineBlocked and never constructs/calls the client")]
    [Fact]
    public async Task Offline_skips_client()
    {
        var clientFactoryCalled = false;
        var client = new FakeSecretClient("Server=should-not-be-reached;");
        var source = new AzureKeyVaultConnectionStringSource(_ =>
        {
            clientFactoryCalled = true;
            return client;
        });
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "MyConnectionString"
        };

        await Given("an offline context with valid settings", () => BuildContext(settings, offline: true))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is OfflineBlocked", r => r.Outcome == ConnectionStringSourceOutcome.OfflineBlocked)
            .And("the client factory was never invoked", _ => !clientFactoryCalled)
            .And("the client was never called", _ => !client.Called)
            .AssertPassed();
    }

    [Scenario("Misconfigured settings are checked before the offline check")]
    [Fact]
    public async Task Misconfigured_checked_before_offline()
    {
        // Missing settings should surface as Misconfigured even when offline=true, since there's
        // nothing network-related to block yet - this documents evaluation order.
        var source = new AzureKeyVaultConnectionStringSource(_ => new FakeSecretClient());

        await Given("an offline context with no settings", () => BuildContext([], offline: true))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured, not OfflineBlocked", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .AssertPassed();
    }

    #endregion

    #region Exception -> Outcome Mapping

    [Scenario("RequestFailedException with status 404 maps to NotFound")]
    [Fact]
    public async Task Request_failed_404_maps_to_not_found()
    {
        var client = new FakeSecretClient(throwException: new RequestFailedException(404, "Secret not found"));
        var source = new AzureKeyVaultConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "MissingSecret"
        };

        await Given("a context whose client throws a 404", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .AssertPassed();
    }

    [Scenario("RequestFailedException with a non-404 status maps to Failed")]
    [Fact]
    public async Task Request_failed_other_status_maps_to_failed()
    {
        var client = new FakeSecretClient(throwException: new RequestFailedException(403, "Forbidden"));
        var source = new AzureKeyVaultConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "ForbiddenSecret"
        };

        await Given("a context whose client throws a 403", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Failed", r => r.Outcome == ConnectionStringSourceOutcome.Failed)
            .AssertPassed();
    }

    [Scenario("An unexpected exception maps to Failed")]
    [Fact]
    public async Task Unexpected_exception_maps_to_failed()
    {
        var client = new FakeSecretClient(throwException: new InvalidOperationException("kaboom"));
        var source = new AzureKeyVaultConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "MyConnectionString"
        };

        await Given("a context whose client throws an unexpected exception", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Failed", r => r.Outcome == ConnectionStringSourceOutcome.Failed)
            .AssertPassed();
    }

    [Scenario("An empty secret value maps to NotFound")]
    [Fact]
    public async Task Empty_secret_value_maps_to_not_found()
    {
        var client = new FakeSecretClient("   ");
        var source = new AzureKeyVaultConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["keyVaultUri"] = "https://example.vault.azure.net/",
            ["secretName"] = "EmptySecret"
        };

        await Given("a context whose client returns an empty value", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .AssertPassed();
    }

    #endregion

    #region Real Endpoint (skipped by default)

    [SkippableFact]
    [Trait("Category", "Integration")]
    public void Real_key_vault_endpoint_resolves_secret()
    {
        var vaultUri = Environment.GetEnvironmentVariable("EFCPT_TEST_AZURE_KEYVAULT_URI");
        var secretName = Environment.GetEnvironmentVariable("EFCPT_TEST_AZURE_KEYVAULT_SECRET");
        Skip.If(string.IsNullOrWhiteSpace(vaultUri) || string.IsNullOrWhiteSpace(secretName),
            "Set EFCPT_TEST_AZURE_KEYVAULT_URI and EFCPT_TEST_AZURE_KEYVAULT_SECRET to run this test against a real Key Vault.");

        var source = new AzureKeyVaultConnectionStringSource();
        var context = new ConnectionStringSourceContext(
            SourceKey: "azure-keyvault",
            Settings: new Dictionary<string, string> { ["keyVaultUri"] = vaultUri!, ["secretName"] = secretName! },
            Offline: false,
            Log: NullBuildLog.Instance);

        var result = source.Resolve(in context);

        Assert.Equal(ConnectionStringSourceOutcome.Found, result.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(result.ConnectionString));
    }

    #endregion
}
