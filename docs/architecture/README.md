# Architecture Documentation

Welcome to the JD.Efcpt.Build architecture documentation. This section provides deep technical insights into how the build system works.

## Documents

### [Build Pipeline Architecture](PIPELINE.md)
**Essential reading for understanding the system**

Comprehensive guide to the MSBuild-integrated code generation pipeline:
- Phase-by-phase breakdown of the build process
- Input resolution strategies (DACPAC, configuration, connection strings)
- Incremental build behavior and optimizations
- MSBuild integration and target ordering
- Error handling and diagnostics

**Key Topics:**
- How the pipeline executes during build
- When code is regenerated vs. skipped
- Tool resolution strategies (dnx, local, global)
- Configuration override system

---

### [Change Detection & Fingerprinting](FINGERPRINTING.md)
**Critical for understanding incremental builds**

Detailed explanation of the fingerprinting system that enables fast incremental builds:
- What components make up a fingerprint
- How XXH64 hashing works and why it's used
- Storage and comparison logic
- Troubleshooting fingerprint issues

**Key Topics:**
- Why fingerprinting makes builds 37x-300x faster
- How schema changes are detected
- Debugging regeneration issues
- Best practices for deterministic builds

---

## Component Architecture

### High-Level System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     MSBuild Host Process                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           JD.Efcpt.Build.Tasks Library              │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │                                                     │   │
│  │  MSBuild Tasks (11):                               │   │
│  │  ├─ CheckSdkVersion                                │   │
│  │  ├─ ResolveSqlProjAndInputs ◄────┐                 │   │
│  │  ├─ EnsureDacpacBuilt            │                 │   │
│  │  ├─ StageEfcptInputs              │                 │   │
│  │  ├─ ComputeFingerprint ◄──────────┼──┐              │   │
│  │  ├─ RunEfcpt                      │  │              │   │
│  │  ├─ RenameGeneratedFiles          │  │              │   │
│  │  ├─ SplitOutputs                  │  │              │   │
│  │  ├─ ApplyConfigOverrides          │  │              │   │
│  │  ├─ SerializeConfigProperties     │  │              │   │
│  │  └─ CleanGeneratedFiles           │  │              │   │
│  │                                    │  │              │   │
│  │  Resolution Chains (4):           │  │              │   │
│  │  ├─ DacpacResolutionChain ────────┘  │              │   │
│  │  ├─ ConfigFileResolutionChain        │              │   │
│  │  ├─ ConnectionStringResolutionChain  │              │   │
│  │  └─ TemplateDirectoryResolutionChain │              │   │
│  │                                       │              │   │
│  │  Schema Readers (7):                 │              │   │
│  │  ├─ SqlServerSchemaReader ───────────┘              │   │
│  │  ├─ PostgreSqlSchemaReader                          │   │
│  │  ├─ MySqlSchemaReader                               │   │
│  │  ├─ SqliteSchemaReader                              │   │
│  │  ├─ OracleSchemaReader                              │   │
│  │  ├─ FirebirdSchemaReader                            │   │
│  │  └─ SnowflakeSchemaReader                           │   │
│  │                                                     │   │
│  │  Utilities:                                         │   │
│  │  ├─ SchemaFingerprinter                            │   │
│  │  ├─ DacpacFingerprinter                            │   │
│  │  ├─ BuildLogger (IBuildLog)                        │   │
│  │  └─ Extensions (DataRow, String, etc.)             │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         External Process Execution                  │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │  dotnet dnx ErikEJ.EFCorePowerTools.Cli ───┐        │   │
│  │   OR                                       │        │   │
│  │  dotnet tool run efcpt ────────────────────┼─► efcpt│   │
│  │   OR                                       │        │   │
│  │  efcpt (global) ───────────────────────────┘        │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Module Responsibilities

| Module | Responsibility | Key Classes |
|--------|---------------|-------------|
| **MSBuild Tasks** | Build integration, orchestration | `RunEfcpt`, `ComputeFingerprint`, `ResolveSqlProjAndInputs` |
| **Resolution Chains** | Multi-tier input resolution | `DacpacResolutionChain`, `ConfigFileResolutionChain` |
| **Schema Readers** | Database metadata extraction | `SqlServerSchemaReader`, `PostgreSqlSchemaReader`, etc. |
| **Fingerprinting** | Change detection | `SchemaFingerprinter`, `DacpacFingerprinter` |
| **Configuration** | Override system | `EfcptConfigOverrideApplicator` |
| **Utilities** | Shared functionality | Extensions, logging, file utilities |

### Data Flow

```
[User's .csproj]
    ↓
[MSBuild Property Evaluation]
    ↓
[Input Resolution]
    ├─→ [DACPAC file path]
    ├─→ [Configuration file path]
    └─→ [Connection string]
    ↓
[Fingerprint Computation]
    ├─→ [DACPAC content hash]
    ├─→ [Config content hash]
    ├─→ [Template content hash]
    └─→ [Combined XXH64 hash]
    ↓
[Comparison with Previous Fingerprint]
    ├─→ [Match] → Skip generation
    └─→ [Different] → Continue
    ↓
[External Tool Execution]
    ↓
[Generated Files (.cs)]
    ↓
[File Renaming (.g.cs)]
    ↓
[MSBuild Compile Items]
    ↓
[C# Compiler]
```

## Design Principles

### 1. **Determinism**
All operations produce the same output given the same input:
- Fingerprints are stable across builds
- Generated code is consistent
- Build order doesn't matter

### 2. **Incrementality**
Only regenerate code when necessary:
- Fast fingerprint-based checks
- Leverages MSBuild's incremental logic
- Respects existing MSBuild caches

### 3. **Composability**
Tasks can be used independently:
- Each task has clear inputs/outputs
- Can be tested in isolation
- Supports custom build workflows

### 4. **Extensibility**
Support for customization:
- PatternKit chains for resolution logic
- Override system for configuration
- Custom template support
- Multiple database providers

### 5. **Observability**
Clear logging and diagnostics:
- Structured log messages with `[Efcpt]` prefix
- Verbose logging support (`/v:detailed`)
- Clear error messages with remediation hints

## Code Organization

### Project Structure

```
src/JD.Efcpt.Build.Tasks/
├── Schema/
│   ├── ISchemaReader.cs
│   ├── SchemaReaderBase.cs
│   ├── SchemaModel.cs
│   ├── SchemaFingerprinter.cs
│   └── Providers/
│       ├── SqlServerSchemaReader.cs
│       ├── PostgreSqlSchemaReader.cs
│       ├── MySqlSchemaReader.cs
│       ├── SqliteSchemaReader.cs
│       ├── OracleSchemaReader.cs
│       ├── FirebirdSchemaReader.cs
│       └── SnowflakeSchemaReader.cs
│
├── ConnectionStrings/
│   ├── IConnectionStringParser.cs
│   ├── AppSettingsConnectionStringParser.cs
│   ├── AppConfigConnectionStringParser.cs
│   └── ConfigurationFileTypeValidator.cs
│
├── Resolution/
│   ├── DacpacResolutionChain.cs
│   ├── ConfigFileResolutionChain.cs
│   ├── ConnectionStringResolutionChain.cs
│   └── TemplateDirectoryResolutionChain.cs
│
├── Configuration/
│   ├── EfcptConfigOverrideApplicator.cs
│   └── EfcptConfigModel.cs
│
├── Extensions/
│   ├── DataRowExtensions.cs
│   ├── StringExtensions.cs
│   └── EnumerableExtensions.cs
│
├── Decorators/
│   └── BuildLogDecorator.cs
│
├── Compatibility/
│   └── HashCodePolyfill.cs  (.NET Framework)
│
├── [MSBuild Tasks]
│   ├── CheckSdkVersion.cs
│   ├── ResolveSqlProjAndInputs.cs
│   ├── EnsureDacpacBuilt.cs
│   ├── StageEfcptInputs.cs
│   ├── ComputeFingerprint.cs
│   ├── RunEfcpt.cs
│   ├── RenameGeneratedFiles.cs
│   ├── SplitOutputs.cs
│   ├── ApplyConfigOverrides.cs
│   ├── SerializeConfigProperties.cs
│   └── CleanGeneratedFiles.cs
│
└── JD.Efcpt.Build.Tasks.csproj
```

### Design Patterns Used

| Pattern | Usage | Location |
|---------|-------|----------|
| **Template Method** | Schema reader base logic | `SchemaReaderBase` |
| **Chain of Responsibility** | Input resolution | `*ResolutionChain` classes |
| **Strategy** | Database provider selection | `ISchemaReader` implementations |
| **Decorator** | Logging enhancement | `BuildLogDecorator` |
| **Builder** | MSBuild property construction | Various tasks |
| **Factory** | Schema reader creation | `DatabaseProviderFactory` |

## Technology Stack

### Core Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| Microsoft.Build.Utilities.Core | 17.x | MSBuild task base classes |
| PatternKit.Core | 0.17.3 | Chain of responsibility patterns |
| System.IO.Hashing | 10.0.1 | XXH64 fingerprint computation |
| TinyBDD.Xunit | 0.13.0 | Testing framework (test projects) |

### Database Provider Libraries

| Provider | Package |
|----------|---------|
| SQL Server | Microsoft.Data.SqlClient |
| PostgreSQL | Npgsql |
| MySQL | MySqlConnector |
| SQLite | Microsoft.Data.Sqlite |
| Oracle | Oracle.ManagedDataAccess.Core |
| Firebird | FirebirdSql.Data.FirebirdClient |
| Snowflake | Snowflake.Data |

### Target Frameworks

- **net472** - .NET Framework 4.7.2 (MSBuild 16.x compatibility)
- **net8.0** - .NET 8 LTS
- **net9.0** - .NET 9
- **net10.0** - .NET 10 (with dnx support)

## Testing Architecture

### Test Projects

```
tests/
├── JD.Efcpt.Build.Tests/
│   ├── Unit Tests (TinyBDD)
│   ├── Integration Tests (Testcontainers)
│   └── Schema Reader Tests
│
└── JD.Efcpt.Sdk.IntegrationTests/
    └── End-to-end SDK tests
```

### Testing Patterns

All tests use **TinyBDD** for behavior-driven structure:

```csharp
[Feature("Component: behavior description")]
[Collection(nameof(AssemblySetup))]
public sealed class ComponentTests(ITestOutputHelper output)
    : TinyBddXunitBase(output)
{
    [Scenario("Specific behavior scenario")]
    [Fact]
    public async Task Scenario_Name()
    {
        await Given("setup context", CreateSetup)
            .When("action is performed", ExecuteAction)
            .Then("expected outcome", result => result.IsValid)
            .And("additional assertion", result => result.Count == expected)
            .Finally(result => result.Cleanup())
            .AssertPassed();
    }
}
```

### Integration Test Strategy

- **Testcontainers** for database providers (PostgreSQL, MySQL, etc.)
- **LocalStack** for Snowflake emulation (when available)
- **In-memory SQLite** for fast tests
- **Fake SQL Projects** for DACPAC testing

## See Also

- [Build Pipeline Details](PIPELINE.md)
- [Fingerprinting Deep Dive](FINGERPRINTING.md)
- [User Guide](../user-guide/index.md)
- [Contributing Guide](../../CONTRIBUTING.md)
