using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using Xunit;

namespace JD.Efcpt.Build.Tests;

public sealed class StageEfcptInputsTests
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

    private static StageEfcptInputs ExecuteStage(StageSetup setup, string templateOutputDir)
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

        Assert.True(task.Execute());
        return task;
    }

    [Fact]
    public void Stages_under_output_dir_when_template_output_dir_empty()
    {
        var setup = CreateSetup(TemplateShape.EfCoreSubdir);
        var task = ExecuteStage(setup, "");

        var expectedRoot = Path.Combine(setup.OutputDir, "CodeTemplates");
        Assert.Equal(Path.GetFullPath(expectedRoot), Path.GetFullPath(task.StagedTemplateDir));
        Assert.True(File.Exists(Path.Combine(expectedRoot, "EFCore", "Entity.t4")));
        Assert.False(Directory.Exists(Path.Combine(expectedRoot, "Other")));

        setup.Folder.Dispose();
    }

    [Fact]
    public void Uses_output_relative_template_output_dir()
    {
        var setup = CreateSetup(TemplateShape.CodeTemplatesOnly);
        var task = ExecuteStage(setup, "Generated");

        var expectedRoot = Path.Combine(setup.OutputDir, "Generated", "CodeTemplates");
        Assert.Equal(Path.GetFullPath(expectedRoot), Path.GetFullPath(task.StagedTemplateDir));
        Assert.True(File.Exists(Path.Combine(expectedRoot, "Custom", "Thing.t4")));

        setup.Folder.Dispose();
    }

    [Fact]
    public void Uses_project_relative_obj_template_output_dir()
    {
        var setup = CreateSetup(TemplateShape.NoCodeTemplates);
        var task = ExecuteStage(setup, Path.Combine("obj", "efcpt", "Generated"));

        var expectedRoot = Path.Combine(setup.ProjectDir, "obj", "efcpt", "Generated", "CodeTemplates");
        Assert.Equal(Path.GetFullPath(expectedRoot), Path.GetFullPath(task.StagedTemplateDir));
        Assert.True(File.Exists(Path.Combine(expectedRoot, "Readme.txt")));

        setup.Folder.Dispose();
    }

    [Fact]
    public void Uses_absolute_template_output_dir()
    {
        var setup = CreateSetup(TemplateShape.CodeTemplatesOnly);
        var absoluteOutput = Path.Combine(setup.Folder.Root, "absolute", "gen");
        var task = ExecuteStage(setup, absoluteOutput);

        var expectedRoot = Path.Combine(absoluteOutput, "CodeTemplates");
        Assert.Equal(Path.GetFullPath(expectedRoot), Path.GetFullPath(task.StagedTemplateDir));
        Assert.True(File.Exists(Path.Combine(expectedRoot, "Custom", "Thing.t4")));
        Assert.True(File.Exists(task.StagedConfigPath));
        Assert.True(File.Exists(task.StagedRenamingPath));

        setup.Folder.Dispose();
    }
}
