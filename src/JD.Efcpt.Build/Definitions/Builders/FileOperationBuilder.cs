using JD.MSBuild.Fluent.Fluent;
using JDEfcptBuild.Constants;

namespace JDEfcptBuild.Builders;

/// <summary>
/// Simplifies common file and directory operations in MSBuild targets.
/// Eliminates repetitive task configuration patterns.
/// </summary>
public static class FileOperationBuilder
{
    /// <summary>
    /// Adds a MakeDir task to create a directory.
    /// </summary>
    public static void AddMakeDir(TargetBuilder target, string dirProperty)
    {
        target.Task(MsBuildTasks.MakeDir, task =>
        {
            task.Param(TaskParameters.Directories, MsBuildExpressions.Property(dirProperty));
        });
    }
    
    /// <summary>
    /// Adds a Copy task to copy files.
    /// </summary>
    public static void AddCopy(TargetBuilder target, string sourceItem, string destDir, string? condition = null)
    {
        target.Task(MsBuildTasks.Copy, task =>
        {
            task.Param(TaskParameters.SourceFiles, $"@({sourceItem})");
            task.Param(TaskParameters.DestinationFolder, destDir);
            task.Param("SkipUnchangedFiles", PropertyValues.True);
        }, condition);
    }
    
    /// <summary>
    /// Adds a Delete task to delete files.
    /// </summary>
    public static void AddDelete(TargetBuilder target, string filesItem, string condition)
    {
        target.Task(MsBuildTasks.Delete, task =>
        {
            task.Param(TaskParameters.Files, $"@({filesItem})");
        }, condition);
    }
    
    /// <summary>
    /// Adds a RemoveDir task to remove directories.
    /// </summary>
    public static void AddRemoveDir(TargetBuilder target, string dirProperty)
    {
        target.Task(MsBuildTasks.RemoveDir, task =>
        {
            task.Param(TaskParameters.Directories, MsBuildExpressions.Property(dirProperty));
        });
    }
}
