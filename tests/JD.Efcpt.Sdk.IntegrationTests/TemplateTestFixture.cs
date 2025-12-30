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
        // Use a named mutex to ensure only one process packs at a time across test runs
        using var mutex = new System.Threading.Mutex(false, "Global\\JD.Efcpt.Build.Templates.PackMutex");
        
        try
        {
            // Wait up to 2 minutes for other processes to finish packing
            if (!mutex.WaitOne(TimeSpan.FromMinutes(2)))
            {
                throw new InvalidOperationException("Timeout waiting for template packing mutex");
            }

            // Use the same package output path as AssemblyFixture to share SDK/Build packages
            // This ensures version consistency and avoids packing the same packages twice
            _packageOutputPath = AssemblyFixture.PackageOutputPath;

            var templateProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Build.Templates", "JD.Efcpt.Build.Templates.csproj");

            // Check if package already exists to avoid redundant packing
            var existingPackages = Directory.GetFiles(_packageOutputPath, "JD.Efcpt.Build.Templates.*.nupkg");
            if (existingPackages.Length > 0)
            {
                Console.WriteLine($"Template package already exists at {existingPackages[0]}, skipping pack");
                return existingPackages[0];
            }

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
        finally
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex was not owned by this thread - ignore
            }
        }
    }

    private static async Task PackProjectAsync(string projectPath, string outputPath)
    {
        // Use retry logic with exponential backoff for file locking issues
        const int maxRetries = 3;
        const int baseDelayMs = 1000;
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
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
                    // Check if it's a file locking issue that we should retry
                    if (attempt < maxRetries - 1 && IsFileLockingError(output, error))
                    {
                        var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                        Console.WriteLine($"File locking detected in pack, retrying in {delay}ms (attempt {attempt + 1}/{maxRetries})");
                        await Task.Delay(delay).ConfigureAwait(false);
                        continue;
                    }
                    
                    throw new InvalidOperationException(
                        $"Failed to pack {Path.GetFileName(projectPath)}.\nOutput: {output}\nError: {error}");
                }
                
                // Success - break out of retry loop
                return;
            }
            catch (Exception ex) when (attempt < maxRetries - 1 && IsTransientError(ex))
            {
                var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                Console.WriteLine($"Transient error in pack, retrying in {delay}ms (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }
    
    private static bool IsFileLockingError(string output, string error)
    {
        var combinedOutput = output + error;
        return combinedOutput.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
               combinedOutput.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
               combinedOutput.Contains("cannot access the file", StringComparison.OrdinalIgnoreCase) ||
               combinedOutput.Contains("lock", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool IsTransientError(Exception ex)
    {
        return ex is IOException ||
               ex.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase);
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
    /// <param name="workingDirectory">Directory to create the project in</param>
    /// <param name="projectName">Name of the project to create</param>
    /// <param name="framework">Optional target framework (net8.0, net9.0, or net10.0). Defaults to net8.0 if not specified.</param>
    public async Task<TestUtilities.CommandResult> CreateProjectFromTemplateAsync(
        string workingDirectory,
        string projectName,
        string? framework = null)
    {
        var args = $"efcptbuild --name {projectName}";
        if (!string.IsNullOrEmpty(framework))
        {
            args += $" --Framework {framework}";
        }
        return await RunDotnetNewCommandAsync(workingDirectory, args);
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
    /// Uses retry logic with exponential backoff for file locking resilience.
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
                    DeleteFileWithRetry(file);
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
                DeleteFileWithRetry(contentFile);
            }

            // Also try to delete the entire cache directory for a clean slate
            // This is more aggressive but ensures no stale template registrations
            var cacheFiles = new[] { "templatecache.json", "settings.json" };
            foreach (var file in cacheFiles)
            {
                var filePath = Path.Combine(templateCacheDir, file);
                if (File.Exists(filePath))
                {
                    DeleteFileWithRetry(filePath);
                }
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
    
    /// <summary>
    /// Deletes a file with retry logic for file locking resilience.
    /// </summary>
    private static void DeleteFileWithRetry(string filePath, int maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return; // Success
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                // File is locked, wait and retry
                var delay = 200 * (int)Math.Pow(2, attempt); // 200ms, 400ms, 800ms
                Thread.Sleep(delay);
            }
            catch (UnauthorizedAccessException) when (attempt < maxRetries - 1)
            {
                // Access denied, wait and retry
                var delay = 200 * (int)Math.Pow(2, attempt);
                Thread.Sleep(delay);
            }
            catch
            {
                // Other errors or final attempt - best effort, ignore
                return;
            }
        }
    }
}

[CollectionDefinition("Template Tests", DisableParallelization = true)]
public class TemplateTestCollection : ICollectionFixture<TemplateTestFixture> { }
