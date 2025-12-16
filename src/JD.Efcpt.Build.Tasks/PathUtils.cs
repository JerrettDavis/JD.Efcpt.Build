namespace JD.Efcpt.Build.Tasks;

internal static class PathUtils
{
    public static string FullPath(string path, string baseDir)
        => string.IsNullOrWhiteSpace(path)
            ? path
            : Path.GetFullPath(Path.IsPathRooted(path)
                ? path
                : Path.Combine(baseDir, path));

    public static bool HasValue(string? s) => !string.IsNullOrWhiteSpace(s);

    public static bool HasExplicitPath(string? s)
        => !string.IsNullOrWhiteSpace(s)
           && (Path.IsPathRooted(s) 
               || s.Contains(Path.DirectorySeparatorChar) 
               || s.Contains(Path.AltDirectorySeparatorChar));
}
