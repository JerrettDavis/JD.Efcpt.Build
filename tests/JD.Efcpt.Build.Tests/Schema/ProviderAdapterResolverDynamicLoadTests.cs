using JD.Efcpt.Build.Tasks.Schema;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

/// <summary>
/// Covers <see cref="ProviderAdapterResolver"/>'s dynamic, reflection-based satellite-package
/// loading path (<see cref="ProviderAdapterResolver.ResolveFromSatellitePackage"/>), which the
/// existing <c>ProviderAdapterResolverTests</c> never exercises because every provider currently
/// resolves in-assembly.
/// </summary>
/// <remarks>
/// These tests stand in for a real satellite provider package (e.g. <c>JD.Efcpt.Build.Firebird</c>)
/// using <c>JD.Efcpt.Build.TestProvider.dll</c>, a tiny fixture assembly built alongside this test
/// project (see <c>tests/Fixtures/JD.Efcpt.Build.TestProvider</c>) that contains a single, real
/// <see cref="IProviderAdapter"/> implementation. The fixture DLL is copied into a temporary
/// "providers" directory and renamed to the <c>JD.Efcpt.Build.{Suffix}.dll</c> convention the
/// resolver expects, then discovered purely through the search-path mechanism - exactly like a
/// real satellite package would be. The fixture is never referenced by compile-time code here;
/// see its project remarks for why.
/// </remarks>
[Feature("ProviderAdapterResolver: dynamic satellite-package loading (reflection load path)")]
[Collection(nameof(AssemblySetup))]
public sealed class ProviderAdapterResolverDynamicLoadTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    /// <summary>
    /// Path to the built test-fixture adapter assembly, which
    /// <c>JD.Efcpt.Build.Tests.csproj</c> copies next to this test assembly via a
    /// <c>ProjectReference</c>.
    /// </summary>
    private static readonly string FixtureAssemblyPath =
        Path.Combine(AppContext.BaseDirectory, "JD.Efcpt.Build.TestProvider.dll");

    private static string CreateTempProviderDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "efcpt-provider-resolver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    #region Successful Satellite Load

    [Scenario("Loads and instantiates a satellite adapter discovered via a caller-supplied search path")]
    [Fact]
    public async Task Loads_satellite_adapter_from_search_path()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            var destination = Path.Combine(tempDir, "JD.Efcpt.Build.Firebird.dll");

            await Given("a search-path directory containing a satellite adapter assembly", () =>
                {
                    File.Copy(FixtureAssemblyPath, destination, overwrite: true);
                    return tempDir;
                })
                .When("resolved directly via the satellite package path",
                    dir => ProviderAdapterResolver.ResolveFromSatellitePackage("firebird", [dir]))
                .Then("returns a non-null adapter", adapter => adapter is not null)
                .And("the adapter is genuinely usable through the shared abstraction types",
                    adapter => adapter!.CreateSchemaReader().ReadSchema("ignored") == SchemaModel.Empty)
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Load Failure Wrapping (fix under test: never leak a raw CLR exception)

    [Scenario("Wraps a corrupt satellite assembly load failure in ProviderDriverNotFoundException with install guidance")]
    [Fact]
    public async Task Wraps_corrupt_satellite_assembly_load_failure()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            var destination = Path.Combine(tempDir, "JD.Efcpt.Build.Firebird.dll");

            await Given("a search-path directory containing a corrupt file named like a satellite adapter assembly", () =>
                {
                    // Deliberately not a valid PE image - simulates a corrupted or truncated
                    // package install. Before the fix under test, this would surface as a raw
                    // BadImageFormatException instead of an actionable ProviderDriverNotFoundException.
                    File.WriteAllBytes(destination, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);
                    return tempDir;
                })
                .When("resolved directly via the satellite package path", dir =>
                {
                    try
                    {
                        ProviderAdapterResolver.ResolveFromSatellitePackage("firebird", [dir]);
                        return (Exception?)null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                })
                .Then("throws ProviderDriverNotFoundException rather than a raw CLR exception",
                    ex => ex is ProviderDriverNotFoundException)
                .And("preserves the original CLR failure as InnerException",
                    ex => ex!.InnerException is not null)
                .And("Provider matches the provider being resolved",
                    ex => ((ProviderDriverNotFoundException)ex!).Provider == "firebird")
                .And("message contains the exact dotnet add package install command",
                    ex => ex!.Message.Contains("dotnet add package JD.Efcpt.Build.Firebird"))
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Not Found (no matching assembly - not an error)

    [Scenario("Returns null, not an exception, when no search path contains a matching assembly")]
    [Fact]
    public async Task Returns_null_when_no_search_path_matches()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), "efcpt-provider-resolver-tests-missing-" + Guid.NewGuid().ToString("N"));

        await Given("a search path that does not exist on disk", () => missingDir)
            .When("resolved directly via the satellite package path",
                dir => ProviderAdapterResolver.ResolveFromSatellitePackage("firebird", [dir]))
            .Then("returns null rather than throwing", adapter => adapter is null)
            .AssertPassed();
    }

    [Scenario("Returns null when the search-path directory exists but has no matching assembly file")]
    [Fact]
    public async Task Returns_null_when_directory_exists_but_file_missing()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            await Given("an existing, empty search-path directory", () => tempDir)
                .When("resolved directly via the satellite package path",
                    dir => ProviderAdapterResolver.ResolveFromSatellitePackage("firebird", [dir]))
                .Then("returns null rather than throwing", adapter => adapter is null)
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
