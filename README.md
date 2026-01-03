# JD.Efcpt.Build

[![NuGet](https://img.shields.io/nuget/v/JD.Efcpt.Build.svg)](https://www.nuget.org/packages/JD.Efcpt.Build/)
[![License](https://img.shields.io/github/license/jerrettdavis/JD.Efcpt.Build.svg)](LICENSE)
[![CI](https://github.com/JerrettDavis/JD.Efcpt.Build/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/JD.Efcpt.Build/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JerrettDavis/JD.Efcpt.Build/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/JerrettDavis/JD.Efcpt.Build/security/code-scanning)
[![codecov](https://codecov.io/gh/JerrettDavis/JD.Efcpt.Build/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/JD.Efcpt.Build)

**MSBuild integration for EF Core Power Tools CLI**

Automate database-first EF Core model generation during `dotnet build`. Zero manual steps, full CI/CD support, reproducible builds.

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

> **.NET 8-9 users:** Install the CLI tool first: `dotnet tool install -g ErikEJ.EFCorePowerTools.Cli --version "10.*"`
>
> **.NET 10+ users:** No tool installation needed - uses `dnx` automatically.

## Available Packages

| Package | Purpose | Usage |
|---------|---------|-------|
| [JD.Efcpt.Build](https://www.nuget.org/packages/JD.Efcpt.Build/) | MSBuild integration | Add as `PackageReference` |
| [JD.Efcpt.Sdk](https://www.nuget.org/packages/JD.Efcpt.Sdk/) | SDK package (cleanest setup) | Use as project SDK |
| [JD.Efcpt.Build.Templates](https://www.nuget.org/packages/JD.Efcpt.Build.Templates/) | Project templates | `dotnet new install` |

## Key Features

- **Automatic generation** - DbContext and entities generated during `dotnet build`
- **Incremental builds** - Only regenerates when schema or config changes
- **Database-First SqlProj Generation** - Extract schema from live databases to DACPAC (NEW!)
- **Dual input modes** - Works with SQL Projects (.sqlproj) or live database connections
- **Smart discovery** - Auto-finds database projects and configuration files
- **T4 template support** - Customize code generation with your own templates
- **Multi-schema support** - Generate models across multiple database schemas
- **CI/CD ready** - Works everywhere .NET runs (GitHub Actions, Azure DevOps, Docker)
- **Cross-platform SQL Projects** - Supports Microsoft.Build.Sql and MSBuild.Sdk.SqlProj

## Documentation

| Topic | Description |
|-------|-------------|
| [Getting Started](docs/user-guide/getting-started.md) | Installation and first project setup |
| [Using the SDK](docs/user-guide/sdk.md) | SDK approach for cleanest project files |
| [Configuration](docs/user-guide/configuration.md) | MSBuild properties and JSON config options |
| [Connection String Mode](docs/user-guide/connection-string-mode.md) | Generate from live databases |
| [T4 Templates](docs/user-guide/t4-templates.md) | Customize code generation |
| [CI/CD Integration](docs/user-guide/ci-cd.md) | GitHub Actions, Azure DevOps, Docker |
| [Troubleshooting](docs/user-guide/troubleshooting.md) | Common issues and solutions |
| [API Reference](docs/user-guide/api-reference.md) | Complete MSBuild properties and tasks |
| [Core Concepts](docs/user-guide/core-concepts.md) | How the build pipeline works |
| [Architecture](docs/architecture/README.md) | Internal architecture details |

## Requirements

- **.NET SDK 8.0+**
- **EF Core Power Tools CLI** - Auto-executed via `dnx` on .NET 10+; requires manual install on .NET 8-9
- **Database source** - SQL Server Database Project (.sqlproj) or live database connection

### Supported SQL Project Types

| Type | Extension | Cross-Platform |
|------|-----------|----------------|
| [Microsoft.Build.Sql](https://github.com/microsoft/DacFx) | `.sqlproj` | Yes |
| [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) | `.csproj` / `.fsproj` | Yes |
| Traditional SQL Projects | `.sqlproj` | Windows only |

## New: Database-First SQL Generation

Automatically generate SQL scripts from your live database when JD.Efcpt.Build detects it's referenced in a SQL project:

**DatabaseProject** (SQL):
```xml
<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">
    <PropertyGroup>
        <EfcptConnectionString>Server=...;Database=MyDb;...</EfcptConnectionString>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
    </ItemGroup>
</Project>
```

**DataAccessProject** (EF Core):
```xml
<ItemGroup>
    <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj" />
    <PackageReference Include="JD.Efcpt.Build" Version="*" />
</ItemGroup>
```

This enables the complete two-project workflow:

```
Live Database → SQL Scripts (in SQL Project) → DACPAC → EF Core Models (in DataAccess Project)
```

**Benefits:**
- ✅ Automatic SQL project detection (no configuration needed)
- ✅ Database as source of truth
- ✅ Human-readable SQL scripts for review and version control
- ✅ Clean separation: Database project (schema) + DataAccess project (models)
- ✅ Incremental builds with schema fingerprinting
- ✅ Works with .NET 10+ `dnx` (no sqlpackage installation required)

See the [Database-First SQL Generation sample](samples/database-first-sql-generation/) for a complete example.

## Samples

See the [samples directory](samples/) for complete working examples:

- [Simple Generation](samples/simple-generation/) - Basic DACPAC-based generation
- [SDK Zero Config](samples/sdk-zero-config/) - Minimal SDK project setup
- [Database-First SQL Generation](samples/database-first-sql-generation/) - Auto-generate SQL scripts from live database (NEW!)
- [Connection String Mode](samples/connection-string-sqlite/) - Generate from live database
- [Custom Renaming](samples/custom-renaming/) - Table and column renaming
- [Schema Organization](samples/schema-organization/) - Multi-schema folder structure
- [Split Outputs](samples/split-data-and-models-between-multiple-projects/) - Separate Models and Data projects

## Contributing

Contributions are welcome! Please open an issue first to discuss changes. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- **[EF Core Power Tools](https://github.com/ErikEJ/EFCorePowerTools)** by Erik Ejlskov Jensen - The tool this package automates
- **Microsoft** - For Entity Framework Core and MSBuild

## Support

- [GitHub Issues](https://github.com/jerrettdavis/JD.Efcpt.Build/issues) - Bug reports and feature requests
- [GitHub Discussions](https://github.com/jerrettdavis/JD.Efcpt.Build/discussions) - Questions and community support
