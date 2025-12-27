namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Provides helper methods for common file system operations.
/// </summary>
internal static class FileSystemHelpers
{
    /// <summary>
    /// Copies an entire directory tree from source to destination.
    /// </summary>
    /// <param name="sourceDir">The source directory to copy from.</param>
    /// <param name="destDir">The destination directory to copy to.</param>
    /// <param name="overwrite">If true (default), deletes the destination directory if it exists before copying.</param>
    /// <exception cref="ArgumentNullException">Thrown when sourceDir or destDir is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the source directory does not exist.</exception>
    /// <remarks>
    /// <para>
    /// This method recursively copies all files and subdirectories from the source directory
    /// to the destination directory. If <paramref name="overwrite"/> is true and the destination
    /// directory already exists, it will be deleted before copying.
    /// </para>
    /// <para>
    /// The directory structure is preserved, including empty subdirectories.
    /// </para>
    /// </remarks>
    public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = true)
    {
        ArgumentNullException.ThrowIfNull(sourceDir);
        ArgumentNullException.ThrowIfNull(destDir);

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        if (overwrite && Directory.Exists(destDir))
            Directory.Delete(destDir, recursive: true);

        Directory.CreateDirectory(destDir);

        // Create all subdirectories first
        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(destDir, relativePath));
        }

        // Copy all files
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);

            // Ensure parent directory exists (handles edge cases)
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, overwrite: true);
        }
    }

    /// <summary>
    /// Deletes a directory if it exists.
    /// </summary>
    /// <param name="path">The directory path to delete.</param>
    /// <param name="recursive">If true (default), deletes all contents recursively.</param>
    /// <returns>True if the directory was deleted, false if it didn't exist.</returns>
    public static bool DeleteDirectoryIfExists(string path, bool recursive = true)
    {
        if (!Directory.Exists(path))
            return false;

        Directory.Delete(path, recursive);
        return true;
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The directory path to ensure exists.</param>
    /// <returns>The DirectoryInfo for the directory.</returns>
    public static DirectoryInfo EnsureDirectoryExists(string path)
    {
        return Directory.CreateDirectory(path);
    }
}
