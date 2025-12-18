# Contributing to JD.Efcpt.Build

Thank you for your interest in contributing to JD.Efcpt.Build! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Environment details:**
  - OS (Windows/Linux/macOS)
  - .NET SDK version (`dotnet --info`)
  - JD.Efcpt.Build version
  - EF Core Power Tools CLI version
- **Relevant logs** with `EfcptLogVerbosity` set to `detailed`
- **Sample project** if possible (minimal reproduction)

### Suggesting Features

Feature suggestions are welcome! Please:

- **Check existing feature requests** first
- **Describe the use case** clearly
- **Explain why** this feature would be useful
- **Provide examples** of how it would work

### Pull Requests

1. **Fork the repository** and create a branch from `main`
2. **Follow existing code style** and patterns
3. **Add tests** for new functionality
4. **Update documentation** as needed
5. **Ensure all tests pass** before submitting
6. **Write clear commit messages**

## Development Setup

### Prerequisites

- .NET SDK 8.0 or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- EF Core Power Tools CLI (`dotnet tool install -g ErikEJ.EFCorePowerTools.Cli`)
- SQL Server or SQL Server Express (for testing)

### Building the Project

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/JD.Efcpt.Build.git
cd JD.Efcpt.Build

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test
```

### Project Structure

```
JD.Efcpt.Build/
├── src/
│   ├── JD.Efcpt.Build/              # NuGet package project
│   │   ├── build/                   # MSBuild .props and .targets
│   │   ├── buildTransitive/         # Transitive MSBuild files
│   │   └── defaults/                # Default configuration files
│   └── JD.Efcpt.Build.Tasks/        # MSBuild tasks implementation
│       ├── ResolveSqlProjAndInputs.cs
│       ├── EnsureDacpacBuilt.cs
│       ├── StageEfcptInputs.cs
│       ├── ComputeFingerprint.cs
│       ├── RunEfcpt.cs
│       └── RenameGeneratedFiles.cs
├── tests/
│   ├── JD.Efcpt.Build.Tasks.Tests/  # Unit tests
│   └── TestAssets/                   # Test projects
├── samples/
│   └── simple-generation/            # Sample usage
└── docs/                             # Documentation
```

### Code Style

- Follow existing C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-purpose
- Prefer readability over cleverness

### MSBuild Targets and Tasks

When adding or modifying MSBuild targets:

1. **Keep targets small and focused**
2. **Use descriptive target names** prefixed with `Efcpt`
3. **Document target dependencies** clearly
4. **Add logging** at appropriate verbosity levels
5. **Test incremental build scenarios**

When adding or modifying tasks:

1. **Inherit from `Microsoft.Build.Utilities.Task`**
2. **Mark parameters** with `[Required]` or `[Output]` attributes
3. **Add XML documentation** for all public properties
4. **Implement proper error handling**
5. **Log diagnostic information**
6. **Write unit tests**

### Testing

#### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test -v detailed

# Run specific test
dotnet test --filter "FullyQualifiedName~TestName"
```

#### Writing Tests

- Add tests for new features
- Test both success and error scenarios
- Use descriptive test names: `Should_ExpectedBehavior_When_Condition`
- Keep tests isolated and independent
- Mock external dependencies

Example test structure:

```csharp
[Fact]
public void Should_StageTemplates_When_TemplateDirectoryExists()
{
    // Arrange
    var task = new StageEfcptInputs
    {
        OutputDir = testDir,
        TemplateDir = sourceTemplateDir,
        // ... other properties
    };

    // Act
    var result = task.Execute();

    // Assert
    Assert.True(result);
    Assert.True(Directory.Exists(expectedStagedPath));
}
```

### Documentation

When contributing, please update:

- **README.md** - For user-facing features
- **QUICKSTART.md** - For common usage scenarios
- **XML comments** - For all public APIs
- **Code comments** - For complex logic

### Commit Messages

Follow conventional commits format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**

```
feat(staging): add TemplateOutputDir parameter

Allows templates to be staged to custom subdirectories within
the output directory. Supports both relative and absolute paths.

Closes #123
```

```
fix(fingerprint): detect deleted obj/efcpt folder

Added stamp file existence check to trigger regeneration when
intermediate directory is deleted.

Fixes #456
```

### Pull Request Process

1. **Create a descriptive PR title** following commit message format
2. **Fill out the PR template** (if provided)
3. **Link related issues** using keywords (Fixes #123, Closes #456)
4. **Ensure CI passes** (all tests, builds)
5. **Respond to review feedback** promptly
6. **Squash commits** if requested
7. **Update documentation** if feature changes user-facing behavior

### Release Process

Maintainers handle releases using this process:

1. Update version in `JD.Efcpt.Build.csproj`
2. Create git tag: `git tag -a v0.2.4 -m "Release v0.2.4"`
3. Push tag: `git push origin v0.2.4`
4. Build NuGet package: `dotnet pack -c Release`
5. Publish to NuGet.org

## Getting Help

- **GitHub Issues** - For bugs and feature requests
- **GitHub Discussions** - For questions and community support
- **Documentation** - Check README.md and QUICKSTART.md first

## Recognition

Contributors will be recognized in:
- GitHub contributors page
- Release notes (for significant contributions)
- Special thanks in README (for major features)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to JD.Efcpt.Build!** 🎉

