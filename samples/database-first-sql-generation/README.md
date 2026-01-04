# Database-First SQL Generation Sample

This sample demonstrates the **automatic database-first SQL project generation** feature where JD.Efcpt.Build automatically detects when it's referenced in a SQL project and generates SQL scripts from a live database.

## What This Demonstrates

- **Automatic SDK Detection**: JD.Efcpt.Build detects Microsoft.Build.Sql or MSBuild.Sdk.SqlProj SDKs
- **Two-Project Pattern**: Separate DatabaseProject (SQL) and DataAccessProject (EF Core)
- **Build Orchestration**: DatabaseProject builds first, creating DACPAC from generated SQL scripts
- **EF Core Integration**: DataAccessProject references DatabaseProject and generates models from its DACPAC

## Workflow

```
Live Database 
  ↓ (sqlpackage extract)
SQL Scripts (in DatabaseProject)
  ↓ (MSBuild.Sdk.SqlProj build)
DACPAC
  ↓ (EF Core Power Tools)
EF Core Models (in DataAccessProject)
```

## Project Structure

```
database-first-sql-generation/
├── DatabaseProject/
│   ├── DatabaseProject.csproj (MSBuild.Sdk.SqlProj)
│   └── [Generated SQL Scripts organized by schema/type]
└── DataAccessProject/
    ├── DataAccessProject.csproj
    └── [Generated EF Core Models]
```

## Key Configuration

### DatabaseProject (SQL Project)

```xml
<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">
    <PropertyGroup>
        <EfcptConnectionString>Server=...;Database=MyDb;...</EfcptConnectionString>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
    </ItemGroup>
</Project>
```

**What happens:**
1. JD.Efcpt.Build detects the SQL SDK (`MSBuild.Sdk.SqlProj`)
2. Connects to the database using `EfcptConnectionString`
3. Runs `sqlpackage /Action:Extract /p:ExtractTarget=Flat`
4. Generates organized SQL scripts (e.g., `dbo/Tables/Users.sql`, `dbo/Views/...`)
5. Adds auto-generation warnings to all SQL files
6. SQL project builds normally, creating a DACPAC

### DataAccessProject (EF Core)

```xml
<ItemGroup>
    <!-- Reference DatabaseProject to get its DACPAC -->
    <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
        <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    </ProjectReference>
    
    <PackageReference Include="JD.Efcpt.Build" Version="*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
</ItemGroup>
```

**What happens:**
1. MSBuild builds DatabaseProject first (project reference)
2. JD.Efcpt.Build finds the DatabaseProject DACPAC
3. Generates EF Core models from the DACPAC
4. Models are compiled into DataAccessProject

## How It Works

### Automatic Detection

JD.Efcpt.Build uses MSBuild properties to detect SQL projects:

- **Microsoft.Build.Sql**: Checks for `$(DSP)` property
- **MSBuild.Sdk.SqlProj**: Checks for `$(SqlServerVersion)` property

When detected, it runs SQL generation instead of EF Core generation.

### SQL Script Generation

1. **Query Schema**: Fingerprints the database schema
2. **Extract**: Uses `sqlpackage` to extract to flat SQL files
3. **Add Warnings**: Stamps each file with auto-generation header
4. **Build**: SQL project builds scripts into DACPAC

### Incremental Builds

- Schema fingerprinting prevents unnecessary regeneration
- Only re-extracts when database schema changes
- Fast subsequent builds

## Requirements

- .NET SDK 8.0+ (10.0 recommended)
- SQL Server or LocalDB with an existing database
- **For .NET 8-9**: Install sqlpackage globally: `dotnet tool install -g microsoft.sqlpackage`
- **For .NET 10+**: No installation needed - uses `dnx` automatically

## Building the Sample

1. **Set up a database**:
   ```bash
   # Create a LocalDB instance
   sqllocaldb create mssqllocaldb
   sqllocaldb start mssqllocaldb
   
   # Create test database with tables
   sqlcmd -S "(localdb)\mssqllocaldb" -Q "CREATE DATABASE EfcptSampleDb"
   sqlcmd -S "(localdb)\mssqllocaldb" -d EfcptSampleDb -Q "CREATE TABLE Users (Id INT PRIMARY KEY, Name NVARCHAR(100))"
   ```

2. **Update connection string**: Edit `DatabaseProject/DatabaseProject.csproj`

3. **Build**:
   ```bash
   # Build DatabaseProject - generates SQL scripts and DACPAC
   dotnet build DatabaseProject
   
   # Build DataAccessProject - generates EF Core models from DACPAC
   dotnet build DataAccessProject
   
   # Or build both:
   dotnet build
   ```

4. **Check generated files**:
   - SQL Scripts: `DatabaseProject/dbo/Tables/`, `DatabaseProject/dbo/Views/`, etc.
   - DACPAC: `DatabaseProject/bin/Debug/net10.0/DatabaseProject.dacpac`
   - EF Core Models: `DataAccessProject/obj/efcpt/Generated/`

## Customization

### Change Script Output Location

```xml
<PropertyGroup>
    <EfcptSqlScriptsDir>$(MSBuildProjectDirectory)\Schema\</EfcptSqlScriptsDir>
</PropertyGroup>
```

### SQL Server Version

```xml
<PropertyGroup>
    <SqlServerVersion>Sql160</SqlServerVersion> <!-- SQL Server 2022 -->
</PropertyGroup>
```

### Custom SqlPackage Version

```xml
<PropertyGroup>
    <EfcptSqlPackageToolVersion>162.3.566</EfcptSqlPackageToolVersion>
</PropertyGroup>
```

## Lifecycle Hooks

Extend the generation process with custom targets:

```xml
<!-- In DatabaseProject -->
<Target Name="BeforeSqlProjGeneration">
    <Message Text="About to generate SQL scripts..." />
</Target>

<Target Name="AfterSqlProjGeneration">
    <Message Text="SQL scripts generated!" />
</Target>
```

```xml
<!-- In DataAccessProject -->
<Target Name="BeforeEfcptGeneration">
    <Message Text="About to generate EF Core models..." />
</Target>

<Target Name="AfterEfcptGeneration">
    <Message Text="EF Core models generated!" />
</Target>
```

## Benefits

✅ **No manual project file creation** - JD.Efcpt.Build detects SQL projects automatically
✅ **Human-readable SQL artifacts** - Individual scripts for review and version control
✅ **Separation of concerns** - Database schema separate from data access code
✅ **Extensible** - Add custom scripts and seeded data to DatabaseProject
✅ **Deterministic** - Schema fingerprinting ensures consistent builds
✅ **Build orchestration** - MSBuild handles dependency order automatically

## Comparison with Old Approach

### Old Approach (Single Project)
- Set `<EfcptGenerateSqlProj>true</EfcptGenerateSqlProj>`
- Generated a separate SQL project in `obj/`
- Built that project internally
- More complex, less discoverable

### New Approach (Two Projects)
- Create standard SQL project
- Add `JD.Efcpt.Build` package reference
- Automatic detection and generation
- Natural MSBuild project references
- Cleaner, more maintainable

## See Also

- [Split Data and Models Sample](../split-data-and-models-between-multiple-projects/) - Similar two-project pattern for separating Models and Data
- [Microsoft.Build.Sql Zero Config](../microsoft-build-sql-zero-config/) - Traditional SQL project workflow
- [Main Documentation](../../docs/) - Complete JD.Efcpt.Build documentation
