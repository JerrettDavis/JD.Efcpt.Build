using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager.Model;
using JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager;
using JD.Efcpt.Build.Core.ConnectionStrings;
using JD.Efcpt.Build.Core.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager.Tests;

[Feature("AwsSecretsManagerConnectionStringSource: resolves connection strings from AWS Secrets Manager secrets")]
public sealed class AwsSecretsManagerConnectionStringSourceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    /// <summary>A fake <see cref="ISecretsManagerClient"/> that records the request and returns a scripted response/exception.</summary>
    private sealed class FakeSecretsManagerClient(string? value = null, Exception? throwException = null) : ISecretsManagerClient
    {
        public bool Called { get; private set; }
        public string? RequestedSecretId { get; private set; }

        public string GetSecretValue(string secretId, CancellationToken cancellationToken)
        {
            Called = true;
            RequestedSecretId = secretId;

            if (throwException is not null)
                throw throwException;

            return value ?? "";
        }
    }

    private static ConnectionStringSourceContext BuildContext(Dictionary<string, string> settings, bool offline = false)
        => new(SourceKey: "aws-secrets", Settings: settings, Offline: offline, Log: NullBuildLog.Instance);

    #region Key / Priority

    [Scenario("Key and priority are stable")]
    [Fact]
    public async Task Key_and_priority_are_stable()
    {
        await Given("a source", () => new AwsSecretsManagerConnectionStringSource())
            .When("read Key/Priority", s => (s.Key, s.Priority))
            .Then("key is 'aws-secrets'", r => r.Key == "aws-secrets")
            .AssertPassed();
    }

    #endregion

    #region Settings -> Request Mapping

    [Scenario("Maps secretId/region settings to the client request")]
    [Fact]
    public async Task Maps_settings_to_request()
    {
        var client = new FakeSecretsManagerClient("Server=aws-host;Database=AwsDb;");
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/connection-string",
            ["region"] = "us-east-1"
        };

        await Given("a context with secretId and region", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Found", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .And("connection string is returned", r => r.ConnectionString == "Server=aws-host;Database=AwsDb;")
            .And("client received the secret id", _ => client.RequestedSecretId == "my/connection-string")
            .AssertPassed();
    }

    [Scenario("Extracts a JSON field when secretJsonKey is configured")]
    [Fact]
    public async Task Extracts_json_field_when_configured()
    {
        var client = new FakeSecretsManagerClient("""{"connectionString":"Server=json-host;Database=JsonDb;","username":"admin"}""");
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/json-secret",
            ["region"] = "us-east-1",
            ["secretJsonKey"] = "connectionString"
        };

        await Given("a context with secretJsonKey", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Found", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .And("the extracted field is returned", r => r.ConnectionString == "Server=json-host;Database=JsonDb;")
            .AssertPassed();
    }

    [Scenario("Missing JSON field with secretJsonKey configured is NotFound")]
    [Fact]
    public async Task Missing_json_field_is_not_found()
    {
        var client = new FakeSecretsManagerClient("""{"username":"admin"}""");
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/json-secret",
            ["region"] = "us-east-1",
            ["secretJsonKey"] = "connectionString"
        };

        await Given("a context whose secret JSON lacks the configured key", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .AssertPassed();
    }

    [Scenario("Non-JSON secret with secretJsonKey configured is Failed")]
    [Fact]
    public async Task Non_json_secret_with_json_key_is_failed()
    {
        var client = new FakeSecretsManagerClient("not-json-at-all");
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/plain-secret",
            ["region"] = "us-east-1",
            ["secretJsonKey"] = "connectionString"
        };

        await Given("a context whose secret is not JSON", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Failed", r => r.Outcome == ConnectionStringSourceOutcome.Failed)
            .AssertPassed();
    }

    #endregion

    #region Misconfigured

    [Scenario("Missing secretId is Misconfigured")]
    [Fact]
    public async Task Missing_secret_id_is_misconfigured()
    {
        var source = new AwsSecretsManagerConnectionStringSource(_ => new FakeSecretsManagerClient());
        var settings = new Dictionary<string, string> { ["region"] = "us-east-1" };

        await Given("a context missing secretId", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .And("diagnostic mentions secretId", r => r.Diagnostic != null && r.Diagnostic.Contains("secretId"))
            .AssertPassed();
    }

    [Scenario("Missing region is Misconfigured")]
    [Fact]
    public async Task Missing_region_is_misconfigured()
    {
        var source = new AwsSecretsManagerConnectionStringSource(_ => new FakeSecretsManagerClient());
        var settings = new Dictionary<string, string> { ["secretId"] = "my/secret" };

        await Given("a context missing region", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .And("diagnostic mentions region", r => r.Diagnostic != null && r.Diagnostic.Contains("region"))
            .AssertPassed();
    }

    #endregion

    #region Offline

    [Scenario("Offline mode returns OfflineBlocked and never constructs/calls the client")]
    [Fact]
    public async Task Offline_skips_client()
    {
        var clientFactoryCalled = false;
        var client = new FakeSecretsManagerClient("Server=should-not-be-reached;");
        var source = new AwsSecretsManagerConnectionStringSource(_ =>
        {
            clientFactoryCalled = true;
            return client;
        });
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/secret",
            ["region"] = "us-east-1"
        };

        await Given("an offline context with valid settings", () => BuildContext(settings, offline: true))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is OfflineBlocked", r => r.Outcome == ConnectionStringSourceOutcome.OfflineBlocked)
            .And("the client factory was never invoked", _ => !clientFactoryCalled)
            .And("the client was never called", _ => !client.Called)
            .AssertPassed();
    }

    #endregion

    #region Exception -> Outcome Mapping

    [Scenario("ResourceNotFoundException maps to NotFound")]
    [Fact]
    public async Task Resource_not_found_maps_to_not_found()
    {
        var client = new FakeSecretsManagerClient(throwException: new ResourceNotFoundException("Secrets Manager can't find the specified secret."));
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/missing-secret",
            ["region"] = "us-east-1"
        };

        await Given("a context whose client throws ResourceNotFoundException", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .AssertPassed();
    }

    [Scenario("AmazonServiceException maps to Failed")]
    [Fact]
    public async Task Amazon_service_exception_maps_to_failed()
    {
        var client = new FakeSecretsManagerClient(throwException: new AmazonServiceException("Access denied"));
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/forbidden-secret",
            ["region"] = "us-east-1"
        };

        await Given("a context whose client throws AmazonServiceException", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Failed", r => r.Outcome == ConnectionStringSourceOutcome.Failed)
            .AssertPassed();
    }

    [Scenario("NotSupportedException (binary secret) maps to Misconfigured")]
    [Fact]
    public async Task Not_supported_exception_maps_to_misconfigured()
    {
        var client = new FakeSecretsManagerClient(throwException: new NotSupportedException(
            "Secret 'my/binary-secret' is stored as a binary value (SecretBinary). Connection strings must be stored as a plain-text SecretString."));
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/binary-secret",
            ["region"] = "us-east-1"
        };

        await Given("a context whose client throws NotSupportedException for a binary secret", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is Misconfigured", r => r.Outcome == ConnectionStringSourceOutcome.Misconfigured)
            .And("diagnostic mentions binary", r => r.Diagnostic != null && r.Diagnostic.Contains("binary", StringComparison.OrdinalIgnoreCase))
            .AssertPassed();
    }

    [Scenario("An unexpected exception maps to Failed")]
    [Fact]
    public async Task Unexpected_exception_maps_to_failed()
    {
        var client = new FakeSecretsManagerClient(throwException: new InvalidOperationException("kaboom"));
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/secret",
            ["region"] = "us-east-1"
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
        var client = new FakeSecretsManagerClient("   ");
        var source = new AwsSecretsManagerConnectionStringSource(_ => client);
        var settings = new Dictionary<string, string>
        {
            ["secretId"] = "my/empty-secret",
            ["region"] = "us-east-1"
        };

        await Given("a context whose client returns an empty value", () => BuildContext(settings))
            .When("resolve", ctx => source.Resolve(in ctx))
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .AssertPassed();
    }

    #endregion

    #region Real Endpoint (skipped by default)

    [SkippableFact]
    public void Real_secrets_manager_endpoint_resolves_secret()
    {
        var secretId = Environment.GetEnvironmentVariable("EFCPT_TEST_AWS_SECRET_ID");
        var region = Environment.GetEnvironmentVariable("EFCPT_TEST_AWS_REGION");
        Skip.If(string.IsNullOrWhiteSpace(secretId) || string.IsNullOrWhiteSpace(region),
            "Set EFCPT_TEST_AWS_SECRET_ID and EFCPT_TEST_AWS_REGION to run this test against a real AWS account.");

        var source = new AwsSecretsManagerConnectionStringSource();
        var context = new ConnectionStringSourceContext(
            SourceKey: "aws-secrets",
            Settings: new Dictionary<string, string> { ["secretId"] = secretId!, ["region"] = region! },
            Offline: false,
            Log: NullBuildLog.Instance);

        var result = source.Resolve(in context);

        Assert.Equal(ConnectionStringSourceOutcome.Found, result.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(result.ConnectionString));
    }

    #endregion
}
