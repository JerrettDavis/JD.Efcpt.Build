# SQLite Connection String Mode Sample

This sample demonstrates using **JD.Efcpt.Build** with **connection string mode** to reverse engineer Entity Framework Core models directly from a SQLite database file.

## Features Demonstrated

- **Connection String Mode**: No DACPAC or SQL project required
- **SQLite Provider**: Using `Microsoft.Data.Sqlite` for schema reading
- **Automatic Schema Fingerprinting**: Detects schema changes and regenerates only when needed
- **T4 Templates**: Customizable code generation

## Project Structure

```
connection-string-sqlite/
├── Database/
│   └── sample.db          # SQLite database file (created by setup script)
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   ├── efcpt-config.json
│   └── efcpt.renaming.json
├── setup-database.ps1     # Creates the sample database
└── README.md
```

## Prerequisites

- .NET 10.0 SDK or later
- PowerShell (for setup script)

## Getting Started

### 1. Create the Sample Database

Run the setup script to create a SQLite database with sample tables:

```powershell
./setup-database.ps1
```

This creates `Database/sample.db` with the following schema:
- `categories` - Product categories
- `products` - Products with category references
- `orders` - Customer orders
- `order_items` - Order line items

### 2. Build the Project

```bash
dotnet build EntityFrameworkCoreProject
```

During build, JD.Efcpt.Build will:
1. Connect to the SQLite database using the connection string
2. Read the schema metadata
3. Generate Entity Framework Core models and DbContext
4. Output files to the `Models/` directory

### 3. Verify Generated Files

After building, check `EntityFrameworkCoreProject/Models/` for:
- `SampleDbContext.cs` - The DbContext
- Entity classes for each table

## Configuration

### Connection String Mode Properties

The `.csproj` file configures connection string mode:

```xml
<!-- Use connection string mode instead of DACPAC -->
<EfcptConnectionString>Data Source=$(MSBuildProjectDirectory)\..\Database\sample.db</EfcptConnectionString>
<EfcptProvider>sqlite</EfcptProvider>
```

### Supported Providers

| Provider | Value | Description |
|----------|-------|-------------|
| SQL Server | `mssql` | Microsoft SQL Server |
| PostgreSQL | `postgres` | PostgreSQL / CockroachDB |
| MySQL | `mysql` | MySQL / MariaDB |
| SQLite | `sqlite` | SQLite database files |
| Oracle | `oracle` | Oracle Database |
| Firebird | `firebird` | Firebird SQL |
| Snowflake | `snowflake` | Snowflake Data Cloud |

## Schema Changes

When you modify the database schema:

1. The fingerprint will detect the change
2. Next build will regenerate the models
3. Previous fingerprint is stored in `obj/efcpt/.fingerprint`

To force regeneration:
```bash
dotnet clean EntityFrameworkCoreProject
dotnet build EntityFrameworkCoreProject
```

## Troubleshooting

### "Database file not found"

Ensure you've run `setup-database.ps1` first to create the sample database.

### Models not regenerating

Delete the fingerprint file to force regeneration:
```bash
rm EntityFrameworkCoreProject/obj/efcpt/.fingerprint
```
