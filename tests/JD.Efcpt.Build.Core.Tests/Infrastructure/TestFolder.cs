namespace JD.Efcpt.Build.Core.Tests.Infrastructure;

/// <summary>
/// A scratch temp directory, cleaned up on <see cref="Dispose"/>. Self-contained copy of
/// <c>JD.Efcpt.Build.Tests.Infrastructure.TestFolder</c>'s pattern - this project cannot
/// reference <c>JD.Efcpt.Build.Tasks</c> (it would reintroduce the MSBuild dependency
/// <c>JD.Efcpt.Build.Core</c> is deliberately free of).
/// </summary>
internal sealed class TestFolder : IDisposable
{
    public string Root { get; }

    public TestFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "efcpt-core-tests", Guid.NewGuid().ToString("N"));
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
