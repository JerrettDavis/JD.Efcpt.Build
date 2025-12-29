using System.Diagnostics;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Fixture for template tests that provides access to the packed template package.
/// </summary>
public class TemplateTestFixture : IDisposable
{
    private static readonly Lazy<Task<string>> _templatePackageTask = new(PackTemplatePackageAsync);
    private static string? _templatePackagePath;
    private static string? _packageOutputPath;

    public string TemplatePackagePath => GetTemplatePackagePath();
    public string PackageOutputPath => GetPackageOutputPath();
    public string SdkVersion => AssemblyFixture.SdkVersion;
    public string BuildVersion => AssemblyFixture.BuildVersion;

    private static readonly string RepoRoot = TestUtilities.FindRepoRoot();

    public string GetTestFixturesPath() => AssemblyFixture.TestFixturesPath;

    private static string GetTemplatePackagePath()
    {
        if (_templatePackagePath == null)
        {
            _templatePackagePath = _templatePackageTask.Value.GetAwaiter().GetResult();
        }
        return _templatePackagePath;
    }

    private static string GetPackageOutputPath()
    {
        if (_packageOutputPath == null)
        {
            // Ensure template is packed
            GetTemplatePackagePath();
        }
        return _packageOutputPath!;
    }

    private static async Task<string> PackTemplatePackageAsync()
    {
        _packageOutputPath = Path.Combine(Path.GetTempPath(), "JD.Efcpt.TemplateTests", $"pkg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_packageOutputPath);

        var templateProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Build.Templates", "JD.Efcpt.Build.Templates.csproj");

        await PackProjectAsync(templateProject, _packageOutputPath).ConfigureAwait(false);

        // Find packaged file
        var templatePackages = Directory.GetFiles(_packageOutputPath, "JD.Efcpt.Build.Templates.*.nupkg");

        if (templatePackages.Length == 0)
            throw new InvalidOperationException($"JD.Efcpt.Build.Templates package not found in {_packageOutputPath}");

        var templatePath = templatePackages[0];

        // Also pack SDK and Build packages to the same location for testing
        var sdkProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Sdk", "JD.Efcpt.Sdk.csproj");
        var buildProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Build", "JD.Efcpt.Build.csproj");

        await Task.WhenAll(
            PackProjectAsync(sdkProject, _packageOutputPath),
            PackProjectAsync(buildProject, _packageOutputPath)
        ).ConfigureAwait(false);

        // Register cleanup on process exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(_packageOutputPath, true); } catch { /* best effort */ }
        };

        return templatePath;
    }

    private static async Task PackProjectAsync(string projectPath, string outputPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"pack \"{projectPath}\" -c Release -o \"{outputPath}\" /p:PackageVersion=1.0.0-test",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
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
                $"Pack of {Path.GetFileName(projectPath)} timed out after 5 minutes.");
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to pack {Path.GetFileName(projectPath)}.\nOutput: {output}\nError: {error}");
        }
    }

    /// <summary>
    /// Installs the template package using dotnet new install.
    /// </summary>
    public async Task<TestUtilities.CommandResult> InstallTemplateAsync(string workingDirectory)
    {
        return await RunDotnetNewCommandAsync(workingDirectory, $"install \"{TemplatePackagePath}\"");
    }

    /// <summary>
    /// Uninstalls the template package using dotnet new uninstall.
    /// </summary>
    public async Task<TestUtilities.CommandResult> UninstallTemplateAsync(string workingDirectory)
    {
        return await RunDotnetNewCommandAsync(workingDirectory, "uninstall JD.Efcpt.Build.Templates");
    }

    /// <summary>
    /// Creates a project from the template using dotnet new efcptbuild.
    /// </summary>
    public async Task<TestUtilities.CommandResult> CreateProjectFromTemplateAsync(string workingDirectory, string projectName)
    {
        return await RunDotnetNewCommandAsync(workingDirectory, $"efcptbuild --name {projectName}");
    }

    private static async Task<TestUtilities.CommandResult> RunDotnetNewCommandAsync(string workingDirectory, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new {arguments}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new InvalidOperationException($"dotnet new {arguments} timed out after 2 minutes.");
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

    public void Dispose()
    {
        // Cleanup is handled by AppDomain.ProcessExit
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition("Template Tests", DisableParallelization = true)]
public class TemplateTestCollection : ICollectionFixture<TemplateTestFixture> { }
