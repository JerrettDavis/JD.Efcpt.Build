# Connection String Mode

JD.Efcpt.Build supports generating EF Core models directly from a live database connection, as an alternative to using SQL Server Database Projects (.sqlproj).

## Overview

Connection string mode allows you to reverse-engineer your EF Core models directly from a running database without requiring a DACPAC file. The package connects to the database, queries the schema, and generates models using the same EF Core Power Tools CLI.

## When to Use Connection String Mode

**Use Connection String Mode when:**

- You don't have a SQL Server Database Project (.sqlproj)
- You want faster builds (no DACPAC compilation step)
- You're working with a cloud database or managed database instance
- You prefer to scaffold from a live database environment

**Use DACPAC Mode when:**

- You have an existing `.sqlproj` that defines your schema
- You want schema versioning through database projects
- You prefer design-time schema validation
- Your CI/CD already builds DACPACs

## Configuration Methods

### Method 1: Explicit Connection String

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

### Method 2: appsettings.json (ASP.NET Core)

Reference your existing ASP.NET Core configuration:

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyDb;Integrated Security=True;"
  }
}
```

**.csproj:**
```xml
<PropertyGroup>
  <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
</PropertyGroup>
```

You can also reference environment-specific files:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Development'">
  <EfcptAppSettings>appsettings.Development.json</EfcptAppSettings>
</PropertyGroup>
```

### Method 3: app.config or web.config (.NET Framework)

For .NET Framework projects, use the traditional configuration format:

**app.config:**
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

**.csproj:**
```xml
<PropertyGroup>
  <EfcptAppConfig>app.config</EfcptAppConfig>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
</PropertyGroup>
```

### Method 4: Auto-Discovery

If you don't specify any connection string properties, the package automatically searches for connection strings in this order:

1. `appsettings.json` in your project directory
2. `appsettings.Development.json` in your project directory
3. `app.config` in your project directory
4. `web.config` in your project directory

If a connection string named `DefaultConnection` exists, it will be used. If not, the first available connection string will be used (with a warning logged).

**Example - Zero configuration:**

```
MyApp/
├── MyApp.csproj
└── appsettings.json  ← Connection string auto-discovered here
```

No properties needed! Just run `dotnet build`.

## Discovery Priority Chain

When multiple connection string sources are present, this priority order is used:

1. **`EfcptConnectionString`** property (highest priority)
2. **`EfcptAppSettings`** or **`EfcptAppConfig`** explicit paths
3. **Auto-discovered** configuration files
4. **Fallback to `.sqlproj`** (DACPAC mode) if no connection string found

## How Schema Fingerprinting Works

In connection string mode, instead of hashing the DACPAC file, JD.Efcpt.Build:

1. **Queries the database** system tables (`sys.tables`, `sys.columns`, `sys.indexes`, etc.)
2. **Builds a canonical schema model** with all tables, columns, indexes, foreign keys, and constraints
3. **Computes an XxHash64 fingerprint** of the schema structure (fast, non-cryptographic)
4. **Caches the fingerprint** to skip regeneration when the schema hasn't changed

This means your builds are still **incremental** - models are only regenerated when the database schema actually changes.

## Connection String Properties Reference

### Input Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptConnectionString` | *(empty)* | Explicit connection string. Takes highest priority. |
| `EfcptAppSettings` | *(empty)* | Path to `appsettings.json` file |
| `EfcptAppConfig` | *(empty)* | Path to `app.config` or `web.config` file |
| `EfcptConnectionStringName` | `DefaultConnection` | Name of the connection string key |
| `EfcptProvider` | `mssql` | Database provider (see Supported Providers below) |

### Output Properties

These properties are set by the pipeline and can be used in subsequent targets:

| Property | Description |
|----------|-------------|
| `ResolvedConnectionString` | The resolved connection string that will be used |
| `UseConnectionString` | `true` when using connection string mode |

## Database Provider Support

JD.Efcpt.Build supports all database providers that EF Core Power Tools supports:

| Provider | Value | Aliases | Notes |
|----------|-------|---------|-------|
| SQL Server | `mssql` | `sqlserver`, `sql-server` | Default provider |
| PostgreSQL | `postgres` | `postgresql`, `pgsql` | Uses Npgsql |
| MySQL/MariaDB | `mysql` | `mariadb` | Uses MySqlConnector |
| SQLite | `sqlite` | `sqlite3` | Single-file databases |
| Oracle | `oracle` | `oracledb` | Uses Oracle.ManagedDataAccess.Core |
| Firebird | `firebird` | `fb` | Uses FirebirdSql.Data.FirebirdClient |
| Snowflake | `snowflake` | `sf` | Uses Snowflake.Data |

### Provider Configuration

Specify the provider in your `.csproj`:

```xml
<PropertyGroup>
  <EfcptProvider>postgres</EfcptProvider>
  <EfcptConnectionString>Host=localhost;Database=mydb;Username=user;Password=pass</EfcptConnectionString>
</PropertyGroup>
```

### Connection String Examples

#### SQL Server
```xml
<PropertyGroup>
  <EfcptProvider>mssql</EfcptProvider>
  <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;TrustServerCertificate=True</EfcptConnectionString>
</PropertyGroup>
```

#### PostgreSQL
```xml
<PropertyGroup>
  <EfcptProvider>postgres</EfcptProvider>
  <EfcptConnectionString>Host=localhost;Database=mydb;Username=postgres;Password=secret</EfcptConnectionString>
</PropertyGroup>
```

#### MySQL/MariaDB
```xml
<PropertyGroup>
  <EfcptProvider>mysql</EfcptProvider>
  <EfcptConnectionString>Server=localhost;Database=mydb;User=root;Password=secret</EfcptConnectionString>
</PropertyGroup>
```

#### SQLite
```xml
<PropertyGroup>
  <EfcptProvider>sqlite</EfcptProvider>
  <EfcptConnectionString>Data Source=./mydatabase.db</EfcptConnectionString>
</PropertyGroup>
```

#### Oracle
```xml
<PropertyGroup>
  <EfcptProvider>oracle</EfcptProvider>
  <EfcptConnectionString>Data Source=localhost:1521/ORCL;User Id=system;Password=oracle</EfcptConnectionString>
</PropertyGroup>
```

#### Firebird
```xml
<PropertyGroup>
  <EfcptProvider>firebird</EfcptProvider>
  <EfcptConnectionString>Database=localhost:C:\data\mydb.fdb;User=SYSDBA;Password=masterkey</EfcptConnectionString>
</PropertyGroup>
```

#### Snowflake
```xml
<PropertyGroup>
  <EfcptProvider>snowflake</EfcptProvider>
  <EfcptConnectionString>account=myaccount;user=myuser;password=mypassword;db=mydb;schema=public</EfcptConnectionString>
</PropertyGroup>
```

### Provider-Specific Notes

**PostgreSQL:**
- Uses lowercase identifiers by default
- Schema defaults to "public" if not specified
- Supports all PostgreSQL data types

**MySQL/MariaDB:**
- InnoDB primary keys are treated as clustered indexes
- Schema concept maps to database name
- Compatible with MariaDB

**SQLite:**
- No schema concept (single database)
- Limited index metadata available
- Excellent for local development and testing

**Oracle:**
- Schema maps to user/owner
- System schemas (SYS, SYSTEM, etc.) are automatically excluded
- Uses uppercase identifiers

**Firebird:**
- No schema concept
- System objects (RDB$*, MON$*) are automatically excluded
- Identifiers may have trailing whitespace (trimmed automatically)

**Snowflake:**
- Uses INFORMATION_SCHEMA for metadata
- No traditional indexes (uses micro-partitioning)
- Primary key and unique constraints are reported as indexes for fingerprinting

## Security Best Practices

### Don't commit credentials

Never commit connection strings with passwords to source control:

```xml
<!-- BAD: Password in plain text -->
<EfcptConnectionString>Server=prod;Database=MyDb;User=sa;Password=Secret123;</EfcptConnectionString>
```

### Use environment variables

Reference environment variables instead:

```xml
<!-- GOOD: Reference environment variable -->
<EfcptConnectionString>$(PRODUCTION_DB_CONNECTION_STRING)</EfcptConnectionString>
```

### Use Integrated Authentication

Use Windows/Integrated Authentication when possible:

```xml
<EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
```

### Use different connections per environment

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Development'">
  <EfcptConnectionString>Server=localhost;Database=MyDb_Dev;Integrated Security=True;</EfcptConnectionString>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Production'">
  <EfcptConnectionString>$(PRODUCTION_DB_CONNECTION_STRING)</EfcptConnectionString>
</PropertyGroup>
```

## Migration Guide

### From DACPAC Mode to Connection String Mode

**Before (DACPAC Mode):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptSqlProj>..\Database\Database.sqlproj</EfcptSqlProj>
  </PropertyGroup>
</Project>
```

**After (Connection String Mode - Explicit):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
  </PropertyGroup>
</Project>
```

**After (Connection String Mode - appsettings.json):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  </PropertyGroup>
</Project>
```

**After (Connection String Mode - Auto-discovery):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>

  <!-- No connection string config needed! -->
  <!-- Will auto-discover from appsettings.json -->
</Project>
```

## Example: ASP.NET Core Web API

Complete example for an ASP.NET Core project:

**MyApp.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  </ItemGroup>

  <PropertyGroup>
    <EfcptAppSettings>appsettings.json</EfcptAppSettings>
    <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
  </PropertyGroup>
</Project>
```

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Integrated Security=True;TrustServerCertificate=True;"
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

Generated models appear in `obj/efcpt/Generated/` automatically.

## Troubleshooting

### Connection refused

Ensure the database server is running and accessible:

```bash
# Test connection manually
sqlcmd -S localhost -d MyDb -E -Q "SELECT 1"
```

### Authentication failed

Check that your credentials or Integrated Security settings are correct:

```xml
<!-- For Windows Authentication -->
<EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;TrustServerCertificate=True;</EfcptConnectionString>

<!-- For SQL Server Authentication -->
<EfcptConnectionString>Server=localhost;Database=MyDb;User Id=myuser;Password=mypassword;TrustServerCertificate=True;</EfcptConnectionString>
```

### No tables generated

Verify the connection string points to the correct database:

```xml
<PropertyGroup>
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
</PropertyGroup>
```

Check the build output for schema query results.

## Next Steps

- [Configuration](configuration.md) - Complete configuration reference
- [T4 Templates](t4-templates.md) - Customize code generation
- [Troubleshooting](troubleshooting.md) - Solve common problems
