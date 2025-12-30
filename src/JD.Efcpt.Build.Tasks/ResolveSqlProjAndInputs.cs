using System.Text.RegularExpressions;
using System.Xml.Linq;
using JD.Efcpt.Build.Tasks.Chains;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using PatternKit.Behavioral.Strategy;
using PatternKit.Creational.Builder;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that resolves the SQL project to use and locates efcpt configuration, renaming, and template inputs.
/// </summary>
/// <remarks>
/// <para>
/// This task is the first stage of the efcpt MSBuild pipeline. It selects a single SQL project file
/// (<c>.sqlproj</c> or <c>.csproj</c>/<c>.fsproj</c> using a supported SQL SDK)
/// associated with the current project and probes for configuration artifacts in the following order:
/// <list type="number">
///   <item><description>Explicit override properties (<see cref="SqlProjOverride"/>, <see cref="ConfigOverride"/>, <see cref="RenamingOverride"/>, <see cref="TemplateDirOverride"/>) when they contain an explicit path.</description></item>
///   <item><description>Files or directories next to the consuming project (<see cref="ProjectDirectory"/>).</description></item>
///   <item><description>Files or directories located under <see cref="SolutionDir"/> when <see cref="ProbeSolutionDir"/> evaluates to <c>true</c>.</description></item>
///   <item><description>Packaged defaults under <see cref="DefaultsRoot"/> (typically the <c>Defaults</c> folder from the NuGet package).</description></item>
/// </list>
/// If resolution fails for any of the inputs, the task throws an exception and the build fails.
/// </para>
/// <para>
/// For the SQL project reference, the task inspects <see cref="ProjectReferences"/> and enforces that exactly
/// one SQL project reference is present unless <see cref="SqlProjOverride"/> is supplied. The resolved
/// path is validated on disk.
/// </para>
/// <para>
/// When <see cref="DumpResolvedInputs"/> evaluates to <c>true</c>, a <c>resolved-inputs.json</c> file is
/// written to <see cref="OutputDir"/> containing the resolved paths. This is primarily intended for
/// debugging and diagnostics.
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER
public sealed partial class ResolveSqlProjAndInputs : Task
#else
public sealed class ResolveSqlProjAndInputs : Task
#endif
{
    /// <summary>
    /// Full path to the consuming project file.
    /// </summary>
    [Required]
    public string ProjectFullPath { get; set; } = "";

    /// <summary>
    /// Directory that contains the consuming project file.
    /// </summary>
    [Required]
    public string ProjectDirectory { get; set; } = "";

    /// <summary>
    /// Active build configuration (for example <c>Debug</c> or <c>Release</c>).
    /// </summary>
    [Required]
    public string Configuration { get; set; } = "";

    /// <summary>
    /// Project references of the consuming project.
    /// </summary>
    /// <remarks>
    /// The task inspects this item group to locate a single SQL project reference when
    /// <see cref="SqlProjOverride"/> is not provided.
    /// </remarks>
    public ITaskItem[] ProjectReferences { get; set; } = [];

    /// <summary>
    /// Optional override path for the SQL project to use.
    /// </summary>
    /// <value>
    /// When set to a non-empty explicit path (rooted or containing a directory separator), this value
    /// is resolved against <see cref="ProjectDirectory"/> and used instead of probing
    /// <see cref="ProjectReferences"/>.
    /// </value>
    public string SqlProjOverride { get; set; } = "";

    /// <summary>
    /// Optional override path for the efcpt configuration JSON file.
    /// </summary>
    public string ConfigOverride { get; set; } = "";

    /// <summary>
    /// Optional override path for the efcpt renaming JSON file.
    /// </summary>
    public string RenamingOverride { get; set; } = "";

    /// <summary>
    /// Optional override path for the efcpt template directory.
    /// </summary>
    public string TemplateDirOverride { get; set; } = "";

    /// <summary>
    /// Optional explicit connection string override. When set, connection string mode is used instead of .sqlproj mode.
    /// </summary>
    public string EfcptConnectionString { get; set; } = "";

    /// <summary>
    /// Optional path to appsettings.json file containing connection strings.
    /// </summary>
    public string EfcptAppSettings { get; set; } = "";

    /// <summary>
    /// Optional path to app.config or web.config file containing connection strings.
    /// </summary>
    public string EfcptAppConfig { get; set; } = "";

    /// <summary>
    /// Connection string key name to use from configuration files. Defaults to "DefaultConnection".
    /// </summary>
    public string EfcptConnectionStringName { get; set; } = "DefaultConnection";

    /// <summary>
    /// Solution directory to probe when searching for configuration, renaming, and template assets.
    /// </summary>
    /// <remarks>
    /// Typically bound to the <c>EfcptSolutionDir</c> MSBuild property. Resolved relative to
    /// <see cref="ProjectDirectory"/> when not rooted.
    /// </remarks>
    public string SolutionDir { get; set; } = "";

    /// <summary>
    /// Solution file path, when building inside a solution.
    /// </summary>
    /// <remarks>
    /// Typically bound to the <c>SolutionPath</c> MSBuild property. Resolved relative to
    /// <see cref="ProjectDirectory"/> when not rooted.
    /// </remarks>
    public string SolutionPath { get; set; } = "";

    /// <summary>
    /// Controls whether the solution directory should be probed when locating configuration assets.
    /// </summary>
    /// <value>
    /// Interpreted similarly to a boolean value; the strings <c>true</c>, <c>1</c>, and <c>yes</c>
    /// enable probing. Defaults to <c>true</c>.
    /// </value>
    public string ProbeSolutionDir { get; set; } = "true";

    /// <summary>
    /// Output directory that will receive downstream artifacts.
    /// </summary>
    /// <remarks>
    /// This task ensures the directory exists and uses it as the location for
    /// <c>resolved-inputs.json</c> when <see cref="DumpResolvedInputs"/> is enabled.
    /// </remarks>
    [Required]
    public string OutputDir { get; set; } = "";

    /// <summary>
    /// Root directory that contains packaged default configuration and templates.
    /// </summary>
    /// <remarks>
    /// Typically points at the <c>Defaults</c> folder shipped as <c>contentFiles</c> in the NuGet
    /// package. When set, this location is probed after the project and solution directories.
    /// </remarks>
    public string DefaultsRoot { get; set; } = "";

    /// <summary>
    /// Controls whether the task writes a diagnostic JSON file describing resolved inputs.
    /// </summary>
    /// <value>
    /// When interpreted as <c>true</c>, a <c>resolved-inputs.json</c> file is written to
    /// <see cref="OutputDir"/>.
    /// </value>
    public string DumpResolvedInputs { get; set; } = "false";

    /// <summary>
    /// Resolved full path to the SQL project to use.
    /// </summary>
    [Output]
    public string SqlProjPath { get; set; } = "";

    /// <summary>
    /// Resolved full path to the configuration JSON file.
    /// </summary>
    [Output]
    public string ResolvedConfigPath { get; set; } = "";

    /// <summary>
    /// Resolved full path to the renaming JSON file.
    /// </summary>
    [Output]
    public string ResolvedRenamingPath { get; set; } = "";

    /// <summary>
    /// Resolved full path to the template directory.
    /// </summary>
    [Output]
    public string ResolvedTemplateDir { get; set; } = "";

    /// <summary>
    /// Resolved connection string (if using connection string mode).
    /// </summary>
    [Output]
    public string ResolvedConnectionString { get; set; } = "";

    /// <summary>
    /// Indicates whether the build will use connection string mode (true) or .sqlproj mode (false).
    /// </summary>
    [Output]
    public string UseConnectionString { get; set; } = "false";

    /// <summary>
    /// Indicates whether the resolved configuration file is the library default (not user-provided).
    /// </summary>
    /// <value>
    /// The string "true" when the configuration was resolved from <see cref="DefaultsRoot"/>;
    /// otherwise "false".
    /// </value>
    [Output]
    public string IsUsingDefaultConfig { get; set; } = "false";

    #region Context Records

    private readonly record struct SqlProjResolutionContext(
        string SqlProjOverride,
        string ProjectDirectory,
        IReadOnlyList<string> SqlProjReferences
    );

    private readonly record struct SqlProjValidationResult(
        bool IsValid,
        string? SqlProjPath,
        string? ErrorMessage
    );

    private readonly record struct ResolutionState(
        string SqlProjPath,
        string ConfigPath,
        string RenamingPath,
        string TemplateDir,
        string ConnectionString,
        bool UseConnectionStringMode
    );

    #endregion

    #region Strategies

    private static readonly Lazy<Strategy<SqlProjResolutionContext, SqlProjValidationResult>> SqlProjValidationStrategy = new(()
        => Strategy<SqlProjResolutionContext, SqlProjValidationResult>.Create()
            // Branch 1: Explicit override provided
            .When(static (in ctx) =>
                !string.IsNullOrWhiteSpace(ctx.SqlProjOverride))
            .Then((in ctx) =>
            {
                var path = PathUtils.FullPath(ctx.SqlProjOverride, ctx.ProjectDirectory);
                return new SqlProjValidationResult(
                    IsValid: true,
                    SqlProjPath: path,
                    ErrorMessage: null);
            })
            // Branch 2: No SQL project references found
            .When(static (in ctx) =>
                ctx.SqlProjReferences.Count == 0)
            .Then(static (in _) =>
                new SqlProjValidationResult(
                    IsValid: false,
                    SqlProjPath: null,
                    ErrorMessage: "No SQL project ProjectReference found. Add a single .sqlproj or MSBuild.Sdk.SqlProj reference, or set EfcptSqlProj."))
            // Branch 3: Multiple SQL project references (ambiguous)
            .When(static (in ctx) =>
                ctx.SqlProjReferences.Count > 1)
            .Then((in ctx) =>
                new SqlProjValidationResult(
                    IsValid: false,
                    SqlProjPath: null,
                    ErrorMessage:
                    $"Multiple SQL project references detected ({string.Join(", ", ctx.SqlProjReferences)}). Exactly one is allowed; use EfcptSqlProj to disambiguate."))
            // Branch 4: Exactly one reference (success path)
            .Default((in ctx) =>
            {
                var resolved = ctx.SqlProjReferences[0];
                return File.Exists(resolved)
                    ? new SqlProjValidationResult(IsValid: true, SqlProjPath: resolved, ErrorMessage: null)
                    : new SqlProjValidationResult(
                        IsValid: false,
                        SqlProjPath: null,
                        ErrorMessage: $"SQL project ProjectReference not found on disk: {resolved}");
            })
            .Build());

    #endregion

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(ResolveSqlProjAndInputs));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, "");

        // Log runtime context for troubleshooting
        var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        log.Detail($"MSBuild Runtime: {runtime}");
        log.Detail($"ProjectReferences Count: {ProjectReferences?.Length ?? 0}");
        log.Detail($"SolutionPath: {SolutionPath}");

        Directory.CreateDirectory(OutputDir);

        var resolutionState = BuildResolutionState(log);

        // Set output properties
        SqlProjPath = resolutionState.SqlProjPath;
        ResolvedConfigPath = resolutionState.ConfigPath;
        ResolvedRenamingPath = resolutionState.RenamingPath;
        ResolvedTemplateDir = resolutionState.TemplateDir;
        ResolvedConnectionString = resolutionState.ConnectionString;
        UseConnectionString = resolutionState.UseConnectionStringMode ? "true" : "false";
        IsUsingDefaultConfig = IsConfigFromDefaults(resolutionState.ConfigPath) ? "true" : "false";

        if (DumpResolvedInputs.IsTrue())
            WriteDumpFile(resolutionState);

        log.Detail(resolutionState.UseConnectionStringMode
            ? $"Resolved connection string from: {resolutionState.ConnectionString}"
            : $"Resolved SQL project: {SqlProjPath}");

        return true;
    }

    private TargetContext DetermineMode(BuildLog log)
        => TryExplicitConnectionString(log)
           ?? TrySqlProjDetection(log)
           ?? TryAutoDiscoveredConnectionString(log)
           ?? new(false, "", ""); // Neither found - validation will fail later

    private TargetContext? TryExplicitConnectionString(BuildLog log)
    {
        if (!HasExplicitConnectionConfig())
            return null;

        var connectionString = TryResolveConnectionString(log);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            log.Warn("JD0016", "Explicit connection string configuration provided but failed to resolve. Falling back to .sqlproj detection.");
            return null;
        }

        log.Detail("Using connection string mode due to explicit configuration property");
        return new(true, connectionString, "");
    }

    private TargetContext? TrySqlProjDetection(BuildLog log)
    {
        try
        {
            var sqlProjPath = ResolveSqlProjWithValidation(log);
            if (string.IsNullOrWhiteSpace(sqlProjPath))
                return null;

            WarnIfAutoDiscoveredConnectionStringExists(log);
            return new(false, "", sqlProjPath);
        }
        catch (Exception ex)
        {
            // Log detailed exception information to help users diagnose SQL project resolution issues.
            // This is intentionally more verbose than other catch blocks in this file because this
            // specific failure point is commonly reported by users and requires diagnostic context.
            log.Warn($"SQL project detection failed: {ex.Message}");
            log.Detail($"Exception details: {ex}");
            return null;
        }
    }

    private TargetContext? TryAutoDiscoveredConnectionString(BuildLog log)
    {
        var connectionString = TryResolveAutoDiscoveredConnectionString(log);
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        log.Info("No .sqlproj found. Using auto-discovered connection string.");
        return new(true, connectionString, "");
    }

    private bool HasExplicitConnectionConfig()
        => PathUtils.HasValue(EfcptConnectionString)
           || PathUtils.HasValue(EfcptAppSettings)
           || PathUtils.HasValue(EfcptAppConfig);

    private void WarnIfAutoDiscoveredConnectionStringExists(BuildLog log)
    {
        var autoDiscoveredConnectionString = TryResolveAutoDiscoveredConnectionString(log);
        if (!string.IsNullOrWhiteSpace(autoDiscoveredConnectionString))
        {
            log.Warn("JD0015",
                "Both .sqlproj and auto-discovered connection strings detected. Using .sqlproj mode (default behavior). " +
                "Set EfcptConnectionString explicitly to use connection string mode.");
        }
    }

    private record TargetContext(bool UseConnectionStringMode, string ConnectionString, string SqlProjPath);

    private ResolutionState BuildResolutionState(BuildLog log)
    {
        // Determine mode using priority-based resolution
        var (useConnectionStringMode, connectionString, sqlProjPath) = DetermineMode(log);

        return Composer<ResolutionState, ResolutionState>
            .New(() => default)
            .With(state => state with
            {
                ConnectionString = connectionString,
                UseConnectionStringMode = useConnectionStringMode,
                SqlProjPath = sqlProjPath
            })
            .With(state => state with
            {
                ConfigPath = ResolveFile(ConfigOverride, "efcpt-config.json")
            })
            .With(state => state with
            {
                RenamingPath = ResolveFile(
                    RenamingOverride,
                    "efcpt.renaming.json",
                    "efcpt-renaming.json",
                    "efpt.renaming.json")
            })
            .With(state => state with
            {
                TemplateDir = ResolveDir(
                    TemplateDirOverride,
                    "Template",
                    "CodeTemplates",
                    "Templates")
            })
            // Either connection string or SQL project must be resolved
            .Require(state
                => state.UseConnectionStringMode
                    ? string.IsNullOrWhiteSpace(state.ConnectionString)
                        ? "Connection string resolution failed. No connection string could be resolved from configuration."
                        : null
                    : string.IsNullOrWhiteSpace(state.SqlProjPath)
                        ? "SqlProj resolution failed. No SQL project reference found. " +
                          "Add a .sqlproj ProjectReference, set EfcptSqlProj property, or provide a connection string via " +
                          "EfcptConnectionString/appsettings.json/app.config. Check build output for detailed error messages."
                        : null)
            .Build(state => state);
    }

    private string ResolveSqlProjWithValidation(BuildLog log)
    {
        // ProjectReferences may be null on some .NET Framework MSBuild hosts
        var references = ProjectReferences ?? [];

        var sqlRefs = references
            .Where(x => x?.ItemSpec != null)
            .Select(x => PathUtils.FullPath(x.ItemSpec, ProjectDirectory))
            .Where(SqlProjectDetector.IsSqlProjectReference)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!PathUtils.HasValue(SqlProjOverride) && sqlRefs.Count == 0)
        {
            var fallback = TryResolveFromSolution();
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                log.Warn("No SQL project references found in project; using SQL project detected from solution: " + fallback);
                sqlRefs.Add(fallback);
            }
        }

        var ctx = new SqlProjResolutionContext(
            SqlProjOverride: SqlProjOverride,
            ProjectDirectory: ProjectDirectory,
            SqlProjReferences: sqlRefs);

        var result = SqlProjValidationStrategy.Value.Execute(in ctx);

        return result.IsValid
            ? result.SqlProjPath!
            : throw new InvalidOperationException(result.ErrorMessage);
    }

    private string? TryResolveFromSolution()
    {
        if (!PathUtils.HasValue(SolutionPath))
            return null;

        var solutionPath = PathUtils.FullPath(SolutionPath, ProjectDirectory);
        if (!File.Exists(solutionPath))
            return null;

        var matches = ScanSolutionForSqlProjects(solutionPath).ToList();
        return matches.Count switch
        {
            < 1 => throw new InvalidOperationException("No SQL project references found and none detected in solution."),
            1 => matches[0].Path,
            > 1 => throw new InvalidOperationException(
                $"Multiple SQL projects detected while scanning solution '{solutionPath}' ({string.Join(", ", matches.Select(m => m.Path))}). Reference one directly or set EfcptSqlProj."),
        };
    }

    private static IEnumerable<(string Name, string Path)> ScanSolutionForSqlProjects(string solutionPath)
    {
        var ext = Path.GetExtension(solutionPath);
        if (ext.EqualsIgnoreCase(".slnx"))
        {
            foreach (var match in ScanSlnxForSqlProjects(solutionPath))
                yield return match;

            yield break;
        }

        foreach (var match in ScanSlnForSqlProjects(solutionPath))
            yield return match;
    }

    private static IEnumerable<(string Name, string Path)> ScanSlnForSqlProjects(string solutionPath)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath) ?? "";
        List<string> lines;
        try
        {
            lines = File.ReadLines(solutionPath).ToList();
        }
        catch
        {
            yield break;
        }

        foreach (var line in lines)
        {
            var match = SolutionProjectLine.Match(line);
            if (!match.Success)
                continue;

            var nameGroup = match.Groups["name"];
            var pathGroup = match.Groups["path"];

            // Skip if required groups are missing or empty
            if (!nameGroup.Success || !pathGroup.Success ||
                string.IsNullOrWhiteSpace(nameGroup.Value) ||
                string.IsNullOrWhiteSpace(pathGroup.Value))
                continue;

            var name = nameGroup.Value;
            var relativePath = pathGroup.Value
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            if (!IsProjectFile(Path.GetExtension(relativePath)))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(solutionDir, relativePath));
            if (!File.Exists(fullPath))
                continue;

            if (SqlProjectDetector.IsSqlProjectReference(fullPath))
                yield return (name, fullPath);
        }
    }

    private static IEnumerable<(string Name, string Path)> ScanSlnxForSqlProjects(string solutionPath)
    {
        var solutionDir = Path.GetDirectoryName(solutionPath) ?? "";
        XDocument doc;
        try
        {
            doc = XDocument.Load(solutionPath);
        }
        catch
        {
            yield break;
        }

        foreach (var project in doc.Descendants().Where(e => e.Name.LocalName == "Project"))
        {
            var pathAttr = project.Attributes().FirstOrDefault(a => a.Name.LocalName == "Path");
            if (pathAttr == null || string.IsNullOrWhiteSpace(pathAttr.Value))
                continue;

            var relativePath = pathAttr.Value.Trim()
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            if (!IsProjectFile(Path.GetExtension(relativePath)))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(solutionDir, relativePath));
            if (!File.Exists(fullPath))
                continue;

            var nameAttr = project.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name");
            var name = string.IsNullOrWhiteSpace(nameAttr?.Value)
                ? Path.GetFileNameWithoutExtension(fullPath)
                : nameAttr.Value;

            if (SqlProjectDetector.IsSqlProjectReference(fullPath))
                yield return (name, fullPath);
        }
    }

    private static bool IsProjectFile(string? extension)
        => extension.EqualsIgnoreCase(".sqlproj") ||
           extension.EqualsIgnoreCase(".csproj") ||
           extension.EqualsIgnoreCase(".fsproj");

    private static readonly Regex SolutionProjectLine = SolutionProjectLineRegex();

    private string ResolveFile(string overridePath, params string[] fileNames)
    {
        var chain = FileResolutionChain.Build();
        var candidates = EnumerableExtensions.BuildCandidateNames(overridePath, fileNames);

        var context = new FileResolutionContext(
            OverridePath: overridePath,
            ProjectDirectory: ProjectDirectory,
            SolutionDir: SolutionDir,
            ProbeSolutionDir: ProbeSolutionDir.IsTrue(),
            DefaultsRoot: DefaultsRoot,
            FileNames: candidates);

        return chain.Execute(in context, out var result)
            ? result!
            : throw new InvalidOperationException("Chain should always produce result or throw");
    }

    private string ResolveDir(string overridePath, params string[] dirNames)
    {
        var chain = DirectoryResolutionChain.Build();
        var candidates = EnumerableExtensions.BuildCandidateNames(overridePath, dirNames);

        var context = new DirectoryResolutionContext(
            OverridePath: overridePath,
            ProjectDirectory: ProjectDirectory,
            SolutionDir: SolutionDir,
            ProbeSolutionDir: ProbeSolutionDir.IsTrue(),
            DefaultsRoot: DefaultsRoot,
            DirNames: candidates);

        return chain.Execute(in context, out var result)
            ? result!
            : throw new InvalidOperationException("Chain should always produce result or throw");
    }

    private bool IsConfigFromDefaults(string configPath)
    {
        if (string.IsNullOrWhiteSpace(DefaultsRoot) || string.IsNullOrWhiteSpace(configPath))
            return false;

        var normalizedConfig = Path.GetFullPath(configPath);
        var normalizedDefaults = Path.GetFullPath(DefaultsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                 + Path.DirectorySeparatorChar;

        return normalizedConfig.StartsWith(normalizedDefaults, StringComparison.OrdinalIgnoreCase);
    }

    private string? TryResolveConnectionString(BuildLog log)
    {
        var chain = ConnectionStringResolutionChain.Build();

        var context = new ConnectionStringResolutionContext(
            ExplicitConnectionString: EfcptConnectionString,
            EfcptAppSettings: EfcptAppSettings,
            EfcptAppConfig: EfcptAppConfig,
            ConnectionStringName: EfcptConnectionStringName,
            ProjectDirectory: ProjectDirectory,
            Log: log);

        return chain.Execute(in context, out var result)
            ? result
            : null; // Fallback to .sqlproj mode
    }

    private string? TryResolveAutoDiscoveredConnectionString(BuildLog log)
    {
        // Only try auto-discovery (not explicit properties like EfcptConnectionString, EfcptAppSettings, EfcptAppConfig)
        var chain = ConnectionStringResolutionChain.Build();

        var context = new ConnectionStringResolutionContext(
            ExplicitConnectionString: "", // Ignore explicit connection string
            EfcptAppSettings: "",         // Ignore explicit app settings path
            EfcptAppConfig: "",           // Ignore explicit app config path
            ConnectionStringName: EfcptConnectionStringName,
            ProjectDirectory: ProjectDirectory,
            Log: log);

        return chain.Execute(in context, out var result)
            ? result
            : null;
    }

    private void WriteDumpFile(ResolutionState state)
    {
        var dump =
            $"""
             "project": "{ProjectFullPath}",
             "sqlproj": "{state.SqlProjPath}",
             "config": "{state.ConfigPath}",
             "renaming": "{state.RenamingPath}",
             "template": "{state.TemplateDir}",
             "connectionString": "{state.ConnectionString}",
             "useConnectionStringMode": "{state.UseConnectionStringMode}",
             "output": "{OutputDir}"
             """;

        File.WriteAllText(Path.Combine(OutputDir, "resolved-inputs.json"), dump);
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex("^\\s*Project\\(\"(?<typeGuid>[^\"]+)\"\\)\\s*=\\s*\"(?<name>[^\"]+)\",\\s*\"(?<path>[^\"]+)\",\\s*\"(?<guid>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.Multiline)]
    private static partial Regex SolutionProjectLineRegex();
#else
    private static readonly Regex _solutionProjectLineRegex = new(
        "^\\s*Project\\(\"(?<typeGuid>[^\"]+)\"\\)\\s*=\\s*\"(?<name>[^\"]+)\",\\s*\"(?<path>[^\"]+)\",\\s*\"(?<guid>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.Multiline);
    private static Regex SolutionProjectLineRegex() => _solutionProjectLineRegex;
#endif
}