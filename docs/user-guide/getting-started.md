# Getting Started

This guide walks you through installing JD.Efcpt.Build and generating your first EF Core models. By the end, you'll have automatic model generation integrated into your build process.

## Prerequisites

Before you begin, ensure you have:

- **.NET SDK 8.0 or later** installed
- One of:
  - A **SQL Server Database Project** (.sqlproj) that produces a DACPAC
  - A live database connection (SQL Server, PostgreSQL, MySQL, SQLite, Oracle, Firebird, or Snowflake)
- Basic familiarity with MSBuild and NuGet

## Installation

### Step 1: Add the NuGet Package

Add JD.Efcpt.Build to your application project (the project that should contain the generated DbContext and entities):

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
</ItemGroup>
```

Or use the .NET CLI:

```bash
dotnet add package JD.Efcpt.Build
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Step 2: Install EF Core Power Tools CLI

JD.Efcpt.Build uses the EF Core Power Tools CLI (`efcpt`) to generate models.

> [!NOTE]
> **.NET 10+ users**: The CLI is automatically executed via `dnx` and does not need to be installed. Skip this step if you're using .NET 10.0 or later.

**Global installation** (quick start):

```bash
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

**Local tool manifest** (recommended for teams):

```bash
# Create tool manifest if it doesn't exist
dotnet new tool-manifest

# Install as local tool
dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

Local tool manifests ensure everyone on the team uses the same CLI version.

### Step 3: Build Your Project

```bash
dotnet build
```

On the first build, the package will:

1. Discover your SQL Server Database Project
2. Build it to a DACPAC
3. Run the EF Core Power Tools CLI
4. Generate DbContext and entity classes

Generated files appear in `obj/efcpt/Generated/`:

```
obj/efcpt/Generated/
├── YourDbContext.g.cs
└── Models/
    ├── dbo/
    │   ├── User.g.cs
    │   └── Order.g.cs
    └── sales/
        └── Customer.g.cs
```

## Solution Structure

A typical solution layout looks like this:

```
YourSolution/
├── src/
│   └── YourApp/
│       ├── YourApp.csproj          # Add JD.Efcpt.Build here
│       └── efcpt-config.json       # Optional: customize generation
└── database/
    └── YourDatabase/
        └── YourDatabase.sqlproj    # Your database project
```

## Minimal Configuration

For most projects, no configuration is required. The package uses sensible defaults:

- Auto-discovers `.sqlproj` in your solution
- Uses `efcpt-config.json` if present
- Generates to `obj/efcpt/Generated/`
- Enables nullable reference types
- Organizes files by database schema

### Explicit Database Project Path

If auto-discovery doesn't find your database project, specify it explicitly:

```xml
<PropertyGroup>
  <EfcptSqlProj>..\database\YourDatabase\YourDatabase.sqlproj</EfcptSqlProj>
</PropertyGroup>
```

## Configuration File (Optional)

Create `efcpt-config.json` in your project directory to customize generation:

```json
{
  "names": {
    "root-namespace": "YourApp.Data",
    "dbcontext-name": "ApplicationDbContext",
    "dbcontext-namespace": "YourApp.Data",
    "entity-namespace": "YourApp.Data.Entities"
  },
  "code-generation": {
    "use-nullable-reference-types": true,
    "use-date-only-time-only": true,
    "enable-on-configuring": false
  },
  "file-layout": {
    "output-path": "Models",
    "output-dbcontext-path": ".",
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  }
}
```

## Using a Live Database

If you don't have a .sqlproj, you can generate models directly from a database connection. JD.Efcpt.Build supports multiple database providers:

| Provider | Value | Example |
|----------|-------|---------|
| SQL Server | `mssql` | Default |
| PostgreSQL | `postgres` | `Host=localhost;Database=mydb;Username=user;Password=pass` |
| MySQL | `mysql` | `Server=localhost;Database=mydb;User=root;Password=secret` |
| SQLite | `sqlite` | `Data Source=./mydatabase.db` |
| Oracle | `oracle` | `Data Source=localhost:1521/ORCL;User Id=system;Password=oracle` |
| Firebird | `firebird` | `Database=localhost:C:\data\mydb.fdb;User=SYSDBA;Password=masterkey` |
| Snowflake | `snowflake` | `account=myaccount;user=myuser;password=mypassword;db=mydb` |

**SQL Server example:**
```xml
<PropertyGroup>
  <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
</PropertyGroup>
```

**PostgreSQL example:**
```xml
<PropertyGroup>
  <EfcptProvider>postgres</EfcptProvider>
  <EfcptConnectionString>Host=localhost;Database=mydb;Username=user;Password=pass</EfcptConnectionString>
</PropertyGroup>
```

Or reference your existing `appsettings.json`:

```xml
<PropertyGroup>
  <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
</PropertyGroup>
```

See [Connection String Mode](connection-string-mode.md) for details.

## Verifying the Setup

After building, verify that:

1. **Generated files exist**: Check `obj/efcpt/Generated/` for `.g.cs` files
2. **Files compile**: Your project should build without errors
3. **DbContext is available**: You should be able to use the generated DbContext in your code

```csharp
public class MyService
{
    private readonly ApplicationDbContext _context;

    public MyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }
}
```

## Incremental Builds

After the initial generation, subsequent builds are fast. Models are only regenerated when:

- The DACPAC (or database schema) changes
- Configuration files change
- T4 templates change

To force regeneration, delete the intermediate directory:

```bash
# Windows
rmdir /s /q obj\efcpt

# Unix/macOS
rm -rf obj/efcpt
```

Then rebuild:

```bash
dotnet build
```

## Common Issues

### Database project not found

If the package can't find your .sqlproj:

1. Ensure the project exists and builds independently
2. Set `EfcptSqlProj` explicitly in your .csproj
3. Enable detailed logging: `<EfcptLogVerbosity>detailed</EfcptLogVerbosity>`

### efcpt CLI not found

On .NET 8 or 9:

1. Verify the tool is installed: `dotnet tool list --global`
2. Reinstall if needed: `dotnet tool install -g ErikEJ.EFCorePowerTools.Cli --version "10.*"`
3. Try using a local tool manifest with `<EfcptToolMode>tool-manifest</EfcptToolMode>`

### No generated files

1. Check build output for errors
2. Look in `obj/efcpt/Generated/` for files
3. Enable diagnostic logging: `<EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>`

## Next Steps

- [Core Concepts](core-concepts.md) - Understand how the pipeline works
- [Configuration](configuration.md) - Explore all configuration options
- [T4 Templates](t4-templates.md) - Customize code generation
