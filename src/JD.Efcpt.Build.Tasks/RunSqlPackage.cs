using System.Diagnostics;
using System.Text;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that invokes sqlpackage to extract database schema to SQL scripts.
/// </summary>
/// <remarks>
/// <para>
/// This task is invoked from the SqlProj generation pipeline to extract schema from a live database.
/// It executes the sqlpackage CLI to generate SQL script files that represent the database schema.
/// </para>
/// <para>
/// Tool resolution follows this order:
/// <list type="number">
///   <item>
///     <description>
///       If <see cref="ToolPath"/> is a non-empty explicit path, that executable is run directly.
///     </description>
///   </item>
///   <item>
///     <description>
///       When the project targets .NET 10.0 or later, the .NET 10+ SDK is installed, and dnx is available,
///       the task runs <c>dnx microsoft.sqlpackage</c> to execute the tool without requiring installation.
///     </description>
///   </item>
///   <item>
///     <description>
///       Otherwise the global tool path is used. When <see cref="ToolRestore"/> evaluates to <c>true</c>,
///       the task runs <c>dotnet tool update --global microsoft.sqlpackage</c>, then invokes
///       <c>sqlpackage</c> directly.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public sealed class RunSqlPackage : Task
{
    /// <summary>
    /// Timeout in milliseconds for external process operations (SDK checks, dnx availability).
    /// </summary>
    private const int ProcessTimeoutMs = 5000;

    /// <summary>
    /// Package identifier of the sqlpackage dotnet tool.
    /// </summary>
    private const string SqlPackageToolPackageId = "microsoft.sqlpackage";

    /// <summary>
    /// Command name for sqlpackage.
    /// </summary>
    private const string SqlPackageCommand = "sqlpackage";

    /// <summary>
    /// Optional version constraint for the sqlpackage tool package.
    /// </summary>
    public string ToolVersion { get; set; } = "";

    /// <summary>
    /// Indicates whether the task should restore or update the dotnet tool before running it.
    /// </summary>
    public string ToolRestore { get; set; } = "true";

    /// <summary>
    /// Explicit path to the sqlpackage executable.
    /// </summary>
    public string ToolPath { get; set; } = "";

    /// <summary>
    /// Path to the <c>dotnet</c> host executable.
    /// </summary>
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>
    /// Working directory for the sqlpackage invocation.
    /// </summary>
    [Required]
    public string WorkingDirectory { get; set; } = "";

    /// <summary>
    /// Connection string for the source database.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Target directory where SQL scripts will be extracted.
    /// </summary>
    [Required]
    public string TargetDirectory { get; set; } = "";

    /// <summary>
    /// Extract target mode: "Flat" for SQL scripts, "File" for DACPAC.
    /// </summary>
    public string ExtractTarget { get; set; } = "Flat";

    /// <summary>
    /// Target framework being built (for example <c>net8.0</c>, <c>net9.0</c>, <c>net10.0</c>).
    /// </summary>
    public string TargetFramework { get; set; } = "";

    /// <summary>
    /// Log verbosity level.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Output parameter: Target directory where extraction occurred.
    /// </summary>
    [Output]
    public string ExtractedPath { get; set; } = "";

    /// <summary>
    /// Executes the task.
    /// </summary>
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);

        try
        {
            log.Info($"Starting SqlPackage extract operation (ExtractTarget={ExtractTarget})");

            // Create target directory if it doesn't exist
            if (!Directory.Exists(TargetDirectory))
            {
                Directory.CreateDirectory(TargetDirectory);
                log.Detail($"Created target directory: {TargetDirectory}");
            }

            // Set the output path
            ExtractedPath = TargetDirectory;

            // Check for test hook
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EFCPT_FAKE_SQLPACKAGE")))
            {
                log.Info("EFCPT_FAKE_SQLPACKAGE is set - skipping actual sqlpackage execution");
                CreateFakeOutput();
                return true;
            }

            // Resolve tool path
            var toolInfo = ResolveToolPath(log);
            if (toolInfo == null)
            {
                return false;
            }

            // Build sqlpackage command arguments
            var args = BuildSqlPackageArguments();

            // Execute sqlpackage
            var success = ExecuteSqlPackage(toolInfo.Value, args, log);

            if (success)
            {
                log.Info("SqlPackage extract completed successfully");
            }
            else
            {
                log.Error("SqlPackage extract failed");
            }

            return success;
        }
        catch (Exception ex)
        {
            log.Error($"SqlPackage execution failed: {ex.Message}");
            log.Detail($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Resolves the tool path for sqlpackage execution.
    /// </summary>
    private (string Executable, string Arguments)? ResolveToolPath(IBuildLog log)
    {
        // Explicit path override
        if (!string.IsNullOrEmpty(ToolPath))
        {
            var resolvedPath = Path.IsPathRooted(ToolPath)
                ? ToolPath
                : Path.GetFullPath(Path.Combine(WorkingDirectory, ToolPath));

            if (!File.Exists(resolvedPath))
            {
                log.Error($"Explicit tool path does not exist: {resolvedPath}");
                return null;
            }

            log.Info($"Using explicit sqlpackage path: {resolvedPath}");
            return (resolvedPath, string.Empty);
        }

        // Check for .NET 10+ SDK with dnx support
        if (IsDnxAvailable(log) && IsTargetFrameworkNet10OrLater())
        {
            log.Info($"Using dnx to execute {SqlPackageToolPackageId}");
            return (DotNetExe, $"dnx {SqlPackageToolPackageId}");
        }

        // Use global tool
        if (ShouldRestoreTool())
        {
            RestoreGlobalTool(log);
        }

        log.Info("Using global sqlpackage tool");
        return (SqlPackageCommand, string.Empty);
    }

    /// <summary>
    /// Checks if dnx is available.
    /// </summary>
    private bool IsDnxAvailable(IBuildLog log)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = DotNetExe,
                Arguments = "dnx --help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            var completed = process.WaitForExit(ProcessTimeoutMs);
            
            // If timeout expired, kill the process to prevent resource leaks
            if (!completed)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Process may have already exited
                }
                return false;
            }
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the target framework is .NET 10 or later.
    /// </summary>
    private bool IsTargetFrameworkNet10OrLater()
    {
        if (string.IsNullOrEmpty(TargetFramework))
        {
            return false;
        }

        // Parse target framework (e.g., "net10.0", "net9.0", "net8.0")
        // Only handle modern .NET (net5.0+), not .NET Framework or .NET Standard
        if (TargetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
        {
            var versionPart = TargetFramework.Substring(3);
            
            // Skip .NET Standard and .NET Framework patterns
            if (versionPart.StartsWith("standard", StringComparison.OrdinalIgnoreCase) ||
                versionPart.StartsWith("coreapp", StringComparison.OrdinalIgnoreCase) ||
                versionPart.StartsWith("framework", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            
            // Extract major version (e.g., "10" from "10.0" or "net10.0")
            if (versionPart.Contains('.'))
            {
                versionPart = versionPart.Split('.')[0];
            }

            if (int.TryParse(versionPart, out var majorVersion))
            {
                return majorVersion >= 10;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if tool restore should be performed.
    /// </summary>
    private bool ShouldRestoreTool()
    {
        if (string.IsNullOrEmpty(ToolRestore))
        {
            return true;
        }

        var normalized = ToolRestore.Trim().ToLowerInvariant();
        return normalized == "true" || normalized == "1" || normalized == "yes";
    }

    /// <summary>
    /// Restores the global sqlpackage tool.
    /// </summary>
    private void RestoreGlobalTool(IBuildLog log)
    {
        log.Info($"Restoring global tool: {SqlPackageToolPackageId}");

        var versionArg = !string.IsNullOrEmpty(ToolVersion) ? $" --version {ToolVersion}" : "";
        var arguments = $"tool update --global {SqlPackageToolPackageId}{versionArg}";

        var psi = new ProcessStartInfo
        {
            FileName = DotNetExe,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = WorkingDirectory
        };

        log.Detail($"Running: {DotNetExe} {arguments}");

        using var process = Process.Start(psi);
        if (process == null)
        {
            log.Warn("Failed to start tool restore process");
            return;
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            log.Warn($"Tool restore completed with exit code {process.ExitCode}");
            if (!string.IsNullOrEmpty(error))
            {
                log.Detail($"Restore stderr: {error}");
            }
        }
        else
        {
            log.Detail("Tool restore completed successfully");
        }
    }

    /// <summary>
    /// Builds the command-line arguments for sqlpackage.
    /// </summary>
    private string BuildSqlPackageArguments()
    {
        var args = new StringBuilder();

        // Action: Extract
        args.Append("/Action:Extract ");

        // Source connection string
        args.Append($"/SourceConnectionString:\"{ConnectionString}\" ");

        // Target file or directory
        args.Append($"/TargetFile:\"{TargetDirectory}\" ");

        // Extract target mode (Flat for SQL scripts, File for DACPAC)
        args.Append($"/p:ExtractTarget={ExtractTarget} ");

        // Properties for application-scoped objects only
        args.Append("/p:ExtractApplicationScopedObjectsOnly=True ");

        return args.ToString().Trim();
    }

    /// <summary>
    /// Executes sqlpackage with the specified arguments.
    /// </summary>
    private bool ExecuteSqlPackage((string Executable, string Arguments) toolInfo, string sqlPackageArgs, IBuildLog log)
    {
        var fullArgs = string.IsNullOrEmpty(toolInfo.Arguments)
            ? sqlPackageArgs
            : $"{toolInfo.Arguments} {sqlPackageArgs}";

        var psi = new ProcessStartInfo
        {
            FileName = toolInfo.Executable,
            Arguments = fullArgs,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = WorkingDirectory
        };

        log.Detail($"Running: {toolInfo.Executable} {fullArgs}");

        using var process = Process.Start(psi);
        if (process == null)
        {
            log.Error("Failed to start sqlpackage process");
            return false;
        }

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine(e.Data);
                log.Detail(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error.AppendLine(e.Data);
                log.Detail(e.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            log.Error($"SqlPackage failed with exit code {process.ExitCode}");
            if (error.Length > 0)
            {
                log.Error($"SqlPackage error output:\n{error}");
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates fake output for testing purposes.
    /// </summary>
    private void CreateFakeOutput()
    {
        if (ExtractTarget == "Flat")
        {
            // Create fake SQL script structure
            var dboTablesDir = Path.Combine(TargetDirectory, "dbo", "Tables");
            Directory.CreateDirectory(dboTablesDir);
            File.WriteAllText(Path.Combine(dboTablesDir, "TestTable.sql"), "-- FAKE SQL SCRIPT FOR TESTING");
        }
        else
        {
            // Create fake DACPAC file
            File.WriteAllText(Path.Combine(TargetDirectory, "extracted.dacpac"), "FAKE DACPAC FOR TESTING");
        }
    }
}
