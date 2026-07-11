namespace JD.Efcpt.Build.Core;

/// <summary>
/// Shared path-manipulation helpers used across the efcpt MSBuild pipeline and the jd-efcpt CLI.
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// Resolves <paramref name="path"/> to a full path, relative to <paramref name="baseDir"/>
    /// when it is not already rooted.
    /// </summary>
    public static string FullPath(string path, string baseDir)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        // Handle null/empty baseDir by using current directory
        // This can happen when MSBuild sets properties to null on .NET Framework
        if (string.IsNullOrWhiteSpace(baseDir))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(baseDir, path));
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="s"/> is non-null and non-whitespace.</summary>
    public static bool HasValue(string? s) => !string.IsNullOrWhiteSpace(s);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="s"/> looks like an explicit path
    /// (rooted, or contains a directory separator) rather than a bare command name.
    /// </summary>
    public static bool HasExplicitPath(string? s)
        => !string.IsNullOrWhiteSpace(s)
           && (Path.IsPathRooted(s)
               || s.Contains(Path.DirectorySeparatorChar)
               || s.Contains(Path.AltDirectorySeparatorChar));
}
