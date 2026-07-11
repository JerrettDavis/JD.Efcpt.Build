using System.Text.Json.Nodes;
using JD.Efcpt.Cli.Commands;
using JD.Efcpt.Cli.Logging;
using JD.Efcpt.Cli.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="InitCommand"/>, driven directly via <see cref="InitCommand.ExecuteAsync"/>
/// (no <c>System.CommandLine</c> argument parsing, no subprocess) so these run fast and
/// deterministically. The default (offline) path reads the schema bundled next to this test
/// assembly's own output (see the csproj's None item mirroring JD.Efcpt.Cli's bundling) - no
/// network access occurs.
/// </summary>
[Feature("jd-efcpt init: bootstraps efcpt-config.json offline-first")]
[Collection(nameof(AssemblySetup))]
public sealed partial class InitCommandTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record InitResult(TestFolder Folder, int ExitCode, string ConfigPath);

    private static async Task<InitResult> RunInit(
        TestFolder folder,
        string? provider = null,
        string? dbContextName = null,
        string? rootNamespace = null,
        bool force = false,
        bool online = false)
    {
        var exitCode = await InitCommand.ExecuteAsync(
            new ConsoleBuildLog(),
            folder.Root,
            provider,
            dbContextName,
            rootNamespace,
            force,
            online);

        return new InitResult(folder, exitCode, Path.Combine(folder.Root, "efcpt-config.json"));
    }

    [Scenario("Offline init writes a valid efcpt-config.json from the bundled schema")]
    [Fact]
    public async Task Offline_init_writes_valid_config()
    {
        await Given("an empty output directory", () => new TestFolder())
            .When("init runs offline with default options", f => RunInit(f))
            .Then("exit code is 0", r => r.ExitCode == 0)
            .And("efcpt-config.json was written", r => File.Exists(r.ConfigPath))
            .And("the written file is valid JSON with a $schema property", r =>
            {
                var json = JsonNode.Parse(File.ReadAllText(r.ConfigPath));
                return json?["$schema"] is not null;
            })
            .And("code-generation, names, and file-layout sections are present", r =>
            {
                var json = JsonNode.Parse(File.ReadAllText(r.ConfigPath));
                return json?["code-generation"] is not null
                    && json["names"] is not null
                    && json["file-layout"] is not null;
            })
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Offline init applies --dbcontext-name and --namespace")]
    [Fact]
    public async Task Offline_init_applies_custom_names()
    {
        await Given("an empty output directory", () => new TestFolder())
            .When("init runs with custom dbcontext-name/namespace", f => RunInit(f, dbContextName: "MyDbContext", rootNamespace: "MyApp.Data"))
            .Then("exit code is 0", r => r.ExitCode == 0)
            .And("the config uses the custom DbContext name", r =>
            {
                var json = JsonNode.Parse(File.ReadAllText(r.ConfigPath));
                return json?["names"]?["dbcontext-name"]?.GetValue<string>() == "MyDbContext";
            })
            .And("the config uses the custom root namespace", r =>
            {
                var json = JsonNode.Parse(File.ReadAllText(r.ConfigPath));
                return json?["names"]?["root-namespace"]?.GetValue<string>() == "MyApp.Data";
            })
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Init refuses to overwrite an existing config without --force")]
    [Fact]
    public async Task Init_refuses_overwrite_without_force()
    {
        await Given("an output directory with an existing efcpt-config.json", () =>
            {
                var folder = new TestFolder();
                File.WriteAllText(Path.Combine(folder.Root, "efcpt-config.json"), "{\"marker\":\"original\"}");
                return folder;
            })
            .When("init runs without --force", f => RunInit(f))
            .Then("exit code is 1", r => r.ExitCode == 1)
            .And("the original file is untouched", r => File.ReadAllText(r.ConfigPath).Contains("original"))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Init overwrites an existing config with --force")]
    [Fact]
    public async Task Init_overwrites_with_force()
    {
        await Given("an output directory with an existing efcpt-config.json", () =>
            {
                var folder = new TestFolder();
                File.WriteAllText(Path.Combine(folder.Root, "efcpt-config.json"), "{\"marker\":\"original\"}");
                return folder;
            })
            .When("init runs with --force", f => RunInit(f, force: true))
            .Then("exit code is 0", r => r.ExitCode == 0)
            .And("the file was regenerated (marker gone)", r => !File.ReadAllText(r.ConfigPath).Contains("original"))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Init accepts a recognized --provider alias")]
    [Fact]
    public async Task Init_accepts_recognized_provider()
    {
        await Given("an empty output directory", () => new TestFolder())
            .When("init runs with a recognized provider alias", f => RunInit(f, provider: "postgresql"))
            .Then("exit code is 0", r => r.ExitCode == 0)
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Init rejects an unrecognized --provider")]
    [Fact]
    public async Task Init_rejects_unrecognized_provider()
    {
        await Given("an empty output directory", () => new TestFolder())
            .When("init runs with an unrecognized provider", f => RunInit(f, provider: "cockroachdb"))
            .Then("exit code is 1", r => r.ExitCode == 1)
            .And("no config file was written", r => !File.Exists(r.ConfigPath))
            .Finally(r => r.Folder.Dispose())
            .AssertPassed();
    }
}
