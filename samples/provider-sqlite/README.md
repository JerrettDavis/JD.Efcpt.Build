# SQLite reference sample

Build the committed reference model without a database:

```sh
dotnet build EntityFrameworkCoreProject -p:EfcptEnabled=false
```

The project, `efcpt-config.json`, and `Generated/AppDbContext.cs` demonstrate a Customers table with an integer primary key and a required name. The committed code is an illustrative reference, not a live database test.

To regenerate, create a SQLite database containing:

```sql
CREATE TABLE Customers (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
```

Set the `EfcptConnectionString` environment variable to `Data Source=/absolute/path/sample.db`, then run `dotnet build EntityFrameworkCoreProject`. The project includes the SQLite build-time satellite when generation is enabled. Generation replaces the committed reference types in compilation with generated files under `obj/efcpt/Generated`.

Configure the application context using `new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options`.
