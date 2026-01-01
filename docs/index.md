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
- **Dual Source Support**: Work with SQL Server Database Projects (.sqlproj) or connect directly to databases
- **T4 Template Support**: Customize code generation with your own templates
- **CI/CD Ready**: Works everywhere .NET runs—local dev, GitHub Actions, Azure DevOps, Docker

## Quick Start

### Option A: Project Template (Easiest)

```bash
dotnet new install JD.Efcpt.Build.Templates
dotnet new efcptbuild --name MyDataProject
dotnet build
```

### Option B: SDK Approach (Recommended)

```xml
<Project Sdk="JD.Efcpt.Sdk/1.0.0">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
</Project>
```

### Option C: PackageReference

```bash
dotnet add package JD.Efcpt.Build
dotnet build
```

> **.NET 8-9:** Install CLI first: `dotnet tool install -g ErikEJ.EFCorePowerTools.Cli --version "10.*"`
>
> **.NET 10+:** No tool installation needed.

## How It Works

The package orchestrates a six-stage MSBuild pipeline:

1. **Resolve** - Discover database project and configuration files
2. **Build** - Compile .sqlproj to DACPAC (or query live database)
3. **Stage** - Prepare configuration and templates
4. **Fingerprint** - Detect if regeneration is needed
5. **Generate** - Run efcpt CLI to create EF Core models
6. **Compile** - Add generated .g.cs files to build

## Requirements

- .NET SDK 8.0 or later
- EF Core Power Tools CLI (auto-executed via `dnx` on .NET 10+)
- SQL Server Database Project (.sqlproj) or live database connection

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](user-guide/getting-started.md) | Installation and first project setup |
| [Using the SDK](user-guide/sdk.md) | SDK integration for cleanest project files |
| [Configuration](user-guide/configuration.md) | MSBuild properties and JSON config |
| [Connection String Mode](user-guide/connection-string-mode.md) | Generate from live databases |
| [CI/CD Integration](user-guide/ci-cd.md) | GitHub Actions, Azure DevOps, Docker |
| [Troubleshooting](user-guide/troubleshooting.md) | Common issues and solutions |
| [API Reference](user-guide/api-reference.md) | Complete MSBuild properties and tasks |
| [Core Concepts](user-guide/core-concepts.md) | How the build pipeline works |

## License

This project is licensed under the MIT License.