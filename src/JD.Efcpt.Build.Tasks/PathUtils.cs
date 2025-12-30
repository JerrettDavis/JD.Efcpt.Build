namespace JD.Efcpt.Build.Tasks;

internal static class PathUtils
{
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

    public static bool HasValue(string? s) => !string.IsNullOrWhiteSpace(s);

    public static bool HasExplicitPath(string? s)
        => !string.IsNullOrWhiteSpace(s)
           && (Path.IsPathRooted(s)
               || s.Contains(Path.DirectorySeparatorChar)
               || s.Contains(Path.AltDirectorySeparatorChar));
}
