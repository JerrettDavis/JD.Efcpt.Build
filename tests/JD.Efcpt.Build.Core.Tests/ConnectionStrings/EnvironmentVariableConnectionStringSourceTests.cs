using JD.Efcpt.Build.Core.ConnectionStrings;
using JD.Efcpt.Build.Core.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Core.Tests.ConnectionStrings;

[Feature("EnvironmentVariableConnectionStringSource: resolves a connection string from an environment variable")]
[Collection(nameof(AssemblySetup))]
public sealed class EnvironmentVariableConnectionStringSourceTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(string EnvVarName, string? EnvVarValue, Dictionary<string, string> Settings);

    private static ConnectionStringSourceResult Execute(SetupState setup)
    {
        Environment.SetEnvironmentVariable(setup.EnvVarName, setup.EnvVarValue);
        var source = new EnvironmentVariableConnectionStringSource();
        var context = new ConnectionStringSourceContext(
            SourceKey: source.Key,
            Settings: setup.Settings,
            Offline: false,
            Log: new RecordingBuildLog());
        return source.Resolve(in context);
    }

    private static void CleanUp(SetupState setup)
        => Environment.SetEnvironmentVariable(setup.EnvVarName, null);

    [Scenario("Key and priority are stable")]
    [Fact]
    public async Task Key_and_priority_are_stable()
    {
        var source = new EnvironmentVariableConnectionStringSource();
        await Given("a source instance", () => source)
            .When("read Key/Priority", s => (s.Key, s.Priority))
            .Then("key is 'env'", r => r.Key == "env")
            .And("priority is 0", r => r.Priority == 0)
            .AssertPassed();
    }

    [Scenario("Default environment variable set resolves Found")]
    [Fact]
    public async Task Default_env_var_set_resolves_found()
    {
        await Given("EFCPT_CONNECTION_STRING set to a value", () =>
                new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, "Server=env-host;Database=EnvDb;", []))
            .When("resolve", Execute)
            .Then("outcome is Found", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .And("connection string matches", r => r.ConnectionString == "Server=env-host;Database=EnvDb;")
            .And("source key is 'env'", r => r.SourceKey == "env")
            .Finally(_ => CleanUp(new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, null, [])))
            .AssertPassed();
    }

    [Scenario("Default environment variable unset resolves NotFound")]
    [Fact]
    public async Task Default_env_var_unset_resolves_not_found()
    {
        await Given("EFCPT_CONNECTION_STRING unset", () =>
                new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, null, []))
            .When("resolve", Execute)
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .And("connection string is null", r => r.ConnectionString is null)
            .Finally(_ => CleanUp(new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, null, [])))
            .AssertPassed();
    }

    [Scenario("Default environment variable empty resolves NotFound")]
    [Fact]
    public async Task Default_env_var_empty_resolves_not_found()
    {
        await Given("EFCPT_CONNECTION_STRING set to empty string", () =>
                new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, "", []))
            .When("resolve", Execute)
            .Then("outcome is NotFound", r => r.Outcome == ConnectionStringSourceOutcome.NotFound)
            .Finally(_ => CleanUp(new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, null, [])))
            .AssertPassed();
    }

    [Scenario("Custom envVar setting overrides the variable name read")]
    [Fact]
    public async Task Custom_env_var_setting_overrides_variable_name()
    {
        const string customVar = "MY_CUSTOM_CONNSTR_VAR";
        await Given("a custom envVar setting pointing at a set variable", () =>
                new SetupState(customVar, "Server=custom-host;Database=CustomDb;",
                    new Dictionary<string, string> { [EnvironmentVariableConnectionStringSource.EnvVarSettingKey] = customVar }))
            .When("resolve", Execute)
            .Then("outcome is Found", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .And("connection string matches the custom variable", r => r.ConnectionString == "Server=custom-host;Database=CustomDb;")
            .Finally(_ => CleanUp(new SetupState(customVar, null, [])))
            .AssertPassed();
    }

    [Scenario("Never returns OfflineBlocked, even when offline")]
    [Fact]
    public async Task Never_returns_offline_blocked()
    {
        await Given("offline=true and the variable set", () =>
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, "Server=x;");
                var source = new EnvironmentVariableConnectionStringSource();
                var context = new ConnectionStringSourceContext(
                    SourceKey: source.Key,
                    Settings: new Dictionary<string, string>(),
                    Offline: true,
                    Log: new RecordingBuildLog());
                return (source, context);
            })
            .When("resolve", t => t.source.Resolve(in t.context))
            .Then("outcome is Found, not OfflineBlocked", r => r.Outcome == ConnectionStringSourceOutcome.Found)
            .Finally(_ => CleanUp(new SetupState(EnvironmentVariableConnectionStringSource.DefaultEnvVarName, null, [])))
            .AssertPassed();
    }
}
