# Split Data and Models Between Multiple Projects

This sample demonstrates using `JD.Efcpt.Build` with the **Split Outputs** feature to separate generated entity models from your DbContext into different projects.

## Project Structure

```
src/
  SampleApp.Sql/           # SQL Project (schema definition)
    SampleApp.Sql.sqlproj  # Microsoft.Build.Sql format
    dbo/Tables/
      Blog.sql
      Post.sql
      Author.sql
  SampleApp.Models/        # PRIMARY: Entity models only (NO EF Core dependencies)
    SampleApp.Models.csproj
    efcpt-config.json      # efcpt configuration lives here
    efcpt.renaming.json
    Template/              # T4 templates
    obj/efcpt/Generated/
      Models/              # Entity models (kept in Models project)
        Blog.g.cs
        Post.g.cs
        Author.g.cs
      SampleDbContext.g.cs # DbContext (copied to Data project)
      Configurations/      # Configurations (copied to Data project)
  SampleApp.Data/          # SECONDARY: DbContext + EF Core
    SampleApp.Data.csproj
    obj/efcpt/Generated/   # Receives DbContext and configs from Models
      SampleDbContext.g.cs
      Configurations/
```

## How It Works

The Split Outputs feature allows you to:

1. **Generate all files in the Models project** (the primary project with no EF Core dependencies)
2. **Copy DbContext and configurations to the Data project** (which has EF Core dependencies)
3. **Keep entity models in the Models project** for use by projects that shouldn't reference EF Core

This is useful when:
- You want entity models available to projects that shouldn't reference EF Core
- You follow clean architecture principles with domain models separate from data access
- You want to reduce package dependencies in your domain layer

## Key Configuration

### SampleApp.Models.csproj (PRIMARY - runs efcpt)

```xml
<PropertyGroup>
  <!-- Models project IS the primary project - it runs efcpt -->
  <EfcptEnabled>true</EfcptEnabled>

  <!-- Enable split outputs and specify Data project -->
  <EfcptSplitOutputs>true</EfcptSplitOutputs>
  <EfcptDataProject>..\SampleApp.Data\SampleApp.Data.csproj</EfcptDataProject>
</PropertyGroup>

<!-- Reference SQL Project -->
<ItemGroup>
  <ProjectReference Include="..\SampleApp.Sql\SampleApp.Sql.sqlproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
  <!-- Use .sqlproj for Microsoft.Build.Sql, .csproj/.fsproj for MSBuild.Sdk.SqlProj -->
</ItemGroup>

<!-- NO EF Core dependencies - just DataAnnotations -->
<ItemGroup>
  <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
</ItemGroup>
```

### SampleApp.Data.csproj (SECONDARY - receives copied files)

```xml
<PropertyGroup>
  <!-- Data project does NOT run efcpt - it receives copied DbContext/configs -->
  <EfcptEnabled>false</EfcptEnabled>

  <!-- Include external data files copied from Models project -->
  <EfcptExternalDataDir>$(MSBuildProjectDirectory)\obj\efcpt\Generated\</EfcptExternalDataDir>
</PropertyGroup>

<!-- Reference Models project (normal reference - Models builds first) -->
<ItemGroup>
  <ProjectReference Include="..\SampleApp.Models\SampleApp.Models.csproj" />
</ItemGroup>

<!-- EF Core dependencies -->
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
</ItemGroup>
```

## Build Order

**Build sequence:**
1. `SampleApp.Sql` is built → produces DACPAC
2. `SampleApp.Models` runs efcpt → generates all files in `obj/efcpt/Generated/`
3. `SampleApp.Models` copies DbContext and configs to `SampleApp.Data/obj/efcpt/Generated/`
4. `SampleApp.Models` compiles with only entity models (`Models/**/*.g.cs`)
5. `SampleApp.Data` compiles with DbContext, configs, and reference to `SampleApp.Models`

## Building the Sample

```powershell
# From this directory
dotnet build

# Or build just the Models project (triggers generation and copy)
dotnet build src/SampleApp.Models/SampleApp.Models.csproj
```

## Verifying the Output

After building, check:

```powershell
# Models project should have entity models
ls src/SampleApp.Models/obj/efcpt/Generated/Models/

# Data project should have DbContext and configurations
ls src/SampleApp.Data/obj/efcpt/Generated/
```

## For Production Usage

In a real project, you would consume JD.Efcpt.Build as a NuGet package:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="PACKAGE_VERSION" />
</ItemGroup>
```

See the main [README.md](../../README.md) and [Split Outputs documentation](../../docs/user-guide/split-outputs.md) for full details.
