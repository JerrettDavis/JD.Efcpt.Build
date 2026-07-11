using System.Diagnostics;
using System.Text.Json;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Utilities;
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

    private static readonly string[] NewLineSeparators = ["\r\n", "\n"];

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
    [ProfileInput]
    public string ToolMode { get; set; } = "auto";

    /// <summary>
    /// Package identifier of the efcpt dotnet tool used when restoring or updating the global tool.
    /// </summary>
    /// <value>
    /// Defaults to <c>ErikEJ.EFCorePowerTools.Cli</c>. Only used when <see cref="ToolMode"/> selects the
    /// global tool path and <see cref="ToolRestore"/> evaluates to <c>true</c>.
    /// </value>
    [Required]
    [ProfileInput]
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
    [ProfileInput]
    public string DacpacPath { get; set; } = "";

    /// <summary>
    /// Connection string for database connection (used in connection string mode).
    /// </summary>
    [ProfileInput(Exclude = true)] // Excluded for security - use ConnectionStringRedacted instead
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Indicates whether to use connection string mode (true) or DACPAC mode (false).
    /// </summary>
    [ProfileInput]
    public string UseConnectionStringMode { get; set; } = "false";

    /// <summary>
    /// Redacted connection string for profiling (only included if ConnectionString is set).
    /// </summary>
    [ProfileInput(Name = "ConnectionString")]
    private string ConnectionStringRedacted => string.IsNullOrWhiteSpace(ConnectionString) ? "" : "<redacted>";

    /// <summary>
    /// Full path to the efcpt configuration JSON file.
    /// </summary>
    [Required]
    [ProfileInput]
    public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Full path to the efcpt renaming JSON file.
    /// </summary>
    [Required]
    [ProfileInput]
    public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Path to the template directory that contains the C# template files used by efcpt.
    /// </summary>
    [Required]
    [ProfileInput]
    public string TemplateDir { get; set; } = "";

    /// <summary>
    /// Directory where generated C# model files will be written.
    /// </summary>
    /// <value>
    /// The directory is created if it does not exist. Generated files are later renamed to
    /// <c>.g.cs</c> and added to compilation by the <c>EfcptAddToCompile</c> target.
    /// </value>
    [Required]
    [ProfileInput]
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
    [ProfileInput]
    public string Provider { get; set; } = "mssql";

    /// <summary>
    /// Target framework of the project being built (e.g., "net8.0", "net9.0", "net10.0").
    /// </summary>
    /// <value>
    /// Used to determine whether to use dnx for tool execution on .NET 10+ projects.
    /// If empty or not specified, falls back to runtime version detection.
    /// </value>
    public string TargetFramework { get; set; } = "";

    /// <summary>
    /// Full path to the MSBuild project file (used for profiling).
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Controls whether the task avoids any network-dependent tool resolution/restore step
    /// (dnx execution, tool-manifest restore, global tool update) and update-check calls.
    /// </summary>
    /// <value>
    /// Interpreted case-insensitively via the same truthy convention as other boolean-like
    /// properties on this task (see <see cref="Extensions.StringExtensions.IsTrue"/>). Also
    /// honours the <c>EFCPT_OFFLINE</c> environment variable - if either is truthy, offline mode
    /// is enabled. Defaults to <c>"false"</c>.
    /// </value>
    /// <remarks>
    /// When enabled, the task will refuse to spawn any of the three network-dependent branches
    /// (dnx, tool-manifest restore, global tool update) and instead requires the efcpt tool to
    /// already be runnable via an explicit <see cref="ToolPath"/>, a restored tool manifest, or a
    /// global tool already on <c>PATH</c>. If none of those are available, the task fails with
    /// error <c>JD0026</c> instead of attempting (and blocking on) a network call. See
    /// <c>docs/user-guide/offline.md</c>.
    /// </remarks>
    public string OfflineMode { get; set; } = "false";

    /// <summary>
    /// Controls whether the task will automatically bootstrap an obj-local dotnet tool
    /// manifest and install the efcpt tool into it when no hermetic, network-free way to run
    /// the tool is otherwise available - specifically, on .NET 8/9 (where dnx is not usable)
    /// with no explicit <see cref="ToolPath"/>, no already-usable tool manifest, and no global
    /// tool already resolvable on <c>PATH</c>.
    /// </summary>
    /// <value>
    /// Interpreted case-insensitively via the same truthy convention as other boolean-like
    /// properties on this task (see <see cref="Extensions.StringExtensions.IsTrue"/>). Defaults
    /// to <c>"true"</c>.
    /// </value>
    /// <remarks>
    /// <see cref="OfflineMode"/> always takes precedence: when offline mode is enabled,
    /// auto-acquisition never runs regardless of this property's value - the task treats it as
    /// effectively <c>false</c> in that case, since installing a tool is itself a network
    /// operation. This precedence is enforced in <see cref="AcquireToolIfNeeded"/> (the task
    /// itself), not merely via an MSBuild-level condition. See
    /// <c>docs/user-guide/tool-acquisition.md</c>.
    /// </remarks>
    public string AutoAcquireTool { get; set; } = "true";

    /// <summary>
    /// Testability seam for the SDK/dnx/global-tool capability probes used during tool
    /// resolution and restore. Defaults to <see cref="Utilities.DefaultSdkProbe"/>, which
    /// delegates to the existing memoized probes; tests may substitute a fake implementation.
    /// </summary>
    internal ISdkProbe Probe { get; set; } = new DefaultSdkProbe();

    /// <summary>
    /// Testability seam for tool acquisition (obj-local manifest bootstrap + <c>dotnet tool
    /// install</c>), used by <see cref="AcquireToolIfNeeded"/>. Defaults to
    /// <see cref="Utilities.DefaultToolAcquirer"/>, which shells out via
    /// <see cref="ProcessRunner"/>; tests may substitute a fake implementation to assert the
    /// exact acquisition request without spawning any process or touching the network.
    /// </summary>
    internal IToolAcquirer ToolAcquirer { get; set; } = new DefaultToolAcquirer();

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
        BuildLog Log,
        ISdkProbe Probe,
        bool Offline
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
        BuildLog Log,
        ISdkProbe Probe,
        bool Offline
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
            .When((in ctx) => !ctx.Offline && IsDotNet10OrLater(ctx.TargetFramework) && ctx.Probe.IsDotNet10SdkInstalled(ctx.DotNetExe) && ctx.Probe.IsDnxAvailable(ctx.DotNetExe))
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
        ToolModeUsesManifest(ctx.ToolMode, ctx.ManifestDir, ctx.ForceManifestOnNonWindows);

    /// <summary>
    /// Determines whether <paramref name="toolMode"/> would actually resolve to using a local
    /// tool manifest - i.e. <c>tool-manifest</c> mode, or <c>auto</c> mode with a discovered
    /// manifest directory or a forced manifest fallback (non-Windows, no explicit ToolPath).
    /// </summary>
    /// <remarks>
    /// This is the same condition <see cref="ToolResolutionStrategy"/> uses (via
    /// <see cref="ToolIsAutoOrManifest"/>) to pick the tool-manifest invocation branch, extracted
    /// so the offline pre-flight check in <see cref="ExecuteCore"/> can apply it before a
    /// <see cref="ToolResolutionContext"/> exists.
    /// </remarks>
    internal static bool ToolModeUsesManifest(string toolMode, string? manifestDir, bool forceManifestOnNonWindows) =>
        toolMode.EqualsIgnoreCase("tool-manifest") ||
        (toolMode.EqualsIgnoreCase("auto") &&
        (manifestDir is not null || forceManifestOnNonWindows));

    /// <summary>
    /// Reads a discovered <c>.config/dotnet-tools.json</c> manifest and determines whether it
    /// lists an entry for the target efcpt tool - matched either by package id or by exposing a
    /// command name matching <paramref name="toolCommand"/>.
    /// </summary>
    /// <remarks>
    /// This is a local file read only (no network access), so it is safe to call from the
    /// offline pre-flight path. Any parse failure - missing file, malformed JSON, or an
    /// unexpected shape - is tolerated by returning <c>false</c> (i.e. "does not prove
    /// runnability") rather than throwing, since a corrupt manifest is exactly the kind of
    /// situation the strengthened pre-flight check is meant to catch.
    /// </remarks>
    internal static bool ManifestListsTool(string manifestDir, string toolPackageId, string toolCommand)
    {
        try
        {
            var manifestPath = Path.Combine(manifestDir, ".config", "dotnet-tools.json");
            if (!File.Exists(manifestPath)) return false;

            using var stream = File.OpenRead(manifestPath);
            using var doc = JsonDocument.Parse(stream);

            if (!doc.RootElement.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var tool in tools.EnumerateObject())
            {
                if (tool.Name.EqualsIgnoreCase(toolPackageId))
                    return true;

                if (tool.Value.ValueKind == JsonValueKind.Object &&
                    tool.Value.TryGetProperty("commands", out var commands) &&
                    commands.ValueKind == JsonValueKind.Array)
                {
                    foreach (var command in commands.EnumerateArray())
                    {
                        if (command.ValueKind == JsonValueKind.String &&
                            command.GetString().EqualsIgnoreCase(toolCommand))
                            return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            // Malformed/unreadable manifest: don't treat it as proof of runnability.
            return false;
        }
    }

    private static readonly Lazy<ActionStrategy<ToolRestoreContext>> ToolRestoreStrategy = new(() =>
        ActionStrategy<ToolRestoreContext>.Create()
            // Manifest restore: restore tools from local manifest
            // Skip when: offline OR dnx will be used OR no manifest directory exists
            .When((in ctx) => !ctx.Offline && ctx is { UseManifest: true, ShouldRestore: true, ManifestDir: not null }
                && !(IsDotNet10OrLater(ctx.TargetFramework) && ctx.Probe.IsDotNet10SdkInstalled(ctx.DotNetExe) && ctx.Probe.IsDnxAvailable(ctx.DotNetExe)))
            .Then((in ctx) =>
            {
                var restoreCwd = ctx.ManifestDir ?? ctx.WorkingDir;
                ProcessRunner.RunOrThrow(ctx.Log, ctx.DotNetExe, "tool restore", restoreCwd);
            })
            // Global restore: update global tool package
            // Skip when: offline OR dnx will be used (all three conditions: .NET 10+ target, SDK installed, dnx available)
            .When((in ctx)
                => !ctx.Offline && ctx is
                {
                    UseManifest: false,
                    ShouldRestore: true,
                    HasExplicitPath: false,
                    HasPackageId: true
                } && !(IsDotNet10OrLater(ctx.TargetFramework) && ctx.Probe.IsDotNet10SdkInstalled(ctx.DotNetExe) && ctx.Probe.IsDnxAvailable(ctx.DotNetExe)))
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
        => TaskExecutionDecorator.ExecuteWithProfiling(
            this, ExecuteCore, ProfilingHelper.GetProfiler(ProjectPath));

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

        // Offline mode: OfflineMode task property OR-ed with the EFCPT_OFFLINE environment
        // variable (test/CI escape hatch, mirroring the EFCPT_FAKE_EFCPT/EFCPT_TEST_DACPAC
        // convention documented on this task).
        var offline = OfflineMode.IsTrue() || Environment.GetEnvironmentVariable("EFCPT_OFFLINE").IsTrue();

        // On non-Windows, a bare efcpt executable is unlikely to exist unless explicitly provided
        // via ToolPath. To avoid fragile PATH assumptions on CI agents, treat "auto" as
        // "tool-manifest" whenever a manifest is present *or* when running on non-Windows and
        // no explicit ToolPath was supplied. Computed here (before the offline pre-flight check)
        // so that check can use the same "would this ToolMode actually use the manifest?" logic
        // as the real tool-resolution strategy below.
#if NETFRAMEWORK
        var forceManifestOnNonWindows = !OperatingSystemPolyfill.IsWindows() && !PathUtils.HasExplicitPath(ToolPath);
#else
        var forceManifestOnNonWindows = !OperatingSystem.IsWindows() && !PathUtils.HasExplicitPath(ToolPath);
#endif

        if (offline)
        {
            log.Info("[Efcpt] Offline mode enabled (EfcptOfflineMode=true): dnx execution, tool-manifest restore, and global tool update are skipped.");

            // Pre-flight: offline mode only works if the tool is already guaranteed runnable
            // without any network access - an explicit, existing ToolPath; a discovered tool
            // manifest that will actually be used (same condition as ToolIsAutoOrManifest) and
            // that genuinely lists the target tool (assumed already restored, since we cannot
            // restore it now); or a global tool already resolvable on PATH. If none apply, fail
            // actionably rather than let a later step hang or fail obscurely against a missing
            // tool.
            //
            // The manifest leg is deliberately stricter than "a dotnet-tools.json exists
            // somewhere above WorkingDirectory": a manifest that ToolMode wouldn't even use, or
            // that doesn't list this tool (stale, foreign, or never-restored), is not proof the
            // tool is runnable - it would otherwise pass pre-flight here and only fail later with
            // a cryptic `dotnet tool run` error instead of the actionable JD0026.
            var manifestIsRunnable =
                manifestDir is not null &&
                ToolModeUsesManifest(mode, manifestDir, forceManifestOnNonWindows) &&
                ManifestListsTool(manifestDir, ToolPackageId, ToolCommand);

            var isRunnableOffline =
                (PathUtils.HasExplicitPath(ToolPath) && File.Exists(PathUtils.FullPath(ToolPath, workingDir))) ||
                manifestIsRunnable ||
                Probe.IsGlobalToolInstalled(ToolCommand);

            if (!isRunnableOffline)
            {
                var ex = new EfcptToolNotAvailableOfflineException(TargetFramework, manifestDir, ToolPath, ToolPackageId, ToolVersion);
                log.Error("JD0026", ex.Message);
                return false;
            }
        }

        // Auto-acquisition: on .NET 8/9 (dnx not usable) with no explicit ToolPath and no
        // already-usable tool manifest or global tool, bootstrap an obj-local tool manifest and
        // install the tool into it, before tool resolution runs, so that FindManifestDir
        // discovers the fresh manifest and resolution below takes the `dotnet tool run` path.
        // Offline mode always wins - see AcquireToolIfNeeded for the full precedence/gating
        // rules, which are enforced here (the task itself, the source of truth) rather than
        // only via the MSBuild-level EfcptAutoAcquireTool condition.
        if (!AcquireToolIfNeeded(workingDir, manifestDir, mode, forceManifestOnNonWindows, offline, log, out var acquiredManifestDir))
            return false;
        manifestDir = acquiredManifestDir;

        // Use the Strategy pattern to resolve tool invocation
        var context = new ToolResolutionContext(
            ToolPath, mode, manifestDir, forceManifestOnNonWindows,
            DotNetExe, ToolCommand, ToolPackageId, workingDir, args, TargetFramework, log, Probe, offline);

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
            Log: log,
            Probe: Probe,
            Offline: offline
        );

        ToolRestoreStrategy.Value.Execute(in restoreContext);

        ProcessRunner.RunOrThrow(log, invokeExe, invokeArgs, invokeCwd);

        return true;
    }

    /// <summary>
    /// Bootstraps an obj-local dotnet tool manifest and installs the efcpt tool into it when the
    /// current invocation would otherwise have no hermetic, network-free way to run the tool
    /// (dnx unusable, no already-runnable manifest or global tool available). Called before tool
    /// resolution so a freshly-bootstrapped manifest is discoverable by the resolution strategy.
    /// </summary>
    /// <param name="workingDir">The (already <see cref="Path.GetFullPath(string)"/>'d) working directory - the manifest is bootstrapped here.</param>
    /// <param name="manifestDir">The manifest directory discovered by <see cref="FindManifestDir"/> prior to acquisition, or <see langword="null"/>.</param>
    /// <param name="mode">The effective <see cref="ToolMode"/>.</param>
    /// <param name="forceManifestOnNonWindows">Whether "auto" mode is forced to manifest resolution on non-Windows with no explicit ToolPath.</param>
    /// <param name="offline">Whether offline mode is enabled - always disables auto-acquisition when <see langword="true"/>, regardless of <see cref="AutoAcquireTool"/>.</param>
    /// <param name="log">Build log for diagnostic output.</param>
    /// <param name="updatedManifestDir">
    /// On return, the manifest directory to use for subsequent tool resolution/restore - either
    /// the original <paramref name="manifestDir"/> (nothing acquired) or the freshly-bootstrapped
    /// manifest directory (acquisition succeeded).
    /// </param>
    /// <returns>
    /// <see langword="true"/> if no acquisition was needed, or acquisition succeeded;
    /// <see langword="false"/> if acquisition was attempted and failed (a JD0027 error has
    /// already been logged), or if resolution would use a tool manifest that is absent or
    /// incomplete and acquisition could not be attempted (a JD0028 error has already been
    /// logged) - in either case the caller should return <see langword="false"/> from
    /// <see cref="Execute"/> without throwing.
    /// </returns>
    private bool AcquireToolIfNeeded(
        string workingDir,
        string? manifestDir,
        string mode,
        bool forceManifestOnNonWindows,
        bool offline,
        BuildLog log,
        out string? updatedManifestDir)
    {
        updatedManifestDir = manifestDir;

        // Offline wins: auto-acquisition is a network operation and must never run when
        // offline, regardless of what AutoAcquireTool is set to. Enforced here (the task), not
        // merely via an MSBuild-level condition on EfcptAutoAcquireTool.
        if (offline)
            return true;

        // An explicit ToolPath always wins over any form of automatic resolution/acquisition.
        if (PathUtils.HasExplicitPath(ToolPath))
            return true;

        // .NET 10+ with dnx available: dnx handles tool execution on-demand without requiring
        // any install, so acquisition is irrelevant here - unaffected by this feature.
        if (IsDotNet10OrLater(TargetFramework) && Probe.IsDotNet10SdkInstalled(DotNetExe) && Probe.IsDnxAvailable(DotNetExe))
            return true;

        // Would resolution (ToolResolutionStrategy, via ToolIsAutoOrManifest) actually use a
        // local tool manifest for this invocation? Computed with the SAME condition the real
        // resolution strategy uses below, so this method's gating can never drift from what
        // actually happens once resolution runs.
        var wouldUseManifest = ToolModeUsesManifest(mode, manifestDir, forceManifestOnNonWindows);

        // Already usable via an existing, already-restored tool manifest that resolution would
        // actually pick up.
        var manifestAlreadyUsable =
            wouldUseManifest &&
            manifestDir is not null &&
            ManifestListsTool(manifestDir, ToolPackageId, ToolCommand);
        if (manifestAlreadyUsable)
            return true;

        // Already usable via a global tool already resolvable on PATH - but only when resolution
        // would actually use the global tool path. When resolution would use a manifest (e.g.
        // ToolMode="tool-manifest", or "auto" with a discovered-but-incomplete manifest), a
        // global tool on PATH is irrelevant: ToolResolutionStrategy still emits `dotnet tool run`
        // against the manifest, which would fail if we skipped acquisition here on the strength
        // of an unrelated global install.
        if (!wouldUseManifest && Probe.IsGlobalToolInstalled(ToolCommand))
            return true;

        var canAutoAcquire = AutoAcquireTool.IsTrue() && PathUtils.HasValue(ToolPackageId);

        if (!canAutoAcquire)
        {
            if (!wouldUseManifest)
                // Legacy behavior: not using a manifest, so resolution falls back to invoking the
                // global tool directly (or 'dotnet tool update --global' first, if ToolRestore is
                // enabled) - unaffected by this feature when auto-acquisition can't run.
                return true;

            // Resolution WOULD use a manifest, but that manifest is absent or doesn't list the
            // tool, and we have no way to fix that (auto-acquire disabled, or no package id to
            // install). Proceeding would guarantee a `dotnet tool run <cmd>` failure against a
            // manifest that can't resolve the command - fail now with an actionable error instead.
            var notConfiguredEx = new EfcptToolAcquisitionNotConfiguredException(
                workingDir, manifestDir, ToolPackageId, ToolVersion, AutoAcquireTool);
            log.Error("JD0028", notConfiguredEx.Message);
            return false;
        }

        // Target the already-discovered manifest directory when one exists (even if resolution
        // wouldn't otherwise use it as-is) so acquisition installs the missing tool entry into
        // that manifest rather than bootstrapping a second, shadowing manifest in workingDir.
        var acquisitionDir = manifestDir ?? workingDir;

        var versionSuffix = string.IsNullOrWhiteSpace(ToolVersion) ? "" : $" {ToolVersion}";
        log.Info(
            $"[Efcpt] No hermetic, network-free way to run the efcpt tool was found for " +
            $"TargetFramework='{TargetFramework}' (dnx unavailable/unusable, no usable tool " +
            $"manifest or global tool). Bootstrapping/updating the tool manifest in " +
            $"'{acquisitionDir}' and installing '{ToolPackageId}{versionSuffix}' into it " +
            "(EfcptAutoAcquireTool=true)...");

        var request = new ToolAcquisitionRequest(acquisitionDir, DotNetExe, ToolPackageId, ToolVersion);
        var outcome = ToolAcquirer.Acquire(request, log);

        if (!outcome.Success)
        {
            var ex = new EfcptToolAcquisitionFailedException(
                acquisitionDir, ToolPackageId, ToolVersion, outcome.ErrorMessage ?? "(no details captured)");
            log.Error("JD0027", ex.Message);
            return false;
        }

        updatedManifestDir = FindManifestDir(workingDir);
        return true;
    }

    /// <summary>
    /// Checks if the target framework is .NET 10.0 or later.
    /// </summary>
    /// <param name="targetFramework">The target framework string (e.g., "net8.0", "net10.0").</param>
    /// <returns>True if the target framework is .NET 10.0 or later; otherwise false.</returns>
    /// <remarks>
    /// Internal (not private) so <see cref="EfcptDoctor"/> can call the exact same TFM-parsing
    /// logic <see cref="ToolResolutionStrategy"/> and <see cref="AcquireToolIfNeeded"/> use,
    /// rather than a separately-maintained copy that could drift on odd TFM shapes (see #186
    /// adversarial review).
    /// </remarks>
    internal static bool IsDotNet10OrLater(string targetFramework)
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
    /// <remarks>
    /// The underlying process spawn is memoized via <see cref="SdkProbeCache"/> under the
    /// <c>"list-sdks"</c> probe name - the same command (and thus the same cache key) used by
    /// <see cref="DotNetToolUtilities.IsDotNet10SdkInstalled"/>, so both tasks share a single
    /// probe result within a build session. Only a determinate probe result is memoized - a
    /// transient failure (process launch hiccup, timeout) is retried on the next call rather
    /// than being cached as a permanent negative.
    /// </remarks>
    internal static bool IsDotNet10SdkInstalled(string dotnetExe) =>
        SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, () => ProbeDotNet10SdkInstalled(dotnetExe));

    /// <summary>
    /// Performs the actual `dotnet --list-sdks` process spawn and output parsing. Not memoized
    /// itself - callers should go through <see cref="IsDotNet10SdkInstalled"/>.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if .NET 10+ SDK is installed;
    /// <see cref="ProbeOutcome.Unavailable"/> if the probe ran to completion but no such SDK
    /// was found; or <see cref="ProbeOutcome.Transient"/> if the probe could not produce a
    /// determinate answer (launch failure, timeout, unexpected exception).
    /// </returns>
    private static ProbeOutcome ProbeDotNet10SdkInstalled(string dotnetExe)
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
            if (p is null) return ProbeOutcome.Transient;

            // Check if process completed within timeout
            if (!p.WaitForExit(ProcessTimeoutMs))
                return ProbeOutcome.Transient;

            return MapListSdksOutcome(p.ExitCode, p.StandardOutput.ReadToEnd());
        }
        catch
        {
            return ProbeOutcome.Transient;
        }
    }

    /// <summary>
    /// Pure decision logic for a completed <c>dotnet --list-sdks</c> invocation: given its exit
    /// code and captured standard output, determines whether a qualifying SDK is listed.
    /// Extracted from <see cref="ProbeDotNet10SdkInstalled"/> so it can be unit tested without
    /// spawning a process; the timeout/launch-failure/exception paths remain in the caller since
    /// they are inherent to the process spawn itself.
    /// </summary>
    /// <param name="exitCode">The exit code of the completed process.</param>
    /// <param name="output">The captured standard output of the completed process.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if a listed SDK version is &gt;= 10.0;
    /// <see cref="ProbeOutcome.Unavailable"/> otherwise (including a non-zero exit code).
    /// </returns>
    internal static ProbeOutcome MapListSdksOutcome(int exitCode, string output)
    {
        if (exitCode != 0)
            return ProbeOutcome.Unavailable;

        // Parse output like "10.0.100 [C:\Program Files\dotnet\sdk]"
        // Check if any line starts with "10." or higher
        foreach (var line in output.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries))
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
                    return ProbeOutcome.Available;
            }
        }

        return ProbeOutcome.Unavailable;
    }

    /// <summary>
    /// Checks if dnx (dotnet native execution) is available by running <c>dotnet dnx --help</c>.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable.</param>
    /// <returns>True if dnx is available; otherwise false.</returns>
    /// <remarks>
    /// The underlying process spawn is memoized via <see cref="SdkProbeCache"/> under the
    /// <c>"dnx-help"</c> probe name (distinct from <see cref="DotNetToolUtilities.IsDnxAvailable"/>,
    /// which probes via <c>--list-runtimes</c> instead - the two are intentionally different
    /// commands and therefore keep separate cache entries). Only a determinate probe result is
    /// memoized - a transient failure (process launch hiccup, timeout) is retried on the next
    /// call rather than being cached as a permanent negative.
    /// </remarks>
    internal static bool IsDnxAvailable(string dotnetExe) =>
        SdkProbeCache.GetOrProbe("dnx-help", dotnetExe, () => ProbeDnxAvailable(dotnetExe));

    /// <summary>
    /// Performs the actual `dotnet dnx --help` process spawn. Not memoized itself - callers
    /// should go through <see cref="IsDnxAvailable"/>.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if dnx responds successfully;
    /// <see cref="ProbeOutcome.Unavailable"/> if the probe ran to completion with a non-zero
    /// exit code; or <see cref="ProbeOutcome.Transient"/> if the probe could not produce a
    /// determinate answer (launch failure, timeout, unexpected exception).
    /// </returns>
    private static ProbeOutcome ProbeDnxAvailable(string dotnetExe)
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
            if (p is null) return ProbeOutcome.Transient;

            if (!p.WaitForExit(ProcessTimeoutMs))
                return ProbeOutcome.Transient;

            return MapExitCodeOutcome(p.ExitCode);
        }
        catch
        {
            return ProbeOutcome.Transient;
        }
    }

    /// <summary>
    /// Pure decision logic for a completed <c>dotnet dnx --help</c> invocation: maps its exit
    /// code to a probe outcome. Extracted from <see cref="ProbeDnxAvailable"/> so it can be unit
    /// tested without spawning a process; the timeout/launch-failure/exception paths remain in
    /// the caller since they are inherent to the process spawn itself.
    /// </summary>
    /// <param name="exitCode">The exit code of the completed process.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if <paramref name="exitCode"/> is zero;
    /// otherwise <see cref="ProbeOutcome.Unavailable"/>.
    /// </returns>
    internal static ProbeOutcome MapExitCodeOutcome(int exitCode) =>
        exitCode == 0 ? ProbeOutcome.Available : ProbeOutcome.Unavailable;

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

    internal static string? FindManifestDir(string start)
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