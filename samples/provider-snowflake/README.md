# Snowflake Reference Sample (Entities-Only)

This sample demonstrates configuring **JD.Efcpt.Build** for **Snowflake** using Connection String
Mode. Unlike the other four provider samples, this one is **entities-only**.

## Why Entities-Only

JD.Efcpt.Build can read Snowflake schema at *generation* time via the
`JD.Efcpt.Build.Snowflake` satellite driver package (it bundles `Snowflake.Data` purely for
schema reading). However, there is no first-party (or well-established community) **EF Core
runtime provider** for Snowflake - i.e. no `UseSnowflake(...)` extension method exists anywhere to
actually connect a `DbContext` to a Snowflake warehouse at runtime.

So this sample's `AppDbContext` has entity classes and `DbSet<T>` properties, generated exactly
like the other providers, but **no `OnConfiguring` override / no `UseX` call**. A host application
that wants to actually query Snowflake through this `DbContext` needs to supply its own configured
`DbContextOptions<AppDbContext>` - for example by hand-rolling ADO.NET queries against
`Snowflake.Data` and mapping results into these entity types, or wiring up a third-party/community
EF Core provider if one exists at the time you read this.

## What This Demonstrates

- `EfcptProvider=snowflake` configuration
- Entity classes (`Customer`, `Order`) and a `DbSet<T>`-based `AppDbContext` with **no** runtime
  provider wiring (only `Microsoft.EntityFrameworkCore` + `Microsoft.EntityFrameworkCore.Relational`
  are referenced, the latter purely so `OnModelCreating` can use relational fluent-API members like
  `HasColumnType`)
- A minimal `efcpt-config.json` for connection-string-mode generation
- How to keep a sample buildable in CI **without a live database** by committing a reference
  copy of the generated code (see [Committed Generated Code](#committed-generated-code) below)

## Project Structure

```
provider-snowflake/
├── ProviderSnowflake.sln
├── nuget.config
└── EntityFrameworkCoreProject/
    ├── EntityFrameworkCoreProject.csproj
    ├── efcpt-config.json
    └── Generated/                  # Committed reference copy (see below)
        ├── AppDbContext.cs         # Entities + DbSets only, no OnConfiguring/UseX
        └── Entities/
            ├── Customer.cs
            └── Order.cs
```

## Connection String Format

```
Account=myaccount;User=user;Password=pwd;Warehouse=mywarehouse;Database=mydb;Schema=myschema
```

## Regenerating Locally

1. Install the Snowflake driver satellite package (only needed while regenerating,
   JD.Efcpt.Build itself is already referenced):
   ```bash
   dotnet add EntityFrameworkCoreProject package JD.Efcpt.Build.Snowflake
   ```
2. Point at a real warehouse and build:
   ```bash
   set EfcptConnectionString=Account=myaccount;User=user;Password=pwd;Warehouse=mywarehouse;Database=mydb;Schema=myschema
   dotnet build EntityFrameworkCoreProject
   ```
   (`export EfcptConnectionString=...` on macOS/Linux.)

With `EfcptConnectionString` set, `EfcptEnabled` turns on automatically (see the csproj), `efcpt`
regenerates real models into `obj/efcpt/Generated`, and the committed `Generated/` reference copy
in the project directory is excluded from compilation for that build so the two don't collide. The
regenerated `AppDbContext` will still have no `OnConfiguring`/`UseX` call, for the reasons above.

## Committed Generated Code

Without a connection string (as in CI), `EfcptEnabled` is forced to `false`, which fully
short-circuits generation - no DB connection, DACPAC query, or `efcpt` invocation happens. Instead
the project compiles the **committed** `Generated/AppDbContext.cs` and `Generated/Entities/*.cs`
files. This is what lets `dotnet build EntityFrameworkCoreProject -p:EfcptEnabled=false` succeed
in CI with zero database dependency - see the `samples-build` job in `.github/workflows/ci.yml`.

## Building

```bash
dotnet build ProviderSnowflake.sln
```
