using FluentAssertions;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

#region Net8.0 SDK Tests

[Collection("SDK Net8.0 Tests")]
public class SdkNet80Tests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public SdkNet80Tests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    [Fact]
    public async Task Sdk_Net80_BuildsSuccessfully()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net80", "net8.0");
        var restoreResult = await _builder.RestoreAsync();
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
    }

    [Fact]
    public async Task Sdk_Net80_GeneratesEntityModels()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net80", "net8.0");
        await _builder.RestoreAsync();

        // Act
        await _builder.BuildAsync();

        // Assert
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("Should generate at least one file");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
        generatedFiles.Should().Contain(f => f.EndsWith("Category.g.cs"), "Should generate Category entity");
        generatedFiles.Should().Contain(f => f.EndsWith("Order.g.cs"), "Should generate Order entity");
    }

    [Fact]
    public async Task Sdk_Net80_GeneratesDbContext()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net80", "net8.0");
        await _builder.RestoreAsync();

        // Act
        await _builder.BuildAsync();

        // Assert
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().Contain(f => f.Contains("Context.g.cs"), "Should generate DbContext");
    }

    [Fact]
    public async Task Sdk_Net80_GeneratesConfigurations()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net80", "net8.0");
        await _builder.RestoreAsync();

        // Act
        await _builder.BuildAsync();

        // Assert
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().Contain(f => f.EndsWith("ProductConfiguration.g.cs"), "Should generate ProductConfiguration");
        generatedFiles.Should().Contain(f => f.EndsWith("CategoryConfiguration.g.cs"), "Should generate CategoryConfiguration");
        generatedFiles.Should().Contain(f => f.EndsWith("OrderConfiguration.g.cs"), "Should generate OrderConfiguration");
    }

    [Fact]
    public async Task Sdk_Net80_CleanRemovesGeneratedFiles()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_clean_net80", "net8.0");
        await _builder.RestoreAsync();
        await _builder.BuildAsync();

        // Act
        var cleanResult = await _builder.CleanAsync();

        // Assert
        cleanResult.Success.Should().BeTrue($"Clean should succeed.\n{cleanResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().BeEmpty("Generated files should be removed after clean");
    }
}

#endregion

#region Net9.0 SDK Tests

[Collection("SDK Net9.0 Tests")]
public class SdkNet90Tests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public SdkNet90Tests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    [Fact]
    public async Task Sdk_Net90_BuildsSuccessfully()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net90", "net9.0");
        var restoreResult = await _builder.RestoreAsync();
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
    }

    [Fact]
    public async Task Sdk_Net90_GeneratesEntityModels()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net90", "net9.0");
        await _builder.RestoreAsync();

        // Act
        await _builder.BuildAsync();

        // Assert
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("Should generate at least one file");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
    }
}

#endregion

#region Net10.0 SDK Tests

[Collection("SDK Net10.0 Tests")]
public class SdkNet100Tests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public SdkNet100Tests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    [Fact]
    public async Task Sdk_Net100_BuildsSuccessfully()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net100", "net10.0");
        var restoreResult = await _builder.RestoreAsync();
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
    }

    [Fact]
    public async Task Sdk_Net100_GeneratesEntityModels()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net100", "net10.0");
        await _builder.RestoreAsync();

        // Act
        await _builder.BuildAsync();

        // Assert
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("Should generate at least one file");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
    }
}

#endregion

#region PackageReference (JD.Efcpt.Build) Tests

[Collection("Build Package Tests")]
public class BuildPackageTests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public BuildPackageTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    [Fact]
    public async Task BuildPackage_Net80_BuildsSuccessfully()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net80_pkg", "net8.0");
        var restoreResult = await _builder.RestoreAsync();
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
    }

    [Fact]
    public async Task BuildPackage_Net90_BuildsSuccessfully()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net90_pkg", "net9.0");
        var restoreResult = await _builder.RestoreAsync();
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
    }

    [Fact]
    public async Task BuildPackage_Net100_BuildsSuccessfully()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net100_pkg", "net10.0");
        var restoreResult = await _builder.RestoreAsync();
        restoreResult.Success.Should().BeTrue($"Restore should succeed.\n{restoreResult}");

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
    }
}

#endregion
