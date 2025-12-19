using System.Diagnostics;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Strategies;
using Microsoft.Build.Framework;
using PatternKit.Behavioral.Strategy;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that ensures a DACPAC exists for a given SQL project and build configuration.
/// </summary>
/// <remarks>
/// <para>
/// This task is typically invoked by the <c>EfcptEnsureDacpac</c> target in the JD.Efcpt.Build
/// pipeline. It locates the SQL project, determines whether an existing DACPAC is
/// up to date, and, if necessary, triggers a build using either <c>msbuild.exe</c> or
/// <c>dotnet msbuild</c>.
/// </para>
/// <para>
/// The staleness heuristic compares the last write time of the most recently modified source file
/// (excluding <c>bin</c> and <c>obj</c> directories) with the last write time of the DACPAC. When the
/// DACPAC is missing or older than any source file, the SQL project is rebuilt.
/// </para>
/// <para>
/// For testing and diagnostics, the task honours the following environment variables:
/// <list type="bullet">
///   <item><description><c>EFCPT_FAKE_BUILD</c> - when set, no external build is invoked. Instead a fake DACPAC file is written under the expected <c>bin/&lt;Configuration&gt;</c> folder.</description></item>
///   <item><description><c>EFCPT_TEST_DACPAC</c> - if present, forwarded to the child process as an environment variable of the same name.</description></item>
/// </list>
/// These hooks are primarily intended for the test suite and are not considered a stable public API.
/// </para>
/// </remarks>
public sealed class EnsureDacpacBuilt : Task
{
    /// <summary>
    /// Path to the SQL project that produces the DACPAC.
    /// </summary>
    [Required]
    public string SqlProjPath { get; set; } = "";

    /// <summary>
    /// Build configuration to use when compiling the SQL project.
    /// </summary>
    /// <value>Typically <c>Debug</c> or <c>Release</c>, but any valid configuration is accepted.</value>
    [Required]
    public string Configuration { get; set; } = "";

    /// <summary>Path to <c>msbuild.exe</c> when available (Windows/Visual Studio scenarios).</summary>
    /// <value>
    /// When non-empty and the file exists, this executable is preferred over <see cref="DotNetExe"/> for
    /// building the SQL project.
    /// </value>
    public string MsBuildExe { get; set; } = "";

    /// <summary>Path to the <c>dotnet</c> host executable.</summary>
    /// <value>
    /// Defaults to <c>dotnet</c>. Used to run <c>dotnet msbuild</c> when <see cref="MsBuildExe"/> is not
    /// provided or does not exist.
    /// </value>
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Full path to the resolved DACPAC after the task completes.
    /// </summary>
    /// <value>
    /// When an up-to-date DACPAC already exists, this is set to that file. Otherwise it points to the
    /// DACPAC produced by the build.
    /// </value>
    [Output]
    public string DacpacPath { get; set; } = "";

    #region Context Records

    private readonly record struct DacpacStalenessContext(
        string SqlProjPath,
        string BinDir,
        DateTime LatestSourceWrite
    );

    private readonly record struct BuildToolContext(
        string SqlProjPath,
        string Configuration,
        string MsBuildExe,
        string DotNetExe,
        bool IsFakeBuild,
        bool UsesModernSdk
    );

    private readonly record struct StalenessCheckResult(
        bool ShouldRebuild,
        string? ExistingDacpac,
        string Reason
    );

    private readonly record struct BuildToolSelection(
        string Exe,
        string Args,
        bool IsFake
    );

    #endregion

    #region Strategies

    private static readonly Lazy<Strategy<DacpacStalenessContext, StalenessCheckResult>> StalenessStrategy = new(() =>
        Strategy<DacpacStalenessContext, StalenessCheckResult>.Create()
            // Branch 1: No existing DACPAC found
            .When(static (in ctx) =>
                FindDacpacInDir(ctx.BinDir) == null)
            .Then(static (in _) =>
                new StalenessCheckResult(
                    ShouldRebuild: true,
                    ExistingDacpac: null,
                    Reason: "DACPAC not found. Building sqlproj..."))
            // Branch 2: DACPAC exists but is stale
            .When((in ctx) =>
            {
                var existing = FindDacpacInDir(ctx.BinDir);
                return existing != null && File.GetLastWriteTimeUtc(existing) < ctx.LatestSourceWrite;
            })
            .Then((in ctx) =>
            {
                var existing = FindDacpacInDir(ctx.BinDir);
                return new StalenessCheckResult(
                    ShouldRebuild: true,
                    ExistingDacpac: existing,
                    Reason: "DACPAC exists but appears stale. Rebuilding sqlproj...");
            })
            // Branch 3: DACPAC is current
            .Default((in ctx) =>
            {
                var existing = FindDacpacInDir(ctx.BinDir);
                return new StalenessCheckResult(
                    ShouldRebuild: false,
                    ExistingDacpac: existing,
                    Reason: $"Using existing DACPAC: {existing}");
            })
            .Build());

    private static readonly Lazy<Strategy<BuildToolContext, BuildToolSelection>> BuildToolStrategy = new(() =>
        Strategy<BuildToolContext, BuildToolSelection>.Create()
            // Branch 1: Fake build mode (testing)
            .When(static (in ctx) => ctx.IsFakeBuild)
            .Then(static (in _) =>
                new BuildToolSelection(
                    Exe: string.Empty,
                    Args: string.Empty,
                    IsFake: true))
            // Branch 2: Modern dotnet build (for supported SQL SDK projects)
            .When(static (in ctx) => ctx.UsesModernSdk)
            .Then((in ctx) =>
                new BuildToolSelection(
                    Exe: ctx.DotNetExe,
                    Args: $"build \"{ctx.SqlProjPath}\" -c {ctx.Configuration} --nologo",
                    IsFake: false))
            // Branch 3: Use MSBuild.exe (Windows/Visual Studio for legacy projects)
            .When(static (in ctx) =>
                !string.IsNullOrWhiteSpace(ctx.MsBuildExe) && File.Exists(ctx.MsBuildExe))
            .Then((in ctx) =>
                new BuildToolSelection(
                    Exe: ctx.MsBuildExe,
                    Args: $"\"{ctx.SqlProjPath}\" /t:Restore /t:Build /p:Configuration=\"{ctx.Configuration}\" /nologo",
                    IsFake: false))
            // Branch 4: Use dotnet msbuild (cross-platform fallback for legacy projects)
            .Default((in ctx) =>
                new BuildToolSelection(
                    Exe: ctx.DotNetExe,
                    Args: $"msbuild \"{ctx.SqlProjPath}\" /t:Restore /t:Build /p:Configuration=\"{ctx.Configuration}\" /nologo",
                    IsFake: false))
            .Build());

    #endregion

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(EnsureDacpacBuilt));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        var sqlproj = Path.GetFullPath(SqlProjPath);
        if (!File.Exists(sqlproj))
            throw new FileNotFoundException("SQL project not found", sqlproj);

        var binDir = Path.Combine(Path.GetDirectoryName(sqlproj)!, "bin", Configuration);
        Directory.CreateDirectory(binDir);

        // Use Strategy to check staleness
        var stalenessCtx = new DacpacStalenessContext(
            SqlProjPath: sqlproj,
            BinDir: binDir,
            LatestSourceWrite: LatestSourceWrite(sqlproj));

        var check = StalenessStrategy.Value.Execute(in stalenessCtx);

        if (!check.ShouldRebuild)
        {
            DacpacPath = check.ExistingDacpac!;
            log.Detail(check.Reason);
            return true;
        }

        log.Detail(check.Reason);
        BuildSqlProj(log, sqlproj);

        var built = FindDacpacInDir(binDir) ??
                    FindDacpacInDir(Path.Combine(Path.GetDirectoryName(sqlproj)!, "bin")) ??
                    throw new FileNotFoundException($"DACPAC not found after build. Looked under: {binDir}");

        DacpacPath = built;
        log.Info($"DACPAC: {DacpacPath}");
        return true;
    }

    private void BuildSqlProj(BuildLog log, string sqlproj)
    {
        var fake = Environment.GetEnvironmentVariable("EFCPT_FAKE_BUILD");
        var toolCtx = new BuildToolContext(
            SqlProjPath: sqlproj,
            Configuration: Configuration,
            MsBuildExe: MsBuildExe,
            DotNetExe: DotNetExe,
            IsFakeBuild: !string.IsNullOrWhiteSpace(fake),
            UsesModernSdk: SqlProjectDetector.UsesModernSqlSdk(sqlproj));

        var selection = BuildToolStrategy.Value.Execute(in toolCtx);

        if (selection.IsFake)
        {
            WriteFakeDacpac(log, sqlproj);
            return;
        }

        var normalized = CommandNormalizationStrategy.Normalize(selection.Exe, selection.Args);

        var psi = new ProcessStartInfo
        {
            FileName = normalized.FileName,
            Arguments = normalized.Args,
            WorkingDirectory = Path.GetDirectoryName(sqlproj) ?? "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        var testDac = Environment.GetEnvironmentVariable("EFCPT_TEST_DACPAC");
        if (!string.IsNullOrWhiteSpace(testDac))
            psi.Environment["EFCPT_TEST_DACPAC"] = testDac;

        var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {normalized.FileName}");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            log.Error(stdout);
            log.Error(stderr);
            throw new InvalidOperationException($"SQL project build failed with exit code {p.ExitCode}");
        }

        if (!string.IsNullOrWhiteSpace(stdout)) log.Detail(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) log.Detail(stderr);
    }

    private void WriteFakeDacpac(BuildLog log, string sqlproj)
    {
        var projectName = Path.GetFileNameWithoutExtension(sqlproj);
        var dest = Path.Combine(Path.GetDirectoryName(sqlproj)!, "bin", Configuration, projectName + ".dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, "fake dacpac");
        log.Info($"EFCPT_FAKE_BUILD set; wrote {dest}");
    }

    #region Helper Methods

    private static readonly IReadOnlySet<string> ExcludedDirs = new HashSet<string>(
        ["bin", "obj"],
        StringComparer.OrdinalIgnoreCase);

    private static string? FindDacpacInDir(string dir) =>
        !Directory.Exists(dir)
            ? null
            : Directory
                .EnumerateFiles(dir, "*.dacpac", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

    private static DateTime LatestSourceWrite(string sqlproj)
    {
        var root = Path.GetDirectoryName(sqlproj)!;

        return Directory
            .EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => !IsUnderExcludedDir(file, root))
            .Select(File.GetLastWriteTimeUtc)
            .Prepend(File.GetLastWriteTimeUtc(sqlproj))
            .Max();
    }

    private static bool IsUnderExcludedDir(string filePath, string root)
    {
        var relativePath = Path.GetRelativePath(root, filePath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Any(segment => ExcludedDirs.Contains(segment));
    }

    #endregion
}
