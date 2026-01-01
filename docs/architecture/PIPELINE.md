# Build Pipeline Architecture

**Document Version:** 1.0
**Last Updated:** December 2024

---

## Overview

JD.Efcpt.Build implements a sophisticated MSBuild-integrated pipeline that automatically generates Entity Framework Core models from database schemas during the build process. The pipeline is designed to be deterministic, incremental, and cache-friendly.

## Pipeline Phases

The build pipeline executes in several distinct phases, each implemented as an MSBuild task:

```
┌──────────────────────────────────────────────────────────────────────┐
│                         MSBuild Integration                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌────────────────┐    ┌──────────────────┐    ┌────────────────┐  │
│  │   CheckSdk     │───▶│  Resolve Inputs  │───▶│ EnsureDacpac   │  │
│  │   Version      │    │  & SQL Project   │    │     Built      │  │
│  └────────────────┘    └──────────────────┘    └────────────────┘  │
│          │                      │                        │          │
│          └──────────────────────┴────────────────────────┘          │
│                                 │                                    │
│                                 ▼                                    │
│                    ┌────────────────────────┐                        │
│                    │  ComputeFingerprint    │                        │
│                    │   (Change Detection)   │                        │
│                    └────────────────────────┘                        │
│                                 │                                    │
│                    ┌────────────▼──────────┐                         │
│                    │ Fingerprint Changed?  │                         │
│                    └────────────┬──────────┘                         │
│                           No    │    Yes                             │
│                    ┌────────────▼──────────┐                         │
│                    │   Skip Generation     │                         │
│                    └───────────────────────┘                         │
│                                 │                                    │
│                                Yes                                   │
│                                 │                                    │
│                    ┌────────────▼──────────┐                         │
│                    │    RunEfcpt (dnx/     │                         │
│                    │   dotnet tool run)    │                         │
│                    └────────────┬──────────┘                         │
│                                 │                                    │
│                    ┌────────────▼──────────┐                         │
│                    │  RenameGenerated      │                         │
│                    │  Files (.g.cs)        │                         │
│                    └────────────┬──────────┘                         │
│                                 │                                    │
│                    ┌────────────▼──────────┐                         │
│                    │   SplitOutputs        │                         │
│                    │  (ItemGroup)          │                         │
│                    └────────────┬──────────┘                         │
│                                 │                                    │
│                    ┌────────────▼──────────┐                         │
│                    │  SerializeConfig      │                         │
│                    │   Properties          │                         │
│                    └───────────────────────┘                         │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

## Phase Details

### 1. SDK Version Check (`CheckSdkVersion`)

**Purpose:** Validates that the JD.Efcpt.Build package version matches expectations.

**Inputs:**
- `PackageVersion` - The current package version
- `ExpectedSdkVersion` - The expected SDK version (optional)

**Outputs:**
- `SdkVersionCheckPassed` - Boolean indicating if check passed

**Behavior:**
- If `ExpectedSdkVersion` is not specified, check always passes
- Logs a warning (not error) if versions don't match
- This is a non-breaking check to help identify version mismatches

### 2. Input Resolution (`ResolveSqlProjAndInputs`)

**Purpose:** Resolves the DACPAC file, configuration files, and connection string based on the project's configuration.

**Resolution Strategy:**

The task follows a multi-tier resolution chain for each input type:

#### DACPAC Resolution

1. **Explicit DacpacPath** - If `EfcptDacpacPath` is set, use it directly
2. **SQL Project Reference** - If `EfcptSqlProjectPath` is set, locate the `.dacpac` in its output directory
3. **Auto-Discovery** - Search for `.sqlproj` files in:
   - Same directory as the .csproj
   - Parent directories (up to solution root)
   - Adjacent directories

#### Configuration File Resolution

1. **Explicit Path** - If `EfcptConfigFilePath` is set, use it
2. **Convention-Based** - Search for `efcpt.json` in:
   - Project directory
   - Solution directory
   - `.efcpt/` subdirectories

#### Connection String Resolution

Supports multiple input sources:

1. **Direct Connection String** - `EfcptConnectionString` property
2. **appsettings.json** - Reads `ConnectionStrings:DefaultConnection` or `ConnectionStrings:Default`
3. **app.config** (Framework projects) - Reads from `<connectionStrings>` section
4. **User Secrets** (.NET Core+) - Reads from user secrets if configured

**Outputs:**
- `ResolvedDacpacPath` - Absolute path to the DACPAC file
- `ResolvedConfigPath` - Absolute path to the efcpt.json file (if found)
- `ResolvedConnectionString` - Connection string for database access (if using connection string mode)
- `ResolvedSqlProjectPath` - Path to the .sqlproj file (if found)

### 3. DACPAC Build Verification (`EnsureDacpacBuilt`)

**Purpose:** Ensures that if a SQL project is referenced, its DACPAC is built and up-to-date.

**Inputs:**
- `SqlProjectPath` - Path to the .sqlproj file
- `ExpectedDacpacPath` - Where the DACPAC should be

**Behavior:**
- Checks if DACPAC exists at the expected location
- Compares timestamps of .sqlproj and .dacpac
- Logs a warning if DACPAC is missing or stale
- Does NOT automatically build the SQL project (respects build orchestration)

### 4. Fingerprint Computation (`ComputeFingerprint`)

**Purpose:** Computes a deterministic hash representing all inputs to the code generation process.

**Components of the Fingerprint:**

The fingerprint is a XXH64 hash of:

1. **DACPAC Content**
   - Full binary content of the .dacpac file
   - Includes schema definitions, table structures, columns, indexes

2. **Configuration File**
   - Content of efcpt.json (if present)
   - Includes all override settings

3. **Template Files**
   - Content of custom T4 templates (if used)
   - Includes EntityType.t4, DbContext.t4, etc.

4. **Tool Version**
   - Version of the efcpt CLI tool being used
   - Ensures regeneration when tool is updated

5. **Connection String Schema Fingerprint** (when using connection string mode)
   - Schema metadata from the live database
   - Includes table names, column definitions, indexes
   - Normalized to ensure deterministic ordering

**Output:**
- `GeneratedFingerprint` - 16-character hexadecimal hash

**Algorithm:**
```csharp
fingerprint = XXH64(
    File.ReadAllBytes(dacpacPath) +
    File.ReadAllBytes(configPath) +
    Directory.GetFiles(templateDir)
        .OrderBy(f => f)
        .SelectMany(f => File.ReadAllBytes(f)) +
    Encoding.UTF8.GetBytes(toolVersion) +
    Encoding.UTF8.GetBytes(schemaFingerprint)
)
```

### 5. Incremental Build Check

**Purpose:** Compares the computed fingerprint against the last successful build to determine if regeneration is needed.

**Fingerprint Storage:**
- Stored in `$(IntermediateOutputPath).efcpt/fingerprint.txt`
- Plain text file containing the hex fingerprint
- Persisted across builds for comparison

**Decision Logic:**
```
if fingerprint.txt exists AND
   contents match GeneratedFingerprint:
    Skip code generation (use cached files)
else:
    Proceed with code generation
    Write new fingerprint.txt
```

### 6. Code Generation (`RunEfcpt`)

**Purpose:** Invokes the Entity Framework Core Power Tools CLI to generate model files.

**Tool Resolution Strategy:**

The task supports multiple modes for running the efcpt tool:

#### 1. Explicit Tool Path Mode

```xml
<EfcptToolPath>/path/to/efcpt.exe</EfcptToolPath>
```

Directly executes the specified executable.

#### 2. DNX Mode (.NET 10+)

For projects targeting .NET 10.0 or later:

```bash
dnx EFCorePowerTools.Cli
```

- Automatically used when:
  - Target framework is `net10.0` or later
  - .NET 10 SDK is installed
  - `dnx` command is available
- Benefits:
  - No tool installation required
  - Uses SDK-provided tool execution
  - Faster startup

#### 3. Local Tool Manifest Mode

```bash
dotnet tool run efcpt
```

- Used when `.config/dotnet-tools.json` is found
- Searches parent directories for the manifest
- Automatically runs `dotnet tool restore` if `EfcptToolRestore=true`

#### 4. Global Tool Mode

```bash
dotnet tool update --global EFCorePowerTools.Cli
efcpt
```

- Fallback mode when no manifest is found
- Updates/installs the global tool if `EfcptToolRestore=true`
- Executes the global `efcpt` command

**Execution:**

```bash
efcpt reverse-engineer \
    --dacpac /path/to/project.dacpac \
    --config /path/to/efcpt.json \
    --output-dir /path/to/generated \
    --namespace MyProject.Models
```

**Environment Variables:**
- `EFCPT_TEST_DACPAC` - Forwarded from MSBuild environment (for testing)

### 7. File Renaming (`RenameGeneratedFiles`)

**Purpose:** Renames generated files with `.g.cs` extension to clearly mark them as generated code.

**Pattern:**
```
Product.cs         → Product.g.cs
Customer.cs        → Customer.g.cs
NorthwindContext.cs → NorthwindContext.g.cs
```

**Rationale:**
- Clear visual indicator of generated code
- Follows .NET convention (similar to `.g.i.cs` for XAML)
- Enables `.gitignore` patterns like `*.g.cs`
- IDE integration (some IDEs treat `.g.cs` specially)

### 8. Output Categorization (`SplitOutputs`)

**Purpose:** Categorizes generated files into MSBuild item groups for proper compiler integration.

**Output Item Groups:**

```xml
<ItemGroup>
  <!-- Regular entity classes -->
  <Compile Include="Generated\*.g.cs"
           Exclude="Generated\*Context.g.cs" />

  <!-- DbContext classes (treated specially) -->
  <EfcptDbContextFile Include="Generated\*Context.g.cs" />
</ItemGroup>
```

**Why Separate DbContext?**
- Enables conditional inclusion
- Supports custom compilation settings
- Allows for different code analysis rules

### 9. Configuration Serialization (`SerializeConfigProperties`)

**Purpose:** Serializes MSBuild properties into a JSON file for consumption by the efcpt tool.

**Generated File:** `$(IntermediateOutputPath).efcpt/build-properties.json`

**Content:**
```json
{
  "ProjectDir": "/path/to/project",
  "IntermediateOutputPath": "obj/Debug/net8.0/",
  "TargetFramework": "net8.0",
  "RootNamespace": "MyProject",
  "AssemblyName": "MyProject",
  "Configuration": "Debug"
}
```

## Incremental Build Behavior

### When Code IS Regenerated

Code generation occurs when:

1. **DACPAC changes** - Schema modifications detected
2. **Configuration changes** - efcpt.json modified
3. **Template changes** - Custom T4 templates updated
4. **Tool version changes** - efcpt CLI updated
5. **First build** - No previous fingerprint exists
6. **Clean build** - Intermediate output cleaned
7. **Connection string schema changes** - Live database schema modified

### When Code is NOT Regenerated

Code generation is skipped when:

1. **Fingerprint matches** - All inputs unchanged since last build
2. **Rebuild without changes** - Manual rebuild with identical inputs

### Benefits of Incremental Builds

- **Faster builds** - Skips expensive schema analysis and code generation
- **Better caching** - Works with MSBuild's incremental build system
- **CI/CD friendly** - Deterministic, cacheable outputs
- **Developer experience** - Quick iteration when models unchanged

## Integration with MSBuild

### Target Ordering

The pipeline integrates into MSBuild's standard target graph:

```
BeforeBuild
    ↓
CheckSdkVersion
    ↓
ResolveSqlProjAndInputs
    ↓
EnsureDacpacBuilt
    ↓
ComputeFingerprint
    ↓
StageEfcptInputs
    ↓
RunEfcpt
    ↓
RenameGeneratedFiles
    ↓
SplitOutputs
    ↓
SerializeConfigProperties
    ↓
CoreCompile (standard MSBuild)
```

### Dependency Management

The pipeline properly declares dependencies:

```xml
<Target Name="EfcptGenerateModels"
        BeforeTargets="CoreCompile"
        DependsOnTargets="ComputeFingerprint;RunEfcpt"
        Inputs="$(ResolvedDacpacPath);$(ResolvedConfigPath)"
        Outputs="$(IntermediateOutputPath).efcpt\fingerprint.txt">
```

**MSBuild Optimization:**
- `Inputs` and `Outputs` attributes enable MSBuild's own incremental logic
- Complements the fingerprint-based approach
- Ensures proper build ordering

## Configuration Override System

The pipeline supports a sophisticated override system:

### Application Point

Configuration overrides are applied:

1. **After** efcpt generates the base configuration
2. **Before** code generation executes

### Override Sources

```json
{
  "Overrides": {
    "Names": {
      "DbContext": "CustomContext",
      "Namespace": "Custom.Namespace"
    },
    "FileLayout": {
      "OutputPath": "Generated/Models",
      "SplitDbContext": true
    },
    "Preferences": {
      "UseDataAnnotations": false,
      "UseDatabaseNames": true
    }
  }
}
```

### Application Strategy

The `ApplyConfigOverrides` task:

1. Reads base efcpt.json configuration
2. Merges with `Overrides` section
3. Writes updated configuration
4. efcpt tool reads the updated configuration

## Error Handling

### Failure Points and Recovery

| Phase | Failure Scenario | Behavior |
|-------|-----------------|----------|
| SDK Check | Version mismatch | ⚠️ Warning, continues |
| Input Resolution | Missing DACPAC | ❌ Error, build fails |
| DACPAC Verification | Stale DACPAC | ⚠️ Warning, continues |
| Fingerprint | I/O error | ❌ Error, build fails |
| Code Generation | efcpt.exe fails | ❌ Error, build fails |
| File Renaming | Permission denied | ❌ Error, build fails |

### Diagnostic Output

Enable verbose logging:

```bash
dotnet build /v:detailed
```

Look for:
- `[Efcpt]` log messages
- Fingerprint computation details
- Tool resolution steps
- Configuration application logs

## Performance Characteristics

### Typical Build Times

| Scenario | Time | Notes |
|----------|------|-------|
| Incremental (no changes) | ~100ms | Fingerprint check only |
| Incremental (schema change) | ~2-5s | Full regeneration |
| Clean build | ~2-5s | Full regeneration |
| First build | ~3-6s | Tool resolution + generation |

### Optimization Strategies

1. **Use DACPAC mode** - Faster than connection string mode
2. **Minimize template customization** - Reduces fingerprint surface
3. **Cache tool installations** - Use local tool manifest
4. **Leverage incremental builds** - Don't clean unnecessarily

## See Also

- [Fingerprinting Deep Dive](FINGERPRINTING.md)
- [Multi-Targeting Explained](MULTI-TARGETING.md)
- [Troubleshooting Guide](../user-guide/troubleshooting.md)
- [CI/CD Integration](../user-guide/ci-cd.md)
