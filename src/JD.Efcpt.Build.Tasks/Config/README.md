# Config Generator

This directory contains the `EfcptConfigGenerator` utility that generates default `efcpt-config.json` files from the EFCorePowerTools JSON schema.

## Purpose

The generator ensures that the default config files packaged with JD.Efcpt.Build match the structure and defaults produced by the efcpt CLI tool. This is important for:

1. **Consistency**: Users get the same config structure whether they use our templates or run efcpt directly
2. **Maintainability**: When the schema changes, we can regenerate configs rather than manually updating them
3. **Correctness**: Automatically excludes preview properties and uses schema-defined defaults

## Usage

### Generating Config Files

The generator can be used programmatically:

```csharp
using JD.Efcpt.Build.Tasks.Config;

// From local schema file
var config = EfcptConfigGenerator.GenerateFromFile(
    schemaPath: "path/to/efcpt-config.schema.json",
    dbContextName: "ApplicationDbContext",
    rootNamespace: "EfcptProject");

// From URL
var config = await EfcptConfigGenerator.GenerateFromUrlAsync(
    schemaUrl: "https://raw.githubusercontent.com/.../efcpt-config.schema.json",
    dbContextName: "ApplicationDbContext",
    rootNamespace: "EfcptProject");

// Write to file
File.WriteAllText("efcpt-config.json", config);
```

### Updating Package Config Files

When the schema is updated, regenerate the packaged config files:

1. Update `/lib/efcpt-config.schema.json` if needed
2. Run the generator to update both config files:
   - `/src/JD.Efcpt.Build/defaults/efcpt-config.json`
   - `/src/JD.Efcpt.Build.Templates/templates/efcptbuild/efcpt-config.json`

Example script:

```csharp
var schemaPath = "lib/efcpt-config.schema.json";
var config = EfcptConfigGenerator.GenerateFromFile(
    schemaPath,
    dbContextName: "ApplicationDbContext",
    rootNamespace: "EfcptProject");

File.WriteAllText("src/JD.Efcpt.Build/defaults/efcpt-config.json", config);
File.WriteAllText("src/JD.Efcpt.Build.Templates/templates/efcptbuild/efcpt-config.json", config);
```

## Generator Behavior

- **Includes all properties** with defined defaults (not just required ones)
- **Excludes preview properties** (any property containing "-preview")
- **Uses schema defaults** where specified
- **Provides sensible defaults** for required properties without schema defaults:
  - `dbcontext-name`: "ApplicationDbContext"
  - `root-namespace`: "EfcptProject"
  - `output-path`: "Models"
- **Sets nullable properties** to `null` by default

## When to Use This

This generator is **only needed at pack-time** for our own libraries. End users don't need it because:

1. The efcpt CLI automatically generates a default config if one is missing
2. Our packages include pre-generated configs that match what efcpt produces
3. Users can customize configs via MSBuild properties without regenerating files

## Testing

Tests are located in `/tests/JD.Efcpt.Build.Tests/Config/EfcptConfigGeneratorTests.cs` and verify:

- Valid JSON output
- Correct structure (code-generation, names, file-layout, type-mappings sections)
- Exclusion of preview properties
- Custom name support
- Schema default values
