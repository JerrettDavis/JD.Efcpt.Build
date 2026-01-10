# JD.Efcpt.Build Samples

This directory contains sample projects demonstrating various usage patterns of JD.Efcpt.Build for automatic Entity Framework Core model generation during MSBuild.

## Sample Overview

### SDK Mode Samples

| Sample | Description | Key Features |
|--------|-------------|--------------|
| [sdk-zero-config](#sdk-zero-config) | JD.Efcpt.Sdk as MSBuild SDK | **Cleanest setup**, SDK-style project |

### DACPAC Mode Samples

| Sample | SQL SDK / Provider | Key Features |
|--------|-------------------|--------------|
| [microsoft-build-sql-zero-config](#microsoft-build-sql-zero-config) | Microsoft.Build.Sql | **Zero-config** with official MS SDK |
| [dacpac-zero-config](#dacpac-zero-config) | Pre-built .dacpac | **Zero-config** direct DACPAC |
| [simple-generation](#simple-generation) | Traditional SQL Project (.sqlproj) | Basic usage, direct source import |
| [msbuild-sdk-sql-proj-generation](#msbuild-sdk-sql-proj-generation) | MSBuild.Sdk.SqlProj (.csproj) | Modern cross-platform SQL SDK |
| [split-data-and-models-between-multiple-projects](#split-outputs) | Traditional SQL Project (.sqlproj) | Clean architecture, split outputs |
| [custom-renaming](#custom-renaming) | Microsoft.Build.Sql | Entity/property renaming rules |
| [schema-organization](#schema-organization) | Microsoft.Build.Sql | Schema-based folders and namespaces |

### Connection String Mode Samples

| Sample | Database Provider | Key Features |
|--------|------------------|--------------|
| [connection-string-sqlite](#connection-string-sqlite) | SQLite | Direct database reverse engineering |
| [connection-string-mssql](#connection-string-mssql) | SQL Server + Aspire | SQL Server container with .NET Aspire |
| [aspnet-core-appsettings](#aspnet-core-appsettings) | SQL Server + Aspire | appsettings.json + Aspire container |

## Input Modes

JD.Efcpt.Build supports two primary input modes:

### 1. DACPAC Mode (Default)
Reverse engineers from a SQL Project that produces a .dacpac file.

JD.Efcpt.Build supports multiple SQL Project SDKs:

| SDK | Extension | Cross-Platform | Notes |
|-----|-----------|----------------|-------|
| [Microsoft.Build.Sql](https://github.com/microsoft/DacFx) | `.sqlproj` | Yes | Microsoft's official SDK-style SQL Projects for .NET |
| [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) | `.csproj` or `.fsproj` | Yes | Community SDK with additional features and extensibility |
| Traditional SQL Projects | `.sqlproj` | No (Windows only) | Legacy format, requires SQL Server Data Tools |

```xml
<ItemGroup>
  <ProjectReference Include="..\Database\Database.sqlproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
  <!-- Use .sqlproj for Microsoft.Build.Sql -->
  <!-- Use .csproj or .fsproj for MSBuild.Sdk.SqlProj -->
</ItemGroup>
```

**Key Differences:**
- **Microsoft.Build.Sql** uses `.sqlproj` extension and is Microsoft's official SDK
- **MSBuild.Sdk.SqlProj** uses `.csproj`/`.fsproj` extension (despite having "SqlProj" in its name)
- Both produce DACPACs and work identically with JD.Efcpt.Build

### 2. Connection String Mode
Reverse engineers directly from a live database connection.

---

## Sample Details

### sdk-zero-config

**Location:** `sdk-zero-config/`

Demonstrates the **cleanest possible setup** using `JD.Efcpt.Sdk` as an MSBuild SDK instead of a PackageReference. This is the recommended approach for dedicated EF Core model generation projects.

```
sdk-zero-config/
├── SdkZeroConfigSample.sln
├── DatabaseProject/
│   ├── DatabaseProject.csproj         # Microsoft.Build.Sql project
│   └── dbo/Tables/*.sql
└── EntityFrameworkCoreProject/
    └── EntityFrameworkCoreProject.csproj  # Uses JD.Efcpt.Sdk/PACKAGE_VERSION
```

**Key Features:**
- Uses `JD.Efcpt.Sdk` as project SDK (not PackageReference)
- Extends `Microsoft.NET.Sdk` with EF Core Power Tools integration
- Automatic SQL project detection via `ProjectReference`
- Zero configuration required

**Project File:**
```xml
<Project Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <OutputItemType>None</OutputItemType>
        </ProjectReference>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

**Build:**
```bash
dotnet build sdk-zero-config/SdkZeroConfigSample.sln
```

---

### microsoft-build-sql-zero-config

**Location:** `microsoft-build-sql-zero-config/`

Demonstrates true **zero-configuration** usage with Microsoft's official `Microsoft.Build.Sql` SDK. Just add JD.Efcpt.Build to your project - no efcpt-config.json, no templates, no project references needed.

**Key Features:**
- **Zero configuration** - no efcpt-config.json, templates, or project references
- Uses Microsoft's official `Microsoft.Build.Sql` SDK (cross-platform)
- Automatic SQL project discovery from solution
- Default sensible configuration applied automatically

**Build:**
```bash
dotnet build microsoft-build-sql-zero-config/ZeroConfigMsBuildSql.sln
```

---

### dacpac-zero-config

**Location:** `dacpac-zero-config/`

Demonstrates **zero-configuration** reverse engineering directly from a pre-built `.dacpac` file. Ideal when you receive a DACPAC from a DBA or CI/CD pipeline.

**Key Features:**
- **Zero configuration** - no efcpt-config.json or templates
- Uses pre-built DACPAC file (no SQL project in solution)
- Simply set `EfcptDacpac` property to point to the .dacpac file
- No build step for SQL project - just reverse engineering

**Build:**
```bash
dotnet build dacpac-zero-config/ZeroConfigDacpac.sln
```

---

### simple-generation

**Location:** `simple-generation/`

Basic sample demonstrating DACPAC-based model generation with direct source import (useful for development).

```
simple-generation/
├── DatabaseProject/          # SQL Project
│   └── DatabaseProject.sqlproj  # Traditional format
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   ├── efcpt-config.json
│   └── Template/            # T4 templates
└── SimpleGenerationSample.sln
```

**Build:**
```bash
dotnet build simple-generation/SimpleGenerationSample.sln
```

---

### msbuild-sdk-sql-proj-generation

**Location:** `msbuild-sdk-sql-proj-generation/`

Demonstrates using MSBuild.Sdk.SqlProj for cross-platform DACPAC builds. This SDK uses `.csproj` extension (not `.sqlproj`).

```
msbuild-sdk-sql-proj-generation/
├── DatabaseProject/          # MSBuild.Sdk.SqlProj project
│   └── DatabaseProject.csproj   # Uses .csproj extension
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   └── efcpt-config.json
└── SimpleGenerationSample.sln
```

**Key Features:**
- Uses `MSBuild.Sdk.SqlProj` SDK for the SQL Project (note: uses `.csproj` extension)
- Works on Windows, Linux, and macOS
- Dynamic SQL Project discovery (no explicit reference needed)

> **Note:** Despite having "SqlProj" in its name, MSBuild.Sdk.SqlProj uses `.csproj` or `.fsproj` extensions, not `.sqlproj`.

---

### split-data-and-models-between-multiple-projects

**Location:** `split-data-and-models-between-multiple-projects/`

Advanced sample showing how to split generated output across multiple projects following clean architecture principles.

```
split-data-and-models-between-multiple-projects/
└── src/
    ├── SampleApp.Sql/       # SQL Project (Microsoft.Build.Sql format)
    ├── SampleApp.Models/    # Entity classes only (NO EF Core)
    └── SampleApp.Data/      # DbContext + EF Core dependencies
```

**Key Features:**
- `EfcptSplitOutputs=true` enables split generation
- Models project has no EF Core dependency
- DbContext and configurations go to Data project
- Automatic file distribution during build

---

### custom-renaming

**Location:** `custom-renaming/`

Demonstrates using `efcpt.renaming.json` to rename database objects to clean C# names. Useful for legacy databases with naming conventions like `tbl` prefixes or `snake_case` columns.

```
custom-renaming/
├── DatabaseProject/          # SQL Project with legacy-named tables
│   └── dbo/Tables/
│       ├── tblCustomers.sql     # Legacy tbl prefix
│       ├── tblOrders.sql
│       └── tblOrderItems.sql
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   ├── efcpt-config.json
│   └── efcpt.renaming.json      # Renaming rules
└── CustomRenaming.sln
```

**Key Features:**
- Renames tables: `tblCustomers` → `Customer`
- Renames columns: `cust_id` → `Id`, `cust_first_name` → `FirstName`
- Renaming file is auto-discovered by convention
- Schema-level `UseSchemaName` setting

**Configuration (efcpt.renaming.json):**
```json
[
  {
    "SchemaName": "dbo",
    "UseSchemaName": false,
    "Tables": [
      {
        "Name": "tblCustomers",
        "NewName": "Customer",
        "Columns": [
          { "Name": "cust_id", "NewName": "Id" },
          { "Name": "cust_first_name", "NewName": "FirstName" }
        ]
      }
    ]
  }
]
```

**Build:**
```bash
dotnet build custom-renaming/CustomRenaming.sln
```

---

### schema-organization

**Location:** `schema-organization/`

Demonstrates organizing generated entities by database schema using folder and namespace organization.

```
schema-organization/
├── DatabaseProject/
│   ├── dbo/Tables/Customer.sql
│   ├── sales/Tables/Order.sql
│   ├── sales/Tables/OrderItem.sql
│   ├── inventory/Tables/Product.sql
│   └── inventory/Tables/Warehouse.sql
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   └── efcpt-config.json
└── SchemaOrganization.sln
```

**Key Features:**
- `use-schema-folders-preview`: Creates subdirectories per schema (`Models/dbo/`, `Models/sales/`)
- `use-schema-namespaces-preview`: Adds schema to namespace (`EntityFrameworkCoreProject.Models.Sales`)
- Useful for large databases with multiple schemas

**Generated Output:**
```
obj/efcpt/Generated/Models/
├── dbo/
│   └── Customer.g.cs       # namespace: *.Models.Dbo
├── sales/
│   ├── Order.g.cs          # namespace: *.Models.Sales
│   └── OrderItem.g.cs
└── inventory/
    ├── Product.g.cs        # namespace: *.Models.Inventory
    └── Warehouse.g.cs
```

**Configuration (efcpt-config.json):**
```json
{
  "file-layout": {
    "output-path": "Models",
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  }
}
```

**Build:**
```bash
dotnet build schema-organization/SchemaOrganization.sln
```

---

### connection-string-sqlite

**Location:** `connection-string-sqlite/`

Demonstrates connection string mode with SQLite - no SQL Project needed, reverse engineers directly from a database.

---

### connection-string-mssql

**Location:** `connection-string-mssql/`

Demonstrates connection string mode with SQL Server using .NET Aspire to manage a SQL Server container.

```
connection-string-mssql/
├── ConnectionStringMssql.AppHost/    # Aspire orchestrator
├── EntityFrameworkCoreProject/       # EF Core project with JD.Efcpt.Build
├── Database/
│   └── init.sql                      # Database initialization
└── ConnectionStringMssql.sln
```

**Key Features:**
- SQL Server runs in Docker, managed by Aspire
- No external database dependencies
- Uses `EfcptProvider` and `EfcptConnectionString` properties

**Quick Start:**
```bash
# 1. Start the SQL Server container
dotnet run --project ConnectionStringMssql.AppHost

# 2. Initialize the database
sqlcmd -S localhost,11433 -U sa -P "YourStrong@Passw0rd" -i Database/init.sql

# 3. Build the EF Core project
dotnet build EntityFrameworkCoreProject
```

**Prerequisites:** Docker Desktop, .NET 9.0 SDK

---

### aspnet-core-appsettings

**Location:** `aspnet-core-appsettings/`

Demonstrates reading connection strings from `appsettings.json` with .NET Aspire managing the SQL Server container.

```
aspnet-core-appsettings/
├── AspNetCoreAppSettings.AppHost/    # Aspire orchestrator
├── MyApp.Api/
│   ├── MyApp.Api.csproj
│   ├── appsettings.json              # Connection string for build
│   └── Program.cs
├── Database/
│   └── init.sql                      # Database initialization
└── AspNetCoreAppSettings.sln
```

**Key Features:**
- Uses `EfcptAppSettings` to read connection string from appsettings.json
- SQL Server runs in Docker, managed by Aspire
- Works with ASP.NET Core configuration patterns

**Configuration (csproj):**
```xml
<PropertyGroup>
  <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
  <EfcptProvider>mssql</EfcptProvider>
</PropertyGroup>
```

**Quick Start:**
```bash
# 1. Start the SQL Server container
dotnet run --project AspNetCoreAppSettings.AppHost

# 2. Initialize the database
sqlcmd -S localhost,11434 -U sa -P "YourStrong@Passw0rd" -i Database/init.sql

# 3. Build the API project
dotnet build MyApp.Api
```

**Prerequisites:** Docker Desktop, .NET 9.0 SDK

---

## Common Configuration

All samples use:
- **T4 Templates** for code generation (customizable)
- **efcpt-config.json** for EF Core Power Tools configuration
- **efcpt.renaming.json** for entity/property renaming rules (optional)
- **Fingerprint-based incremental builds** - only regenerates when schema changes

> **Note:** The zero-config samples (`microsoft-build-sql-zero-config` and `dacpac-zero-config`) use sensible defaults and don't require any configuration files.

## Getting Started

1. Clone the repository
2. Choose a sample that matches your use case
3. Build the solution:
   ```bash
   dotnet build <sample-name>/<solution-file>.sln
   ```
4. Check the generated files in `obj/efcpt/Generated/`

## More Information

- [JD.Efcpt.Build Documentation](../docs/user-guide/)
- [EF Core Power Tools](https://github.com/ErikEJ/EFCorePowerTools)
