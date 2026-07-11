# MySQL Reference Sample

This sample demonstrates configuring **JD.Efcpt.Build** for **MySQL** using Connection String
Mode, and shows the shape of the EF Core models `efcpt` generates for a MySQL schema.

## What This Demonstrates

- `EfcptProvider=mysql` configuration
- The `Pomelo.EntityFrameworkCore.MySql` runtime provider wired up in `AppDbContext`
- A minimal `efcpt-config.json` for connection-string-mode generation
- How to keep a sample buildable in CI **without a live database** by committing a reference
  copy of the generated code (see [Committed Generated Code](#committed-generated-code) below)

## Project Structure

```
provider-mysql/
├── ProviderMySql.sln
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
Server=localhost;Database=mydb;Uid=user;Pwd=pwd;Port=3306
```

## A Note on Package Versions

`Pomelo.EntityFrameworkCore.MySql`'s latest stable release at the time this sample was written
(9.0.0) only targets **EF Core 9.x** - there is no EF Core 10 build of Pomelo yet. The project
still targets `net10.0` (NuGet resolves Pomelo's `net8.0` asset group fine for a `net10.0` app),
but `Microsoft.EntityFrameworkCore` is deliberately pinned to the `9.0.x` line here instead of the
`10.0.x` line used by the other provider samples, to satisfy Pomelo's dependency range. Revisit
this pin once Pomelo ships an EF Core 10 compatible release.

## Regenerating Locally

MySQL has no DACPAC equivalent, so real generation requires a live database:

1. Install the MySQL driver satellite package (only needed while regenerating, JD.Efcpt.Build
   itself is already referenced):
   ```bash
   dotnet add EntityFrameworkCoreProject package JD.Efcpt.Build.MySqlConnector
   ```
2. Point at a real database and build:
   ```bash
   set EfcptConnectionString=Server=localhost;Database=mydb;Uid=user;Pwd=pwd
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
dotnet build ProviderMySql.sln
```
