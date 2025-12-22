using JD.Efcpt.Build.Tasks.Chains;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for FileResolutionChain and DirectoryResolutionChain.
/// </summary>
[Feature("Resolution Chains: multi-tier fallback for locating files and directories")]
[Collection(nameof(AssemblySetup))]
public sealed class ResolutionChainTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region FileResolutionChain Tests

    [Scenario("FileResolutionChain: finds file via explicit override path")]
    [Fact]
    public async Task File_explicit_override_path()
    {
        await Given("a file at an explicit path", () =>
            {
                var folder = new TestFolder();
                var configPath = folder.WriteFile("custom/config.json", "{}");
                return (folder, configPath);
            })
            .When("chain executes with override", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "custom/config.json",
                    ProjectDirectory: t.folder.Root,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    FileNames: ["default.json"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("found file matches override", t => t.result?.EndsWith("config.json") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: finds file in project directory")]
    [Fact]
    public async Task File_found_in_project_directory()
    {
        await Given("a file in project directory", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                folder.WriteFile("project/efcpt-config.json", "{}");
                return (folder, projectDir);
            })
            .When("chain executes", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    FileNames: ["efcpt-config.json"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("file is found", t => File.Exists(t.result))
            .And("path contains project directory", t => t.result?.Contains("project") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: finds file in solution directory")]
    [Fact]
    public async Task File_found_in_solution_directory()
    {
        await Given("a file only in solution directory", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var solutionDir = folder.CreateDir("solution");
                folder.WriteFile("solution/efcpt-config.json", "{}");
                return (folder, projectDir, solutionDir);
            })
            .When("chain executes with solution probing", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: t.solutionDir,
                    ProbeSolutionDir: true,
                    DefaultsRoot: "",
                    FileNames: ["efcpt-config.json"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("file is found in solution dir", t => t.result?.Contains("solution") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: finds file in defaults root")]
    [Fact]
    public async Task File_found_in_defaults_root()
    {
        await Given("a file only in defaults root", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var defaultsDir = folder.CreateDir("defaults");
                folder.WriteFile("defaults/efcpt-config.json", "{}");
                return (folder, projectDir, defaultsDir);
            })
            .When("chain executes", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: t.defaultsDir,
                    FileNames: ["efcpt-config.json"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("file is found in defaults", t => t.result?.Contains("defaults") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: throws when file not found anywhere")]
    [Fact]
    public async Task File_not_found_throws()
    {
        await Given("empty directories", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                return (folder, projectDir);
            })
            .When("chain executes", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    FileNames: ["missing.json"]);
                try
                {
                    chain.Execute(in ctx, out _);
                    return (threw: false, t.folder);
                }
                catch (FileNotFoundException)
                {
                    return (threw: true, t.folder);
                }
            })
            .Then("FileNotFoundException is thrown", t => t.threw)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: throws when override path doesn't exist")]
    [Fact]
    public async Task File_override_not_found_throws()
    {
        await Given("no file at override path", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                return (folder, projectDir);
            })
            .When("chain executes with missing override", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "missing/path/config.json",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    FileNames: ["default.json"]);
                try
                {
                    chain.Execute(in ctx, out _);
                    return (threw: false, t.folder);
                }
                catch (FileNotFoundException)
                {
                    return (threw: true, t.folder);
                }
            })
            .Then("FileNotFoundException is thrown", t => t.threw)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: project directory takes priority over solution")]
    [Fact]
    public async Task File_project_priority_over_solution()
    {
        await Given("files in both project and solution directories", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var solutionDir = folder.CreateDir("solution");
                folder.WriteFile("project/config.json", "project");
                folder.WriteFile("solution/config.json", "solution");
                return (folder, projectDir, solutionDir);
            })
            .When("chain executes", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: t.solutionDir,
                    ProbeSolutionDir: true,
                    DefaultsRoot: "",
                    FileNames: ["config.json"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("project file is returned", t => t.result?.Contains("project") == true && !t.result.Contains("solution"))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("FileResolutionChain: tries multiple file names in order")]
    [Fact]
    public async Task File_tries_multiple_names()
    {
        await Given("only second candidate exists", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                folder.WriteFile("project/alternate-config.json", "{}");
                return (folder, projectDir);
            })
            .When("chain executes with multiple names", t =>
            {
                var chain = FileResolutionChain.Build();
                var ctx = new FileResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    FileNames: ["primary-config.json", "alternate-config.json"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("second name is found", t => t.result?.EndsWith("alternate-config.json") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    #endregion

    #region DirectoryResolutionChain Tests

    [Scenario("DirectoryResolutionChain: finds directory via explicit override")]
    [Fact]
    public async Task Dir_explicit_override_path()
    {
        await Given("a directory at an explicit path", () =>
            {
                var folder = new TestFolder();
                var templateDir = folder.CreateDir("custom/Templates");
                return (folder, templateDir);
            })
            .When("chain executes with override", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "custom/Templates",
                    ProjectDirectory: t.folder.Root,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    DirNames: ["Default"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("found directory matches override", t => t.result?.EndsWith("Templates") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: finds directory in project directory")]
    [Fact]
    public async Task Dir_found_in_project_directory()
    {
        await Given("a template directory in project", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                folder.CreateDir("project/Template");
                return (folder, projectDir);
            })
            .When("chain executes", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    DirNames: ["Template"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("directory is found", t => Directory.Exists(t.result))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: finds directory in solution directory")]
    [Fact]
    public async Task Dir_found_in_solution_directory()
    {
        await Given("template only in solution directory", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var solutionDir = folder.CreateDir("solution");
                folder.CreateDir("solution/Template");
                return (folder, projectDir, solutionDir);
            })
            .When("chain executes with solution probing", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: t.solutionDir,
                    ProbeSolutionDir: true,
                    DefaultsRoot: "",
                    DirNames: ["Template"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("directory is found in solution", t => t.result?.Contains("solution") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: finds directory in defaults root")]
    [Fact]
    public async Task Dir_found_in_defaults_root()
    {
        await Given("template only in defaults", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var defaultsDir = folder.CreateDir("defaults");
                folder.CreateDir("defaults/Template");
                return (folder, projectDir, defaultsDir);
            })
            .When("chain executes", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: t.defaultsDir,
                    DirNames: ["Template"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("directory is found in defaults", t => t.result?.Contains("defaults") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: throws when directory not found")]
    [Fact]
    public async Task Dir_not_found_throws()
    {
        await Given("empty directories", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                return (folder, projectDir);
            })
            .When("chain executes", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    DirNames: ["Missing"]);
                try
                {
                    chain.Execute(in ctx, out _);
                    return (threw: false, t.folder);
                }
                catch (DirectoryNotFoundException)
                {
                    return (threw: true, t.folder);
                }
            })
            .Then("DirectoryNotFoundException is thrown", t => t.threw)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: project priority over solution")]
    [Fact]
    public async Task Dir_project_priority_over_solution()
    {
        await Given("directories in both project and solution", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var solutionDir = folder.CreateDir("solution");
                folder.CreateDir("project/Template");
                folder.CreateDir("solution/Template");
                return (folder, projectDir, solutionDir);
            })
            .When("chain executes", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: t.solutionDir,
                    ProbeSolutionDir: true,
                    DefaultsRoot: "",
                    DirNames: ["Template"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("project directory is returned", t => t.result?.Contains("project") == true && !t.result.Contains("solution"))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: tries multiple directory names")]
    [Fact]
    public async Task Dir_tries_multiple_names()
    {
        await Given("only second candidate exists", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                folder.CreateDir("project/CodeTemplates");
                return (folder, projectDir);
            })
            .When("chain executes with multiple names", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: "",
                    ProbeSolutionDir: false,
                    DefaultsRoot: "",
                    DirNames: ["Template", "CodeTemplates"]);
                chain.Execute(in ctx, out var result);
                return (result, t.folder);
            })
            .Then("second name is found", t => t.result?.EndsWith("CodeTemplates") == true)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DirectoryResolutionChain: skips solution probing when disabled")]
    [Fact]
    public async Task Dir_skips_solution_when_disabled()
    {
        await Given("template only in solution directory", () =>
            {
                var folder = new TestFolder();
                var projectDir = folder.CreateDir("project");
                var solutionDir = folder.CreateDir("solution");
                folder.CreateDir("solution/Template");
                return (folder, projectDir, solutionDir);
            })
            .When("chain executes with probing disabled", t =>
            {
                var chain = DirectoryResolutionChain.Build();
                var ctx = new DirectoryResolutionContext(
                    OverridePath: "",
                    ProjectDirectory: t.projectDir,
                    SolutionDir: t.solutionDir,
                    ProbeSolutionDir: false, // Disabled
                    DefaultsRoot: "",
                    DirNames: ["Template"]);
                try
                {
                    chain.Execute(in ctx, out _);
                    return (threw: false, t.folder);
                }
                catch (DirectoryNotFoundException)
                {
                    return (threw: true, t.folder);
                }
            })
            .Then("DirectoryNotFoundException is thrown (solution not checked)", t => t.threw)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    #endregion
}
