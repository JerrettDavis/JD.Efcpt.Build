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

    private static StageResult ExecuteStage(StageSetup setup, string templateOutputDir)
    {
        var task = new StageEfcptInputs
        {
            BuildEngine = new TestBuildEngine(),
            OutputDir = setup.OutputDir,
            ProjectDirectory = setup.ProjectDir,
            ConfigPath = setup.ConfigPath,
            RenamingPath = setup.RenamingPath,
            TemplateDir = setup.TemplateDir,
            TemplateOutputDir = templateOutputDir
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
}
