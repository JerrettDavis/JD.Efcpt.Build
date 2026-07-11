using System;
using System.IO;
using System.Linq;
using JD.Efcpt.Ide.Core;
using Xunit;

namespace JD.Efcpt.Ide.Core.Tests;

/// <summary>
/// Exercises <see cref="BuildProfileReader"/> against fixtures that mirror the REAL schema
/// written by <c>JD.Efcpt.Build.Tasks.Profiling.BuildRunOutput</c> (the same fixtures used by
/// <c>ide/vscode/src/test/unit/buildProfile.test.ts</c>, copied into <c>Fixtures/</c>).
/// </summary>
public sealed class BuildProfileReaderTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void Parse_parses_a_valid_v1_profile_fixture()
    {
        var profile = BuildProfileReader.Parse(ReadFixture("build-profile.success.json"));

        Assert.Equal("1.0.0", profile.SchemaVersion);
        Assert.True(profile.SchemaSupported);
        Assert.Equal("Success", profile.Status);
        Assert.Equal(BuildProfileStatus.Success, profile.StatusValue);
        Assert.Equal(2, profile.ModelCount);
        Assert.Equal(3, profile.Artifacts.Count);
        Assert.Single(profile.Diagnostics);
        Assert.Equal("MyProject", profile.ProjectName);
    }

    [Fact]
    public void Parse_normalizes_the_diagnostic_level_field_into_lowercase_severity()
    {
        var success = BuildProfileReader.Parse(ReadFixture("build-profile.success.json"));
        Assert.Equal("warning", success.Diagnostics[0].Severity);
        Assert.Equal("JD0001", success.Diagnostics[0].Code);

        var failed = BuildProfileReader.Parse(ReadFixture("build-profile.failed.json"));
        Assert.Equal("error", failed.Diagnostics[0].Severity);
        Assert.Equal("JD0011", failed.Diagnostics[0].Code);
    }

    [Fact]
    public void Parse_defaults_diagnostic_severity_to_info_when_level_is_absent()
    {
        var json = """
            {
              "schemaVersion": "1.0.0",
              "status": "Success",
              "diagnostics": [
                { "code": "JD0000", "message": "no level at all" }
              ]
            }
            """;

        var profile = BuildProfileReader.Parse(json);

        Assert.Equal("info", profile.Diagnostics[0].Severity);
    }

    [Fact]
    public void Parse_counts_only_artifacts_of_type_GeneratedModel()
    {
        var profile = BuildProfileReader.Parse(ReadFixture("build-profile.success.json"));

        var nonModelArtifacts = profile.Artifacts.Where(a => a.Type != "GeneratedModel").ToList();
        Assert.Single(nonModelArtifacts);
        Assert.Equal(2, profile.ModelCount);
    }

    [Fact]
    public void Parse_parses_a_failed_profile_with_diagnostics()
    {
        var profile = BuildProfileReader.Parse(ReadFixture("build-profile.failed.json"));

        Assert.Equal("Failed", profile.Status);
        Assert.Equal(BuildProfileStatus.Failed, profile.StatusValue);
        Assert.Equal(0, profile.ModelCount);
        Assert.NotEmpty(profile.Diagnostics);
    }

    [Fact]
    public void Parse_flags_an_unsupported_future_major_schema_version_without_throwing()
    {
        var profile = BuildProfileReader.Parse(ReadFixture("build-profile.futureSchema.json"));

        Assert.False(profile.SchemaSupported);
        Assert.Equal("Success", profile.Status);
    }

    [Fact]
    public void Parse_defaults_artifacts_and_diagnostics_to_empty_when_absent()
    {
        var profile = BuildProfileReader.Parse("""{ "schemaVersion": "1.0.0", "status": "Success" }""");

        Assert.Empty(profile.Artifacts);
        Assert.Empty(profile.Diagnostics);
        Assert.Equal(0, profile.ModelCount);
    }

    [Fact]
    public void Parse_parses_the_duration_field_as_an_iso8601_duration()
    {
        var profile = BuildProfileReader.Parse(ReadFixture("build-profile.success.json"));

        Assert.Equal(TimeSpan.FromSeconds(90), profile.Duration);
    }

    [Fact]
    public void Parse_throws_for_invalid_json()
    {
        Assert.Throws<BuildProfileParseException>(() => BuildProfileReader.Parse("{ not json"));
    }

    [Fact]
    public void Parse_throws_when_schemaVersion_is_missing()
    {
        Assert.Throws<BuildProfileParseException>(
            () => BuildProfileReader.Parse("""{ "status": "Success" }"""));
    }

    [Fact]
    public void Parse_throws_when_status_is_missing()
    {
        Assert.Throws<BuildProfileParseException>(
            () => BuildProfileReader.Parse("""{ "schemaVersion": "1.0.0" }"""));
    }

    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("0.9.0", true)]
    [InlineData("2.0.0", false)]
    [InlineData("not-a-version", false)]
    public void IsSchemaSupported_matches_the_MAJOR_version_gate(string schemaVersion, bool expected)
    {
        Assert.Equal(expected, BuildProfileReader.IsSchemaSupported(schemaVersion));
    }

    [Fact]
    public void ReadFile_reads_and_parses_a_fixture_from_disk()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "build-profile.success.json");

        var profile = BuildProfileReader.ReadFile(path);

        Assert.Equal("Success", profile.Status);
    }

    [Fact]
    public void ReadFile_throws_BuildProfileParseException_when_the_file_does_not_exist()
    {
        Assert.Throws<BuildProfileParseException>(
            () => BuildProfileReader.ReadFile(Path.Combine(AppContext.BaseDirectory, "does-not-exist.json")));
    }
}
