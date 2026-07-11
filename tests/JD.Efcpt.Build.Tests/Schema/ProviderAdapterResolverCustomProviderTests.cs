using JD.Efcpt.Build.Tasks.Schema;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

/// <summary>
/// Covers <see cref="ProviderAdapterResolver"/>'s custom-provider-assembly resolution path
/// (#184's <c>customProviders</c> plugin registry), added on top of the existing dynamic
/// satellite-package loading path covered by <c>ProviderAdapterResolverDynamicLoadTests</c>.
/// </summary>
/// <remarks>
/// Like <c>ProviderAdapterResolverDynamicLoadTests</c>, these tests stand in for a real
/// third-party custom provider assembly using <c>JD.Efcpt.Build.TestProvider.dll</c> - copied
/// into a temporary directory and renamed to an arbitrary custom assembly name (e.g.
/// <c>Acme.Efcpt.Mongo.dll</c>) rather than the <c>JD.Efcpt.Build.{Suffix}.dll</c> convention
/// built-in satellites use, since custom providers are keyed by an arbitrary registered assembly
/// name, not a fixed suffix map. The "no adapter" (JD0040) scenario reuses
/// <c>JD.Efcpt.Build.Providers.Abstractions.dll</c> itself, renamed the same way - it is a real,
/// loadable assembly that contains only the <see cref="IProviderAdapter"/> interface and no
/// concrete implementation, which is exactly the failure mode under test.
/// </remarks>
[Feature("ProviderAdapterResolver: custom provider assembly resolution (#184)")]
[Collection(nameof(AssemblySetup))]
public sealed class ProviderAdapterResolverCustomProviderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static readonly string TestProviderFixturePath =
        Path.Combine(AppContext.BaseDirectory, "JD.Efcpt.Build.TestProvider.dll");

    private static readonly string AbstractionsOnlyFixturePath =
        Path.Combine(AppContext.BaseDirectory, "JD.Efcpt.Build.Providers.Abstractions.dll");

    private static string CreateTempProviderDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "efcpt-custom-provider-resolver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    #region Successful Custom Load

    [Scenario("Loads and instantiates a custom provider adapter discovered via the custom assembly registry")]
    [Fact]
    public async Task Loads_custom_provider_adapter_from_registry()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            var destination = Path.Combine(tempDir, "Acme.Efcpt.Mongo.dll");

            await Given("a search-path directory containing a custom provider assembly, registered under a custom key", () =>
                {
                    File.Copy(TestProviderFixturePath, destination, overwrite: true);
                    return (tempDir, customMap: new Dictionary<string, string> { ["acme-mongo"] = "Acme.Efcpt.Mongo" });
                })
                .When("resolved via the satellite package path with the custom registry",
                    t => ProviderAdapterResolver.ResolveFromSatellitePackage("acme-mongo", [t.tempDir], t.customMap))
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

    #region JD0018: Assembly Not Found

    [Scenario("Throws CustomProviderException JD0018 when the registered custom provider assembly is absent from every search path")]
    [Fact]
    public async Task Throws_jd0018_when_custom_assembly_absent()
    {
        var missingDir = Path.Combine(Path.GetTempPath(), "efcpt-custom-provider-resolver-tests-missing-" + Guid.NewGuid().ToString("N"));
        var customMap = new Dictionary<string, string> { ["acme-mongo"] = "Acme.Efcpt.Mongo" };

        await Given("a custom provider key registered, but no assembly present anywhere", () => missingDir)
            .When("resolved via the satellite package path", dir =>
            {
                try
                {
                    ProviderAdapterResolver.ResolveFromSatellitePackage("acme-mongo", [dir], customMap);
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .Then("throws CustomProviderException", ex => ex is CustomProviderException)
            .And("with code JD0018", ex => ((CustomProviderException)ex!).Code == CustomProviderException.AssemblyLoadFailedCode)
            .AssertPassed();
    }

    #endregion

    #region JD0018: Load Failure

    [Scenario("Throws CustomProviderException JD0018 when the registered custom provider assembly is corrupt")]
    [Fact]
    public async Task Throws_jd0018_when_custom_assembly_corrupt()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            var destination = Path.Combine(tempDir, "Acme.Efcpt.Mongo.dll");
            var customMap = new Dictionary<string, string> { ["acme-mongo"] = "Acme.Efcpt.Mongo" };

            await Given("a search-path directory containing a corrupt file named like the registered custom provider assembly", () =>
                {
                    File.WriteAllBytes(destination, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);
                    return tempDir;
                })
                .When("resolved via the satellite package path", dir =>
                {
                    try
                    {
                        ProviderAdapterResolver.ResolveFromSatellitePackage("acme-mongo", [dir], customMap);
                        return (Exception?)null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                })
                .Then("throws CustomProviderException", ex => ex is CustomProviderException)
                .And("with code JD0018", ex => ((CustomProviderException)ex!).Code == CustomProviderException.AssemblyLoadFailedCode)
                .And("preserves the original CLR failure as InnerException", ex => ex!.InnerException is not null)
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region JD0040: No Adapter Found

    [Scenario("Throws CustomProviderException JD0040 when the registered custom provider assembly contains no IProviderAdapter")]
    [Fact]
    public async Task Throws_jd0040_when_custom_assembly_has_no_adapter()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            var destination = Path.Combine(tempDir, "Acme.Efcpt.Mongo.dll");
            var customMap = new Dictionary<string, string> { ["acme-mongo"] = "Acme.Efcpt.Mongo" };

            await Given("a search-path directory containing a real, loadable assembly with no IProviderAdapter implementation", () =>
                {
                    File.Copy(AbstractionsOnlyFixturePath, destination, overwrite: true);
                    return tempDir;
                })
                .When("resolved via the satellite package path", dir =>
                {
                    try
                    {
                        ProviderAdapterResolver.ResolveFromSatellitePackage("acme-mongo", [dir], customMap);
                        return (Exception?)null;
                    }
                    catch (Exception ex)
                    {
                        return ex;
                    }
                })
                .Then("throws CustomProviderException", ex => ex is CustomProviderException)
                .And("with code JD0040", ex => ((CustomProviderException)ex!).Code == CustomProviderException.NoAdapterFoundCode)
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Not Found / Unknown (no registry match - not an error)

    [Scenario("Returns null, not an exception, when the provider matches neither a built-in suffix nor the custom registry")]
    [Fact]
    public async Task Returns_null_when_provider_unknown_to_both_builtin_and_custom()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            await Given("an existing search-path directory and an empty custom registry", () => tempDir)
                .When("resolved via the satellite package path with an empty custom registry",
                    dir => ProviderAdapterResolver.ResolveFromSatellitePackage("totally-unknown", [dir], new Dictionary<string, string>()))
                .Then("returns null rather than throwing", adapter => adapter is null)
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Scenario("Built-in provider resolution is unaffected when a custom registry is also supplied")]
    [Fact]
    public async Task Builtin_provider_resolution_unaffected_by_custom_registry()
    {
        var tempDir = CreateTempProviderDirectory();
        try
        {
            var destination = Path.Combine(tempDir, "JD.Efcpt.Build.Firebird.dll");
            var customMap = new Dictionary<string, string> { ["firebird"] = "Acme.Efcpt.Mongo" };

            await Given("a search-path directory with the real built-in firebird assembly name, plus an (ignored) custom registry entry for the same key", () =>
                {
                    File.Copy(TestProviderFixturePath, destination, overwrite: true);
                    return tempDir;
                })
                .When("resolved via the satellite package path",
                    dir => ProviderAdapterResolver.ResolveFromSatellitePackage("firebird", [dir], customMap))
                .Then("returns a non-null adapter resolved via the built-in suffix map, not the custom registry",
                    adapter => adapter is not null)
                .AssertPassed();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region JD0018: ReflectionTypeLoadException detail flattening (missing transitive dependency)

    [Scenario("FlattenLoaderExceptions surfaces the underlying loader-exception detail, not the generic type-load message")]
    [Fact]
    public async Task Flattens_loader_exceptions_into_detail()
    {
        await Given("a ReflectionTypeLoadException carrying a real missing-dependency loader exception", () =>
                new System.Reflection.ReflectionTypeLoadException(
                    classes: [null],
                    exceptions: [new FileNotFoundException("Could not load file or assembly 'MongoDB.Driver, Version=2.0.0.0'.")]))
            .When("flattened", ex => ProviderAdapterResolver.FlattenLoaderExceptions(ex))
            .Then("contains the actual missing dependency", detail => detail.Contains("MongoDB.Driver"))
            .And("does not degrade to the generic type-load wording",
                detail => !detail.Contains("Unable to load one or more of the requested types"))
            .AssertPassed();
    }

    [Scenario("FlattenLoaderExceptions joins multiple distinct loader-exception messages")]
    [Fact]
    public async Task Flattens_multiple_loader_exceptions()
    {
        await Given("a ReflectionTypeLoadException carrying two distinct loader exceptions", () =>
                new System.Reflection.ReflectionTypeLoadException(
                    classes: [null, null],
                    exceptions:
                    [
                        new FileNotFoundException("Could not load file or assembly 'Acme.One'."),
                        new FileNotFoundException("Could not load file or assembly 'Acme.Two'.")
                    ]))
            .When("flattened", ex => ProviderAdapterResolver.FlattenLoaderExceptions(ex))
            .Then("includes the first dependency", detail => detail.Contains("Acme.One"))
            .And("includes the second dependency", detail => detail.Contains("Acme.Two"))
            .And("joins them with a separator", detail => detail.Contains(";"))
            .AssertPassed();
    }

    #endregion
}
