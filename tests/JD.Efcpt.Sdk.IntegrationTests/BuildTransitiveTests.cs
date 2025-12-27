using FluentAssertions;
using System.IO.Compression;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Tests that verify the buildTransitive content is correctly packaged in the SDK.
/// </summary>
[Collection("Package Content Tests")]
public class BuildTransitiveTests
{
    private readonly SdkPackageTestFixture _fixture;

    public BuildTransitiveTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SdkPackage_ContainsSdkFolder()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("Sdk/"), "SDK package should contain Sdk folder");
    }

    [Fact]
    public void SdkPackage_ContainsSdkProps()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain("Sdk/Sdk.props", "SDK package should contain Sdk/Sdk.props");
    }

    [Fact]
    public void SdkPackage_ContainsSdkTargets()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain("Sdk/Sdk.targets", "SDK package should contain Sdk/Sdk.targets");
    }

    [Fact]
    public void SdkPackage_ContainsBuildFolder()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("build/"), "SDK package should contain build folder");
    }

    [Fact]
    public void SdkPackage_ContainsBuildTransitiveFolder()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("buildTransitive/"), "SDK package should contain buildTransitive folder");
    }

    [Fact]
    public void SdkPackage_ContainsBuildTransitiveProps()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain("buildTransitive/JD.Efcpt.Build.props", "SDK package should contain buildTransitive props");
    }

    [Fact]
    public void SdkPackage_ContainsBuildTransitiveTargets()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain("buildTransitive/JD.Efcpt.Build.targets", "SDK package should contain buildTransitive targets");
    }

    [Fact]
    public void SdkPackage_ContainsTasksFolder()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("tasks/"), "SDK package should contain tasks folder");
    }

    [Fact]
    public void SdkPackage_ContainsNet80Tasks()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("tasks/net8.0/") && e.EndsWith(".dll"),
            "SDK package should contain net8.0 task assemblies");
    }

    [Fact]
    public void SdkPackage_ContainsNet90Tasks()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("tasks/net9.0/") && e.EndsWith(".dll"),
            "SDK package should contain net9.0 task assemblies");
    }

    [Fact]
    public void SdkPackage_ContainsNet100Tasks()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.StartsWith("tasks/net10.0/") && e.EndsWith(".dll"),
            "SDK package should contain net10.0 task assemblies");
    }

    [Fact]
    public void SdkPackage_ContainsDefaultsFolder()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.Contains("Defaults/"), "SDK package should contain Defaults folder");
    }

    [Fact]
    public void SdkPackage_ContainsDefaultConfig()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.Contains("efcpt-config.json"), "SDK package should contain default config file");
    }

    [Fact]
    public void SdkPackage_ContainsT4Templates()
    {
        var entries = GetPackageEntries(_fixture.SdkPackagePath);
        entries.Should().Contain(e => e.EndsWith(".t4"), "SDK package should contain T4 templates");
    }

    [Fact]
    public void BuildPackage_ContainsBuildFolder()
    {
        var entries = GetPackageEntries(_fixture.BuildPackagePath);
        entries.Should().Contain(e => e.StartsWith("build/"), "Build package should contain build folder");
    }

    [Fact]
    public void BuildPackage_ContainsBuildTransitiveFolder()
    {
        var entries = GetPackageEntries(_fixture.BuildPackagePath);
        entries.Should().Contain(e => e.StartsWith("buildTransitive/"), "Build package should contain buildTransitive folder");
    }

    [Fact]
    public void SdkAndBuildPackages_HaveMatchingBuildTransitiveContent()
    {
        var sdkEntries = GetPackageEntries(_fixture.SdkPackagePath)
            .Where(e => e.StartsWith("buildTransitive/") && !e.EndsWith("/"))
            .Select(e => e.Replace("buildTransitive/", ""))
            .ToHashSet();

        var buildEntries = GetPackageEntries(_fixture.BuildPackagePath)
            .Where(e => e.StartsWith("buildTransitive/") && !e.EndsWith("/"))
            .Select(e => e.Replace("buildTransitive/", ""))
            .ToHashSet();

        // SDK and Build should have matching buildTransitive content
        sdkEntries.Should().BeEquivalentTo(buildEntries,
            "SDK and Build packages should have matching buildTransitive content");
    }

    private static List<string> GetPackageEntries(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        return archive.Entries.Select(e => e.FullName).ToList();
    }
}
