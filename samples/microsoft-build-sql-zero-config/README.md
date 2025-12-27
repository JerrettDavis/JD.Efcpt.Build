# Microsoft.Build.Sql Zero Configuration Sample

This sample demonstrates the **true zero-configuration** approach with JD.Efcpt.Build using Microsoft's official SQL SDK.

## What This Demonstrates

- **Zero Configuration**: No `efcpt-config.json`, no templates, no explicit MSBuild references to the SQL project
- **Auto-Discovery**: JD.Efcpt.Build automatically discovers the SQL project in the solution
- **Microsoft.Build.Sql**: Uses Microsoft's official SDK-style SQL project format

## Project Structure

```
ZeroConfigMsBuildSql.sln
├── DatabaseProject/           # Microsoft.Build.Sql SQL project
│   ├── DatabaseProject.csproj # Uses Microsoft.Build.Sql SDK
│   └── dbo/Tables/
│       ├── Author.sql
│       ├── Blog.sql
│       └── Post.sql
└── EntityFrameworkCoreProject/
    └── EntityFrameworkCoreProject.csproj  # Only JD.Efcpt.Build + EF Core
```

## EntityFrameworkCoreProject.csproj

Notice how minimal the configuration is:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="JD.Efcpt.Build" Version="*" />
        <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

That's it! No explicit configuration properties. JD.Efcpt.Build:
1. Enables automatically (default: `EfcptEnabled=true`)
2. Discovers the SQL project in the solution
3. Generates EF Core models during build

## Building

```bash
dotnet build ZeroConfigMsBuildSql.sln
```

Generated files appear in `EntityFrameworkCoreProject/obj/efcpt/Generated/`.

## Why Microsoft.Build.Sql?

[Microsoft.Build.Sql](https://github.com/microsoft/DacFx) is Microsoft's official SDK-style SQL project format:
- Cross-platform (works on Windows, Linux, macOS)
- Modern SDK-style project format
- Active development by Microsoft
- Works with Azure Data Studio and VS Code
