using System.Xml.Linq;
using JD.Efcpt.Build.Tasks.Decorators;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that discovers downstream projects that reference the current SQL project
/// and should have their EF Core models regenerated.
/// </summary>
/// <remarks>
/// <para>
/// This task searches for projects that:
/// <list type="bullet">
///   <item><description>Reference the current SQL project (via ProjectReference)</description></item>
///   <item><description>Have JD.Efcpt.Build package installed</description></item>
///   <item><description>Have an efcpt-config.json file</description></item>
/// </list>
/// </para>
/// <para>
/// The search starts from the SQL project directory and moves up to find the solution root,
/// then searches for candidate projects. Projects can be explicitly specified via
/// <see cref="ExplicitDownstreamProjects"/> to override auto-discovery.
/// </para>
/// </remarks>
public sealed class DiscoverDownstreamProjects : Task
{
    /// <summary>
    /// Gets or sets the full path to the current SQL project file.
    /// </summary>
    [Required]
    public string SqlProjectPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the directory containing the SQL project.
    /// </summary>
    [Required]
    public string SqlProjectDirectory { get; set; } = "";

    /// <summary>
    /// Gets or sets the solution directory to search for downstream projects.
    /// When not set, will attempt to discover by walking up from SqlProjectDirectory.
    /// </summary>
    public string SolutionDirectory { get; set; } = "";

    /// <summary>
    /// Gets or sets whether to enable auto-discovery of downstream projects.
    /// </summary>
    public string AutoDiscover { get; set; } = "true";

    /// <summary>
    /// Gets or sets explicit downstream project paths (semicolon-separated).
    /// When set, auto-discovery is skipped and only these projects are used.
    /// </summary>
    public string ExplicitDownstreamProjects { get; set; } = "";

    /// <summary>
    /// Gets or sets additional search paths for discovering projects (semicolon-separated).
    /// </summary>
    public string AdditionalSearchPaths { get; set; } = "";

    /// <summary>
    /// Gets or sets the logging verbosity level.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Gets the discovered downstream project paths.
    /// </summary>
    [Output]
    public string[] DownstreamProjects { get; private set; } = Array.Empty<string>();

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(DiscoverDownstreamProjects));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        // Normalize properties
        SqlProjectPath = SqlProjectPath?.Trim() ?? "";
        SqlProjectDirectory = SqlProjectDirectory?.Trim() ?? "";
        SolutionDirectory = SolutionDirectory?.Trim() ?? "";
        ExplicitDownstreamProjects = ExplicitDownstreamProjects?.Trim() ?? "";
        AdditionalSearchPaths = AdditionalSearchPaths?.Trim() ?? "";
        AutoDiscover = AutoDiscover?.Trim() ?? "true";

        // If explicit projects are specified, use them directly
        if (!string.IsNullOrWhiteSpace(ExplicitDownstreamProjects))
        {
            DownstreamProjects = ParseProjectList(ExplicitDownstreamProjects, SqlProjectDirectory);
            log.Info($"Using explicit downstream projects: {string.Join(", ", DownstreamProjects)}");
            return true;
        }

        // Check if auto-discovery is enabled
        if (!IsAutoDiscoverEnabled())
        {
            log.Detail("Auto-discovery is disabled. No downstream projects will be discovered.");
            DownstreamProjects = Array.Empty<string>();
            return true;
        }

        // Discover solution directory if not provided
        var solutionDir = ResolveSolutionDirectory(log);
        if (string.IsNullOrEmpty(solutionDir))
        {
            log.Info("Could not determine solution directory. No downstream projects discovered.");
            DownstreamProjects = Array.Empty<string>();
            return true;
        }

        log.Detail($"Searching for downstream projects in solution directory: {solutionDir}");

        // Find all candidate projects
        var candidates = DiscoverCandidateProjects(solutionDir, log);

        // Filter candidates to those that reference this SQL project
        var downstream = FilterDownstreamProjects(candidates, log);

        DownstreamProjects = downstream.ToArray();
        
        if (DownstreamProjects.Length > 0)
        {
            log.Info($"Discovered {DownstreamProjects.Length} downstream project(s):");
            foreach (var project in DownstreamProjects)
            {
                log.Info($"  - {project}");
            }
        }
        else
        {
            log.Detail("No downstream projects discovered that reference this SQL project.");
        }

        return true;
    }

    private bool IsAutoDiscoverEnabled()
    {
        return AutoDiscover.Equals("true", StringComparison.OrdinalIgnoreCase)
            || AutoDiscover.Equals("1", StringComparison.OrdinalIgnoreCase)
            || AutoDiscover.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveSolutionDirectory(BuildLog log)
    {
        // Use explicit solution directory if provided
        if (!string.IsNullOrEmpty(SolutionDirectory) && Directory.Exists(SolutionDirectory))
        {
            return Path.GetFullPath(SolutionDirectory);
        }

        // Walk up from SQL project directory to find solution file
        var currentDir = SqlProjectDirectory;
        while (!string.IsNullOrEmpty(currentDir))
        {
            var solutionFiles = Directory.GetFiles(currentDir, "*.sln");
            if (solutionFiles.Length > 0)
            {
                log.Detail($"Found solution directory: {currentDir}");
                return currentDir;
            }

            var parentDir = Path.GetDirectoryName(currentDir);
            if (parentDir == currentDir) break; // Reached root
            currentDir = parentDir;
        }

        log.Detail("Could not find solution file by walking up directory tree.");
        return "";
    }

    private List<string> DiscoverCandidateProjects(string solutionDir, BuildLog log)
    {
        var candidates = new List<string>();

        // Search in solution directory
        AddProjectsFromDirectory(solutionDir, candidates, log);

        // Search in additional paths if specified
        if (!string.IsNullOrWhiteSpace(AdditionalSearchPaths))
        {
            var paths = AdditionalSearchPaths.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                var resolvedPath = Path.IsPathRooted(path)
                    ? path
                    : Path.GetFullPath(Path.Combine(SqlProjectDirectory, path));

                if (Directory.Exists(resolvedPath))
                {
                    AddProjectsFromDirectory(resolvedPath, candidates, log);
                }
            }
        }

        return candidates;
    }

    private void AddProjectsFromDirectory(string directory, List<string> candidates, BuildLog log)
    {
        try
        {
            // Find all .csproj files
            var projects = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories);

            foreach (var project in projects)
            {
                // Skip the SQL project itself
                if (PathsAreEqual(project, SqlProjectPath))
                    continue;

                // Check if project has JD.Efcpt.Build or efcpt-config.json
                if (IsEfcptProject(project, log))
                {
                    candidates.Add(project);
                    log.Detail($"Found candidate project: {project}");
                }
            }
        }
        catch (Exception ex)
        {
            log.Detail($"Error searching directory {directory}: {ex.Message}");
        }
    }

    private bool IsEfcptProject(string projectPath, BuildLog log)
    {
        try
        {
            // Check for efcpt-config.json next to the project
            var projectDir = Path.GetDirectoryName(projectPath);
            if (projectDir != null)
            {
                var configPath = Path.Combine(projectDir, "efcpt-config.json");
                if (File.Exists(configPath))
                {
                    log.Detail($"Project has efcpt-config.json: {projectPath}");
                    return true;
                }
            }

            // Check if project references JD.Efcpt.Build package
            if (HasEfcptPackageReference(projectPath, log))
            {
                log.Detail($"Project has JD.Efcpt.Build package reference: {projectPath}");
                return true;
            }
        }
        catch (Exception ex)
        {
            log.Detail($"Error checking if project is efcpt project {projectPath}: {ex.Message}");
        }

        return false;
    }

    private bool HasEfcptPackageReference(string projectPath, BuildLog log)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            var packageRefs = doc.Descendants("PackageReference")
                .Where(e => e.Attribute("Include")?.Value?.StartsWith("JD.Efcpt.", StringComparison.OrdinalIgnoreCase) == true);

            return packageRefs.Any();
        }
        catch (Exception ex)
        {
            log.Detail($"Error parsing project file {projectPath}: {ex.Message}");
            return false;
        }
    }

    private List<string> FilterDownstreamProjects(List<string> candidates, BuildLog log)
    {
        var downstream = new List<string>();
        var sqlProjectName = Path.GetFileNameWithoutExtension(SqlProjectPath);

        foreach (var candidate in candidates)
        {
            if (ReferenceSqlProject(candidate, sqlProjectName, log))
            {
                downstream.Add(candidate);
            }
        }

        return downstream;
    }

    private bool ReferenceSqlProject(string projectPath, string sqlProjectName, BuildLog log)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            
            // Check ProjectReference elements
            var projectRefs = doc.Descendants("ProjectReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .ToList();

            foreach (var refPath in projectRefs)
            {
                if (refPath == null)
                    continue;

                // Resolve relative path
                var projectDir = Path.GetDirectoryName(projectPath);
                if (projectDir != null)
                {
                    var resolvedRef = Path.GetFullPath(Path.Combine(projectDir, refPath));
                    
                    // Check if this reference points to our SQL project
                    if (PathsAreEqual(resolvedRef, SqlProjectPath))
                    {
                        log.Detail($"Project {projectPath} references SQL project via path: {refPath}");
                        return true;
                    }

                    // Also check by filename match
                    var refFileName = Path.GetFileNameWithoutExtension(refPath);
                    if (refFileName.Equals(sqlProjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        log.Detail($"Project {projectPath} references SQL project by name: {refFileName}");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Detail($"Error checking project references in {projectPath}: {ex.Message}");
        }

        return false;
    }

    private bool PathsAreEqual(string path1, string path2)
    {
        try
        {
            var fullPath1 = Path.GetFullPath(path1);
            var fullPath2 = Path.GetFullPath(path2);
            return string.Equals(fullPath1, fullPath2, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string[] ParseProjectList(string projectList, string basePath)
    {
        var projects = projectList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => Path.IsPathRooted(p)
                ? Path.GetFullPath(p)
                : Path.GetFullPath(Path.Combine(basePath, p)))
            .ToArray();

        return projects;
    }
}
