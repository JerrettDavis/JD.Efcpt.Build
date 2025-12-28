# EfcptProject

This project uses **JD.Efcpt.Build** to automatically generate Entity Framework Core models from a database project during build.

## Getting Started

### 1. Add a Database Project Reference

Add a reference to your SQL Server Database Project:

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

### 2. Build Your Project

```bash
dotnet build
```

The build process will:
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
- [Quick Start Guide](https://github.com/jerrettdavis/JD.Efcpt.Build#-quick-start)
- [Configuration Options](https://github.com/jerrettdavis/JD.Efcpt.Build#%EF%B8%8F-configuration)

## Prerequisites

- .NET 8.0 SDK or later
- EF Core Power Tools CLI (installed automatically on .NET 10+)
- A SQL Server Database Project (Microsoft.Build.Sql or MSBuild.Sdk.SqlProj)

For .NET 8.0-9.0, install the EF Core Power Tools CLI:

```bash
dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version "10.*"
```
