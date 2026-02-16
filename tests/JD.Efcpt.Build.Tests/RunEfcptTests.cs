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
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("working directory") ?? false))
            .And("info message about output logged", r =>
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("Output") ?? false))
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
                r.Setup.Engine.Messages.Any(m => m.Message?.Contains("EFCPT_FAKE_EFCPT") ?? false))
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

    [Scenario("Resolves relative tool path correctly")]
    [Fact]
    public async Task Resolves_relative_tool_path()
    {
        await Given("inputs with relative tool path", () =>
            {
                var setup = SetupForDacpacMode();
                // Create a fake tool in a subdirectory
                var toolDir = setup.Folder.CreateDir("tools");
                var toolPath = Path.Combine(toolDir, "fake-efcpt.exe");
                File.WriteAllText(toolPath, "fake tool");
                return (setup, toolPath: Path.Combine("tools", "fake-efcpt.exe"));
            })
            .When("task executes with relative path", ctx =>
                ExecuteTaskWithFakeMode(ctx.setup, t => t.ToolPath = ctx.toolPath))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Logs error when explicit tool path does not exist")]
    [Fact]
    public async Task Explicit_tool_path_not_exists_logs_error()
    {
        await Given("inputs with non-existent tool path", SetupForDacpacMode)
            .When("task executes without fake mode", s =>
            {
                // Use an absolute path that's valid on both Windows and Unix
                var nonExistentPath = Path.Combine(
                    Path.GetTempPath(), 
                    "nonexistent_dir_" + Guid.NewGuid().ToString("N"),
                    "nonexistent_tool.exe");
                
                var task = new RunEfcpt
                {
                    BuildEngine = s.Engine,
                    WorkingDirectory = s.WorkingDir,
                    DacpacPath = s.DacpacPath,
                    ConfigPath = s.ConfigPath,
                    RenamingPath = s.RenamingPath,
                    TemplateDir = s.TemplateDir,
                    OutputDir = s.OutputDir,
                    ToolPath = nonExistentPath
                };
                var success = task.Execute();
                return new TaskResult(s, task, success);
            })
            .Then("task fails", r => !r.Success)
            .And("error is logged", r => r.Setup.Engine.Errors.Count > 0)
            .And("error mentions tool path or file not found", r => 
                r.Setup.Engine.Errors.Any(e => 
                    (e.Message?.Contains("nonexistent_tool.exe", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.Message?.Contains("cannot find", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (e.Message?.Contains("No such file", StringComparison.OrdinalIgnoreCase) ?? false)))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Walks up directory tree to find tool manifest")]
    [Fact]
    public async Task Walks_up_to_find_manifest()
    {
        await Given("inputs with manifest in parent directory", () =>
            {
                var folder = new TestFolder();
                // Create manifest in root
                var configDir = folder.CreateDir(".config");
                var manifestPath = Path.Combine(configDir, "dotnet-tools.json");
                File.WriteAllText(manifestPath, """
                    {
                      "version": 1,
                      "isRoot": true,
                      "tools": {
                        "erikej.efcorepowertools.cli": {
                          "version": "10.0.0",
                          "commands": ["efcpt"]
                        }
                      }
                    }
                    """);
                
                // Working directory is nested deep
                var workingDir = folder.CreateDir(Path.Combine("a", "b", "c", "obj"));
                var dacpac = folder.WriteFile("db.dacpac", "content");
                var config = folder.WriteFile("config.json", "{}");
                var renaming = folder.WriteFile("renaming.json", "[]");
                var templateDir = folder.CreateDir("Templates");
                var outputDir = Path.Combine(folder.Root, "Generated");
                var engine = new TestBuildEngine();
                
                return new SetupState(folder, workingDir, dacpac, config, renaming, templateDir, outputDir, engine);
            })
            .When("task executes in fake mode with auto mode", s =>
                ExecuteTaskWithFakeMode(s, t => t.ToolMode = "auto"))
            .Then("task succeeds", r => r.Success)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Forwards EFCPT_TEST_DACPAC environment variable to process")]
    [Fact]
    public async Task Forwards_test_dacpac_env_var()
    {
        const string testDacpacValue = "C:\\test\\fake.dacpac";
        Environment.SetEnvironmentVariable("EFCPT_TEST_DACPAC", testDacpacValue);
        try
        {
            await Given("inputs for DACPAC mode", SetupForDacpacMode)
                .When("task executes in fake mode", s => ExecuteTaskWithFakeMode(s))
                .Then("task succeeds", r => r.Success)
                .And("environment variable is preserved", _ => 
                    Environment.GetEnvironmentVariable("EFCPT_TEST_DACPAC") == testDacpacValue)
                .Finally(r => r.Setup.Folder.Dispose())
                .AssertPassed();
        }
        finally
        {
            Environment.SetEnvironmentVariable("EFCPT_TEST_DACPAC", null);
        }
    }

    [Scenario("Passes all required arguments in DACPAC mode")]
    [Fact]
    public async Task Passes_dacpac_mode_arguments()
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes in fake mode", s => ExecuteTaskWithFakeMode(s))
            .Then("task succeeds", r => r.Success)
            .And("DACPAC path is used", r => !string.IsNullOrEmpty(r.Task.DacpacPath))
            .And("config path is used", r => !string.IsNullOrEmpty(r.Task.ConfigPath))
            .And("renaming path is used", r => !string.IsNullOrEmpty(r.Task.RenamingPath))
            .And("template dir is used", r => !string.IsNullOrEmpty(r.Task.TemplateDir))
            .And("output dir is used", r => !string.IsNullOrEmpty(r.Task.OutputDir))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Passes all required arguments in connection string mode")]
    [Fact]
    public async Task Passes_connection_string_mode_arguments()
    {
        await Given("inputs for connection string mode", SetupForConnectionStringMode)
            .When("task executes in fake mode", s =>
            {
                Environment.SetEnvironmentVariable("EFCPT_FAKE_EFCPT", "true");
                try
                {
                    var task = new RunEfcpt
                    {
                        BuildEngine = s.Engine,
                        WorkingDirectory = s.WorkingDir,
                        ConnectionString = "Server=.;Database=test;",
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
            .And("connection string is used", r => !string.IsNullOrEmpty(r.Task.ConnectionString))
            .And("connection string mode flag is set", r => r.Task.UseConnectionStringMode == "true")
            .And("config path is used", r => !string.IsNullOrEmpty(r.Task.ConfigPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Template directory path is passed correctly")]
    [Fact]
    public async Task Template_directory_passed_correctly()
    {
        await Given("inputs with custom template directory", () =>
            {
                var setup = SetupForDacpacMode();
                var customTemplateDir = setup.Folder.CreateDir("CustomTemplates");
                File.WriteAllText(Path.Combine(customTemplateDir, "test.template"), "template content");
                return (setup, customTemplateDir);
            })
            .When("task executes with custom template dir", ctx =>
                ExecuteTaskWithFakeMode(ctx.setup, t => t.TemplateDir = ctx.customTemplateDir))
            .Then("task succeeds", r => r.Success)
            .And("custom template dir is used", r => r.Task.TemplateDir.Contains("CustomTemplates"))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Provider parameter is passed correctly")]
    [Theory]
    [InlineData("mssql")]
    [InlineData("sqlite")]
    [InlineData("postgres")]
    public async Task Provider_parameter_passed_correctly(string provider)
    {
        await Given("inputs for DACPAC mode", SetupForDacpacMode)
            .When("task executes with specific provider", s =>
                ExecuteTaskWithFakeMode(s, t => t.Provider = provider))
            .Then("task succeeds", r => r.Success)
            .And("provider is set correctly", r => r.Task.Provider == provider)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("ProjectPath parameter is passed correctly")]
    [Fact]
    public async Task Project_path_passed_correctly()
    {
        await Given("inputs with project path", () =>
            {
                var setup = SetupForDacpacMode();
                var projectPath = Path.Combine(setup.Folder.Root, "Test.csproj");
                File.WriteAllText(projectPath, "<Project />");
                return (setup, projectPath);
            })
            .When("task executes with project path", ctx =>
                ExecuteTaskWithFakeMode(ctx.setup, t => t.ProjectPath = ctx.projectPath))
            .Then("task succeeds", r => r.Success)
            .And("project path is set", r => !string.IsNullOrEmpty(r.Task.ProjectPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
