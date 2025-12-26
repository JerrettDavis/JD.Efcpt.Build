# Modern SDK-Style SQL Project Sample

This sample demonstrates using `JD.Efcpt.Build` with a modern **MSBuild.Sdk.SqlProj** SQL Project.

## Project Structure

- `DatabaseProject/` - Modern SDK-style SQL Project using **MSBuild.Sdk.SqlProj**
  - Note: Despite the SDK name containing "SqlProj", this project uses a **`.csproj`** file extension
- `EntityFrameworkCoreProject/` - .NET project that consumes the generated EF Core models

## Important: Understanding MSBuild.Sdk.SqlProj

The **MSBuild.Sdk.SqlProj** package provides modern SDK-style SQL projects. Key points:

- **File Extension:** Uses `.csproj` or `.fsproj` (NOT `.sqlproj`)
- **SDK Reference:** `<Project Sdk="MSBuild.Sdk.SqlProj/3.3.0">`
- **No Visual Studio Required:** Works with just the .NET SDK
- **Cross-Platform:** Builds on Windows, Linux, and macOS

This is different from traditional **Microsoft.Build.Sql** projects which use the `.sqlproj` extension.

## How It Works

This sample **imports JD.Efcpt.Build directly from source** rather than consuming it as a NuGet package. This makes it ideal for:
- Developing and testing JD.Efcpt.Build itself
- Seeing how the build targets work without NuGet packaging complexity
- Quick iteration during development

The `EntityFrameworkCoreProject.csproj` uses:
```xml
<Import Project="$(EfcptBuildRoot)build\JD.Efcpt.Build.props" />
<Import Project="$(EfcptBuildRoot)build\JD.Efcpt.Build.targets" />
```

This is the same approach used by the test assets in `tests/TestAssets/SampleApp`.

## Building the Sample

```powershell
# From this directory
dotnet build
```

The build will:
- Build the MSBuild.Sdk.SqlProj project (DatabaseProject.csproj) to a DACPAC
- Run the Efcpt pipeline to generate EF Core models
- Compile the generated models into the application

## For Production Usage

In a real project, you would consume JD.Efcpt.Build as a NuGet package:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
</ItemGroup>
```

The NuGet package automatically imports the props and targets files, so you don't need explicit `<Import>` statements.

See the main [README.md](../../README.md) for full documentation on NuGet package consumption.

## Configuration Files

- `efcpt-config.json` - EF Core Power Tools configuration
- `efcpt.renaming.json` - Renaming rules for generated code

These files are automatically discovered by the build pipeline.

