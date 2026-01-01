# Change Detection & Fingerprinting

**Document Version:** 1.0
**Last Updated:** December 2024

---

## Overview

JD.Efcpt.Build uses a sophisticated fingerprinting system to detect when database schemas or configuration have changed, enabling intelligent incremental builds. This document explains how fingerprinting works, why it matters, and how to troubleshoot fingerprint-related issues.

## Why Fingerprinting?

### The Problem

Code generation is expensive:

- **DACPAC parsing** - Reading and analyzing database schema (1-2 seconds)
- **Schema reading** - Querying live databases for metadata (1-3 seconds)
- **Code generation** - Running efcpt and generating C# files (1-2 seconds)
- **File I/O** - Writing dozens of entity class files (0.5-1 second)

For a medium-sized database (50-100 tables), code generation takes 3-6 seconds. Running this on every build slows development and CI/CD pipelines.

### The Solution

**Fingerprinting enables intelligent skipping:**

```
Build 1: Schema changed → Fingerprint: ABC123 → Generate code
Build 2: No changes    → Fingerprint: ABC123 → Skip generation (0.1s)
Build 3: Config changed→ Fingerprint: DEF456 → Generate code
```

**Benefits:**
- ⚡ **90%+ faster** incremental builds
- 🎯 **Deterministic** - Same inputs always produce same outputs
- 🔄 **Cache-friendly** - Works with build servers and local caches
- 🐛 **Debuggable** - Clear indicator of what changed

## Fingerprint Components

A fingerprint is a 16-character hexadecimal hash (XXH64) computed from:

### 1. DACPAC Content (DACPAC Mode)

```csharp
byte[] dacpacBytes = File.ReadAllBytes(dacpacPath);
```

**What's Included:**
- Complete binary content of the .dacpac file
- All schema definitions (tables, views, procedures)
- Column definitions (names, types, constraints)
- Index definitions
- Foreign key relationships

**Why the Entire File:**
- DACPAC is already a compact binary format
- Partial hashing would miss schema changes
- Full content hash ensures 100% accuracy

**Typical Size:** 50KB - 5MB

### 2. Database Schema (Connection String Mode)

When using connection string mode instead of DACPAC:

```csharp
SchemaModel schema = schemaReader.ReadSchema(connectionString);
string schemaFingerprint = SchemaFingerprinter.ComputeFingerprint(schema);
```

**Schema Fingerprint Components:**

```
Fingerprint = Hash(
    "Table:dbo.Products|Columns:Id:int:NotNull:PK,Name:nvarchar(100):NotNull,Price:decimal(18,2):Null|Indexes:PK_Products:Clustered,IX_Name:NonClustered\n" +
    "Table:dbo.Categories|Columns:Id:int:NotNull:PK,Name:nvarchar(50):NotNull|Indexes:PK_Categories:Clustered\n" +
    ...
)
```

**Normalization Rules:**
- Tables sorted alphabetically by schema.name
- Columns sorted by ordinal position
- Indexes sorted by name
- Data type names normalized (varchar→nvarchar for consistency)
- Whitespace normalized

**Why Normalize:**
- Database providers return metadata in different orders
- Ensures deterministic fingerprints across runs
- PostgreSQL uses lowercase, SQL Server uses case-sensitive names

### 3. Configuration File

```csharp
if (File.Exists(configPath))
{
    byte[] configBytes = File.ReadAllBytes(configPath);
}
```

**What's Included:**
- Complete content of efcpt-config.json
- All override sections
- Formatting and whitespace (JSON content-based)

**Example Changes That Trigger Regeneration:**
```json
// Before
{
  "names": {
    "dbcontext-name": "NorthwindContext"
  }
}

// After - changes fingerprint
{
  "names": {
    "dbcontext-name": "NorthwindDbContext"  // ← Different name
  }
}
```

### 4. Custom Templates

When using custom T4 templates:

```csharp
string templateDir = Path.Combine(projectDir, "Templates");
if (Directory.Exists(templateDir))
{
    foreach (var file in Directory.GetFiles(templateDir, "*.t4").OrderBy(f => f))
    {
        byte[] templateBytes = File.ReadAllBytes(file);
    }
}
```

**Included Templates:**
- `EntityType.t4` - Entity class template
- `DbContext.t4` - DbContext template
- `Configuration.t4` - Entity configuration template

**Why Include Templates:**
- Template changes should regenerate all entities
- Ensures consistency between template and generated code
- Detects customization impacts

### 5. Tool Version

```csharp
string toolVersion = GetEfcptToolVersion();
// e.g., "8.0.0"
```

**Why Include Tool Version:**
- Different tool versions may generate different code
- Ensures regeneration after tool updates
- Prevents subtle bugs from version mismatches

**How It's Detected:**
- Reads from tool manifest (`.config/dotnet-tools.json`)
- Queries global tool installation
- Falls back to default version string

## Fingerprint Computation

### Algorithm

JD.Efcpt.Build uses **XXH64** (xxHash 64-bit):

```csharp
using (var hash = new XxHash64())
{
    // Add DACPAC content
    hash.Append(File.ReadAllBytes(dacpacPath));

    // Add configuration
    if (File.Exists(configPath))
        hash.Append(File.ReadAllBytes(configPath));

    // Add templates
    foreach (var template in templateFiles)
        hash.Append(File.ReadAllBytes(template));

    // Add tool version
    hash.Append(Encoding.UTF8.GetBytes(toolVersion));

    // Get final hash
    ulong hashValue = hash.GetCurrentHashAsUInt64();
    string fingerprint = hashValue.ToString("X16"); // "0123456789ABCDEF"
}
```

### Why XXH64?

| Algorithm | Speed | Collision Resistance | Availability |
|-----------|-------|---------------------|--------------|
| MD5 | Medium | Low | Deprecated |
| SHA-256 | Slow | High | Overkill |
| XXH64 | **Very Fast** | **Sufficient** | ✅ .NET 8+ |
| XXH3 | Fastest | Sufficient | Future |

**Benefits of XXH64:**
- **Speed:** 10-20x faster than SHA-256
- **Low collision:** Sufficient for build cache
- **Deterministic:** Same input → same hash
- **Available:** Built into .NET via `System.IO.Hashing`

## Fingerprint Storage

### Location

```
$(ProjectDir)/obj/$(Configuration)/$(TargetFramework)/.efcpt/fingerprint.txt
```

**Example:**
```
/MyProject/obj/Debug/net8.0/.efcpt/fingerprint.txt
```

### Content

```
ABC123DEF456789
```

**Format:**
- Plain text file
- Single line
- 16 hexadecimal characters
- No whitespace, no newlines

### Lifecycle

```
┌─────────────────────────────────────────────┐
│  First Build                                │
├─────────────────────────────────────────────┤
│  1. No fingerprint.txt exists               │
│  2. Compute fingerprint: ABC123...          │
│  3. Generate code                           │
│  4. Write fingerprint.txt ← ABC123...       │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  Incremental Build (No Changes)             │
├─────────────────────────────────────────────┤
│  1. Read fingerprint.txt: ABC123...         │
│  2. Compute fingerprint: ABC123...          │
│  3. Compare: MATCH ✓                        │
│  4. Skip generation                         │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  Incremental Build (Schema Changed)         │
├─────────────────────────────────────────────┤
│  1. Read fingerprint.txt: ABC123...         │
│  2. Compute fingerprint: DEF456...          │
│  3. Compare: DIFFERENT ✗                    │
│  4. Generate code                           │
│  5. Write fingerprint.txt ← DEF456...       │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  Clean Build                                │
├─────────────────────────────────────────────┤
│  1. obj/ directory deleted                  │
│  2. No fingerprint.txt exists               │
│  3. Generate code                           │
│  4. Write fingerprint.txt                   │
└─────────────────────────────────────────────┘
```

## Change Detection Logic

### Comparison Algorithm

```csharp
public bool ShouldRegenerate()
{
    string fingerprintPath = Path.Combine(intermediateOutputPath, ".efcpt", "fingerprint.txt");

    // First build or after clean
    if (!File.Exists(fingerprintPath))
        return true;

    string currentFingerprint = ComputeFingerprint();
    string previousFingerprint = File.ReadAllText(fingerprintPath).Trim();

    // Case-sensitive comparison
    return currentFingerprint != previousFingerprint;
}
```

### Edge Cases

| Scenario | Behavior | Rationale |
|----------|----------|-----------|
| fingerprint.txt missing | Generate | First build or clean build |
| fingerprint.txt empty | Generate | Corrupted state, be safe |
| fingerprint.txt corrupted | Generate | Cannot trust, regenerate |
| DACPAC missing | Error | Cannot compute fingerprint |
| Config file deleted | Regenerate | Fingerprint changes |
| Whitespace-only change in config | Regenerate | JSON content changed |

## Troubleshooting

### Problem: Code Regenerates Every Build

**Symptoms:**
- Build takes 3-6 seconds even with no changes
- Logs show "Fingerprint changed, regenerating models"

**Diagnosis:**

1. **Enable verbose logging:**
   ```bash
   dotnet build /v:detailed > build.log
   ```

2. **Check fingerprint stability:**
   ```bash
   # Build twice without changes
   dotnet build
   dotnet build

   # Check if fingerprint changed
   cat obj/Debug/net8.0/.efcpt/fingerprint.txt
   ```

3. **Look for:**
   - "Computing fingerprint from..."
   - "Previous fingerprint: ..."
   - "Current fingerprint: ..."

**Common Causes:**

| Cause | Solution |
|-------|----------|
| Non-deterministic timestamp in DACPAC | Ensure SQL project has deterministic builds |
| Template files being modified | Check source control for template changes |
| Tool version changing | Pin tool version in `.config/dotnet-tools.json` |
| Schema normalization issue | Check for provider-specific column name casing |

### Problem: Changes Not Detected

**Symptoms:**
- Modified schema
- Build skips generation
- Old models still in use

**Diagnosis:**

```bash
# Check current fingerprint
cat obj/Debug/net8.0/.efcpt/fingerprint.txt

# Force regeneration by deleting fingerprint
rm obj/Debug/net8.0/.efcpt/fingerprint.txt

# Rebuild
dotnet build
```

**Common Causes:**

| Cause | Solution |
|-------|----------|
| DACPAC not rebuilt after schema change | Rebuild SQL project first |
| Connection string mode with cached schema | Clear database query cache |
| Fingerprint file permissions | Check file is writable |
| Custom build logic bypassing fingerprint | Review custom MSBuild targets |

### Problem: Fingerprint File Missing

**Symptoms:**
- Every build regenerates code
- `fingerprint.txt` doesn't exist after build

**Diagnosis:**

```bash
# Check intermediate output path
dotnet build /p:IntermediateOutputPath=obj/Debug/net8.0/

# Verify .efcpt directory creation
ls -la obj/Debug/net8.0/.efcpt/
```

**Common Causes:**

| Cause | Solution |
|-------|----------|
| Custom clean target deletes .efcpt/ | Exclude from clean |
| Permissions issue | Check write permissions on obj/ |
| MSBuild incremental build disabled | Enable incremental builds |

## Advanced Scenarios

### Multi-Project Solutions

**Challenge:** Multiple projects share a DACPAC

```
Solution/
  ├── Database.sqlproj → Database.dacpac
  ├── Project1/ → References Database.dacpac
  └── Project2/ → References Database.dacpac
```

**Fingerprint Behavior:**
- Each project computes its own fingerprint
- Both use the same DACPAC content
- Both fingerprints include project-specific configuration

**Result:**
- DACPAC change triggers regeneration in both projects
- Project1 config change only affects Project1

### Custom Fingerprint Extensions

**Use Case:** Include additional files in fingerprint

```xml
<Target Name="AddCustomFingerprintInputs" BeforeTargets="ComputeFingerprint">
  <ItemGroup>
    <EfcptFingerprintInput Include="custom-mapping.json" />
    <EfcptFingerprintInput Include="schema-overrides.xml" />
  </ItemGroup>
</Target>
```

**Effect:**
- Changes to these files trigger regeneration
- Fingerprint includes their content

### Parallel Builds

**Scenario:** Building multiple configurations in parallel

```bash
dotnet build -c Debug &
dotnet build -c Release &
```

**Fingerprint Isolation:**
- Each configuration has separate `obj/` directory
- Each has independent `fingerprint.txt`
- No collision or race conditions

**Location:**
```
obj/Debug/net8.0/.efcpt/fingerprint.txt
obj/Release/net8.0/.efcpt/fingerprint.txt
```

## Performance Impact

### Fingerprint Computation Cost

| Component | Time | Notes |
|-----------|------|-------|
| Read DACPAC | 10-50ms | Depends on file size (50KB-5MB) |
| Hash computation | 5-20ms | XXH64 is very fast |
| Read config | 1-2ms | Small JSON file |
| Read templates | 2-5ms | Few small .t4 files |
| **Total** | **~20-80ms** | Negligible vs. 3-6s generation |

### Comparison vs. Generation

```
Fingerprint check:  20-80ms   (0.02-0.08s)
Code generation:    3,000-6,000ms (3-6s)

Speedup: 37x - 300x faster
```

## Best Practices

### 1. Keep DACPAC Builds Deterministic

**Problem:** Non-deterministic builds produce different DACPACs with identical schemas

```xml
<!-- In .sqlproj -->
<PropertyGroup>
  <!-- Ensure deterministic builds -->
  <DeterministicSourcePaths>true</DeterministicSourcePaths>
  <Deterministic>true</Deterministic>
</PropertyGroup>
```

### 2. Version Lock Your Tools

```json
// .config/dotnet-tools.json
{
  "tools": {
    "efcorepowertools.cli": {
      "version": "8.0.0",  // ← Pin specific version
      "commands": ["efcpt"]
    }
  }
}
```

### 3. Don't Manually Modify fingerprint.txt

**Never:**
```bash
# ❌ Don't do this
echo "FAKE123" > obj/Debug/net8.0/.efcpt/fingerprint.txt
```

**Reason:**
- Build system expects valid fingerprints
- Manually modified fingerprints cause false cache hits
- Can lead to using stale generated code

### 4. Clean Builds When Troubleshooting

```bash
# Full clean rebuild
dotnet clean
dotnet build
```

**When to Clean:**
- Fingerprint issues suspected
- After upgrading tools
- After major schema changes
- CI/CD pipeline failures

## See Also

- [Build Pipeline Architecture](PIPELINE.md)
- [Troubleshooting Guide](../user-guide/troubleshooting.md)
- [CI/CD Integration Patterns](../user-guide/use-cases/ci-cd-patterns.md)
