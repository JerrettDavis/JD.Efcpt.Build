# JD.Efcpt.Build Samples

This directory contains sample projects demonstrating various usage patterns of JD.Efcpt.Build for automatic Entity Framework Core model generation during MSBuild.

## Sample Overview

| Sample | Input Mode | Provider | Key Features |
|--------|------------|----------|--------------|
| [simple-generation](#simple-generation) | DACPAC | SQL Server | Basic usage, direct source import |
| [msbuild-sdk-sql-proj-generation](#msbuild-sdk-sql-proj-generation) | MSBuild.Sdk.SqlProj | SQL Server | Modern SQL SDK, NuGet package |
| [split-data-and-models-between-multiple-projects](#split-outputs) | DACPAC | SQL Server | Clean architecture, split outputs |
| [connection-string-sqlite](#connection-string-sqlite) | Connection String | SQLite | Direct database reverse engineering |

## Input Modes

JD.Efcpt.Build supports three input modes:

### 1. DACPAC Mode (Default)
Reverse engineers from a SQL Server Database Project (.sqlproj) that produces a .dacpac file.

```xml
<ItemGroup>
  <ProjectReference Include="..\Database\Database.sqlproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
</ItemGroup>
```

### 2. MSBuild.Sdk.SqlProj Mode
Works with the modern [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) SDK-style SQL projects.

### 3. Connection String Mode
Reverse engineers directly from a live database connection.

```xml
<PropertyGroup>
  <EfcptConnectionString>Data Source=./database.db</EfcptConnectionString>
  <EfcptProvider>sqlite</EfcptProvider>
</PropertyGroup>
```

#### Supported Providers

| Provider | Value | NuGet Package Used |
|----------|-------|-------------------|
| SQL Server | `mssql` | Microsoft.Data.SqlClient |
| PostgreSQL | `postgres` | Npgsql |
| MySQL/MariaDB | `mysql` | MySqlConnector |
| SQLite | `sqlite` | Microsoft.Data.Sqlite |
| Oracle | `oracle` | Oracle.ManagedDataAccess.Core |
| Firebird | `firebird` | FirebirdSql.Data.FirebirdClient |
| Snowflake | `snowflake` | Snowflake.Data |

---

## Sample Details

### simple-generation

**Location:** `simple-generation/`

Basic sample demonstrating DACPAC-based model generation with direct source import (useful for development).

```
simple-generation/
├── DatabaseProject/          # SQL Server Database Project
│   └── DatabaseProject.sqlproj
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

Demonstrates using MSBuild.Sdk.SqlProj with NuGet package consumption - the recommended approach for production projects.

```
msbuild-sdk-sql-proj-generation/
├── DatabaseProject/          # MSBuild.Sdk.SqlProj project
│   └── DatabaseProject.csproj
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   └── efcpt-config.json
└── SimpleGenerationSample.sln
```

**Key Configuration:**
- Uses `MSBuild.Sdk.SqlProj/3.3.0` for the database project
- Demonstrates dynamic SQL project discovery (no explicit reference needed)

---

### split-data-and-models-between-multiple-projects

**Location:** `split-data-and-models-between-multiple-projects/`

Advanced sample showing how to split generated output across multiple projects following clean architecture principles.

```
split-data-and-models-between-multiple-projects/
└── src/
    ├── SampleApp.Sql/       # SQL Database Project
    ├── SampleApp.Models/    # Entity classes only (NO EF Core)
    └── SampleApp.Data/      # DbContext + EF Core dependencies
```

**Key Features:**
- `EfcptSplitOutputs=true` enables split generation
- Models project has no EF Core dependency
- DbContext and configurations go to Data project
- Automatic file distribution during build

**Configuration (Models project):**
```xml
<PropertyGroup>
  <EfcptSplitOutputs>true</EfcptSplitOutputs>
  <EfcptDataProject>..\SampleApp.Data\SampleApp.Data.csproj</EfcptDataProject>
</PropertyGroup>
```

---

### connection-string-sqlite

**Location:** `connection-string-sqlite/`

Demonstrates connection string mode with SQLite - no SQL project needed, reverse engineers directly from a database.

```
connection-string-sqlite/
├── Database/
│   ├── sample.db            # SQLite database file
│   └── schema.sql           # Schema documentation
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   ├── efcpt-config.json
│   └── Template/
├── setup-database.ps1       # Creates sample database
└── README.md
```

**Setup:**
```powershell
./setup-database.ps1         # Creates Database/sample.db
dotnet build EntityFrameworkCoreProject
```

**Key Configuration:**
```xml
<PropertyGroup>
  <EfcptConnectionString>Data Source=$(MSBuildProjectDirectory)\..\Database\sample.db</EfcptConnectionString>
  <EfcptProvider>sqlite</EfcptProvider>
</PropertyGroup>
```

---

## Common Configuration

All samples use:
- **T4 Templates** for code generation (customizable)
- **efcpt-config.json** for EF Core Power Tools configuration
- **efcpt.renaming.json** for entity/property renaming rules (optional)
- **Fingerprint-based incremental builds** - only regenerates when schema changes

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
