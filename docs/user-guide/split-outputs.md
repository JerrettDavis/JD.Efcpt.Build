# Split Outputs

This guide explains how to use the Split Outputs feature to separate generated entity models from your DbContext into different projects.

## Overview

By default, JD.Efcpt.Build generates all EF Core artifacts (entities, DbContext, configurations) into a single project. The Split Outputs feature allows you to:

- **Generate all files in the Models project** (the primary project with no EF Core dependencies)
- **Copy DbContext and configurations to the Data project** (which has EF Core dependencies)
- **Keep entity models in the Models project** for use by projects that shouldn't reference EF Core

This separation is useful when:
- You want entity models available to projects that shouldn't reference EF Core
- You follow clean architecture principles with domain models separate from data access
- You want to reduce package dependencies in your domain layer

## Project Layout

```
MySolution/
  MyProject.Models/           # PRIMARY: Entity models only (no EF Core)
    MyProject.Models.csproj
    efcpt-config.json         # efcpt configuration lives here
    efcpt.renaming.json
    Template/                 # T4 templates
    obj/efcpt/Generated/
      Models/                 # Entity models (kept in Models project)
        Blog.g.cs
        Post.g.cs
      MyDbContext.g.cs        # DbContext (copied to Data project)
      Configurations/         # Configurations (copied to Data project)
  MyProject.Data/             # SECONDARY: DbContext + EF Core
    MyProject.Data.csproj
    obj/efcpt/Generated/      # Receives DbContext and configs from Models
      MyDbContext.g.cs
      Configurations/
  MyDatabase/
    MyDatabase.sqlproj
```

## Configuration

### Models Project (.csproj) - PRIMARY

The Models project is the primary project that runs efcpt and generates all files.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- Import JD.Efcpt.Build -->
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="1.*" />
  </ItemGroup>

  <PropertyGroup>
    <!-- Models project IS the primary project - it runs efcpt -->
    <EfcptEnabled>true</EfcptEnabled>

    <!-- Enable split outputs and specify Data project -->
    <EfcptSplitOutputs>true</EfcptSplitOutputs>
    <EfcptDataProject>..\MyProject.Data\MyProject.Data.csproj</EfcptDataProject>
  </PropertyGroup>

  <!-- Reference SQL project -->
  <ItemGroup>
    <ProjectReference Include="..\MyDatabase\MyDatabase.sqlproj">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    </ProjectReference>
  </ItemGroup>

  <!-- Only DataAnnotations - NO EF Core -->
  <ItemGroup>
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
  </ItemGroup>
</Project>
```

### Data Project (.csproj) - SECONDARY

The Data project receives DbContext and configurations from the Models project.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- Import JD.Efcpt.Build for external data support -->
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="1.*" />
  </ItemGroup>

  <PropertyGroup>
    <!-- Data project does NOT run efcpt - it receives copied DbContext/configs -->
    <EfcptEnabled>false</EfcptEnabled>

    <!-- Include external data files copied from Models project -->
    <EfcptExternalDataDir>$(MSBuildProjectDirectory)\obj\efcpt\Generated\</EfcptExternalDataDir>
  </PropertyGroup>

  <!-- Reference Models project (normal reference - Models builds first) -->
  <ItemGroup>
    <ProjectReference Include="..\MyProject.Models\MyProject.Models.csproj" />
  </ItemGroup>

  <!-- EF Core packages -->
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  </ItemGroup>
</Project>
```

## Properties Reference

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptSplitOutputs` | `false` | Enable split outputs mode (set in Models project) |
| `EfcptDataProject` | (none) | Path to the Data project that receives DbContext/configs |
| `EfcptDataProjectOutputSubdir` | `obj\efcpt\Generated\` | Destination subdirectory in Data project |
| `EfcptExternalDataDir` | (none) | Directory containing external data files to compile (set in Data project) |

## Build Order

**Build sequence:**
1. SQL project is built → produces DACPAC
2. Models project runs efcpt → generates all files in `obj/efcpt/Generated/`
3. Models project copies DbContext and configs to Data project via `EfcptCopyDataToDataProject`
4. Models project compiles with only entity models (`Models/**/*.g.cs`)
5. Data project compiles with DbContext, configs, and reference to Models assembly

## How It Works

1. **EfcptValidateSplitOutputs** - Validates configuration:
   - Ensures `EfcptDataProject` is set and exists
   - Resolves the Data project path

2. **EfcptCopyDataToDataProject** - Copies generated DbContext and configs:
   - Clears destination to remove stale files
   - Copies root-level `*.g.cs` files (DbContext) and `Configurations/**/*.g.cs` to Data project

3. **EfcptAddToCompile** - Includes appropriate files:
   - In Models project (split mode): only includes `Models/**/*.g.cs`
   - In Data project: includes files from `EfcptExternalDataDir`

4. **EfcptIncludeExternalData** - In Data project:
   - Includes `*.g.cs` files from `EfcptExternalDataDir` in compilation

## Templates

Your T4 templates should generate entity models in a `Models` subdirectory for split outputs to work correctly. The default EF Core Power Tools templates already do this.

If using custom templates, ensure entity classes are output to:
```
$(OutputDir)/Models/EntityName.cs
```

And DbContext/configurations to:
```
$(OutputDir)/DbContextName.cs
$(OutputDir)/Configurations/EntityConfiguration.cs
```

## Troubleshooting

### "EfcptDataProject is not set"

Set the `EfcptDataProject` property in your Models project to the path of your Data project:

```xml
<EfcptDataProject>..\MyProject.Data\MyProject.Data.csproj</EfcptDataProject>
```

### No DbContext copied to Data project

Ensure your templates generate DbContext files at the root level (not in Models folder). Check that files exist at:
```
obj/efcpt/Generated/*.g.cs
```

### Entity models not found in Models project

Ensure your templates generate entity files in a `Models` subdirectory:
```
obj/efcpt/Generated/Models/*.g.cs
```

### Duplicate type definitions

If you see duplicate type errors:
- Ensure the Models project only compiles `Models/**/*.g.cs` (handled automatically in split mode)
- Ensure the Data project uses `EfcptExternalDataDir` to include copied files

## Next Steps

- [Getting Started](getting-started.md) - Basic setup guide
- [Configuration](core-concepts.md) - All configuration options
- [Troubleshooting](troubleshooting.md) - Common issues and solutions
