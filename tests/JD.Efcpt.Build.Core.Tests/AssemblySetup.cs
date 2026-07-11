namespace JD.Efcpt.Build.Core.Tests;

/// <summary>
/// Collection marker for this assembly's tests. Unlike
/// <c>JD.Efcpt.Build.Tests.AssemblySetup</c>, this project needs no MSBuildLocator/SQLite
/// module-initializer setup - it exercises only <c>JD.Efcpt.Build.Core</c>, which has no MSBuild
/// or ADO.NET provider dependency. Kept only so <c>[Collection(nameof(AssemblySetup))]</c> groups
/// every test in this assembly onto a single xunit collection (matching the sibling test
/// projects' convention) for predictable, non-parallel-across-collections execution.
/// </summary>
public static class AssemblySetup
{
}
