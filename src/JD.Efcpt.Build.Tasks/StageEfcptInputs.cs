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
    /// Directory into which all efcpt input assets will be staged.
    /// </summary>
    /// <value>
    /// The directory is created if it does not exist. Existing files with the same names as staged
    /// assets are overwritten.
    /// </value>
    [Required] public string OutputDir { get; set; } = "";

    /// <summary>
    /// Path to the efcpt configuration JSON file to copy.
    /// </summary>
    [Required] public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Path to the efcpt renaming JSON file to copy.
    /// </summary>
    [Required] public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Path to the template directory to copy.
    /// </summary>
    /// <value>
    /// The entire directory tree is mirrored into <see cref="StagedTemplateDir"/>. If the resolved
    /// source and destination directories are the same, no copy is performed.
    /// </value>
    [Required] public string TemplateDir { get; set; } = "";

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
    {
        var log = new BuildLog(Log, LogVerbosity);
        try
        {
            Directory.CreateDirectory(OutputDir);

            var configName = Path.GetFileName(ConfigPath);
            StagedConfigPath = Path.Combine(OutputDir, string.IsNullOrWhiteSpace(configName) ? "efcpt-config.json" : configName);
            File.Copy(ConfigPath, StagedConfigPath, overwrite: true);

            var renamingName = Path.GetFileName(RenamingPath);
            StagedRenamingPath = Path.Combine(OutputDir, string.IsNullOrWhiteSpace(renamingName) ? "efcpt.renaming.json" : renamingName);
            File.Copy(RenamingPath, StagedRenamingPath, overwrite: true);

            var templateName = new DirectoryInfo(TemplateDir).Name;
            StagedTemplateDir = Path.Combine(OutputDir, string.IsNullOrWhiteSpace(templateName) ? "Template" : templateName);
            var sourceTemplate = Path.GetFullPath(TemplateDir);
            var destTemplate = Path.GetFullPath(StagedTemplateDir);
            if (!string.Equals(sourceTemplate, destTemplate, StringComparison.OrdinalIgnoreCase))
            {
                CopyDirectory(sourceTemplate, destTemplate);
            }

            log.Detail($"Staged config: {StagedConfigPath}");
            log.Detail($"Staged renaming: {StagedRenamingPath}");
            log.Detail($"Staged template: {StagedTemplateDir}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        if (Directory.Exists(destDir))
            Directory.Delete(destDir, recursive: true);

        Directory.CreateDirectory(destDir);

        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(destDir, rel));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
