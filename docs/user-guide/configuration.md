# Configuration

JD.Efcpt.Build can be configured through MSBuild properties and JSON configuration files. This guide covers all available options.

## Configuration Hierarchy

The package uses a three-level configuration hierarchy:

1. **Package Defaults** - Sensible defaults shipped with the NuGet package
2. **JSON Configuration Files** - Project-level `efcpt-config.json` and `efcpt.renaming.json`
3. **MSBuild Properties** - Highest priority, override everything else

## MSBuild Properties

Set these properties in your `.csproj` file or `Directory.Build.props`.

### Core Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptEnabled` | `true` | Master switch for the entire pipeline |
| `EfcptSqlProj` | *(auto-discovered)* | Path to `.sqlproj` file |
| `EfcptDacpac` | *(empty)* | Path to pre-built `.dacpac` file (skips .sqlproj build) |
| `EfcptConfig` | `efcpt-config.json` | EF Core Power Tools configuration file |
| `EfcptRenaming` | `efcpt.renaming.json` | Renaming rules file |
| `EfcptTemplateDir` | `Template` | T4 template directory |

### Output Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptOutput` | `$(BaseIntermediateOutputPath)efcpt\` | Intermediate staging directory |
| `EfcptGeneratedDir` | `$(EfcptOutput)Generated\` | Generated code output directory |
| `EfcptFingerprintFile` | `$(EfcptOutput)fingerprint.txt` | Fingerprint cache location |
| `EfcptStampFile` | `$(EfcptOutput).efcpt.stamp` | Generation stamp file |

### Connection String Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptConnectionString` | *(empty)* | Explicit connection string (enables connection string mode) |
| `EfcptAppSettings` | *(empty)* | Path to `appsettings.json` for connection string |
| `EfcptAppConfig` | *(empty)* | Path to `app.config` or `web.config` for connection string |
| `EfcptConnectionStringName` | `DefaultConnection` | Key name in configuration file |
| `EfcptProvider` | `mssql` | Database provider identifier |

### Tool Configuration Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptToolMode` | `auto` | Tool resolution mode: `auto`, `tool-manifest`, or global |
| `EfcptToolPackageId` | `ErikEJ.EFCorePowerTools.Cli` | NuGet package ID for CLI |
| `EfcptToolVersion` | `10.*` | Version constraint |
| `EfcptToolCommand` | `efcpt` | Command name |
| `EfcptToolPath` | *(empty)* | Explicit path to efcpt executable |
| `EfcptDotNetExe` | `dotnet` | Path to dotnet host |
| `EfcptToolRestore` | `true` | Whether to restore/update tool |

### Discovery Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptSolutionDir` | `$(SolutionDir)` | Solution root for project discovery |
| `EfcptSolutionPath` | `$(SolutionPath)` | Solution file path (fallback discovery) |
| `EfcptProbeSolutionDir` | `true` | Whether to probe solution directory |

### Diagnostic Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptLogVerbosity` | `minimal` | Logging level: `minimal` or `detailed` |
| `EfcptDumpResolvedInputs` | `false` | Log all resolved input paths |

### Config Override Properties

These properties override values in `efcpt-config.json` without editing the JSON file directly. This is useful for CI/CD scenarios or when you want different settings per build configuration.

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptApplyMsBuildOverrides` | `true` | Whether to apply MSBuild property overrides to user-provided config files |

#### Names Section Overrides

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigRootNamespace` | `root-namespace` | Root namespace for generated code |
| `EfcptConfigDbContextName` | `dbcontext-name` | Name of the DbContext class |
| `EfcptConfigDbContextNamespace` | `dbcontext-namespace` | Namespace for the DbContext class |
| `EfcptConfigModelNamespace` | `model-namespace` | Namespace for entity model classes |

#### File Layout Section Overrides

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigOutputPath` | `output-path` | Output path for generated entity files |
| `EfcptConfigDbContextOutputPath` | `output-dbcontext-path` | Output path for the DbContext file |
| `EfcptConfigSplitDbContext` | `split-dbcontext-preview` | Enable split DbContext generation (preview) |
| `EfcptConfigUseSchemaFolders` | `use-schema-folders-preview` | Use schema-based folders (preview) |
| `EfcptConfigUseSchemaNamespaces` | `use-schema-namespaces-preview` | Use schema-based namespaces (preview) |

#### Code Generation Section Overrides

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigEnableOnConfiguring` | `enable-on-configuring` | Add OnConfiguring method to the DbContext |
| `EfcptConfigGenerationType` | `type` | Type of files to generate: `all`, `dbcontext`, `entities` |
| `EfcptConfigUseDatabaseNames` | `use-database-names` | Use table and column names from the database |
| `EfcptConfigUseDataAnnotations` | `use-data-annotations` | Use DataAnnotation attributes rather than fluent API |
| `EfcptConfigUseNullableReferenceTypes` | `use-nullable-reference-types` | Use nullable reference types |
| `EfcptConfigUseInflector` | `use-inflector` | Pluralize or singularize generated names |
| `EfcptConfigUseLegacyInflector` | `use-legacy-inflector` | Use EF6 Pluralizer instead of Humanizer |
| `EfcptConfigUseManyToManyEntity` | `use-many-to-many-entity` | Preserve many-to-many entity instead of skipping |
| `EfcptConfigUseT4` | `use-t4` | Customize code using T4 templates |
| `EfcptConfigUseT4Split` | `use-t4-split` | Customize code using T4 templates with EntityTypeConfiguration.t4 |
| `EfcptConfigRemoveDefaultSqlFromBool` | `remove-defaultsql-from-bool-properties` | Remove SQL default from bool columns |
| `EfcptConfigSoftDeleteObsoleteFiles` | `soft-delete-obsolete-files` | Run cleanup of obsolete files |
| `EfcptConfigDiscoverMultipleResultSets` | `discover-multiple-stored-procedure-resultsets-preview` | Discover multiple result sets from stored procedures (preview) |
| `EfcptConfigUseAlternateResultSetDiscovery` | `use-alternate-stored-procedure-resultset-discovery` | Use sp_describe_first_result_set for result set discovery |
| `EfcptConfigT4TemplatePath` | `t4-template-path` | Global path to T4 templates |
| `EfcptConfigUseNoNavigations` | `use-no-navigations-preview` | Remove all navigation properties (preview) |
| `EfcptConfigMergeDacpacs` | `merge-dacpacs` | Merge .dacpac files when using references |
| `EfcptConfigRefreshObjectLists` | `refresh-object-lists` | Refresh object lists from database during scaffolding |
| `EfcptConfigGenerateMermaidDiagram` | `generate-mermaid-diagram` | Create a Mermaid ER diagram during scaffolding |
| `EfcptConfigUseDecimalAnnotationForSprocs` | `use-decimal-data-annotation-for-sproc-results` | Use explicit decimal annotation for stored procedure results |
| `EfcptConfigUsePrefixNavigationNaming` | `use-prefix-navigation-naming` | Use prefix-based naming of navigations (EF Core 8+) |
| `EfcptConfigUseDatabaseNamesForRoutines` | `use-database-names-for-routines` | Use database names for stored procedures and functions |
| `EfcptConfigUseInternalAccessForRoutines` | `use-internal-access-modifiers-for-sprocs-and-functions` | Use internal access modifiers for stored procedures and functions |

#### Type Mappings Section Overrides

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigUseDateOnlyTimeOnly` | `use-DateOnly-TimeOnly` | Map date and time to DateOnly/TimeOnly |
| `EfcptConfigUseHierarchyId` | `use-HierarchyId` | Map hierarchyId type |
| `EfcptConfigUseSpatial` | `use-spatial` | Map spatial columns |
| `EfcptConfigUseNodaTime` | `use-NodaTime` | Use NodaTime types |

#### Replacements Section Overrides

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigPreserveCasingWithRegex` | `preserve-casing-with-regex` | Preserve casing with regex when custom naming |

#### Override Behavior

- **Default config**: When using the library-provided default config, overrides are **always** applied
- **User-provided config**: Overrides are only applied if `EfcptApplyMsBuildOverrides` is `true` (default)
- **Empty values**: Empty or whitespace-only property values are treated as "no override" and preserve the original JSON value

#### Example Usage

Override settings via MSBuild properties in your `.csproj`:

```xml
<PropertyGroup>
  <EfcptConfigRootNamespace>MyApp.Data</EfcptConfigRootNamespace>
  <EfcptConfigDbContextName>AppDbContext</EfcptConfigDbContextName>
  <EfcptConfigUseNullableReferenceTypes>true</EfcptConfigUseNullableReferenceTypes>
  <EfcptConfigUseDateOnlyTimeOnly>true</EfcptConfigUseDateOnlyTimeOnly>
</PropertyGroup>
```

Or per-configuration in CI/CD:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <EfcptConfigEnableOnConfiguring>false</EfcptConfigEnableOnConfiguring>
</PropertyGroup>
```

## efcpt-config.json

The primary configuration file for EF Core Power Tools generation options.

### File Location

The file is resolved in this order:

1. Path specified in `<EfcptConfig>` property
2. `efcpt-config.json` in project directory
3. `efcpt-config.json` in solution directory
4. Package default

### Configuration Sections

#### names

Controls naming conventions for generated code:

```json
{
  "names": {
    "root-namespace": "MyApp.Data",
    "dbcontext-name": "ApplicationDbContext",
    "dbcontext-namespace": "MyApp.Data",
    "entity-namespace": "MyApp.Data.Entities"
  }
}
```

| Property | Description |
|----------|-------------|
| `root-namespace` | Root namespace for all generated code |
| `dbcontext-name` | Name of the generated DbContext class |
| `dbcontext-namespace` | Namespace for the DbContext |
| `entity-namespace` | Namespace for entity classes |

#### code-generation

Controls code generation features:

```json
{
  "code-generation": {
    "use-t4": true,
    "t4-template-path": "Template",
    "use-nullable-reference-types": true,
    "use-date-only-time-only": true,
    "enable-on-configuring": false,
    "use-data-annotations": false
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `use-t4` | `false` | Use T4 templates for generation |
| `t4-template-path` | `Template` | Path to T4 templates (relative to config) |
| `use-nullable-reference-types` | `true` | Generate nullable reference type annotations |
| `use-date-only-time-only` | `true` | Use `DateOnly`/`TimeOnly` types |
| `enable-on-configuring` | `false` | Generate `OnConfiguring` method |
| `use-data-annotations` | `false` | Use data annotations instead of Fluent API |

#### file-layout

Controls output file organization:

```json
{
  "file-layout": {
    "output-path": "Models",
    "output-dbcontext-path": ".",
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  }
}
```

| Property | Default | Description |
|----------|---------|-------------|
| `output-path` | `Models` | Subdirectory for entity classes |
| `output-dbcontext-path` | `.` | Subdirectory for DbContext |
| `use-schema-folders-preview` | `false` | Organize entities by database schema |
| `use-schema-namespaces-preview` | `false` | Use schema-based namespaces |

#### table-selection

Controls which tables are included:

```json
{
  "table-selection": [
    {
      "schema": "dbo",
      "include": true
    },
    {
      "schema": "audit",
      "include": false
    },
    {
      "schema": "dbo",
      "tables": ["Users", "Orders"],
      "include": true
    },
    {
      "schema": "dbo",
      "tables": ["__EFMigrationsHistory"],
      "include": false
    }
  ]
}
```

Each selection rule has:

| Property | Description |
|----------|-------------|
| `schema` | Database schema name |
| `tables` | Optional list of specific table names |
| `include` | Whether to include (`true`) or exclude (`false`) |

Rules are processed in order; later rules override earlier ones.

### Complete Example

```json
{
  "names": {
    "root-namespace": "MyApp.Data",
    "dbcontext-name": "AppDbContext",
    "dbcontext-namespace": "MyApp.Data",
    "entity-namespace": "MyApp.Data.Entities"
  },
  "code-generation": {
    "use-t4": true,
    "t4-template-path": ".",
    "use-nullable-reference-types": true,
    "use-date-only-time-only": true,
    "enable-on-configuring": false,
    "use-data-annotations": false
  },
  "file-layout": {
    "output-path": "Models",
    "output-dbcontext-path": ".",
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  },
  "table-selection": [
    {
      "schema": "dbo",
      "include": true
    },
    {
      "schema": "audit",
      "include": false
    }
  ]
}
```

## efcpt.renaming.json

Customize how database object names are mapped to C# names.

### File Location

Resolved in this order:

1. Path specified in `<EfcptRenaming>` property
2. `efcpt.renaming.json` in project directory
3. `efcpt.renaming.json` in solution directory
4. Package default (no renaming)

### File Structure

The renaming file is a JSON array where each entry represents a schema configuration:

```json
[
  {
    "SchemaName": "dbo",
    "Tables": [
      {
        "Name": "Categories",
        "NewName": "Category",
        "Columns": [
          {
            "Name": "Picture",
            "NewName": "Image"
          }
        ]
      }
    ],
    "UseSchemaName": false
  }
]
```

### Schema Entry Properties

| Property | Description |
|----------|-------------|
| `SchemaName` | The database schema name |
| `Tables` | Array of table renaming rules (optional) |
| `UseSchemaName` | Whether to include schema name in generated namespaces |

### Table Entry Properties

| Property | Description |
|----------|-------------|
| `Name` | Original table name in the database |
| `NewName` | New name for the generated entity class |
| `Columns` | Array of column renaming rules (optional) |

### Column Entry Properties

| Property | Description |
|----------|-------------|
| `Name` | Original column name in the database |
| `NewName` | New name for the generated property |

### Complete Example

```json
[
  {
    "SchemaName": "dbo",
    "Tables": [
      {
        "Name": "tblUsers",
        "NewName": "User",
        "Columns": [
          {
            "Name": "usr_id",
            "NewName": "Id"
          },
          {
            "Name": "usr_email",
            "NewName": "Email"
          }
        ]
      },
      {
        "Name": "tblOrders",
        "NewName": "Order",
        "Columns": [
          {
            "Name": "ord_id",
            "NewName": "Id"
          },
          {
            "Name": "ord_total",
            "NewName": "Total"
          }
        ]
      }
    ],
    "UseSchemaName": false
  },
  {
    "SchemaName": "audit",
    "UseSchemaName": true
  }
]
```

This example:
- Renames `tblUsers` to `User` and `tblOrders` to `Order` in the `dbo` schema
- Renames various columns with prefixes to cleaner names
- Keeps the `dbo` schema without a namespace prefix (`UseSchemaName: false`)
- Includes the `audit` schema name in generated namespaces (`UseSchemaName: true`)

## Common Configuration Patterns

### Minimal Configuration

Just add the package; everything is auto-discovered:

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
</ItemGroup>
```

### Custom Namespace

```json
{
  "names": {
    "root-namespace": "MyCompany.MyApp.Data",
    "dbcontext-name": "MyAppContext"
  }
}
```

### Schema-Based Organization

```json
{
  "file-layout": {
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  }
}
```

### Selective Table Generation

Include only specific tables:

```json
{
  "table-selection": [
    {
      "schema": "dbo",
      "tables": ["Users", "Orders", "Products", "Categories"],
      "include": true
    }
  ]
}
```

### Connection String Mode

```xml
<PropertyGroup>
  <EfcptAppSettings>appsettings.json</EfcptAppSettings>
  <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
</PropertyGroup>
```

### Team Configuration via Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <EfcptEnabled>true</EfcptEnabled>
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolVersion>10.*</EfcptToolVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.x.x" />
  </ItemGroup>
</Project>
```

## Next Steps

- [Connection String Mode](connection-string-mode.md) - Generate from live databases
- [T4 Templates](t4-templates.md) - Customize code generation templates
- [API Reference](api-reference.md) - Complete MSBuild task documentation
