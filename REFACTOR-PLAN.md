# JD.Efcpt.Build Comprehensive Audit & Refactor Plan

**Created:** December 2024
**Version:** 1.0
**Status:** Planning Phase
**Branch:** `claude/document-efcpt-build-hxY3d`

---

## Executive Summary

This document provides a comprehensive audit of the JD.Efcpt.Build library and outlines a detailed plan to:

1. **Document** all code via approachable user-facing markdown files
2. **Ensure complete code coverage** with behavior-driven tests using TinyBDD
3. **DRY up the codebase** using PatternKit and modular design
4. **Apply SOLID principles** to ensure maintainability and extensibility

---

## Table of Contents

1. [Current State Audit](#current-state-audit)
2. [Testing Audit](#testing-audit)
3. [DRY Analysis](#dry-analysis)
4. [SOLID Compliance Review](#solid-compliance-review)
5. [Documentation Plan](#documentation-plan)
6. [Testing Plan](#testing-plan)
7. [Refactoring Plan](#refactoring-plan)
8. [Implementation Phases](#implementation-phases)
9. [Success Criteria](#success-criteria)

---

## Current State Audit

### Codebase Statistics

| Metric | Value |
|--------|-------|
| **Core Tasks LOC** | ~4,930 lines |
| **Source Projects** | 4 (Build, Tasks, Sdk, Templates) |
| **Test Files** | 48 |
| **Documentation Files** | 14 |
| **Sample Projects** | 12 |
| **Database Providers** | 7 |
| **Target Frameworks** | 4 (net472, net8.0, net9.0, net10.0) |

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     JD.Efcpt.Build.Tasks                        │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │   MSBuild   │ │    Chain    │ │   Schema    │ │   Config  │ │
│  │    Tasks    │ │  Resolution │ │   Readers   │ │ Overrides │ │
│  │   (11)      │ │    (4)      │ │    (7)      │ │   (5)     │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │ Connection  │ │  Strategy   │ │  Decorator  │ │   Utils   │ │
│  │   Strings   │ │   Pattern   │ │   Pattern   │ │           │ │
│  │    (4)      │ │    (1)      │ │    (1)      │ │   (8+)    │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Files | Responsibility |
|-----------|-------|----------------|
| **MSBuild Tasks** | 11 | Core build integration |
| **Schema Readers** | 7 | Database schema extraction per provider |
| **Resolution Chains** | 4 | Multi-tier file/directory/resource resolution |
| **Connection String Parsers** | 4 | Parse connection strings from various sources |
| **Config System** | 2 | Override application and model |
| **Extensions** | 3 | Helper methods for collections, strings, DataRow |
| **Compatibility** | 2 | .NET Framework polyfills |

---

## Testing Audit

### Current Test Coverage

| Category | Test Files | Pattern | Status |
|----------|------------|---------|--------|
| **Pipeline Tests** | 1 | TinyBDD | Complete |
| **Task Tests** | 15 | TinyBDD | Complete |
| **Schema Tests** | 4 | TinyBDD | Complete |
| **Integration Tests** | 10 | TinyBDD + Testcontainers | Good |
| **Connection String Tests** | 4 | TinyBDD | Complete |
| **Utility Tests** | 10 | TinyBDD | Complete |
| **SDK Integration Tests** | 5 | xUnit | Needs TinyBDD |

### TinyBDD Framework Assessment

**Current Status:** Excellent - All core tests use TinyBDD consistently

**Pattern Used:**
```csharp
[Feature("Component: brief description")]
[Collection(nameof(AssemblySetup))]
public sealed class ComponentTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(...);
    private sealed record TaskResult(...);

    [Scenario("Description of behavior")]
    [Fact]
    public async Task Scenario_name()
    {
        await Given("context", Setup)
            .When("action", Execute)
            .Then("assertion", r => r.Condition)
            .And("more assertions", r => r.OtherCondition)
            .Finally(r => r.Cleanup())
            .AssertPassed();
    }
}
```

### Test Coverage Gaps

#### High Priority (Must Address)

| Area | Gap | Risk |
|------|-----|------|
| **Error Paths** | Limited negative testing for schema readers | Medium |
| **Edge Cases** | Missing boundary tests for fingerprinting | Medium |
| **Concurrency** | No parallel build tests | Low |
| **Large Files** | Limited tests for large DACPACs | Low |

#### Medium Priority (Should Address)

| Area | Gap | Risk |
|------|-----|------|
| **SDK Integration** | Tests not using TinyBDD pattern | Style inconsistency |
| **Connection String Edge Cases** | Malformed connection strings | Low |
| **Template Processing** | T4 template error handling | Medium |
| **Cross-Platform** | Limited path separator tests | Platform-specific bugs |

#### Missing Test Scenarios

**Schema Readers:**
- [ ] Connection timeout handling
- [ ] Invalid credentials handling
- [ ] Network interruption during schema read
- [ ] Empty schema variations per provider
- [ ] Large schema performance (1000+ tables)

**Configuration:**
- [ ] Malformed JSON handling
- [ ] Missing required sections
- [ ] Type coercion edge cases
- [ ] Unicode in property values

**Pipeline:**
- [ ] DACPAC corruption detection
- [ ] Disk full scenarios
- [ ] Permission denied scenarios
- [ ] Concurrent build invocations

---

## DRY Analysis

### Critical DRY Violations

#### 1. Schema Reader Duplication (Severity: High)

**Location:** `src/JD.Efcpt.Build.Tasks/Schema/Providers/`

**Problem:** All 7 schema readers duplicate significant logic:

```
SqlServerSchemaReader.cs    (132 lines)
PostgreSqlSchemaReader.cs   (144 lines)
MySqlSchemaReader.cs        (147 lines)
SqliteSchemaReader.cs       (~120 lines)
OracleSchemaReader.cs       (~140 lines)
FirebirdSchemaReader.cs     (~130 lines)
SnowflakeSchemaReader.cs    (~135 lines)
```

**Duplicated Patterns:**
1. `ReadSchema()` method structure (identical in all)
2. `GetUserTables()` filtering logic (similar in all)
3. `ReadColumnsForTable()` column mapping (90% similar)
4. `ReadIndexesForTable()` index extraction (80% similar)
5. Column name resolution helpers (GetColumnName/GetExistingColumn)

**Current Duplication Example:**
```csharp
// Repeated in SqlServerSchemaReader, PostgreSqlSchemaReader, MySqlSchemaReader
private static IEnumerable<ColumnModel> ReadColumnsForTable(
    DataTable columnsData,
    string schemaName,
    string tableName)
    => columnsData
        .AsEnumerable()
        .Where(row => /* filter condition - varies slightly */)
        .OrderBy(row => Convert.ToInt32(row["ORDINAL_POSITION"]))
        .Select(row => new ColumnModel(
            Name: row.GetString("COLUMN_NAME"),
            DataType: row.GetString("DATA_TYPE"),
            // ... same mapping logic
        ));
```

**Proposed Solution:**
```csharp
// Base class with template method pattern
internal abstract class SchemaReaderBase : ISchemaReader
{
    protected abstract IEnumerable<string> SystemSchemas { get; }
    protected abstract ColumnNameMapping ColumnNames { get; }

    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = CreateConnection(connectionString);
        connection.Open();

        var columnsData = connection.GetSchema("Columns");
        var tablesList = GetUserTables(connection);

        // Unified logic with provider-specific column names
        return BuildSchemaModel(columnsData, tablesList);
    }

    protected virtual IEnumerable<ColumnModel> MapColumns(DataTable data, string schema, string table)
    {
        // Default implementation using ColumnNames mapping
    }
}
```

#### 2. Process Execution Duplication (Severity: Medium)

**Location:** `src/JD.Efcpt.Build.Tasks/RunEfcpt.cs`

**Problem:** ProcessStartInfo setup is duplicated:
- `IsDotNet10SdkInstalled()` - lines 503-556
- `IsDnxAvailable()` - lines 558-584

**Proposed Solution:**
```csharp
internal static class ProcessHelper
{
    public static bool TryExecuteForCheck(
        string exe,
        string args,
        int timeoutMs,
        Func<string, bool>? outputValidator = null)
    {
        // Unified process execution with timeout
    }
}
```

#### 3. Column Name Resolution (Severity: Medium)

**Location:** Multiple schema readers

**Problem:** Each reader has its own `GetColumnName` or `GetExistingColumn` method:
```csharp
// PostgreSqlSchemaReader
private static string GetColumnName(DataTable table, params string[] possibleNames)
    => possibleNames.FirstOrDefault(name => table.Columns.Contains(name)) ?? possibleNames[0];

// MySqlSchemaReader
private static string? GetExistingColumn(DataTable table, params string[] possibleNames)
    => possibleNames.FirstOrDefault(name => table.Columns.Contains(name));
```

**Proposed Solution:**
Add to `DataRowExtensions.cs`:
```csharp
public static string GetColumnOrDefault(this DataTable table, params string[] candidates)
    => candidates.FirstOrDefault(table.Columns.Contains) ?? candidates[0];

public static string? GetColumnOrNull(this DataTable table, params string[] candidates)
    => candidates.FirstOrDefault(table.Columns.Contains);
```

### PatternKit Enhancement Opportunities

#### 1. Schema Reader Factory Pattern

**Current:** Simple switch expression in `DatabaseProviderFactory`

**Proposed:** Chain-based provider resolution
```csharp
internal static class SchemaReaderChain
{
    private static readonly ResultChain<string, ISchemaReader> Chain =
        ResultChain<string, ISchemaReader>.Create()
            .When(static p => p.IsOneOf("mssql", "sqlserver", "sql-server"))
            .Then(static _ => new SqlServerSchemaReader())
            .When(static p => p.IsOneOf("postgres", "postgresql", "pgsql"))
            .Then(static _ => new PostgreSqlSchemaReader())
            // ... other providers
            .Default(static p => throw new NotSupportedException($"Provider '{p}' not supported"))
            .Build();
}
```

#### 2. Configuration Override Chain

**Current:** Direct property application in `EfcptConfigOverrideApplicator`

**Proposed:** Chain of responsibility for override sections
```csharp
internal static class ConfigOverrideChain
{
    private static readonly ActionChain<ConfigOverrideContext> Chain =
        ActionChain<ConfigOverrideContext>.Create()
            .When(ctx => ctx.Overrides.Names != null)
            .Then(ctx => ApplyNamesSection(ctx))
            .When(ctx => ctx.Overrides.FileLayout != null)
            .Then(ctx => ApplyFileLayoutSection(ctx))
            // ... other sections
            .Build();
}
```

---

## SOLID Compliance Review

### Single Responsibility Principle (SRP)

| Component | Status | Issue | Action |
|-----------|--------|-------|--------|
| `RunEfcpt` | Warning | 658 lines, multiple responsibilities | Extract tool resolution |
| `ResolveSqlProjAndInputs` | Warning | File resolution + connection string + SQL proj detection | Consider splitting |
| `EfcptConfigOverrideApplicator` | Good | Single purpose |  |
| Schema Readers | Good | Each handles one provider |  |
| Resolution Chains | Good | Each handles one resolution type |  |

**Recommended Extractions:**

1. **From `RunEfcpt`:**
   - `ToolResolutionService` - Handle tool mode detection and path resolution
   - `SdkVersionDetector` - Handle .NET SDK version checks
   - `ProcessExecutor` - Handle external process execution

2. **From `ResolveSqlProjAndInputs`:**
   - `ConnectionStringResolver` - Handle all connection string resolution
   - `SqlProjectDetector` - Handle SQL project discovery

### Open/Closed Principle (OCP)

| Component | Status | Issue | Action |
|-----------|--------|-------|--------|
| `DatabaseProviderFactory` | Warning | Requires modification for new providers | Use registry pattern |
| Schema Readers | Good | Each provider is a separate class |  |
| Resolution Chains | Good | Uses PatternKit chain pattern |  |

**Recommended Pattern:**
```csharp
public interface ISchemaReaderRegistry
{
    void Register(string provider, Func<ISchemaReader> factory);
    ISchemaReader Get(string provider);
}
```

### Liskov Substitution Principle (LSP)

| Component | Status | Notes |
|-----------|--------|-------|
| `ISchemaReader` | Good | All implementations are substitutable |
| `IBuildLog` | Good | All loggers are substitutable |
| Task inheritance | Good | Uses composition over inheritance |

### Interface Segregation Principle (ISP)

| Component | Status | Issue | Action |
|-----------|--------|-------|--------|
| `ISchemaReader` | Good | Single method interface |  |
| `IBuildLog` | Good | Focused logging methods |  |
| MSBuild Task Properties | Warning | Tasks have many properties | Consider property groups |

### Dependency Inversion Principle (DIP)

| Component | Status | Issue | Action |
|-----------|--------|-------|--------|
| Tasks → BuildLog | Good | Uses `IBuildLog` interface |  |
| Tasks → File System | Warning | Direct `File`/`Directory` calls | Consider `IFileSystem` |
| Tasks → Process | Warning | Direct `Process` calls | Consider `IProcessRunner` |

**Recommended Abstractions:**
```csharp
public interface IFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    // ...
}

public interface IProcessRunner
{
    ProcessResult Run(ProcessSpec spec);
}
```

---

## Documentation Plan

### Current Documentation Assessment

| Document | Audience | Quality | Gaps |
|----------|----------|---------|------|
| `README.md` | All | Good | Missing architecture diagrams |
| `QUICKSTART.md` | Beginners | Good | None |
| `CONTRIBUTING.md` | Contributors | Fair | Missing testing guide |
| `docs/user-guide/` | Users | Good | Missing case studies |
| `docs/user-guide/api-reference.md` | Developers | Excellent | None |

### Documentation Gaps

#### Missing Documentation

1. **Architecture Documentation**
   - System component diagrams
   - Data flow diagrams
   - Decision records (ADRs)

2. **Educational Content**
   - "How It Works" deep-dives
   - Build pipeline walkthrough
   - Fingerprinting explanation
   - Multi-target framework explanation

3. **Use Cases & Case Studies**
   - Enterprise adoption guide
   - CI/CD integration patterns
   - Multi-database scenarios
   - Microservices patterns

4. **API Coverage**
   - Internal API documentation
   - Extension points guide
   - Custom task development

5. **Troubleshooting Expansion**
   - Common error catalog
   - Debug flowcharts
   - Performance tuning guide

### Documentation Deliverables

#### Phase 1: Core Documentation Enhancement

| Document | Purpose | Priority |
|----------|---------|----------|
| `docs/architecture/README.md` | Architecture overview | High |
| `docs/architecture/PIPELINE.md` | Build pipeline deep-dive | High |
| `docs/architecture/FINGERPRINTING.md` | Change detection explained | High |
| `docs/architecture/MULTI-TARGETING.md` | Framework targeting | Medium |

#### Phase 2: User Guides

| Document | Purpose | Priority |
|----------|---------|----------|
| `docs/user-guide/use-cases/README.md` | Use case index | High |
| `docs/user-guide/use-cases/enterprise.md` | Enterprise adoption | High |
| `docs/user-guide/use-cases/microservices.md` | Microservices patterns | Medium |
| `docs/user-guide/use-cases/ci-cd-patterns.md` | CI/CD deep-dive | High |
| `docs/user-guide/use-cases/multi-database.md` | Multi-database setup | Medium |

#### Phase 3: Case Studies

| Document | Purpose | Priority |
|----------|---------|----------|
| `docs/case-studies/README.md` | Case study index | Medium |
| `docs/case-studies/large-schema.md` | 500+ table database | Medium |
| `docs/case-studies/hybrid-approach.md` | DACPAC + connection string | Medium |
| `docs/case-studies/monorepo.md` | Monorepo integration | Low |

#### Phase 4: Developer Documentation

| Document | Purpose | Priority |
|----------|---------|----------|
| `docs/contributing/TESTING.md` | Testing guide | High |
| `docs/contributing/ARCHITECTURE.md` | Architecture decisions | Medium |
| `docs/contributing/DEBUGGING.md` | Debugging guide | Medium |
| `docs/contributing/RELEASING.md` | Release process | Low |

### Sample Enhancements

| Sample | Enhancement | Priority |
|--------|-------------|----------|
| All samples | Add README.md explaining purpose | High |
| `schema-organization/` | Add schema diagram | Medium |
| New: `multi-provider/` | PostgreSQL + SQL Server | Medium |
| New: `github-actions/` | Complete CI/CD example | High |
| New: `azure-devops/` | Azure DevOps pipeline | Medium |

---

## Testing Plan

### Test Improvement Strategy

#### 1. Convert SDK Integration Tests to TinyBDD

**Files to Convert:**
- `SdkIntegrationTests.cs`
- `BuildTransitiveTests.cs`
- `FrameworkMsBuildTests.cs`
- `CodeGenerationTests.cs`
- `TemplateTests.cs`

**Conversion Template:**
```csharp
// Before (xUnit)
[Fact]
public async Task Test_Something()
{
    // Arrange
    var setup = CreateSetup();

    // Act
    var result = await Execute(setup);

    // Assert
    Assert.True(result.Success);
}

// After (TinyBDD)
[Scenario("Description of behavior")]
[Fact]
public async Task Test_Something()
{
    await Given("setup context", CreateSetup)
        .When("executing action", Execute)
        .Then("expected outcome", r => r.Success)
        .Finally(r => r.Cleanup())
        .AssertPassed();
}
```

#### 2. Add Missing Negative Tests

**Schema Reader Negative Tests:**
```csharp
[Feature("SchemaReader: error handling")]
public sealed class SchemaReaderErrorTests
{
    [Scenario("Invalid connection string throws descriptive exception")]
    [Fact]
    public async Task Invalid_connection_string_throws()
    {
        await Given("invalid connection string", () => "InvalidConnectionString")
            .When("reading schema", connectionString =>
            {
                try { reader.ReadSchema(connectionString); return (false, null); }
                catch (Exception ex) { return (true, ex); }
            })
            .Then("exception is thrown", r => r.Item1)
            .And("exception message is helpful", r =>
                r.Item2!.Message.Contains("connection") ||
                r.Item2!.Message.Contains("format"))
            .AssertPassed();
    }

    [Scenario("Network timeout produces clear error")]
    [Scenario("Permission denied produces clear error")]
    [Scenario("Database not found produces clear error")]
}
```

**Configuration Negative Tests:**
```csharp
[Feature("ApplyConfigOverrides: error handling")]
public sealed class ConfigOverrideErrorTests
{
    [Scenario("Malformed JSON is handled gracefully")]
    [Scenario("Missing file produces clear error")]
    [Scenario("Invalid property type is reported")]
    [Scenario("Permission denied is handled")]
}
```

#### 3. Add Edge Case Tests

**Fingerprinting Edge Cases:**
```csharp
[Feature("ComputeFingerprint: edge cases")]
public sealed class FingerprintEdgeCaseTests
{
    [Scenario("Empty DACPAC produces valid fingerprint")]
    [Scenario("Binary-identical files produce same fingerprint")]
    [Scenario("Whitespace-only config changes fingerprint")]
    [Scenario("File with null bytes handled correctly")]
    [Scenario("Very large file (>1GB) handled correctly")]
}
```

**Path Edge Cases:**
```csharp
[Feature("PathUtils: edge cases")]
public sealed class PathEdgeCaseTests
{
    [Scenario("Path with spaces handled correctly")]
    [Scenario("Path with unicode characters handled")]
    [Scenario("Very long path (>260 chars) handled")]
    [Scenario("Relative path with .. normalized")]
    [Scenario("Mixed path separators normalized")]
}
```

#### 4. Add Integration Test Scenarios

**End-to-End Scenarios:**
```csharp
[Feature("Pipeline: complex scenarios")]
public sealed class PipelineComplexTests
{
    [Scenario("Multiple SQL projects in solution detected correctly")]
    [Scenario("Config at solution level overrides project level")]
    [Scenario("Template changes trigger regeneration")]
    [Scenario("Incremental build skips unchanged projects")]
    [Scenario("Clean build removes all generated files")]
}
```

### Test Coverage Metrics Target

| Metric | Current | Target |
|--------|---------|--------|
| Line Coverage | ~75% | 90%+ |
| Branch Coverage | ~60% | 85%+ |
| Method Coverage | ~80% | 95%+ |
| Mutation Score | Unknown | 70%+ |

---

## Refactoring Plan

### Phase 1: Schema Reader Consolidation

**Goal:** Reduce schema reader duplication by 60%

**Approach:**
1. Create abstract base class with template method pattern
2. Define provider-specific column mappings
3. Extract common filtering and projection logic
4. Maintain full backward compatibility

**Implementation:**

```
src/JD.Efcpt.Build.Tasks/Schema/
├── ISchemaReader.cs (unchanged)
├── SchemaReaderBase.cs (new)
├── ColumnMapping.cs (new)
├── Providers/
│   ├── SqlServerSchemaReader.cs (refactored)
│   ├── PostgreSqlSchemaReader.cs (refactored)
│   └── ... (other providers)
```

**Files to Create:**
- `SchemaReaderBase.cs` - Abstract base with template methods
- `ColumnMapping.cs` - Column name mapping per provider
- `SchemaReaderExtensions.cs` - Shared extension methods

**Estimated Reduction:**
- Current: ~950 lines across 7 readers
- After: ~500 lines (45% reduction)

### Phase 2: Task Decomposition

**Goal:** Improve SRP compliance for large tasks

**`RunEfcpt` Decomposition:**
```
Current: RunEfcpt.cs (658 lines)

Proposed:
├── RunEfcpt.cs (~200 lines) - Orchestration only
├── Services/
│   ├── ToolResolver.cs (~150 lines) - Tool mode detection
│   ├── SdkDetector.cs (~100 lines) - SDK version checks
│   └── EfcptInvoker.cs (~150 lines) - Process execution
```

**`ResolveSqlProjAndInputs` Decomposition:**
```
Current: ResolveSqlProjAndInputs.cs (~500 lines)

Proposed:
├── ResolveSqlProjAndInputs.cs (~250 lines) - Orchestration
├── Services/
│   ├── SqlProjectDetector.cs (~100 lines) - SQL project discovery
│   └── ConnectionStringResolver.cs (~100 lines) - Connection resolution
```

### Phase 3: PatternKit Integration

**Goal:** Leverage PatternKit for consistent patterns

**New Chain Implementations:**
```csharp
// Provider resolution chain
internal static class ProviderResolutionChain
{
    public static ResultChain<string, (DbConnection, ISchemaReader)> Build()
        => ResultChain<string, (DbConnection, ISchemaReader)>.Create()
            .When(IsSqlServer)
            .Then(CreateSqlServerPair)
            .When(IsPostgreSql)
            .Then(CreatePostgreSqlPair)
            // ...
            .Build();
}

// Configuration section chain
internal static class ConfigSectionChain
{
    public static ActionChain<ConfigContext> Build()
        => ActionChain<ConfigContext>.Create()
            .When(HasNamesOverrides)
            .Then(ApplyNamesSection)
            .When(HasFileLayoutOverrides)
            .Then(ApplyFileLayoutSection)
            // ...
            .Build();
}
```

### Phase 4: Extension Methods Consolidation

**Goal:** Centralize reusable extensions

**Files to Enhance:**
- `DataRowExtensions.cs` - Add column resolution helpers
- `StringExtensions.cs` - Add provider normalization
- `FileSystemExtensions.cs` (new) - Path utilities

**Example Additions:**
```csharp
public static class DataRowExtensions
{
    // Existing methods...

    // New methods
    public static string ResolveColumnName(
        this DataTable table,
        params string[] candidates)
        => candidates.FirstOrDefault(table.Columns.Contains)
           ?? throw new InvalidOperationException(
               $"None of columns [{string.Join(", ", candidates)}] found");
}
```

---

## Implementation Phases

### Phase 1: Foundation (Weeks 1-2)

**Focus:** Testing infrastructure and base refactoring

| Task | Priority | Effort |
|------|----------|--------|
| Convert SDK integration tests to TinyBDD | High | Medium |
| Add schema reader negative tests | High | Medium |
| Create `SchemaReaderBase` abstract class | High | High |
| Add missing edge case tests | Medium | Medium |

**Deliverables:**
- [ ] All tests use TinyBDD pattern
- [ ] 20+ new negative test cases
- [ ] Schema reader base class implemented
- [ ] Test coverage increased to 85%

### Phase 2: DRY Refactoring (Weeks 3-4)

**Focus:** Code consolidation and pattern application

| Task | Priority | Effort |
|------|----------|--------|
| Consolidate schema readers using base class | High | High |
| Extract `ToolResolver` from `RunEfcpt` | High | Medium |
| Extract `SqlProjectDetector` service | Medium | Medium |
| Add PatternKit chains for providers | Medium | Medium |

**Deliverables:**
- [ ] Schema readers reduced by 45%
- [ ] RunEfcpt reduced to ~200 lines
- [ ] New service classes created
- [ ] All tests passing

### Phase 3: Documentation (Weeks 5-6)

**Focus:** User-facing documentation

| Task | Priority | Effort |
|------|----------|--------|
| Create architecture documentation | High | Medium |
| Write pipeline deep-dive | High | Medium |
| Add use case guides | High | High |
| Enhance sample READMEs | Medium | Low |
| Create case studies | Medium | Medium |

**Deliverables:**
- [ ] Architecture docs complete
- [ ] 4 use case guides
- [ ] 2 case studies
- [ ] All samples have READMEs

### Phase 4: Polish (Week 7)

**Focus:** Quality and consistency

| Task | Priority | Effort |
|------|----------|--------|
| Add remaining edge case tests | Medium | Medium |
| Update CONTRIBUTING.md with testing guide | High | Low |
| Create debugging guide | Medium | Medium |
| Final code review and cleanup | High | Medium |

**Deliverables:**
- [ ] Test coverage at 90%
- [ ] All documentation complete
- [ ] Code review completed
- [ ] Release notes prepared

---

## Success Criteria

### Testing Success Criteria

| Metric | Target | Measurement |
|--------|--------|-------------|
| Line Coverage | 90%+ | Coverlet report |
| Branch Coverage | 85%+ | Coverlet report |
| All Tests TinyBDD | 100% | Manual audit |
| Negative Tests | 50+ | Test count |
| Edge Case Tests | 30+ | Test count |

### Code Quality Success Criteria

| Metric | Target | Measurement |
|--------|--------|-------------|
| Schema Reader LOC | -45% | Line count comparison |
| RunEfcpt LOC | -70% | Line count comparison |
| Code Duplication | <5% | SonarQube/similar |
| Cyclomatic Complexity | <15 per method | Analysis tool |

### Documentation Success Criteria

| Metric | Target | Measurement |
|--------|--------|-------------|
| Architecture Docs | 4 files | File count |
| Use Case Guides | 4 files | File count |
| Case Studies | 2 files | File count |
| Sample READMEs | 12 files | File count |
| API Coverage | 100% | Manual audit |

### SOLID Compliance Criteria

| Principle | Target | Measurement |
|-----------|--------|-------------|
| SRP | No class >300 lines | Line count |
| OCP | Provider registry pattern | Code review |
| LSP | All interfaces substitutable | Test coverage |
| ISP | No interface >5 methods | Interface audit |
| DIP | Key abstractions defined | Code review |

---

## Appendix A: File Inventory

### Files Requiring Changes

| File | Change Type | Priority |
|------|-------------|----------|
| `SqlServerSchemaReader.cs` | Refactor to use base | High |
| `PostgreSqlSchemaReader.cs` | Refactor to use base | High |
| `MySqlSchemaReader.cs` | Refactor to use base | High |
| `SqliteSchemaReader.cs` | Refactor to use base | High |
| `OracleSchemaReader.cs` | Refactor to use base | High |
| `FirebirdSchemaReader.cs` | Refactor to use base | High |
| `SnowflakeSchemaReader.cs` | Refactor to use base | High |
| `RunEfcpt.cs` | Decompose | High |
| `ResolveSqlProjAndInputs.cs` | Extract services | Medium |
| `DataRowExtensions.cs` | Add methods | Medium |
| `SdkIntegrationTests.cs` | Convert to TinyBDD | High |
| `BuildTransitiveTests.cs` | Convert to TinyBDD | High |

### Files to Create

| File | Purpose | Priority |
|------|---------|----------|
| `SchemaReaderBase.cs` | Abstract base class | High |
| `ColumnMapping.cs` | Provider column mappings | High |
| `ToolResolver.cs` | Tool resolution service | High |
| `SdkDetector.cs` | SDK detection service | Medium |
| `SqlProjectDetector.cs` | SQL project detection | Medium |
| `docs/architecture/README.md` | Architecture overview | High |
| `docs/architecture/PIPELINE.md` | Pipeline deep-dive | High |
| Multiple test files | Negative/edge cases | High |

---

## Appendix B: Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Breaking changes during refactor | High | Medium | Comprehensive test coverage first |
| Performance regression | Medium | Low | Benchmark before/after |
| Documentation staleness | Medium | Medium | Integrate docs into PR process |
| Test flakiness | Low | Medium | Review test infrastructure |

---

## Appendix C: Dependencies

### External Dependencies

| Dependency | Version | Purpose | Update Needed |
|------------|---------|---------|---------------|
| PatternKit.Core | 0.17.3 | Chain patterns | No |
| TinyBDD.Xunit | 0.13.0 | Test framework | No |
| System.IO.Hashing | 10.0.1 | XxHash64 | No |

### Internal Dependencies

| Component | Depends On | Coupling Level |
|-----------|-----------|----------------|
| Tasks | Schema Readers | Loose |
| Tasks | Resolution Chains | Loose |
| Tasks | Config System | Medium |
| Schema Readers | Database Providers | Tight |

---

*This document is a living document and will be updated as the refactor progresses.*
