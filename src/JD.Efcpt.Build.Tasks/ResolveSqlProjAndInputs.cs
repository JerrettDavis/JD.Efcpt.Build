using Microsoft.Build.Framework;
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
    [Required] public string ProjectFullPath { get; set; } = "";

    /// <summary>
    /// Directory that contains the consuming project file.
    /// </summary>
    [Required] public string ProjectDirectory { get; set; } = "";

    /// <summary>
    /// Active build configuration (for example <c>Debug</c> or <c>Release</c>).
    /// </summary>
    [Required] public string Configuration { get; set; } = "";

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
    [Required] public string OutputDir { get; set; } = "";

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
    [Output] public string SqlProjPath { get; set; } = "";

    /// <summary>
    /// Resolved full path to the configuration JSON file.
    /// </summary>
    [Output] public string ResolvedConfigPath { get; set; } = "";

    /// <summary>
    /// Resolved full path to the renaming JSON file.
    /// </summary>
    [Output] public string ResolvedRenamingPath { get; set; } = "";

    /// <summary>
    /// Resolved full path to the template directory.
    /// </summary>
    [Output] public string ResolvedTemplateDir { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
    {
        var log = new BuildLog(Log, "");
        try
        {
            Directory.CreateDirectory(OutputDir);

            SqlProjPath = ResolveSqlProj(log);
            ResolvedConfigPath = ResolveFile(log, ConfigOverride, "efcpt-config.json");
            ResolvedRenamingPath = ResolveFile(log, RenamingOverride, "efcpt.renaming.json", "efcpt-renaming.json", "efpt.renaming.json");
            ResolvedTemplateDir = ResolveDir(log, TemplateDirOverride, "Template", "Templates");

            if (IsTrue(DumpResolvedInputs))
            {
                var dump = $"""
                             "project": "{ProjectFullPath}",
                             "sqlproj": "{SqlProjPath}",
                             "config": "{ResolvedConfigPath}",
                             "renaming": "{ResolvedRenamingPath}",
                             "template": "{ResolvedTemplateDir}",
                             "output": "{OutputDir}"
                             """;

                File.WriteAllText(Path.Combine(OutputDir, "resolved-inputs.json"), dump);
            }

            log.Detail($"Resolved sqlproj: {SqlProjPath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private string ResolveSqlProj(BuildLog log)
    {
        if (!string.IsNullOrWhiteSpace(SqlProjOverride))
            return PathUtils.FullPath(SqlProjOverride, ProjectDirectory);

        var sqlRefs = ProjectReferences
            .Where(x => Path.HasExtension(x.ItemSpec) && string.Equals(Path.GetExtension(x.ItemSpec), ".sqlproj", StringComparison.OrdinalIgnoreCase))
            .Select(x => PathUtils.FullPath(x.ItemSpec, ProjectDirectory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        switch (sqlRefs.Count)
        {
            case 0:
                throw new InvalidOperationException("No .sqlproj ProjectReference found. Add a single .sqlproj reference or set EfcptSqlProj.");
            case > 1:
                throw new InvalidOperationException($"Multiple .sqlproj references detected ({string.Join(", ", sqlRefs)}). Exactly one is allowed; use EfcptSqlProj to disambiguate.");
        }

        var resolved = sqlRefs[0];
        return File.Exists(resolved) 
            ? resolved 
            : throw new FileNotFoundException(".sqlproj ProjectReference not found on disk", resolved);
    }

    private string ResolveFile(BuildLog log, string overridePath, params string[] fileNames)
    {
        // Prefer explicit override (rooted or includes a directory)
        if (PathUtils.HasExplicitPath(overridePath))
        {
            var p = PathUtils.FullPath(overridePath, ProjectDirectory);
            if (!File.Exists(p)) throw new FileNotFoundException($"Override not found", p);
            return p;
        }

        var candidates = BuildNames(overridePath, fileNames);
        foreach (var name in candidates)
        {
            var candidate1 = Path.Combine(ProjectDirectory, name);
            if (File.Exists(candidate1)) return candidate1;
        }

        if (IsTrue(ProbeSolutionDir) && !string.IsNullOrWhiteSpace(SolutionDir))
        {
            var sol = PathUtils.FullPath(SolutionDir, ProjectDirectory);
            foreach (var name in candidates)
            {
                var candidate2 = Path.Combine(sol, name);
                if (File.Exists(candidate2)) return candidate2;
            }
        }

        // Fall back to packaged defaults root if present
        if (!string.IsNullOrWhiteSpace(DefaultsRoot))
        {
            foreach (var name in candidates)
            {
                var candidate3 = Path.Combine(DefaultsRoot, name);
                if (File.Exists(candidate3)) return candidate3;
            }
        }

        throw new FileNotFoundException($"Unable to locate {string.Join(" or ", candidates)}. Provide EfcptConfig/EfcptRenaming, place next to project, in solution dir, or ensure defaults are present.");
    }

    private string ResolveDir(BuildLog log, string overridePath, params string[] dirNames)
    {
        if (PathUtils.HasExplicitPath(overridePath))
        {
            var p = PathUtils.FullPath(overridePath, ProjectDirectory);
            if (!Directory.Exists(p)) throw new DirectoryNotFoundException($"Template override not found: {p}");
            return p;
        }

        var candidates = BuildNames(overridePath, dirNames);
        foreach (var name in candidates)
        {
            var candidate1 = Path.Combine(ProjectDirectory, name);
            if (Directory.Exists(candidate1)) return candidate1;
        }

        if (IsTrue(ProbeSolutionDir) && !string.IsNullOrWhiteSpace(SolutionDir))
        {
            var sol = PathUtils.FullPath(SolutionDir, ProjectDirectory);
            foreach (var name in candidates)
            {
                var candidate2 = Path.Combine(sol, name);
                if (Directory.Exists(candidate2)) return candidate2;
            }
        }

        if (!string.IsNullOrWhiteSpace(DefaultsRoot))
        {
            foreach (var name in candidates)
            {
                var candidate3 = Path.Combine(DefaultsRoot, name);
                if (Directory.Exists(candidate3)) return candidate3;
            }
        }

        throw new DirectoryNotFoundException($"Unable to locate template directory ({string.Join(" or ", candidates)}). Provide EfcptTemplateDir, place Template next to project, in solution dir, or ensure defaults are present.");
    }

    private static bool IsTrue(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1" || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> BuildNames(string candidate, string[] fileNames)
    {
        var names = new List<string>();
        if (PathUtils.HasValue(candidate))
            names.Add(Path.GetFileName(candidate));

        foreach (var n in fileNames)
        {
            if (!string.IsNullOrWhiteSpace(n))
                names.Add(Path.GetFileName(n));
        }

        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
