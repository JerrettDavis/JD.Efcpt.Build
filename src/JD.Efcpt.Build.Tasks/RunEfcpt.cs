using System.Diagnostics;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using PatternKit.Behavioral.Strategy;
using Task = Microsoft.Build.Utilities.Task;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that invokes the EF Core Power Tools CLI (efcpt) using one of several dotnet tool modes.
/// </summary>
/// <remarks>
/// <para>
/// This task is typically invoked from the <c>EfcptGenerateModels</c> MSBuild target defined in
/// <c>JD.Efcpt.Build</c>. It executes the efcpt CLI against a DACPAC and configuration files in order to
/// generate EF Core model C# files into <see cref="OutputDir"/>.
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
///       the task runs <c>dnx &lt;ToolPackageId&gt;</c> to execute the tool without requiring installation.
///     </description>
///   </item>
///   <item>
///     <description>
///       Otherwise, if <see cref="ToolMode"/> is <c>tool-manifest</c>, or is <c>auto</c> and a
///       <c>.config/dotnet-tools.json</c> file is found by walking up from <see cref="WorkingDirectory"/>,
///       the task runs <c>dotnet tool run &lt;ToolCommand&gt;</c> using the discovered manifest. When
///       <see cref="ToolRestore"/> evaluates to <c>true</c>, <c>dotnet tool restore</c> is run first.
///     </description>
///   </item>
///   <item>
///     <description>
///       Otherwise the global tool path is used. When <see cref="ToolRestore"/> evaluates to <c>true</c>
///       and <see cref="ToolPackageId"/> has a value, the task runs <c>dotnet tool update --global</c>
///       for the specified package (and optional <see cref="ToolVersion"/>), then invokes
///       <see cref="ToolCommand"/> directly.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The task always creates <see cref="WorkingDirectory"/> and <see cref="OutputDir"/> before invoking the
/// external tool. All paths passed to efcpt are absolute.
/// </para>
/// <para>
/// For test and troubleshooting scenarios, the following environment variables are honoured:
/// <list type="bullet">
///   <item>
///     <description>
///       <c>EFCPT_FAKE_EFCPT</c> - when set to a non-empty value, the task does not invoke any
///       external process. Instead it writes a single <c>SampleModel.cs</c> file into
///       <see cref="OutputDir"/> and returns success.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>EFCPT_TEST_DACPAC</c> - if present, its value is forwarded to the child process as an
///       environment variable of the same name. This is primarily used by the test suite.
///     </description>
///   </item>
/// </list>
/// These hooks are intended for testing and diagnostics and are not considered a stable public API.
/// </para>
/// </remarks>
public sealed class RunEfcpt : Task
{
    /// <summary>
    /// Timeout in milliseconds for external process operations (SDK checks, dnx availability).
    /// </summary>
    private const int ProcessTimeoutMs = 5000;

    /// <summary>
    /// Controls how the efcpt dotnet tool is resolved.
    /// </summary>
    /// <value>
    /// One of:
    /// <list type="bullet">
    ///   <item><description><c>auto</c> (default) - use a local tool manifest if one is discovered by walking up from <see cref="WorkingDirectory"/>; otherwise fall back to the global tool.</description></item>
    ///   <item><description><c>tool-manifest</c> - require a local tool manifest; the task will run within the directory that contains <c>.config/dotnet-tools.json</c>.</description></item>
    ///   <item><description>Any other non-empty value behaves like the global tool mode but is reserved for future extension.</description></item>
    /// </list>
    /// </value>
    [Required]
    public string ToolMode { get; set; } = "auto";

    /// <summary>
    /// Package identifier of the efcpt dotnet tool used when restoring or updating the global tool.
    /// </summary>
    /// <value>
    /// Defaults to <c>ErikEJ.EFCorePowerTools.Cli</c>. Only used when <see cref="ToolMode"/> selects the
    /// global tool path and <see cref="ToolRestore"/> evaluates to <c>true</c>.
    /// </value>
    [Required]
    public string ToolPackageId { get; set; } = "ErikEJ.EFCorePowerTools.Cli";

    /// <summary>
    /// Optional version constraint for the efcpt tool package.
    /// </summary>
    /// <value>
    /// When non-empty and the task performs a global tool restore, the value is passed as a
    /// <c>--version</c> argument. When empty, the latest available version is used.
    /// </value>
    public string ToolVersion { get; set; } = "";

    /// <summary>
    /// Indicates whether the task should restore or update the dotnet tool before running it.
    /// </summary>
    /// <value>
    /// The value is interpreted case-insensitively. The strings <c>true</c>, <c>1</c>, and <c>yes</c>
    /// enable restore; any other value disables it. Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// When the project targets .NET 10.0 or later and the .NET 10+ SDK is installed, tool restoration
    /// is skipped even when this property is <c>true</c> because the <c>dnx</c> command handles tool
    /// execution directly without requiring prior installation. The tool is fetched and run on-demand
    /// by the dotnet SDK.
    /// </para>
    /// </remarks>
    public string ToolRestore { get; set; } = "true";

    /// <summary>
    /// Name of the efcpt tool command to execute.
    /// </summary>
    /// <value>
    /// Defaults to <c>efcpt</c>. When running under a tool manifest, the command is executed via
    /// <c>dotnet tool run</c>. In global mode the command name is executed directly.
    /// </value>
    public string ToolCommand { get; set; } = "efcpt";

    /// <summary>
    /// Explicit path to the efcpt executable.
    /// </summary>
    /// <value>
    /// When non-empty and contains a rooted or relative directory component, this path is resolved
    /// against <see cref="WorkingDirectory"/> and executed directly, bypassing dotnet tool resolution.
    /// </value>
    public string ToolPath { get; set; } = "";

    /// <summary>
    /// Path to the <c>dotnet</c> host executable.
    /// </summary>
    /// <value>
    /// Defaults to <c>dotnet</c>. Used for <c>dotnet tool</c> operations and, where applicable,
    /// when invoking the tool via a manifest.
    /// </value>
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>
    /// Working directory for the efcpt invocation and manifest discovery.
    /// </summary>
    /// <value>
    /// Typically points at the intermediate output directory created by earlier pipeline stages.
    /// The directory is created if it does not already exist.
    /// </value>
    [Required]
    public string WorkingDirectory { get; set; } = "";

    /// <summary>
    /// Full path to the DACPAC file that efcpt will inspect (used in .sqlproj mode).
    /// </summary>
    public string DacpacPath { get; set; } = "";

    /// <summary>
    /// Connection string for database connection (used in connection string mode).
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Indicates whether to use connection string mode (true) or DACPAC mode (false).
    /// </summary>
    public string UseConnectionStringMode { get; set; } = "false";

    /// <summary>
    /// Full path to the efcpt configuration JSON file.
    /// </summary>
    [Required]
    public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Full path to the efcpt renaming JSON file.
    /// </summary>
    [Required]
    public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Path to the template directory that contains the C# template files used by efcpt.
    /// </summary>
    [Required]
    public string TemplateDir { get; set; } = "";

    /// <summary>
    /// Directory where generated C# model files will be written.
    /// </summary>
    /// <value>
    /// The directory is created if it does not exist. Generated files are later renamed to
    /// <c>.g.cs</c> and added to compilation by the <c>EfcptAddToCompile</c> target.
    /// </value>
    [Required]
    public string OutputDir { get; set; } = "";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    /// <value>
    /// When set to <c>detailed</c> (case-insensitive), additional informational messages are emitted.
    /// Any other value results in a minimal log. Defaults to <c>minimal</c>.
    /// </value>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Database provider identifier passed to efcpt.
    /// </summary>
    /// <value>
    /// Defaults to <c>mssql</c>. The concrete set of supported providers is determined by the efcpt
    /// CLI version in use.
    /// </value>
    public string Provider { get; set; } = "mssql";

    /// <summary>
    /// Target framework of the project being built (e.g., "net8.0", "net9.0", "net10.0").
    /// </summary>
    /// <value>
    /// Used to determine whether to use dnx for tool execution on .NET 10+ projects.
    /// If empty or not specified, falls back to runtime version detection.
    /// </value>
    public string TargetFramework { get; set; } = "";

    private readonly record struct ToolResolutionContext(
        string ToolPath,
        string ToolMode,
        string? ManifestDir,
        bool ForceManifestOnNonWindows,
        string DotNetExe,
        string ToolCommand,
        string ToolPackageId,
        string WorkingDir,
        string Args,
        string TargetFramework,
        BuildLog Log
    );

    private readonly record struct ToolInvocation(
        string Exe,
        string Args,
        string Cwd,
        bool UseManifest
    );

    private readonly record struct ToolRestoreContext(
        bool UseManifest,
        bool ShouldRestore,
        bool HasExplicitPath,
        bool HasPackageId,
        string? ManifestDir,
        string WorkingDir,
        string DotNetExe,
        string ToolPath,
        string ToolPackageId,
        string ToolVersion,
        string TargetFramework,
        BuildLog Log
    );

    private static readonly Lazy<Strategy<ToolResolutionContext, ToolInvocation>> ToolResolutionStrategy = new(() =>
        Strategy<ToolResolutionContext, ToolInvocation>.Create()
            .When(static (in ctx) => PathUtils.HasExplicitPath(ctx.ToolPath))
            .Then(static (in ctx)
                => new ToolInvocation(
                    Exe: PathUtils.FullPath(ctx.ToolPath, ctx.WorkingDir),
                    Args: ctx.Args,
                    Cwd: ctx.WorkingDir,
                    UseManifest: false))
            .When((in ctx) => IsDotNet10OrLater(ctx.TargetFramework) && IsDotNet10SdkInstalled(ctx.DotNetExe) && IsDnxAvailable(ctx.DotNetExe))
            .Then((in ctx)
                => new ToolInvocation(
                    Exe: ctx.DotNetExe,
                    Args: $"dnx {ctx.ToolPackageId} --yes -- {ctx.Args}",
                    Cwd: ctx.WorkingDir,
                    UseManifest: false))
            .When((in ctx) => ToolIsAutoOrManifest(ctx))
            .Then(static (in ctx)
                => new ToolInvocation(
                    Exe: ctx.DotNetExe,
                    Args: $"tool run {ctx.ToolCommand} -- {ctx.Args}",
                    Cwd: ctx.WorkingDir,
                    UseManifest: true))
            .Default(static (in ctx)
                => new ToolInvocation(
                    Exe: ctx.ToolCommand,
                    Args: ctx.Args,
                    Cwd: ctx.WorkingDir,
                    UseManifest: false))
            .Build());

    private static bool ToolIsAutoOrManifest(ToolResolutionContext ctx) =>
        ctx.ToolMode.EqualsIgnoreCase("tool-manifest") ||
        (ctx.ToolMode.EqualsIgnoreCase("auto") &&
        (ctx.ManifestDir is not null || ctx.ForceManifestOnNonWindows));

    private static readonly Lazy<ActionStrategy<ToolRestoreContext>> ToolRestoreStrategy = new(() =>
        ActionStrategy<ToolRestoreContext>.Create()
            // Manifest restore: restore tools from local manifest
            // Skip when: dnx will be used OR no manifest directory exists
            .When((in ctx) => ctx is { UseManifest: true, ShouldRestore: true, ManifestDir: not null } 
                && !(IsDotNet10OrLater(ctx.TargetFramework) && IsDotNet10SdkInstalled(ctx.DotNetExe) && IsDnxAvailable(ctx.DotNetExe)))
            .Then((in ctx) =>
            {
                var restoreCwd = ctx.ManifestDir ?? ctx.WorkingDir;
                ProcessRunner.RunOrThrow(ctx.Log, ctx.DotNetExe, "tool restore", restoreCwd);
            })
            // Global restore: update global tool package
            // Skip only when dnx will be used (all three conditions: .NET 10+ target, SDK installed, dnx available)
            .When((in ctx)
                => ctx is
                {
                    UseManifest: false,
                    ShouldRestore: true,
                    HasExplicitPath: false,
                    HasPackageId: true
                } && !(IsDotNet10OrLater(ctx.TargetFramework) && IsDotNet10SdkInstalled(ctx.DotNetExe) && IsDnxAvailable(ctx.DotNetExe)))
            .Then((in ctx) =>
            {
                var versionArg = string.IsNullOrWhiteSpace(ctx.ToolVersion) ? "" : $" --version \"{ctx.ToolVersion}\"";
                ProcessRunner.RunOrThrow(ctx.Log, ctx.DotNetExe, $"tool update --global {ctx.ToolPackageId}{versionArg}", ctx.WorkingDir);
            })
            // Default: no restoration needed (dnx will be used OR no manifest for manifest mode)
            .Default(static (in _) => { })
            .Build());

    /// <summary>
    /// Invokes the efcpt CLI against the specified DACPAC and configuration files.
    /// </summary>
    /// <returns>>True on success; false on error.</returns>
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(RunEfcpt));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        var workingDir = Path.GetFullPath(WorkingDirectory);
        var args = BuildArgs();

        var fake = Environment.GetEnvironmentVariable("EFCPT_FAKE_EFCPT");
        if (!string.IsNullOrWhiteSpace(fake))
        {
            log.Info($"Running in working directory {workingDir}: (fake efcpt) {args}");
            log.Info($"Output will be written to {OutputDir}");
            Directory.CreateDirectory(workingDir);
            Directory.CreateDirectory(OutputDir);

            // Generate realistic structure for testing split outputs:
            // - DbContext in root (stays in Data project)
            // - Entity models in Models subdirectory (copied to Models project)
            var modelsDir = Path.Combine(OutputDir, "Models");
            Directory.CreateDirectory(modelsDir);

            // Root: DbContext (stays in Data project)
            var dbContext = Path.Combine(OutputDir, "SampleDbContext.cs");
            var source = DacpacPath ?? ConnectionString;
            File.WriteAllText(dbContext, $"// generated from {source}\nnamespace Sample.Data;\npublic partial class SampleDbContext : DbContext {{ }}");

            // Models folder: Entity classes (will be copied to Models project)
            var blogModel = Path.Combine(modelsDir, "Blog.cs");
            File.WriteAllText(blogModel, $"// generated from {source}\nnamespace Sample.Data.Models;\npublic partial class Blog {{ public int BlogId {{ get; set; }} }}");

            var postModel = Path.Combine(modelsDir, "Post.cs");
            File.WriteAllText(postModel, $"// generated from {source}\nnamespace Sample.Data.Models;\npublic partial class Post {{ public int PostId {{ get; set; }} }}");

            // For backwards compatibility, also generate the legacy file
            var sample = Path.Combine(OutputDir, "SampleModel.cs");
            File.WriteAllText(sample, $"// generated from {DacpacPath ?? ConnectionString}");

            log.Detail("EFCPT_FAKE_EFCPT set; wrote sample output with Models subdirectory.");
            return true;
        }

        // Determine whether we will use a local tool manifest or fall back to the global tool.
        var manifestDir = FindManifestDir(workingDir);
        var mode = ToolMode;

        // On non-Windows, a bare efcpt executable is unlikely to exist unless explicitly provided
        // via ToolPath. To avoid fragile PATH assumptions on CI agents, treat "auto" as
        // "tool-manifest" whenever a manifest is present *or* when running on non-Windows and
        // no explicit ToolPath was supplied.
#if NETFRAMEWORK
        var forceManifestOnNonWindows = !OperatingSystemPolyfill.IsWindows() && !PathUtils.HasExplicitPath(ToolPath);
#else
        var forceManifestOnNonWindows = !OperatingSystem.IsWindows() && !PathUtils.HasExplicitPath(ToolPath);
#endif

        // Use the Strategy pattern to resolve tool invocation
        var context = new ToolResolutionContext(
            ToolPath, mode, manifestDir, forceManifestOnNonWindows,
            DotNetExe, ToolCommand, ToolPackageId, workingDir, args, TargetFramework, log);

        var invocation = ToolResolutionStrategy.Value.Execute(in context);

        var invokeExe = invocation.Exe;
        var invokeArgs = invocation.Args;
        var invokeCwd = invocation.Cwd;
        var useManifest = invocation.UseManifest;

        log.Info($"Running in working directory {invokeCwd}: {invokeExe} {invokeArgs}");
        log.Info($"Output will be written to {OutputDir}");
        Directory.CreateDirectory(workingDir);
        Directory.CreateDirectory(OutputDir);

        // Restore tools if needed using the ActionStrategy pattern
        var restoreContext = new ToolRestoreContext(
            UseManifest: useManifest,
            ShouldRestore: ToolRestore.IsTrue(),
            HasExplicitPath: PathUtils.HasExplicitPath(ToolPath),
            HasPackageId: PathUtils.HasValue(ToolPackageId),
            ManifestDir: manifestDir,
            WorkingDir: workingDir,
            DotNetExe: DotNetExe,
            ToolPath: ToolPath,
            ToolPackageId: ToolPackageId,
            ToolVersion: ToolVersion,
            TargetFramework: TargetFramework,
            Log: log
        );

        ToolRestoreStrategy.Value.Execute(in restoreContext);

        ProcessRunner.RunOrThrow(log, invokeExe, invokeArgs, invokeCwd);

        return true;
    }


    /// <summary>
    /// Checks if the target framework is .NET 10.0 or later.
    /// </summary>
    /// <param name="targetFramework">The target framework string (e.g., "net8.0", "net10.0").</param>
    /// <returns>True if the target framework is .NET 10.0 or later; otherwise false.</returns>
    private static bool IsDotNet10OrLater(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            return false;

        try
        {
            // Parse target framework to get major version (e.g., "net8.0" -> 8, "net10.0" -> 10)
            if (!targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
                return false;

            var versionPart = targetFramework[3..];

            // Trim at the first '.' or '-' after "net" to handle formats like:
            // - "net10.0"           -> "10"
            // - "net10.0-windows"   -> "10"
            // - "net10-windows"     -> "10"
            var dotIndex = versionPart.IndexOf('.');
            var hyphenIndex = versionPart.IndexOf('-');

            var cutIndex = (dotIndex >= 0, hyphenIndex >= 0) switch
            {
                (true, true) => Math.Min(dotIndex, hyphenIndex),
                (true, false) => dotIndex,
                (false, true) => hyphenIndex,
                _ => -1
            };

            if (cutIndex > 0)
                versionPart = versionPart[..cutIndex];

            if (int.TryParse(versionPart, out var version))
                return version >= 10;

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if .NET SDK version 10 or later is installed.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable.</param>
    /// <returns>True if .NET 10+ SDK is installed; otherwise false.</returns>
    private static bool IsDotNet10SdkInstalled(string dotnetExe)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = "--list-sdks",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p is null) return false;

            // Check if process completed within timeout
            if (!p.WaitForExit(ProcessTimeoutMs))
                return false;

            if (p.ExitCode != 0)
                return false;

            var output = p.StandardOutput.ReadToEnd();

            // Parse output like "10.0.100 [C:\Program Files\dotnet\sdk]"
            // Check if any line starts with "10." or higher
            foreach (var line in output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // Extract version number (first part before space or bracket)
                var spaceIndex = trimmed.IndexOf(' ');
                var versionStr = spaceIndex >= 0 ? trimmed.Substring(0, spaceIndex) : trimmed;

                // Parse major version
                var dotIndex = versionStr.IndexOf('.');
                if (dotIndex > 0 && int.TryParse(versionStr.Substring(0, dotIndex), out var major))
                {
                    if (major >= 10)
                        return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDnxAvailable(string dotnetExe)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = "dnx --help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p is null) return false;

            if (!p.WaitForExit(ProcessTimeoutMs))
                return false;

            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string BuildArgs()
    {
        var workingDir = Path.GetFullPath(WorkingDirectory);

        // Make paths relative to working directory to avoid duplication
        var configPath = MakeRelativeIfPossible(ConfigPath, workingDir);
        var renamingPath = MakeRelativeIfPossible(RenamingPath, workingDir);
        var outputDir = MakeRelativeIfPossible(OutputDir, workingDir);

        // Ensure paths don't end with backslash to avoid escaping the closing quote
        configPath = configPath.TrimEnd('\\', '/');
        renamingPath = renamingPath.TrimEnd('\\', '/');
        outputDir = outputDir.TrimEnd('\\', '/');

        // First positional argument: connection string OR DACPAC path
        // The efcpt CLI auto-detects which one it is
        string firstArg;
        if (UseConnectionStringMode.IsTrue())
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("ConnectionString is required when UseConnectionStringMode is true");
            firstArg = $"\"{ConnectionString}\"";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(DacpacPath) || !File.Exists(DacpacPath))
                throw new InvalidOperationException($"DacpacPath '{DacpacPath}' does not exist");
            firstArg = $"\"{DacpacPath}\"";
        }

        return $"{firstArg} {Provider} -i \"{configPath}\" -r \"{renamingPath}\"" +
               (workingDir.EqualsIgnoreCase(Path.GetFullPath(OutputDir)) ? string.Empty : $" -o \"{outputDir}\"");
    }

    private static string MakeRelativeIfPossible(string path, string basePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullBase = Path.GetFullPath(basePath);

            // If the path is under the base directory, make it relative
            if (fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
            {
#if NETFRAMEWORK
                var relative = NetFrameworkPolyfills.GetRelativePath(fullBase, fullPath);
#else
                var relative = Path.GetRelativePath(fullBase, fullPath);
#endif
                return relative;
            }
        }
        catch
        {
            // Fall back to absolute path on any error
        }

        return path;
    }

    private static string? FindManifestDir(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var manifest = Path.Combine(dir.FullName, ".config", "dotnet-tools.json");
            if (File.Exists(manifest)) return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }
}