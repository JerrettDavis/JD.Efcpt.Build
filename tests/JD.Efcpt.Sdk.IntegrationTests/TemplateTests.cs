using FluentAssertions;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Integration tests for the JD.Efcpt.Build.Templates package and dotnet new template functionality.
/// Tests validate that the template creates projects with the expected structure and that they build correctly.
/// </summary>
[Collection("Template Tests")]
public class TemplateTests : IDisposable
{
    private readonly TemplateTestFixture _fixture;
    private readonly string _testDirectory;

    public TemplateTests(TemplateTestFixture fixture)
    {
        _fixture = fixture;
        _testDirectory = Path.Combine(Path.GetTempPath(), "TemplateTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task Template_InstallsSuccessfully()
    {
        // Act
        var result = await _fixture.InstallTemplateAsync(_testDirectory);

        // Assert
        result.Success.Should().BeTrue($"Template installation should succeed.\n{result}");
        result.Output.Should().Contain("efcptbuild", "Template should be installed with short name 'efcptbuild'");
    }

    [Fact]
    public async Task Template_CreatesProjectWithCorrectStructure()
    {
        // Arrange
        await _fixture.InstallTemplateAsync(_testDirectory);
        var projectName = "TestEfcptProject";

        // Act
        var createResult = await _fixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Assert
        createResult.Success.Should().BeTrue($"Project creation should succeed.\n{createResult}");

        var projectDir = Path.Combine(_testDirectory, projectName);
        Directory.Exists(projectDir).Should().BeTrue("Project directory should be created");

        // Verify expected files
        File.Exists(Path.Combine(projectDir, $"{projectName}.csproj")).Should().BeTrue("Project file should exist");
        File.Exists(Path.Combine(projectDir, "efcpt-config.json")).Should().BeTrue("Config file should exist");
        File.Exists(Path.Combine(projectDir, "README.md")).Should().BeTrue("README should exist");
    }

    [Fact]
    public async Task Template_CreatesProjectUsingSdkApproach()
    {
        // Arrange
        await _fixture.InstallTemplateAsync(_testDirectory);
        var projectName = "TestSdkProject";
        await _fixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Act
        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Assert
        projectContent.Should().Contain("<Project Sdk=\"JD.Efcpt.Sdk\">",
            "Project should use JD.Efcpt.Sdk");
        projectContent.Should().NotMatch("*<PackageReference*Include=\"JD.Efcpt.Build\"*",
            "Project should not reference JD.Efcpt.Build package directly");
        projectContent.Should().Contain("Microsoft.EntityFrameworkCore",
            "Project should include EF Core packages");
    }

    [Fact]
    public async Task Template_ConfigFileContainsCorrectProjectName()
    {
        // Arrange
        await _fixture.InstallTemplateAsync(_testDirectory);
        var projectName = "MyCustomProject";
        await _fixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Act
        var configFile = Path.Combine(_testDirectory, projectName, "efcpt-config.json");
        var configContent = await File.ReadAllTextAsync(configFile);

        // Assert
        configContent.Should().Contain($"\"root-namespace\": \"{projectName}\"",
            "Config should use project name for root namespace");
        configContent.Should().Contain($"\"{projectName}.Data\"",
            "Config should use project name in namespace references");
    }

    [Fact]
    public async Task Template_CreatedProjectBuildsSuccessfully()
    {
        // Arrange
        await _fixture.InstallTemplateAsync(_testDirectory);
        var projectName = "BuildableProject";
        await _fixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Copy database project to test directory
        var dbProjectSource = Path.Combine(_fixture.GetTestFixturesPath(), "DatabaseProject");
        var dbProjectDest = Path.Combine(_testDirectory, "DatabaseProject");
        CopyDirectory(dbProjectSource, dbProjectDest);

        // Add ProjectReference to the generated project
        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);
        var modifiedContent = projectContent.Replace(
            "<!--\n    Reference your SQL Server Database Project for automatic DACPAC generation\n    \n    <ItemGroup>",
            "<ItemGroup>");
        modifiedContent = modifiedContent.Replace(
            "      <ProjectReference Include=\"..\\YourDatabase\\YourDatabase.sqlproj\">",
            "      <ProjectReference Include=\"..\\DatabaseProject\\DatabaseProject.csproj\">");
        modifiedContent = modifiedContent.Replace(
            "    </ItemGroup>\n    \n    Or for MSBuild.Sdk.SqlProj:",
            "    </ItemGroup>\n    <!--");
        await File.WriteAllTextAsync(projectFile, modifiedContent);

        // Create nuget.config to use local packages
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{_fixture.PackageOutputPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "nuget.config"), nugetConfig);

        // Create global.json
        var globalJson = $@"{{
  ""msbuild-sdks"": {{
    ""JD.Efcpt.Sdk"": ""{_fixture.SdkVersion}""
  }}
}}";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "global.json"), globalJson);

        // Act - Restore
        var restoreResult = await RunDotnetCommandAsync(_testDirectory, projectName, "restore");
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act - Build
        var buildResult = await RunDotnetCommandAsync(_testDirectory, projectName, "build --no-restore");

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");

        // Verify generated files exist
        var generatedDir = Path.Combine(_testDirectory, projectName, "obj", "efcpt", "Generated");
        Directory.Exists(generatedDir).Should().BeTrue("Generated directory should exist after build");
    }

    [Fact]
    public async Task Template_ReadmeContainsSdkInformation()
    {
        // Arrange
        await _fixture.InstallTemplateAsync(_testDirectory);
        var projectName = "ReadmeTestProject";
        await _fixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Act
        var readmePath = Path.Combine(_testDirectory, projectName, "README.md");
        var readmeContent = await File.ReadAllTextAsync(readmePath);

        // Assert
        readmeContent.Should().Contain("JD.Efcpt.Sdk", "README should mention JD.Efcpt.Sdk");
        readmeContent.Should().Contain("MSBuild SDK", "README should explain it's an MSBuild SDK");
        readmeContent.Should().NotContain("JD.Efcpt.Build package reference",
            "README should not reference the Build package approach");
    }

    [Fact]
    public async Task Template_UninstallsSuccessfully()
    {
        // Arrange
        await _fixture.InstallTemplateAsync(_testDirectory);

        // Act
        var result = await _fixture.UninstallTemplateAsync(_testDirectory);

        // Assert
        result.Success.Should().BeTrue($"Template uninstallation should succeed.\n{result}");
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, true);
        }
    }

    private static async Task<TestUtilities.CommandResult> RunDotnetCommandAsync(string workingDirectory, string projectName, string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = Path.Combine(workingDirectory, projectName),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new InvalidOperationException(
                $"dotnet {arguments} timed out after 5 minutes.");
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);

        return new TestUtilities.CommandResult(
            process.ExitCode == 0,
            output,
            error,
            process.ExitCode
        );
    }
}
