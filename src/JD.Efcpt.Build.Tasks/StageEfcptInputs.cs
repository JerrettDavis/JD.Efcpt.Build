using JD.Efcpt.Build.Tasks.Decorators;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that stages efcpt configuration, renaming, and template assets into an output directory.
/// </summary>
/// <remarks>
/// <para>
/// This task is typically invoked by the <c>EfcptStageInputs</c> target in the JD.Efcpt.Build pipeline.
/// It copies the specified configuration and renaming JSON files, and a template directory, into a
/// single <see cref="OutputDir"/> that is later consumed by <see cref="ComputeFingerprint"/> and
/// <see cref="RunEfcpt"/>.
/// </para>
/// <para>
/// If the input file names are empty, well-known default names are used:
/// <list type="bullet">
///   <item><description><c>efcpt-config.json</c> for <see cref="ConfigPath"/></description></item>
///   <item><description><c>efcpt.renaming.json</c> for <see cref="RenamingPath"/></description></item>
///   <item><description><c>Template</c> for <see cref="TemplateDir"/></description></item>
/// </list>
/// Existing files and directories under <see cref="OutputDir"/> with the same names are overwritten.
/// </para>
/// </remarks>
public sealed class StageEfcptInputs : Task
{
    /// <summary>
    /// Full path to the MSBuild project file (used for profiling).
    /// </summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>
    /// Directory into which all efcpt input assets will be staged.
    /// </summary>
    /// <value>
    /// The directory is created if it does not exist. Existing files with the same names as staged
    /// assets are overwritten.
    /// </value>
    [Required]
    [ProfileInput]
    public string OutputDir { get; set; } = "";

    /// <summary>
    /// Path to the project that models are being generated into.
    /// </summary>
    [Required]
    [ProfileInput]
    public string ProjectDirectory { get; set; } = "";

    /// <summary>
    /// Path to the efcpt configuration JSON file to copy.
    /// </summary>
    [Required]
    [ProfileInput]
    public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Path to the efcpt renaming JSON file to copy.
    /// </summary>
    [Required]
    [ProfileInput]
    public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Path to the template directory to copy.
    /// </summary>
    /// <value>
    /// The entire directory tree is mirrored into <see cref="StagedTemplateDir"/>. If the resolved
    /// source and destination directories are the same, no copy is performed.
    /// </value>
    [Required]
    [ProfileInput]
    public string TemplateDir { get; set; } = "";

    /// <summary>
    /// Subdirectory within OutputDir where templates should be staged.
    /// </summary>
    /// <value>
    /// If empty or null, templates are staged directly under OutputDir/CodeTemplates.
    /// If a relative path like "Generated", templates are staged under OutputDir/Generated/CodeTemplates.
    /// If an absolute path, it is used directly.
    /// </value>
    public string TemplateOutputDir { get; set; } = "";

    /// <summary>
    /// Target framework of the consuming project (e.g., "net8.0", "net9.0", "net10.0").
    /// </summary>
    /// <value>
    /// Used to select version-specific templates when available. If empty or not specified,
    /// no version-specific selection is performed.
    /// </value>
    public string TargetFramework { get; set; } = "";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    /// <value>
    /// When set to <c>detailed</c>, the task logs the resolved staging paths. Any other value produces
    /// minimal logging.
    /// </value>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Full path to the staged configuration file under <see cref="OutputDir"/>.
    /// </summary>
    [Output] public string StagedConfigPath { get; set; } = "";

    /// <summary>
    /// Full path to the staged renaming file under <see cref="OutputDir"/>.
    /// </summary>
    [Output] public string StagedRenamingPath { get; set; } = "";

    /// <summary>
    /// Full path to the staged template directory under <see cref="OutputDir"/>.
    /// </summary>
    [Output] public string StagedTemplateDir { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
        => TaskExecutionDecorator.ExecuteWithProfiling(
            this, ExecuteCore, ProfilingHelper.GetProfiler(ProjectPath));

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);

        Directory.CreateDirectory(OutputDir);

        var configName = Path.GetFileName(ConfigPath);
        StagedConfigPath = Path.Combine(OutputDir, string.IsNullOrWhiteSpace(configName) ? "efcpt-config.json" : configName);
        File.Copy(ConfigPath, StagedConfigPath, overwrite: true);

        var renamingName = Path.GetFileName(RenamingPath);
        StagedRenamingPath = Path.Combine(OutputDir, string.IsNullOrWhiteSpace(renamingName) ? "efcpt.renaming.json" : renamingName);
        File.Copy(RenamingPath, StagedRenamingPath, overwrite: true);

        var outputDirFull = Full(OutputDir);
        var templateBaseDir = ResolveTemplateBaseDir(outputDirFull, TemplateOutputDir);
        var finalStagedDir = Path.Combine(templateBaseDir, "CodeTemplates");

        // Delete any existing CodeTemplates to ensure clean state
        if (Directory.Exists(finalStagedDir))
            Directory.Delete(finalStagedDir, recursive: true);

        Directory.CreateDirectory(finalStagedDir);

        var sourceTemplate = Path.GetFullPath(TemplateDir);
        var codeTemplatesSubdir = Path.Combine(sourceTemplate, "CodeTemplates");

        // Check if source has Template/CodeTemplates/EFCore structure
        var efcoreSubdir = Path.Combine(codeTemplatesSubdir, "EFCore");
        if (Directory.Exists(efcoreSubdir))
        {
            // Check for version-specific templates (e.g., EFCore/net800, EFCore/net900, EFCore/net1000)
            var versionSpecificDir = TryResolveVersionSpecificTemplateDir(efcoreSubdir, TargetFramework, log);
            var destEFCore = Path.Combine(finalStagedDir, "EFCore");

            if (versionSpecificDir != null)
            {
                // Copy version-specific templates to CodeTemplates/EFCore
                log.Detail($"Using version-specific templates from: {versionSpecificDir}");
                CopyDirectory(versionSpecificDir, destEFCore);
            }
            else
            {
                // Copy entire EFCore contents to CodeTemplates/EFCore (fallback for user templates)
                CopyDirectory(efcoreSubdir, destEFCore);
            }
            StagedTemplateDir = finalStagedDir;
        }
        else if (Directory.Exists(codeTemplatesSubdir))
        {
            // Copy entire CodeTemplates subdirectory
            CopyDirectory(codeTemplatesSubdir, finalStagedDir);
            StagedTemplateDir = finalStagedDir;
        }
        else
        {
            // No CodeTemplates subdirectory - copy and rename entire template dir
            CopyDirectory(sourceTemplate, finalStagedDir);
            StagedTemplateDir = finalStagedDir;
        }

        log.Detail($"Staged config: {StagedConfigPath}");
        log.Detail($"Staged renaming: {StagedRenamingPath}");
        log.Detail($"Staged template: {StagedTemplateDir}");
        return true;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
        => FileSystemHelpers.CopyDirectory(sourceDir, destDir);

    private static string Full(string p) => Path.GetFullPath(p.Trim());

    private static bool IsUnder(string parent, string child)
    {
        parent = Full(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                 + Path.DirectorySeparatorChar;
        child  = Full(child).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                 + Path.DirectorySeparatorChar;

        return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to resolve a version-specific template directory based on the target framework.
    /// </summary>
    /// <param name="efcoreDir">The EFCore templates directory to search.</param>
    /// <param name="targetFramework">The target framework (e.g., "net8.0", "net9.0", "net10.0").</param>
    /// <param name="log">Build log for diagnostic output.</param>
    /// <returns>The path to the version-specific directory, or null if not found.</returns>
    private static string? TryResolveVersionSpecificTemplateDir(string efcoreDir, string targetFramework, BuildLog log)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            return null;

        // Parse target framework to get major version (e.g., "net8.0" -> 8, "net10.0" -> 10)
        var majorVersion = ParseTargetFrameworkVersion(targetFramework);
        if (majorVersion == null)
        {
            log.Detail($"Could not parse target framework version from: {targetFramework}");
            return null;
        }

        // Convert to folder format (e.g., 8 -> "net800", 10 -> "net1000")
        var versionFolder = $"net{majorVersion}00";
        var versionDir = Path.Combine(efcoreDir, versionFolder);

        if (Directory.Exists(versionDir))
        {
            log.Detail($"Found version-specific template folder: {versionFolder}");
            return versionDir;
        }

        // Try fallback to nearest lower version
        var availableVersions = GetAvailableVersionFolders(efcoreDir);
        var fallbackVersion = availableVersions
            .Where(v => v <= majorVersion)
            .OrderByDescending(v => v)
            .FirstOrDefault();

        if (fallbackVersion > 0)
        {
            var fallbackFolder = $"net{fallbackVersion}00";
            var fallbackDir = Path.Combine(efcoreDir, fallbackFolder);
            log.Detail($"Using fallback template folder {fallbackFolder} for target framework {targetFramework}");
            return fallbackDir;
        }

        log.Detail($"No version-specific templates found for {targetFramework}");
        return null;
    }

    /// <summary>
    /// Parses the major version from a target framework string.
    /// </summary>
    private static int? ParseTargetFrameworkVersion(string targetFramework)
    {
        if (!targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            return null;
        
        // Handle formats like "net8.0", "net9.0", "net10.0",
        // including platform-specific variants such as "net10.0-windows" and "net10-windows".
        var versionPart = targetFramework[3..];

        // Trim at the first '.' or '-' after "net" so that we handle:
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
            return version;

        return null;
    }

    /// <summary>
    /// Gets the available version folder numbers from the EFCore directory.
    /// </summary>
    private static IEnumerable<int> GetAvailableVersionFolders(string efcoreDir)
    {
        if (!Directory.Exists(efcoreDir))
            yield break;

        foreach (var dir in Directory.EnumerateDirectories(efcoreDir))
        {
            var name = Path.GetFileName(dir);
            if (!name.StartsWith("net", StringComparison.OrdinalIgnoreCase) || !name.EndsWith("00"))
                continue;
            
            var versionPart = name.Substring(3, name.Length - 5); // "net800" -> "8"
            if (int.TryParse(versionPart, out var version))
                yield return version;
        }
    }

    private string ResolveTemplateBaseDir(string outputDirFull, string templateOutputDirRaw)
    {
        if (string.IsNullOrWhiteSpace(templateOutputDirRaw))
            return outputDirFull;

        var candidate = templateOutputDirRaw.Trim();

        // Absolute? Use it.
        if (Path.IsPathRooted(candidate))
            return Full(candidate);

        // Resolve relative to OutputDir (your original intent)
        var asOutputRelative = Full(Path.Combine(outputDirFull, candidate));

        // ALSO resolve relative to ProjectDirectory (handles "obj\efcpt\Generated\")
        var projDirFull = Full(ProjectDirectory);
        var asProjectRelative = Full(Path.Combine(projDirFull, candidate));

        // If candidate starts with "obj\" or ".\obj\" etc, it is almost certainly project-relative.
        // Prefer project-relative if it lands under the project's obj folder.
        var projObj = Full(Path.Combine(projDirFull, "obj")) + Path.DirectorySeparatorChar;
        if (asProjectRelative.StartsWith(projObj, StringComparison.OrdinalIgnoreCase))
            return asProjectRelative;

        // Otherwise, if the output-relative resolution would cause nested output/output, avoid it.
        // (obj\efcpt + obj\efcpt\Generated)
        if (IsUnder(outputDirFull, asOutputRelative) && candidate.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return asProjectRelative;

        // Default: original behavior
        return asOutputRelative;
    }

}
