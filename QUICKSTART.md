# Quick Reference Guide

## Installation

### Option 1: Quick Start (Global Tool)
```bash
dotnet add package JD.Efcpt.Build
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "10.*"
dotnet build
```

### Option 2: Team/CI Recommended (Local Tool)
```bash
dotnet add package JD.Efcpt.Build
dotnet new tool-manifest  # if not exists
dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
dotnet build
```

---

## Common Scenarios

### Scenario 1: Simple Database-First Project

**Project structure:**
```
MySolution/
├── src/MyApp/MyApp.csproj
└── database/MyDb/MyDb.sqlproj   # Microsoft.Build.Sql format
```

**MyApp.csproj:**
```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
</ItemGroup>

<PropertyGroup>
  <EfcptSqlProj>..\..\database\MyDb\MyDb.sqlproj</EfcptSqlProj>
</PropertyGroup>
```

**Build:**
```bash
dotnet build
```

**Result:** DbContext and entities in `obj/efcpt/Generated/`

---

### Scenario 2: Custom Namespaces

**efcpt-config.json:**
```json
{
  "names": {
    "root-namespace": "MyCompany.Data",
    "dbcontext-name": "AppDbContext",
    "dbcontext-namespace": "MyCompany.Data.Context",
    "entity-namespace": "MyCompany.Data.Entities"
  }
}
```

---

### Scenario 3: Schema-Based Organization

**efcpt-config.json:**
```json
{
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
    },
    {
      "schema": "sales",
      "include": true
    }
  ]
}
```

**Result:**
```
obj/efcpt/Generated/
├── AppDbContext.g.cs
└── Models/
    ├── dbo/
    │   └── User.g.cs
    └── sales/
        └── Customer.g.cs
```

---

### Scenario 4: T4 Template Customization

**1. Create template directory:**
```
MyApp/
└── Template/
    └── CodeTemplates/
        └── EFCore/
            ├── DbContext.t4
            └── EntityType.t4
```

**2. Configure in efcpt-config.json:**
```json
{
  "code-generation": {
    "use-t4": true,
    "t4-template-path": "."
  }
}
```

**3. Build:**
```bash
dotnet build
```

Templates automatically staged to `obj/efcpt/Generated/CodeTemplates/`

---

### Scenario 5: Multi-Project Solution

**Directory.Build.props (at solution root):**
```xml
<Project>
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolVersion>10.*</EfcptToolVersion>
  </PropertyGroup>
</Project>
```

**Each project's .csproj:**
```xml
<PropertyGroup>
  <EfcptSqlProj>..\..\database\MyDb\MyDb.sqlproj</EfcptSqlProj>
</PropertyGroup>
```

---

### Scenario 6: Disable for Debug Builds

**YourApp.csproj:**
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

---

### Scenario 7: CI/CD Pipeline

**GitHub Actions (.github/workflows/build.yml):**
```yaml
name: Build
on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - run: dotnet tool restore
    - run: dotnet restore
    - run: dotnet build --configuration Release --no-restore
    - run: dotnet test --configuration Release --no-build
```

**Azure DevOps (azure-pipelines.yml):**
```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- script: dotnet tool restore
  displayName: 'Restore tools'

- script: dotnet restore
  displayName: 'Restore packages'

- script: dotnet build --configuration Release --no-restore
  displayName: 'Build'
```

---

### Scenario 8: Detailed Logging for Debugging

**YourApp.csproj:**
```xml
<PropertyGroup>
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
  <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
</PropertyGroup>
```

**Build:**
```bash
dotnet build -v detailed > build.log 2>&1
```

---

### Scenario 9: Table Renaming

**efcpt.renaming.json:**
```json
{
  "tables": [
    {
      "name": "tblUsers",
      "newName": "User"
    },
    {
      "name": "tblOrders",
      "newName": "Order"
    }
  ],
  "columns": [
    {
      "table": "User",
      "name": "usr_id",
      "newName": "Id"
    },
    {
      "table": "User",
      "name": "usr_name",
      "newName": "Name"
    }
  ]
}
```

---

## Troubleshooting Quick Fixes

### Issue: Generated files don't appear

**Quick Fix:**
```bash
dotnet clean
rmdir /s /q obj\efcpt
dotnet build
```

### Issue: "efcpt not found"

**Quick Fix:**
```bash
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "10.*"
# or
dotnet tool restore
```

### Issue: DACPAC build fails

**Quick Fix:**
```bash
# Test SQL Project independently
dotnet build path\to\Database.sqlproj
# or for MSBuild.Sdk.SqlProj projects (.csproj)
dotnet build path\to\Database.csproj
```

### Issue: Old schema still generating

**Quick Fix:**
```bash
# Force full regeneration
rmdir /s /q obj\efcpt
dotnet build
```

### Issue: Template duplication

**Quick Fix:**
```bash
# Update to latest version
dotnet add package JD.Efcpt.Build --version x.x.x
dotnet clean
dotnet build
```

---

## Property Quick Reference

### Most Common Properties

| Property | Use When | Example |
|----------|----------|---------|
| `EfcptSqlProj` | SQL Project not auto-discovered | `..\..\db\MyDb.sqlproj` |
| `EfcptConfig` | Using custom config file name | `my-config.json` |
| `EfcptTemplateDir` | Using custom template location | `CustomTemplates` |
| `EfcptLogVerbosity` | Debugging issues | `detailed` |
| `EfcptEnabled` | Conditionally disable generation | `false` |

### Tool Configuration

| Property | Use When | Example |
|----------|----------|---------|
| `EfcptToolMode` | Force local/global tool | `tool-manifest` |
| `EfcptToolVersion` | Pin specific version | `10.0.1055` |
| `EfcptToolPath` | Using custom efcpt location | `C:\tools\efcpt.exe` |

---

## Command Cheat Sheet

```bash
# Clean build
dotnet clean && dotnet build

# Force regeneration
rmdir /s /q obj\efcpt && dotnet build

# Detailed logging
dotnet build -v detailed

# Check tool installation
dotnet tool list --global
dotnet tool list

# Install/update efcpt
dotnet tool install -g ErikEJ.EFCorePowerTools.Cli --version "10.*"
dotnet tool update -g ErikEJ.EFCorePowerTools.Cli

# Local tool (team/CI)
dotnet new tool-manifest
dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
dotnet tool restore

# Check package version
dotnet list package | findstr JD.Efcpt.Build

# Update package
dotnet add package JD.Efcpt.Build --version x.x.x
```

---

## File Locations Reference

### Default Paths

```
YourProject/
├── efcpt-config.json              # Main configuration (optional)
├── efcpt.renaming.json            # Renaming rules (optional)
├── Template/                      # Custom templates (optional)
│   └── CodeTemplates/
│       └── EFCore/
│           ├── DbContext.t4
│           └── EntityType.t4
└── obj/
    └── efcpt/                     # Intermediate directory
        ├── efcpt-config.json      # Staged config
        ├── efcpt.renaming.json    # Staged renaming
        ├── fingerprint.txt        # Change detection
        ├── .efcpt.stamp          # Generation marker
        └── Generated/             # Generated code
            ├── YourDbContext.g.cs
            ├── CodeTemplates/     # Staged templates
            │   └── EFCore/
            └── Models/            # Entities
                └── dbo/
                    └── User.g.cs
```

---

## Common Patterns

### Pattern: Development vs Production Config

```xml
<!-- YourApp.csproj -->

<!-- Development: detailed logging -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
  <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
</PropertyGroup>

<!-- Production: minimal logging -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <EfcptLogVerbosity>minimal</EfcptLogVerbosity>
</PropertyGroup>
```

### Pattern: Environment-Specific Databases

```xml
<!-- YourApp.csproj -->

<PropertyGroup Condition="'$(Environment)' == 'Development'">
  <EfcptSqlProj>..\..\database\Dev\Dev.sqlproj</EfcptSqlProj>
</PropertyGroup>

<PropertyGroup Condition="'$(Environment)' == 'Production'">
  <EfcptSqlProj>..\..\database\Prod\Prod.sqlproj</EfcptSqlProj>
</PropertyGroup>
```

### Pattern: Shared Configuration

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolVersion>10.*</EfcptToolVersion>
  </PropertyGroup>
</Project>

<!-- YourApp.csproj -->
<PropertyGroup>
  <!-- Project-specific override -->
  <EfcptSqlProj>..\..\database\MyDb\MyDb.sqlproj</EfcptSqlProj>
</PropertyGroup>
```

---

**Need more help?** See [README.md](README.md) for comprehensive documentation.

