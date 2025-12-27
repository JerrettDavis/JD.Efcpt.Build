# Schema-Based Organization

This sample demonstrates organizing generated entities by database schema using the `use-schema-folders-preview` and `use-schema-namespaces-preview` configuration options.

## When to Use

Schema-based organization is useful when:

- Your database has **multiple schemas** (e.g., `dbo`, `sales`, `inventory`, `audit`)
- You want to **group related entities** in the file system
- You want **schema-based namespaces** to match your database structure
- You're working with a **large database** where flat organization becomes unwieldy

## Database Structure

This sample uses three schemas:

```
Database
├── dbo
│   └── Customer
├── sales
│   ├── Order
│   └── OrderItem
└── inventory
    ├── Product
    └── Warehouse
```

## Configuration

### efcpt-config.json

```json
{
  "file-layout": {
    "output-path": "Models",
    "use-schema-folders-preview": true,
    "use-schema-namespaces-preview": true
  }
}
```

### Configuration Options

| Option | Effect |
|--------|--------|
| `use-schema-folders-preview` | Creates subdirectories per schema: `Models/dbo/`, `Models/sales/`, etc. |
| `use-schema-namespaces-preview` | Adds schema to namespace: `EntityFrameworkCoreProject.Models.Sales` |

## Generated Output

### File Structure

```
EntityFrameworkCoreProject/
└── obj/efcpt/Generated/
    └── Models/
        ├── dbo/
        │   └── Customer.g.cs
        ├── sales/
        │   ├── Order.g.cs
        │   └── OrderItem.g.cs
        └── inventory/
            ├── Product.g.cs
            └── Warehouse.g.cs
```

### Generated Namespaces

With `use-schema-namespaces-preview: true`:

```csharp
// Models/dbo/Customer.g.cs
namespace EntityFrameworkCoreProject.Models.Dbo;

public partial class Customer { ... }
```

```csharp
// Models/sales/Order.g.cs
namespace EntityFrameworkCoreProject.Models.Sales;

public partial class Order { ... }
```

```csharp
// Models/inventory/Product.g.cs
namespace EntityFrameworkCoreProject.Models.Inventory;

public partial class Product { ... }
```

## Build

```bash
dotnet build
```

## Using the Generated Entities

```csharp
using EntityFrameworkCoreProject.Models.Dbo;
using EntityFrameworkCoreProject.Models.Sales;
using EntityFrameworkCoreProject.Models.Inventory;

// Entities from different schemas are in different namespaces
var customer = new Customer { Name = "Acme Corp" };
var order = new Order { CustomerId = customer.Id };
var product = new Product { Name = "Widget", Sku = "WDG-001" };
```

## Comparison

### Without Schema Organization (default)

```
Models/
├── Customer.g.cs      # namespace: EntityFrameworkCoreProject.Models
├── Order.g.cs         # namespace: EntityFrameworkCoreProject.Models
├── OrderItem.g.cs     # namespace: EntityFrameworkCoreProject.Models
├── Product.g.cs       # namespace: EntityFrameworkCoreProject.Models
└── Warehouse.g.cs     # namespace: EntityFrameworkCoreProject.Models
```

### With Schema Organization

```
Models/
├── dbo/
│   └── Customer.g.cs       # namespace: EntityFrameworkCoreProject.Models.Dbo
├── sales/
│   ├── Order.g.cs          # namespace: EntityFrameworkCoreProject.Models.Sales
│   └── OrderItem.g.cs      # namespace: EntityFrameworkCoreProject.Models.Sales
└── inventory/
    ├── Product.g.cs        # namespace: EntityFrameworkCoreProject.Models.Inventory
    └── Warehouse.g.cs      # namespace: EntityFrameworkCoreProject.Models.Inventory
```

## Tips

1. **Use with renaming** - Combine with `efcpt.renaming.json` to set `UseSchemaName: false` for the `dbo` schema if you don't want "Dbo" in namespaces
2. **Large databases** - This is especially useful for databases with 50+ tables across multiple schemas
3. **Team organization** - Schema folders can map to team ownership boundaries
