namespace JD.Efcpt.Cli.Tests;

/// <summary>
/// Collection marker for this assembly's tests. No module-initializer setup is needed - the CLI
/// commands under test are driven directly (in-process, via their testable
/// <c>ExecuteAsync</c>/<c>Execute</c> methods), so no MSBuild or SQLite bootstrap applies here.
/// </summary>
public static class AssemblySetup
{
}
