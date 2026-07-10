# Database Provider Support

JD.Efcpt.Build integrates with EF Core Power Tools to support multiple database providers. However, the way you configure and use each provider differs based on whether DACPAC files are available.

## Quick Reference

| Provider | DACPAC Mode | Live DB Mode | Recommended Approach |
|----------|---|---|---|
| **SQL Server** | Yes | Yes | DACPAC (if you have .sqlproj) |
| **PostgreSQL** | No | Yes | Live DB connection string |
| **MySQL** | No | Yes | Live DB connection string |
| **SQLite** | No | Yes | Live DB connection string |
| **Oracle** | No | Yes | Live DB connection string |
| **Firebird** | No | Yes | Live DB connection string |
| **Snowflake** | No | Yes | Live DB connection string |

## SQL Server (Full Support)

SQL Server is the best-supported provider because it has native tooling for DACPAC generation.

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

For PostgreSQL, MySQL, SQLite, Oracle, Firebird, and Snowflake, you must use **Connection String Mode** — DACPAC files don't exist for these providers.

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

Connection string format:
```
Host=localhost;Database=mydb;Username=user;Password=pwd;Port=5432
```

### MySQL

Connection string format:
```
Server=localhost;Database=mydb;Uid=user;Pwd=pwd;Port=3306
```

### SQLite

Connection string format:
```
Data Source=mydb.db
```

Useful for small databases and development. Supports file-based and in-memory databases.

### Oracle

Requires Oracle.ManagedDataAccess.Core NuGet package.

Connection string format:
```
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=mydb)));User Id=user;Password=pwd
```

### Snowflake

Requires Snowflake.Data NuGet package.

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

## See Also

- [Configuration Reference](configuration.md) - All EfcptProvider values and properties
- [Connection String Mode](connection-string-mode.md) - Detailed live database setup
- [Core Concepts](core-concepts.md) - How the build pipeline works
