namespace JD.Efcpt.Build.Tests.Infrastructure;

internal sealed class TestFolder : IDisposable
{
    public string Root { get; }

    public TestFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "efcpt-build-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string CreateDir(string relative)
    {
        var dir = Path.Combine(Root, relative);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string WriteFile(string relative, string contents)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); }
        catch { /* swallow cleanup failures */ }
    }
}

internal static class TestPaths
{
    public static string RepoRoot => _repoRoot.Value;
    public static string DefaultsRoot => Path.Combine(RepoRoot, "src", "JD.Efcpt.Build", "defaults");
    public static string Asset(string relative) => Path.Combine(RepoRoot, "tests", "TestAssets", relative);
    public static string DotNetExe => "dotnet";

    private static readonly Lazy<string> _repoRoot = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "JD.Efcpt.Build.sln");
            if (File.Exists(candidate)) return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate repo root (JD.Efcpt.Build.sln).");
    });
}

internal static class TestFileSystem
{
    public static void CopyDirectory(string sourceDir, string destDir)
    {
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

    public static void MakeExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        const UnixFileMode mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                  UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                  UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
        File.SetUnixFileMode(path, mode);
    }
}
