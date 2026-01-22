# Test Coverage Analysis & Improvement Plan
**Generated:** 2026-01-22  
**Project:** JD.Efcpt.Build  
**Current Coverage:** 84.4% Line Coverage, 68.1% Branch Coverage

## Executive Summary

Current test coverage is **good but not enterprise-grade**. We have 84.4% line coverage with 814 uncovered lines and only 68.1% branch coverage (845 uncovered branches). For enterprise deployment, we need:
- **Target:** 95%+ line coverage
- **Target:** 90%+ branch coverage  
- **Target:** 100% coverage of error paths and edge cases

## Critical Gaps (Classes <70% Coverage)

### 🔴 ZERO COVERAGE (0%)
#### 1. **DetectSqlProject.cs**
- **Lines:** 80 total
- **Issue:** Completely untested despite being critical for SQL project detection
- **Risk Level:** HIGH - Used in build pipeline decision-making
- **Test Plan:**
  ```csharp
  // Unit Tests Needed:
  - DetectSqlProject_WithModernSdkAttribute_ReturnsTrue()
  - DetectSqlProject_WithLegacySsdt_SqlServerVersion_ReturnsTrue()
  - DetectSqlProject_WithLegacySsdt_DSP_ReturnsTrue()
  - DetectSqlProject_NonSqlProject_ReturnsFalse()
  - DetectSqlProject_NullProjectPath_LogsErrorAndReturnsFalse()
  - DetectSqlProject_EmptyProjectPath_LogsErrorAndReturnsFalse()
  ```
- **Testing Approach:** Create unit test file `DetectSqlProjectTests.cs` with mocked file system

---

### 🟠 VERY LOW COVERAGE (18%)
#### 2. **RunSqlPackage.cs**  
- **Lines:** 474 total, 387 uncovered
- **Issue:** SqlPackage extraction logic barely tested
- **Risk Level:** HIGH - Used for database-first SQL generation feature
- **Current Coverage:** Only basic happy path tested
- **Test Plan:**
  ```csharp
  // Unit Tests Needed:
  - RunSqlPackage_ExplicitToolPath_UsesPath()
  - RunSqlPackage_ExplicitToolPath_NotExists_ReturnsError()
  - RunSqlPackage_DotNet10WithDnx_UsesDnx()
  - RunSqlPackage_GlobalTool_RestoresAndRuns()
  - RunSqlPackage_GlobalTool_NoRestore_RunsDirectly()
  - RunSqlPackage_ToolRestore_True_RestoresTool()
  - RunSqlPackage_ToolRestore_False_SkipsRestore()
  - RunSqlPackage_ToolRestore_InvalidValue_DefaultsToTrue()
  - RunSqlPackage_CreateTargetDirectory_Success()
  - RunSqlPackage_CreateTargetDirectory_Failure_ReturnsError()
  - RunSqlPackage_SqlPackageFailsWithExitCode_ReturnsError()
  - RunSqlPackage_MovesFilesFromDacpacSubdirectory()
  - RunSqlPackage_SkipsSystemObjects_Security()
  - RunSqlPackage_SkipsSystemObjects_ServerObjects()
  - RunSqlPackage_SkipsSystemObjects_Storage()
  - RunSqlPackage_CleanupTemporaryDirectory()
  - RunSqlPackage_CleanupFails_LogsWarning()
  ```
- **Testing Approach:** 
  - Mock ProcessRunner for unit tests
  - Create integration test with Testcontainers SQL Server
  - Test all three tool resolution modes
  - Test error paths and edge cases

---

### 🟠 LOW COVERAGE (40.9%)
#### 3. **CheckSdkVersion.cs**
- **Lines:** 257 total, 152 uncovered  
- **Issue:** Version checking and caching logic undertested
- **Risk Level:** MEDIUM - Non-critical feature but user-facing
- **Current Coverage:** Basic version check tested
- **Test Plan:**
  ```csharp
  // Unit Tests Needed:
  - CheckSdkVersion_UpdateAvailable_EmitsWarning()
  - CheckSdkVersion_UpdateAvailable_WarningLevel_Info_EmitsInfo()
  - CheckSdkVersion_UpdateAvailable_WarningLevel_Error_EmitsError()
  - CheckSdkVersion_UpdateAvailable_WarningLevel_None_NoOutput()
  - CheckSdkVersion_NoUpdate_NoWarning()
  - CheckSdkVersion_CacheHit_WithinWindow_UsesCachedVersion()
  - CheckSdkVersion_CacheHit_Expired_FetchesNewVersion()
  - CheckSdkVersion_ForceCheck_IgnoresCache()
  - CheckSdkVersion_NuGetApiFailure_ContinuesWithoutError()
  - CheckSdkVersion_CacheReadFailure_FetchesFromNuGet()
  - CheckSdkVersion_CacheWriteFailure_ContinuesSilently()
  - CheckSdkVersion_InvalidVersionString_HandlesGracefully()
  - CheckSdkVersion_PreReleaseVersions_IgnoresInFavorOfStable()
  ```
- **Testing Approach:**
  - Mock HttpClient responses
  - Mock file system for cache tests
  - Test all warning levels
  - Test error recovery paths

---

### 🟡 MODERATE LOW COVERAGE (50-60%)
#### 4. **RunEfcpt.cs** (60.3%)
- **Lines:** 544 total, ~216 uncovered
- **Issue:** Main efcpt invocation logic has gaps
- **Risk Level:** HIGH - Core code generation task
- **Test Plan:**
  ```csharp
  // Additional Unit Tests Needed:
  - RunEfcpt_ExplicitToolPath_RelativePath_ResolvesCorrectly()
  - RunEfcpt_ExplicitToolPath_NotExists_LogsError()
  - RunEfcpt_DotNet10_DnxNotAvailable_FallsBackToManifest()
  - RunEfcpt_ToolManifest_NotFound_FallsBackToGlobal()
  - RunEfcpt_ToolManifest_MultipleFound_UsesNearest()
  - RunEfcpt_GlobalTool_ToolVersionSpecified_UsesVersion()
  - RunEfcpt_ProcessFails_ReturnsError()
  - RunEfcpt_ProcessTimesOut_HandlesGracefully()
  - RunEfcpt_ConnectionStringMode_PassesCorrectArgs()
  - RunEfcpt_DacpacMode_PassesCorrectArgs()
  - RunEfcpt_ContextName_SpecifiedInConfig_UsesConfig()
  - RunEfcpt_ContextName_Empty_AutoGenerates()
  - RunEfcpt_FakeEfcpt_EnvVar_GeneratesFakeOutput()
  - RunEfcpt_TestDacpac_EnvVar_ForwardsToProcess()
  ```
- **Testing Approach:**
  - Expand existing tests to cover all tool resolution paths
  - Add tests for all argument combinations
  - Test environment variable hooks
  - Test error scenarios

#### 5. **Decorators (50%)** - ProfileInputAttribute, ProfileOutputAttribute
- **Lines:** Small classes, ~10 lines each
- **Issue:** Attribute classes not tested
- **Risk Level:** LOW - Metadata only
- **Test Plan:**
  ```csharp
  // Unit Tests Needed:
  - ProfileInputAttribute_DefaultValues_CorrectlySet()
  - ProfileInputAttribute_WithExclude_SetsExcludeTrue()
  - ProfileInputAttribute_WithCustomName_UsesName()
  - ProfileOutputAttribute_InstantiatesCorrectly()
  ```
- **Testing Approach:** Simple attribute instantiation tests

---

## Moderate Coverage Gaps (60-70%)

#### 6. **BuildLog.cs** (66.6%)
- **Test Plan:**
  ```csharp
  - BuildLog_LogVerbosity_Minimal_FiltersLowImportance()
  - BuildLog_LogVerbosity_Normal_ShowsNormalMessages()
  - BuildLog_LogVerbosity_Detailed_ShowsAllMessages()
  - BuildLog_Detail_WithMinimalVerbosity_Suppressed()
  - BuildLog_Info_WithMinimalVerbosity_Shown()
  ```

#### 7. **TaskExecutionContext.cs** (66.6%)
- **Test Plan:**
  ```csharp
  - TaskExecutionContext_ProfilingEnabled_RecordsData()
  - TaskExecutionContext_ProfilingDisabled_NoRecording()
  - TaskExecutionContext_Logger_ForwardsToMsBuildEngine()
  ```

#### 8. **DotNetToolUtilities.cs** (66.6%)
- **Test Plan:**
  ```csharp
  - IsDotNet10OrLater_Net10_ReturnsTrue()
  - IsDotNet10OrLater_Net9_ReturnsFalse()
  - IsDotNet10OrLater_InvalidFramework_ReturnsFalse()
  - IsDnxAvailable_DnxExists_ReturnsTrue()
  - IsDnxAvailable_DnxNotExists_ReturnsFalse()
  - IsDnxAvailable_ProcessFails_ReturnsFalse()
  ```

#### 9. **JsonTimeSpanConverter.cs** (63.6%)
- **Test Plan:**
  ```csharp
  - JsonTimeSpanConverter_Read_ValidString_Parses()
  - JsonTimeSpanConverter_Write_FormatsCorrectly()
  - JsonTimeSpanConverter_Read_InvalidFormat_ThrowsException()
  ```

---

## Branch Coverage Gaps (68.1%)

845 uncovered branches indicate **insufficient edge case testing**. Key areas:

### High-Priority Branch Coverage
1. **Error Handling Branches**
   - Exception catch blocks
   - Null checks
   - Empty string validation
   - File/directory existence checks

2. **Conditional Logic Branches**
   - If/else branches for tool resolution
   - Switch statements for message levels
   - Try/catch/finally blocks
   - Ternary operators

3. **Loop Coverage**
   - Early exit conditions
   - Empty collection handling
   - First/last iteration edge cases

---

## Integration Test Gaps

Current integration tests cover happy paths well but miss:

### SQL Generation Integration Tests
✅ **Currently Covered:**
- SqlProject_WithEfcptBuild_IsDetectedAsSqlProject
- SqlProject_GeneratesSqlScriptsWithProperStructure
- SqlProject_AddsAutoGenerationWarningsToSqlFiles
- DataAccessProject_ReferencingSqlProject_GeneratesEfCoreModels
- SqlProject_WithUnchangedSchema_SkipsRegeneration

❌ **Missing:**
- SQL generation with connection string errors
- SQL generation with invalid credentials
- SQL generation with unreachable database
- SQL generation with schema validation errors
- SQL generation with large databases (performance test)
- SQL generation with special characters in table/column names
- SQL generation incremental updates
- Concurrent builds with SQL generation

### DACPAC Build Integration Tests
❌ **Missing:**
- DACPAC build failures
- DACPAC path resolution errors
- DACPAC with circular dependencies
- DACPAC version incompatibility

### Tool Resolution Integration Tests
❌ **Missing:**
- Tool resolution in CI environments (no PATH)
- Tool resolution with corrupted tool manifests
- Tool resolution with network failures (NuGet down)
- Tool resolution with permission errors

---

## Error Condition Coverage Plan

### File System Errors
```csharp
// Tests Needed:
- FileNotFound when reading configuration
- DirectoryNotFound when creating output
- UnauthorizedAccess when writing files
- PathTooLong for deep directory structures
- InvalidPath for malformed paths
- FileLocked when output files in use
- DiskFull when writing large files
```

### Network Errors
```csharp
// Tests Needed:
- HttpRequestTimeout when checking NuGet
- ConnectionRefused when connecting to database
- ConnectionTimeout during schema query
- AuthenticationFailure with invalid credentials
- SSLHandshakeFailure with certificate issues
```

### Process Execution Errors
```csharp
// Tests Needed:
- ProcessNotFound when tool missing
- ProcessAccessDenied when permissions insufficient
- ProcessKilledByUser (Ctrl+C handling)
- ProcessCrashed (exit code 139)
- ProcessHung (timeout handling)
- ProcessOutputTooLarge (buffer overflow)
```

### Configuration Errors
```csharp
// Tests Needed:
- MalformedJson in efcpt-config.json
- MissingRequiredProperty in configuration
- InvalidPropertyValue (e.g., negative numbers)
- ConflictingProperties (mutually exclusive options)
- UnknownProperties (forward compatibility)
```

### MSBuild Errors
```csharp
// Tests Needed:
- TargetNotFound when dependency missing
- PropertyNotSet when required property missing
- ItemNotDefined when collection empty
- MultipleProjects in solution ambiguity
- CircularDependency detection
```

---

## Implementation Priority

### Phase 1: Critical Coverage (Week 1)
**Goal: 90% line coverage**
1. ✅ DetectSqlProject.cs - Add full unit test suite (8 tests)
2. ✅ RunSqlPackage.cs - Add comprehensive unit tests (17 tests)
3. ✅ CheckSdkVersion.cs - Complete warning level and cache tests (13 tests)
4. ✅ RunEfcpt.cs - Fill tool resolution gaps (14 tests)

### Phase 2: Branch Coverage (Week 2)
**Goal: 85% branch coverage**
1. ✅ Add tests for all catch blocks
2. ✅ Add tests for all conditional branches
3. ✅ Add tests for early returns
4. ✅ Add tests for null/empty validations

### Phase 3: Error Scenarios (Week 3)
**Goal: 100% error path coverage**
1. ✅ File system error tests (7 scenarios)
2. ✅ Network error tests (5 scenarios)
3. ✅ Process execution error tests (6 scenarios)
4. ✅ Configuration error tests (5 scenarios)
5. ✅ MSBuild error tests (5 scenarios)

### Phase 4: Integration Tests (Week 4)
**Goal: Full E2E coverage**
1. ✅ SQL generation error scenarios (8 tests)
2. ✅ DACPAC build failures (4 tests)
3. ✅ Tool resolution edge cases (4 tests)
4. ✅ Concurrent build scenarios (3 tests)
5. ✅ Performance tests with large databases (2 tests)

---

## Testing Infrastructure Improvements

### 1. Add Mutation Testing
```bash
dotnet tool install -g stryker
stryker --config-file stryker-config.json
```
**Purpose:** Verify tests actually catch bugs (not just exercise code)

### 2. Add Property-Based Testing (FsCheck)
```csharp
[Property]
public Property PathNormalization_AlwaysProducesValidPath(string input)
{
    var normalized = PathUtils.Normalize(input);
    return (Path.IsPathRooted(normalized) || string.IsNullOrEmpty(normalized))
        .ToProperty();
}
```
**Purpose:** Test with thousands of random inputs

### 3. Add Benchmarking Tests
```csharp
[Benchmark]
public void ComputeFingerprint_LargeDacpac()
{
    var fingerprint = ComputeFingerprint(LargeDacpacPath);
}
```
**Purpose:** Detect performance regressions

### 4. Add Snapshot Testing
```csharp
[Fact]
public void GeneratedConfig_MatchesSnapshot()
{
    var config = GenerateConfig();
    Snapshot.Match(config);
}
```
**Purpose:** Catch unintended output changes

---

## Continuous Monitoring

### CI/CD Pipeline Additions
```yaml
# .github/workflows/test.yml additions:
- name: Run Tests with Coverage
  run: dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
  
- name: Generate Coverage Report
  run: reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html;Cobertura;Badges"
  
- name: Upload Coverage to Codecov
  uses: codecov/codecov-action@v3
  with:
    files: ./CoverageReport/Cobertura.xml
    fail_ci_if_error: true
    
- name: Enforce Coverage Threshold
  run: |
    COVERAGE=$(grep -oP 'line-rate="\K[0-9.]+' CoverageReport/Cobertura.xml | head -1)
    if (( $(echo "$COVERAGE < 0.95" | bc -l) )); then
      echo "Coverage $COVERAGE is below 95% threshold"
      exit 1
    fi
```

### Pre-commit Hook
```bash
#!/bin/bash
# .git/hooks/pre-commit
dotnet test --collect:"XPlat Code Coverage" --no-build
COVERAGE=$(xmllint --xpath "string(//coverage/@line-rate)" TestResults/*/coverage.cobertura.xml)
if (( $(echo "$COVERAGE < 0.85" | bc -l) )); then
    echo "❌ Coverage dropped below 85%: $COVERAGE"
    exit 1
fi
```

---

## Success Metrics

### Coverage Targets
| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Line Coverage | 84.4% | 95% | 🟡 |
| Branch Coverage | 68.1% | 90% | 🔴 |
| Method Coverage | 93.9% | 98% | 🟡 |
| Full Method Coverage | 82.5% | 95% | 🟡 |

### Quality Gates
- ✅ **Zero** untested public methods
- ✅ **Zero** uncaught error conditions
- ✅ **All** edge cases documented and tested
- ✅ **All** error messages have corresponding tests
- ✅ **All** configuration options have tests
- ✅ **All** MSBuild tasks have unit tests
- ✅ **All** MSBuild tasks have integration tests

---

## Risk Assessment

### Current Risks with 84% Coverage

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Unhandled file system errors | HIGH | MEDIUM | Add file I/O error tests |
| SQL connection failures | HIGH | HIGH | Add database error tests |
| Tool resolution failures | MEDIUM | MEDIUM | Add tool path tests |
| Configuration parsing errors | MEDIUM | LOW | Add config validation tests |
| Concurrent build issues | LOW | MEDIUM | Add parallel build tests |

### Post-95% Coverage Risks

| Risk | Impact | Probability |
|------|--------|-------------|
| Unhandled file system errors | LOW | LOW |
| SQL connection failures | LOW | LOW |
| Tool resolution failures | LOW | LOW |
| Configuration parsing errors | LOW | LOW |
| Concurrent build issues | LOW | LOW |

---

## Appendix: Coverage Commands

### Generate Coverage Report
```bash
cd C:\git\JD.Efcpt.Build

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults -c Release

# Generate HTML report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"Html;TextSummary;Badges"

# View report
start TestResults/CoverageReport/index.html
```

### Coverage by Class
```bash
# Find lowest coverage classes
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:"JsonSummary"

cat TestResults/CoverageReport/Summary.json | jq '.coverage[] | select(.coverage < 70) | {name, coverage}'
```

### Diff Coverage (Only Changed Lines)
```bash
# Install diff-cover
pip install diff-cover

# Compare against main branch
diff-cover TestResults/**/coverage.cobertura.xml --compare-branch=main --fail-under=100
```

---

**Document Maintained By:** Engineering Team  
**Last Updated:** 2026-01-22  
**Review Cycle:** Weekly during coverage improvement phase
