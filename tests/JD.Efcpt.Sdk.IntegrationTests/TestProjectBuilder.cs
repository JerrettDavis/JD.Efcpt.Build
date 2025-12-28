using System.Diagnostics;
using System.Text;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Helper class for creating and building test projects.
/// </summary>
public class TestProjectBuilder : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _packageSource;
    private readonly string _sdkVersion;
    private readonly string _buildVersion;

    public string TestDirectory => _testDirectory;
    public string ProjectDirectory { get; private set; } = null!;
    public string GeneratedDirectory => Path.Combine(ProjectDirectory, "obj", "efcpt", "Generated");

    public TestProjectBuilder(SdkPackageTestFixture fixture)
    {
        _packageSource = fixture.PackageOutputPath;
        _sdkVersion = fixture.SdkVersion;
        _buildVersion = fixture.BuildVersion;
        _testDirectory = Path.Combine(Path.GetTempPath(), "SdkTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    /// <summary>
    /// Creates a test project using the SDK.
    /// </summary>
    public void CreateSdkProject(string projectName, string targetFramework, string? additionalContent = null)
    {
        ProjectDirectory = Path.Combine(_testDirectory, projectName);
        Directory.CreateDirectory(ProjectDirectory);

        // Create nuget.config
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{_packageSource}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
        File.WriteAllText(Path.Combine(_testDirectory, "nuget.config"), nugetConfig);

        // Create global.json with SDK version
        var globalJson = $@"{{
  ""msbuild-sdks"": {{
    ""JD.Efcpt.Sdk"": ""{_sdkVersion}""
  }}
}}";
        File.WriteAllText(Path.Combine(_testDirectory, "global.json"), globalJson);

        // Create project file
        var efCoreVersion = GetEfCoreVersionForTargetFramework(targetFramework);
        var projectContent = $@"<Project Sdk=""JD.Efcpt.Sdk"">
    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include=""..\DatabaseProject\DatabaseProject.csproj"">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <OutputItemType>None</OutputItemType>
        </ProjectReference>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""{efCoreVersion}"" />
        <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""{efCoreVersion}"" />
    </ItemGroup>
{additionalContent ?? ""}
</Project>";
        File.WriteAllText(Path.Combine(ProjectDirectory, $"{projectName}.csproj"), projectContent);
    }

    /// <summary>
    /// Creates a test project using PackageReference to JD.Efcpt.Build.
    /// </summary>
    public void CreateBuildPackageProject(string projectName, string targetFramework, string? additionalContent = null)
    {
        ProjectDirectory = Path.Combine(_testDirectory, projectName);
        Directory.CreateDirectory(ProjectDirectory);

        // Create nuget.config
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{_packageSource}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
        File.WriteAllText(Path.Combine(_testDirectory, "nuget.config"), nugetConfig);

        // Create project file using PackageReference
        var efCoreVersion = GetEfCoreVersionForTargetFramework(targetFramework);
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include=""..\DatabaseProject\DatabaseProject.csproj"">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <OutputItemType>None</OutputItemType>
        </ProjectReference>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include=""JD.Efcpt.Build"" Version=""{_buildVersion}"" />
        <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""{efCoreVersion}"" />
        <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""{efCoreVersion}"" />
    </ItemGroup>
{additionalContent ?? ""}
</Project>";
        File.WriteAllText(Path.Combine(ProjectDirectory, $"{projectName}.csproj"), projectContent);
    }

    /// <summary>
    /// Copies the database project to the test directory.
    /// </summary>
    public void CopyDatabaseProject(string fixturesPath)
    {
        var sourceDir = Path.Combine(fixturesPath, "DatabaseProject");
        var destDir = Path.Combine(_testDirectory, "DatabaseProject");

        CopyDirectory(sourceDir, destDir);
    }

    /// <summary>
    /// Runs dotnet restore on the project.
    /// </summary>
    public async Task<BuildResult> RestoreAsync()
    {
        return await RunDotnetAsync("restore", ProjectDirectory);
    }

    /// <summary>
    /// Runs dotnet build on the project.
    /// </summary>
    public async Task<BuildResult> BuildAsync(string? additionalArgs = null)
    {
        var args = "build";
        if (!string.IsNullOrEmpty(additionalArgs))
            args += " " + additionalArgs;

        return await RunDotnetAsync(args, ProjectDirectory);
    }

    /// <summary>
    /// Runs dotnet clean on the project.
    /// </summary>
    public async Task<BuildResult> CleanAsync()
    {
        return await RunDotnetAsync("clean", ProjectDirectory);
    }

    /// <summary>
    /// Gets the list of generated files.
    /// </summary>
    public string[] GetGeneratedFiles()
    {
        if (!Directory.Exists(GeneratedDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(GeneratedDirectory, "*.g.cs", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Checks if a specific generated file exists.
    /// </summary>
    public bool GeneratedFileExists(string relativePath)
    {
        return File.Exists(Path.Combine(GeneratedDirectory, relativePath));
    }

    /// <summary>
    /// Reads the content of a generated file.
    /// </summary>
    public string ReadGeneratedFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(GeneratedDirectory, relativePath));
    }

    private async Task<BuildResult> RunDotnetAsync(string args, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return new BuildResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString()
        };
    }

    /// <summary>
    /// Gets a compatible EF Core version for the target framework.
    /// </summary>
    /// <remarks>
    /// We use specific versions rather than floating versions (like 8.*) because:
    /// 1. NuGet PackageReference doesn't support wildcards in the same way as packages.config
    /// 2. Floating versions can cause non-reproducible builds
    /// 3. Integration tests need predictable package resolution
    /// These versions should be updated periodically to match latest stable releases.
    /// </remarks>
    private static string GetEfCoreVersionForTargetFramework(string targetFramework) =>
        targetFramework switch
        {
            "net8.0" => "8.0.11",
            "net9.0" => "9.0.1",
            "net10.0" => "10.0.1",
            _ => throw new ArgumentException($"Unknown target framework: {targetFramework}")
        };

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}

public class BuildResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = "";
    public string Error { get; init; } = "";
    public bool Success => ExitCode == 0;

    public override string ToString() =>
        $"ExitCode: {ExitCode}\nOutput:\n{Output}\nError:\n{Error}";
}
