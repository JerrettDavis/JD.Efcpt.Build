using FluentAssertions;
using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Detailed tests for code generation output.
/// </summary>
[Collection("Code Generation Tests")]
public class CodeGenerationTests : IDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;

    public CodeGenerationTests(SdkPackageTestFixture fixture)
    {
        _fixture = fixture;
        _builder = new TestProjectBuilder(fixture);
    }

    public void Dispose() => _builder.Dispose();

    [Fact]
    public async Task GeneratedEntities_HaveCorrectNamespace()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        var productContent = FindAndReadGeneratedFile("Product.g.cs");
        productContent.Should().Contain("namespace", "Should have namespace declaration");
        // In zero-config mode, namespace matches the project name
        productContent.Should().Contain("namespace TestProject_net80", "Should have project-based namespace");
    }

    [Fact]
    public async Task GeneratedEntities_HaveNullableReferenceTypes()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        var productContent = FindAndReadGeneratedFile("Product.g.cs");
        // Nullable reference types are enabled - check for the null-forgiving operator pattern
        // or explicit nullable directive (depending on template version)
        var hasNullableSupport = productContent.Contains("= null!;") || productContent.Contains("#nullable enable");
        hasNullableSupport.Should().BeTrue("Should have nullable reference type support (either = null!; pattern or #nullable enable directive)");
        productContent.Should().Contain("string?", "Should have nullable string properties");
    }

    [Fact]
    public async Task GeneratedDbContext_InheritsFromDbContext()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        var contextContent = FindAndReadGeneratedFile("Context.g.cs");
        contextContent.Should().Contain(": DbContext", "DbContext should inherit from DbContext");
    }

    [Fact]
    public async Task GeneratedDbContext_HasDbSets()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        var contextContent = FindAndReadGeneratedFile("Context.g.cs");
        contextContent.Should().Contain("DbSet<Product>", "Should have DbSet for Product");
        contextContent.Should().Contain("DbSet<Category>", "Should have DbSet for Category");
        contextContent.Should().Contain("DbSet<Order>", "Should have DbSet for Order");
    }

    [Fact]
    public async Task GeneratedDbContext_HasEntityConfigurations()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        // By default (without use-t4-split), configurations are embedded in the DbContext's OnModelCreating
        var contextContent = FindAndReadGeneratedFile("Context.g.cs");
        contextContent.Should().Contain("OnModelCreating", "DbContext should have OnModelCreating method");
        contextContent.Should().Contain("modelBuilder.Entity<Product>", "Should configure Product entity");
    }

    [Fact]
    public async Task GeneratedProduct_HasExpectedProperties()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        var productContent = FindAndReadGeneratedFile("Product.g.cs");
        productContent.Should().Contain("ProductId", "Should have ProductId property");
        productContent.Should().Contain("Name", "Should have Name property");
        productContent.Should().Contain("Description", "Should have Description property");
        productContent.Should().Contain("Price", "Should have Price property");
        productContent.Should().Contain("CategoryId", "Should have CategoryId property");
        productContent.Should().Contain("IsActive", "Should have IsActive property");
    }

    [Fact]
    public async Task GeneratedCategory_HasSelfReference()
    {
        // Arrange & Act
        await BuildSdkProject("net8.0");

        // Assert
        var categoryContent = FindAndReadGeneratedFile("Category.g.cs");
        categoryContent.Should().Contain("ParentCategoryId", "Should have ParentCategoryId for self-reference");
    }

    [Fact]
    public async Task IncrementalBuild_SkipsGenerationWhenUnchanged()
    {
        // Arrange
        await BuildSdkProject("net8.0");

        // Act - Build again
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Rebuild should succeed.\n{buildResult}");
        buildResult.Output.Should().Contain("fingerprint unchanged",
            "Should skip generation when fingerprint is unchanged");
    }

    [Fact]
    public async Task CustomRootNamespace_IsApplied()
    {
        // Arrange
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        var additionalContent = @"
    <PropertyGroup>
        <EfcptConfigRootNamespace>MyCustomNamespace</EfcptConfigRootNamespace>
    </PropertyGroup>";
        _builder.CreateSdkProject("TestProject_CustomNs", "net8.0", additionalContent);
        await _builder.RestoreAsync();

        // Act
        var buildResult = await _builder.BuildAsync();

        // Assert
        buildResult.Success.Should().BeTrue($"Build should succeed.\n{buildResult}");
        var productContent = FindAndReadGeneratedFile("Product.g.cs");
        productContent.Should().Contain("namespace MyCustomNamespace",
            "Should use custom namespace");
    }

    private async Task BuildSdkProject(string targetFramework)
    {
        _builder.CopyDatabaseProject(_fixture.GetTestFixturesPath());
        _builder.CreateSdkProject($"TestProject_{targetFramework.Replace(".", "")}", targetFramework);
        await _builder.RestoreAsync();
        var buildResult = await _builder.BuildAsync();
        buildResult.Success.Should().BeTrue($"Build should succeed for assertions.\n{buildResult}");
    }

    private string FindAndReadGeneratedFile(string fileNameContains)
    {
        var files = _builder.GetGeneratedFiles();
        var file = files.FirstOrDefault(f => f.Contains(fileNameContains));
        file.Should().NotBeNull($"Should find generated file containing '{fileNameContains}'");
        return File.ReadAllText(file!);
    }
}
