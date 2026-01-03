# Database-First SqlProj Generation Sample

This sample demonstrates the **new database-first workflow** where JD.Efcpt.Build extracts a SQL Server database schema into a DACPAC and then generates EF Core models from it.

## What This Demonstrates

- **Database-First Workflow**: Start with a live SQL Server database as the source of truth
- **Automatic Schema Extraction**: Uses `sqlpackage` to extract the database schema to a DACPAC
- **Integrated Pipeline**: The extracted DACPAC is automatically used for EF Core model generation
- **Zero Configuration**: No SQL project needed - just point to your database

## Workflow

```
Live Database → sqlpackage Extract → DACPAC → EF Core Models
```

This enables organizations to:
- Treat the database as the source of truth
- Automatically sync EF Core models when schema changes
- Skip maintaining separate SQL projects
- Integrate with existing database-first development processes

## Project Structure

```
DatabaseFirstSqlProjGeneration.sln
└── EntityFrameworkCoreProject/
    ├── EntityFrameworkCoreProject.csproj
    └── appsettings.json (contains connection string)
```

## Configuration

The key configuration is in `EntityFrameworkCoreProject.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        
        <!-- Enable SqlProj generation from database -->
        <EfcptGenerateSqlProj>true</EfcptGenerateSqlProj>
        
        <!-- Connection string configuration -->
        <EfcptAppSettings>appsettings.json</EfcptAppSettings>
        <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
        <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

The connection string is defined in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyDatabase;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

## How It Works

When you run `dotnet build`:

1. **Extract Schema**: JD.Efcpt.Build runs `sqlpackage /Action:Extract` to create a DACPAC from your database
2. **Generate Models**: The extracted DACPAC is used as input for EF Core Power Tools to generate models
3. **Compile**: The generated models are included in your project compilation

The DACPAC is cached in `obj/efcpt/GeneratedSqlProj/` and only regenerated when the database schema changes.

## Requirements

- .NET SDK 8.0 or later
- SQL Server or LocalDB with an existing database
- **For .NET 8-9**: Install sqlpackage globally: `dotnet tool install -g microsoft.sqlpackage`
- **For .NET 10+**: No installation needed - uses `dnx` to run sqlpackage on-demand

## Building the Sample

1. **Set up a database**:
   ```bash
   # Create a LocalDB instance if you don't have one
   sqllocaldb create mssqllocaldb
   sqllocaldb start mssqllocaldb
   
   # Create a test database with some tables
   sqlcmd -S "(localdb)\mssqllocaldb" -Q "CREATE DATABASE MyDatabase"
   sqlcmd -S "(localdb)\mssqllocaldb" -d MyDatabase -Q "CREATE TABLE Users (Id INT PRIMARY KEY, Name NVARCHAR(100))"
   ```

2. **Update connection string**: Edit `appsettings.json` to match your database server

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Check generated files**:
   - DACPAC: `obj/efcpt/GeneratedSqlProj/EntityFrameworkCoreProject.Database.dacpac`
   - EF Core models: `obj/efcpt/Generated/Models/`
   - DbContext: `obj/efcpt/Generated/`

## Optional: Generate SQL Project File

If you want to also generate a `.sqlproj` file for documentation or versioning:

```xml
<PropertyGroup>
    <EfcptGenerateSqlProj>true</EfcptGenerateSqlProj>
    <EfcptGenerateSqlProjFile>true</EfcptGenerateSqlProjFile>
    <EfcptSqlProjType>microsoft-build-sql</EfcptSqlProjType>
</PropertyGroup>
```

This creates a `Microsoft.Build.Sql` project file alongside the DACPAC.

## Advanced Configuration

### SQL Server Version

Specify the SQL Server version for compatibility:

```xml
<PropertyGroup>
    <EfcptSqlServerVersion>Sql160</EfcptSqlServerVersion> <!-- SQL Server 2022 -->
</PropertyGroup>
```

Supported versions: `Sql160`, `Sql150`, `Sql140`, `Sql130`, `Sql120`, etc.

### SqlPackage Tool Version

Control which version of sqlpackage to use:

```xml
<PropertyGroup>
    <EfcptSqlPackageToolVersion>162.3.566</EfcptSqlPackageToolVersion>
</PropertyGroup>
```

### Custom DACPAC Output Location

```xml
<PropertyGroup>
    <EfcptSqlProjOutputDir>$(MSBuildProjectDirectory)\GeneratedDacpac\</EfcptSqlProjOutputDir>
    <EfcptSqlProjName>MyCustomDatabase</EfcptSqlProjName>
</PropertyGroup>
```

## Comparison with Traditional Workflow

### Traditional (SQL Project First)

```
Developer → SQL Scripts → Build SqlProj → DACPAC → EF Core Models
```

- Requires maintaining SQL scripts
- More control over schema organization
- Good for complex databases

### New Database-First (This Sample)

```
Database Administrator → Live Database → Extract DACPAC → EF Core Models
```

- No SQL scripts to maintain
- Database is the source of truth
- Perfect for database-first teams
- Simpler setup

## Benefits

✅ **No SQL Project Maintenance**: Skip creating and maintaining `.sqlproj` files
✅ **Database as Source of Truth**: Your live database drives model generation
✅ **Automatic Sync**: Models regenerate when database schema changes
✅ **CI/CD Ready**: Works in build pipelines just like other samples
✅ **Deterministic**: Fingerprinting ensures consistent builds

## See Also

- [Connection String Mode](../connection-string-mssql/) - Generate models directly from a connection string (without SqlProj/DACPAC)
- [Microsoft.Build.Sql Zero Config](../microsoft-build-sql-zero-config/) - Traditional SQL project workflow
- [Main Documentation](../../docs/) - Complete JD.Efcpt.Build documentation
