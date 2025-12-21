# JD.Efcpt.Build

[![NuGet](https://img.shields.io/nuget/v/JD.Efcpt.Build.svg)](https://www.nuget.org/packages/JD.Efcpt.Build/)
[![License](https://img.shields.io/github/license/jerrettdavis/JD.Efcpt.Build.svg)](LICENSE)

**MSBuild integration for EF Core Power Tools CLI**

Automate database-first EF Core model generation as part of your build pipeline. Zero manual steps, full CI/CD support, reproducible builds.

## 🚀 Quick Start

### Install (2-3 steps, 30 seconds)

**Step 1:** Add the NuGet package to your application project:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
</ItemGroup>
```

**Step 2:** *(Optional for .NET 10+)* Ensure EF Core Power Tools CLI is available:

> **✨ .NET 10+ Users:** The tool is automatically executed via `dnx` and does **not** need to be installed. Skip this step if you're using .NET 10.0 or later!

```bash
# Only required for .NET 8.0 and 9.0
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "8.*"
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "9.*"
```

**Step 3:** Build your project:

```bash
dotnet build
```

**That's it!** Your EF Core DbContext and entities are now automatically generated from your database project during every build.

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Quick Start](#-quick-start)
- [Features](#-features)
- [Installation](#-installation)
- [Minimal Usage Example](#-minimal-usage-example)
- [Configuration](#-configuration)
- [Advanced Scenarios](#-advanced-scenarios)
- [Troubleshooting](#-troubleshooting)
- [CI/CD Integration](#-cicd-integration)
- [API Reference](#-api-reference)

---

## 🎯 Overview

`JD.Efcpt.Build` transforms EF Core Power Tools into a **fully automated build step**. Instead of manually regenerating your EF Core models in Visual Studio, this package:

✅ **Automatically builds** your SQL Server Database Project (`.sqlproj`) to a DACPAC
✅ **OR connects directly** to your database via connection string
✅ **Runs EF Core Power Tools** CLI during `dotnet build`
✅ **Generates DbContext and entities** from your database schema
✅ **Intelligently caches** - only regenerates when schema or config changes
✅ **Works everywhere** - local dev, CI/CD, Docker, anywhere .NET runs
✅ **Zero manual steps** - true database-first development automation  

### Architecture

The package orchestrates a MSBuild pipeline with these stages:

1. **Resolve** - Locate database project and configuration files
2. **Build** - Compile `.sqlproj` to DACPAC (if needed)
3. **Stage** - Prepare configuration and templates
4. **Fingerprint** - Detect if regeneration is needed
5. **Generate** - Run `efcpt` to create EF Core models
6. **Compile** - Add generated `.g.cs` files to build

---

## ✨ Features

### Core Capabilities

- **🔄 Incremental Builds** - Only regenerates when database schema or configuration changes
- **🎨 T4 Template Support** - Customize code generation with your own templates
- **📁 Smart File Organization** - Schema-based folders and namespaces
- **🔧 Highly Configurable** - Override namespaces, output paths, and generation options
- **🌐 Multi-Schema Support** - Generate models across multiple database schemas
- **📦 NuGet Ready** - Enterprise-ready package for production use

### Build Integration

- **Automatic DACPAC compilation** from `.sqlproj` files
- **Project discovery** - Automatically finds your database project
- **Template staging** - Handles T4 templates correctly (no duplicate folders!)
- **Generated file management** - Clean `.g.cs` file naming and compilation
- **Rebuild detection** - Triggers regeneration when `obj/efcpt` is deleted

---

## 📦 Installation

### Prerequisites

- **.NET SDK 8.0+** (or compatible version)
- **EF Core Power Tools CLI** (`ErikEJ.EFCorePowerTools.Cli`) - **Not required for .NET 10.0+** (uses `dnx` instead)
- **SQL Server Database Project** (`.sqlproj`) that compiles to DACPAC

### Step 1: Install the Package

Add to your application project (`.csproj`):

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*" />
</ItemGroup>
```

Or install via .NET CLI:

```bash
dotnet add package JD.Efcpt.Build
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Step 2: Install EF Core Power Tools CLI

**Option A: Global Tool (Quick Start)**

```bash
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

**Option B: Local Tool (Recommended for Teams/CI)**

```bash
# Create tool manifest (if not exists)
dotnet new tool-manifest

# Install as local tool
dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

Local tools ensure everyone on the team uses the same version.

---

## 💡 Minimal Usage Example

### Solution Structure

```
YourSolution/
├── src/
│   └── YourApp/
│       ├── YourApp.csproj          # Add JD.Efcpt.Build here
│       ├── efcpt-config.json       # Optional: customize generation
│       └── Template/               # Optional: custom T4 templates
│           └── CodeTemplates/
│               └── EFCore/
│                   ├── DbContext.t4
│                   └── EntityType.t4
└── database/
    └── YourDatabase/
        └── YourDatabase.sqlproj    # Your database project
```

### Minimal Configuration (YourApp.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
  </ItemGroup>

  <!-- Optional: Point to specific database project -->
  <PropertyGroup>
    <EfcptSqlProj>..\..\database\YourDatabase\YourDatabase.sqlproj</EfcptSqlProj>
  </PropertyGroup>
</Project>
```

### Build and Run

```bash
dotnet build
```

**Generated files** appear in `obj/efcpt/Generated/`:

```
obj/efcpt/Generated/
├── YourDbContext.g.cs       # DbContext
└── Models/                  # Entity classes
    ├── dbo/
    │   ├── User.g.cs
    │   └── Order.g.cs
    └── sales/
        └── Customer.g.cs
```

These files are **automatically compiled** into your project!

---

## ⚙️ Configuration

### Option 1: Use Defaults (Zero Config)

Just add the package. Sensible defaults are applied:

- Auto-discovers `.sqlproj` in solution
- Uses `efcpt-config.json` if present, otherwise uses defaults
- Generates to `obj/efcpt/Generated/`
- Enables nullable reference types
- Uses schema-based namespaces

### Option 2: Customize with efcpt-config.json

Create `efcpt-config.json` in your project:

```json
{
  "names": {
    "root-namespace": "YourApp.Data",
    "dbcontext-name": "ApplicationDbContext",
    "dbcontext-namespace": "YourApp.Data",
    "entity-namespace": "YourApp.Data.Entities"
  },
  "code-generation": {
    "use-t4": true,
    "t4-template-path": "Template",
    "use-nullable-reference-types": true,
    "use-date-only-time-only": true,
    "enable-on-configuring": false
  },
  "file-layout": {
    "output-path": "Models",
    "output-dbcontext-path": ".",
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  },
  "table-selection": [
    {
      "schema": "dbo",
      "include": true
    }
  ]
}
```

### Option 3: MSBuild Properties (Advanced)

Override in your `.csproj` or `Directory.Build.props`:

```xml
<PropertyGroup>
  <!-- Core Settings -->
  <EfcptEnabled>true</EfcptEnabled>
  <EfcptSqlProj>..\Database\Database.sqlproj</EfcptSqlProj>
  
  <!-- Paths -->
  <EfcptConfig>custom-efcpt-config.json</EfcptConfig>
  <EfcptRenaming>custom-renaming.json</EfcptRenaming>
  <EfcptTemplateDir>CustomTemplates</EfcptTemplateDir>
  
  <!-- Output -->
  <EfcptOutput>$(MSBuildProjectDirectory)\obj\efcpt\</EfcptOutput>
  <EfcptGeneratedDir>$(EfcptOutput)Generated\</EfcptGeneratedDir>
  
  <!-- Tool Configuration -->
  <EfcptToolMode>tool-manifest</EfcptToolMode>
  <EfcptToolVersion>10.*</EfcptToolVersion>
  
  <!-- Diagnostics -->
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
</PropertyGroup>
```

---

## 🔧 Advanced Scenarios

### Multi-Project Solutions (Directory.Build.props)

Share configuration across multiple projects:

```xml
<!-- Directory.Build.props at solution root -->
<Project>
  <PropertyGroup>
    <EfcptEnabled>true</EfcptEnabled>
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolVersion>10.*</EfcptToolVersion>
    <EfcptLogVerbosity>minimal</EfcptLogVerbosity>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>
</Project>
```

Individual projects can override specific settings:

```xml
<!-- src/MyApp/MyApp.csproj -->
<PropertyGroup>
  <EfcptSqlProj>..\..\database\MyDatabase\MyDatabase.sqlproj</EfcptSqlProj>
  <EfcptConfig>my-specific-config.json</EfcptConfig>
</PropertyGroup>
```

### Custom T4 Templates

1. **Copy default templates** from the package or create your own
2. **Place in your project** under `Template/CodeTemplates/EFCore/` (recommended)
3. **Configure** in `efcpt-config.json`:

```json
{
  "code-generation": {
    "use-t4": true,
    "t4-template-path": "."
  }
}
```

Templates are automatically staged to `obj/efcpt/Generated/CodeTemplates/` during build.

Notes:

- `StageEfcptInputs` understands the common `Template/CodeTemplates/EFCore` layout, but it also supports:
  - `Template/CodeTemplates/*` (copies the full `CodeTemplates` tree)
  - A template folder without a `CodeTemplates` subdirectory (the entire folder is staged as `CodeTemplates`)
- The staging destination is `$(EfcptGeneratedDir)\CodeTemplates\` by default.

### Renaming Rules (efcpt.renaming.json)

Customize table and column naming:

```json
{
  "tables": [
    {
      "name": "tblUsers",
      "newName": "User"
    }
  ],
  "columns": [
    {
      "table": "User",
      "name": "usr_id",
      "newName": "Id"
    }
  ]
}
```

### Disable for Specific Build Configurations

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

---

## 🔌 Connection String Mode 

### Overview

`JD.Efcpt.Build` supports direct database connection as an alternative to DACPAC-based workflows. Connection string mode allows you to reverse-engineer your EF Core models directly from a live database without requiring a `.sqlproj` file.

### When to Use Connection String Mode vs DACPAC Mode

**Use Connection String Mode When:**

- You don't have a SQL Server Database Project (`.sqlproj`)
- You want faster builds (no DACPAC compilation step)
- You're working with a cloud database or managed database instance
- You prefer to scaffold from a live database environment

**Use DACPAC Mode When:**

- You have an existing `.sqlproj` that defines your schema
- You want schema versioning through database projects
- You prefer design-time schema validation
- Your CI/CD already builds DACPACs

### Configuration Methods

#### Method 1: Explicit Connection String (Highest Priority)

Set the connection string directly in your `.csproj`:

```xml
<PropertyGroup>
  <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
</PropertyGroup>
```

Or use environment variables for security:

```xml
<PropertyGroup>
  <EfcptConnectionString>$(DB_CONNECTION_STRING)</EfcptConnectionString>
</PropertyGroup>
```

#### Method 2: appsettings.json (ASP.NET Core)

**Recommended for ASP.NET Core projects.** Place your connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;Integrated Security=True;"
  }
}
```

Then configure in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Points to the appsettings.json file -->
  <EfcptAppSettings>appsettings.json</EfcptAppSettings>

  <!-- Optional: specify which key to use (defaults to "DefaultConnection") -->
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
</PropertyGroup>
```

You can also reference environment-specific files:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Development'">
  <EfcptAppSettings>appsettings.Development.json</EfcptAppSettings>
</PropertyGroup>
```

#### Method 3: app.config or web.config (.NET Framework)

**Recommended for .NET Framework projects.** Add your connection string to `app.config` or `web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <connectionStrings>
    <add name="DefaultConnection"
         connectionString="Server=localhost;Database=MyDb;Integrated Security=True;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

Configure in your `.csproj`:

```xml
<PropertyGroup>
  <EfcptAppConfig>app.config</EfcptAppConfig>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
</PropertyGroup>
```

#### Method 4: Auto-Discovery (Zero Configuration)

If you don't specify any connection string properties, `JD.Efcpt.Build` will **automatically search** for connection strings in this order:

1. **appsettings.json** in your project directory
2. **appsettings.Development.json** in your project directory
3. **app.config** in your project directory
4. **web.config** in your project directory

If a connection string named `DefaultConnection` exists, it will be used. If not, the **first available connection string** will be used (with a warning logged).

**Example - Zero configuration:**

```
MyApp/
├── MyApp.csproj
└── appsettings.json  ← Connection string auto-discovered here
```

No properties needed! Just run `dotnet build`.

### Discovery Priority Chain

When multiple connection string sources are present, this priority order is used:

1. **`EfcptConnectionString`** property (highest priority)
2. **`EfcptAppSettings`** or **`EfcptAppConfig`** explicit paths
3. **Auto-discovered** configuration files
4. **Fallback to `.sqlproj`** (DACPAC mode) if no connection string found

### Migration Guide: From DACPAC Mode to Connection String Mode

#### Before (DACPAC Mode)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="1.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptSqlProj>..\Database\Database.sqlproj</EfcptSqlProj>
  </PropertyGroup>
</Project>
```

#### After (Connection String Mode)

**Option A: Explicit connection string**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="2.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
  </PropertyGroup>
</Project>
```

**Option B: Use existing appsettings.json (Recommended)**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="2.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  </PropertyGroup>
</Project>
```

**Option C: Auto-discovery (Simplest)**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="2.x.x" />
  </ItemGroup>

  <!-- No connection string config needed! -->
  <!-- Will auto-discover from appsettings.json -->
</Project>
```

### Connection String Mode Properties Reference

#### Input Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptConnectionString` | *(empty)* | Explicit connection string override. **Takes highest priority.** |
| `EfcptAppSettings` | *(empty)* | Path to `appsettings.json` file containing connection strings. |
| `EfcptAppConfig` | *(empty)* | Path to `app.config` or `web.config` file containing connection strings. |
| `EfcptConnectionStringName` | `DefaultConnection` | Name of the connection string key to use from configuration files. |
| `EfcptProvider` | `mssql` | Database provider (currently only `mssql` is supported). |

#### Output Properties

| Property | Description |
|----------|-------------|
| `ResolvedConnectionString` | The resolved connection string that will be used. |
| `UseConnectionString` | `true` when using connection string mode, `false` for DACPAC mode. |

### Database Provider Support

**Currently Supported:**
- **SQL Server** (`mssql`) - Fully supported

**Planned for Future Versions:**
- ⏳ PostgreSQL (`postgresql`)
- ⏳ MySQL (`mysql`)
- ⏳ MariaDB (`mariadb`)
- ⏳ Oracle (`oracle`)
- ⏳ SQLite (`sqlite`)

### Security Best Practices

**❌ DON'T** commit connection strings with passwords to source control:

```xml
<!-- BAD: Password in plain text -->
<EfcptConnectionString>Server=prod;Database=MyDb;User=sa;Password=Secret123;</EfcptConnectionString>
```

**✅ DO** use environment variables or user secrets:

```xml
<!-- GOOD: Reference environment variable -->
<EfcptConnectionString>$(ProductionDbConnectionString)</EfcptConnectionString>
```

**✅ DO** use Windows/Integrated Authentication when possible:

```xml
<EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
```

**✅ DO** use different connection strings for different environments:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Development'">
  <EfcptConnectionString>Server=localhost;Database=MyDb_Dev;Integrated Security=True;</EfcptConnectionString>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Production'">
  <EfcptConnectionString>$(PRODUCTION_DB_CONNECTION_STRING)</EfcptConnectionString>
</PropertyGroup>
```

### How Schema Fingerprinting Works

In connection string mode, instead of hashing the DACPAC file, `JD.Efcpt.Build`:

1. **Queries the database** system tables (`sys.tables`, `sys.columns`, `sys.indexes`, etc.)
2. **Builds a canonical schema model** with all tables, columns, indexes, foreign keys, and constraints
3. **Computes an XxHash64 fingerprint** of the schema structure
4. **Caches the fingerprint** to skip regeneration when the schema hasn't changed

This means your builds are still **incremental** - models are only regenerated when the database schema actually changes!

### Example: ASP.NET Core with Connection String Mode

```xml
<!-- MyApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="2.x.x" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.x" />
  </ItemGroup>

  <!-- Connection string mode: Use appsettings.json -->
  <PropertyGroup>
    <EfcptAppSettings>appsettings.json</EfcptAppSettings>
    <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
  </PropertyGroup>
</Project>
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Integrated Security=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

Build your project:

```bash
dotnet build
```

Generated models appear in `obj/efcpt/Generated/` automatically!

---

## 🐛 Troubleshooting

### Generated Files Don't Appear

**Check:**

1. **Verify package is referenced:**
   ```bash
   dotnet list package | findstr JD.Efcpt.Build
   ```

2. **Check if generation ran:**
   ```bash
   # Look for obj/efcpt/Generated/ folder
   dir obj\efcpt\Generated /s
   ```

3. **Enable detailed logging:**
   ```xml
   <PropertyGroup>
     <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
     <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
   </PropertyGroup>
   ```

4. **Rebuild from scratch:**
   ```bash
   dotnet clean
   dotnet build
   ```

### DACPAC Build Fails

### efcpt CLI Not Found

**Symptoms:** "efcpt command not found" or similar

**Solutions:**

**.NET 10+ Users:**
- This issue should not occur on .NET 10+ as the tool is executed via `dnx` without installation
- If you see this error, verify you're running .NET 10.0 or later: `dotnet --version`

**.NET 8-9 Users:**

1. **Verify installation:**
   ```bash
   dotnet tool list --global
   # or
   dotnet tool list
   ```

2. **Reinstall:**
   ```bash
   dotnet tool uninstall -g ErikEJ.EFCorePowerTools.Cli
   dotnet tool install -g ErikEJ.EFCorePowerTools.Cli --version "10.*"
   ```

3. **Force tool manifest mode:**
   ```xml
   <PropertyGroup>
     <EfcptToolMode>tool-manifest</EfcptToolMode>
   </PropertyGroup>
   ```

### Build Doesn't Detect Schema Changes

**Cause:** Fingerprint not updating

**Solution:** Delete intermediate folder to force regeneration:

```bash
rmdir /s /q obj\efcpt
dotnet build
```

---

## 🚢 CI/CD Integration

### GitHub Actions

**.NET 10+ (Recommended - No tool installation required!)**

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build
```

**.NET 8-9 (Requires tool installation)**

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore tools
      run: dotnet tool restore

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore tools'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--configuration Release --no-restore'
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore
COPY *.sln .
COPY **/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore

# Restore tools
COPY .config/dotnet-tools.json .config/
RUN dotnet tool restore

# Copy everything and build
COPY . .
RUN dotnet build --configuration Release --no-restore
```

### Key CI/CD Considerations

1. **Use .NET 10+** - Eliminates the need for tool manifests and installation steps via `dnx`
2. **Use local tool manifest (.NET 8-9)** - Ensures consistent `efcpt` version across environments
3. **Cache tool restoration (.NET 8-9)** - Speed up builds by caching `.dotnet/tools`
4. **Windows agents for DACPAC** - Database projects typically require Windows build agents
5. **Deterministic builds** - Generated code should be identical across builds with same inputs

---

## 📚 API Reference

### MSBuild Targets

| Target | Purpose | When It Runs |
|--------|---------|--------------|
| `EfcptResolveInputs` | Discovers database project and config files | Before build |
| `EfcptEnsureDacpac` | Builds `.sqlproj` to DACPAC if needed | After resolve |
| `EfcptStageInputs` | Stages config and templates | After DACPAC |
| `EfcptComputeFingerprint` | Detects if regeneration needed | After staging |
| `EfcptGenerateModels` | Runs `efcpt` CLI | When fingerprint changes |
| `EfcptAddToCompile` | Adds `.g.cs` files to compilation | Before C# compile |

### MSBuild Properties

#### Core Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptEnabled` | `true` | Master switch for the entire pipeline |
| `EfcptSqlProj` | *(auto-discovered)* | Path to `.sqlproj` file |
| `EfcptConfig` | `efcpt-config.json` | EF Core Power Tools configuration |
| `EfcptRenaming` | `efcpt.renaming.json` | Renaming rules file |
| `EfcptTemplateDir` | `Template` | T4 template directory |
| `EfcptOutput` | `$(BaseIntermediateOutputPath)efcpt\` | Intermediate staging directory |
| `EfcptGeneratedDir` | `$(EfcptOutput)Generated\` | Generated code output directory |

#### Connection String Properties

When `EfcptConnectionString` is set (or when a connection string can be resolved from configuration files), the pipeline switches to **connection string mode**:

- `EfcptEnsureDacpac` is skipped.
- `EfcptQuerySchemaMetadata` runs to fingerprint the database schema.

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptConnectionString` | *(empty)* | Explicit connection string override (enables connection string mode) |
| `EfcptAppSettings` | *(empty)* | Optional `appsettings.json` path used to resolve connection strings |
| `EfcptAppConfig` | *(empty)* | Optional `app.config`/`web.config` path used to resolve connection strings |
| `EfcptConnectionStringName` | `DefaultConnection` | Connection string name/key to read from configuration files |
| `EfcptProvider` | `mssql` | Provider identifier for schema querying and efcpt (Phase 1 supports SQL Server only) |

#### Tool Configuration

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptToolMode` | `auto` | Tool resolution mode: `auto` or `tool-manifest` (any other value forces the global tool path) |
| `EfcptToolPackageId` | `ErikEJ.EFCorePowerTools.Cli` | NuGet package ID for efcpt |
| `EfcptToolVersion` | `10.*` | Version constraint |
| `EfcptToolCommand` | `efcpt` | Command name |
| `EfcptToolPath` | *(empty)* | Explicit path to efcpt executable |
| `EfcptDotNetExe` | `dotnet` | Path to dotnet host |
| `EfcptToolRestore` | `true` | Whether to restore/update tool |

#### Advanced Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptLogVerbosity` | `minimal` | Logging level: `minimal` or `detailed` |
| `EfcptDumpResolvedInputs` | `false` | Log all resolved input paths |
| `EfcptSolutionDir` | `$(SolutionDir)` | Solution root for project discovery |
| `EfcptSolutionPath` | `$(SolutionPath)` | Solution file path (fallback SQL project discovery) |
| `EfcptProbeSolutionDir` | `true` | Whether to probe solution directory |
| `EfcptFingerprintFile` | `$(EfcptOutput)fingerprint.txt` | Fingerprint cache location |
| `EfcptStampFile` | `$(EfcptOutput).efcpt.stamp` | Generation stamp file |

### MSBuild Tasks

#### StageEfcptInputs

Stages configuration files and templates into the intermediate directory.

**Parameters:**
- `OutputDir` (required) - Base staging directory
- `ProjectDirectory` (required) - Consuming project directory (used to keep staging paths stable)
- `ConfigPath` (required) - Path to `efcpt-config.json`
- `RenamingPath` (required) - Path to `efcpt.renaming.json`
- `TemplateDir` (required) - Path to template directory
- `TemplateOutputDir` - Subdirectory within OutputDir for templates (e.g., "Generated")
- `LogVerbosity` - Logging level

**Outputs:**
- `StagedConfigPath` - Full path to staged config
- `StagedRenamingPath` - Full path to staged renaming file
- `StagedTemplateDir` - Full path to staged templates

#### ComputeFingerprint

Computes SHA256 fingerprint of all inputs to detect when regeneration is needed.

**Parameters:**
- `DacpacPath` - Path to DACPAC file (used in `.sqlproj` mode)
- `SchemaFingerprint` - Schema fingerprint produced by `QuerySchemaMetadata` (used in connection string mode)
- `UseConnectionStringMode` - Boolean-like flag indicating connection string mode
- `ConfigPath` (required) - Path to efcpt config
- `RenamingPath` (required) - Path to renaming file
- `TemplateDir` (required) - Path to templates
- `FingerprintFile` (required) - Path to the fingerprint cache file that is read/written
- `LogVerbosity` - Logging level

**Outputs:**
- `Fingerprint` - Computed SHA256 hash
- `HasChanged` - Boolean-like flag indicating if the fingerprint changed

#### RunEfcpt

Executes EF Core Power Tools CLI to generate EF Core models.

**Parameters:**
- `ToolMode` - How to find efcpt: `auto` or `tool-manifest` (any other value uses the global tool path)
- `ToolPackageId` - NuGet package ID
- `ToolVersion` - Version constraint
- `ToolRestore` - Whether to restore tool
- `ToolCommand` - Command name
- `ToolPath` - Explicit path to executable
- `DotNetExe` - Path to dotnet host
- `WorkingDirectory` - Working directory for efcpt
- `DacpacPath` - Input DACPAC (used in `.sqlproj` mode)
- `ConnectionString` - Database connection string (used in connection string mode)
- `UseConnectionStringMode` - Boolean-like flag indicating connection string mode
- `Provider` - Provider identifier passed to efcpt (default: `mssql`)
- `ConfigPath` (required) - efcpt configuration
- `RenamingPath` (required) - Renaming rules
- `TemplateDir` (required) - Template directory
- `OutputDir` (required) - Output directory
- `LogVerbosity` - Logging level

#### QuerySchemaMetadata

Queries database schema metadata and computes a deterministic schema fingerprint (used in connection string mode).

**Parameters:**
- `ConnectionString` (required) - Database connection string
- `OutputDir` (required) - Output directory (writes `schema-model.json` for diagnostics)
- `Provider` - Provider identifier (default: `mssql`; Phase 1 supports SQL Server only)
- `LogVerbosity` - Logging level

**Outputs:**
- `SchemaFingerprint` - Computed schema fingerprint

#### RenameGeneratedFiles

Renames generated `.cs` files to `.g.cs` for better identification.

**Parameters:**
- `GeneratedDir` (required) - Directory containing generated files
- `LogVerbosity` - Logging level

#### ResolveSqlProjAndInputs

Discovers database project and configuration files.

**Parameters:**
- `ProjectFullPath` (required) - Full path to the consuming project
- `ProjectDirectory` (required) - Directory containing the consuming project
- `Configuration` (required) - Active build configuration (e.g. `Debug` or `Release`)
- `ProjectReferences` - Project references of the consuming project
- `SqlProjOverride` - Optional override path for the SQL project
- `ConfigOverride` - Optional override path for efcpt config
- `RenamingOverride` - Optional override path for renaming rules
- `TemplateDirOverride` - Optional override path for templates
- `SolutionDir` - Optional solution root to probe for inputs
- `SolutionPath` - Optional solution file path (used as a fallback when discovering the SQL project)
- `ProbeSolutionDir` - Boolean-like flag controlling whether `SolutionDir` is probed (default: `true`)
- `OutputDir` (required) - Output directory used by later stages (and for `resolved-inputs.json`)
- `DefaultsRoot` - Root directory containing packaged default inputs (typically the NuGet `Defaults` folder)
- `DumpResolvedInputs` - When `true`, writes `resolved-inputs.json` to `OutputDir`
- `EfcptConnectionString` - Optional explicit connection string (enables connection string mode)
- `EfcptAppSettings` - Optional `appsettings.json` path used to resolve connection strings
- `EfcptAppConfig` - Optional `app.config`/`web.config` path used to resolve connection strings
- `EfcptConnectionStringName` - Connection string name/key (default: `DefaultConnection`)

**Outputs:**
- `SqlProjPath` - Discovered SQL project path
- `ResolvedConfigPath` - Discovered config path
- `ResolvedRenamingPath` - Discovered renaming path
- `ResolvedTemplateDir` - Discovered template directory
- `ResolvedConnectionString` - Resolved connection string (connection string mode)
- `UseConnectionString` - Boolean-like flag indicating whether connection string mode is active

#### EnsureDacpacBuilt

Builds a `.sqlproj` to DACPAC if it's out of date.

**Parameters:**
- `SqlProjPath` (required) - Path to `.sqlproj`
- `Configuration` (required) - Build configuration (e.g. `Debug` / `Release`)
- `MsBuildExe` - Path to `msbuild.exe` (preferred on Windows when present)
- `DotNetExe` - Path to dotnet host (used for `dotnet msbuild` when `msbuild.exe` is unavailable)
- `LogVerbosity` - Logging level

**Outputs:**
- `DacpacPath` - Path to built DACPAC file

---

## 🤝 Contributing

Contributions are welcome! Please:

1. **Open an issue** first to discuss changes
2. **Follow existing code style** and patterns
3. **Add tests** for new features
4. **Update documentation** as needed

---

## 📄 License

This project is licensed under the MIT License. See LICENSE file for details.

---

## 🙏 Acknowledgments

- **EF Core Power Tools** by Erik Ejlskov Jensen - The amazing tool this package automates
- **Microsoft** - For EF Core and MSBuild
- **Community contributors** - Thank you for your feedback and contributions!

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/jerrettdavis/JD.Efcpt.Build/issues)
- **Discussions:** [GitHub Discussions](https://github.com/jerrettdavis/JD.Efcpt.Build/discussions)
- **Documentation:** [README](https://github.com/jerrettdavis/JD.Efcpt.Build/blob/main/README.md)

---

**Made with ❤️ for the .NET community**

Use `JD.Efcpt.Build` when:

- You have a SQL Server database described by a Database Project (`.sqlproj`) and want EF Core DbContext and entity classes generated from it.
- You want EF Core Power Tools generation to run as part of `dotnet build` instead of being a manual step in Visual Studio.
- You need deterministic, source-controlled model generation that works the same way on developer machines and in CI/CD.

The package focuses on database-first modeling using EF Core Power Tools CLI (`ErikEJ.EFCorePowerTools.Cli`).

---

## 2. Installation

### 2.1 Add the NuGet package

Add a package reference to your application project (the project that should contain the generated DbContext and entity classes):

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
</ItemGroup>
```

Or enable it solution-wide via `Directory.Build.props`:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
  </ItemGroup>
</Project>
```

### 2.2 Install EF Core Power Tools CLI

`JD.Efcpt.Build` drives the EF Core Power Tools CLI (`efcpt`). You must ensure the CLI is available on all machines that run your build.

Global tool example:

```powershell
# PowerShell
 dotnet tool install -g ErikEJ.EFCorePowerTools.Cli
```

Local tool (recommended for shared/CI environments):

```powershell
# From your solution root
 dotnet new tool-manifest
 dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

By default the build uses `dotnet tool run efcpt` when a local tool manifest is present, or falls back to running `efcpt` directly when it is globally installed. These behaviors can be controlled using the properties described later.

### 2.3 Prerequisites

- .NET SDK 8.0 or newer.
- EF Core Power Tools CLI installed as a .NET tool (global or local).
- A SQL Server Database Project (`.sqlproj`) that can be built to a DACPAC. On build agents this usually requires the appropriate SQL Server Data Tools / build tools components.

---

## 3. High-level architecture

`JD.Efcpt.Build` wires a set of MSBuild targets into your project. When `EfcptEnabled` is `true` (the default), the following pipeline runs as part of `dotnet build`:

1. **EfcptResolveInputs** – locates the `.sqlproj` and resolves configuration inputs.
2. **EfcptQuerySchemaMetadata** *(connection string mode only)* – fingerprints the live database schema.
3. **EfcptEnsureDacpac** *(.sqlproj mode only)* – builds the database project to a DACPAC if needed.
4. **EfcptStageInputs** – stages the EF Core Power Tools configuration, renaming rules, and templates into an intermediate directory.
5. **EfcptComputeFingerprint** – computes a fingerprint across the DACPAC (or schema fingerprint) and staged inputs.
6. **EfcptGenerateModels** – runs `efcpt` and renames generated files to `.g.cs` when the fingerprint changes.
7. **EfcptAddToCompile** – adds the generated `.g.cs` files to the `Compile` item group so they are part of your build.

The underlying targets and tasks live in `build/JD.Efcpt.Build.targets` and `JD.Efcpt.Build.Tasks.dll`.

---

## 4. Minimal usage

### 4.1 Typical solution layout

A common setup looks like this:

- `MyApp.csproj` – application project where you want the EF Core DbContext and entities.
- `Database/Database.sqlproj` – SQL Server Database Project that produces a DACPAC.
- `Directory.Build.props` – optional solution-wide configuration.

### 4.2 Quick start

1. Add `JD.Efcpt.Build` to your application project (or to `Directory.Build.props`).
2. Ensure a `.sqlproj` exists somewhere in the solution that builds to a DACPAC.
3. Optionally copy the default `efcpt-config.json` from the package (see below) into your application project to customize namespaces and options.
4. Run:

```powershell
 dotnet build
```

On the first run the build will:

- Build the `.sqlproj` to a DACPAC.
- Stage EF Core Power Tools configuration.
- Run `efcpt` to generate DbContext and entity types.
- Place generated code under the directory specified by `EfcptGeneratedDir` (by default under `obj/efcpt/Generated` in the sample tests).

Subsequent builds will only re-run `efcpt` when the DACPAC or staged configuration changes.

---

## 5. Configuration via MSBuild properties

The behavior of the pipeline is controlled by a set of MSBuild properties. You can define these in your project file or in `Directory.Build.props`.

### 5.1 Core properties

- `EfcptEnabled` (default: `true`)
  - Master on/off switch for the entire pipeline.

- `EfcptOutput`
  - Intermediate directory used to stage configuration and compute fingerprints.
  - If not set, a reasonable default is chosen relative to the project.

- `EfcptGeneratedDir`
  - Directory where generated C# files are written.
  - Used by `EfcptGenerateModels` and `EfcptAddToCompile`.

- `EfcptSqlProj`
  - Optional override for the path to the Database Project (`.sqlproj`).
  - When not set, `ResolveSqlProjAndInputs` attempts to discover the project based on project references and solution layout.

- `EfcptConnectionString`
  - Optional explicit connection string override.
  - When set (or when a connection string is resolved from configuration files), the pipeline runs in **connection string mode**:
    - `EfcptEnsureDacpac` is skipped.
    - `EfcptQuerySchemaMetadata` runs and its schema fingerprint is used in incremental builds instead of the DACPAC content.

- `EfcptAppSettings`
  - Optional `appsettings.json` path used to resolve connection strings.

- `EfcptAppConfig`
  - Optional `app.config` / `web.config` path used to resolve connection strings.

- `EfcptConnectionStringName` (default: `DefaultConnection`)
  - Connection string name/key to read from configuration files.

- `EfcptProvider` (default: `mssql`)
  - Provider identifier passed to schema querying and efcpt.
  - Phase 1 supports SQL Server only.

- `EfcptConfig`
  - Optional override for the EF Core Power Tools configuration file (defaults to `efcpt-config.json` in the project directory when present).

- `EfcptRenaming`
  - Optional override for the renaming configuration (defaults to `efcpt.renaming.json` in the project directory when present).

- `EfcptTemplateDir`
  - Optional override for the template directory (defaults to `Template` in the project directory when present).

- `EfcptSolutionDir`
  - Root directory used when probing for related projects, if automatic discovery needs help.

- `EfcptProbeSolutionDir`
  - Controls whether solution probing is performed. Use this if your layout is non-standard.

- `EfcptSolutionPath`
  - Optional solution file path used as a fallback when discovering the SQL project.

- `EfcptLogVerbosity`
  - Controls task logging (`minimal` or `detailed`).

### 5.2 Tool resolution properties

These properties control how the `RunEfcpt` task finds and invokes the EF Core Power Tools CLI:

- `EfcptToolMode`
  - Controls the strategy used to locate the tool. Common values:
    - `auto` – use a local tool if a manifest is present, otherwise fall back to a global tool.
    - `tool-manifest` – require a local tool manifest and fail if one is not present.
  - Any other non-empty value forces the global tool path.

- `EfcptToolPackageId`
  - NuGet package ID for the CLI. Defaults to `ErikEJ.EFCorePowerTools.Cli`.

- `EfcptToolVersion`
  - Requested CLI version or version range (for example, `10.*`).

- `EfcptToolRestore`
  - When `true`, the task may restore or update the tool as part of the build.

- `EfcptToolCommand`
  - The command to execute when running the tool (defaults to `efcpt`).

- `EfcptToolPath`
  - Optional explicit path to the `efcpt` executable. When set, this takes precedence over `dotnet tool run`.

- `EfcptDotNetExe`
  - Optional explicit path to the `dotnet` host used for tool invocations and `.sqlproj` builds.

### 5.3 Fingerprinting and diagnostics

- `EfcptFingerprintFile`
  - Path to the fingerprint file produced by `ComputeFingerprint`.

- `EfcptStampFile`
  - Path to the stamp file written by `EfcptGenerateModels` to record the last successful fingerprint.

- `EfcptDumpResolvedInputs`
  - When `true`, `ResolveSqlProjAndInputs` logs the resolved inputs to help diagnose discovery and configuration issues.

---

## 6. Configuration files and defaults

The NuGet package ships default configuration assets under a `Defaults` folder. These defaults are used when you do not provide your own, and they can be copied into your project and customized.

### 6.1 `efcpt-config.json`

`efcpt-config.json` is the main configuration file for EF Core Power Tools. The version shipped by this package sets sensible defaults for code generation, including:

- Enabling nullable reference types.
- Enabling `DateOnly`/`TimeOnly` where appropriate.
- Controlling which schemas and tables are included.
- Controlling namespaces, DbContext name, and output folder structure.

Typical sections you might customize include:

- `code-generation` – toggles for features such as data annotations, T4 usage, or using `DbContextFactory`.
- `names` – default namespace, DbContext name, and related name settings.
- `file-layout` – where files are written relative to the project and how they are grouped.
- `replacements` and `type-mappings` – table/column renaming rules and type overrides.

You can start with the default `efcpt-config.json` from the package and adjust these sections to match your conventions.

### 6.2 `efcpt.renaming.json`

`efcpt.renaming.json` is an optional JSON file that contains additional renaming rules for database objects and generated code. Use it to:

- Apply custom naming conventions beyond those specified in `efcpt-config.json`.
- Normalize table, view, or schema names.

If a project-level `efcpt.renaming.json` is present, it will be preferred over the default shipped with the package.

### 6.3 Template folder

The package also ships a `Template` folder containing template files used by EF Core Power Tools when T4-based generation is enabled.

If you need to customize templates:

1. Copy the `Template` folder from the package into your project or a shared location.
2. Update `EfcptTemplateDir` (or the corresponding setting in `efcpt-config.json`) to point to your customized templates.

During a build, the `StageEfcptInputs` task stages the effective config, renaming file, and template folder into `EfcptOutput` before running `efcpt`.

---

## 7. Examples

### 7.1 Basic project-level configuration

Application project (`MyApp.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
  </ItemGroup>

  <!-- Optional: point directly at a specific .sqlproj -->
  <PropertyGroup>
    <EfcptSqlProj>..\Database\Database.sqlproj</EfcptSqlProj>
  </PropertyGroup>
</Project>
```

Place `efcpt-config.json` and (optionally) `efcpt.renaming.json` in the same directory as `MyApp.csproj`, then run `dotnet build`. Generated DbContext and entities are automatically included in the compilation.

### 7.2 Solution-wide configuration via `Directory.Build.props`

To enable the pipeline across multiple application projects, you can centralize configuration in `Directory.Build.props` at the solution root:

```xml
<Project>
  <PropertyGroup>
    <!-- Enable EF Core Power Tools generation for all projects by default -->
    <EfcptEnabled>true</EfcptEnabled>

    <!-- Use a consistent intermediate and output layout across the solution -->
    <EfcptOutput>$(MSBuildProjectDirectory)\obj\efcpt\</EfcptOutput>
    <EfcptGeneratedDir>$(MSBuildProjectDirectory)\obj\efcpt\Generated\</EfcptGeneratedDir>

    <!-- Prefer local dotnet tool manifests for the CLI -->
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolPackageId>ErikEJ.EFCorePowerTools.Cli</EfcptToolPackageId>
    <EfcptToolVersion>10.*</EfcptToolVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
  </ItemGroup>
</Project>
```

Individual projects can then override `EfcptSqlProj`, `EfcptConfig`, or other properties when they diverge from the solution defaults.

### 7.3 CI / build pipeline integration

No special steps are required beyond installing the prerequisites. A typical CI job includes:

```powershell
# Restore tools (if using a local manifest)
 dotnet tool restore

# Restore and build the solution
 dotnet restore
 dotnet build --configuration Release
```

On each run the EF Core models are regenerated only when the DACPAC or EF Core Power Tools inputs change.

Ensure that the build agent has the necessary SQL Server Data Tools components to build the `.sqlproj` to a DACPAC.

---

## 8. Troubleshooting

### 8.1 Generated models do not appear

- Confirm that `EfcptEnabled` is `true` for the project.
- Verify that the `.sqlproj` can be built independently (for example, by opening it in Visual Studio or running `dotnet msbuild` directly).
- If discovery fails, set `EfcptSqlProj` explicitly to the full path of the `.sqlproj`.
- Increase logging verbosity by setting `EfcptLogVerbosity` to `detailed` and inspect the build output.
- Check that `EfcptGeneratedDir` exists after the build and that it contains `.g.cs` files.

### 8.2 DACPAC build problems

- Ensure that either `msbuild.exe` (Windows) or `dotnet msbuild` is available.
- Install the SQL Server Data Tools / database build components on the machine running the build.
- Review the detailed build log from the `EnsureDacpacBuilt` task for underlying MSBuild errors.

### 8.3 `efcpt` CLI issues

- Run `dotnet tool list -g` or `dotnet tool list` (with a manifest) to confirm that `ErikEJ.EFCorePowerTools.Cli` is installed.
- If using a local tool manifest, set `EfcptToolMode` to `tool-manifest` to enforce its use.
- If needed, provide an explicit `EfcptToolPath` to the `efcpt` executable.
- Make sure the CLI version requested by `EfcptToolVersion` is compatible with your EF Core version.

### 8.4 Inspecting inputs and intermediate outputs

- Set `EfcptDumpResolvedInputs` to `true` to log how the `.sqlproj`, config, renaming file, and templates are resolved.
- Inspect the directory specified by `EfcptOutput` to see:
  - The staged `efcpt-config.json`.
  - The staged `efcpt.renaming.json`.
  - The staged `Template` folder used by EF Core Power Tools.
  - The fingerprint and stamp files that control incremental generation.

### 8.5 Test-only environment variables

This repository’s own tests use a few environment variables to simulate external tools and speed up test runs:

- `EFCPT_FAKE_BUILD` – simulates building the DACPAC without invoking a real database build.
- `EFCPT_FAKE_EFCPT` – simulates the `efcpt` CLI and writes deterministic sample output.
- `EFCPT_TEST_DACPAC` – points tests at a specific DACPAC.

These variables are intended for internal tests and should not be used in production builds.

---

## 9. Development and testing

To run the repository’s test suite:

```powershell
 dotnet test
```

The tests include end-to-end coverage that:

- Builds a real SQL Server Database Project from `tests/TestAssets/SampleDatabase` to a DACPAC.
- Runs the EF Core Power Tools CLI through the `JD.Efcpt.Build` MSBuild tasks.
- Generates EF Core model code into a sample application under `obj/efcpt/Generated`.
- Verifies that the generated models contain DbSets and entities for multiple schemas and tables.

---

## 10. Support and feedback

For issues, questions, or feature requests, please open an issue in the Git repository where this project is hosted. Include relevant information such as:

- A short description of the problem.
- The `dotnet --info` output.
- The versions of `JD.Efcpt.Build` and `ErikEJ.EFCorePowerTools.Cli` you are using.
- Relevant sections of the MSBuild log with `EfcptLogVerbosity` set to `detailed`.

`JD.Efcpt.Build` is intended to be suitable for enterprise and FOSS usage. Contributions in the form of bug reports, documentation improvements, and pull requests are welcome, subject to the project’s contribution guidelines and license.
