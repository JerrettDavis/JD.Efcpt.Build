# Database Provider Support

JD.Efcpt.Build integrates with EF Core Power Tools to support multiple database providers. However, the way you configure and use each provider differs based on whether DACPAC files are available.

## Quick Reference

| Provider | DACPAC Mode | Live DB Mode | Driver Package | Recommended Approach |
|----------|---|---|---|---|
| **SQL Server** | Yes | Yes | Bundled with `JD.Efcpt.Build` | DACPAC (if you have .sqlproj) |
| **PostgreSQL** | No | Yes | `JD.Efcpt.Build.PostgreSQL` | Live DB connection string |
| **MySQL** | No | Yes | `JD.Efcpt.Build.MySqlConnector` | Live DB connection string |
| **SQLite** | No | Yes | `JD.Efcpt.Build.Sqlite` | Live DB connection string |
| **Oracle** | No | Yes | `JD.Efcpt.Build.Oracle` | Live DB connection string |
| **Firebird** | No | Yes | `JD.Efcpt.Build.Firebird` | Live DB connection string |
| **Snowflake** | No | Yes | `JD.Efcpt.Build.Snowflake` | Live DB connection string |

## Installing Provider Drivers (Satellite Packages)

To keep the core `JD.Efcpt.Build` package lightweight, only the SQL Server driver ships in the
box. Every other provider's ADO.NET driver lives in its own **satellite package** that you
install alongside `JD.Efcpt.Build`:

```bash
# Example: add PostgreSQL support to a project that already references JD.Efcpt.Build
dotnet add package JD.Efcpt.Build.PostgreSQL
```

| `EfcptProvider` value | Install command |
|---|---|
| `postgres` | `dotnet add package JD.Efcpt.Build.PostgreSQL` |
| `mysql` | `dotnet add package JD.Efcpt.Build.MySqlConnector` |
| `sqlite` | `dotnet add package JD.Efcpt.Build.Sqlite` |
| `oracle` | `dotnet add package JD.Efcpt.Build.Oracle` |
| `firebird` | `dotnet add package JD.Efcpt.Build.Firebird` |
| `snowflake` | `dotnet add package JD.Efcpt.Build.Snowflake` |

Each satellite package bundles its own driver DLLs (e.g. `JD.Efcpt.Build.PostgreSQL` bundles
Npgsql) and wires itself into the build automatically via an MSBuild `.targets` file - no
additional configuration is needed beyond installing the package and setting `EfcptProvider`
and `EfcptConnectionString`.

### What happens if the driver package is missing

If you set `EfcptProvider` to a non-SQL-Server value but haven't installed the matching
satellite package, the build fails fast with a clear, actionable error instead of a cryptic
type-load failure:

```
Driver for provider 'postgres' is not available. Install it with: dotnet add package
JD.Efcpt.Build.PostgreSQL See https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/provider-support.html
for details.
```

This is thrown as a `ProviderDriverNotFoundException` from the same provider-resolution code
path regardless of *why* the driver couldn't be loaded - whether the satellite package was
never installed, or its assembly failed to load for some other reason (corrupt install, wrong
architecture, etc.) - so you always get the exact install command rather than having to
diagnose an internal reflection failure yourself.

## SQL Server (Full Support)

SQL Server is the best-supported provider because it has native tooling for DACPAC generation.

> **Platform Note:** Traditional (non-SDK-style) `.sqlproj` files are Windows-only. For cross-platform SQL projects, use [Microsoft.Build.Sql](https://github.com/microsoft/DacFx) or [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj), which are supported by JD.Efcpt.Build and work on Windows, Linux, and macOS.

### Option 1: DACPAC Mode (Recommended for SQL Server)

Use a SQL Server Database Project (.sqlproj) to define your schema:

```xml
<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="0.15.*" />
  </ItemGroup>
</Project>
```

**Benefits:**
- Schema versioning through .sqlproj files
- Incremental builds and caching
- Design-time validation
- Works in CI/CD without database connections

See [Getting Started](getting-started.md) for a complete example.

### Option 2: Live Database Mode

Connect directly to a SQL Server database:

```xml
<PropertyGroup>
  <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
</PropertyGroup>
```

See [Connection String Mode](connection-string-mode.md) for detailed configuration.

## Non-SQL-Server Providers (Live DB Only)

For PostgreSQL, MySQL, SQLite, Oracle, Firebird, and Snowflake, you must use **Connection String Mode** — DACPAC files don't exist for these providers. You must also install the matching satellite package - see [Installing Provider Drivers](#installing-provider-drivers-satellite-packages) above.

### Configuration

Set the database provider and connection string:

```xml
<PropertyGroup>
  <EfcptProvider>postgres</EfcptProvider>
  <EfcptConnectionString>Host=localhost;Database=mydb;Username=user;Password=pwd</EfcptConnectionString>
</PropertyGroup>
```

**Available Provider Values:**
- `mssql` (SQL Server, default)
- `postgres` (PostgreSQL)
- `mysql` (MySQL)
- `sqlite` (SQLite)
- `oracle` (Oracle)
- `firebird` (Firebird)
- `snowflake` (Snowflake)

### Limitations

- Requires a live database connection at build time
- No incremental caching (regenerates on every build unless you implement custom fingerprinting)
- Not suitable for CI/CD environments where database connections are unavailable or ephemeral

### Workaround: Generate Offline, Commit Models

For non-SQL-Server providers in CI/CD:

1. **Local Development**: Use Connection String Mode to scaffold models against your development database
2. **Commit Generated Code**: Commit the generated `.g.cs` files to version control
3. **CI/CD**: Disable model generation in your build, use committed code

This approach trades automation for portability:

```xml
<!-- Only regenerate in local development -->
<PropertyGroup Condition="'$(GITHUB_ACTIONS)' != 'true'">
  <EfcptEnabled>true</EfcptEnabled>
</PropertyGroup>
```

## Provider-Specific Notes

### PostgreSQL

Requires `dotnet add package JD.Efcpt.Build.PostgreSQL` (bundles Npgsql - see
[Installing Provider Drivers](#installing-provider-drivers-satellite-packages)).

Connection string format:
```
Host=localhost;Database=mydb;Username=user;Password=pwd;Port=5432
```

### MySQL

Requires `dotnet add package JD.Efcpt.Build.MySqlConnector` (bundles MySqlConnector - see
[Installing Provider Drivers](#installing-provider-drivers-satellite-packages)).

Connection string format:
```
Server=localhost;Database=mydb;Uid=user;Pwd=pwd;Port=3306
```

### SQLite

Requires `dotnet add package JD.Efcpt.Build.Sqlite` (bundles Microsoft.Data.Sqlite.Core - see
[Installing Provider Drivers](#installing-provider-drivers-satellite-packages)).

Connection string format:
```
Data Source=mydb.db
```

Useful for small databases and development. Supports file-based and in-memory databases.

### Oracle

Requires `dotnet add package JD.Efcpt.Build.Oracle` (bundles Oracle.ManagedDataAccess[.Core] -
see [Installing Provider Drivers](#installing-provider-drivers-satellite-packages)).

Connection string format:
```
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=mydb)));User Id=user;Password=pwd
```

### Firebird

Requires `dotnet add package JD.Efcpt.Build.Firebird` (bundles FirebirdSql.Data.FirebirdClient -
see [Installing Provider Drivers](#installing-provider-drivers-satellite-packages)).

Connection string format:
```
Database=localhost:mydb.fdb;User=SYSDBA;Password=masterkey
```

### Snowflake

Requires `dotnet add package JD.Efcpt.Build.Snowflake` (bundles Snowflake.Data - see
[Installing Provider Drivers](#installing-provider-drivers-satellite-packages)).

Connection string format:
```
Account=myaccount;User=user;Password=pwd;Warehouse=mywarehouse;Database=mydb;Schema=myschema
```

## Choosing Your Approach

### Use DACPAC Mode if:
- You're using SQL Server
- You want fully automated CI/CD without live database connections
- You need schema versioning and design-time validation
- You prefer separation of schema (SQL project) and data access (EF Core project)

### Use Connection String Mode if:
- You're using a non-SQL-Server provider
- You have a live database available at build time
- You want to scaffold models locally and commit them to CI/CD
- You're in early-stage development with rapidly changing schemas

## Troubleshooting

### "Provider not supported" error

Ensure the provider value matches the supported list above. Common typos: `sqlserver` (should be `mssql`), `psql` (should be `postgres`).

### Connection string not found

Check that `EfcptConnectionString` or `EfcptAppSettings` is set correctly. See [Configuration](configuration.md) for all supported methods.

### DACPAC mode requires SQL Server

Only SQL Server supports `.sqlproj` and DACPAC files. For other providers, use Connection String Mode.

### "Driver for provider '...' is not available" error

You set `EfcptProvider` to a non-SQL-Server value but haven't installed that provider's
satellite package. The error message includes the exact `dotnet add package` command to run -
see [Installing Provider Drivers](#installing-provider-drivers-satellite-packages).

## Beyond the Built-In Providers: Custom Providers

Need a database engine that isn't in the list above (MongoDB, DynamoDB, ClickHouse, etc.)? You can
author and register your own **custom provider** - a small assembly implementing the same
`IProviderAdapter`/`ISchemaReader` contract the built-in providers use, loaded via the identical
satellite-package resolution machinery. This is disabled by default (custom providers execute
third-party code at build time) and requires an explicit opt-in. See
[Custom Providers](custom-providers.md) for the full authoring and registration guide.

## See Also

- [Configuration Reference](configuration.md) - All EfcptProvider values and properties
- [Connection String Mode](connection-string-mode.md) - Detailed live database setup
- [Core Concepts](core-concepts.md) - How the build pipeline works
- [Custom Providers](custom-providers.md) - Authoring and registering your own database provider
