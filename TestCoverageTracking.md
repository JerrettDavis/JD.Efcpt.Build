# Test Coverage Improvement Tracking

## Phase 1: Critical Coverage (Week 1) - Target: 90% Line Coverage

### DetectSqlProject.cs (Currently: 0% → Target: 100%)
- [ ] DetectSqlProject_WithModernSdkAttribute_ReturnsTrue
- [ ] DetectSqlProject_WithLegacySsdt_SqlServerVersion_ReturnsTrue  
- [ ] DetectSqlProject_WithLegacySsdt_DSP_ReturnsTrue
- [ ] DetectSqlProject_NonSqlProject_ReturnsFalse
- [ ] DetectSqlProject_NullProjectPath_LogsErrorAndReturnsFalse
- [ ] DetectSqlProject_EmptyProjectPath_LogsErrorAndReturnsFalse
- [ ] DetectSqlProject_BothLegacyProperties_ReturnsTrue
- [ ] DetectSqlProject_NoSdkNoProperties_ReturnsFalse

**Estimated Time:** 4 hours  
**File:** `tests/JD.Efcpt.Build.Tests/DetectSqlProjectTests.cs` (NEW)

---

### RunSqlPackage.cs (Currently: 18% → Target: 85%)
- [ ] RunSqlPackage_ExplicitToolPath_UsesPath
- [ ] RunSqlPackage_ExplicitToolPath_NotExists_ReturnsError
- [ ] RunSqlPackage_ExplicitToolPath_RelativePath_ResolvesCorrectly
- [ ] RunSqlPackage_DotNet10WithDnx_UsesDnx
- [ ] RunSqlPackage_GlobalTool_RestoresAndRuns
- [ ] RunSqlPackage_GlobalTool_NoRestore_RunsDirectly
- [ ] RunSqlPackage_ToolRestore_True_RestoresTool
- [ ] RunSqlPackage_ToolRestore_False_SkipsRestore
- [ ] RunSqlPackage_ToolRestore_One_RestoresTool
- [ ] RunSqlPackage_ToolRestore_Yes_RestoresTool
- [ ] RunSqlPackage_ToolRestore_Empty_DefaultsToTrue
- [ ] RunSqlPackage_CreateTargetDirectory_Success
- [ ] RunSqlPackage_CreateTargetDirectory_Failure_ReturnsError
- [ ] RunSqlPackage_SqlPackageFailsWithExitCode_ReturnsError
- [ ] RunSqlPackage_MovesFilesFromDacpacSubdirectory
- [ ] RunSqlPackage_SkipsSystemObjects_Security
- [ ] RunSqlPackage_SkipsSystemObjects_ServerObjects
- [ ] RunSqlPackage_SkipsSystemObjects_Storage
- [ ] RunSqlPackage_CleanupTemporaryDirectory
- [ ] RunSqlPackage_CleanupFails_LogsWarning
- [ ] RunSqlPackage_ProcessStartFails_ReturnsError
- [ ] RunSqlPackage_ToolVersion_PassedToRestore

**Estimated Time:** 12 hours  
**File:** `tests/JD.Efcpt.Build.Tests/RunSqlPackageTests.cs` (NEW)

---

### CheckSdkVersion.cs (Currently: 40.9% → Target: 90%)
- [ ] CheckSdkVersion_UpdateAvailable_EmitsWarning
- [ ] CheckSdkVersion_UpdateAvailable_WarningLevel_Info_EmitsInfo
- [ ] CheckSdkVersion_UpdateAvailable_WarningLevel_Error_EmitsError
- [ ] CheckSdkVersion_UpdateAvailable_WarningLevel_None_NoOutput
- [ ] CheckSdkVersion_NoUpdate_NoWarning
- [ ] CheckSdkVersion_CurrentVersionNewer_NoWarning
- [ ] CheckSdkVersion_SameVersion_NoWarning
- [ ] CheckSdkVersion_CacheHit_WithinWindow_UsesCachedVersion
- [ ] CheckSdkVersion_CacheHit_Expired_FetchesNewVersion
- [ ] CheckSdkVersion_ForceCheck_IgnoresCache
- [ ] CheckSdkVersion_NuGetApiFailure_ContinuesWithoutError
- [ ] CheckSdkVersion_CacheReadFailure_FetchesFromNuGet
- [ ] CheckSdkVersion_CacheWriteFailure_ContinuesSilently
- [ ] CheckSdkVersion_InvalidVersionString_HandlesGracefully
- [ ] CheckSdkVersion_PreReleaseVersions_IgnoresInFavorOfStable
- [ ] CheckSdkVersion_EmptyCurrentVersion_NoWarning
- [ ] CheckSdkVersion_EmptyLatestVersion_NoWarning

**Estimated Time:** 8 hours  
**File:** `tests/JD.Efcpt.Build.Tests/CheckSdkVersionTests.cs` (EXPAND)

---

### RunEfcpt.cs (Currently: 60.3% → Target: 85%)
- [ ] RunEfcpt_ExplicitToolPath_RelativePath_ResolvesCorrectly
- [ ] RunEfcpt_ExplicitToolPath_NotExists_LogsError
- [ ] RunEfcpt_DotNet10_DnxNotAvailable_FallsBackToManifest
- [ ] RunEfcpt_ToolManifest_NotFound_FallsBackToGlobal
- [ ] RunEfcpt_ToolManifest_MultipleFound_UsesNearest
- [ ] RunEfcpt_ToolManifest_WalkUpFromWorkingDir_FindsManifest
- [ ] RunEfcpt_GlobalTool_ToolVersionSpecified_UsesVersion
- [ ] RunEfcpt_ProcessFails_ReturnsError
- [ ] RunEfcpt_ProcessFailsWithStderr_LogsError
- [ ] RunEfcpt_ConnectionStringMode_PassesCorrectArgs
- [ ] RunEfcpt_DacpacMode_PassesCorrectArgs
- [ ] RunEfcpt_ContextName_SpecifiedInConfig_UsesConfig
- [ ] RunEfcpt_ContextName_Empty_AutoGenerates
- [ ] RunEfcpt_FakeEfcpt_EnvVar_GeneratesFakeOutput
- [ ] RunEfcpt_TestDacpac_EnvVar_ForwardsToProcess
- [ ] RunEfcpt_CreateDirectories_WorkingAndOutput
- [ ] RunEfcpt_TemplateOverrides_PassedToProcess

**Estimated Time:** 10 hours  
**File:** `tests/JD.Efcpt.Build.Tests/RunEfcptTests.cs` (EXPAND)

---

### Decorator Attributes (Currently: 50% → Target: 100%)
- [ ] ProfileInputAttribute_DefaultValues_CorrectlySet
- [ ] ProfileInputAttribute_WithExclude_SetsExcludeTrue
- [ ] ProfileInputAttribute_WithCustomName_UsesName
- [ ] ProfileOutputAttribute_InstantiatesCorrectly
- [ ] ProfileOutputAttribute_CanBeAppliedToProperty

**Estimated Time:** 2 hours  
**File:** `tests/JD.Efcpt.Build.Tests/Decorators/ProfileAttributeTests.cs` (NEW)

---

## Phase 2: Branch Coverage (Week 2) - Target: 85% Branch Coverage

### Focus Areas
- [ ] All catch blocks have exception tests
- [ ] All if/else branches covered
- [ ] All switch statements exhaustive
- [ ] All ternary operators tested both ways
- [ ] All early returns tested
- [ ] All null/empty checks tested
- [ ] All loop edge cases (empty, single, multiple)

### Specific Gaps to Address
- [ ] BuildLog verbosity filtering (3 untested branches)
- [ ] TaskExecutionContext profiling branches (4 untested)
- [ ] DotNetToolUtilities framework detection (6 untested)
- [ ] JsonTimeSpanConverter error handling (3 untested)
- [ ] RegEx-generated code branches (varies)

**Estimated Time:** 20 hours  
**Approach:** Systematic review of each file with coverage report

---

## Phase 3: Error Scenarios (Week 3) - Target: 100% Error Path Coverage

### File System Errors
- [ ] FileNotFoundException when reading configuration
- [ ] DirectoryNotFoundException when creating output
- [ ] UnauthorizedAccessException when writing files
- [ ] PathTooLongException for deep directories
- [ ] ArgumentException for invalid paths
- [ ] IOException when file locked
- [ ] IOException when disk full

**File:** `tests/JD.Efcpt.Build.Tests/ErrorScenarios/FileSystemErrorTests.cs` (NEW)  
**Time:** 6 hours

---

### Network Errors
- [ ] HttpRequestException on NuGet API failure
- [ ] SqlException on connection refused
- [ ] SqlException on connection timeout
- [ ] SqlException on authentication failure
- [ ] SslHandshakeException on certificate error

**File:** `tests/JD.Efcpt.Build.Tests/ErrorScenarios/NetworkErrorTests.cs` (NEW)  
**Time:** 6 hours

---

### Process Execution Errors
- [ ] Win32Exception when process not found
- [ ] UnauthorizedAccessException when permissions insufficient
- [ ] InvalidOperationException when process crashed
- [ ] TimeoutException when process hung
- [ ] OutOfMemoryException when output too large

**File:** `tests/JD.Efcpt.Build.Tests/ErrorScenarios/ProcessErrorTests.cs` (NEW)  
**Time:** 6 hours

---

### Configuration Errors
- [ ] JsonException on malformed JSON
- [ ] KeyNotFoundException on missing required property
- [ ] ArgumentOutOfRangeException on invalid values
- [ ] InvalidOperationException on conflicting properties
- [ ] Forward compatibility with unknown properties

**File:** `tests/JD.Efcpt.Build.Tests/ErrorScenarios/ConfigurationErrorTests.cs` (NEW)  
**Time:** 6 hours

---

### MSBuild Errors
- [ ] MSBuildException on target not found
- [ ] MSBuildException on property not set
- [ ] MSBuildException on item not defined
- [ ] MSBuildException on multiple projects ambiguity
- [ ] MSBuildException on circular dependency

**File:** `tests/JD.Efcpt.Sdk.IntegrationTests/ErrorScenarios/MSBuildErrorTests.cs` (NEW)  
**Time:** 8 hours

---

## Phase 4: Integration Tests (Week 4) - Target: Full E2E Coverage

### SQL Generation Error Scenarios
- [ ] SqlGeneration_ConnectionStringInvalid_ReturnsError
- [ ] SqlGeneration_InvalidCredentials_ReturnsError
- [ ] SqlGeneration_DatabaseUnreachable_ReturnsError
- [ ] SqlGeneration_SchemaValidationError_ReturnsError
- [ ] SqlGeneration_TableNameWithSpecialChars_HandlesCorrectly
- [ ] SqlGeneration_ColumnNameWithReservedWord_HandlesCorrectly
- [ ] SqlGeneration_EmptyDatabase_GeneratesMinimalOutput
- [ ] SqlGeneration_LargeDatabase_CompletesInReasonableTime

**File:** `tests/JD.Efcpt.Sdk.IntegrationTests/SqlGenerationErrorTests.cs` (NEW)  
**Time:** 12 hours (requires Testcontainers setup)

---

### DACPAC Build Failures
- [ ] DacpacBuild_SqlProjNotFound_ReturnsError
- [ ] DacpacBuild_SqlProjCompileError_ReturnsError
- [ ] DacpacBuild_CircularReference_ReturnsError
- [ ] DacpacBuild_VersionIncompat_ReturnsError

**File:** `tests/JD.Efcpt.Sdk.IntegrationTests/DacpacBuildErrorTests.cs` (NEW)  
**Time:** 8 hours

---

### Tool Resolution Edge Cases
- [ ] ToolResolution_NoPath_InCI_UsesGlobal
- [ ] ToolResolution_CorruptManifest_FallsBackToGlobal
- [ ] ToolResolution_NuGetDown_UsesCached
- [ ] ToolResolution_PermissionDenied_ReturnsError

**File:** `tests/JD.Efcpt.Sdk.IntegrationTests/ToolResolutionEdgeCaseTests.cs` (NEW)  
**Time:** 6 hours

---

### Concurrent Builds
- [ ] ConcurrentBuilds_SameProject_NoConflict
- [ ] ConcurrentBuilds_SharedOutputDir_Isolated
- [ ] ConcurrentBuilds_FingerprintCaching_ThreadSafe

**File:** `tests/JD.Efcpt.Sdk.IntegrationTests/ConcurrentBuildTests.cs` (NEW)  
**Time:** 8 hours

---

### Performance Tests
- [ ] Performance_LargeDatabase_1000Tables_Under5Minutes
- [ ] Performance_ComplexSchema_DeepNesting_NoStackOverflow

**File:** `tests/JD.Efcpt.Sdk.IntegrationTests/PerformanceTests.cs` (NEW)  
**Time:** 6 hours

---

## Summary

### Time Estimates
- **Phase 1:** 36 hours (1 week with 1 dev)
- **Phase 2:** 20 hours (0.5 week)
- **Phase 3:** 32 hours (1 week)  
- **Phase 4:** 40 hours (1 week)
- **Total:** 128 hours (~4 weeks for 1 developer)

### New Test Files to Create
1. `DetectSqlProjectTests.cs` (8 tests)
2. `RunSqlPackageTests.cs` (22 tests)
3. `ProfileAttributeTests.cs` (5 tests)
4. `FileSystemErrorTests.cs` (7 tests)
5. `NetworkErrorTests.cs` (5 tests)
6. `ProcessErrorTests.cs` (5 tests)
7. `ConfigurationErrorTests.cs` (5 tests)
8. `MSBuildErrorTests.cs` (5 tests)
9. `SqlGenerationErrorTests.cs` (8 tests)
10. `DacpacBuildErrorTests.cs` (4 tests)
11. `ToolResolutionEdgeCaseTests.cs` (4 tests)
12. `ConcurrentBuildTests.cs` (3 tests)
13. `PerformanceTests.cs` (2 tests)

### Existing Files to Expand
1. `CheckSdkVersionTests.cs` (+13 tests)
2. `RunEfcptTests.cs` (+14 tests)

---

## Progress Tracking

### Phase 1: ⬜ 0% Complete (0/57 tests)
### Phase 2: ⬜ 0% Complete
### Phase 3: ⬜ 0% Complete (0/27 tests)
### Phase 4: ⬜ 0% Complete (0/19 tests)

**Overall Progress: 0/103+ tests implemented**

---

## CI Integration Checklist

- [ ] Add coverage reporting to GitHub Actions
- [ ] Configure coverage badge
- [ ] Set up Codecov integration
- [ ] Enforce 95% line coverage threshold
- [ ] Enforce 90% branch coverage threshold
- [ ] Add coverage check to PR workflow
- [ ] Set up daily coverage reports
- [ ] Add pre-commit hook for local coverage check

---

## Notes

- Use `[Theory]` with `[InlineData]` for parameterized tests where appropriate
- Mock file system with `System.IO.Abstractions` for testability
- Mock `HttpClient` with `Moq` or custom `HttpMessageHandler`
- Use `Testcontainers` for SQL Server integration tests
- Consider property-based testing with `FsCheck` for complex logic
- Add mutation testing with `Stryker` to verify test quality

---

**Last Updated:** 2026-01-22  
**Next Review:** Start of each week
