using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
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

    [Scenario("EfcptCustomProviders and AllowCustomProviders default sensibly when not set")]
    [Fact]
    public async Task Custom_provider_inputs_default_sensibly()
    {
        await Given("a new QuerySchemaMetadata task", () => new QuerySchemaMetadata())
            .When("reading the custom provider inputs", task => (task.EfcptCustomProviders, task.AllowCustomProviders))
            .Then("EfcptCustomProviders is an empty array, not null", t => t.EfcptCustomProviders is { Length: 0 })
            .And("AllowCustomProviders is false", t => t.AllowCustomProviders == false)
            .AssertPassed();
    }

    #region Security Gate Tests (#184)

    private sealed record ExecResult(QuerySchemaMetadata Task, TestBuildEngine Engine, bool Success, string OutputDir) : IDisposable
    {
        public void Dispose()
        {
            if (Directory.Exists(OutputDir))
                Directory.Delete(OutputDir, recursive: true);
        }
    }

    private static ExecResult ExecuteWithCustomProvider(
        string provider,
        Microsoft.Build.Framework.ITaskItem[] customProviders,
        bool allowCustomProviders,
        string connectionString = "unused")
    {
        var engine = new TestBuildEngine();
        var outputDir = Path.Combine(Path.GetTempPath(), $"efcpt-custom-provider-task-test-{Guid.NewGuid()}");

        var task = new QuerySchemaMetadata
        {
            BuildEngine = engine,
            ConnectionString = connectionString,
            OutputDir = outputDir,
            Provider = provider,
            EfcptCustomProviders = customProviders,
            AllowCustomProviders = allowCustomProviders,
            LogVerbosity = "minimal"
        };

        var success = task.Execute();
        return new ExecResult(task, engine, success, outputDir);
    }

    [Scenario("A registered custom provider key without the security opt-in fails the task with JD0017")]
    [Fact]
    public async Task Custom_provider_without_opt_in_fails_jd0017()
    {
        await Given("a registered custom provider key selected as Provider, but AllowCustomProviders not set", () =>
                ExecuteWithCustomProvider(
                    "acme-mongo",
                    [new Microsoft.Build.Utilities.TaskItem("acme-mongo", new Dictionary<string, string> { ["AssemblyName"] = "Acme.Efcpt.Mongo" })],
                    allowCustomProviders: false))
            .Then("Execute() returns false", r => r.Success == false)
            .And("JD0017 is logged", r => r.Engine.Errors.Any(e => e.Code == "JD0017"))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    [Scenario("A custom provider key colliding with a built-in provider alias fails the task with JD0019, regardless of the opt-in")]
    [Fact]
    public async Task Custom_provider_collision_fails_jd0019()
    {
        await Given("a custom provider item whose identity collides with a built-in provider alias", () =>
                ExecuteWithCustomProvider(
                    "mssql",
                    [new Microsoft.Build.Utilities.TaskItem("postgres", new Dictionary<string, string> { ["AssemblyName"] = "Acme.Efcpt.Postgres" })],
                    allowCustomProviders: true))
            .Then("Execute() returns false", r => r.Success == false)
            .And("JD0019 is logged", r => r.Engine.Errors.Any(e => e.Code == "JD0019"))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    [Scenario("An enabled, valid custom provider succeeds and logs the build-time-code-execution warning")]
    [Fact]
    public async Task Enabled_valid_custom_provider_succeeds_with_warning()
    {
        // Reuses the real JD.Efcpt.Build.Sqlite satellite adapter (already deployed next to this
        // test assembly via a test-only ProjectReference - see DatabaseProviderFactoryTests'
        // SqliteSearchPaths remarks) under an arbitrary custom key, so this exercises a genuine
        // end-to-end success (a real in-memory SQLite connection) rather than a fixture that
        // merely proves reflection loading works.
        var testAssemblyDir = Path.GetDirectoryName(typeof(QuerySchemaMetadataTests).Assembly.Location)!;

        await Given("a registered, opted-in custom provider key backed by a real, working adapter", () =>
                ExecuteWithCustomProvider(
                    "acme-sqlite",
                    [new Microsoft.Build.Utilities.TaskItem("acme-sqlite", new Dictionary<string, string>
                    {
                        ["AssemblyName"] = "JD.Efcpt.Build.Sqlite",
                        ["SearchPath"] = testAssemblyDir
                    })],
                    allowCustomProviders: true,
                    connectionString: "Data Source=:memory:"))
            .Then("Execute() returns true", r => r.Success)
            .And("no errors are logged", r => r.Engine.Errors.Count == 0)
            .And("the build-time-code-execution warning is logged", r =>
                r.Engine.Warnings.Any(w => w.Message?.Contains("custom provider", StringComparison.OrdinalIgnoreCase) == true))
            .And("the schema fingerprint was computed", r => !string.IsNullOrEmpty(r.Task.SchemaFingerprint))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    [Scenario("A custom provider item with blank AssemblyName metadata fails the task with JD0041")]
    [Fact]
    public async Task Custom_provider_blank_assembly_name_fails_jd0041()
    {
        await Given("a registered custom provider item missing AssemblyName metadata", () =>
                ExecuteWithCustomProvider(
                    "mssql", // a valid, selected built-in provider - proves validation is unconditional
                    [new Microsoft.Build.Utilities.TaskItem("acme-mongo")],
                    allowCustomProviders: true))
            .Then("Execute() returns false", r => r.Success == false)
            .And("JD0041 is logged", r => r.Engine.Errors.Any(e => e.Code == "JD0041"))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    [Scenario("A custom provider item with a blank provider key fails the task with JD0041")]
    [Fact]
    public async Task Custom_provider_blank_key_fails_jd0041()
    {
        await Given("a registered custom provider item with a blank/whitespace key", () =>
                ExecuteWithCustomProvider(
                    "mssql",
                    [new Microsoft.Build.Utilities.TaskItem("   ", new Dictionary<string, string> { ["AssemblyName"] = "Acme.Efcpt.Mongo" })],
                    allowCustomProviders: true))
            .Then("Execute() returns false", r => r.Success == false)
            .And("JD0041 is logged", r => r.Engine.Errors.Any(e => e.Code == "JD0041"))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    [Scenario("A duplicate custom provider key fails the task with JD0041")]
    [Fact]
    public async Task Custom_provider_duplicate_key_fails_jd0041()
    {
        await Given("two registered custom provider items sharing the same key", () =>
                ExecuteWithCustomProvider(
                    "mssql",
                    [
                        new Microsoft.Build.Utilities.TaskItem("acme-mongo", new Dictionary<string, string> { ["AssemblyName"] = "Acme.Efcpt.Mongo" }),
                        new Microsoft.Build.Utilities.TaskItem("acme-mongo", new Dictionary<string, string> { ["AssemblyName"] = "Acme.Efcpt.Mongo.Other" })
                    ],
                    allowCustomProviders: true))
            .Then("Execute() returns false", r => r.Success == false)
            .And("JD0041 is logged", r => r.Engine.Errors.Any(e => e.Code == "JD0041"))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    [Scenario("A well-formed custom provider registration passes JD0041 validation (built-in provider selected)")]
    [Fact]
    public async Task Wellformed_custom_provider_passes_validation()
    {
        // A well-formed (key + AssemblyName), non-duplicate custom provider registration must NOT
        // trip JD0041 - and since the selected Provider here is the built-in mssql, the task
        // proceeds past validation without any custom-provider error code.
        await Given("a well-formed custom provider item, with a built-in provider selected", () =>
                ExecuteWithCustomProvider(
                    "mssql",
                    [new Microsoft.Build.Utilities.TaskItem("acme-mongo", new Dictionary<string, string> { ["AssemblyName"] = "Acme.Efcpt.Mongo" })],
                    allowCustomProviders: true))
            .Then("no JD0041 is logged", r => !r.Engine.Errors.Any(e => e.Code == "JD0041"))
            .And("no JD0019 collision is logged", r => !r.Engine.Errors.Any(e => e.Code == "JD0019"))
            .And("no JD0017 opt-in error is logged", r => !r.Engine.Errors.Any(e => e.Code == "JD0017"))
            .Finally(r => r.Dispose())
            .AssertPassed();
    }

    #endregion
}
