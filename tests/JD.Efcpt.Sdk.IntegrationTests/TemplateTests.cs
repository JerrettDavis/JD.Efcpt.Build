using FluentAssertions;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Integration tests for the JD.Efcpt.Build.Templates package and dotnet new template functionality.
/// Tests validate that the template creates projects with the expected structure and that they build correctly.
/// </summary>
[Collection("Template Tests")]
public partial class TemplateTests : IDisposable
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
        // Arrange - template is already installed by fixture
        var projectName = "TestEfcptProject";

        // Act
        var createResult = await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

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
        // Arrange - template is already installed by fixture
        var projectName = "TestSdkProject";
        await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Act
        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Assert
        projectContent.Should().Match("*<Project Sdk=\"JD.Efcpt.Sdk/*\">*",
            "Project should use JD.Efcpt.Sdk with version");
        projectContent.Should().NotMatch("*<PackageReference*Include=\"JD.Efcpt.Build\"*",
            "Project should not reference JD.Efcpt.Build package directly");
        projectContent.Should().Contain("Microsoft.EntityFrameworkCore",
            "Project should include EF Core packages");
    }

    [Fact]
    public async Task Template_ConfigFileContainsCorrectProjectName()
    {
        // Arrange - template is already installed by fixture
        var projectName = "MyCustomProject";
        await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

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
        // Arrange - template is already installed by fixture
        var projectName = "BuildableProject";
        await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

        // Copy database project to test directory
        var dbProjectSource = Path.Combine(TemplateTestFixture.GetTestFixturesPath(), "DatabaseProject");
        var dbProjectDest = Path.Combine(_testDirectory, "DatabaseProject");
        CopyDirectory(dbProjectSource, dbProjectDest);

        // Add ProjectReference to the generated project
        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Insert a ProjectReference before the EF Core packages ItemGroup
        // This is more robust than trying to uncomment the template example
        var projectReferenceBlock = @"
  <ItemGroup>
    <ProjectReference Include=""..\DatabaseProject\DatabaseProject.csproj"">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
      <OutputItemType>None</OutputItemType>
    </ProjectReference>
  </ItemGroup>

  <!-- EF Core packages";
        var modifiedContent = projectContent.Replace(
            "<!-- EF Core packages",
            projectReferenceBlock);
        await File.WriteAllTextAsync(projectFile, modifiedContent);

        // Create nuget.config to use local packages
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{TemplateTestFixture.PackageOutputPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "nuget.config"), nugetConfig);

        // Create global.json
        var globalJson = $@"{{
  ""msbuild-sdks"": {{
    ""JD.Efcpt.Sdk"": ""{TemplateTestFixture.SdkVersion}""
  }}
}}";
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "global.json"), globalJson);

        // Create tool manifest and restore tools for tool-manifest mode support
        await CreateToolManifestAndRestoreAsync(_testDirectory);

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
        // Arrange - template is already installed by fixture
        var projectName = "ReadmeTestProject";
        await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);

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
        // Arrange - reinstall to test uninstall
        await _fixture.InstallTemplateAsync(_testDirectory);

        // Act
        var result = await TemplateTestFixture.UninstallTemplateAsync(_testDirectory);

        // Assert
        result.Success.Should().BeTrue($"Template uninstallation should succeed.\n{result}");

        // Reinstall the template so subsequent tests can use it
        var reinstallResult = await _fixture.InstallTemplateAsync(_testDirectory);
        reinstallResult.Success.Should().BeTrue("Template should be reinstalled after uninstall test");
    }

    #region Framework Variant Tests

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public async Task Template_CreatesProjectWithCorrectTargetFramework(string framework)
    {
        // Arrange
        var projectName = $"TestFramework_{framework.Replace(".", "")}";

        // Act
        var createResult = await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName, framework);
        createResult.Success.Should().BeTrue($"Project creation for {framework} should succeed.\n{createResult}");

        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Assert
        projectContent.Should().Contain($"<TargetFramework>{framework}</TargetFramework>",
            $"Project should target {framework}");
    }

    [Theory]
    [InlineData("net8.0", "9.0.")]
    [InlineData("net9.0", "9.0.")]
    [InlineData("net10.0", "10.0.")]
    public async Task Template_HasCorrectEFCoreVersion(string framework, string expectedVersionPrefix)
    {
        // Arrange
        var projectName = $"TestEFCore_{framework.Replace(".", "")}";

        // Act
        var createResult = await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName, framework);
        createResult.Success.Should().BeTrue($"Project creation for {framework} should succeed.\n{createResult}");

        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Assert
        projectContent.Should().MatchRegex(
            $@"Microsoft\.EntityFrameworkCore\.SqlServer.*Version=""{expectedVersionPrefix}",
            $"Project targeting {framework} should use EF Core {expectedVersionPrefix}x");
    }

    [Theory]
    [InlineData("net8.0", "9.0.11")]
    [InlineData("net9.0", "9.0.11")]
    [InlineData("net10.0", "10.0.1")]
    public async Task Template_FrameworkVariant_BuildsSuccessfully(string framework, string efCoreVersion)
    {
        // Arrange
        var projectName = $"BuildTest_{framework.Replace(".", "")}";
        var createResult = await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName, framework);
        createResult.Success.Should().BeTrue($"Project creation for {framework} should succeed.\n{createResult}");

        // Copy database project to test directory
        var dbProjectSource = Path.Combine(TemplateTestFixture.GetTestFixturesPath(), "DatabaseProject");
        var dbProjectDest = Path.Combine(_testDirectory, "DatabaseProject");
        if (!Directory.Exists(dbProjectDest))
        {
            CopyDirectory(dbProjectSource, dbProjectDest);
        }

        // Modify project to use specific EF Core version (not floating) and add ProjectReference
        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Replace floating version with specific version
        projectContent = MyRegex().Replace(projectContent, $@"Version=""{efCoreVersion}""");

        // Add ProjectReference to database project
        var projectReferenceBlock = @"
  <ItemGroup>
    <ProjectReference Include=""..\DatabaseProject\DatabaseProject.csproj"">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
      <OutputItemType>None</OutputItemType>
    </ProjectReference>
  </ItemGroup>

  <!-- EF Core packages";
        projectContent = projectContent.Replace("<!-- EF Core packages", projectReferenceBlock);
        await File.WriteAllTextAsync(projectFile, projectContent);

        // Create nuget.config to use local packages
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{TemplateTestFixture.PackageOutputPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
        var nugetConfigPath = Path.Combine(_testDirectory, "nuget.config");
        if (!File.Exists(nugetConfigPath))
        {
            await File.WriteAllTextAsync(nugetConfigPath, nugetConfig);
        }

        // Create global.json
        var globalJson = $@"{{
  ""msbuild-sdks"": {{
    ""JD.Efcpt.Sdk"": ""{TemplateTestFixture.SdkVersion}""
  }}
}}";
        var globalJsonPath = Path.Combine(_testDirectory, "global.json");
        if (!File.Exists(globalJsonPath))
        {
            await File.WriteAllTextAsync(globalJsonPath, globalJson);
        }

        // Create tool manifest and restore tools for tool-manifest mode support
        var toolManifestPath = Path.Combine(_testDirectory, ".config", "dotnet-tools.json");
        if (!File.Exists(toolManifestPath))
        {
            await CreateToolManifestAndRestoreAsync(_testDirectory);
        }

        // Act - Restore
        var restoreResult = await RunDotnetCommandAsync(_testDirectory, projectName, "restore");
        restoreResult.Success.Should().BeTrue($"Restore for {framework} should succeed.\n{restoreResult}");

        // Act - Build
        var buildResult = await RunDotnetCommandAsync(_testDirectory, projectName, "build --no-restore");

        // Assert
        buildResult.Success.Should().BeTrue($"Build for {framework} should succeed.\n{buildResult}");

        // Verify generated files exist
        var generatedDir = Path.Combine(_testDirectory, projectName, "obj", "efcpt", "Generated");
        Directory.Exists(generatedDir).Should().BeTrue($"Generated directory should exist after {framework} build");

        // Verify at least one generated file exists
        var generatedFiles = Directory.GetFiles(generatedDir, "*.g.cs", SearchOption.AllDirectories);
        generatedFiles.Should().NotBeEmpty($"Should have generated files for {framework} build");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public async Task Template_FrameworkVariant_UsesJDEfcptSdk(string framework)
    {
        // Arrange
        var projectName = $"SdkCheck_{framework.Replace(".", "")}";

        // Act
        var createResult = await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName, framework);
        createResult.Success.Should().BeTrue($"Project creation for {framework} should succeed.\n{createResult}");

        var projectFile = Path.Combine(_testDirectory, projectName, $"{projectName}.csproj");
        var projectContent = await File.ReadAllTextAsync(projectFile);

        // Assert
        projectContent.Should().Match("*<Project Sdk=\"JD.Efcpt.Sdk/*\">*",
            $"{framework} project should use JD.Efcpt.Sdk with version");
        projectContent.Should().NotContain("PackageReference Include=\"JD.Efcpt.Build\"",
            $"{framework} project should not have JD.Efcpt.Build package reference");
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public async Task Template_FrameworkVariant_HasConfigFile(string framework)
    {
        // Arrange
        var projectName = $"ConfigCheck_{framework.Replace(".", "")}";

        // Act
        var createResult = await TemplateTestFixture.CreateProjectFromTemplateAsync(_testDirectory, projectName, framework);
        createResult.Success.Should().BeTrue($"Project creation for {framework} should succeed.\n{createResult}");

        var configFile = Path.Combine(_testDirectory, projectName, "efcpt-config.json");

        // Assert
        File.Exists(configFile).Should().BeTrue($"{framework} project should have efcpt-config.json");

        var configContent = await File.ReadAllTextAsync(configFile);
        configContent.Should().Contain("root-namespace",
            $"{framework} project config should have root-namespace");
    }

    #endregion

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

    /// <summary>
    /// Creates a .config/dotnet-tools.json manifest and restores tools.
    /// Required for tool-manifest mode to find the efcpt tool.
    /// </summary>
    private static async Task CreateToolManifestAndRestoreAsync(string testDirectory)
    {
        var configDir = Path.Combine(testDirectory, ".config");
        Directory.CreateDirectory(configDir);

        var toolManifest = @"{
  ""version"": 1,
  ""isRoot"": true,
  ""tools"": {
    ""erikej.efcorepowertools.cli"": {
      ""version"": ""10.1.1055"",
      ""commands"": [
        ""efcpt""
      ],
      ""rollForward"": false
    }
  }
}";
        await File.WriteAllTextAsync(Path.Combine(configDir, "dotnet-tools.json"), toolManifest);

        // Restore tools so they're available for both tool-manifest and dnx modes
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "tool restore",
            WorkingDirectory = testDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        await process.WaitForExitAsync();
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

    [System.Text.RegularExpressions.GeneratedRegex(@"Version=""[0-9]+\.\*""")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
