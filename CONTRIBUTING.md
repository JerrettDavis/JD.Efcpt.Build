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

JD.Efcpt.Build uses **TinyBDD** for behavior-driven testing. All tests follow a consistent Given-When-Then pattern.

#### Testing Framework

We use **TinyBDD** for all tests (not traditional xUnit Arrange-Act-Assert). This provides:
- ✅ Clear behavior specifications
- ✅ Readable test scenarios
- ✅ Consistent patterns across the codebase
- ✅ Self-documenting tests

#### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test -v detailed

# Run specific test category
dotnet test --filter "FullyQualifiedName~SchemaReader"

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

#### Writing Tests with TinyBDD

**Test Structure:**

```csharp
using TinyBDD.Xunit;
using Xunit;

[Feature("Component: brief description of functionality")]
[Collection(nameof(AssemblySetup))]
public sealed class ComponentTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Define state records
    private sealed record SetupState(
        string InputValue,
        ITestOutputHelper Output);

    private sealed record ExecutionResult(
        bool Success,
        string Output,
        Exception? Error = null);

    [Scenario("Description of specific behavior")]
    [Fact]
    public async Task Scenario_Name()
    {
        await Given("context setup", () => new SetupState("test-value", Output))
            .When("action is performed", state => PerformAction(state))
            .Then("expected outcome occurs", result => result.Success)
            .And("additional assertion", result => result.Output == "expected")
            .Finally(result => CleanupResources(result))
            .AssertPassed();
    }

    private static ExecutionResult PerformAction(SetupState state)
    {
        try
        {
            // Execute the action being tested
            var output = DoSomething(state.InputValue);
            return new ExecutionResult(true, output);
        }
        catch (Exception ex)
        {
            return new ExecutionResult(false, "", ex);
        }
    }

    private static void CleanupResources(ExecutionResult result)
    {
        // Clean up any resources
    }
}
```

#### Testing Best Practices

**DO:**
- ✅ Use TinyBDD for all new tests
- ✅ Write descriptive scenario names (e.g., "Should detect changed fingerprint when DACPAC modified")
- ✅ Use state records for Given context
- ✅ Use result records for When outcomes
- ✅ Test both success and failure paths
- ✅ Clean up resources in `Finally` blocks
- ✅ Use meaningful assertion messages

**DON'T:**
- ❌ Use traditional Arrange-Act-Assert (use Given-When-Then)
- ❌ Skip the `Finally` block if cleanup is needed
- ❌ Write tests without clear scenarios
- ❌ Test implementation details (test behavior)
- ❌ Create inter-dependent tests

#### Testing Patterns

**Pattern 1: Simple Value Transformation**

```csharp
[Scenario("Should compute fingerprint from byte array")]
[Fact]
public async Task Computes_fingerprint_from_bytes()
{
    await Given("byte array with known content", () => new byte[] { 1, 2, 3, 4 })
        .When("computing fingerprint", bytes => ComputeFingerprint(bytes))
        .Then("fingerprint is deterministic", fp => !string.IsNullOrEmpty(fp))
        .And("fingerprint has expected format", fp => fp.Length == 16)
        .AssertPassed();
}
```

**Pattern 2: File System Operations**

```csharp
[Scenario("Should create output directory when it doesn't exist")]
[Fact]
public async Task Creates_missing_output_directory()
{
    await Given("non-existent directory path", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            return new SetupState(path, Output);
        })
        .When("ensuring directory exists", state =>
        {
            Directory.CreateDirectory(state.Path);
            return new Result(Directory.Exists(state.Path), state.Path);
        })
        .Then("directory is created", result => result.Exists)
        .Finally(result =>
        {
            if (Directory.Exists(result.Path))
                Directory.Delete(result.Path, true);
        })
        .AssertPassed();
}
```

**Pattern 3: Exception Testing**

```csharp
[Scenario("Should throw when connection string is invalid")]
[Fact]
public async Task Throws_on_invalid_connection_string()
{
    await Given("invalid connection string", () => "not-a-valid-connection-string")
        .When("reading schema", connectionString =>
        {
            try
            {
                reader.ReadSchema(connectionString);
                return (false, null as Exception);
            }
            catch (Exception ex)
            {
                return (true, ex);
            }
        })
        .Then("exception is thrown", result => result.Item1)
        .And("exception message is descriptive", result =>
            result.Item2!.Message.Contains("connection") ||
            result.Item2!.Message.Contains("invalid"))
        .AssertPassed();
}
```

**Pattern 4: Integration Tests with Testcontainers**

```csharp
[Feature("PostgreSqlSchemaReader: integration with real database")]
[Collection(nameof(PostgreSqlContainer))]
public sealed class PostgreSqlSchemaIntegrationTests(
    PostgreSqlFixture fixture,
    ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Should read schema from PostgreSQL database")]
    [Fact]
    public async Task Reads_schema_from_postgres()
    {
        await Given("PostgreSQL database with test schema", () => fixture.ConnectionString)
            .When("reading schema", cs => new PostgreSqlSchemaReader().ReadSchema(cs))
            .Then("schema contains expected tables", schema => schema.Tables.Count > 0)
            .And("tables have columns", schema => schema.Tables.All(t => t.Columns.Any()))
            .AssertPassed();
    }
}
```

#### Test Coverage Goals

| Component | Target | Current |
|-----------|--------|---------|
| **MSBuild Tasks** | 95%+ | ~90% |
| **Schema Readers** | 90%+ | ~85% |
| **Resolution Chains** | 90%+ | ~88% |
| **Utilities** | 85%+ | ~82% |

#### Integration Testing

**Database Provider Tests:**
- Use Testcontainers for SQL Server, PostgreSQL, MySQL
- Use in-memory SQLite for fast tests
- Mock unavailable providers (Snowflake requires LocalStack Pro)

**Sample Projects:**
- Create minimal test projects in `tests/TestAssets/`
- Test actual MSBuild integration
- Verify generated code compiles

#### Running Integration Tests

```bash
# Requires Docker for Testcontainers
docker info

# Run integration tests
dotnet test --filter "Category=Integration"

# Run specific provider tests
dotnet test --filter "FullyQualifiedName~PostgreSql"
```

#### Debugging Tests

```csharp
// TinyBDD provides detailed output on failure
await Given("setup", CreateSetup)
    .When("action", Execute)
    .Then("assertion", result => result.IsValid)
    .AssertPassed();

// On failure, you'll see:
// ❌ Scenario failed at step: Then "assertion"
// Expected: True
// Actual: False
// State: { ... }
```

For more details, see [TinyBDD documentation](https://github.com/ledjon-behluli/TinyBDD).

### Documentation

When contributing, please update:

- **README.md** - For user-facing features
- **docs/** - For detailed documentation in docs/user-guide/
- **XML comments** - For all public APIs
- **Code comments** - For complex logic

#### Version Placeholders in Documentation

Documentation and README files use `PACKAGE_VERSION` as a placeholder for version numbers. This placeholder is automatically replaced with the actual version during the CI/CD build process.

**When to use placeholders:**

Use `PACKAGE_VERSION` in documentation for:
- SDK version references: `Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION"`
- PackageReference version attributes: `Version="PACKAGE_VERSION"`
- Any mention of the current package version in examples

**Example:**

```xml
<!-- In documentation, write: -->
<Project Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="PACKAGE_VERSION" />
  </ItemGroup>
</Project>

<!-- During CI build, this becomes (e.g., for version 1.2.3): -->
<Project Sdk="JD.Efcpt.Sdk/1.2.3">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="1.2.3" />
  </ItemGroup>
</Project>
```

**Testing version replacement locally:**

```bash
# Dry run (shows what would be replaced)
pwsh ./build/replace-version.ps1 -Version "1.2.3" -DryRun

# Actually replace versions
pwsh ./build/replace-version.ps1 -Version "1.2.3"

# Revert changes after testing
git checkout README.md docs/ samples/
```

**Important:** Always commit documentation with `PACKAGE_VERSION` placeholders, not actual version numbers. The CI/CD workflow automatically replaces these during the build and package process.

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
- **Documentation** - Check README.md and docs/user-guide/ first

## Recognition

Contributors will be recognized in:
- GitHub contributors page
- Release notes (for significant contributions)
- Special thanks in README (for major features)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to JD.Efcpt.Build!** 🎉

