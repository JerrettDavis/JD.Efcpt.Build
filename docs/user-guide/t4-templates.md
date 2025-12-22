# T4 Templates

JD.Efcpt.Build supports T4 (Text Template Transformation Toolkit) templates for customizing code generation. This guide explains how to use and customize templates.

## Overview

T4 templates let you control exactly how your DbContext and entity classes are generated. You can:

- Change the coding style and formatting
- Add custom attributes or annotations
- Include additional methods or properties
- Generate partial classes with custom logic
- Apply your organization's coding standards

## Enabling T4 Templates

### Step 1: Enable in Configuration

Add to your `efcpt-config.json`:

```json
{
  "code-generation": {
    "use-t4": true,
    "t4-template-path": "."
  }
}
```

The `t4-template-path` is relative to the configuration file location.

### Step 2: Create Template Directory

Create the template folder structure in your project:

```
YourProject/
├── YourProject.csproj
├── efcpt-config.json
└── Template/
    └── CodeTemplates/
        └── EFCore/
            ├── DbContext.t4
            └── EntityType.t4
```

Or use a simpler structure:

```
YourProject/
├── YourProject.csproj
├── efcpt-config.json
└── CodeTemplates/
    └── EFCore/
        ├── DbContext.t4
        └── EntityType.t4
```

### Step 3: Add Template Files

Copy the default templates from EF Core Power Tools or create your own. The minimum required templates are:

- `DbContext.t4` - Generates the DbContext class
- `EntityType.t4` - Generates entity classes

## Template Structure

The `StageEfcptInputs` task understands several common layouts:

### Layout 1: Template/CodeTemplates/EFCore

```
Template/
└── CodeTemplates/
    └── EFCore/
        ├── DbContext.t4
        └── EntityType.t4
```

The task copies `CodeTemplates` to the staging directory.

### Layout 2: CodeTemplates/EFCore

```
CodeTemplates/
└── EFCore/
    ├── DbContext.t4
    └── EntityType.t4
```

The entire `CodeTemplates` tree is copied.

### Layout 3: Custom folder without CodeTemplates

```
MyTemplates/
├── DbContext.t4
└── EntityType.t4
```

The folder is staged as `CodeTemplates`.

## Template Staging

During build, templates are staged to:

```
obj/efcpt/Generated/CodeTemplates/EFCore/
├── DbContext.t4
└── EntityType.t4
```

This ensures:
- Consistent paths for efcpt CLI
- Clean separation from source templates
- Correct fingerprinting for incremental builds

## Customizing Templates

### DbContext Template

The `DbContext.t4` template generates your DbContext class. Key customization points:

**Adding custom using statements:**
```t4
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyApp.Common;  // Add your custom using
```

**Adding custom methods:**
```t4
<#
foreach (var entityType in Model.GetEntityTypes())
{
#>
        public DbSet<<#= entityType.Name #>> <#= entityType.GetDbSetName() #> => Set<<#= entityType.Name #>>();
<#
}
#>

        // Custom method
        public async Task<int> SaveChangesWithAuditAsync(CancellationToken cancellationToken = default)
        {
            // Add audit logic
            return await SaveChangesAsync(cancellationToken);
        }
```

### EntityType Template

The `EntityType.t4` template generates entity classes. Common customizations:

**Adding custom attributes:**
```t4
<#
var displayName = property.GetDisplayName();
if (!string.IsNullOrEmpty(displayName))
{
#>
        [Display(Name = "<#= displayName #>")]
<#
}
#>
        public <#= code.Reference(property.ClrType) #> <#= property.Name #> { get; set; }
```

**Generating partial classes:**
```t4
namespace <#= code.Namespace(entityType.GetNamespace(), Model) #>
{
    public partial class <#= entityType.Name #>
    {
        // Generated properties
<#
foreach (var property in entityType.GetProperties())
{
#>
        public <#= code.Reference(property.ClrType) #> <#= property.Name #> { get; set; }
<#
}
#>
    }
}
```

## Template Configuration

### Setting Template Path

In `.csproj`:

```xml
<PropertyGroup>
  <EfcptTemplateDir>CustomTemplates</EfcptTemplateDir>
</PropertyGroup>
```

Or in `efcpt-config.json`:

```json
{
  "code-generation": {
    "use-t4": true,
    "t4-template-path": "CustomTemplates"
  }
}
```

### Resolution Order

Templates are resolved in this order:

1. `<EfcptTemplateDir>` property (if set)
2. `Template` directory in project directory
3. `Template` directory in solution directory
4. Package default templates

## Common Customizations

### Adding XML Documentation

```t4
        /// <summary>
        /// Gets or sets the <#= property.GetDisplayName() ?? property.Name #>.
        /// </summary>
<#
if (property.GetComment() != null)
{
#>
        /// <remarks>
        /// <#= property.GetComment() #>
        /// </remarks>
<#
}
#>
        public <#= code.Reference(property.ClrType) #> <#= property.Name #> { get; set; }
```

### Adding Interface Implementation

```t4
namespace <#= code.Namespace(entityType.GetNamespace(), Model) #>
{
    public partial class <#= entityType.Name #> : IEntity
    {
        // ... properties
    }
}
```

### Custom Naming Conventions

```t4
<#
// Convert to camelCase for private fields
var fieldName = "_" + char.ToLower(property.Name[0]) + property.Name.Substring(1);
#>
        private <#= code.Reference(property.ClrType) #> <#= fieldName #>;

        public <#= code.Reference(property.ClrType) #> <#= property.Name #>
        {
            get => <#= fieldName #>;
            set => <#= fieldName #> = value;
        }
```

### Adding Validation Attributes

```t4
<#
var maxLength = property.GetMaxLength();
if (maxLength.HasValue)
{
#>
        [MaxLength(<#= maxLength.Value #>)]
<#
}
if (!property.IsNullable)
{
#>
        [Required]
<#
}
#>
        public <#= code.Reference(property.ClrType) #> <#= property.Name #> { get; set; }
```

## Template Variables

Templates have access to the EF Core model through the `Model` variable:

| Variable/Method | Description |
|----------------|-------------|
| `Model` | The full EF Core model |
| `Model.GetEntityTypes()` | All entity types in the model |
| `entityType.GetProperties()` | Properties of an entity |
| `entityType.GetNavigations()` | Navigation properties |
| `property.ClrType` | The CLR type of a property |
| `property.IsNullable` | Whether the property is nullable |
| `property.GetMaxLength()` | Maximum length constraint |

## Troubleshooting

### Templates not being used

Verify:
1. `use-t4` is set to `true` in `efcpt-config.json`
2. Template files exist in the expected location
3. Template directory is correctly resolved (check with `EfcptDumpResolvedInputs`)

```xml
<PropertyGroup>
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
  <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
</PropertyGroup>
```

### Template errors

Template compilation errors appear in the build output. Common issues:

- Syntax errors in T4 directives
- Missing assembly references
- Incorrect namespace references

### Templates not updating

The fingerprint includes template files. If templates change, regeneration should occur automatically. If not:

```bash
# Force regeneration
rmdir /s /q obj\efcpt
dotnet build
```

## Best Practices

1. **Start with defaults** - Copy default templates and modify incrementally
2. **Version control templates** - Keep templates in source control alongside your project
3. **Test changes** - Build after each template change to catch errors early
4. **Use partial classes** - Generate partial classes to separate generated and custom code
5. **Document customizations** - Comment your template modifications for team awareness

## Next Steps

- [Configuration](configuration.md) - Complete configuration reference
- [Advanced Topics](advanced.md) - Multi-project and complex scenarios
- [API Reference](api-reference.md) - MSBuild task documentation
