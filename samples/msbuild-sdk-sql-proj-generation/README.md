# MSBuild.Sdk.SqlProj Generation Sample

This sample demonstrates using `JD.Efcpt.Build` with the **[MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj)** SDK.

## Key Differences from Microsoft.Build.Sql

**MSBuild.Sdk.SqlProj**:
- Uses `.csproj` or `.fsproj` file extension (not `.sqlproj`)
- Community-maintained SDK with additional features and extensibility
- Cross-platform: works on Linux/macOS/Windows
- More similar to the legacy .NET Framework SQL Projects

**Microsoft.Build.Sql**:
- Uses `.sqlproj` file extension
- Microsoft's official SDK for SQL Projects in .NET SDK
- Cross-platform: works on Linux/macOS/Windows

Both produce DACPACs that work with JD.Efcpt.Build.

## Project Structure

- `DatabaseProject/` - MSBuild.Sdk.SqlProj project (uses `.csproj` extension)
- `EntityFrameworkCoreProject/` - .NET project that consumes the generated EF Core models

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
- Build the DatabaseProject to a DACPAC
- Run the Efcpt pipeline to generate EF Core models
- Compile the generated models into the application

## For Production Usage

In a real project, you would consume JD.Efcpt.Build as a NuGet package:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="PACKAGE_VERSION" />
</ItemGroup>
```

The NuGet package automatically imports the props and targets files, so you don't need explicit `<Import>` statements.

See the main [README.md](../../README.md) for full documentation on NuGet package consumption.

## Configuration Files

- `efcpt-config.json` - EF Core Power Tools configuration
- `efcpt.renaming.json` - Renaming rules for generated code

These files are automatically discovered by the build pipeline.

