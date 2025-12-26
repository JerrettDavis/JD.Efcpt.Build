# Advanced Topics

This guide covers advanced patterns and configuration scenarios for JD.Efcpt.Build.

## Multi-Project Solutions

In solutions with multiple projects that need EF Core model generation, you can centralize configuration using `Directory.Build.props`.

### Shared Configuration

Create a `Directory.Build.props` file at the solution root:

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- Enable for all projects by default -->
    <EfcptEnabled>true</EfcptEnabled>

    <!-- Consistent tool configuration -->
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolPackageId>ErikEJ.EFCorePowerTools.Cli</EfcptToolPackageId>
    <EfcptToolVersion>10.*</EfcptToolVersion>

    <!-- Logging -->
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

### Disabling for Specific Projects

Some projects may not need model generation. Disable it explicitly:

```xml
<!-- src/Tests/Tests.csproj -->
<PropertyGroup>
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

Or conditionally disable for test projects:

```xml
<!-- Directory.Build.props -->
<PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

## Configuration-Based Switching

### Different Configurations per Environment

Use MSBuild conditions to switch database sources by configuration:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <EfcptConnectionString>Server=localhost;Database=MyDb_Dev;Integrated Security=True;</EfcptConnectionString>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <EfcptSqlProj>..\database\MyDatabase.sqlproj</EfcptSqlProj>
</PropertyGroup>
```

### Disable for Specific Configurations

Disable model generation entirely for certain configurations:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

## Working with Multiple Databases

### Generating from Multiple Sources

If you need models from multiple databases, create separate projects:

```
MySolution/
├── src/
│   ├── MyApp.Core/           # Business logic
│   ├── MyApp.Data.Primary/   # Primary database models
│   │   └── efcpt-config.json
│   └── MyApp.Data.Reporting/ # Reporting database models
│       └── efcpt-config.json
└── database/
    ├── Primary.sqlproj
    └── Reporting.sqlproj
```

Each data project has its own configuration:

```xml
<!-- MyApp.Data.Primary.csproj -->
<PropertyGroup>
  <!-- SQL Project with .sqlproj extension -->
  <EfcptSqlProj>..\..\database\Primary.sqlproj</EfcptSqlProj>
</PropertyGroup>

<!-- MyApp.Data.Reporting.csproj -->
<PropertyGroup>
  <!-- MSBuild.Sdk.SqlProj project (uses .csproj extension) -->
  <EfcptSqlProj>..\..\database\Reporting.csproj</EfcptSqlProj>
</PropertyGroup>
```

## Custom Output Locations

### Changing the Generated Directory

By default, files are generated in `obj/efcpt/Generated/`. To change this:

```xml
<PropertyGroup>
  <EfcptOutput>$(MSBuildProjectDirectory)\obj\custom-efcpt\</EfcptOutput>
  <EfcptGeneratedDir>$(EfcptOutput)CustomGenerated\</EfcptGeneratedDir>
</PropertyGroup>
```

### Generating to the Project Directory

While not recommended (generated files should typically be in `obj/`), you can generate to the project:

```xml
<PropertyGroup>
  <EfcptGeneratedDir>$(MSBuildProjectDirectory)\Generated\</EfcptGeneratedDir>
</PropertyGroup>
```

> [!WARNING]
> Generating to the project directory means files will be included in source control unless explicitly ignored. The default `obj/efcpt/` location is recommended.

## Renaming Rules

Use `efcpt.renaming.json` to customize table and column names. The file is a JSON array organized by schema:

```json
[
  {
    "SchemaName": "dbo",
    "Tables": [
      {
        "Name": "tblUsers",
        "NewName": "User",
        "Columns": [
          {
            "Name": "usr_id",
            "NewName": "Id"
          },
          {
            "Name": "usr_email",
            "NewName": "Email"
          }
        ]
      },
      {
        "Name": "tblOrders",
        "NewName": "Order"
      }
    ],
    "UseSchemaName": false
  }
]
```

### Resolution Order

Renaming files are resolved in this order:

1. `<EfcptRenaming>` property (if set)
2. `efcpt.renaming.json` in project directory
3. `efcpt.renaming.json` in solution directory
4. Package default (empty renaming rules)

## Diagnostic Logging

### Enabling Detailed Logs

For troubleshooting, enable detailed logging:

```xml
<PropertyGroup>
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
  <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
</PropertyGroup>
```

This outputs:
- All resolved input paths
- Fingerprint computation details
- CLI invocation commands
- Detailed error messages

### Inspecting Resolved Inputs

When `EfcptDumpResolvedInputs` is `true`, a `resolved-inputs.json` file is written to `obj/efcpt/`:

```json
{
  "sqlProjPath": "..\\database\\MyDatabase.sqlproj",
  "configPath": "efcpt-config.json",
  "renamingPath": "efcpt.renaming.json",
  "templateDir": "Template",
  "connectionString": null,
  "useConnectionString": false
}
```

## Working with DACPAC Build

### Using a Pre-built DACPAC

If you have a pre-built DACPAC file, you can point to it directly:

```xml
<PropertyGroup>
  <EfcptDacpac>path\to\MyDatabase.dacpac</EfcptDacpac>
</PropertyGroup>
```

When `EfcptDacpac` is set, the package skips the .sqlproj build step and uses the specified DACPAC directly.

### DACPAC Build Configuration

Control how the .sqlproj is built:

```xml
<PropertyGroup>
  <!-- Use specific MSBuild executable -->
  <EfcptMsBuildExe>C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe</EfcptMsBuildExe>

  <!-- Use specific dotnet executable -->
  <EfcptDotNetExe>C:\dotnet\dotnet.exe</EfcptDotNetExe>
</PropertyGroup>
```

## Modern SQL SDK Projects

JD.Efcpt.Build supports modern SQL SDK projects that use **Microsoft.Build.Sql** or **MSBuild.Sdk.SqlProj**:

### Microsoft.Build.Sql SDK

**Microsoft.Build.Sql** is Microsoft's official SDK for building SQL Server Database Projects with the .NET SDK:

```xml
<!-- SQL Project using Microsoft.Build.Sql SDK -->
<Project Sdk="Microsoft.Build.Sql/0.1.12-preview">
  <PropertyGroup>
    <Name>DatabaseProject</Name>
    <TargetFramework>netstandard2.1</TargetFramework>
    <SqlServerVersion>Sql160</SqlServerVersion>
  </PropertyGroup>
</Project>
```

**File extension:** `.sqlproj`

**SDK:** Microsoft.Build.Sql (official Microsoft SDK)

**Requirements:** .NET SDK only (no SQL Server Data Tools or Visual Studio required)

**Key Features:**
- Official Microsoft support
- Cross-platform with .NET SDK
- Standard `.sqlproj` file extension
- Modern SDK-style project format

### MSBuild.Sdk.SqlProj SDK

**MSBuild.Sdk.SqlProj** is a community-maintained SDK that provides similar functionality with additional configurability and extensibility:

```xml
<!-- SQL Project using MSBuild.Sdk.SqlProj SDK -->
<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">
  <PropertyGroup>
    <Name>DatabaseProject</Name>
    <TargetFramework>netstandard2.1</TargetFramework>
    <SqlServerVersion>Sql160</SqlServerVersion>
  </PropertyGroup>
</Project>
```

**File extension:** `.csproj` or `.fsproj` (NOT `.sqlproj`)

**SDK:** MSBuild.Sdk.SqlProj (community-maintained NuGet package)

**Requirements:** .NET SDK only

**Key Features:**
- Additional configurability and extensibility
- Cross-platform with .NET SDK
- Uses `.csproj` or `.fsproj` file extensions despite the SDK name
- More similar to legacy .NET Framework `.sqlproj` projects in some behaviors

### Legacy .sqlproj Projects

JD.Efcpt.Build also supports legacy SQL Server Database Projects that use the traditional .NET Framework format:

```xml
<!-- Legacy .NET Framework SQL Project -->
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003" 
         ToolsVersion="4.0">
  <PropertyGroup>
    <Name>DatabaseProject</Name>
    <SqlServerVersion>Sql160</SqlServerVersion>
  </PropertyGroup>
</Project>
```

**File extension:** `.sqlproj`

**Format:** Traditional MSBuild format (not SDK-style)

**Requirements:** SQL Server Data Tools (SSDT) or Visual Studio with database tooling

### Using Different SQL Project Types

When referencing SQL Projects in JD.Efcpt.Build, specify the project file path:

```xml
<PropertyGroup>
  <!-- Microsoft.Build.Sql or legacy .sqlproj -->
  <EfcptSqlProj>..\..\database\MyDatabase\MyDatabase.sqlproj</EfcptSqlProj>
  
  <!-- MSBuild.Sdk.SqlProj (uses .csproj extension) -->
  <EfcptSqlProj>..\..\database\MyDatabase\MyDatabase.csproj</EfcptSqlProj>
</PropertyGroup>
```

The package automatically detects the project type and handles it appropriately.

## Excluding Tables and Schemas

Use `efcpt-config.json` to control what's included in generation:

```json
{
  "table-selection": [
    {
      "schema": "dbo",
      "include": true
    },
    {
      "schema": "audit",
      "include": false
    },
    {
      "schema": "dbo",
      "tables": ["__EFMigrationsHistory"],
      "include": false
    }
  ]
}
```

This includes all `dbo` schema tables except `__EFMigrationsHistory`, and excludes the entire `audit` schema.

## Handling Large Databases

### Selecting Specific Tables

For large databases, explicitly select tables to generate:

```json
{
  "table-selection": [
    {
      "schema": "dbo",
      "tables": ["Users", "Orders", "Products"],
      "include": true
    }
  ]
}
```

### Splitting by Schema

Use schema-based organization to manage large models:

```json
{
  "file-layout": {
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  }
}
```

## Error Recovery

### Handling Failed Builds

If model generation fails, previous generated files remain. To start fresh:

```bash
# Delete intermediate directory
rmdir /s /q obj\efcpt

# Clean build
dotnet clean
dotnet build
```

### Inspecting Build Logs

Check MSBuild logs for detailed error information:

```bash
dotnet build /v:detailed > build.log
```

Look for `JD.Efcpt.Build` entries in the log.

## Source Control Integration

### Recommended .gitignore

Add these patterns to your `.gitignore`:

```gitignore
# JD.Efcpt.Build generated files
obj/efcpt/
*.g.cs
```

### Checking in Generated Files

If you need to check in generated files (not recommended), generate to a project directory:

```xml
<PropertyGroup>
  <EfcptGeneratedDir>$(MSBuildProjectDirectory)\Generated\</EfcptGeneratedDir>
</PropertyGroup>
```

Remove `*.g.cs` from `.gitignore`.

## Performance Optimization

### Reducing Build Time

1. **Use fingerprinting** - Don't delete `obj/efcpt/` unnecessarily
2. **Use connection string mode** - Skips DACPAC build step
3. **Select specific tables** - Don't generate unused entities
4. **Use parallel builds** - The package supports parallel project builds

### Caching in CI/CD

Cache the `obj/efcpt/` directory between builds to avoid regeneration:

```yaml
# GitHub Actions
- uses: actions/cache@v3
  with:
    path: |
      **/obj/efcpt/
    key: efcpt-${{ hashFiles('**/*.sqlproj') }}-${{ hashFiles('**/efcpt-config.json') }}
```

## Next Steps

- [Troubleshooting](troubleshooting.md) - Solve common problems
- [API Reference](api-reference.md) - Complete property and task reference
- [CI/CD Integration](ci-cd.md) - Deploy in automated pipelines
