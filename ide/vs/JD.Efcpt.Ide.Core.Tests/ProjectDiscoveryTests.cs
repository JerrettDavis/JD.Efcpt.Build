using System.Collections.Generic;
using JD.Efcpt.Ide.Core;
using Xunit;

namespace JD.Efcpt.Ide.Core.Tests;

/// <summary>
/// Exercises <see cref="ProjectDiscovery"/>, mirroring
/// <c>ide/vscode/src/test/unit/projectDiscovery.test.ts</c>.
/// </summary>
public sealed class ProjectDiscoveryTests
{
    private const string CsprojWithReference = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="JD.Efcpt.Build" Version="1.2.3" />
          </ItemGroup>
        </Project>
        """;

    private const string CsprojWithoutReference = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void HasJdEfcptPackageReference_true_for_a_matching_PackageReference()
    {
        Assert.True(ProjectDiscovery.HasJdEfcptPackageReference(CsprojWithReference));
    }

    [Fact]
    public void HasJdEfcptPackageReference_false_when_no_matching_reference()
    {
        Assert.False(ProjectDiscovery.HasJdEfcptPackageReference(CsprojWithoutReference));
    }

    [Fact]
    public void HasJdEfcptPackageReference_is_case_insensitive_and_attribute_order_tolerant()
    {
        const string csproj = "<PackageReference Version=\"1.0.0\" Include=\"jd.efcpt.build\" />";
        Assert.True(ProjectDiscovery.HasJdEfcptPackageReference(csproj));
    }

    [Fact]
    public void HasJdEfcptPackageReference_false_for_empty_content()
    {
        Assert.False(ProjectDiscovery.HasJdEfcptPackageReference(string.Empty));
    }

    [Fact]
    public void DiscoverJdEfcptProjects_filters_to_matching_projects_only()
    {
        var paths = new List<string> { "A.csproj", "B.csproj", "C.csproj" };
        var contents = new Dictionary<string, string>
        {
            ["A.csproj"] = CsprojWithReference,
            ["B.csproj"] = CsprojWithoutReference,
            ["C.csproj"] = CsprojWithReference
        };

        var result = ProjectDiscovery.DiscoverJdEfcptProjects(paths, p => contents[p]);

        Assert.Equal(new[] { "A.csproj", "C.csproj" }, result);
    }

    [Fact]
    public void DiscoverJdEfcptProjects_skips_files_that_fail_to_read_rather_than_throwing()
    {
        var paths = new List<string> { "A.csproj", "Missing.csproj" };

        var result = ProjectDiscovery.DiscoverJdEfcptProjects(paths, p =>
            p == "Missing.csproj" ? throw new System.IO.FileNotFoundException() : CsprojWithReference);

        Assert.Equal(new[] { "A.csproj" }, result);
    }

    [Fact]
    public void DiscoverJdEfcptProjects_returns_empty_for_no_candidates()
    {
        var result = ProjectDiscovery.DiscoverJdEfcptProjects(new List<string>(), _ => string.Empty);
        Assert.Empty(result);
    }
}
