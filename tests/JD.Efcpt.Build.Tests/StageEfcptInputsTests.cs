using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests;

[Feature("StageEfcptInputs task: stages configuration and templates to output directory")]
[Collection(nameof(AssemblySetup))]
public sealed class StageEfcptInputsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private enum TemplateShape
    {
        EfCoreSubdir,
        CodeTemplatesOnly,
        NoCodeTemplates
    }

    private sealed record StageSetup(
        TestFolder Folder,
        string ProjectDir,
        string OutputDir,
        string ConfigPath,
        string RenamingPath,
        string TemplateDir);

    private sealed record StageResult(
        StageSetup Setup,
        StageEfcptInputs Task,
        bool Success);

    private static StageSetup CreateSetup(TemplateShape shape)
    {
        var folder = new TestFolder();
        var projectDir = folder.CreateDir("app");
        var outputDir = Path.Combine(projectDir, "obj", "efcpt");
        var config = folder.WriteFile("app/efcpt-config.json", "{}");
        var renaming = folder.WriteFile("app/efcpt.renaming.json", "[]");
        var templateDir = CreateTemplate(folder, shape);

        return new StageSetup(folder, projectDir, outputDir, config, renaming, templateDir);
    }

    private static string CreateTemplate(TestFolder folder, TemplateShape shape)
    {
        const string root = "template";
        switch (shape)
        {
            case TemplateShape.EfCoreSubdir:
                folder.WriteFile($"{root}/CodeTemplates/EFCore/Entity.t4", "efcore");
                folder.WriteFile($"{root}/CodeTemplates/Other/Ignore.txt", "ignore");
                break;
            case TemplateShape.CodeTemplatesOnly:
                folder.WriteFile($"{root}/CodeTemplates/Custom/Thing.t4", "custom");
                break;
            case TemplateShape.NoCodeTemplates:
                folder.WriteFile($"{root}/Readme.txt", "plain");
                break;
        }

        return Path.Combine(folder.Root, root);
    }

    private static StageResult ExecuteStage(StageSetup setup, string templateOutputDir, string? targetFramework = null)
    {
        var task = new StageEfcptInputs
        {
            BuildEngine = new TestBuildEngine(),
            OutputDir = setup.OutputDir,
            ProjectDirectory = setup.ProjectDir,
            ConfigPath = setup.ConfigPath,
            RenamingPath = setup.RenamingPath,
            TemplateDir = setup.TemplateDir,
            TemplateOutputDir = templateOutputDir,
            TargetFramework = targetFramework ?? ""
        };

        var success = task.Execute();
        return new StageResult(setup, task, success);
    }

    [Scenario("Stages under output dir when template output dir empty")]
    [Fact]
    public async Task Stages_under_output_dir_when_template_output_dir_empty()
    {
        await Given("setup with EFCore subdirectory template", () => CreateSetup(TemplateShape.EfCoreSubdir))
            .When("execute stage with empty template output dir", setup => ExecuteStage(setup, ""))
            .Then("task succeeds", r => r.Success)
            .And("staged template dir is under output dir", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.OutputDir, "CodeTemplates");
                return Path.GetFullPath(expectedRoot) == Path.GetFullPath(r.Task.StagedTemplateDir);
            })
            .And("EFCore template files are staged", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.OutputDir, "CodeTemplates");
                return File.Exists(Path.Combine(expectedRoot, "EFCore", "Entity.t4"));
            })
            .And("non-EFCore directories are excluded", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.OutputDir, "CodeTemplates");
                return !Directory.Exists(Path.Combine(expectedRoot, "Other"));
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses output-relative template output dir")]
    [Fact]
    public async Task Uses_output_relative_template_output_dir()
    {
        await Given("setup with CodeTemplates only", () => CreateSetup(TemplateShape.CodeTemplatesOnly))
            .When("execute stage with relative template output dir", setup => ExecuteStage(setup, "Generated"))
            .Then("task succeeds", r => r.Success)
            .And("staged template dir is under output/Generated", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.OutputDir, "Generated", "CodeTemplates");
                return Path.GetFullPath(expectedRoot) == Path.GetFullPath(r.Task.StagedTemplateDir);
            })
            .And("template files are staged", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.OutputDir, "Generated", "CodeTemplates");
                return File.Exists(Path.Combine(expectedRoot, "Custom", "Thing.t4"));
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses project-relative obj template output dir")]
    [Fact]
    public async Task Uses_project_relative_obj_template_output_dir()
    {
        await Given("setup with no CodeTemplates", () => CreateSetup(TemplateShape.NoCodeTemplates))
            .When("execute stage with project-relative path", setup =>
                ExecuteStage(setup, Path.Combine("obj", "efcpt", "Generated")))
            .Then("task succeeds", r => r.Success)
            .And("staged template dir is under project/obj/efcpt/Generated", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.ProjectDir, "obj", "efcpt", "Generated", "CodeTemplates");
                return Path.GetFullPath(expectedRoot) == Path.GetFullPath(r.Task.StagedTemplateDir);
            })
            .And("template files are staged", r =>
            {
                var expectedRoot = Path.Combine(r.Setup.ProjectDir, "obj", "efcpt", "Generated", "CodeTemplates");
                return File.Exists(Path.Combine(expectedRoot, "Readme.txt"));
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses absolute template output dir")]
    [Fact]
    public async Task Uses_absolute_template_output_dir()
    {
        await Given("setup with CodeTemplates only", () => CreateSetup(TemplateShape.CodeTemplatesOnly))
            .When("execute stage with absolute path", setup =>
            {
                var absoluteOutput = Path.Combine(setup.Folder.Root, "absolute", "gen");
                return ExecuteStage(setup, absoluteOutput);
            })
            .Then("task succeeds", r => r.Success)
            .And("staged template dir is under absolute path", r =>
            {
                var absoluteOutput = Path.Combine(r.Setup.Folder.Root, "absolute", "gen");
                var expectedRoot = Path.Combine(absoluteOutput, "CodeTemplates");
                return Path.GetFullPath(expectedRoot) == Path.GetFullPath(r.Task.StagedTemplateDir);
            })
            .And("template files are staged", r =>
            {
                var absoluteOutput = Path.Combine(r.Setup.Folder.Root, "absolute", "gen");
                var expectedRoot = Path.Combine(absoluteOutput, "CodeTemplates");
                return File.Exists(Path.Combine(expectedRoot, "Custom", "Thing.t4"));
            })
            .And("config file is staged", r => File.Exists(r.Task.StagedConfigPath))
            .And("renaming file is staged", r => File.Exists(r.Task.StagedRenamingPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    private static StageSetup CreateVersionSpecificTemplateSetup()
    {
        var folder = new TestFolder();
        var projectDir = folder.CreateDir("app");
        var outputDir = Path.Combine(projectDir, "obj", "efcpt");
        var config = folder.WriteFile("app/efcpt-config.json", "{}");
        var renaming = folder.WriteFile("app/efcpt.renaming.json", "[]");

        // Create version-specific template structure like defaults
        const string root = "template";
        folder.WriteFile($"{root}/CodeTemplates/EFCore/net800/DbContext.t4", "net8 template");
        folder.WriteFile($"{root}/CodeTemplates/EFCore/net900/DbContext.t4", "net9 template");
        folder.WriteFile($"{root}/CodeTemplates/EFCore/net1000/DbContext.t4", "net10 template");

        var templateDir = Path.Combine(folder.Root, root);
        return new StageSetup(folder, projectDir, outputDir, config, renaming, templateDir);
    }

    [Scenario("Selects version-specific templates for net8.0")]
    [Fact]
    public async Task Selects_version_specific_templates_for_net80()
    {
        await Given("setup with version-specific templates", CreateVersionSpecificTemplateSetup)
            .When("execute stage with net8.0 target framework", setup => ExecuteStage(setup, "", "net8.0"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net8 content", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net8 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Selects version-specific templates for net10.0")]
    [Fact]
    public async Task Selects_version_specific_templates_for_net100()
    {
        await Given("setup with version-specific templates", CreateVersionSpecificTemplateSetup)
            .When("execute stage with net10.0 target framework", setup => ExecuteStage(setup, "", "net10.0"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net10 content", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net10 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Falls back to lower version when exact match not found")]
    [Fact]
    public async Task Falls_back_to_lower_version_when_exact_match_not_found()
    {
        await Given("setup with version-specific templates", CreateVersionSpecificTemplateSetup)
            .When("execute stage with net11.0 target framework", setup => ExecuteStage(setup, "", "net11.0"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net10 content (fallback)", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net10 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses regular templates when no target framework specified")]
    [Fact]
    public async Task Uses_regular_templates_when_no_target_framework_specified()
    {
        await Given("setup with EFCore subdirectory template", () => CreateSetup(TemplateShape.EfCoreSubdir))
            .When("execute stage without target framework", setup => ExecuteStage(setup, ""))
            .Then("task succeeds", r => r.Success)
            .And("template files are staged", r =>
            {
                var entityPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "Entity.t4");
                return File.Exists(entityPath);
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses regular templates when target framework is null")]
    [Fact]
    public async Task Uses_regular_templates_when_target_framework_is_null()
    {
        await Given("setup with EFCore subdirectory template", () => CreateSetup(TemplateShape.EfCoreSubdir))
            .When("execute stage with null target framework", setup => ExecuteStage(setup, "", null))
            .Then("task succeeds", r => r.Success)
            .And("template files are staged", r =>
            {
                var entityPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "Entity.t4");
                return File.Exists(entityPath);
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Falls back to regular templates with malformed target framework 'net'")]
    [Fact]
    public async Task Falls_back_to_regular_templates_with_malformed_framework_net()
    {
        await Given("setup with EFCore subdirectory template", () => CreateSetup(TemplateShape.EfCoreSubdir))
            .When("execute stage with malformed 'net' framework", setup => ExecuteStage(setup, "", "net"))
            .Then("task succeeds", r => r.Success)
            .And("template files are staged", r =>
            {
                var entityPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "Entity.t4");
                return File.Exists(entityPath);
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Falls back to regular templates with malformed target framework 'netabc'")]
    [Fact]
    public async Task Falls_back_to_regular_templates_with_malformed_framework_netabc()
    {
        await Given("setup with EFCore subdirectory template", () => CreateSetup(TemplateShape.EfCoreSubdir))
            .When("execute stage with malformed 'netabc' framework", setup => ExecuteStage(setup, "", "netabc"))
            .Then("task succeeds", r => r.Success)
            .And("template files are staged", r =>
            {
                var entityPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "Entity.t4");
                return File.Exists(entityPath);
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Parses target framework without minor version 'net8'")]
    [Fact]
    public async Task Parses_target_framework_without_minor_version()
    {
        await Given("setup with version-specific templates", CreateVersionSpecificTemplateSetup)
            .When("execute stage with 'net8' framework", setup => ExecuteStage(setup, "", "net8"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net8 content", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net8 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Parses target framework with patch version 'net8.0.1'")]
    [Fact]
    public async Task Parses_target_framework_with_patch_version()
    {
        await Given("setup with version-specific templates", CreateVersionSpecificTemplateSetup)
            .When("execute stage with 'net8.0.1' framework", setup => ExecuteStage(setup, "", "net8.0.1"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net8 content", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net8 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    private static StageSetup CreateNonStandardFolderSetup()
    {
        var folder = new TestFolder();
        var projectDir = folder.CreateDir("app");
        var outputDir = Path.Combine(projectDir, "obj", "efcpt");
        var config = folder.WriteFile("app/efcpt-config.json", "{}");
        var renaming = folder.WriteFile("app/efcpt.renaming.json", "[]");

        // Create templates with non-standard folder names that should be ignored
        const string root = "template";
        folder.WriteFile($"{root}/CodeTemplates/EFCore/net800/DbContext.t4", "net8 template");
        folder.WriteFile($"{root}/CodeTemplates/EFCore/net8/Invalid.t4", "invalid - no 00 suffix");
        folder.WriteFile($"{root}/CodeTemplates/EFCore/net900x/Invalid.t4", "invalid - extra char");
        folder.WriteFile($"{root}/CodeTemplates/EFCore/NET1000/DbContext.t4", "uppercase - should be ignored");

        var templateDir = Path.Combine(folder.Root, root);
        return new StageSetup(folder, projectDir, outputDir, config, renaming, templateDir);
    }

    [Scenario("Ignores non-standard folder names and uses valid version folder")]
    [Fact]
    public async Task Ignores_non_standard_folder_names_and_uses_valid_version_folder()
    {
        await Given("setup with non-standard folder names", CreateNonStandardFolderSetup)
            .When("execute stage with net8.0 framework", setup => ExecuteStage(setup, "", "net8.0"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net8 content from valid folder", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net8 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Falls back correctly when only lower version folders exist")]
    [Fact]
    public async Task Falls_back_correctly_when_only_lower_version_folders_exist()
    {
        await Given("setup with non-standard folder names", CreateNonStandardFolderSetup)
            .When("execute stage with net9.0 framework", setup => ExecuteStage(setup, "", "net9.0"))
            .Then("task succeeds", r => r.Success)
            .And("DbContext.t4 contains net8 content (fallback)", r =>
            {
                var dbContextPath = Path.Combine(r.Task.StagedTemplateDir, "EFCore", "DbContext.t4");
                // Should fall back to net800 since net900x is invalid and NET1000 is uppercase
                return File.Exists(dbContextPath) && File.ReadAllText(dbContextPath).Contains("net8 template");
            })
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
