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
    private static bool _templateInstalled = false;
    private static readonly object _installLock = new();
    private static int _instanceCount = 0;

    public string TemplatePackagePath => GetTemplatePackagePath();
    public string PackageOutputPath => GetPackageOutputPath();
    public string SdkVersion => AssemblyFixture.SdkVersion;
    public string BuildVersion => AssemblyFixture.BuildVersion;

    private static readonly string RepoRoot = TestUtilities.FindRepoRoot();

    public TemplateTestFixture()
    {
        var instanceNum = System.Threading.Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"TemplateTestFixture instance #{instanceNum} created");
        
        // Cleanup any previously installed templates to avoid conflicts
        // Only do this for the first instance
        if (instanceNum == 1)
        {
            CleanupInstalledTemplates();
        }
        
        // Install the template once for all tests in the collection
        EnsureTemplateInstalled();
    }

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
        // Use the same package output path as AssemblyFixture to share SDK/Build packages
        // This ensures version consistency and avoids packing the same packages twice
        _packageOutputPath = AssemblyFixture.PackageOutputPath;

        var templateProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Build.Templates", "JD.Efcpt.Build.Templates.csproj");

        // Pack template with the same version as SDK/Build packages from AssemblyFixture
        await PackProjectAsync(templateProject, _packageOutputPath).ConfigureAwait(false);

        // Find packaged file
        var templatePackages = Directory.GetFiles(_packageOutputPath, "JD.Efcpt.Build.Templates.*.nupkg");

        if (templatePackages.Length == 0)
            throw new InvalidOperationException($"JD.Efcpt.Build.Templates package not found in {_packageOutputPath}");

        var templatePath = templatePackages[0];

        // SDK and Build packages are already available from AssemblyFixture
        // No need to pack them again - this avoids version mismatches and file locking

        return templatePath;
    }

    private static async Task PackProjectAsync(string projectPath, string outputPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"pack \"{projectPath}\" -c Release -o \"{outputPath}\"",
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
    /// Ensures the template is installed once for all tests.
    /// </summary>
    private void EnsureTemplateInstalled()
    {
        lock (_installLock)
        {
            if (!_templateInstalled)
            {
                try
                {
                    var result = InstallTemplateAsync(Path.GetTempPath()).GetAwaiter().GetResult();
                    if (!result.Success)
                    {
                        var errorMessage = $"Failed to install template in fixture setup.\nExit Code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.Error}";
                        Console.WriteLine(errorMessage); // Log to console for debugging
                        throw new InvalidOperationException(errorMessage);
                    }
                    _templateInstalled = true;
                    Console.WriteLine("Template installed successfully in fixture setup");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during template installation: {ex}");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Installs the template package using dotnet new install.
    /// This is called automatically by the fixture, but can be called directly for testing.
    /// </summary>
    public async Task<TestUtilities.CommandResult> InstallTemplateAsync(string workingDirectory)
    {
        // Use --force to overwrite existing template package files in ~/.templateengine/packages/
        return await RunDotnetNewCommandAsync(workingDirectory, $"install \"{TemplatePackagePath}\" --force");
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
        // Cleanup any installed templates
        CleanupInstalledTemplates();
        
        // Cleanup is handled by AppDomain.ProcessExit
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Removes any previously installed template packages to avoid conflicts.
    /// </summary>
    private static void CleanupInstalledTemplates()
    {
        try
        {
            // Run dotnet new uninstall to remove the template
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new uninstall JD.Efcpt.Build.Templates",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(10000); // 10 second timeout
            }
        }
        catch
        {
            // Best effort cleanup - ignore errors if template wasn't installed
        }

        // Also remove the cached package file to avoid "File already exists" errors
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var templatePackagesDir = Path.Combine(userProfile, ".templateengine", "packages");
            if (Directory.Exists(templatePackagesDir))
            {
                var packageFiles = Directory.GetFiles(templatePackagesDir, "JD.Efcpt.Build.Templates.*.nupkg");
                foreach (var file in packageFiles)
                {
                    try { File.Delete(file); } catch { /* best effort */ }
                }
            }
        }
        catch
        {
            // Best effort cleanup
        }

        // Clear template engine cache to avoid "Sequence contains more than one matching element" errors
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var templateCacheDir = Path.Combine(userProfile, ".templateengine");
            
            // Delete the template cache content file which can have stale entries
            var contentFile = Path.Combine(templateCacheDir, "content");
            if (File.Exists(contentFile))
            {
                try { File.Delete(contentFile); } catch { /* best effort */ }
            }

            // Also try to delete the entire cache directory for a clean slate
            // This is more aggressive but ensures no stale template registrations
            var cacheFiles = new[] { "templatecache.json", "settings.json" };
            foreach (var file in cacheFiles)
            {
                var filePath = Path.Combine(templateCacheDir, file);
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { /* best effort */ }
                }
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}

[CollectionDefinition("Template Tests", DisableParallelization = true)]
public class TemplateTestCollection : ICollectionFixture<TemplateTestFixture> { }
