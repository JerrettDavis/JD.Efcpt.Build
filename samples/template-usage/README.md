# Template Usage Sample

This directory demonstrates how to use the JD.Efcpt.Build.Templates package to create new SDK-based projects.

## Installation

First, install the templates package:

```bash
dotnet new install JD.Efcpt.Build.Templates
```

## Usage

### Command Line

Create a new EF Core Power Tools SDK project:

```bash
dotnet new efcptbuild --name MyDataProject
```

This creates a new project with:
- JD.Efcpt.Sdk as the project SDK
- EF Core dependencies
- Sample efcpt-config.json with best practices
- README with next steps

### Visual Studio

1. Open Visual Studio
2. Go to **File > New > Project**
3. Search for **"EF Core Power Tools SDK Project"**
4. Select the template and configure your project name and location
5. Click **Create**

## Template Features

The template creates a project with:

- ✅ **JD.Efcpt.Sdk** as the project SDK for cleanest setup
- ✅ **Entity Framework Core** dependencies (SQL Server provider)
- ✅ **Sample configuration** (`efcpt-config.json`) with sensible defaults
- ✅ **Nullable reference types** enabled
- ✅ **Instructions** for adding a database project reference

## Next Steps

After creating a project from the template:

1. **Add a database project reference** to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\YourDatabase\YourDatabase.sqlproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    <OutputItemType>None</OutputItemType>
  </ProjectReference>
</ItemGroup>
```

2. **Customize** `efcpt-config.json` for your needs (namespaces, schemas, etc.)

3. **Build** your project:

```bash
dotnet build
```

Generated models will appear in `obj/efcpt/Generated/`!

## Template Options

The template supports the following options:

| Option | Description | Default |
|--------|-------------|---------|
| `--name` | Project name | (required) |

## Uninstalling

To uninstall the template package:

```bash
dotnet new uninstall JD.Efcpt.Build.Templates
```

## Additional Resources

- [JD.Efcpt.Build Documentation](https://github.com/jerrettdavis/JD.Efcpt.Build)
- [EF Core Power Tools](https://github.com/ErikEJ/EFCorePowerTools)
