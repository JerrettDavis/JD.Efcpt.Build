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
    private readonly string _sharedDatabaseProjectPath;

    public string TestDirectory => _testDirectory;
    public string ProjectDirectory { get; private set; } = null!;
    public string GeneratedDirectory => Path.Combine(ProjectDirectory, "obj", "efcpt", "Generated");

    public TestProjectBuilder(SdkPackageTestFixture fixture)
    {
        _packageSource = fixture.PackageOutputPath;
        _sdkVersion = fixture.SdkVersion;
        _buildVersion = fixture.BuildVersion;
        _sharedDatabaseProjectPath = fixture.SharedDatabaseProjectPath;
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

        // Create nuget.config with shared global packages folder for caching
        var globalPackagesFolder = GetSharedGlobalPackagesFolder();
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{_packageSource}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
  <config>
    <add key=""globalPackagesFolder"" value=""{globalPackagesFolder}"" />
  </config>
</configuration>";
        File.WriteAllText(Path.Combine(_testDirectory, "nuget.config"), nugetConfig);

        // Create global.json with SDK version
        var globalJson = $@"{{
  ""msbuild-sdks"": {{
    ""JD.Efcpt.Sdk"": ""{_sdkVersion}""
  }}
}}";
        File.WriteAllText(Path.Combine(_testDirectory, "global.json"), globalJson);

        // Create project file using shared database project (absolute path)
        var efCoreVersion = GetEfCoreVersionForTargetFramework(targetFramework);
        var dbProjectPath = Path.Combine(_sharedDatabaseProjectPath, "DatabaseProject.csproj").Replace("\\", "/");
        var projectContent = $@"<Project Sdk=""JD.Efcpt.Sdk"">
    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include=""{dbProjectPath}"">
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

        // Create nuget.config with shared global packages folder for caching
        var globalPackagesFolder = GetSharedGlobalPackagesFolder();
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""TestPackages"" value=""{_packageSource}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
  <config>
    <add key=""globalPackagesFolder"" value=""{globalPackagesFolder}"" />
  </config>
</configuration>";
        File.WriteAllText(Path.Combine(_testDirectory, "nuget.config"), nugetConfig);

        // Create project file using shared database project (absolute path)
        var efCoreVersion = GetEfCoreVersionForTargetFramework(targetFramework);
        var dbProjectPath = Path.Combine(_sharedDatabaseProjectPath, "DatabaseProject.csproj").Replace("\\", "/");
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include=""{dbProjectPath}"">
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
    /// No-op: Database project is now shared across all tests via AssemblyFixture.
    /// This method is kept for backwards compatibility but does nothing.
    /// The database project is set up once by AssemblyFixture and referenced via absolute path.
    /// </summary>
    public void CopyDatabaseProject(string fixturesPath)
    {
        // No-op: The database project is now shared across all tests.
    }

    /// <summary>
    /// Runs dotnet restore on the project.
    /// Only call this if you need to restore without building.
    /// BuildAsync() handles restore automatically.
    /// </summary>
    public async Task<BuildResult> RestoreAsync()
    {
        return await RunDotnetAsync("restore", ProjectDirectory);
    }

    /// <summary>
    /// Runs dotnet build on the project.
    /// By default, this includes restore (standard dotnet behavior).
    /// Set noRestore=true if you've already called RestoreAsync().
    /// </summary>
    public async Task<BuildResult> BuildAsync(string? additionalArgs = null, bool noRestore = false)
    {
        var args = "build";
        if (noRestore)
            args += " --no-restore";
        if (!string.IsNullOrEmpty(additionalArgs))
            args += " " + additionalArgs;

        return await RunDotnetAsync(args, ProjectDirectory);
    }

    /// <summary>
    /// Runs dotnet build with restore in a single operation.
    /// This is more efficient than calling RestoreAsync() + BuildAsync() separately.
    /// </summary>
    public async Task<BuildResult> RestoreAndBuildAsync(string? additionalArgs = null)
    {
        // dotnet build already does restore, so just call build
        return await BuildAsync(additionalArgs, noRestore: false);
    }

    /// <summary>
    /// Runs dotnet clean on the project.
    /// </summary>
    public async Task<BuildResult> CleanAsync()
    {
        return await RunDotnetAsync("clean", ProjectDirectory);
    }

    /// <summary>
    /// Runs MSBuild.exe (Framework MSBuild) on the project.
    /// This tests the Framework MSBuild fallback mechanism.
    /// </summary>
    public async Task<BuildResult> BuildWithMSBuildExeAsync(string? additionalArgs = null)
    {
        var msbuildPath = FindMSBuildExe();
        if (msbuildPath == null)
            throw new InvalidOperationException("MSBuild.exe not found. Visual Studio must be installed.");

        // Find the actual project file
        var projectFiles = Directory.GetFiles(ProjectDirectory, "*.csproj");
        if (projectFiles.Length == 0)
            throw new InvalidOperationException($"No .csproj file found in {ProjectDirectory}");

        var projectFile = projectFiles[0];
        var args = $"\"{projectFile}\" -restore";
        if (!string.IsNullOrEmpty(additionalArgs))
            args += " " + additionalArgs;

        return await RunProcessAsync(msbuildPath, args, ProjectDirectory);
    }

    /// <summary>
    /// Checks if MSBuild.exe is available on this machine.
    /// </summary>
    public static bool IsMSBuildExeAvailable() => FindMSBuildExe() != null;

    private static string? FindMSBuildExe()
    {
        // Common Visual Studio installation paths
        var vsBasePaths = new[]
        {
            @"C:\Program Files\Microsoft Visual Studio",
            @"C:\Program Files (x86)\Microsoft Visual Studio"
        };

        var editions = new[] { "Enterprise", "Professional", "Community", "BuildTools" };
        var years = new[] { "2022", "2019", "18" }; // 18 is VS 2022 preview naming

        foreach (var basePath in vsBasePaths)
        {
            if (!Directory.Exists(basePath)) continue;

            foreach (var year in years)
            {
                foreach (var edition in editions)
                {
                    var msbuildPath = Path.Combine(basePath, year, edition, "MSBuild", "Current", "Bin", "MSBuild.exe");
                    if (File.Exists(msbuildPath))
                        return msbuildPath;

                    // Also check amd64 folder
                    msbuildPath = Path.Combine(basePath, year, edition, "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe");
                    if (File.Exists(msbuildPath))
                        return msbuildPath;
                }
            }
        }

        return null;
    }

    private async Task<BuildResult> RunProcessAsync(string fileName, string args, string workingDirectory, int timeoutMs = 300000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
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

        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return new BuildResult
            {
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = errorBuilder + $"\n[TIMEOUT] Process exceeded {timeoutMs / 1000}s timeout and was killed."
            };
        }

        return new BuildResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString()
        };
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
    /// Adds a property to the project file's PropertyGroup.
    /// </summary>
    public void AddProjectProperty(string propertyName, string propertyValue)
    {
        var projectFiles = Directory.GetFiles(ProjectDirectory, "*.csproj");
        if (projectFiles.Length == 0)
            throw new InvalidOperationException($"No .csproj file found in {ProjectDirectory}");

        var projectFile = projectFiles[0];
        var content = File.ReadAllText(projectFile);

        // Find the first PropertyGroup and add the property inside it
        var propertyGroupEnd = content.IndexOf("</PropertyGroup>", StringComparison.OrdinalIgnoreCase);
        if (propertyGroupEnd < 0)
            throw new InvalidOperationException("No PropertyGroup found in project file");

        var propertyElement = $"        <{propertyName}>{propertyValue}</{propertyName}>\n    ";
        content = content.Insert(propertyGroupEnd, propertyElement);

        File.WriteAllText(projectFile, content);
    }

    /// <summary>
    /// Reads the content of a generated file.
    /// </summary>
    public string ReadGeneratedFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(GeneratedDirectory, relativePath));
    }

    private async Task<BuildResult> RunDotnetAsync(string args, string workingDirectory, int timeoutMs = 300000)
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

        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            return new BuildResult
            {
                ExitCode = -1,
                Output = outputBuilder.ToString(),
                Error = errorBuilder + $"\n[TIMEOUT] Process exceeded {timeoutMs / 1000}s timeout and was killed."
            };
        }

        return new BuildResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString()
        };
    }

    /// <summary>
    /// Gets the shared global packages folder path.
    /// Uses the standard NuGet global packages folder to share cached packages across test runs.
    /// </summary>
    private static string GetSharedGlobalPackagesFolder()
    {
        // Use the standard NuGet global packages folder
        // This is typically ~/.nuget/packages or %USERPROFILE%\.nuget\packages on Windows
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
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
