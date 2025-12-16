using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that normalizes generated C# file names by renaming them to the <c>.g.cs</c> convention.
/// </summary>
/// <remarks>
/// <para>
/// This task is invoked from the <c>EfcptGenerateModels</c> target after efcpt has produced C# files in
/// <see cref="GeneratedDir"/>. It walks all <c>*.cs</c> files under that directory, skipping any that
/// already end with <c>.g.cs</c>, and renames the remaining files so that their suffix becomes
/// <c>.g.cs</c>.
/// </para>
/// <para>
/// If a destination file already exists with the desired name, it is deleted before the source file is
/// moved. When <see cref="GeneratedDir"/> does not exist, the task exits successfully without making any
/// changes.
/// </para>
/// </remarks>
public sealed class RenameGeneratedFiles : Task
{
    /// <summary>
    /// Root directory that contains the generated C# files to be normalized.
    /// </summary>
    [Required] public string GeneratedDir { get; set; } = "";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    /// <value>
    /// When set to <c>detailed</c>, the task logs each rename operation it performs.
    /// </value>
    public string LogVerbosity { get; set; } = "minimal";

    /// <inheritdoc />
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);
        try
        {
            if (!Directory.Exists(GeneratedDir))
                return true;

            foreach (var file in Directory.EnumerateFiles(GeneratedDir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var newPath = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + ".g.cs");
                if (File.Exists(newPath))
                    File.Delete(newPath);

                File.Move(file, newPath);
                log.Detail($"Renamed: {file} -> {newPath}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }
}
