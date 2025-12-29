# EfcptProject

This project uses **JD.Efcpt.Sdk** to automatically generate Entity Framework Core models from a database project during build.

## What is JD.Efcpt.Sdk?

JD.Efcpt.Sdk is an MSBuild SDK that:
- Extends Microsoft.NET.Sdk with EF Core Power Tools integration
- Automatically discovers SQL projects in your solution
- Can use an optional ProjectReference to explicitly specify which database to use
- Builds the SQL project to DACPAC and generates EF Core models
- Requires minimal configuration for a clean, simple setup

## Getting Started

### 1. (Optional) Add a Database Project Reference

If you have multiple SQL projects in your solution, or want to be explicit about which database to use, add a reference to your SQL Server Database Project:

```xml
<ItemGroup>
  <ProjectReference Include="..\YourDatabase\YourDatabase.sqlproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    <OutputItemType>None</OutputItemType>
  </ProjectReference>
</ItemGroup>
```

Or for MSBuild.Sdk.SqlProj projects:

```xml
<ItemGroup>
  <ProjectReference Include="..\YourDatabase\YourDatabase.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    <OutputItemType>None</OutputItemType>
  </ProjectReference>
</ItemGroup>
```

**Note:** If you have only a single SQL project in your solution, the SDK will automatically discover and use it without requiring an explicit ProjectReference.

### 2. Build Your Project

```bash
dotnet build
```

The build process will:
- Discover SQL projects in your solution
- Build your database project to a DACPAC
- Run EF Core Power Tools to generate models
- Include the generated models in your compilation

Generated files appear in `obj/efcpt/Generated/`.

### 3. Customize Configuration (Optional)

Edit `efcpt-config.json` to customize:
- Namespaces and naming conventions
- Which schemas/tables to include
- Code generation options

## Documentation

For more information, see:
- [JD.Efcpt.Build Documentation](https://github.com/jerrettdavis/JD.Efcpt.Build)
- [SDK Documentation](https://github.com/jerrettdavis/JD.Efcpt.Build/blob/main/docs/user-guide/sdk.md)
- [Quick Start Guide](https://github.com/jerrettdavis/JD.Efcpt.Build#-quick-start)
- [Configuration Options](https://github.com/jerrettdavis/JD.Efcpt.Build#%EF%B8%8F-configuration)

## Prerequisites

- .NET SDK net8.0 or later
- A SQL Server Database Project (Microsoft.Build.Sql, MSBuild.Sdk.SqlProj, or classic SSDT-style)
#if (IsNet8OrNet9)
- EF Core Power Tools CLI (version 8.*)

Install the EF Core Power Tools CLI:

```bash
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "8.*"
```
#endif
#if (IsNet10)

**Note:** EF Core Power Tools CLI is included with .NET 10.0 SDK and does not need to be installed separately.
#endif
