# Using JD.Efcpt.Sdk

JD.Efcpt.Sdk is an MSBuild SDK that provides the cleanest possible integration for EF Core model generation. Instead of adding a `PackageReference`, you use it as your project's SDK, resulting in minimal configuration and maximum convenience.

## Overview

The SDK approach offers several advantages:

- **Cleaner project files** - No PackageReference needed for JD.Efcpt.Build
- **Extends Microsoft.NET.Sdk** - All standard .NET SDK features work as expected
- **Automatic detection** - SQL projects referenced via `ProjectReference` are automatically discovered
- **Zero configuration** - Works out of the box with sensible defaults

## When to Use the SDK

Choose JD.Efcpt.Sdk when:

- You want the **simplest possible setup**
- Your project is **dedicated to EF Core model generation**
- You're starting a **fresh project** without existing PackageReferences

Choose JD.Efcpt.Build (PackageReference) when:

- You need to **add EF Core generation to an existing project**
- Your project already uses a custom SDK
- You prefer version management via **Directory.Build.props**

## Quick Start

Use the SDK in your project file with the version specified inline:

```xml
<Project Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\DatabaseProject\DatabaseProject.sqlproj">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <OutputItemType>None</OutputItemType>
        </ProjectReference>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

Then build:

```bash
dotnet build
```

Generated files appear in `obj/efcpt/Generated/`.

## Solution Structure

A typical SDK-based solution looks like this:

```
YourSolution/
├── YourSolution.sln
├── src/
│   ├── DatabaseProject/
│   │   └── DatabaseProject.sqlproj   # SQL Project (Microsoft.Build.Sql)
│   └── YourApp.Data/
│       └── YourApp.Data.csproj       # Uses JD.Efcpt.Sdk/1.0.0
```

## How It Works

When you use `JD.Efcpt.Sdk` as your project SDK:

1. **SDK Resolution** - MSBuild resolves the SDK from NuGet using the version in the Sdk attribute
2. **SDK Integration** - The SDK extends `Microsoft.NET.Sdk` by importing it and adding EF Core Power Tools integration
3. **SQL Project Detection** - Any `ProjectReference` to a SQL project is automatically detected
4. **DACPAC Build** - The SQL project is built to produce a DACPAC
5. **Model Generation** - EF Core Power Tools generates models from the DACPAC
6. **Compilation** - Generated `.g.cs` files are included in the build

## Configuration

All configuration options from JD.Efcpt.Build work with the SDK. You can use:

### MSBuild Properties

```xml
<PropertyGroup>
    <EfcptConfigRootNamespace>MyApp.Data</EfcptConfigRootNamespace>
    <EfcptConfigDbContextName>ApplicationDbContext</EfcptConfigDbContextName>
</PropertyGroup>
```

### Configuration Files

Place `efcpt-config.json` in your project directory:

```json
{
  "names": {
    "root-namespace": "MyApp.Data",
    "dbcontext-name": "ApplicationDbContext"
  },
  "code-generation": {
    "use-nullable-reference-types": true
  }
}
```

See [Configuration](configuration.md) for all available options.

## ProjectReference Requirements

When referencing a SQL project, you must disable assembly reference since SQL projects don't produce .NET assemblies:

```xml
<ItemGroup>
    <ProjectReference Include="..\DatabaseProject\DatabaseProject.sqlproj">
        <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
        <OutputItemType>None</OutputItemType>
    </ProjectReference>
</ItemGroup>
```

| Property | Value | Purpose |
|----------|-------|---------|
| `ReferenceOutputAssembly` | `false` | SQL projects don't produce .NET assemblies |
| `OutputItemType` | `None` | Prevents MSBuild from treating DACPAC as a reference |

## Supported SQL Project Types

The SDK works with all SQL project types:

| SDK | Project Extension | Notes |
|-----|-------------------|-------|
| [Microsoft.Build.Sql](https://github.com/microsoft/DacFx) | `.sqlproj` | Microsoft's official SDK, cross-platform |
| [MSBuild.Sdk.SqlProj](https://github.com/rr-wfm/MSBuild.Sdk.SqlProj) | `.csproj` / `.fsproj` | Community SDK, cross-platform |
| Traditional SQL Projects | `.sqlproj` | Legacy format, Windows only |

## Connection String Mode

The SDK also supports connection string mode for direct database reverse engineering:

```xml
<Project Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;</EfcptConnectionString>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    </ItemGroup>
</Project>
```

See [Connection String Mode](connection-string-mode.md) for details.

## Multi-Target Framework Support

The SDK supports multi-targeting just like the standard .NET SDK:

```xml
<Project Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION">
    <PropertyGroup>
        <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    </PropertyGroup>
    <!-- ... -->
</Project>
```

Model generation happens once and is shared across all target frameworks.

## Comparison: SDK vs PackageReference

| Feature | JD.Efcpt.Sdk | JD.Efcpt.Build (PackageReference) |
|---------|--------------|-----------------------------------|
| Project file | `Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION"` | `<PackageReference Include="JD.Efcpt.Build" />` |
| Version location | Sdk attribute or `global.json` | `.csproj` or Directory.Build.props |
| Setup complexity | Lower | Slightly higher |
| Existing projects | Requires SDK change | Drop-in addition |
| Custom SDKs | Not compatible | Compatible |
| All features | ✅ Same | ✅ Same |

## Sample Project

See the [sdk-zero-config](https://github.com/jerrettdavis/JD.Efcpt.Build/tree/main/samples/sdk-zero-config) sample for a complete working example.

```
sdk-zero-config/
├── SdkZeroConfigSample.sln
├── DatabaseProject/
│   ├── DatabaseProject.csproj         # Microsoft.Build.Sql project
│   └── dbo/Tables/*.sql
└── EntityFrameworkCoreProject/
    └── EntityFrameworkCoreProject.csproj  # Uses JD.Efcpt.Sdk/1.0.0
```

## Centralized Version Management (Optional)

If you have multiple projects using JD.Efcpt.Sdk and want to manage the version in one place, you can use `global.json`:

```json
{
  "msbuild-sdks": {
    "JD.Efcpt.Sdk": "1.0.0"
  }
}
```

Then your project files can omit the version:

```xml
<Project Sdk="JD.Efcpt.Sdk">
    <!-- ... -->
</Project>
```

## Staying Up-to-Date

Unlike regular NuGet PackageReferences, MSBuild SDKs don't have built-in support for update notifications. Here are strategies to keep your SDK version current:

### Opt-in Update Check

Enable automatic version checking by setting `EfcptCheckForUpdates` in your project:

```xml
<PropertyGroup>
    <EfcptCheckForUpdates>true</EfcptCheckForUpdates>
</PropertyGroup>
```

When enabled, the build will check NuGet for newer versions (cached for 24 hours) and emit a warning if an update is available:

```
warning EFCPT002: A newer version of JD.Efcpt.Sdk is available: 1.1.0 (current: 1.0.0).
```

Configuration options:
- `EfcptCheckForUpdates` - Enable/disable version checking (default: `false` for package references, `true` for SDK references)
- `EfcptSdkVersionWarningLevel` - Control severity of update notifications: `None`, `Info`, `Warn` (default), or `Error`
- `EfcptUpdateCheckCacheHours` - Hours to cache the result (default: `24`)
- `EfcptForceUpdateCheck` - Bypass cache and always check (default: `false`)

Example: Make version updates informational instead of warnings:

```xml
<PropertyGroup>
    <EfcptSdkVersionWarningLevel>Info</EfcptSdkVersionWarningLevel>
</PropertyGroup>
```

### Use global.json for Centralized Management

When you have multiple projects, use `global.json` to manage SDK versions in one place:

```json
{
  "msbuild-sdks": {
    "JD.Efcpt.Sdk": "1.0.0"
  }
}
```

Then update the version in `global.json` when you want to upgrade all projects at once.

### Consider PackageReference for Update Tools

If you prefer using tools like `dotnet outdated` for version management, use `JD.Efcpt.Build` via PackageReference instead of the SDK approach. Both provide identical functionality.

## Troubleshooting

### SDK not found

If you see an error like "The SDK 'JD.Efcpt.Sdk' could not be resolved":

1. Verify the version is specified (either inline `Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION"` or in `global.json`)
2. Check that the version matches an available package version
3. Ensure the package is available in your NuGet sources

### DACPAC not building

If the SQL project isn't building:

1. Verify the `ProjectReference` is correct
2. Check that `ReferenceOutputAssembly` is set to `false`
3. Try building the SQL project independently: `dotnet build DatabaseProject.sqlproj`

### Version conflicts

If you need different SDK versions for different projects:

1. Specify the version inline in each project file: `Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION"`
2. Or use JD.Efcpt.Build via PackageReference instead

## Next Steps

- [Configuration](configuration.md) - Explore all configuration options
- [Core Concepts](core-concepts.md) - Understand the build pipeline
- [T4 Templates](t4-templates.md) - Customize code generation
