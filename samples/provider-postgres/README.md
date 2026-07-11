# PostgreSQL Reference Sample

This sample demonstrates configuring **JD.Efcpt.Build** for **PostgreSQL** using Connection
String Mode, and shows the shape of the EF Core models `efcpt` generates for a Postgres schema.

## What This Demonstrates

- `EfcptProvider=postgres` configuration
- The `Npgsql.EntityFrameworkCore.PostgreSQL` runtime provider wired up in `AppDbContext`
- A minimal `efcpt-config.json` for connection-string-mode generation
- How to keep a sample buildable in CI **without a live database** by committing a reference
  copy of the generated code (see [Committed Generated Code](#committed-generated-code) below)

## Project Structure

```
provider-postgres/
├── ProviderPostgres.sln
├── nuget.config
└── EntityFrameworkCoreProject/
    ├── EntityFrameworkCoreProject.csproj
    ├── efcpt-config.json
    └── Generated/                  # Committed reference copy (see below)
        ├── AppDbContext.cs
        └── Entities/
            ├── Customer.cs
            └── Order.cs
```

## Connection String Format

```
Host=localhost;Database=mydb;Username=user;Password=pwd;Port=5432
```

## Regenerating Locally

PostgreSQL has no DACPAC equivalent, so real generation requires a live database:

1. Install the PostgreSQL driver satellite package (only needed while regenerating, JD.Efcpt.Build
   itself is already referenced):
   ```bash
   dotnet add EntityFrameworkCoreProject package JD.Efcpt.Build.PostgreSQL
   ```
2. Point at a real database and build:
   ```bash
   set EfcptConnectionString=Host=localhost;Database=mydb;Username=user;Password=pwd
   dotnet build EntityFrameworkCoreProject
   ```
   (`export EfcptConnectionString=...` on macOS/Linux.)

With `EfcptConnectionString` set, `EfcptEnabled` turns on automatically (see the csproj), `efcpt`
regenerates real models into `obj/efcpt/Generated`, and the committed `Generated/` reference copy
in the project directory is excluded from compilation for that build so the two don't collide.

## Committed Generated Code

Without a connection string (as in CI), `EfcptEnabled` is forced to `false`, which fully
short-circuits generation - no DB connection, DACPAC query, or `efcpt` invocation happens. Instead
the project compiles the **committed** `Generated/AppDbContext.cs` and `Generated/Entities/*.cs`
files, which are hand-written to mirror realistic `efcpt` output for a `customers`/`orders`
schema. This is what lets `dotnet build EntityFrameworkCoreProject -p:EfcptEnabled=false` succeed
in CI with zero database dependency - see the `samples-build` job in `.github/workflows/ci.yml`.

## Building

```bash
dotnet build ProviderPostgres.sln
```
