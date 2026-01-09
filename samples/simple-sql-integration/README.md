# Simple SQL Integration Sample

This sample demonstrates the **simple integration workflow** where you add JD.Efcpt.Build to an existing SQL project, and it automatically discovers and builds downstream EF Core projects that reference it.

## What This Demonstrates

- **Zero-configuration setup**: Just add JD.Efcpt.Build to your SQL project
- **Automatic discovery**: Finds EF Core projects that reference the SQL project
- **Build orchestration**: Building the SQL project automatically triggers EF Core generation in downstream projects
- **Clean separation**: SQL project owns the schema, EF Core project owns the data access code

## The Problem This Solves

Before this feature, users had a workflow like this:

1. Make changes to the SQL project
2. Build the SQL project
3. Run a script to trigger EF Core generation in the data access project:

```powershell
$dacPacPath = "..\DatabaseProject\bin\Debug\net10.0\DatabaseProject.dacpac"
dotnet build ..\DatabaseProject
efcpt $dacPacPath
```

This was tedious and error-prone, especially in CI/CD pipelines.

## The Solution

Now, you can simply:

1. Add `JD.Efcpt.Build` package to your SQL project
2. Build your SQL project
3. Done! Downstream projects are automatically discovered and built

```
SQL Project Build
  ↓ Creates DACPAC
  ↓ Discovers DataAccess project (references SQL project + has JD.Efcpt.Build)
  ↓ Builds DataAccess project
  ↓ EF Core models generated automatically
```

## Project Structure

```
simple-sql-integration/
├── DatabaseProject/              # SQL Project
│   ├── DatabaseProject.csproj   # MSBuild.Sdk.SqlProj with JD.Efcpt.Build
│   ├── Tables/                   # SQL scripts
│   │   ├── Categories.sql
│   │   ├── Products.sql
│   │   ├── Orders.sql
│   │   └── OrderItems.sql
│   └── bin/Debug/               # DACPAC output
│
└── DataAccessProject/            # EF Core Project
    ├── DataAccessProject.csproj # References DatabaseProject + JD.Efcpt.Build
    └── obj/efcpt/Generated/     # Generated EF Core models
```

## Key Configuration

### DatabaseProject (SQL Project)

```xml
<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <SqlServerVersion>Sql160</SqlServerVersion>
    </PropertyGroup>

    <ItemGroup>
        <!-- This triggers automatic downstream discovery -->
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
    </ItemGroup>
</Project>
```

**What happens when you build:**
1. SQL project builds to DACPAC (as normal)
2. JD.Efcpt.Build detects this is a SQL project
3. Searches solution directory for projects that:
   - Reference this SQL project
   - Have JD.Efcpt.Build package OR efcpt-config.json
4. Builds each discovered project
5. EF Core generation happens automatically in those projects

### DataAccessProject (EF Core Project)

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <!-- Reference the SQL project to get DACPAC -->
        <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
        </ProjectReference>

        <!-- Enable EF Core generation -->
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
        
        <!-- EF Core dependencies -->
        <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
    </ItemGroup>
</Project>
```

**What happens:**
- Project is discovered because it references DatabaseProject AND has JD.Efcpt.Build
- When built, JD.Efcpt.Build finds the DatabaseProject DACPAC
- Generates EF Core models from the DACPAC
- Models are compiled into the project

## Building the Sample

```bash
# Just build the SQL project - everything else happens automatically!
dotnet build DatabaseProject

# Or build both explicitly
dotnet build
```

The first build will:
1. Build DatabaseProject to create the DACPAC
2. Discover that DataAccessProject references DatabaseProject
3. Build DataAccessProject, which generates EF Core models

Subsequent builds use incremental builds and only regenerate when:
- SQL schema changes (DACPAC changes)
- efcpt-config.json changes
- MSBuild property overrides change

## Configuration Options

### Disable Automatic Discovery

If you want to disable automatic downstream building:

```xml
<!-- In DatabaseProject -->
<PropertyGroup>
    <EfcptTriggerDownstream>false</EfcptTriggerDownstream>
</PropertyGroup>
```

### Explicit Downstream Projects

Instead of auto-discovery, specify projects explicitly:

```xml
<!-- In DatabaseProject -->
<PropertyGroup>
    <EfcptDownstreamProjects>..\DataAccessProject\DataAccessProject.csproj;..\AnotherProject\AnotherProject.csproj</EfcptDownstreamProjects>
</PropertyGroup>
```

### Custom Search Paths

Add additional directories to search for projects:

```xml
<!-- In DatabaseProject -->
<PropertyGroup>
    <EfcptDownstreamSearchPaths>..\src;..\tests</EfcptDownstreamSearchPaths>
</PropertyGroup>
```

### Disable Auto-Discovery (but still trigger explicit projects)

```xml
<!-- In DatabaseProject -->
<PropertyGroup>
    <EfcptDownstreamAutoDiscover>false</EfcptDownstreamAutoDiscover>
    <EfcptDownstreamProjects>..\DataAccessProject\DataAccessProject.csproj</EfcptDownstreamProjects>
</PropertyGroup>
```

## How It Works

### Detection

JD.Efcpt.Build detects when it's in a SQL project by checking:
1. Project SDK attribute (Microsoft.Build.Sql or MSBuild.Sdk.SqlProj)
2. MSBuild properties (SqlServerVersion or DSP for legacy SSDT)

### Discovery

When `EfcptTriggerDownstream` is enabled (default in SQL projects):
1. Walks up directory tree to find solution root
2. Searches for all `.csproj` files
3. Filters to those that:
   - Reference the SQL project (via ProjectReference)
   - Have JD.Efcpt.Build package OR efcpt-config.json file

### Build Orchestration

1. SQL project builds first (creates DACPAC)
2. Discovered downstream projects are built sequentially
3. Each downstream project's build triggers EF Core generation
4. MSBuild's dependency graph ensures proper ordering

## Benefits

✅ **No manual scripts** - Build orchestration is automatic
✅ **CI/CD friendly** - Just `dotnet build` the SQL project
✅ **Incremental builds** - Only regenerates when needed
✅ **Multiple projects** - Works with multiple downstream projects
✅ **Zero configuration** - Works out of the box with sensible defaults
✅ **Customizable** - Override discovery behavior as needed

## Comparison with Other Approaches

### Old Manual Approach
```powershell
# Build SQL project
dotnet build DatabaseProject

# Run efcpt manually
$dacpac = "DatabaseProject\bin\Debug\net9.0\DatabaseProject.dacpac"
efcpt $dacpac

# Or build data project explicitly
dotnet build DataAccessProject
```

### New Automatic Approach
```bash
# Just build the SQL project!
dotnet build DatabaseProject
```

### Alternative: Database-First SQL Generation

The [database-first-sql-generation sample](../database-first-sql-generation/) shows a different workflow where the SQL project ALSO extracts schema from a live database. This sample focuses purely on the simple case where you already have SQL scripts.

## Troubleshooting

### Build Warnings About Missing ProjectReference

If you see a warning like:

```
warning : EFCPT project 'path/to/Project.csproj' will be affected by SQL project generation 
but lacks a ProjectReference to ensure proper build ordering.
```

This means the SQL project discovered an EFCPT-enabled project (has JD.Efcpt.Build or efcpt-config.json) but that project doesn't have a ProjectReference to the SQL project.

**Why this matters**: Without a ProjectReference, MSBuild cannot guarantee the SQL project builds first, which may cause:
- Build failures due to missing DACPAC files
- Outdated DACPAC references
- Race conditions in parallel builds

**Solution**: The warning message includes a ready-to-use code snippet with the correct relative path. Copy and paste it into your EFCPT project's `.csproj` file. For example:

```xml
<ItemGroup>
  <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
</ItemGroup>
```

The `ReferenceOutputAssembly=false` setting ensures you get the build ordering benefit without adding an assembly reference overhead.

## See Also

- [Database-First SQL Generation](../database-first-sql-generation/) - Extract SQL scripts from database AND trigger downstream
- [SDK Zero Config](../sdk-zero-config/) - Minimal SDK project setup
- [Main Documentation](../../docs/) - Complete JD.Efcpt.Build documentation
