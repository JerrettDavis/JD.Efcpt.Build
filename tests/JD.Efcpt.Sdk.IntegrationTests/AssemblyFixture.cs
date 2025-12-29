using System.Diagnostics;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Static assembly-level fixture that packs the SDK packages once for all tests.
/// Uses lazy initialization to ensure thread-safe one-time setup.
/// This runs before any tests and the packages are shared across all test classes.
/// </summary>
public static class AssemblyFixture
{
    private static readonly Lazy<Task<PackageInfo>> _packageInfoTask = new(PackPackagesAsync);
    private static PackageInfo? _packageInfo;

    public static string PackageOutputPath => GetPackageInfo().OutputPath;
    public static string SdkPackagePath => GetPackageInfo().SdkPath;
    public static string BuildPackagePath => GetPackageInfo().BuildPath;
    public static string SdkVersion => GetPackageInfo().SdkVersion;
    public static string BuildVersion => GetPackageInfo().BuildVersion;
    public static string SharedDatabaseProjectPath => GetPackageInfo().SharedDatabaseProjectPath;
    public static string TestFixturesPath => Path.Combine(
        Path.GetDirectoryName(typeof(AssemblyFixture).Assembly.Location)!, "TestFixtures");

    private static readonly string RepoRoot = FindRepoRoot();

    private static PackageInfo GetPackageInfo()
    {
        if (_packageInfo == null)
        {
            // Block synchronously to ensure initialization completes
            // This is safe because we're using Lazy<Task> which ensures one-time execution
            _packageInfo = _packageInfoTask.Value.GetAwaiter().GetResult();
        }
        return _packageInfo;
    }

    private static async Task<PackageInfo> PackPackagesAsync()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), "JD.Efcpt.Sdk.IntegrationTests", $"pkg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputPath);

        var sdkProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Sdk", "JD.Efcpt.Sdk.csproj");
        var buildProject = Path.Combine(RepoRoot, "src", "JD.Efcpt.Build", "JD.Efcpt.Build.csproj");

        // Pack sequentially to avoid file conflicts on shared dependencies (JD.Efcpt.Build.Tasks)
        // Both projects reference the Tasks project, and parallel pack causes obj/ folder conflicts
        await PackProjectAsync(buildProject, outputPath);
        await PackProjectAsync(sdkProject, outputPath);

        // Create shared database project directory (copied once, used by all tests)
        var sharedDbProjectPath = Path.Combine(outputPath, "SharedDatabaseProject");
        var sourceDbProject = Path.Combine(TestFixturesPath, "DatabaseProject");
        CopyDirectory(sourceDbProject, sharedDbProjectPath);

        // Find packaged files
        var sdkPackages = Directory.GetFiles(outputPath, "JD.Efcpt.Sdk.*.nupkg");
        var buildPackages = Directory.GetFiles(outputPath, "JD.Efcpt.Build.*.nupkg");

        if (sdkPackages.Length == 0)
            throw new InvalidOperationException($"JD.Efcpt.Sdk package not found in {outputPath}");
        if (buildPackages.Length == 0)
            throw new InvalidOperationException($"JD.Efcpt.Build package not found in {outputPath}");

        var sdkPath = sdkPackages[0];
        var buildPath = buildPackages[0];

        // Register cleanup on process exit
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try { Directory.Delete(outputPath, true); } catch { /* best effort */ }
        };

        return new PackageInfo(
            outputPath,
            sdkPath,
            buildPath,
            ExtractVersion(Path.GetFileName(sdkPath), "JD.Efcpt.Sdk"),
            ExtractVersion(Path.GetFileName(buildPath), "JD.Efcpt.Build"),
            sharedDbProjectPath
        );
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
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new InvalidOperationException(
                $"Pack of {Path.GetFileName(projectPath)} timed out after 5 minutes.");
        }

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to pack {Path.GetFileName(projectPath)}.\nOutput: {output}\nError: {error}");
        }
    }

    private static string ExtractVersion(string fileName, string packageId)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var prefix = packageId + ".";
        if (withoutExtension.StartsWith(prefix))
            return withoutExtension[prefix.Length..];
        throw new InvalidOperationException($"Could not extract version from {fileName}");
    }

    private static string FindRepoRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "JD.Efcpt.Build.sln")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        var assemblyLocation = typeof(AssemblyFixture).Assembly.Location;
        current = Path.GetDirectoryName(assemblyLocation);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "JD.Efcpt.Build.sln")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not find repository root");
    }

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

    private sealed record PackageInfo(
        string OutputPath,
        string SdkPath,
        string BuildPath,
        string SdkVersion,
        string BuildVersion,
        string SharedDatabaseProjectPath);
}
