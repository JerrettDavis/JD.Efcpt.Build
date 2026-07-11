namespace JD.Efcpt.Cli.Tests.Infrastructure;

/// <summary>
/// A scratch temp directory, cleaned up on <see cref="Dispose"/>. Self-contained copy of the
/// pattern used across the other test projects in this repo (see e.g.
/// <c>JD.Efcpt.Build.Core.Tests.Infrastructure.TestFolder</c>).
/// </summary>
internal sealed class TestFolder : IDisposable
{
    public string Root { get; }

    public TestFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "efcpt-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, recursive: true); }
        catch { /* swallow cleanup failures */ }
    }
}
