# SDK Zero-Config Sample

This sample demonstrates the simplest possible setup using `JD.Efcpt.Sdk` as an MSBuild SDK.

## Overview

Instead of adding a `PackageReference` to `JD.Efcpt.Build`, you can use `JD.Efcpt.Sdk` as your project SDK:

**global.json** (at solution root):
```json
{
  "msbuild-sdks": {
    "JD.Efcpt.Sdk": "1.0.0"
  }
}
```

**EntityFrameworkCoreProject.csproj**:
```xml
<Project Sdk="JD.Efcpt.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\DatabaseProject\DatabaseProject.csproj">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <OutputItemType>None</OutputItemType>
        </ProjectReference>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

The SDK:
- Extends `Microsoft.NET.Sdk` with EF Core Power Tools integration
- Automatically detects the SQL project via `ProjectReference`
- Builds the SQL project to DACPAC and generates EF Core models
- Requires no additional configuration

## Prerequisites

1. .NET 8.0 SDK or later
2. JD.Efcpt.Sdk package available (via NuGet or local package source)

## Building

```bash
# From the sample directory
dotnet build
```

The build will:
1. Build the `DatabaseProject` to produce a DACPAC
2. Run EF Core Power Tools to generate models from the DACPAC
3. Compile the generated models

Generated files appear in `EntityFrameworkCoreProject/obj/efcpt/Generated/`.

## Local Development

To test with a locally-built SDK package:

```bash
# From the repo root
dotnet pack src/JD.Efcpt.Sdk/JD.Efcpt.Sdk.csproj -o pkg

# From the sample directory
dotnet build
```

The `nuget.config` in this sample is already configured to look for packages in `../../pkg`.
