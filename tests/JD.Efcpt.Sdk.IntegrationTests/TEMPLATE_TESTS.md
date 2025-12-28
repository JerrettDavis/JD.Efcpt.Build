# Template Integration Tests

This document describes the integration tests for the JD.Efcpt.Build.Templates package.

## Overview

The `TemplateTests` class provides comprehensive integration tests for the `dotnet new efcptbuild` template functionality. These tests validate that:

1. The template installs successfully
2. Projects created from the template have the correct structure
3. Generated projects use the SDK approach (`<Project Sdk="JD.Efcpt.Sdk">`)
4. Project name substitution works correctly in all template files
5. Generated projects build successfully

## Test Infrastructure

### TemplateTestFixture

The `TemplateTestFixture` class handles:
- Packing the JD.Efcpt.Build.Templates package
- Packing the JD.Efcpt.Sdk and JD.Efcpt.Build packages (required for building generated projects)
- Providing helper methods for template installation, creation, and uninstallation
- Managing package cleanup

### Test Approach

Tests use a local NuGet package store approach:
1. Packages are built and placed in a temporary directory
2. Each test creates an isolated test directory
3. Template is installed using `dotnet new install`
4. Projects are created using `dotnet new efcptbuild`
5. Projects reference the local package store via nuget.config

## Test Cases

### Template_InstallsSuccessfully
Verifies that the template package installs without errors and registers the `efcptbuild` short name.

### Template_CreatesProjectWithCorrectStructure
Validates that all expected files are created:
- `{ProjectName}.csproj`
- `efcpt-config.json`
- `README.md`

### Template_CreatesProjectUsingSdkApproach
Ensures the generated project uses `<Project Sdk="JD.Efcpt.Sdk">` and doesn't include a PackageReference to JD.Efcpt.Build.

### Template_ConfigFileContainsCorrectProjectName
Verifies that the project name is correctly substituted in efcpt-config.json namespaces.

### Template_CreatedProjectBuildsSuccessfully
End-to-end test that:
1. Creates a project from the template
2. Adds a reference to a test database project
3. Configures local package sources
4. Restores and builds the project
5. Verifies that EF Core models are generated

### Template_ReadmeContainsSdkInformation
Validates that the README mentions JD.Efcpt.Sdk and explains the SDK approach.

### Template_UninstallsSuccessfully
Ensures the template can be cleanly uninstalled.

## Running the Tests

### Run all template tests:
```bash
dotnet test --filter "FullyQualifiedName~TemplateTests"
```

### Run a specific test:
```bash
dotnet test --filter "FullyQualifiedName~Template_InstallsSuccessfully"
```

### Run with verbose output:
```bash
dotnet test --filter "FullyQualifiedName~TemplateTests" -v detailed
```

## Test Performance

Template tests are grouped in a dedicated collection to run sequentially. This is necessary because:
- Template installation/uninstallation affects global dotnet new state
- Multiple parallel installations could interfere with each other
- Package building is done once and shared across all tests

Typical execution time: 30-60 seconds for the full suite (depending on build times).

## Troubleshooting

### Tests fail with "Package not found"
Ensure the Template, SDK, and Build projects build successfully before running tests.

### Tests timeout
Increase the timeout in the fixture's `PackTemplatePackageAsync` method if needed for slower environments.

### Template already installed
Tests handle cleanup automatically, but if tests are interrupted, you may need to manually uninstall:
```bash
dotnet new uninstall JD.Efcpt.Build.Templates
```

## Adding New Tests

When adding new template tests:

1. Add the test method to `TemplateTests.cs`
2. Use the `_fixture` to install/create from the template
3. Use FluentAssertions for readable assertions
4. Ensure proper cleanup in test Dispose if needed
5. Follow the naming convention: `Template_{TestName}`

Example:
```csharp
[Fact]
public async Task Template_NewFeature_WorksAsExpected()
{
    // Arrange
    await _fixture.InstallTemplateAsync(_testDirectory);
    var projectName = "TestProject";
    
    // Act
    var result = await _fixture.CreateProjectFromTemplateAsync(_testDirectory, projectName);
    
    // Assert
    result.Success.Should().BeTrue();
    // Additional assertions...
}
```
