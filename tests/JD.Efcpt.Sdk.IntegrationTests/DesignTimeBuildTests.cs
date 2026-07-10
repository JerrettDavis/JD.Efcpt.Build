using FluentAssertions;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Verifies the design-time build guard: the EF Core Power Tools generation pipeline must be
/// skipped entirely when MSBuild reports DesignTimeBuild=true (as IDEs do for IntelliSense),
/// unless the user opts back in via EfcptRunDuringDesignTimeBuild=true.
/// </summary>
[Collection("Design-Time Build Tests")]
public class DesignTimeBuildTests : IDisposable
{
    private const string SkippedMessage = "[Efcpt] Skipping EF Core Power Tools generation pipeline";

    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public DesignTimeBuildTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    [Fact]
    public async Task DesignTimeBuild_SkipsGenerationPipeline()
    {
        // Arrange
        TestProjectBuilder.CopyDatabaseProject(SdkPackageTestFixture.GetTestFixturesPath());
        _builder.CreateSdkProject("DtbSkipProject", "net8.0");

        // Act - simulate what Visual Studio/Rider pass for a design-time build
        var buildResult = await _builder.BuildAsync("-v:d -p:DesignTimeBuild=true");

        // Assert
        buildResult.Success.Should().BeTrue($"Design-time build should still succeed.\n{buildResult}");
        buildResult.Output.Should().Contain(SkippedMessage,
            "the DTB guard should log why the pipeline was skipped");
        _builder.GetGeneratedFiles().Should().BeEmpty(
            "no EF Core models should be generated during a design-time build");
    }

    [Fact]
    public async Task NonDesignTimeBuild_RunsGenerationPipeline()
    {
        // Arrange
        TestProjectBuilder.CopyDatabaseProject(SdkPackageTestFixture.GetTestFixturesPath());
        _builder.CreateSdkProject("DtbNormalProject", "net8.0");

        // Act - a normal build (DesignTimeBuild unset/false)
        var buildResult = await _builder.BuildAsync("-v:d");

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        buildResult.Output.Should().NotContain(SkippedMessage,
            "a normal build should not be treated as a design-time build");
        _builder.GetGeneratedFiles().Should().NotBeEmpty(
            $"EF Core models should be generated during a normal build.\n{buildResult}");
    }

    [Fact]
    public async Task DesignTimeBuild_WithOverride_RunsGenerationPipeline()
    {
        // Arrange
        TestProjectBuilder.CopyDatabaseProject(SdkPackageTestFixture.GetTestFixturesPath());
        _builder.CreateSdkProject("DtbOverrideProject", "net8.0");
        _builder.AddProjectProperty("EfcptRunDuringDesignTimeBuild", "true");

        // Act - design-time build, but the user explicitly opted back in
        var buildResult = await _builder.BuildAsync("-v:d -p:DesignTimeBuild=true");

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        buildResult.Output.Should().NotContain(SkippedMessage,
            "the pipeline should not be skipped once the user opts back in");
        _builder.GetGeneratedFiles().Should().NotBeEmpty(
            "EF Core models should still be generated when EfcptRunDuringDesignTimeBuild=true");
    }
}

[CollectionDefinition("Design-Time Build Tests", DisableParallelization = true)]
public class DesignTimeBuildTestCollection : ICollectionFixture<SdkPackageTestFixture> { }
