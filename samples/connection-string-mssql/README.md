# Connection String Mode - SQL Server with Aspire

This sample demonstrates using JD.Efcpt.Build with connection string mode against a SQL Server container managed by .NET Aspire.

## Overview

Instead of reverse engineering from a DACPAC, this sample connects directly to a running SQL Server database. The database runs in a Docker container orchestrated by .NET Aspire.

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop (for SQL Server container)
- SQL Server client tools (optional, for running init.sql)

## Project Structure

```
connection-string-mssql/
├── ConnectionStringMssql.AppHost/    # Aspire orchestrator
│   ├── ConnectionStringMssql.AppHost.csproj
│   └── Program.cs                    # Configures SQL Server container
├── EntityFrameworkCoreProject/       # EF Core project with JD.Efcpt.Build
│   └── EntityFrameworkCoreProject.csproj
├── Database/
│   └── init.sql                      # Database initialization script
├── ConnectionStringMssql.sln
└── README.md
```

## Quick Start

### 1. Start the SQL Server Container

```bash
cd connection-string-mssql
dotnet run --project ConnectionStringMssql.AppHost
```

This starts a SQL Server container on port **11433** with:
- Database: `Northwind` (empty initially)
- User: `sa`
- Password: `YourStrong@Passw0rd`

The Aspire dashboard will open at https://localhost:15XXX (port shown in console).

### 2. Initialize the Database

Connect to the SQL Server and run the initialization script:

**Using sqlcmd:**
```bash
sqlcmd -S localhost,11433 -U sa -P "YourStrong@Passw0rd" -i Database/init.sql
```

**Using Azure Data Studio or SSMS:**
1. Connect to `localhost,11433` with sa credentials
2. Open and execute `Database/init.sql`

### 3. Build the EF Core Project

With the database running and initialized:

```bash
dotnet build EntityFrameworkCoreProject
```

JD.Efcpt.Build will:
1. Connect to the SQL Server container
2. Read the database schema
3. Generate EF Core models in `obj/efcpt/Generated/`

## Configuration

### Connection String

The connection string is configured in `EntityFrameworkCoreProject.csproj`:

```xml
<PropertyGroup>
  <EfcptProvider>mssql</EfcptProvider>
  <EfcptConnectionString>Server=localhost,11433;Database=Northwind;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True</EfcptConnectionString>
</PropertyGroup>
```

### Using Environment Variables

For CI/CD pipelines, use environment variables:

```xml
<EfcptConnectionString>$(EFCPT_CONNECTION_STRING)</EfcptConnectionString>
```

Then set the environment variable before building:
```bash
export EFCPT_CONNECTION_STRING="Server=...;Database=...;..."
dotnet build
```

## How It Works

1. **Aspire AppHost** starts SQL Server in a Docker container with a persistent lifetime
2. **Database/init.sql** creates the Northwind schema with sample tables
3. **JD.Efcpt.Build** connects at build time and generates EF Core models
4. At **runtime**, Aspire injects the connection string (if you add API/service projects)

## Generated Output

After building, check `EntityFrameworkCoreProject/obj/efcpt/Generated/`:

```
Generated/
├── Models/
│   ├── Category.g.cs
│   ├── Customer.g.cs
│   ├── Order.g.cs
│   ├── OrderDetail.g.cs
│   ├── Product.g.cs
│   └── Supplier.g.cs
└── NorthwindContext.g.cs
```

## Troubleshooting

### "Microsoft.Data.SqlClient is not supported on this platform"
Make sure the SQL Server container is running before building.

### Connection refused
1. Verify Docker is running
2. Check if the container is up: `docker ps`
3. Ensure port 11433 is not blocked

### Database does not exist
Run the `Database/init.sql` script to create the schema.

## Tips

- The container uses `ContainerLifetime.Persistent` so it survives AppHost restarts
- Stop the container with `docker stop <container-id>` or through Aspire dashboard
- For production, use Azure SQL or a proper SQL Server instance
