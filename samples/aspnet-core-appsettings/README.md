# ASP.NET Core with appsettings.json + Aspire

This sample demonstrates the recommended pattern for ASP.NET Core applications: reading the database connection string from `appsettings.json` using the `EfcptAppSettings` property, with .NET Aspire managing the SQL Server container.

## Why This Pattern?

1. **Single source of truth** - Same connection string used at build-time and runtime (for development)
2. **Container-based development** - SQL Server runs in Docker, managed by Aspire
3. **Environment-specific** - Supports `appsettings.Development.json`, `appsettings.Production.json`, etc.
4. **No external dependencies** - Just Docker and .NET SDK required

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop (for SQL Server container)
- SQL Server client tools (optional, for running init.sql)

## Project Structure

```
aspnet-core-appsettings/
├── AspNetCoreAppSettings.AppHost/    # Aspire orchestrator
│   ├── AspNetCoreAppSettings.AppHost.csproj
│   └── Program.cs
├── MyApp.Api/                        # ASP.NET Core API with JD.Efcpt.Build
│   ├── MyApp.Api.csproj
│   ├── appsettings.json              # Connection string for build-time
│   └── Program.cs
├── Database/
│   └── init.sql                      # Database initialization script
├── AspNetCoreAppSettings.sln
└── README.md
```

## Quick Start

### 1. Start the SQL Server Container

```bash
cd aspnet-core-appsettings
dotnet run --project AspNetCoreAppSettings.AppHost
```

This starts a SQL Server container on port **11434** with:
- Database: `MyAppDb`
- User: `sa`
- Password: `YourStrong@Passw0rd`

### 2. Initialize the Database

```bash
sqlcmd -S localhost,11434 -U sa -P "YourStrong@Passw0rd" -i Database/init.sql
```

### 3. Build the API Project

```bash
dotnet build MyApp.Api
```

JD.Efcpt.Build reads the connection string from `appsettings.json` and generates EF Core models.

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,11434;Database=MyAppDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  }
}
```

### Project File (.csproj)

```xml
<PropertyGroup>
  <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
  <EfcptProvider>mssql</EfcptProvider>
</PropertyGroup>
```

## Using the Generated DbContext

After building, register the DbContext in your `Program.cs`:

```csharp
builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Then inject it into your endpoints:

```csharp
app.MapGet("/users", async (MyAppDbContext db) =>
    await db.Users.Take(10).ToListAsync());
```

## How It Works

1. **Aspire AppHost** starts SQL Server in a Docker container
2. **Database/init.sql** creates the schema with Users, Roles, and AuditLogs tables
3. **JD.Efcpt.Build** reads connection string from `appsettings.json` at build time
4. **At runtime**, Aspire can inject a different connection string if needed

## Environment-Specific Configuration

### Option 1: Environment-specific appsettings files

Create `appsettings.Development.json` pointing to the container and `appsettings.Production.json` with production credentials.

### Option 2: MSBuild conditions

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Development'">
  <EfcptAppSettings>appsettings.Development.json</EfcptAppSettings>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <EfcptAppSettings>appsettings.Production.json</EfcptAppSettings>
</PropertyGroup>
```

## Security Best Practices

For production, avoid storing credentials in appsettings.json:

1. **User Secrets** (development): `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."`
2. **Environment Variables**: Set `ConnectionStrings__DefaultConnection` environment variable
3. **Azure Key Vault**: Use managed identities for Azure deployments

## Troubleshooting

### "Microsoft.Data.SqlClient is not supported on this platform"
Ensure the SQL Server container is running before building.

### Connection refused
1. Verify Docker is running
2. Check if the container is up: `docker ps`
3. Ensure port 11434 is not blocked

### Database does not exist
Run the `Database/init.sql` script to create the schema.
