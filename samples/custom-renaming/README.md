# Custom Renaming Rules

This sample demonstrates using `efcpt.renaming.json` to transform legacy database naming conventions (table prefixes, Hungarian notation, column prefixes) into clean, modern C# naming conventions.

## The Problem

Many legacy databases use naming conventions that don't translate well to C#:

| Database Name | Issue |
|--------------|-------|
| `tblCustomers` | "tbl" prefix |
| `cust_first_name` | Column prefix + underscores |
| `ord_cust_id` | Abbreviated prefixes |
| `item_qty` | Abbreviated names |

## The Solution

The `efcpt.renaming.json` file maps these legacy names to clean C# names:

| Database | C# Entity | C# Property |
|----------|-----------|-------------|
| `tblCustomers` | `Customer` | - |
| `cust_first_name` | - | `FirstName` |
| `tblOrders` | `Order` | - |
| `ord_cust_id` | - | `CustomerId` |
| `item_qty` | - | `Quantity` |

## File Structure

```
custom-renaming/
├── DatabaseProject/                 # SQL Project with legacy-named tables
│   └── dbo/Tables/
│       ├── tblCustomers.sql
│       ├── tblOrders.sql
│       └── tblOrderItems.sql
├── EntityFrameworkCoreProject/
│   ├── EntityFrameworkCoreProject.csproj
│   ├── efcpt-config.json
│   └── efcpt.renaming.json         # Renaming rules
└── CustomRenaming.sln
```

## efcpt.renaming.json Format

```json
[
  {
    "SchemaName": "dbo",
    "UseSchemaName": false,
    "Tables": [
      {
        "Name": "tblCustomers",
        "NewName": "Customer",
        "Columns": [
          { "Name": "cust_id", "NewName": "Id" },
          { "Name": "cust_first_name", "NewName": "FirstName" }
        ]
      }
    ]
  }
]
```

### Schema Entry Properties

| Property | Description |
|----------|-------------|
| `SchemaName` | Database schema (e.g., "dbo") |
| `UseSchemaName` | Include schema name in generated namespaces |
| `Tables` | Array of table renaming rules |

### Table Entry Properties

| Property | Description |
|----------|-------------|
| `Name` | Original table name in database |
| `NewName` | New name for the generated entity class |
| `Columns` | Array of column renaming rules |

### Column Entry Properties

| Property | Description |
|----------|-------------|
| `Name` | Original column name in database |
| `NewName` | New name for the generated property |

## Build

```bash
dotnet build
```

## Generated Output

After building, the generated entities use the clean names:

```csharp
// Generated from tblCustomers with renamed columns
public partial class Customer
{
    public int Id { get; set; }           // was: cust_id
    public string FirstName { get; set; } // was: cust_first_name
    public string LastName { get; set; }  // was: cust_last_name
    public string Email { get; set; }     // was: cust_email
    public string? Phone { get; set; }    // was: cust_phone
    public DateTime CreatedDate { get; set; } // was: cust_created_date
    public bool IsActive { get; set; }    // was: cust_is_active

    public virtual ICollection<Order> Orders { get; set; }
}
```

## Tips

1. **Start incrementally** - Add renaming rules for a few tables first, then expand
2. **Use consistent patterns** - If all columns have `cust_` prefix, document that pattern
3. **Keep the renaming file in source control** - It's part of your schema mapping
4. **Combine with inflector** - Enable `use-inflector` in efcpt-config.json for automatic pluralization

## Common Patterns

### Remove table prefixes

```json
{ "Name": "tblUsers", "NewName": "User" }
{ "Name": "tbl_Products", "NewName": "Product" }
```

### Remove column prefixes

```json
{ "Name": "usr_id", "NewName": "Id" }
{ "Name": "usr_email", "NewName": "Email" }
```

### Expand abbreviations

```json
{ "Name": "qty", "NewName": "Quantity" }
{ "Name": "amt", "NewName": "Amount" }
{ "Name": "desc", "NewName": "Description" }
```

### Convert snake_case to PascalCase

```json
{ "Name": "first_name", "NewName": "FirstName" }
{ "Name": "created_at", "NewName": "CreatedAt" }
```
