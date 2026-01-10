# Split Outputs

This guide explains how to use the Split Outputs feature to separate generated entity models from your DbContext into different projects, enabling clean architecture patterns and reducing unnecessary dependencies.

## Table of Contents

- [Overview](#overview)
- [When to Use Split Outputs](#when-to-use-split-outputs)
- [Architecture](#architecture)
- [Step-by-Step Tutorial](#step-by-step-tutorial)
- [Configuration Reference](#configuration-reference)
- [How It Works](#how-it-works)
- [Incremental Builds](#incremental-builds)
- [Common Scenarios](#common-scenarios)
- [Best Practices](#best-practices)
- [Migrating from Single Project](#migrating-from-single-project)
- [Troubleshooting](#troubleshooting)

---

## Overview

By default, JD.Efcpt.Build generates all EF Core artifacts (entities, DbContext, configurations) into a single project. The **Split Outputs** feature allows you to:

- **Generate all files in the Models project** (the primary project with no EF Core dependencies)
- **Automatically copy DbContext and configurations to the Data project** (which has EF Core dependencies)
- **Keep entity models in the Models project** for use by projects that shouldn't reference EF Core

This separation enables clean architecture patterns where your domain models remain free of infrastructure concerns.

---

## When to Use Split Outputs

### Use Split Outputs When:

| Scenario | Benefit |
|----------|---------|
| **Clean Architecture** | Domain models stay in a pure domain layer without EF Core dependencies |
| **Shared Domain Models** | Multiple projects can reference entity models without pulling in EF Core |
| **API DTOs** | Use entity models directly in API projects without heavy dependencies |
| **Blazor WebAssembly** | Share models with client-side code that can't reference EF Core |
| **Testing** | Unit test domain logic without mocking EF Core infrastructure |
| **Microservices** | Share domain models across service boundaries |

### Don't Use Split Outputs When:

- You have a simple application with a single data access project
- All consumers of your entities need EF Core anyway
- You prefer simpler project structures over architectural purity

---

## Architecture

### Project Layout

```
MySolution/
+-- MyDatabase/                    # SQL Server Database Project
|   +-- MyDatabase.sqlproj
|   +-- dbo/Tables/
|       +-- Customers.sql
|       +-- Orders.sql
|
+-- MyProject.Models/              # PRIMARY PROJECT (runs efcpt)
|   +-- MyProject.Models.csproj    # No EF Core dependencies
|   +-- efcpt-config.json          # efcpt configuration
|   +-- efcpt.renaming.json
|   +-- Template/                  # T4 templates (optional)
|   +-- obj/efcpt/Generated/
|       +-- Models/                # Entity models (KEPT here)
|       |   +-- Customer.g.cs
|       |   +-- Order.g.cs
|       +-- MyDbContext.g.cs       # DbContext (COPIED to Data)
|       +-- Configurations/        # Configs (COPIED to Data)
|           +-- CustomerConfiguration.g.cs
|           +-- OrderConfiguration.g.cs
|
+-- MyProject.Data/                # SECONDARY PROJECT (receives files)
|   +-- MyProject.Data.csproj      # Has EF Core dependencies
|   +-- obj/efcpt/Generated/       # Receives DbContext and configs
|       +-- MyDbContext.g.cs
|       +-- Configurations/
|           +-- CustomerConfiguration.g.cs
|           +-- OrderConfiguration.g.cs
|
+-- MyProject.Api/                 # Can reference either or both
    +-- MyProject.Api.csproj
```

### Data Flow Diagram

```
                              BUILD SEQUENCE
                              =============

   +-------------------+
   | 1. SQL Project    |
   | (MyDatabase)      |
   +---------+---------+
             |
             | produces DACPAC
             v
   +-------------------+      +----------------------------------+
   | 2. Models Project |----->| efcpt generates ALL files        |
   | (PRIMARY)         |      | - Models/*.g.cs                  |
   +---------+---------+      | - DbContext.g.cs                 |
             |                | - Configurations/*.g.cs          |
             |                +----------------------------------+
             |
             | copies DbContext + Configurations
             v
   +-------------------+
   | 3. Data Project   |
   | (SECONDARY)       |
   +-------------------+
             |
             | compiles with copied files
             | + reference to Models assembly
             v
   +-------------------+
   | 4. API/Web/etc    |
   | (consumers)       |
   +-------------------+
```

### Dependency Graph

```
                    +------------------+
                    |   SQL Project    |
                    | (schema source)  |
                    +--------+---------+
                             |
              +--------------+--------------+
              |                             |
              v                             |
    +------------------+                    |
    |  Models Project  |<-------------------+
    | (entities only)  |    (ProjectReference)
    +--------+---------+
             |
             | (ProjectReference)
             v
    +------------------+
    |   Data Project   |
    | (DbContext + EF) |
    +--------+---------+
             |
             | (ProjectReference)
             v
    +------------------+
    |    API Project   |
    | (or any consumer)|
    +------------------+
```

---

## Step-by-Step Tutorial

This walkthrough creates a complete split outputs setup from scratch.

### Prerequisites

- .NET 8.0 SDK or later
- A SQL Server Database Project (`.sqlproj`) or DACPAC file
- JD.Efcpt.Build NuGet package

### Step 1: Create the Solution Structure

```powershell
# Create solution
mkdir MySolution
cd MySolution
dotnet new sln -n MySolution

# Create projects
dotnet new classlib -n MyProject.Models -f net8.0
dotnet new classlib -n MyProject.Data -f net8.0

# Add to solution
dotnet sln add MyProject.Models/MyProject.Models.csproj
dotnet sln add MyProject.Data/MyProject.Data.csproj
```

### Step 2: Configure the Models Project (Primary)

Edit `MyProject.Models/MyProject.Models.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- JD.Efcpt.Build package -->
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="PACKAGE_VERSION" />
  </ItemGroup>

  <!-- efcpt Configuration -->
  <PropertyGroup>
    <!-- This is the PRIMARY project - it runs efcpt -->
    <EfcptEnabled>true</EfcptEnabled>

    <!-- Enable split outputs -->
    <EfcptSplitOutputs>true</EfcptSplitOutputs>

    <!-- Point to the Data project -->
    <EfcptDataProject>..\MyProject.Data\MyProject.Data.csproj</EfcptDataProject>

    <!-- Optional: Enable detailed logging -->
    <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
  </PropertyGroup>

  <!-- Reference your SQL project (adjust path as needed) -->
  <ItemGroup>
    <ProjectReference Include="..\MyDatabase\MyDatabase.sqlproj">
      <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
      <OutputItemType>None</OutputItemType>
    </ProjectReference>
  </ItemGroup>

  <!-- IMPORTANT: No EF Core dependencies here - only DataAnnotations -->
  <ItemGroup>
    <PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
  </ItemGroup>
</Project>
```

### Step 3: Configure the Data Project (Secondary)

Edit `MyProject.Data/MyProject.Data.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- JD.Efcpt.Build package (needed for external data support) -->
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="PACKAGE_VERSION" />
  </ItemGroup>

  <!-- efcpt Configuration -->
  <PropertyGroup>
    <!-- This is the SECONDARY project - it does NOT run efcpt -->
    <EfcptEnabled>false</EfcptEnabled>

    <!-- Include DbContext/configs copied from Models project -->
    <EfcptExternalDataDir>$(MSBuildProjectDirectory)\obj\efcpt\Generated\</EfcptExternalDataDir>
  </PropertyGroup>

  <!-- Reference the Models project (creates build dependency) -->
  <ItemGroup>
    <ProjectReference Include="..\MyProject.Models\MyProject.Models.csproj" />
  </ItemGroup>

  <!-- EF Core dependencies live HERE, not in Models -->
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### Step 4: Add efcpt Configuration Files

Create `MyProject.Models/efcpt-config.json`:

```json
{
  "names": {
    "root-namespace": "MyProject",
    "dbcontext-name": "MyDbContext",
    "dbcontext-namespace": "Data",
    "model-namespace": "Models"
  },
  "code-generation": {
    "use-t4": true,
    "t4-template-path": ".",
    "enable-on-configuring": false
  },
  "file-layout": {
    "output-path": "Models",
    "output-dbcontext-path": ".",
    "use-schema-folders-preview": false
  }
}
```

Create `MyProject.Models/efcpt.renaming.json`:

```json
[]
```

### Step 5: Build and Verify

```powershell
# Build the solution
dotnet build

# Verify Models project has entity files
ls MyProject.Models/obj/efcpt/Generated/Models/

# Verify Data project has DbContext and configurations
ls MyProject.Data/obj/efcpt/Generated/
ls MyProject.Data/obj/efcpt/Generated/Configurations/
```

### Step 6: Use in Your Application

In the Data project, you can now use the DbContext:

```csharp
// MyProject.Data/Services/CustomerService.cs
using MyProject.Data;
using MyProject.Models;

public class CustomerService
{
    private readonly MyDbContext _context;

    public CustomerService(MyDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        return await _context.Customers.ToListAsync();
    }
}
```

In other projects, you can use the models without EF Core:

```csharp
// MyProject.Api/Models/CustomerDto.cs
using MyProject.Models;

public static class CustomerMapper
{
    // Models project has no EF Core dependency!
    public static CustomerDto ToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name
        };
    }
}
```

---

## Configuration Reference

### Models Project Properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `EfcptEnabled` | Yes | `true` | Must be `true` for the primary project |
| `EfcptSplitOutputs` | Yes | `false` | Set to `true` to enable split outputs |
| `EfcptDataProject` | Yes | (none) | Relative or absolute path to the Data project |
| `EfcptDataProjectOutputSubdir` | No | `obj\efcpt\Generated\` | Destination folder in Data project |

### Data Project Properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `EfcptEnabled` | Yes | `true` | Must be `false` for the secondary project |
| `EfcptExternalDataDir` | Yes | (none) | Path where DbContext/configs are copied |

### Complete Example

**Models Project:**
```xml
<PropertyGroup>
  <EfcptEnabled>true</EfcptEnabled>
  <EfcptSplitOutputs>true</EfcptSplitOutputs>
  <EfcptDataProject>..\MyProject.Data\MyProject.Data.csproj</EfcptDataProject>
  <EfcptDataProjectOutputSubdir>obj\efcpt\Generated\</EfcptDataProjectOutputSubdir>
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
</PropertyGroup>
```

**Data Project:**
```xml
<PropertyGroup>
  <EfcptEnabled>false</EfcptEnabled>
  <EfcptExternalDataDir>$(MSBuildProjectDirectory)\obj\efcpt\Generated\</EfcptExternalDataDir>
</PropertyGroup>
```

---

## How It Works

### Build Targets

The split outputs feature uses several MSBuild targets:

1. **EfcptGenerateModels** - Generates all files in the Models project
2. **EfcptValidateSplitOutputs** - Validates configuration and resolves paths
3. **EfcptCopyDataToDataProject** - Copies DbContext and configurations
4. **EfcptAddToCompile** - Includes appropriate files in each project
5. **EfcptIncludeExternalData** - Includes copied files in Data project

### File Classification

| File Pattern | Destination |
|--------------|-------------|
| `Models/**/*.g.cs` | Stays in Models project |
| `*Context.g.cs` (root level) | Copied to Data project |
| `*Configuration.g.cs` | Copied to Data project's `Configurations/` folder |
| `Configurations/**/*.g.cs` | Copied to Data project's `Configurations/` folder |

### Build Sequence

```
1. SQL Project builds (produces DACPAC)
       |
       v
2. Models Project builds:
   a. EfcptResolveInputs - Find DACPAC and config files
   b. EfcptStageInputs - Stage config and templates
   c. EfcptComputeFingerprint - Check if regeneration needed
   d. EfcptGenerateModels - Run efcpt CLI (if fingerprint changed)
   e. EfcptCopyDataToDataProject - Copy DbContext/configs to Data
   f. EfcptAddToCompile - Include Models/**/*.g.cs
   g. CoreCompile - Compile Models assembly
       |
       v
3. Data Project builds:
   a. EfcptIncludeExternalData - Include copied DbContext/configs
   b. CoreCompile - Compile Data assembly
```

---

## Incremental Builds

### How Fingerprinting Works

JD.Efcpt.Build uses fingerprinting to avoid unnecessary regeneration:

1. **First build**: Generates files, computes fingerprint, creates stamp file
2. **Subsequent builds**: Compares fingerprint; if unchanged, skips generation
3. **When inputs change**: DACPAC, config, or templates change → regenerate

### What Triggers Regeneration

| Change | Regenerates? |
|--------|--------------|
| SQL schema change (DACPAC) | Yes |
| efcpt-config.json change | Yes |
| efcpt.renaming.json change | Yes |
| T4 template change | Yes |
| C# code in Models project | No |
| C# code in Data project | No |
| Clean build | Yes |

### File Preservation on Skip

When generation is skipped:
- Models project keeps existing `Models/**/*.g.cs` files
- Data project keeps existing DbContext and configuration files
- No files are deleted or modified

This ensures stable incremental builds without losing generated code.

---

## Common Scenarios

### Adding a New Entity

1. Add the table to your SQL project:
   ```sql
   -- MyDatabase/dbo/Tables/NewEntity.sql
   CREATE TABLE [dbo].[NewEntity] (
       [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
       [Name] NVARCHAR(100) NOT NULL
   );
   ```

2. Build the solution:
   ```powershell
   dotnet build
   ```

3. The fingerprint changes, triggering regeneration:
   - `NewEntity.g.cs` appears in Models project
   - `NewEntityConfiguration.g.cs` appears in Data project

### Renaming an Entity

1. Update `efcpt.renaming.json`:
   ```json
   [
     {
       "name": "OldName",
       "new-name": "NewName"
     }
   ]
   ```

2. Build to apply renaming:
   ```powershell
   dotnet build
   ```

### Customizing Generated Code

1. Create custom T4 templates in `MyProject.Models/Template/CodeTemplates/EFCore/`
2. Modify templates as needed
3. Build to regenerate with custom templates

### Adding a Custom DbContext Method

Since DbContext is generated, extend it with a partial class:

```csharp
// MyProject.Data/MyDbContextExtensions.cs
namespace MyProject.Data;

public partial class MyDbContext
{
    // Add custom methods here
    public IQueryable<Customer> GetActiveCustomers()
    {
        return Customers.Where(c => c.IsActive);
    }
}
```

### Using with Dependency Injection

```csharp
// Program.cs or Startup.cs
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString));
```

---

## Best Practices

### Project Organization

1. **Keep Models project minimal** - Only entity classes and shared types
2. **Put all EF logic in Data project** - Migrations, DbContext extensions, repositories
3. **Use meaningful namespaces** - `MyProject.Models` and `MyProject.Data`

### Dependencies

1. **Models project should only reference:**
   - `System.ComponentModel.Annotations` (for data annotations)
   - Other pure .NET libraries (no EF Core!)

2. **Data project should reference:**
   - Models project
   - EF Core packages
   - Database providers

### Template Configuration

1. **Use consistent output paths:**
   ```json
   {
     "file-layout": {
       "output-path": "Models",
       "output-dbcontext-path": "."
     }
   }
   ```

2. **Match namespaces to project names:**
   ```json
   {
     "names": {
       "model-namespace": "Models",
       "dbcontext-namespace": "Data"
     }
   }
   ```

### Version Control

1. **Don't commit generated files** - Add to `.gitignore`:
   ```
   **/obj/efcpt/
   ```

2. **Do commit configuration files:**
   - `efcpt-config.json`
   - `efcpt.renaming.json`
   - `Template/` folder (if customized)

---

## Migrating from Single Project

### Before (Single Project)

```
MyProject.Data/
  MyProject.Data.csproj       # Has EF Core + generates everything
  efcpt-config.json
  obj/efcpt/Generated/
    Models/
    MyDbContext.g.cs
    Configurations/
```

### Migration Steps

1. **Create the Models project:**
   ```powershell
   dotnet new classlib -n MyProject.Models
   ```

2. **Move efcpt configuration to Models:**
   ```powershell
   mv MyProject.Data/efcpt-config.json MyProject.Models/
   mv MyProject.Data/efcpt.renaming.json MyProject.Models/
   mv MyProject.Data/Template MyProject.Models/  # If exists
   ```

3. **Update Models project csproj** (see [Configuration Reference](#configuration-reference))

4. **Update Data project csproj:**
   - Set `EfcptEnabled=false`
   - Add `EfcptExternalDataDir`
   - Add ProjectReference to Models
   - Remove SQL project reference (now in Models)

5. **Update namespace references** in any consuming code

6. **Clean and rebuild:**
   ```powershell
   dotnet clean
   dotnet build
   ```

### After (Split Projects)

```
MyProject.Models/
  MyProject.Models.csproj     # No EF Core, generates entities
  efcpt-config.json
  obj/efcpt/Generated/
    Models/                   # Entity models stay here

MyProject.Data/
  MyProject.Data.csproj       # Has EF Core, receives DbContext
  obj/efcpt/Generated/
    MyDbContext.g.cs          # Copied from Models
    Configurations/           # Copied from Models
```

---

## Troubleshooting

### Build Errors

#### "EfcptDataProject is not set"

**Cause:** Split outputs enabled but Data project path not specified.

**Solution:** Add to Models project:
```xml
<EfcptDataProject>..\MyProject.Data\MyProject.Data.csproj</EfcptDataProject>
```

#### "EfcptDataProject was specified but the file does not exist"

**Cause:** Path to Data project is incorrect.

**Solution:** Verify the relative path is correct:
```powershell
# From Models project directory
ls ..\MyProject.Data\MyProject.Data.csproj
```

#### Duplicate type definitions

**Cause:** Same types being compiled in both projects.

**Solution:** Ensure:
- Models project only compiles `Models/**/*.g.cs` (automatic in split mode)
- Data project uses `EfcptExternalDataDir` (not direct file references)
- No manual `<Compile Include>` for generated files

### Missing Files

#### No DbContext in Data project

**Cause:** Templates not generating DbContext at root level.

**Solution:** Check efcpt-config.json:
```json
{
  "file-layout": {
    "output-dbcontext-path": "."  // Must be root, not a subdirectory
  }
}
```

Verify after build:
```powershell
ls MyProject.Models/obj/efcpt/Generated/*.g.cs
```

#### No entity models in Models project

**Cause:** Templates not generating to Models subdirectory.

**Solution:** Check efcpt-config.json:
```json
{
  "file-layout": {
    "output-path": "Models"  // Must output to Models subdirectory
  }
}
```

#### Files missing after second build

**Cause:** Using an older version without the incremental build fix.

**Solution:** Update to the latest JD.Efcpt.Build version and do a fresh restore:
```powershell
dotnet restore --force
dotnet build
```

### Runtime Errors

#### Entity types not recognized by DbContext

**Cause:** Namespace mismatch between entities and DbContext.

**Solution:** Ensure namespaces are consistent in efcpt-config.json:
```json
{
  "names": {
    "root-namespace": "MyProject",
    "dbcontext-namespace": "Data",
    "model-namespace": "Models"
  }
}
```

The DbContext should have `using MyProject.Models;` to reference entity types.

### Debugging Tips

1. **Enable detailed logging:**
   ```xml
   <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
   <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
   ```

2. **Check build output for messages:**
   ```
   Split outputs enabled. DbContext and configurations will be copied to: ...
   Copied 4 data files to Data project: ...
   ```

3. **Verify file structure after build:**
   ```powershell
   tree MyProject.Models/obj/efcpt/Generated
   tree MyProject.Data/obj/efcpt/Generated
   ```

4. **Force regeneration:**
   ```powershell
   rm MyProject.Models/obj/efcpt/.efcpt.stamp
   dotnet build
   ```

---

## Next Steps

- [Getting Started](getting-started.md) - Basic setup guide
- [T4 Templates](t4-templates.md) - Customizing generated code
- [Configuration](configuration.md) - All configuration options
- [CI/CD](ci-cd.md) - Continuous integration setup
- [Troubleshooting](troubleshooting.md) - More common issues and solutions
