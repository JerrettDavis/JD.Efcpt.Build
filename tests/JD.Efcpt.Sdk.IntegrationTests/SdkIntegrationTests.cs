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
    public async Task Sdk_Net80_GeneratesEntityConfigurationsInDbContext()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject("TestEfProject_net80", "net8.0");
        await _builder.RestoreAsync();

        // Act
        await _builder.BuildAsync();

        // Assert
        // By default (without use-t4-split), configurations are embedded in the DbContext
        var generatedFiles = _builder.GetGeneratedFiles();
        var contextFile = generatedFiles.FirstOrDefault(f => f.Contains("Context.g.cs"));
        contextFile.Should().NotBeNull("Should generate DbContext file");

        var contextContent = File.ReadAllText(contextFile!);
        contextContent.Should().Contain("OnModelCreating", "DbContext should have OnModelCreating method");
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

    /// <summary>
    /// CRITICAL REGRESSION TEST: Verifies that models are actually generated when using PackageReference.
    /// This test prevents the issue where build tasks don't execute and no models are generated.
    /// </summary>
    [Fact]
    public async Task BuildPackage_Net80_GeneratesEntityModels()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net80_models", "net8.0");
        await _builder.RestoreAsync();

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("PackageReference should trigger model generation");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
        generatedFiles.Should().Contain(f => f.EndsWith("Category.g.cs"), "Should generate Category entity");
        generatedFiles.Should().Contain(f => f.EndsWith("Order.g.cs"), "Should generate Order entity");
    }

    /// <summary>
    /// CRITICAL REGRESSION TEST: Verifies that DbContext is generated when using PackageReference.
    /// </summary>
    [Fact]
    public async Task BuildPackage_Net80_GeneratesDbContext()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net80_ctx", "net8.0");
        await _builder.RestoreAsync();

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().Contain(f => f.Contains("Context.g.cs"), "Should generate DbContext");
    }

    /// <summary>
    /// CRITICAL REGRESSION TEST: Verifies that EfcptEnabled defaults to true for PackageReference consumers.
    /// We use build/ (not buildTransitive/) so targets only apply to direct consumers.
    /// </summary>
    [Fact]
    public async Task BuildPackage_DefaultEnablesEfcpt()
    {
        // Arrange - Create project WITHOUT explicitly setting EfcptEnabled
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_autoenable", "net8.0");
        await _builder.RestoreAsync();

        // Act
        var buildResult = await _builder.BuildAsync("-p:EfcptLogVerbosity=detailed");

        // Assert - Build should succeed and generate files (proving EfcptEnabled=true by default)
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty(
            "PackageReference should have EfcptEnabled=true by default");
    }

    /// <summary>
    /// CRITICAL REGRESSION TEST: Verifies models are generated across all target frameworks.
    /// </summary>
    [Fact]
    public async Task BuildPackage_Net90_GeneratesEntityModels()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net90_models", "net9.0");
        await _builder.RestoreAsync();

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("PackageReference should trigger model generation");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
    }

    /// <summary>
    /// CRITICAL REGRESSION TEST: Verifies models are generated across all target frameworks.
    /// </summary>
    [Fact]
    public async Task BuildPackage_Net100_GeneratesEntityModels()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateBuildPackageProject("TestEfProject_net100_models", "net10.0");
        await _builder.RestoreAsync();

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        var generatedFiles = _builder.GetGeneratedFiles();
        generatedFiles.Should().NotBeEmpty("PackageReference should trigger model generation");
        generatedFiles.Should().Contain(f => f.EndsWith("Product.g.cs"), "Should generate Product entity");
    }
}

#endregion
