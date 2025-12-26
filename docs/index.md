---
_layout: landing
---

# JD.Efcpt.Build

MSBuild integration for EF Core Power Tools CLI that automates database-first Entity Framework Core model generation.

## Overview

JD.Efcpt.Build transforms EF Core Power Tools into a fully automated build step. Instead of manually regenerating your EF Core models in Visual Studio, this package integrates seamlessly into your build pipeline to generate DbContext and entity classes automatically during `dotnet build`.

## Key Features

- **Zero Manual Steps**: Generate EF Core models automatically as part of your build
- **Incremental Builds**: Only regenerates when schema or configuration changes
- **Dual Source Support**: Work with SQL Projects (Microsoft.Build.Sql or MSBuild.Sdk.SqlProj) or connect directly to databases
- **T4 Template Support**: Customize code generation with your own templates
- **CI/CD Ready**: Works everywhere .NET runs—local dev, GitHub Actions, Azure DevOps, Docker

## Quick Start

**Step 1:** Add the NuGet package:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
</ItemGroup>
```

**Step 2:** Install EF Core Power Tools CLI (not required for .NET 10+):

```bash
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

**Step 3:** Build your project:

```bash
dotnet build
```

Your EF Core DbContext and entities are now automatically generated from your database schema during every build.

## How It Works

The package orchestrates a six-stage MSBuild pipeline:

1. **Resolve** - Discover SQL Project and configuration files
2. **Build** - Compile SQL Project to DACPAC (or query live database)
3. **Stage** - Prepare configuration and templates
4. **Fingerprint** - Detect if regeneration is needed
5. **Generate** - Run efcpt CLI to create EF Core models
6. **Compile** - Add generated .g.cs files to build

## Requirements

- .NET SDK 8.0 or later
- EF Core Power Tools CLI (auto-executed via `dnx` on .NET 10+)
- SQL Project (Microsoft.Build.Sql `.sqlproj`, MSBuild.Sdk.SqlProj `.csproj`/`.fsproj`, or legacy `.sqlproj`) or live database connection

## Next Steps

- [Getting Started](user-guide/getting-started.md) - Complete installation and setup guide
- [Core Concepts](user-guide/core-concepts.md) - Understanding the build pipeline
- [Configuration](user-guide/configuration.md) - Customize generation behavior

## License

This project is licensed under the MIT License.