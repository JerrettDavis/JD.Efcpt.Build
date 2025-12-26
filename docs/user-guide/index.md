# Introduction

JD.Efcpt.Build is an MSBuild integration package that automates EF Core Power Tools CLI to generate Entity Framework Core models as part of your build process.

## What is JD.Efcpt.Build?

When developing database-first applications with Entity Framework Core, developers typically use EF Core Power Tools in Visual Studio to manually generate DbContext and entity classes from a database schema. This process must be repeated whenever the database schema changes, which can be tedious and error-prone in team environments.

JD.Efcpt.Build eliminates this manual step by:

- **Automating code generation** during `dotnet build`
- **Detecting schema changes** using fingerprinting to avoid unnecessary regeneration
- **Supporting multiple input sources** including SQL Projects (Microsoft.Build.Sql and MSBuild.Sdk.SqlProj) and live database connections
- **Enabling CI/CD workflows** where models are generated consistently on any build machine

## When to Use JD.Efcpt.Build

Use this package when:

- You have a SQL Server database described by a SQL Project and want EF Core models generated automatically
  - Traditional **Microsoft.Build.Sql** projects (`.sqlproj` extension)
  - Modern **MSBuild.Sdk.SqlProj** projects (`.csproj` or `.fsproj` extension)
- You want EF Core Power Tools generation to run as part of `dotnet build` instead of being a manual step
- You need deterministic, source-controlled model generation that works identically on developer machines and in CI/CD
- You're working in a team environment and need consistent code generation across developers

## How It Works

The package hooks into MSBuild to run a multi-stage pipeline:

```
┌───────────────────────────────────────────────────────────────┐
│ Stage 1: Resolve                                              │
│ Discover SQL Project or connection string, locate configs    │
└───────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────┐
│ Stage 2: Build DACPAC (or Query Schema)                       │
│ Build SQL Project to DACPAC or fingerprint live database     │
└───────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────┐
│ Stage 3: Stage Inputs                                         │
│ Copy config, renaming rules, and templates to obj/efcpt/      │
└───────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────┐
│ Stage 4: Compute Fingerprint                                  │
│ XxHash64 of DACPAC/schema + configs to detect changes         │
└───────────────────────────────────────────────────────────────┘
                            │
              (Only if fingerprint changed)
                            │
┌───────────────────────────────────────────────────────────────┐
│ Stage 5: Generate Models                                      │
│ Run efcpt CLI to generate DbContext and entity classes        │
└───────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────┐
│ Stage 6: Add to Compile                                       │
│ Include generated .g.cs files in C# compilation               │
└───────────────────────────────────────────────────────────────┘
```

## Key Features

### Incremental Builds

The package uses fingerprinting to detect when regeneration is needed. It computes an XxHash64 (fast, non-cryptographic) hash of:
- The DACPAC file contents or database schema
- The EF Core Power Tools configuration
- Renaming rules
- T4 templates

Models are only regenerated when this fingerprint changes, making subsequent builds fast.

### Dual Input Modes

**DACPAC Mode** (Default): Works with SQL Projects
- Automatically builds your SQL Project to a DACPAC
- Supports both Microsoft.Build.Sql (`.sqlproj`) and MSBuild.Sdk.SqlProj (`.csproj`/`.fsproj`)
- Generates models from the DACPAC schema

**Connection String Mode**: Works with live databases
- Connects directly to a database server
- No SQL Project required
- Ideal for cloud databases or existing production systems

### Smart Discovery

The package automatically discovers:
- SQL Projects in your solution (both `.sqlproj` and SDK-style projects)
- Configuration files in standard locations
- T4 templates in conventional directories
- Connection strings from appsettings.json

### Generated File Management

Generated files are:
- Placed in `obj/efcpt/Generated/` by default
- Named with `.g.cs` suffix for easy identification
- Automatically included in compilation
- Excluded from source control (via .gitignore patterns)

## Next Steps

- [Getting Started](getting-started.md) - Install and configure JD.Efcpt.Build
- [Core Concepts](core-concepts.md) - Deep dive into the pipeline architecture
- [Configuration](configuration.md) - Customize generation behavior
