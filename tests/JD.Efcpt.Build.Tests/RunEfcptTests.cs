using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the RunEfcpt MSBuild task using fake mode for isolation.
/// </summary>
[Feature("RunEfcpt: invoke efcpt CLI to generate EF Core models")]
[Collection(nameof(AssemblySetup))]
public sealed class RunEfcptTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestFolder Folder,
        string WorkingDir,
        string DacpacPath,
        string ConfigPath,
        string RenamingPath,
        string TemplateDir,
        string OutputDir,
        TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        RunEfcpt Task,
        bool Success);

    private static SetupState SetupForDacpacMode()
    {
        var folder = new TestFolder();
        var workingDir = folder.CreateDir("obj");
        var dacpac = folder.WriteFile("db.dacpac", "DACPAC content");
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
        var templateDir = folder.CreateDir("Templates");
        var outputDir = Path.Combine(folder.Root, "Generated");

        var engine = new TestBuildEngine();
        return new SetupState(folder, workingDir, dacpac, config, renaming, templateDir, outputDir, engine);
    }

    private static SetupState SetupForConnectionStringMode()
    {
        var folder = new TestFolder();
        var workingDir = folder.CreateDir("obj");
        var config = folder.WriteFile("efcpt-config.json", "{}");
        var renaming = folder.WriteFile("efcpt.renaming.json", "[]");
        var templateDir = folder.CreateDir("Templates");
        var outputDir = Path.Combine(folder.Root, "Generated");

        var engine = new TestBuildEngine();
        return new SetupState(folder, workingDir, "", config, renaming, templateDir, outputDir, engine);
    }

    private static SetupState SetupWithToolManifest()
    {
        var setup = SetupForDacpacMode();
        // Create a tool manifest in the working directory
        var configDir = Path.Combine(setup.WorkingDir, ".config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "dotnet-tools.json"), """
            {
                "version": 1,
                "isRoot": true,
                "tools": {
                    "efcpt": {
                        "version": "1.0.0",
                        "commands": ["efcpt"]
                    }
                }
            }
            """);
        return setup;
    }

    private static TaskResult ExecuteTaskWithFakeMode(SetupState setup, Action<RunEfcpt>? configure = null)
    {
        // Set fake mode to avoid running real efcpt
        Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "true");
        try
        {
            var task = new RunEfcpt
            {
                BuildEngine = setup.Engine,
                WorkingDirectory = setup.WorkingDir,
                DacpacPath = setup.DacpacPath,
                ConfigPath = setup.ConfigPath,
                RenamingPath = setup.RenamingPath,
                TemplateDir = setup.TemplateDir,
                OutputDir = setup.OutputDir,
                ToolMode = "auto",
                ToolPackageId = "ErikEJ.EFCorePowerTools.Cli"
            };

            configure?.Invoke(task);
            var success = task.Execute();
            return new TaskResult(setup, task, success);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", null);
        }
    }

    [Scenario("Fake mode creates sample output file")]
    [Fact]
    public async Task Fake_mode_creates_sample_output()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes in fake mode", s => ExecuteTaskWithFakeMode(s))
            .Then("task succeeds", r => r.Success)
            .And("output directory is created", r => Directory.Exists(r.Setup.OutputDir))
            .And("sample model file is created", r =>
                File.Exists(Path.Combine(r.Setup.OutputDir, "SampleModel.cs")))
            .And("sample file references DACPAC", r =>
            {
                var content = File.ReadAllText(Path.Combine(r.Setup.OutputDir, "SampleModel.cs"));
                return content.Contains(r.Setup.DacpacPath);
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates working directory if missing")]
    [Fact]
    public async Task Creates_working_directory()
    {
        await Given("inputs with non-existent working directory", () =>
            {
                var folder = new TestFolder();
                var workingDir = Path.Combine(folder.Root, "new", "working", "dir");
                var dacpac = folder.WriteFile("db.dacpac", "content");
                var config = folder.WriteFile("config.json", "{}");
                var renaming = folder.WriteFile("renaming.json", "[]");
                var templateDir = folder.CreateDir("Templates");
                var outputDir = Path.Combine(folder.Root, "Generated");
                var engine = new TestBuildEngine();
                return new SetupState(folder, workingDir, dacpac, config, renaming, templateDir, outputDir, engine);
            })
            .When("task executes in fake mode", s => ExecuteTaskWithFakeMode(s))
            .Then("task succeeds", r => r.Success)
            .And("working directory is created", r => Directory.Exists(r.Setup.WorkingDir))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates output directory if missing")]
    [Fact]
    public async Task Creates_output_directory()
    {
        await Given("inputs with non-existent output directory", () =>
            {
                var setup = SetupForDacpacMode();
                // Ensure output directory does not exist
                if (Directory.Exists(setup.OutputDir))
                    Directory.Delete(setup.OutputDir, true);
                return setup;
            })
            .When("task executes in fake mode", s => ExecuteTaskWithFakeMode(s))
            .Then("task succeeds", r => r.Success)
            .And("output directory is created", r => Directory.Exists(r.Setup.OutputDir))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fails when DACPAC is missing in DACPAC mode")]
    [Fact]
    public async Task Fails_when_dacpac_missing()
    {
        await Given("inputs with missing DACPAC", () =>
            {
                var setup = SetupForDacpacMode();
                File.Delete(setup.DacpacPath);
                return setup;
            })
            .When("task executes without fake mode", s =>
            {
                // Don't use fake mode so we hit the validation
                var task = new RunEfcpt
                {
                    BuildEngine = s.Engine,
                    WorkingDirectory = s.WorkingDir,
                    DacpacPath = s.DacpacPath,
                    ConfigPath = s.ConfigPath,
                    RenamingPath = s.RenamingPath,
                    TemplateDir = s.TemplateDir,
                    OutputDir = s.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli"
                };
                var success = task.Execute();
                return new TaskResult(s, task, success);
            })
            .Then("task fails", r => !r.Success)
            .And("error is logged", r => r.Setup.Engine.Errors.Count > 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fails when connection string is missing in connection string mode")]
    [Fact]
    public async Task Fails_when_connection_string_missing()
    {
        await Given("inputs for connection string mode without connection string", SetupForConnectionStringMode)
            .When("task executes without fake mode", s =>
            {
                var task = new RunEfcpt
                {
                    BuildEngine = s.Engine,
                    WorkingDirectory = s.WorkingDir,
                    ConnectionString = "", // Missing!
                    UseConnectionStringMode = "true",
                    ConfigPath = s.ConfigPath,
                    RenamingPath = s.RenamingPath,
                    TemplateDir = s.TemplateDir,
                    OutputDir = s.OutputDir,
                    ToolMode = "auto",
                    ToolPackageId = "ErikEJ.EFCorePowerTools.Cli"
                };
                var success = task.Execute();
                return new TaskResult(s, task, success);
            })
            .Then("task fails", r => !r.Success)
            .And("error is logged", r => r.Setup.Engine.Errors.Count > 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Logs execution info with minimal verbosity")]
    [Fact]
    public async Task Logs_execution_info()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with minimal verbosity", s => ExecuteTaskWithFakeMode(s, t => t.LogVerbosity = "minimal"))
            .Then("task succeeds", r => r.Success)
            .And("info message about working directory logged", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("working directory") == true))
            .And("info message about output logged", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("Output") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Logs detailed info when verbosity is detailed")]
    [Fact]
    public async Task Logs_detailed_info()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with detailed verbosity", s => ExecuteTaskWithFakeMode(s, t => t.LogVerbosity = "detailed"))
            .Then("task succeeds", r => r.Success)
            .And("detail message about fake mode logged", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("EFCPT_FAKE_EFCPT") == true))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Discovers tool manifest when present")]
    [Fact]
    public async Task Discovers_tool_manifest()
    {
        await Given("inputs with tool manifest in working directory", SetupWithToolManifest)
            .When("task executes in fake mode", s => ExecuteTaskWithFakeMode(s, t => t.ToolMode = "auto"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses explicit tool path when provided")]
    [Fact]
    public async Task Uses_explicit_tool_path()
    {
        await Given("inputs with explicit tool path", () =>
            {
                var setup = SetupForDacpacMode();
                return setup;
            })
            .When("task executes in fake mode with explicit path", s =>
                ExecuteTaskWithFakeMode(s, t => t.ToolPath = @"C:\tools\efcpt.exe"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles connection string mode")]
    [Fact]
    public async Task Handles_connection_string_mode()
    {
        await Given("inputs for connection string mode", SetupForConnectionStringMode)
            .When("task executes with connection string", s =>
            {
                Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "true");
                try
                {
                    var task = new RunEfcpt
                    {
                        BuildEngine = s.Engine,
                        WorkingDirectory = s.WorkingDir,
                        ConnectionString = "Server=localhost;Database=TestDb",
                        UseConnectionStringMode = "true",
                        ConfigPath = s.ConfigPath,
                        RenamingPath = s.RenamingPath,
                        TemplateDir = s.TemplateDir,
                        OutputDir = s.OutputDir,
                        ToolMode = "auto",
                        ToolPackageId = "ErikEJ.EFCorePowerTools.Cli"
                    };
                    var success = task.Execute();
                    return new TaskResult(s, task, success);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", null);
                }
            })
            .Then("task succeeds", r => r.Success)
            .And("output file is created", r =>
                File.Exists(Path.Combine(r.Setup.OutputDir, "SampleModel.cs")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Respects ToolRestore setting")]
    [Fact]
    public async Task Respects_tool_restore_setting()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with ToolRestore false", s =>
                ExecuteTaskWithFakeMode(s, t => t.ToolRestore = "false"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles provider setting")]
    [Fact]
    public async Task Handles_provider_setting()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with custom provider", s =>
                ExecuteTaskWithFakeMode(s, t => t.Provider = "postgresql"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles tool version constraint")]
    [Fact]
    public async Task Handles_tool_version_constraint()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with tool version", s =>
                ExecuteTaskWithFakeMode(s, t => t.ToolVersion = "1.2.3"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles custom tool command")]
    [Fact]
    public async Task Handles_custom_tool_command()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with custom tool command", s =>
                ExecuteTaskWithFakeMode(s, t => t.ToolCommand = "custom-efcpt"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Tool manifest mode works")]
    [Fact]
    public async Task Tool_manifest_mode_works()
    {
        await Given("inputs with tool manifest", SetupWithToolManifest)
            .When("task executes with tool-manifest mode", s =>
                ExecuteTaskWithFakeMode(s, t => t.ToolMode = "tool-manifest"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Accepts target framework parameter")]
    [Fact]
    public async Task Accepts_target_framework_parameter()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with target framework", s =>
                ExecuteTaskWithFakeMode(s, t => t.TargetFramework = "net10.0"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles various target framework formats")]
    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net10.0-windows")]
    [InlineData("net10-windows")]
    [InlineData("")]
    public async Task Handles_various_target_framework_formats(string targetFramework)
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with target framework", s =>
                ExecuteTaskWithFakeMode(s, t => t.TargetFramework = targetFramework))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
