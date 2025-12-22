using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the ComputeFingerprint MSBuild task.
/// </summary>
[Feature("ComputeFingerprint: deterministic XxHash64-based fingerprinting for incremental builds")]
[Collection(nameof(AssemblySetup))]
public sealed class ComputeFingerprintTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestFolder Folder,
        string DacpacPath,
        string ConfigPath,
        string RenamingPath,
        string TemplateDir,
        string FingerprintFile,
        TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        ComputeFingerprint Task,
        bool Success);

    private static SetupState SetupWithAllInputs()
    {
        var folder = new TestFolder();
        var dacpac = folder.WriteFile("db.dacpac", "DACPAC content v1");
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
        var templateDir = folder.CreateDir("Templates");
        folder.WriteFile("Templates/Entity.t4", "Entity template");
        folder.WriteFile("Templates/Context.t4", "Context template");
        var fingerprintFile = Path.Combine(folder.Root, "fingerprint.txt");

        var engine = new TestBuildEngine();
        return new SetupState(folder, dacpac, config, renaming, templateDir, fingerprintFile, engine);
    }

    private static SetupState SetupWithNoFingerprintFile()
    {
        var folder = new TestFolder();
        var dacpac = folder.WriteFile("db.dacpac", "DACPAC content");
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
        var templateDir = folder.CreateDir("Templates");
        folder.WriteFile("Templates/Entity.t4", "template");
        var fingerprintFile = Path.Combine(folder.Root, "fingerprint.txt");

        var engine = new TestBuildEngine();
        return new SetupState(folder, dacpac, config, renaming, templateDir, fingerprintFile, engine);
    }

    private static SetupState SetupWithExistingFingerprintFile()
    {
        var setup = SetupWithAllInputs();
        // Pre-compute and write the fingerprint
        var task = new ComputeFingerprint
        {
            BuildEngine = setup.Engine,
            DacpacPath = setup.DacpacPath,
            ConfigPath = setup.ConfigPath,
            RenamingPath = setup.RenamingPath,
            TemplateDir = setup.TemplateDir,
            FingerprintFile = setup.FingerprintFile
        };
        task.Execute();
        return setup;
    }

    private static SetupState SetupForConnectionStringMode()
    {
        var folder = new TestFolder();
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
        var templateDir = folder.CreateDir("Templates");
        folder.WriteFile("Templates/Entity.t4", "template");
        var fingerprintFile = Path.Combine(folder.Root, "fingerprint.txt");

        var engine = new TestBuildEngine();
        return new SetupState(folder, "", config, renaming, templateDir, fingerprintFile, engine);
    }

    private static TaskResult ExecuteTask(SetupState setup, string? schemaFingerprint = null, bool useConnectionStringMode = false)
    {
        var task = new ComputeFingerprint
        {
            BuildEngine = setup.Engine,
            DacpacPath = setup.DacpacPath,
            ConfigPath = setup.ConfigPath,
            RenamingPath = setup.RenamingPath,
            TemplateDir = setup.TemplateDir,
            FingerprintFile = setup.FingerprintFile,
            SchemaFingerprint = schemaFingerprint ?? "",
            UseConnectionStringMode = useConnectionStringMode ? "true" : "false"
        };

        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    [Scenario("Computes fingerprint and sets HasChanged to true on first run")]
    [Fact]
    public async Task First_run_sets_has_changed_true()
    {
        await Given("inputs with no existing fingerprint", SetupWithNoFingerprintFile)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("fingerprint is computed", r => !string.IsNullOrEmpty(r.Task.Fingerprint))
            .And("fingerprint is 16 characters", r => r.Task.Fingerprint.Length == 16)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .And("fingerprint file is created", r => File.Exists(r.Setup.FingerprintFile))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HasChanged is false when fingerprint matches cached value")]
    [Fact]
    public async Task No_change_when_fingerprint_matches()
    {
        await Given("inputs with existing fingerprint file", SetupWithExistingFingerprintFile)
            .When("task executes again", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is false", r => r.Task.HasChanged == "false")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HasChanged is true when DACPAC content changes")]
    [Fact]
    public async Task Dacpac_change_triggers_fingerprint_change()
    {
        await Given("inputs with existing fingerprint", SetupWithExistingFingerprintFile)
            .When("DACPAC is modified and task executes", s =>
            {
                File.WriteAllText(s.DacpacPath, "DACPAC content v2 - modified!");
                return ExecuteTask(s);
            })
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HasChanged is true when config changes")]
    [Fact]
    public async Task Config_change_triggers_fingerprint_change()
    {
        await Given("inputs with existing fingerprint", SetupWithExistingFingerprintFile)
            .When("config is modified and task executes", s =>
            {
                File.WriteAllText(s.ConfigPath, "{ \"modified\": true }");
                return ExecuteTask(s);
            })
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HasChanged is true when renaming file changes")]
    [Fact]
    public async Task Renaming_change_triggers_fingerprint_change()
    {
        await Given("inputs with existing fingerprint", SetupWithExistingFingerprintFile)
            .When("renaming file is modified and task executes", s =>
            {
                File.WriteAllText(s.RenamingPath, "[{ \"modified\": true }]");
                return ExecuteTask(s);
            })
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HasChanged is true when template file changes")]
    [Fact]
    public async Task Template_change_triggers_fingerprint_change()
    {
        await Given("inputs with existing fingerprint", SetupWithExistingFingerprintFile)
            .When("template file is modified and task executes", s =>
            {
                File.WriteAllText(Path.Combine(s.TemplateDir, "Entity.t4"), "Modified template content");
                return ExecuteTask(s);
            })
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HasChanged is true when new template file is added")]
    [Fact]
    public async Task New_template_triggers_fingerprint_change()
    {
        await Given("inputs with existing fingerprint", SetupWithExistingFingerprintFile)
            .When("new template file is added and task executes", s =>
            {
                File.WriteAllText(Path.Combine(s.TemplateDir, "NewTemplate.t4"), "New template");
                return ExecuteTask(s);
            })
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses schema fingerprint in connection string mode")]
    [Fact]
    public async Task Uses_schema_fingerprint_in_connection_string_mode()
    {
        await Given("inputs for connection string mode", SetupForConnectionStringMode)
            .When("task executes with schema fingerprint", s => ExecuteTask(s, schemaFingerprint: "abc123", useConnectionStringMode: true))
            .Then("task succeeds", r => r.Success)
            .And("fingerprint is computed", r => !string.IsNullOrEmpty(r.Task.Fingerprint))
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Schema fingerprint change triggers HasChanged in connection string mode")]
    [Fact]
    public async Task Schema_fingerprint_change_triggers_change()
    {
        await Given("inputs with existing schema-based fingerprint", () =>
            {
                var setup = SetupForConnectionStringMode();
                // First run with schema fingerprint
                var task = new ComputeFingerprint
                {
                    BuildEngine = setup.Engine,
                    ConfigPath = setup.ConfigPath,
                    RenamingPath = setup.RenamingPath,
                    TemplateDir = setup.TemplateDir,
                    FingerprintFile = setup.FingerprintFile,
                    SchemaFingerprint = "schema-v1",
                    UseConnectionStringMode = "true"
                };
                task.Execute();
                return setup;
            })
            .When("task executes with different schema fingerprint", s =>
                ExecuteTask(s, schemaFingerprint: "schema-v2", useConnectionStringMode: true))
            .Then("task succeeds", r => r.Success)
            .And("HasChanged is true", r => r.Task.HasChanged == "true")
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint is deterministic")]
    [Fact]
    public async Task Fingerprint_is_deterministic()
    {
        await Given("inputs for fingerprinting", SetupWithAllInputs)
            .When("task executes twice", s =>
            {
                var firstRun = ExecuteTask(s);
                var firstFingerprint = firstRun.Task.Fingerprint;

                // Delete fingerprint file to force recomputation
                File.Delete(s.FingerprintFile);

                var secondRun = ExecuteTask(s);
                var secondFingerprint = secondRun.Task.Fingerprint;

                return (firstFingerprint, secondFingerprint, s.Folder);
            })
            .Then("fingerprints match", t => t.firstFingerprint == t.secondFingerprint)
            .Finally(t => t.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles missing DACPAC gracefully in DACPAC mode")]
    [Fact]
    public async Task Handles_missing_dacpac()
    {
        await Given("inputs with missing DACPAC", () =>
            {
                var setup = SetupWithAllInputs();
                File.Delete(setup.DacpacPath);
                return setup;
            })
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("fingerprint is computed (without DACPAC)", r => !string.IsNullOrEmpty(r.Task.Fingerprint))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates fingerprint file directory if needed")]
    [Fact]
    public async Task Creates_fingerprint_directory()
    {
        await Given("inputs with nested fingerprint path", () =>
            {
                var folder = new TestFolder();
                var dacpac = folder.WriteFile("db.dacpac", "content");
                var config = folder.WriteFile("efcpt-config.json", "{}");
                var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
                var templateDir = folder.CreateDir("Templates");
                folder.WriteFile("Templates/Entity.t4", "template");
                var fingerprintFile = Path.Combine(folder.Root, "nested", "dir", "fingerprint.txt");
                var engine = new TestBuildEngine();
                return new SetupState(folder, dacpac, config, renaming, templateDir, fingerprintFile, engine);
            })
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("fingerprint file is created in nested directory", r => File.Exists(r.Setup.FingerprintFile))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Includes all template files in nested directories")]
    [Fact]
    public async Task Includes_nested_template_files()
    {
        await Given("templates with nested structure", () =>
            {
                var folder = new TestFolder();
                var dacpac = folder.WriteFile("db.dacpac", "content");
                var config = folder.WriteFile("efcpt-config.json", "{}");
                var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
                var templateDir = folder.CreateDir("Templates");
                folder.WriteFile("Templates/Entity.t4", "entity");
                folder.CreateDir("Templates/SubDir");
                folder.WriteFile("Templates/SubDir/Nested.t4", "nested");
                var fingerprintFile = Path.Combine(folder.Root, "fingerprint.txt");
                var engine = new TestBuildEngine();
                return new SetupState(folder, dacpac, config, renaming, templateDir, fingerprintFile, engine);
            })
            .When("task executes and nested template is modified", s =>
            {
                var firstRun = ExecuteTask(s);
                var firstFingerprint = firstRun.Task.Fingerprint;

                // Modify nested template
                File.WriteAllText(Path.Combine(s.TemplateDir, "SubDir", "Nested.t4"), "modified nested");

                var secondRun = ExecuteTask(s);
                var secondFingerprint = secondRun.Task.Fingerprint;

                return (changed: firstFingerprint != secondFingerprint, folder: s.Folder);
            })
            .Then("fingerprint changes when nested template changes", t => t.changed)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Logs fingerprint change with info level")]
    [Fact]
    public async Task Logs_fingerprint_change()
    {
        await Given("inputs with no existing fingerprint", SetupWithNoFingerprintFile)
            .When("task executes", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("info message logged about fingerprint change", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("fingerprint changed") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Logs skip message when fingerprint unchanged")]
    [Fact]
    public async Task Logs_skip_when_unchanged()
    {
        await Given("inputs with existing fingerprint", SetupWithExistingFingerprintFile)
            .When("task executes again", s => ExecuteTask(s))
            .Then("task succeeds", r => r.Success)
            .And("info message logged about skipping", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("skipping") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
