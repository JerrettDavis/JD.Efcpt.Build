# DACPAC Zero Configuration Sample

This sample demonstrates the **true zero-configuration** approach with JD.Efcpt.Build using a pre-built DACPAC file directly.

## What This Demonstrates

- **Zero Configuration**: No `efcpt-config.json`, no templates, no SQL project in the solution
- **Direct DACPAC**: Uses a pre-built `.dacpac` file as the schema source
- **Single Property**: Only one MSBuild property needed (`EfcptDacpac`)

## Project Structure

```
ZeroConfigDacpac.sln
├── Database.dacpac                 # Pre-built DACPAC file
└── EntityFrameworkCoreProject/
    └── EntityFrameworkCoreProject.csproj  # Only JD.Efcpt.Build + EfcptDacpac
```

## EntityFrameworkCoreProject.csproj

Notice how minimal the configuration is:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <!-- Point to the pre-built DACPAC -->
        <EfcptDacpac>$(MSBuildProjectDirectory)\..\Database.dacpac</EfcptDacpac>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
        <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

That's it! JD.Efcpt.Build:
1. Enables automatically (default: `EfcptEnabled=true`)
2. Reads the schema from the DACPAC file
3. Generates EF Core models during build

## Building

```bash
dotnet build ZeroConfigDacpac.sln
```

Generated files appear in `EntityFrameworkCoreProject/obj/efcpt/Generated/`.

## When to Use This Approach

This approach is ideal when:
- You have a pre-built DACPAC from another project or CI/CD pipeline
- You don't want or need the SQL project in your solution
- You're consuming a database schema from an external source
- You want the fastest possible build (no SQL project compilation)

## Database Schema

The included `Database.dacpac` contains a blog schema with:
- `Author` - Blog authors
- `Blog` - Blogs with titles and descriptions
- `Post` - Blog posts with content
