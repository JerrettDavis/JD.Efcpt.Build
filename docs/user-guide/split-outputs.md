# Split Outputs

This guide explains how to use the Split Outputs feature to separate generated entity models from your DbContext into different projects.

## Overview

By default, JD.Efcpt.Build generates all EF Core artifacts (entities, DbContext, configurations) into a single project. The Split Outputs feature allows you to:

- Generate entity models (POCOs) in a separate **Models project** with no EF Core dependencies
- Keep DbContext and configurations in a **Data project** that references the Models project

This separation is useful when:
- You want entity models available to projects that shouldn't reference EF Core
- You follow clean architecture principles with domain models separate from data access
- You want to reduce package dependencies in your domain layer

## Project Layout

```
MySolution/
  MyProject.Models/           # Entity models only (no EF Core)
    MyProject.Models.csproj
    obj/efcpt/Generated/Models/
      Blog.g.cs
      Post.g.cs
  MyProject.Data/             # DbContext + EF Core
    MyProject.Data.csproj
    efcpt-config.json
    obj/efcpt/Generated/
      SampleDbContext.g.cs
      Models/                 # Source models (copied to Models project)
        Blog.g.cs
        Post.g.cs
  MyDatabase/
    MyDatabase.sqlproj
```

## Configuration

### Data Project (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- Import JD.Efcpt.Build -->
  <PackageReference Include="JD.Efcpt.Build" Version="1.*" />

  <PropertyGroup>
    <!-- Enable split outputs -->
    <EfcptSplitOutputs>true</EfcptSplitOutputs>

    <!-- Path to Models project -->
    <EfcptModelsProject>..\MyProject.Models\MyProject.Models.csproj</EfcptModelsProject>
  </PropertyGroup>

  <!-- Reference Models project with BuildReference="false" -->
  <ItemGroup>
    <ProjectReference Include="..\MyProject.Models\MyProject.Models.csproj">
      <ReferenceOutputAssembly>true</ReferenceOutputAssembly>
      <BuildReference>false</BuildReference>
    </ProjectReference>
  </ItemGroup>

  <!-- Reference SQL project -->
  <ItemGroup>
    <ProjectReference Include="..\MyDatabase\MyDatabase.sqlproj">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    </ProjectReference>
  </ItemGroup>

  <!-- EF Core packages -->
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### Models Project (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- Import JD.Efcpt.Build for external models support -->
  <PackageReference Include="JD.Efcpt.Build" Version="1.*" />

  <PropertyGroup>
    <!-- Models project does NOT run efcpt -->
    <EfcptEnabled>false</EfcptEnabled>
  </PropertyGroup>

  <!-- Only DataAnnotations - no EF Core -->
  <ItemGroup>
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
  </ItemGroup>
</Project>
```

## Properties Reference

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptSplitOutputs` | `false` | Enable split outputs mode |
| `EfcptModelsProject` | (none) | Path to the Models project (.csproj) |
| `EfcptModelsProjectOutputSubdir` | `obj\efcpt\Generated\Models\` | Destination subdirectory in Models project |
| `EfcptDataProjectKeepLocalModels` | `false` | If true, also compile models in Data project |
| `EfcptExternalModelsDir` | (none) | Directory containing external models to compile |

## Build Order

The `BuildReference="false"` attribute is **required** on the Models project reference. This prevents MSBuild from building the Models project before the Data project generates and copies the model files.

Without this attribute, the build would fail because:
1. MSBuild tries to build Models first (dependency order)
2. Models project has no source files yet
3. Build fails

With `BuildReference="false"`:
1. Data project generates all files
2. Data project copies model files to Models project
3. Data project explicitly builds Models project
4. Data project compiles with reference to built Models assembly

## How It Works

1. **EfcptValidateSplitOutputs** - Validates configuration:
   - Ensures `EfcptModelsProject` is set and exists
   - Verifies `BuildReference="false"` on the ProjectReference

2. **EfcptCopyModelsToModelsProject** - Copies generated models:
   - Clears destination to remove stale files
   - Copies `Models/**/*.g.cs` to Models project

3. **EfcptBuildModelsProject** - Builds Models project:
   - Invokes MSBuild with `EfcptEnabled=false`
   - Passes `EfcptExternalModelsDir` for source inclusion

4. **EfcptAddToCompile** - Includes appropriate files:
   - In Data project: excludes `Models/**/*.g.cs`
   - In Models project: includes from `EfcptExternalModelsDir`

## Templates

Your T4 templates should generate entity models in a `Models` subdirectory for split outputs to work correctly. The default EF Core Power Tools templates already do this.

If using custom templates, ensure entity classes are output to:
```
$(OutputDir)/Models/EntityName.cs
```

And DbContext/configurations to:
```
$(OutputDir)/DbContextName.cs
```

## Troubleshooting

### "EfcptModelsProject is not set"

Set the `EfcptModelsProject` property to the path of your Models project:

```xml
<EfcptModelsProject>..\MyProject.Models\MyProject.Models.csproj</EfcptModelsProject>
```

### "BuildReference must be false"

Update your ProjectReference to include `BuildReference="false"`:

```xml
<ProjectReference Include="..\MyProject.Models\MyProject.Models.csproj">
  <ReferenceOutputAssembly>true</ReferenceOutputAssembly>
  <BuildReference>false</BuildReference>
</ProjectReference>
```

### No models copied to Models project

Ensure your templates generate entity files in a `Models` subdirectory. Check that files exist at:
```
obj/efcpt/Generated/Models/*.g.cs
```

### Duplicate type definitions

If you see duplicate type errors, ensure `EfcptDataProjectKeepLocalModels` is `false` (the default), which excludes model files from the Data project compilation.

## Next Steps

- [Getting Started](getting-started.md) - Basic setup guide
- [Configuration](core-concepts.md) - All configuration options
- [Troubleshooting](troubleshooting.md) - Common issues and solutions
