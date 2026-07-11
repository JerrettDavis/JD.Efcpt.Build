using System;
using System.Collections.Generic;
using System.IO;
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

        Assert.Equal(new[] { "A.csproj", "C.csproj" }, result.Matches);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void DiscoverJdEfcptProjects_records_unreadable_files_as_skipped_with_reason()
    {
        var paths = new List<string> { "A.csproj", "Locked.csproj", "Denied.csproj" };

        var result = ProjectDiscovery.DiscoverJdEfcptProjects(paths, p => p switch
        {
            "Locked.csproj" => throw new IOException("file is locked"),
            "Denied.csproj" => throw new UnauthorizedAccessException("access denied"),
            _ => CsprojWithReference
        });

        // The matching project is still found...
        Assert.Equal(new[] { "A.csproj" }, result.Matches);

        // ...and the unreadable ones are recorded with their reason, not silently dropped, so a
        // permissions/lock error is distinguishable from "no JD.Efcpt project".
        Assert.Equal(2, result.Skipped.Count);
        Assert.Contains(result.Skipped, s => s.Path == "Locked.csproj" && s.Reason.Contains("locked"));
        Assert.Contains(result.Skipped, s => s.Path == "Denied.csproj" && s.Reason.Contains("denied"));
    }

    [Fact]
    public void DiscoverJdEfcptProjects_does_not_swallow_unexpected_exception_types()
    {
        var paths = new List<string> { "Boom.csproj" };

        // Only IOException / UnauthorizedAccessException are treated as skippable read failures; an
        // unexpected exception type must propagate rather than be silently swallowed.
        Assert.Throws<InvalidOperationException>(() =>
            ProjectDiscovery.DiscoverJdEfcptProjects(paths, _ => throw new InvalidOperationException("bug")));
    }

    [Fact]
    public void DiscoverJdEfcptProjects_returns_empty_for_no_candidates()
    {
        var result = ProjectDiscovery.DiscoverJdEfcptProjects(new List<string>(), _ => string.Empty);
        Assert.Empty(result.Matches);
        Assert.Empty(result.Skipped);
    }
}
