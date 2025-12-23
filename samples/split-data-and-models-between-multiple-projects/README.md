# Split Data and Models Between Multiple Projects

This sample demonstrates using `JD.Efcpt.Build` with the **Split Outputs** feature to separate generated entity models from your DbContext into different projects.

## Project Structure

```
src/
  SampleApp.Sql/           # SQL Server Database Project (schema definition)
    SampleApp.Sql.sqlproj
    dbo/Tables/
      Blog.sql
      Post.sql
      Author.sql
  SampleApp.Models/        # Entity models only (NO EF Core dependencies)
    SampleApp.Models.csproj
    obj/efcpt/Generated/Models/
      Blog.g.cs            # Copied from SampleApp.Data during build
      Post.g.cs
      Author.g.cs
  SampleApp.Data/          # DbContext + EF Core
    SampleApp.Data.csproj
    efcpt-config.json
    efcpt.renaming.json
    obj/efcpt/Generated/
      SampleDbContext.g.cs
      Models/              # Source models (copied to SampleApp.Models)
        Blog.g.cs
        Post.g.cs
        Author.g.cs
```

## How It Works

The Split Outputs feature allows you to:

1. **Generate entity models (POCOs) in a separate Models project** with no EF Core dependencies
2. **Keep DbContext and configurations in a Data project** that references the Models project

This is useful when:
- You want entity models available to projects that shouldn't reference EF Core
- You follow clean architecture principles with domain models separate from data access
- You want to reduce package dependencies in your domain layer

## Key Configuration

### SampleApp.Data.csproj

```xml
<PropertyGroup>
  <!-- Enable split outputs -->
  <EfcptSplitOutputs>true</EfcptSplitOutputs>

  <!-- Path to Models project -->
  <EfcptModelsProject>..\SampleApp.Models\SampleApp.Models.csproj</EfcptModelsProject>
</PropertyGroup>

<!-- Reference Models project with BuildReference="false" -->
<ItemGroup>
  <ProjectReference Include="..\SampleApp.Models\SampleApp.Models.csproj">
    <ReferenceOutputAssembly>true</ReferenceOutputAssembly>
    <BuildReference>false</BuildReference>
  </ProjectReference>
</ItemGroup>
```

### SampleApp.Models.csproj

```xml
<PropertyGroup>
  <!-- Models project does NOT run efcpt - it receives copied models -->
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

## Build Order

The `BuildReference="false"` attribute is **required** on the Models project reference. This prevents MSBuild from building the Models project before the Data project generates and copies the model files.

**Build sequence:**
1. `SampleApp.Sql` is built → produces DACPAC
2. `SampleApp.Data` runs efcpt → generates all files in `obj/efcpt/Generated/`
3. `SampleApp.Data` copies `Models/**/*.g.cs` to `SampleApp.Models`
4. `SampleApp.Data` explicitly builds `SampleApp.Models`
5. `SampleApp.Data` compiles with reference to built `SampleApp.Models` assembly

## Building the Sample

```powershell
# From this directory
dotnet build

# Or build just the Data project (triggers everything)
dotnet build src/SampleApp.Data/SampleApp.Data.csproj
```

## Verifying the Output

After building, check:

```powershell
# Models project should have generated entity files
ls src/SampleApp.Models/obj/efcpt/Generated/Models/

# Data project should have DbContext but NOT entity models in compilation
ls src/SampleApp.Data/obj/efcpt/Generated/
```

## For Production Usage

In a real project, you would consume JD.Efcpt.Build as a NuGet package:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
</ItemGroup>
```

See the main [README.md](../../README.md) and [Split Outputs documentation](../../docs/user-guide/split-outputs.md) for full details.
