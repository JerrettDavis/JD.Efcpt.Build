# JD.Efcpt.Build Samples

This directory contains sample projects demonstrating various usage patterns of JD.Efcpt.Build for automatic Entity Framework Core model generation during MSBuild.

## Sample Overview

| Sample | Input Mode | SQL SDK / Provider | Key Features |
|--------|------------|-------------------|--------------|
| [microsoft-build-sql-zero-config](#microsoft-build-sql-zero-config) | DACPAC | Microsoft.Build.Sql | **Zero-config** with official MS SDK |
| [dacpac-zero-config](#dacpac-zero-config) | DACPAC | Pre-built .dacpac | **Zero-config** direct DACPAC |
| [simple-generation](#simple-generation) | DACPAC | Traditional SQL Project (.sqlproj) | Basic usage, direct source import |
| [msbuild-sdk-sql-proj-generation](#msbuild-sdk-sql-proj-generation) | DACPAC | MSBuild.Sdk.SqlProj (.csproj) | Modern cross-platform SQL SDK |
| [split-data-and-models-between-multiple-projects](#split-outputs) | DACPAC | Traditional SQL Project (.sqlproj) | Clean architecture, split outputs |
| [connection-string-sqlite](#connection-string-sqlite) | Connection String | SQLite | Direct database reverse engineering |

## Input Modes

JD.Efcpt.Build supports two primary input modes:

### 1. DACPAC Mode (Default)
Reverse engineers from a SQL Project that produces a .dacpac file.

JD.Efcpt.Build supports multiple SQL Project SDKs:

| SDK | Extension | Cross-Platform | Notes |
|-----|-----------|----------------|-------|
| [Microsoft.Build.Sql](https://github.com/microsoft/DacFx) | `.sqlproj` | Yes | Microsoft's official SDK-style SQL Projects for .NET |
| [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) | `.csproj` or `.fsproj` | Yes | Community SDK with additional features and extensibility |
| Traditional SQL Projects | `.sqlproj` | No (Windows only) | Legacy format, requires SQL Server Data Tools |

```xml
<ItemGroup>
  <ProjectReference Include="..\Database\Database.sqlproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
  <!-- Use .sqlproj for Microsoft.Build.Sql -->
  <!-- Use .csproj or .fsproj for MSBuild.Sdk.SqlProj -->
</ItemGroup>
```

**Key Differences:**
- **Microsoft.Build.Sql** uses `.sqlproj` extension and is Microsoft's official SDK
- **MSBuild.Sdk.SqlProj** uses `.csproj`/`.fsproj` extension (despite having "SqlProj" in its name)
- Both produce DACPACs and work identically with JD.Efcpt.Build

### 2. Connection String Mode
Reverse engineers directly from a live database connection.

---

## Sample Details

### microsoft-build-sql-zero-config

**Location:** `microsoft-build-sql-zero-config/`

Demonstrates true **zero-configuration** usage with Microsoft's official `Microsoft.Build.Sql` SDK. Just add JD.Efcpt.Build to your project - no efcpt-config.json, no templates, no project references needed.

**Key Features:**
- **Zero configuration** - no efcpt-config.json, templates, or project references
- Uses Microsoft's official `Microsoft.Build.Sql` SDK (cross-platform)
- Automatic SQL project discovery from solution
- Default sensible configuration applied automatically

**Build:**
```bash
dotnet build microsoft-build-sql-zero-config/ZeroConfigMsBuildSql.sln
```

---

### dacpac-zero-config

**Location:** `dacpac-zero-config/`

Demonstrates **zero-configuration** reverse engineering directly from a pre-built `.dacpac` file. Ideal when you receive a DACPAC from a DBA or CI/CD pipeline.

**Key Features:**
- **Zero configuration** - no efcpt-config.json or templates
- Uses pre-built DACPAC file (no SQL project in solution)
- Simply set `EfcptDacpac` property to point to the .dacpac file
- No build step for SQL project - just reverse engineering

**Build:**
```bash
dotnet build dacpac-zero-config/ZeroConfigDacpac.sln
```

---

### simple-generation

**Location:** `simple-generation/`

Basic sample demonstrating DACPAC-based model generation with direct source import (useful for development).

```
simple-generation/
├── DatabaseProject/          # SQL Project
│   └── DatabaseProject.sqlproj  # Traditional format
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   ├── efcpt-config.json
│   └── Template/            # T4 templates
└── SimpleGenerationSample.sln
```

**Build:**
```bash
dotnet build simple-generation/SimpleGenerationSample.sln
```

---

### msbuild-sdk-sql-proj-generation

**Location:** `msbuild-sdk-sql-proj-generation/`

Demonstrates using MSBuild.Sdk.SqlProj for cross-platform DACPAC builds. This SDK uses `.csproj` extension (not `.sqlproj`).

```
msbuild-sdk-sql-proj-generation/
├── DatabaseProject/          # MSBuild.Sdk.SqlProj project
│   └── DatabaseProject.csproj   # Uses .csproj extension
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   └── efcpt-config.json
└── SimpleGenerationSample.sln
```

**Key Features:**
- Uses `MSBuild.Sdk.SqlProj` SDK for the SQL Project (note: uses `.csproj` extension)
- Works on Windows, Linux, and macOS
- Dynamic SQL Project discovery (no explicit reference needed)

> **Note:** Despite having "SqlProj" in its name, MSBuild.Sdk.SqlProj uses `.csproj` or `.fsproj` extensions, not `.sqlproj`.

---

### split-data-and-models-between-multiple-projects

**Location:** `split-data-and-models-between-multiple-projects/`

Advanced sample showing how to split generated output across multiple projects following clean architecture principles.

```
split-data-and-models-between-multiple-projects/
└── src/
    ├── SampleApp.Sql/       # SQL Project (Microsoft.Build.Sql format)
    ├── SampleApp.Models/    # Entity classes only (NO EF Core)
    └── SampleApp.Data/      # DbContext + EF Core dependencies
```

**Key Features:**
- `EfcptSplitOutputs=true` enables split generation
- Models project has no EF Core dependency
- DbContext and configurations go to Data project
- Automatic file distribution during build

---

### connection-string-sqlite

**Location:** `connection-string-sqlite/`

Demonstrates connection string mode with SQLite - no SQL Project needed, reverse engineers directly from a database.

---

## Common Configuration

All samples use:
- **T4 Templates** for code generation (customizable)
- **efcpt-config.json** for EF Core Power Tools configuration
- **efcpt.renaming.json** for entity/property renaming rules (optional)
- **Fingerprint-based incremental builds** - only regenerates when schema changes

> **Note:** The zero-config samples (`microsoft-build-sql-zero-config` and `dacpac-zero-config`) use sensible defaults and don't require any configuration files.

## Getting Started

1. Clone the repository
2. Choose a sample that matches your use case
3. Build the solution:
   ```bash
   dotnet build <sample-name>/<solution-file>.sln
   ```
4. Check the generated files in `obj/efcpt/Generated/`

## More Information

- [JD.Efcpt.Build Documentation](../docs/user-guide/)
- [EF Core Power Tools](https://github.com/ErikEJ/EFCorePowerTools)
