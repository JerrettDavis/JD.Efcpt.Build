# API Reference

This reference documents all MSBuild targets, tasks, and properties provided by JD.Efcpt.Build.

## MSBuild Targets

These targets are executed as part of the build pipeline:

| Target | Purpose | When It Runs |
|--------|---------|--------------|
| `EfcptResolveInputs` | Discovers database project and config files | Before build |
| `EfcptQuerySchemaMetadata` | Queries database schema (connection string mode) | After resolve |
| `EfcptEnsureDacpac` | Builds `.sqlproj` to DACPAC (DACPAC mode) | After resolve |
| `EfcptStageInputs` | Stages config and templates | After DACPAC/schema |
| `EfcptApplyConfigOverrides` | Applies MSBuild property overrides to staged config | After staging |
| `EfcptComputeFingerprint` | Detects if regeneration needed | After overrides |
| `EfcptGenerateModels` | Runs `efcpt` CLI | When fingerprint changes |
| `EfcptAddToCompile` | Adds `.g.cs` files to compilation | Before C# compile |

## MSBuild Tasks

### ResolveSqlProjAndInputs

Discovers database project and configuration files.

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `ProjectFullPath` | Yes | Full path to the consuming project |
| `ProjectDirectory` | Yes | Directory containing the consuming project |
| `Configuration` | Yes | Active build configuration (e.g., `Debug` or `Release`) |
| `ProjectReferences` | No | Project references of the consuming project |
| `SqlProjOverride` | No | Optional override path for the SQL project |
| `ConfigOverride` | No | Optional override path for efcpt config |
| `RenamingOverride` | No | Optional override path for renaming rules |
| `TemplateDirOverride` | No | Optional override path for templates |
| `SolutionDir` | No | Optional solution root to probe for inputs |
| `SolutionPath` | No | Optional solution file path |
| `ProbeSolutionDir` | No | Whether to probe solution directory (default: `true`) |
| `OutputDir` | Yes | Output directory for `resolved-inputs.json` |
| `DefaultsRoot` | No | Root directory containing packaged defaults |
| `DumpResolvedInputs` | No | Write `resolved-inputs.json` to OutputDir |
| `EfcptConnectionString` | No | Optional explicit connection string |
| `EfcptAppSettings` | No | Optional `appsettings.json` path |
| `EfcptAppConfig` | No | Optional `app.config`/`web.config` path |
| `EfcptConnectionStringName` | No | Connection string key (default: `DefaultConnection`) |

**Outputs:**

| Output | Description |
|--------|-------------|
| `SqlProjPath` | Discovered SQL project path |
| `ResolvedConfigPath` | Discovered config path |
| `ResolvedRenamingPath` | Discovered renaming path |
| `ResolvedTemplateDir` | Discovered template directory |
| `ResolvedConnectionString` | Resolved connection string |
| `UseConnectionString` | Whether connection string mode is active |

### EnsureDacpacBuilt

Builds a `.sqlproj` to DACPAC if it's out of date.

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `SqlProjPath` | Yes | Path to `.sqlproj` |
| `Configuration` | Yes | Build configuration (e.g., `Debug` / `Release`) |
| `MsBuildExe` | No | Path to `msbuild.exe` |
| `DotNetExe` | No | Path to dotnet host |
| `LogVerbosity` | No | Logging level |

**Outputs:**

| Output | Description |
|--------|-------------|
| `DacpacPath` | Path to built DACPAC file |

### QuerySchemaMetadata

Queries database schema metadata and computes a fingerprint (connection string mode).

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `ConnectionString` | Yes | Database connection string |
| `OutputDir` | Yes | Output directory (writes `schema-model.json`) |
| `Provider` | No | Provider identifier: `mssql`, `postgres`, `mysql`, `sqlite`, `oracle`, `firebird`, `snowflake` (default: `mssql`) |
| `LogVerbosity` | No | Logging level |

**Outputs:**

| Output | Description |
|--------|-------------|
| `SchemaFingerprint` | Computed schema fingerprint |

### StageEfcptInputs

Stages configuration files and templates into the intermediate directory.

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `OutputDir` | Yes | Base staging directory |
| `ProjectDirectory` | Yes | Consuming project directory |
| `ConfigPath` | Yes | Path to `efcpt-config.json` |
| `RenamingPath` | Yes | Path to `efcpt.renaming.json` |
| `TemplateDir` | Yes | Path to template directory |
| `TemplateOutputDir` | No | Subdirectory for templates (e.g., "Generated") |
| `LogVerbosity` | No | Logging level |

**Outputs:**

| Output | Description |
|--------|-------------|
| `StagedConfigPath` | Full path to staged config |
| `StagedRenamingPath` | Full path to staged renaming file |
| `StagedTemplateDir` | Full path to staged templates |

### ComputeFingerprint

Computes a composite fingerprint to detect when regeneration is needed.

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `DacpacPath` | No | Path to DACPAC file (DACPAC mode) |
| `SchemaFingerprint` | No | Schema fingerprint (connection string mode) |
| `UseConnectionStringMode` | No | Boolean indicating connection string mode |
| `ConfigPath` | Yes | Path to efcpt config |
| `RenamingPath` | Yes | Path to renaming file |
| `TemplateDir` | Yes | Path to templates |
| `FingerprintFile` | Yes | Path to fingerprint cache file |
| `ToolVersion` | No | EF Core Power Tools CLI version |
| `GeneratedDir` | No | Directory containing generated files |
| `DetectGeneratedFileChanges` | No | Whether to detect changes to generated files (default: false) |
| `ConfigPropertyOverrides` | No | JSON string of MSBuild property overrides |
| `LogVerbosity` | No | Logging level |

**Outputs:**

| Output | Description |
|--------|-------------|
| `Fingerprint` | Computed XxHash64 hash including library version, tool version, schema, config, overrides, templates, and optionally generated files |
| `HasChanged` | Whether fingerprint changed |

### RunEfcpt

Executes EF Core Power Tools CLI to generate EF Core models.

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `ToolMode` | No | How to find efcpt: `auto`, `tool-manifest`, or global |
| `ToolPackageId` | No | NuGet package ID |
| `ToolVersion` | No | Version constraint |
| `ToolRestore` | No | Whether to restore tool |
| `ToolCommand` | No | Command name |
| `ToolPath` | No | Explicit path to executable |
| `DotNetExe` | No | Path to dotnet host |
| `WorkingDirectory` | No | Working directory for efcpt |
| `DacpacPath` | No | Input DACPAC (DACPAC mode) |
| `ConnectionString` | No | Connection string (connection string mode) |
| `UseConnectionStringMode` | No | Boolean indicating mode |
| `Provider` | No | Provider identifier (default: `mssql`) |
| `ConfigPath` | Yes | efcpt configuration |
| `RenamingPath` | Yes | Renaming rules |
| `TemplateDir` | Yes | Template directory |
| `OutputDir` | Yes | Output directory |
| `LogVerbosity` | No | Logging level |

### RenameGeneratedFiles

Renames generated `.cs` files to `.g.cs`.

**Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `GeneratedDir` | Yes | Directory containing generated files |
| `LogVerbosity` | No | Logging level |

### ApplyConfigOverrides

Applies MSBuild property overrides to the staged `efcpt-config.json` file. This task enables configuration via MSBuild properties without editing JSON files directly.

**Control Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `StagedConfigPath` | Yes | Path to the staged efcpt-config.json file |
| `ApplyOverrides` | No | Whether to apply overrides to user-provided configs (default: `true`) |
| `IsUsingDefaultConfig` | No | Whether using library default config (default: `false`) |
| `LogVerbosity` | No | Logging level |

**Names Section Parameters:**

| Parameter | JSON Property | Description |
|-----------|---------------|-------------|
| `RootNamespace` | `root-namespace` | Root namespace for generated code |
| `DbContextName` | `dbcontext-name` | Name of the DbContext class |
| `DbContextNamespace` | `dbcontext-namespace` | Namespace for the DbContext class |
| `ModelNamespace` | `model-namespace` | Namespace for entity model classes |

**File Layout Section Parameters:**

| Parameter | JSON Property | Description |
|-----------|---------------|-------------|
| `OutputPath` | `output-path` | Output path for generated files |
| `DbContextOutputPath` | `output-dbcontext-path` | Output path for the DbContext file |
| `SplitDbContext` | `split-dbcontext-preview` | Enable split DbContext generation |
| `UseSchemaFolders` | `use-schema-folders-preview` | Use schema-based folders |
| `UseSchemaNamespaces` | `use-schema-namespaces-preview` | Use schema-based namespaces |

**Code Generation Section Parameters:**

| Parameter | JSON Property | Description |
|-----------|---------------|-------------|
| `EnableOnConfiguring` | `enable-on-configuring` | Add OnConfiguring method |
| `GenerationType` | `type` | Type of files to generate |
| `UseDatabaseNames` | `use-database-names` | Use database names |
| `UseDataAnnotations` | `use-data-annotations` | Use DataAnnotation attributes |
| `UseNullableReferenceTypes` | `use-nullable-reference-types` | Use nullable reference types |
| `UseInflector` | `use-inflector` | Pluralize/singularize names |
| `UseLegacyInflector` | `use-legacy-inflector` | Use EF6 Pluralizer |
| `UseManyToManyEntity` | `use-many-to-many-entity` | Preserve many-to-many entity |
| `UseT4` | `use-t4` | Use T4 templates |
| `UseT4Split` | `use-t4-split` | Use T4 with EntityTypeConfiguration |
| `RemoveDefaultSqlFromBool` | `remove-defaultsql-from-bool-properties` | Remove SQL default from bool |
| `SoftDeleteObsoleteFiles` | `soft-delete-obsolete-files` | Cleanup obsolete files |
| `DiscoverMultipleResultSets` | `discover-multiple-stored-procedure-resultsets-preview` | Discover multiple result sets |
| `UseAlternateResultSetDiscovery` | `use-alternate-stored-procedure-resultset-discovery` | Use alternate discovery |
| `T4TemplatePath` | `t4-template-path` | Path to T4 templates |
| `UseNoNavigations` | `use-no-navigations-preview` | Remove navigation properties |
| `MergeDacpacs` | `merge-dacpacs` | Merge .dacpac files |
| `RefreshObjectLists` | `refresh-object-lists` | Refresh object lists |
| `GenerateMermaidDiagram` | `generate-mermaid-diagram` | Generate Mermaid diagram |
| `UseDecimalAnnotationForSprocs` | `use-decimal-data-annotation-for-sproc-results` | Use decimal annotation |
| `UsePrefixNavigationNaming` | `use-prefix-navigation-naming` | Use prefix navigation naming |
| `UseDatabaseNamesForRoutines` | `use-database-names-for-routines` | Use database names for routines |
| `UseInternalAccessForRoutines` | `use-internal-access-modifiers-for-sprocs-and-functions` | Use internal access modifiers |

**Type Mappings Section Parameters:**

| Parameter | JSON Property | Description |
|-----------|---------------|-------------|
| `UseDateOnlyTimeOnly` | `use-DateOnly-TimeOnly` | Map to DateOnly/TimeOnly |
| `UseHierarchyId` | `use-HierarchyId` | Map hierarchyId type |
| `UseSpatial` | `use-spatial` | Map spatial columns |
| `UseNodaTime` | `use-NodaTime` | Use NodaTime types |

**Replacements Section Parameters:**

| Parameter | JSON Property | Description |
|-----------|---------------|-------------|
| `PreserveCasingWithRegex` | `preserve-casing-with-regex` | Preserve casing with regex |

**Override Behavior:**

- When `IsUsingDefaultConfig` is `true`, overrides are always applied regardless of `ApplyOverrides`
- When using a user-provided config, overrides are only applied if `ApplyOverrides` is `true`
- Empty or whitespace-only parameter values are treated as "no override"

## MSBuild Properties Reference

### Core Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptEnabled` | `true` | Master switch for the entire pipeline |
| `EfcptSqlProj` | *(auto-discovered)* | Path to `.sqlproj` file |
| `EfcptDacpac` | *(empty)* | Path to pre-built `.dacpac` file (skips .sqlproj build) |
| `EfcptConfig` | `efcpt-config.json` | EF Core Power Tools configuration |
| `EfcptRenaming` | `efcpt.renaming.json` | Renaming rules file |
| `EfcptTemplateDir` | `Template` | T4 template directory |
| `EfcptOutput` | `$(BaseIntermediateOutputPath)efcpt\` | Intermediate staging directory |
| `EfcptGeneratedDir` | `$(EfcptOutput)Generated\` | Generated code output directory |

### Connection String Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptConnectionString` | *(empty)* | Explicit connection string (enables connection string mode) |
| `EfcptAppSettings` | *(empty)* | Path to `appsettings.json` |
| `EfcptAppConfig` | *(empty)* | Path to `app.config`/`web.config` |
| `EfcptConnectionStringName` | `DefaultConnection` | Connection string key name |
| `EfcptProvider` | `mssql` | Database provider |

### Tool Configuration Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptToolMode` | `auto` | Tool resolution mode |
| `EfcptToolPackageId` | `ErikEJ.EFCorePowerTools.Cli` | NuGet package ID |
| `EfcptToolVersion` | `10.*` | Version constraint |
| `EfcptToolCommand` | `efcpt` | Command name |
| `EfcptToolPath` | *(empty)* | Explicit path to executable |
| `EfcptDotNetExe` | `dotnet` | Path to dotnet host |
| `EfcptToolRestore` | `true` | Whether to restore/update tool |

### Discovery Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptSolutionDir` | `$(SolutionDir)` | Solution root for discovery |
| `EfcptSolutionPath` | `$(SolutionPath)` | Solution file path |
| `EfcptProbeSolutionDir` | `true` | Whether to probe solution directory |

### Downstream Project Triggering Properties

These properties control automatic discovery and building of downstream EF Core projects when JD.Efcpt.Build is used in a SQL project.

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptTriggerDownstream` | `true` | Enable/disable automatic downstream discovery and building. Only active when in a SQL project. |
| `EfcptDownstreamProjects` | *(empty)* | Explicit semicolon-separated list of downstream project paths. When set, overrides automatic discovery. Paths can be relative to the SQL project directory or absolute. |
| `EfcptDownstreamAutoDiscover` | `true` | Enable/disable automatic discovery of downstream projects. When `false`, only projects specified in `EfcptDownstreamProjects` are built. |
| `EfcptDownstreamSearchPaths` | *(empty)* | Additional semicolon-separated directories to search for downstream projects. Paths can be relative to the SQL project directory or absolute. |

**Example Usage:**

```xml
<!-- In SQL Project: Disable automatic triggering -->
<PropertyGroup>
    <EfcptTriggerDownstream>false</EfcptTriggerDownstream>
</PropertyGroup>

<!-- In SQL Project: Explicit downstream projects only -->
<PropertyGroup>
    <EfcptDownstreamProjects>
        ..\DataAccessProject\DataAccessProject.csproj;
        ..\TestProject\TestProject.csproj
    </EfcptDownstreamProjects>
</PropertyGroup>

<!-- In SQL Project: Disable auto-discovery but use explicit list -->
<PropertyGroup>
    <EfcptDownstreamAutoDiscover>false</EfcptDownstreamAutoDiscover>
    <EfcptDownstreamProjects>..\DataAccessProject\DataAccessProject.csproj</EfcptDownstreamProjects>
</PropertyGroup>

<!-- In SQL Project: Add custom search paths -->
<PropertyGroup>
    <EfcptDownstreamSearchPaths>..\src\DataAccess;..\tests</EfcptDownstreamSearchPaths>
</PropertyGroup>
```

### Advanced Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptLogVerbosity` | `minimal` | Logging level: `minimal` or `detailed` |
| `EfcptDumpResolvedInputs` | `false` | Write resolved inputs to JSON |
| `EfcptFingerprintFile` | `$(EfcptOutput)fingerprint.txt` | Fingerprint cache location |
| `EfcptStampFile` | `$(EfcptOutput).efcpt.stamp` | Generation stamp file |
| `EfcptDetectGeneratedFileChanges` | `false` | Detect changes to generated `.g.cs` files and trigger regeneration. **Warning**: When enabled, manual edits to generated files will be overwritten. |
| `EfcptAutoDetectWarningLevel` | `Info` | Severity for SQL project/connection string auto-detection messages. Valid values: `None`, `Info`, `Warn`, `Error` |
| `EfcptSdkVersionWarningLevel` | `Warn` | Severity for SDK version update notifications. Valid values: `None`, `Info`, `Warn`, `Error` |

### Config Override Properties

These properties override values in `efcpt-config.json` without editing the JSON file directly.

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptApplyMsBuildOverrides` | `true` | Whether to apply MSBuild property overrides |

#### Names Section

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigRootNamespace` | `root-namespace` | Root namespace for generated code (defaults to `$(RootNamespace)` if not specified) |
| `EfcptConfigDbContextName` | `dbcontext-name` | Name of the DbContext class |
| `EfcptConfigDbContextNamespace` | `dbcontext-namespace` | Namespace for the DbContext class |
| `EfcptConfigModelNamespace` | `model-namespace` | Namespace for entity model classes |

#### File Layout Section

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigOutputPath` | `output-path` | Output path for generated files |
| `EfcptConfigDbContextOutputPath` | `output-dbcontext-path` | Output path for DbContext |
| `EfcptConfigSplitDbContext` | `split-dbcontext-preview` | Split DbContext generation |
| `EfcptConfigUseSchemaFolders` | `use-schema-folders-preview` | Use schema-based folders |
| `EfcptConfigUseSchemaNamespaces` | `use-schema-namespaces-preview` | Use schema-based namespaces |

#### Code Generation Section

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigEnableOnConfiguring` | `enable-on-configuring` | Add OnConfiguring method |
| `EfcptConfigGenerationType` | `type` | Type of files to generate |
| `EfcptConfigUseDatabaseNames` | `use-database-names` | Use database names |
| `EfcptConfigUseDataAnnotations` | `use-data-annotations` | Use DataAnnotation attributes |
| `EfcptConfigUseNullableReferenceTypes` | `use-nullable-reference-types` | Use nullable reference types |
| `EfcptConfigUseInflector` | `use-inflector` | Pluralize/singularize names |
| `EfcptConfigUseLegacyInflector` | `use-legacy-inflector` | Use EF6 Pluralizer |
| `EfcptConfigUseManyToManyEntity` | `use-many-to-many-entity` | Preserve many-to-many entity |
| `EfcptConfigUseT4` | `use-t4` | Use T4 templates |
| `EfcptConfigUseT4Split` | `use-t4-split` | Use T4 with EntityTypeConfiguration |
| `EfcptConfigRemoveDefaultSqlFromBool` | `remove-defaultsql-from-bool-properties` | Remove SQL default from bool |
| `EfcptConfigSoftDeleteObsoleteFiles` | `soft-delete-obsolete-files` | Cleanup obsolete files |
| `EfcptConfigDiscoverMultipleResultSets` | `discover-multiple-stored-procedure-resultsets-preview` | Discover multiple result sets |
| `EfcptConfigUseAlternateResultSetDiscovery` | `use-alternate-stored-procedure-resultset-discovery` | Use alternate discovery |
| `EfcptConfigT4TemplatePath` | `t4-template-path` | Path to T4 templates |
| `EfcptConfigUseNoNavigations` | `use-no-navigations-preview` | Remove navigation properties |
| `EfcptConfigMergeDacpacs` | `merge-dacpacs` | Merge .dacpac files |
| `EfcptConfigRefreshObjectLists` | `refresh-object-lists` | Refresh object lists |
| `EfcptConfigGenerateMermaidDiagram` | `generate-mermaid-diagram` | Generate Mermaid diagram |
| `EfcptConfigUseDecimalAnnotationForSprocs` | `use-decimal-data-annotation-for-sproc-results` | Use decimal annotation |
| `EfcptConfigUsePrefixNavigationNaming` | `use-prefix-navigation-naming` | Use prefix navigation naming |
| `EfcptConfigUseDatabaseNamesForRoutines` | `use-database-names-for-routines` | Use database names for routines |
| `EfcptConfigUseInternalAccessForRoutines` | `use-internal-access-modifiers-for-sprocs-and-functions` | Use internal access modifiers |

#### Type Mappings Section

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigUseDateOnlyTimeOnly` | `use-DateOnly-TimeOnly` | Map to DateOnly/TimeOnly |
| `EfcptConfigUseHierarchyId` | `use-HierarchyId` | Map hierarchyId type |
| `EfcptConfigUseSpatial` | `use-spatial` | Map spatial columns |
| `EfcptConfigUseNodaTime` | `use-NodaTime` | Use NodaTime types |

#### Replacements Section

| Property | JSON Property | Description |
|----------|---------------|-------------|
| `EfcptConfigPreserveCasingWithRegex` | `preserve-casing-with-regex` | Preserve casing with regex |

## Configuration File Schemas

### efcpt-config.json

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "properties": {
    "$schema": {
      "type": "string"
    },
    "code-generation": {
      "$ref": "#/definitions/CodeGeneration"
    },
    "tables": {
      "type": "array",
      "title": "List of tables discovered in the source database",
      "items": {
        "$ref": "#/definitions/Table"
      }
    },
    "views": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/View"
      }
    },
    "stored-procedures": {
      "type": "array",
      "title": "List of stored procedures discovered in the source database",
      "items": {
        "$ref": "#/definitions/StoredProcedure"
      }
    },
    "functions": {
      "type": "array",
      "title": "List of scalar and TVF functions discovered in the source database",
      "items": {
        "$ref": "#/definitions/Function"
      }
    },
    "names": {
      "title": "Custom class and namespace names",
      "$ref": "#/definitions/Names"
    },
    "file-layout": {
      "title": "Custom file layout options",
      "$ref": "#/definitions/FileLayout"
    },
    "replacements": {
      "title": "Custom naming options",
      "$ref": "#/definitions/Replacements"
    },
    "type-mappings": {
      "title": "Optional type mappings",
      "$ref": "#/definitions/TypeMappings"
    }
  },
  "definitions": {
    "Table": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "title": "Full table name"
        },
        "exclude": {
          "type": "boolean",
          "title": "Set to true to exclude this table from code generation"
        },
        "exclusionWildcard": {
          "type": "string",
          "title": "Exclusion pattern with * symbol, use '*' to exclude all by default"
        },
        "excludedColumns": {
          "type": "array",
          "default": [],
          "title": "Columns to Exclude from code generation",
          "items": {
            "type": "string",
            "title": "Column"
          }
        },
        "excludedIndexes": {
          "type": "array",
          "default": [],
          "title": "Indexes to Exclude from code generation",
          "items": {
            "type": "string",
            "title": "Index"
          }
        }
      }
    },
    "View": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string"
        },
        "exclusionWildcard": {
          "type": "string",
          "title": "Exclusion pattern with * symbol, use '*' to exclude all by default"
        },
        "excludedColumns": {
          "type": "array",
          "default": [],
          "title": "Columns to Exclude from code generation",
          "items": {
            "type": "string",
            "title": "Column"
          }
        }
      }
    },
    "StoredProcedure": {
      "type": "object",
      "title": "Stored procedure",
      "properties": {
        "name": {
          "type": "string",
          "title": "The stored procedure name"
        },
        "exclude": {
          "type": "boolean",
          "default": false,
          "title": "Set to true to exclude this stored procedure from code generation",
          "examples": [
            true
          ]
        },
        "use-legacy-resultset-discovery": {
          "type": "boolean",
          "default": false,
          "title": "Use sp_describe_first_result_set instead of SET FMTONLY for result set discovery"
        },
        "mapped-type": {
          "type": "string",
          "default": null,
          "title": "Name of an entity class (DbSet) in your DbContext that maps the result of the stored procedure "
        },
        "exclusionWildcard": {
          "type": "string",
          "title": "Exclusion pattern with * symbol, use '*' to exclude all by default"
        }
      }
    },
    "Function": {
      "type": "object",
      "title": "Function",
      "properties": {
        "name": {
          "type": "string",
          "title": "Name of function"
        },
        "exclude": {
          "type": "boolean",
          "default": false,
          "title": "Set to true to exclude this function from code generation"
        },
        "exclusionWildcard": {
          "type": "string",
          "title": "Exclusion pattern with * symbol, use '*' to exclude all by default"
        }
      }
    },
    "CodeGeneration": {
      "type": "object",
      "title": "Options for code generation",
      "required": [
        "enable-on-configuring",
        "type",
        "use-database-names",
        "use-data-annotations",
        "use-nullable-reference-types",
        "use-inflector",
        "use-legacy-inflector",
        "use-many-to-many-entity",
        "use-t4",
        "remove-defaultsql-from-bool-properties",
        "soft-delete-obsolete-files",
        "use-alternate-stored-procedure-resultset-discovery"
      ],
      "properties": {
        "enable-on-configuring": {
          "type": "boolean",
          "title": "Add OnConfiguring method to the DbContext"
        },
        "type": {
          "default": "all",
          "enum": [ "all", "dbcontext", "entities" ],
          "type": "string",
          "title": "Type of files to generate"
        },
        "use-database-names": {
          "type": "boolean",
          "title": "Use table and column names from the database"
        },
        "use-data-annotations": {
          "type": "boolean",
          "title": "Use DataAnnotation attributes rather than the fluent API (as much as possible)"
        },
        "use-nullable-reference-types": {
          "type": "boolean",
          "title": "Use nullable reference types"
        },
        "use-inflector": {
          "type": "boolean",
          "default": true,
          "title": "Pluralize or singularize generated names (entity class names singular and DbSet names plural)"
        },
        "use-legacy-inflector": {
          "type": "boolean",
          "title": "Use EF6 Pluralizer instead of Humanizer"
        },
        "use-many-to-many-entity": {
          "type": "boolean",
          "title": "Preserve a many to many entity instead of skipping it "
        },
        "use-t4": {
          "type": "boolean",
          "title": "Customize code using T4 templates"
        },
        "use-t4-split": {
          "type": "boolean",
          "default": false,
          "title": "Customize code using T4 templates including EntityTypeConfiguration.t4.  This cannot be used in combination with use-t4 or split-dbcontext-preview"
        },
        "remove-defaultsql-from-bool-properties": {
          "type": "boolean",
          "title": "Remove SQL default from bool columns to avoid them being bool?"
        },
        "soft-delete-obsolete-files": {
          "type": "boolean",
          "default": true,
          "title": "Run Cleanup of obsolete files"
        },
        "discover-multiple-stored-procedure-resultsets-preview": {
          "type": "boolean",
          "title": "Discover multiple result sets from SQL stored procedures (preview)"
        },
        "use-alternate-stored-procedure-resultset-discovery": {
          "type": "boolean",
          "title": "Use alternate result set discovery - use sp_describe_first_result_set to retrieve stored procedure result sets"
        },
        "t4-template-path": {
          "type": [ "string", "null" ],
          "title": "Global path to T4 templates"
        },
        "use-no-navigations-preview": {
          "type": "boolean",
          "title": "Remove all navigation properties from the generated code (preview)"
        },
        "merge-dacpacs": {
          "type": "boolean",
          "title": "Merge .dacpac files (when using .dacpac references)"
        },
        "refresh-object-lists": {
          "type": "boolean",
          "default": true,
          "title": "Refresh the lists of objects (tables, views, stored procedures, functions) from the database in the config file during scaffolding"
        },
        "generate-mermaid-diagram": {
          "type": "boolean",
          "title": "Create a markdown file with a Mermaid ER diagram during scaffolding"
        },
        "use-decimal-data-annotation-for-sproc-results": {
          "type": "boolean",
          "title": "Use explicit decimal annotation for store procedure results",
          "default": true
        },
        "use-prefix-navigation-naming": {
          "type": "boolean",
          "title": "Use prefix based naming of navigations with EF Core 8 or later"
        },
        "use-database-names-for-routines": {
          "type": "boolean",
          "title": "Use stored procedure, stored procedure result and function names from the database",
          "default": true
        },
        "use-internal-access-modifiers-for-sprocs-and-functions": {
          "type": "boolean",
          "title": "When generating the stored procedure and function classes and helpers, set them to internal instead of public.",
          "default": false
        }
      }
    },
    "Names": {
      "type": "object",
      "title": "Custom class and namespace names",
      "required": [
        "dbcontext-name",
        "root-namespace"
      ],
      "properties": {
        "root-namespace": {
          "type": "string",
          "title": "Root namespace"
        },
        "dbcontext-name": {
          "type": "string",
          "title": "Name of DbContext class"
        },
        "dbcontext-namespace": {
          "type": [ "string", "null" ],
          "title": "Namespace of DbContext class"
        },
        "model-namespace": {
          "type": [ "string", "null" ],
          "title": "Namespace of entities"
        }
      }
    },
    "FileLayout": {
      "type": "object",
      "title": "Custom file layout options",
      "required": [
        "output-path"
      ],
      "properties": {
        "output-path": {
          "type": "string",
          "default": "Models",
          "title": "Output path"
        },
        "output-dbcontext-path": {
          "type": [ "string", "null" ],
          "title": "DbContext output path"
        },
        "split-dbcontext-preview": {
          "type": "boolean",
          "title": "Split DbContext (preview)"
        },
        "use-schema-folders-preview": {
          "type": "boolean",
          "title": "Use schema folders (preview)"
        },
        "use-schema-namespaces-preview": {
          "type": "boolean",
          "title": "Use schema namespaces (preview)"
        }
      }
    },
    "TypeMappings": {
      "type": "object",
      "title": "Optional type mappings",
      "properties": {
        "use-DateOnly-TimeOnly": {
          "type": "boolean",
          "title": "Map date and time to DateOnly/TimeOnly (mssql)"
        },
        "use-HierarchyId": {
          "type": "boolean",
          "title": "Map hierarchyId (mssql)"
        },
        "use-spatial": {
          "type": "boolean",
          "title": "Map spatial columns"
        },
        "use-NodaTime": {
          "type": "boolean",
          "title": "Use NodaTime"
        }
      }
    },
    "Replacements": {
      "type": "object",
      "title": "Custom naming options",
      "properties": {
        "preserve-casing-with-regex": {
          "type": "boolean",
          "title": "Preserve casing with regex when custom naming"
        },
        "irregular-words": {
          "type": "array",
          "title": "Irregular words (words which cannot easily be pluralized/singularized) for Humanizer's AddIrregular() method.",
          "items": {
            "$ref": "#/definitions/IrregularWord"
          }
        },
        "uncountable-words": {
          "type": "array",
          "title": "Uncountable (ignored) words for Humanizer's AddUncountable() method.",
          "items": {
            "$ref": "#/definitions/UncountableWord"
          }
        },
        "plural-rules": {
          "type": "array",
          "title": "Plural word rules for Humanizer's AddPlural() method.",
          "items": {
            "$ref": "#/definitions/RuleReplacement"
          }
        },
        "singular-rules": {
          "type": "array",
          "title": "Singular word rules for Humanizer's AddSingular() method.",
          "items": {
            "$ref": "#/definitions/RuleReplacement"
          }
        }
      }
    },
    "IrregularWord": {
      "type": "object",
      "title": "Irregular word rule",
      "properties": {
        "singular": {
          "type": "string",
          "title": "Singular form"
        },
        "plural": {
          "type": "string",
          "title": "Plural form"
        },
        "match-case": {
          "type": "boolean",
          "title": "Match these words on their own as well as at the end of longer words. True by default."
        }
      }
    },
    "UncountableWord": {
      "type": "string",
      "title": "Word list"
    },
    "RuleReplacement": {
      "type": "object",
      "title": "Humanizer RegEx-based rule and replacement",
      "properties": {
        "rule": {
          "type": "string",
          "title": "RegEx to be matched, case insensitive"
        },
        "replacement": {
          "type": "string",
          "title": "RegEx replacement"
        }
      }
    }
  }
}
```

### efcpt.renaming.json

```json
[
  {
    "SchemaName": "string",
    "Tables": [
      {
        "Name": "string",
        "NewName": "string",
        "Columns": [
          {
            "Name": "string",
            "NewName": "string"
          }
        ]
      }
    ],
    "UseSchemaName": "boolean"
  }
]
```

## Output Files

### Generated Files

| File | Location | Description |
|------|----------|-------------|
| `*.g.cs` | `$(EfcptGeneratedDir)` | Generated DbContext and entity classes |
| `fingerprint.txt` | `$(EfcptOutput)` | Cached fingerprint for incremental builds |
| `.efcpt.stamp` | `$(EfcptOutput)` | Generation timestamp |

### Diagnostic Files

| File | Location | Condition | Description |
|------|----------|-----------|-------------|
| `resolved-inputs.json` | `$(EfcptOutput)` | `EfcptDumpResolvedInputs=true` | Resolved input paths |
| `schema-model.json` | `$(EfcptOutput)` | Connection string mode | Database schema model |

## Pipeline Execution Order

```
1. EfcptResolveInputs
   └── Discovers .sqlproj, config, renaming, templates, connection string

2a. EfcptEnsureDacpac (DACPAC mode)
    └── Builds .sqlproj to DACPAC

2b. EfcptQuerySchemaMetadata (connection string mode)
    └── Queries database schema

3. EfcptStageInputs
   └── Copies config, renaming, templates to obj/efcpt/

4. EfcptApplyConfigOverrides
   └── Applies MSBuild property overrides to staged config
   └── Uses typed model for all 37 config properties

5. EfcptComputeFingerprint
   └── Computes XxHash64 of all inputs (including overrides)
   └── Compares with cached fingerprint

6. EfcptGenerateModels (only if fingerprint changed)
   └── Executes efcpt CLI
   └── Renames files to .g.cs
   └── Updates fingerprint cache

7. EfcptAddToCompile
   └── Adds *.g.cs to Compile item group
```

## Extensibility Points

### Custom Pre-Generation Logic

Run before model generation:

```xml
<Target Name="MyPreGeneration" BeforeTargets="EfcptGenerateModels">
  <Message Text="About to generate models..." Importance="high" />
</Target>
```

### Custom Post-Generation Logic

Run after model generation:

```xml
<Target Name="MyPostGeneration" AfterTargets="EfcptGenerateModels">
  <Message Text="Models generated!" Importance="high" />
</Target>
```

### Conditional Execution

Skip generation based on custom conditions:

```xml
<PropertyGroup Condition="'$(SkipEfcpt)' == 'true'">
  <EfcptEnabled>false</EfcptEnabled>
</PropertyGroup>
```

## Next Steps

- [Configuration](configuration.md) - Detailed configuration guide
- [Core Concepts](core-concepts.md) - Understanding the pipeline
- [Troubleshooting](troubleshooting.md) - Solving common problems
