using JD.Efcpt.Build.Tasks.Chains;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using PatternKit.Behavioral.Strategy;
using PatternKit.Creational.Builder;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that resolves the sqlproj to use and locates efcpt configuration, renaming, and template inputs.
/// </summary>
/// <remarks>
/// <para>
/// This task is the first stage of the efcpt MSBuild pipeline. It selects a single <c>.sqlproj</c> file
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
/// For the sqlproj reference, the task inspects <see cref="ProjectReferences"/> and enforces that exactly
/// one <c>.sqlproj</c> reference is present unless <see cref="SqlProjOverride"/> is supplied. The resolved
/// path is validated on disk.
/// </para>
/// <para>
/// When <see cref="DumpResolvedInputs"/> evaluates to <c>true</c>, a <c>resolved-inputs.json</c> file is
/// written to <see cref="OutputDir"/> containing the resolved paths. This is primarily intended for
/// debugging and diagnostics.
/// </para>
/// </remarks>
public sealed class ResolveSqlProjAndInputs : Task
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
    /// The task inspects this item group to locate a single <c>.sqlproj</c> reference when
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
    /// Solution directory to probe when searching for configuration, renaming, and template assets.
    /// </summary>
    /// <remarks>
    /// Typically bound to the <c>EfcptSolutionDir</c> MSBuild property. Resolved relative to
    /// <see cref="ProjectDirectory"/> when not rooted.
    /// </remarks>
    public string SolutionDir { get; set; } = "";

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
        string TemplateDir
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
            // Branch 2: No sqlproj references found
            .When(static (in ctx) =>
                ctx.SqlProjReferences.Count == 0)
            .Then(static (in _) =>
                new SqlProjValidationResult(
                    IsValid: false,
                    SqlProjPath: null,
                    ErrorMessage: "No .sqlproj ProjectReference found. Add a single .sqlproj reference or set EfcptSqlProj."))
            // Branch 3: Multiple sqlproj references (ambiguous)
            .When(static (in ctx) =>
                ctx.SqlProjReferences.Count > 1)
            .Then((in ctx) =>
                new SqlProjValidationResult(
                    IsValid: false,
                    SqlProjPath: null,
                    ErrorMessage:
                    $"Multiple .sqlproj references detected ({string.Join(", ", ctx.SqlProjReferences)}). Exactly one is allowed; use EfcptSqlProj to disambiguate."))
            // Branch 4: Exactly one reference (success path)
            .Default((in ctx) =>
            {
                var resolved = ctx.SqlProjReferences[0];
                return File.Exists(resolved)
                    ? new SqlProjValidationResult(IsValid: true, SqlProjPath: resolved, ErrorMessage: null)
                    : new SqlProjValidationResult(
                        IsValid: false,
                        SqlProjPath: null,
                        ErrorMessage: $".sqlproj ProjectReference not found on disk: {resolved}");
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

        Directory.CreateDirectory(OutputDir);

        var resolutionState = BuildResolutionState();

        // Set output properties
        SqlProjPath = resolutionState.SqlProjPath;
        ResolvedConfigPath = resolutionState.ConfigPath;
        ResolvedRenamingPath = resolutionState.RenamingPath;
        ResolvedTemplateDir = resolutionState.TemplateDir;

        if (DumpResolvedInputs.IsTrue())
        {
            WriteDumpFile(resolutionState);
        }

        log.Detail($"Resolved sqlproj: {SqlProjPath}");
        return true;
    }

    private ResolutionState BuildResolutionState()
        => Composer<ResolutionState, ResolutionState>
            .New(() => default)
            .With(state => state with
            {
                SqlProjPath = ResolveSqlProjWithValidation()
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
            .Require(state =>
                string.IsNullOrWhiteSpace(state.SqlProjPath)
                    ? "SqlProj resolution failed"
                    : null)
            .Build(state => state);

    private string ResolveSqlProjWithValidation()
    {
        var sqlRefs = ProjectReferences
            .Where(x => Path.HasExtension(x.ItemSpec) &&
                        Path.GetExtension(x.ItemSpec).EqualsIgnoreCase(".sqlproj"))
            .Select(x => PathUtils.FullPath(x.ItemSpec, ProjectDirectory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ctx = new SqlProjResolutionContext(
            SqlProjOverride: SqlProjOverride,
            ProjectDirectory: ProjectDirectory,
            SqlProjReferences: sqlRefs);

        var result = SqlProjValidationStrategy.Value.Execute(in ctx);

        return result.IsValid
            ? result.SqlProjPath!
            : throw new InvalidOperationException(result.ErrorMessage);
    }

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

    private void WriteDumpFile(ResolutionState state)
    {
        var dump = $"""
                    "project": "{ProjectFullPath}",
                    "sqlproj": "{state.SqlProjPath}",
                    "config": "{state.ConfigPath}",
                    "renaming": "{state.RenamingPath}",
                    "template": "{state.TemplateDir}",
                    "output": "{OutputDir}"
                    """;

        File.WriteAllText(Path.Combine(OutputDir, "resolved-inputs.json"), dump);
    }
}