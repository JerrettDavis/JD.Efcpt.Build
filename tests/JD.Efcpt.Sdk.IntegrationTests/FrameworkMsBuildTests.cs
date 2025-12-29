using FluentAssertions;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Tests that validate native .NET Framework MSBuild task loading.
/// These tests use MSBuild.exe (Framework MSBuild) to verify that code generation
/// works correctly when building with Visual Studio's Framework MSBuild using
/// the native net472 task assembly.
/// </summary>
/// <remarks>
/// These tests are skipped if MSBuild.exe is not available (e.g., on CI without VS).
/// The net472 task assembly is loaded natively by Framework MSBuild without any
/// fallback mechanism - this is the primary validation that VS builds work.
/// </remarks>
[Collection("Framework MSBuild Tests")]
public class FrameworkMsBuildTests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public FrameworkMsBuildTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    /// <summary>
    /// Verifies that the native net472 task assembly loads and generates models.
    /// This is the core test for Visual Studio compatibility.
    /// </summary>
    [SkippableFact]
    public async Task FrameworkMsBuild_BuildPackage_GeneratesEntityModels()
    {
        Skip.IfNot(TestProjectBuilder.IsMSBuildExeAvailable(),
            "MSBuild.exe not found - Visual Studio must be installed to run this test");

        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_framework", "net8.0");

        // Act - Build with MSBuild.exe (Framework MSBuild)
        // BuildWithMSBuildExeAsync passes -restore to MSBuild.exe
        var buildResult = await _builder.BuildWithMSBuildExeAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Framework MSBuild build should succeed.\n{buildResult}");

        // Verify models were generated using the native net472 task assembly
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("Framework MSBuild should generate models using net472 tasks");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
        generatedFiles.Should().Contain(f => f.EndsWith("Category.g.cs"), "Should generate Category entity");
        generatedFiles.Should().Contain(f => f.EndsWith("Order.g.cs"), "Should generate Order entity");
    }

    /// <summary>
    /// Verifies that DbContext is generated when building with Framework MSBuild.
    /// </summary>
    [SkippableFact]
    public async Task FrameworkMsBuild_BuildPackage_GeneratesDbContext()
    {
        Skip.IfNot(TestProjectBuilder.IsMSBuildExeAvailable(),
            "MSBuild.exe not found - Visual Studio must be installed to run this test");

        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_framework_ctx", "net8.0");

        // Act - BuildWithMSBuildExeAsync passes -restore to MSBuild.exe
        var buildResult = await _builder.BuildWithMSBuildExeAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Framework MSBuild build should succeed.\n{buildResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().Contain(f => f.Contains("Context.g.cs"), "Should generate DbContext");
    }

    /// <summary>
    /// Verifies that the SDK package also works with Framework MSBuild using native net472 tasks.
    /// </summary>
    [SkippableFact]
    public async Task FrameworkMsBuild_Sdk_GeneratesEntityModels()
    {
        Skip.IfNot(TestProjectBuilder.IsMSBuildExeAvailable(),
            "MSBuild.exe not found - Visual Studio must be installed to run this test");

        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_sdk_framework", "net8.0");

        // Act - BuildWithMSBuildExeAsync passes -restore to MSBuild.exe
        var buildResult = await _builder.BuildWithMSBuildExeAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Framework MSBuild build should succeed.\n{buildResult}");

        // Verify models were generated using the native net472 task assembly
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("Framework MSBuild should generate models using net472 tasks");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
    }

    /// <summary>
    /// Verifies that Framework MSBuild correctly selects the net472 task folder.
    /// Uses EfcptLogVerbosity=detailed to verify task assembly selection.
    /// </summary>
    [SkippableFact]
    public async Task FrameworkMsBuild_SelectsNet472TaskFolder()
    {
        Skip.IfNot(TestProjectBuilder.IsMSBuildExeAvailable(),
            "MSBuild.exe not found - Visual Studio must be installed to run this test");

        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net472_check", "net8.0");

        // Add detailed logging to see task assembly selection
        _builder.AddProjectProperty("EfcptLogVerbosity", "detailed");

        // Act - Build with MSBuild.exe (Framework MSBuild)
        // BuildWithMSBuildExeAsync passes -restore to MSBuild.exe
        var buildResult = await _builder.BuildWithMSBuildExeAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Framework MSBuild build should succeed.\n{buildResult}");

        // Verify the net472 task folder was selected
        buildResult.Output.Should().Contain("Selected TasksFolder: net472",
            "Framework MSBuild should select the net472 task folder");
    }

    /// <summary>
    /// Verifies that incremental builds work with Framework MSBuild.
    /// Second build should be faster (no regeneration if inputs unchanged).
    /// </summary>
    [SkippableFact]
    public async Task FrameworkMsBuild_IncrementalBuild_SkipsRegenerationWhenUnchanged()
    {
        Skip.IfNot(TestProjectBuilder.IsMSBuildExeAvailable(),
            "MSBuild.exe not found - Visual Studio must be installed to run this test");

        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_incremental", "net8.0");

        // Act - First build (BuildWithMSBuildExeAsync passes -restore to MSBuild.exe)
        var firstBuild = await _builder.BuildWithMSBuildExeAsync();
        firstBuild.Success.Should().BeTrue($"First build should succeed.\n{firstBuild}");

        // Get generated file timestamps
        var generatedFiles = _builder.GetGeneratedFiles();
        var firstBuildTimestamps = generatedFiles.ToDictionary(f => f, File.GetLastWriteTimeUtc);

        // Small delay to ensure timestamps would differ if files were regenerated
        await Task.Delay(100);

        // Act - Second build (should be incremental)
        var secondBuild = await _builder.BuildWithMSBuildExeAsync();
        secondBuild.Success.Should().BeTrue($"Second build should succeed.\n{secondBuild}");

        // Assert - Files should not have been regenerated (timestamps unchanged)
        var secondBuildTimestamps = generatedFiles.ToDictionary(f => f, File.GetLastWriteTimeUtc);

        foreach (var file in generatedFiles)
        {
            secondBuildTimestamps[file].Should().Be(firstBuildTimestamps[file],
                $"File {Path.GetFileName(file)} should not have been regenerated on incremental build");
        }
    }
}

/// <summary>
/// Collection definition for Framework MSBuild tests.
/// Uses the same fixture as other package tests to share package setup.
/// </summary>
[CollectionDefinition("Framework MSBuild Tests")]
public class FrameworkMsBuildTestsCollection : ICollectionFixture<SdkPackageTestFixture>
{
}
