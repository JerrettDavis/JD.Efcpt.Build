# SQL Server reference sample

Build the committed reference model without a database:

```sh
dotnet build EntityFrameworkCoreProject -p:EfcptEnabled=false
```

The project, `efcpt-config.json`, and `Generated/AppDbContext.cs` demonstrate a Customers table with an integer primary key and a required name. The committed code is an illustrative reference, not a live database test.

To regenerate, create this table in a development database:

```sql
CREATE TABLE dbo.Customers (Id int IDENTITY PRIMARY KEY, Name nvarchar(200) NOT NULL);
```

Set the `EfcptConnectionString` environment variable to your development SQL Server connection string, then run `dotnet build EntityFrameworkCoreProject`. SQL Server's build-time driver is bundled. Generation replaces the committed reference types in compilation with generated files under `obj/efcpt/Generated`.

Configure the application context using `new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options`. Keep credentials outside source control.
