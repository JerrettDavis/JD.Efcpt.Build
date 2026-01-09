# Simple SQL Integration

JD.Efcpt.Build now supports a **simple integration workflow** where you add the package to your SQL project, and it automatically discovers and builds downstream EF Core projects that reference it.

This feature eliminates the need for manual build scripts and provides a seamless developer experience.

## Overview

**The Problem:**

Previously, if you had a SQL project and an EF Core project, you needed to manually coordinate builds:

1. Build the SQL project to create the DACPAC
2. Run a script (or manually build) the EF Core project to generate models

**The Solution:**

Now, simply add JD.Efcpt.Build to your SQL project, and it will:

1. Automatically discover EF Core projects that reference the SQL project
2. Build those projects after the SQL project completes
3. Trigger EF Core model generation automatically

## Quick Start

### 1. Add to SQL Project

```xml
<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <!-- This enables automatic downstream discovery -->
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
    </ItemGroup>
</Project>
```

### 2. Configure EF Core Project

Your EF Core project should:
- Reference the SQL project (via `<ProjectReference>`)
- Have JD.Efcpt.Build installed OR have an `efcpt-config.json` file

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <ItemGroup>
        <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
        </ProjectReference>
        
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
        <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
    </ItemGroup>
</Project>
```

### 3. Build

```bash
# Build the SQL project - everything else happens automatically!
dotnet build DatabaseProject
```

The build will:
1. Build `DatabaseProject` to create the DACPAC
2. Discover that `DataAccessProject` references it
3. Build `DataAccessProject`, generating EF Core models

## How It Works

### Detection

When you build a SQL project with JD.Efcpt.Build, it:

1. **Detects SQL Project**: Checks if the current project is a SQL project (by SDK or properties)
2. **Searches for Downstream Projects**: Walks up to the solution root and searches for all `.csproj` files
3. **Filters Candidates**: Keeps only projects that:
   - Reference the SQL project (via `<ProjectReference>`)
   - Have JD.Efcpt.Build package OR `efcpt-config.json` file
4. **Builds Downstream Projects**: Sequentially builds each discovered project

### Discovery Criteria

A project is considered a downstream candidate if it:

- **References the SQL project**: Has a `<ProjectReference>` to the SQL project
- **Has EF Core generation configured**: Either:
  - Has `JD.Efcpt.Build` package installed, OR
  - Has an `efcpt-config.json` file in the project directory

This allows for flexible configuration while ensuring only intentional projects are built.

## Configuration

### Disable Automatic Discovery

If you want to disable automatic downstream building:

```xml
<!-- In SQL Project -->
<PropertyGroup>
    <EfcptTriggerDownstream>false</EfcptTriggerDownstream>
</PropertyGroup>
```

### Explicit Downstream Projects

Instead of auto-discovery, specify projects explicitly:

```xml
<!-- In SQL Project -->
<PropertyGroup>
    <EfcptDownstreamProjects>
        ..\DataAccessProject\DataAccessProject.csproj;
        ..\AnotherProject\AnotherProject.csproj
    </EfcptDownstreamProjects>
</PropertyGroup>
```

When explicit projects are specified, auto-discovery is bypassed.

### Disable Auto-Discovery (Keep Explicit Projects)

```xml
<!-- In SQL Project -->
<PropertyGroup>
    <EfcptDownstreamAutoDiscover>false</EfcptDownstreamAutoDiscover>
    <EfcptDownstreamProjects>..\DataAccessProject\DataAccessProject.csproj</EfcptDownstreamProjects>
</PropertyGroup>
```

### Custom Search Paths

Add additional directories to search for projects:

```xml
<!-- In SQL Project -->
<PropertyGroup>
    <EfcptDownstreamSearchPaths>..\src;..\tests;..\integrations</EfcptDownstreamSearchPaths>
</PropertyGroup>
```

## Configuration Reference

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptTriggerDownstream` | `true` | Enable/disable automatic downstream discovery and building |
| `EfcptDownstreamProjects` | (empty) | Explicit semicolon-separated list of downstream projects. Overrides auto-discovery. |
| `EfcptDownstreamAutoDiscover` | `true` | Enable/disable automatic discovery. When `false`, only `EfcptDownstreamProjects` are used. |
| `EfcptDownstreamSearchPaths` | (empty) | Additional semicolon-separated directories to search for projects |

## Build Behavior

### Build Order

1. **SQL Project Builds**: Creates DACPAC (normal SQL project build)
2. **Discovery Runs**: After SQL project completes
3. **Downstream Projects Build**: Each discovered project builds sequentially
4. **EF Core Generation**: Happens during each downstream project's build

### Incremental Builds

- **SQL Project**: Uses normal SQL project incremental build
- **Downstream Projects**: Use JD.Efcpt.Build's fingerprinting
  - Only regenerate models when:
    - DACPAC changes (schema changed)
    - Configuration changes
    - MSBuild property overrides change

### Parallel Builds

Downstream projects are built **sequentially** (not in parallel) to avoid race conditions and resource contention during EF Core generation.

## Use Cases

### Single Data Access Project

The most common scenario:

```
Solution/
├── DatabaseProject/      (SQL project with JD.Efcpt.Build)
└── DataAccessProject/    (EF Core project referencing DatabaseProject)
```

**Build command**: `dotnet build DatabaseProject`

### Multiple Data Access Projects

Multiple projects can reference the same SQL project:

```
Solution/
├── DatabaseProject/          (SQL project)
├── DataAccess.Core/          (Common models)
├── DataAccess.Admin/         (Admin-specific models)
└── DataAccess.Public/        (Public API models)
```

All three projects will be discovered and built when `DatabaseProject` builds.

### Test Projects

Test projects can also be downstream:

```
Solution/
├── DatabaseProject/          (SQL project)
├── DataAccessProject/        (Main EF Core project)
└── DataAccessProject.Tests/  (Test project with efcpt-config.json)
```

The test project will be discovered if it references `DatabaseProject` and has `efcpt-config.json`.

## Comparison with Other Approaches

### vs. Manual Builds

**Before (Manual)**:
```powershell
dotnet build DatabaseProject
dotnet build DataAccessProject  # Must remember to do this
```

**After (Automatic)**:
```bash
dotnet build DatabaseProject  # Triggers everything
```

### vs. Build Scripts

**Before (Script)**:
```powershell
$dacpac = "..\DatabaseProject\bin\Debug\net10.0\DatabaseProject.dacpac"
dotnet build DatabaseProject
efcpt $dacpac
```

**After (Integrated)**:
- No script needed
- Works everywhere (CI/CD, IDE, command line)
- Respects MSBuild build order

### vs. Database-First SQL Generation

The [database-first-sql-generation](../../samples/database-first-sql-generation/) feature extracts SQL scripts from a live database. This simple integration feature works with existing SQL scripts.

You can use **both** features together:
1. SQL project extracts schema from database
2. SQL project builds to DACPAC
3. Downstream projects generate EF Core models

## Troubleshooting

### No Downstream Projects Discovered

**Symptoms**: Build completes but no downstream projects are built.

**Causes**:
1. Downstream project doesn't reference SQL project
2. Downstream project doesn't have JD.Efcpt.Build or efcpt-config.json
3. Solution directory not found

**Solutions**:
- Verify `<ProjectReference>` exists in downstream project
- Add JD.Efcpt.Build or efcpt-config.json to downstream project
- Use `EfcptDownstreamProjects` for explicit configuration

### Warning: Project Lacks ProjectReference

**Symptom**: Build warning about EFCPT project lacking ProjectReference to SQL project.

**Cause**: The SQL project discovered an EFCPT-enabled project (has JD.Efcpt.Build or efcpt-config.json) but that project doesn't have a ProjectReference to the SQL project. Without this reference, MSBuild cannot guarantee the SQL project builds first, which may cause build failures or outdated DACPAC references.

**Solution**: Add the ProjectReference snippet shown in the warning message to your EFCPT project. The warning includes a ready-to-use code snippet with the correct relative path. For example:

```xml
<ItemGroup>
  <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
</ItemGroup>
```

The `ReferenceOutputAssembly=false` setting ensures you get the build ordering benefit without adding an assembly reference overhead.

### Downstream Project Built Twice

**Symptoms**: Downstream project appears to build twice.

**Cause**: Downstream project also has direct project reference to SQL project, AND is discovered by automatic discovery.

**Solution**: This is usually fine - MSBuild handles duplicate builds efficiently.

### Configuration Property Not Working

**Symptoms**: Set `EfcptTriggerDownstream=false` but downstream projects still build.

**Cause**: Property set in wrong project or wrong location.

**Solution**: Ensure property is set in the **SQL project**, not the downstream project.

## See Also

- [Sample: simple-sql-integration](../../samples/simple-sql-integration/) - Working example
- [Database-First SQL Generation](../../samples/database-first-sql-generation/) - Combine with database extraction
- [API Reference](api-reference.md) - Complete property reference
- [Getting Started](getting-started.md) - General setup guide
