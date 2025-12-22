# Core Concepts

This article explains the architecture and key concepts of JD.Efcpt.Build.

## Architecture Overview

JD.Efcpt.Build integrates into MSBuild by defining custom targets and tasks that run during the build process. The package consists of two main components:

1. **JD.Efcpt.Build** - The NuGet package containing MSBuild targets and default configuration files
2. **JD.Efcpt.Build.Tasks** - The .NET assembly containing MSBuild task implementations

When you add the package to your project, it hooks into the build pipeline and executes a series of stages to generate EF Core models.

## The Build Pipeline

The pipeline consists of six stages that run before C# compilation:

### Stage 1: EfcptResolveInputs

**Purpose**: Discover the database source and locate all configuration files.

**What it does**:
- Locates the SQL Server Database Project (.sqlproj) from project references or explicit configuration
- Resolves the EF Core Power Tools configuration file (`efcpt-config.json`)
- Finds renaming rules (`efcpt.renaming.json`)
- Discovers T4 template directories
- Resolves connection strings from various sources (explicit property, appsettings.json, app.config)

**Outputs**:
- `SqlProjPath` - Path to the discovered database project
- `ResolvedConfigPath` - Path to the configuration file
- `ResolvedRenamingPath` - Path to renaming rules
- `ResolvedTemplateDir` - Path to templates
- `ResolvedConnectionString` - Connection string (if using connection string mode)

### Stage 2: EfcptEnsureDacpac / EfcptQuerySchemaMetadata

**Purpose**: Prepare the schema source for code generation.

**DACPAC Mode** (when using .sqlproj):
- Builds the SQL Server Database Project to produce a DACPAC file
- Only rebuilds if source files are newer than the existing DACPAC
- Uses `msbuild.exe` on Windows or `dotnet msbuild` on other platforms

**Connection String Mode** (when using a live database):
- Connects to the database and queries system tables
- Extracts table, column, index, and constraint metadata
- Builds a canonical schema model for fingerprinting

**Outputs**:
- `DacpacPath` - Path to the DACPAC file (DACPAC mode)
- `SchemaFingerprint` - Hash of the database schema (connection string mode)

### Stage 3: EfcptStageInputs

**Purpose**: Copy all inputs to a stable intermediate directory.

**What it does**:
- Copies configuration files to `obj/efcpt/`
- Stages T4 templates to `obj/efcpt/Generated/CodeTemplates/`
- Normalizes paths for consistent fingerprinting

**Outputs**:
- `StagedConfigPath` - Path to staged configuration
- `StagedRenamingPath` - Path to staged renaming rules
- `StagedTemplateDir` - Path to staged templates

### Stage 4: EfcptComputeFingerprint

**Purpose**: Detect whether code regeneration is needed.

**What it does**:
- Computes an XxHash64 (fast, non-cryptographic) hash of:
  - The DACPAC file contents (or schema fingerprint)
  - The staged configuration file
  - The staged renaming file
  - All files in the staged template directory
- Compares with the previous fingerprint stored in `obj/efcpt/fingerprint.txt`

**Outputs**:
- `Fingerprint` - The computed XxHash64 hash
- `HasChanged` - Boolean indicating whether regeneration is needed

### Stage 5: EfcptGenerateModels

**Purpose**: Run the EF Core Power Tools CLI to generate code.

**What it does** (only if `HasChanged` is true):
- Locates the `efcpt` CLI using the configured tool mode
- Executes `efcpt` with the DACPAC or connection string
- Generates DbContext and entity classes
- Renames generated files from `.cs` to `.g.cs`
- Updates the fingerprint file

**Tool Resolution Strategies**:
1. **dnx** (.NET 10+) - Executes via `dotnet run` without installation
2. **tool-manifest** - Uses local tool manifest (`.config/dotnet-tools.json`)
3. **global** - Uses globally installed tool
4. **explicit** - Uses path specified in `EfcptToolPath`

### Stage 6: EfcptAddToCompile

**Purpose**: Include generated files in compilation.

**What it does**:
- Adds all `.g.cs` files from `obj/efcpt/Generated/` to the `Compile` item group
- Ensures generated code is compiled into your assembly

## Fingerprinting

Fingerprinting is a key optimization that prevents unnecessary code regeneration. The system creates a unique hash based on all inputs that affect code generation.

### What's Included in the Fingerprint

- **DACPAC content** (in .sqlproj mode) or **schema metadata** (in connection string mode)
- **efcpt-config.json** - Generation options, namespaces, table selection
- **efcpt.renaming.json** - Custom naming rules
- **T4 templates** - All template files and their contents

All hashing uses XxHash64, a fast non-cryptographic hash algorithm.

### How Fingerprinting Works

```
Build 1 (first run):
  Fingerprint = Hash(DACPAC/Schema + config + renaming + templates)
  → No previous fingerprint exists
  → Generate models
  → Store fingerprint

Build 2 (no changes):
  Fingerprint = Hash(DACPAC/Schema + config + renaming + templates)
  → Same as stored fingerprint
  → Skip generation (fast build)

Build 3 (schema changed):
  Fingerprint = Hash(new DACPAC/Schema + config + renaming + templates)
  → Different from stored fingerprint
  → Regenerate models
  → Store new fingerprint
```

### Forcing Regeneration

To force regeneration regardless of fingerprint:

```bash
# Delete the intermediate directory
rmdir /s /q obj\efcpt

# Rebuild
dotnet build
```

## Input Resolution

The package uses a multi-tier resolution strategy to find configuration files and database sources.

### Resolution Priority

For each input type, the package searches in this order:

1. **Explicit MSBuild property** - Highest priority
2. **Project directory** - Files in the consuming project
3. **Solution directory** - Files at the solution root
4. **Package defaults** - Sensible defaults shipped with the package

### Example: Configuration File Resolution

```
1. <EfcptConfig>custom-config.json</EfcptConfig>  → Use specified path
2. {ProjectDir}/efcpt-config.json                  → Use if exists
3. {SolutionDir}/efcpt-config.json                 → Use if exists
4. {PackageDir}/defaults/efcpt-config.json         → Use package default
```

### SQL Project Discovery

The package discovers .sqlproj files by:

1. Checking `EfcptSqlProj` property (if set)
2. Scanning `ProjectReference` items for .sqlproj files
3. Looking for .sqlproj in the solution directory
4. Checking for modern SQL SDK projects (projects using `Microsoft.Build.Sql` SDK)

## Generated File Naming

Generated files use the `.g.cs` suffix by convention:

- `ApplicationDbContext.g.cs` - The generated DbContext
- `User.g.cs` - Entity class for the Users table
- `Order.g.cs` - Entity class for the Orders table

This convention:
- Clearly identifies generated files
- Prevents conflicts with hand-written code
- Makes .gitignore patterns easy (`*.g.cs`)
- Allows IDE tooling to recognize generated code

## Schema-Based Organization

When `use-schema-folders-preview` is enabled, generated files are organized by database schema:

```
obj/efcpt/Generated/
├── ApplicationDbContext.g.cs
└── Models/
    ├── dbo/
    │   ├── User.g.cs
    │   └── Order.g.cs
    ├── sales/
    │   └── Customer.g.cs
    └── audit/
        └── Log.g.cs
```

With `use-schema-namespaces-preview`, entities also get schema-based namespaces:

```csharp
namespace YourApp.Data.Entities.Dbo
{
    public class User { ... }
}

namespace YourApp.Data.Entities.Sales
{
    public class Customer { ... }
}
```

## Tool Execution Modes

The `RunEfcpt` task supports multiple ways to locate and execute the EF Core Power Tools CLI:

### dnx Mode (.NET 10+)

On .NET 10 and later, the tool is executed via `dotnet run` without requiring installation:

```bash
dotnet run --package ErikEJ.EFCorePowerTools.Cli --version 10.* -- efcpt ...
```

This is the default mode on .NET 10+ and requires no setup.

### Tool Manifest Mode

Uses a local tool manifest (`.config/dotnet-tools.json`):

```bash
dotnet tool run efcpt ...
```

Enable with:
```xml
<EfcptToolMode>tool-manifest</EfcptToolMode>
```

### Global Tool Mode

Uses a globally installed tool:

```bash
efcpt ...
```

This is the default mode on .NET 8 and 9.

### Explicit Path Mode

Specify an exact path to the executable:

```xml
<EfcptToolPath>C:\tools\efcpt.exe</EfcptToolPath>
```

## Next Steps

- [Configuration](configuration.md) - Explore all MSBuild properties
- [Connection String Mode](connection-string-mode.md) - Use live database connections
- [T4 Templates](t4-templates.md) - Customize code generation
