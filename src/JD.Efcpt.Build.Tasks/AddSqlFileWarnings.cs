using System.Text;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that adds auto-generation warning headers to SQL script files.
/// </summary>
/// <remarks>
/// <para>
/// This task scans SQL script files and adds a standardized warning header to inform users
/// that the files are auto-generated and should not be manually edited.
/// </para>
/// </remarks>
public sealed class AddSqlFileWarnings : Task
{
    /// <summary>
    /// Directory containing SQL script files.
    /// </summary>
    [Required]
    public string ScriptsDirectory { get; set; } = "";

    /// <summary>
    /// Database name for the warning header.
    /// </summary>
    public string DatabaseName { get; set; } = "";

    /// <summary>
    /// Log verbosity level.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Output parameter: Number of files processed.
    /// </summary>
    [Output]
    public int FilesProcessed { get; set; }

    /// <summary>
    /// Executes the task.
    /// </summary>
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);

        try
        {
            log.Info("Adding auto-generation warnings to SQL files...");

            if (!Directory.Exists(ScriptsDirectory))
            {
                log.Warn($"Scripts directory not found: {ScriptsDirectory}");
                return true; // Not an error
            }

            // Find all SQL files
            var sqlFiles = Directory.GetFiles(ScriptsDirectory, "*.sql", SearchOption.AllDirectories);

            FilesProcessed = 0;
            foreach (var sqlFile in sqlFiles)
            {
                try
                {
                    AddWarningHeader(sqlFile, log);
                    FilesProcessed++;
                }
                catch (Exception ex)
                {
                    log.Warn($"Failed to process {Path.GetFileName(sqlFile)}: {ex.Message}");
                }
            }

            log.Info($"Processed {FilesProcessed} SQL files");
            return true;
        }
        catch (Exception ex)
        {
            log.Error("JD0025", $"Failed to add SQL file warnings: {ex.Message}");
            log.Detail($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Adds warning header to a SQL file if not already present.
    /// </summary>
    private void AddWarningHeader(string filePath, IBuildLog log)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8);

        // Check if warning already exists
        if (content.Contains("AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY"))
        {
            log.Detail($"Warning already present: {Path.GetFileName(filePath)}");
            return;
        }

        var header = new StringBuilder();
        header.AppendLine("/*");
        header.AppendLine(" * ============================================================================");
        header.AppendLine(" * AUTO-GENERATED FILE - DO NOT EDIT DIRECTLY");
        header.AppendLine(" * ============================================================================");
        header.AppendLine(" *");
        
        if (!string.IsNullOrEmpty(DatabaseName))
        {
            header.AppendLine($" * This file was automatically generated from database: {DatabaseName}");
        }

        header.AppendLine($" * Generator: JD.Efcpt.Build (Database-First SqlProj Generation)");
        header.AppendLine(" *");
        header.AppendLine(" * IMPORTANT:");
        header.AppendLine(" * - Changes to this file may be overwritten during the next generation.");
        header.AppendLine(" * - To preserve custom changes, configure the generation process");
        header.AppendLine(" *   or create separate files that will not be regenerated.");
        header.AppendLine(" * - To extend the database with custom scripts or seeded data,");
        header.AppendLine(" *   add them to the SQL project separately.");
        header.AppendLine(" *");
        header.AppendLine(" * For more information:");
        header.AppendLine(" * https://github.com/jerrettdavis/JD.Efcpt.Build");
        header.AppendLine(" * ============================================================================");
        header.AppendLine(" */");
        header.AppendLine();

        // Prepend header to content
        var newContent = header.ToString() + content;

        // Write back to file
        File.WriteAllText(filePath, newContent, Encoding.UTF8);

        log.Detail($"Added warning: {Path.GetFileName(filePath)}");
    }
}
