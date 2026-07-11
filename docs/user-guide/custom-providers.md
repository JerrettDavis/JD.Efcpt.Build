# Custom Providers

JD.Efcpt.Build ships built-in support for SQL Server, PostgreSQL, MySQL, SQLite, Oracle,
Firebird, and Snowflake (see [Provider Support](provider-support.md)). For any other database
engine, you can author and register a **custom provider** - a small .NET assembly that plugs into
the same satellite-package assembly-resolution machinery the built-in providers use (#189),
without needing to fork or extend JD.Efcpt.Build itself.

> **Security warning:** custom providers load and execute third-party code at build time. This
> feature is disabled by default and must be explicitly opted into with
> `EfcptAllowCustomProviders`. Only enable custom providers whose assembly you trust - the same
> way you'd only install a NuGet package from a source you trust.

## Authoring a custom provider

### 1. Reference the contract package

Create a class library and reference `JD.Efcpt.Build.Providers.Abstractions`:

```bash
dotnet add package JD.Efcpt.Build.Providers.Abstractions
```

This package contains only the two interfaces you need to implement - no ADO.NET driver, no
MSBuild dependencies - keeping your provider's own dependency footprint minimal.

### 2. Implement IProviderAdapter and ISchemaReader

Both interfaces live in the `JD.Efcpt.Build.Tasks.Schema` namespace (not
`JD.Efcpt.Build.Providers.Abstractions` - the package's `RootNamespace` matches the namespace
`JD.Efcpt.Build.Tasks` already uses internally, so a reflection-loaded adapter can be cast back to
the exact same interface type regardless of which assembly produced it):

```csharp
using System.Data.Common;
using JD.Efcpt.Build.Tasks.Schema;

namespace Acme.Efcpt.Mongo;

// Must be public, concrete, and have a public parameterless constructor - see the
// "one type per assembly" rule below.
public sealed class MongoProviderAdapter : IProviderAdapter
{
    public DbConnection CreateConnection(string connectionString)
        => new MongoDbConnectionAdapter(connectionString); // your own DbConnection wrapper

    public ISchemaReader CreateSchemaReader() => new MongoSchemaReader();
}

public sealed class MongoSchemaReader : ISchemaReader
{
    public SchemaModel ReadSchema(string connectionString)
    {
        // Connect, enumerate collections/fields, and build a SchemaModel.
        // See SchemaModel in JD.Efcpt.Build.Providers.Abstractions for the shape.
        ...
    }
}
```

- `IProviderAdapter.CreateConnection` returns an (unopened) `System.Data.Common.DbConnection` for
  your engine - if your driver doesn't ship an ADO.NET `DbConnection`, you can implement a thin
  wrapper.
- `IProviderAdapter.CreateSchemaReader` returns your `ISchemaReader`, whose `ReadSchema` builds
  the canonical `SchemaModel` (tables, columns, indexes, constraints) that the rest of the
  pipeline (fingerprinting, `efcpt` invocation) consumes.

### 3. One type per assembly, public parameterless constructor

The resolver (the same `ProviderAdapterResolver` built-in satellite providers use) discovers your
adapter by reflecting over the assembly for the first concrete type assignable to
`IProviderAdapter`, then instantiates it via `Activator.CreateInstance` - which requires a public,
accessible parameterless constructor. Your provider assembly should therefore contain **exactly
one** concrete `IProviderAdapter` implementation. If none is found, the build fails with
[`JD0040`](error-codes.md#jd0040-custom-provider-assembly-has-no-iprovideradapter).

### 4. Build and note the output assembly name

Build your project normally (`dotnet build`). Note the simple assembly name of the resulting DLL
(without the `.dll` extension or any path) - for example `Acme.Efcpt.Mongo` for
`Acme.Efcpt.Mongo.dll`. You'll need this for the `AssemblyName` metadata below.

## Registering a custom provider

Register your provider in the consuming project via the `@(EfcptCustomProvider)` item group:

```xml
<ItemGroup>
  <EfcptCustomProvider Include="acme-mongo"
                        AssemblyName="Acme.Efcpt.Mongo"
                        SearchPath="$(MSBuildThisFileDirectory)providers\mongo" />
</ItemGroup>

<PropertyGroup>
  <EfcptProvider>acme-mongo</EfcptProvider>
  <EfcptAllowCustomProviders>true</EfcptAllowCustomProviders>
  <EfcptConnectionString>...</EfcptConnectionString>
</PropertyGroup>
```

| Part | Meaning |
|---|---|
| **Item identity** (`acme-mongo`) | The custom provider **key** - the value you set `EfcptProvider` to, to select this provider. Must not collide with any built-in provider key or alias (`mssql`, `sqlserver`, `sql-server`, `postgres`, `postgresql`, `pgsql`, `mysql`, `mariadb`, `sqlite`, `sqlite3`, `oracle`, `oracledb`, `firebird`, `fb`, `snowflake`, `sf`) - a collision fails the build with [`JD0019`](error-codes.md#jd0019-custom-provider-key-collides-with-a-built-in-provider), regardless of which provider `EfcptProvider` actually selects. |
| `AssemblyName` (required) | The simple assembly name (no directory, no `.dll`) containing your `IProviderAdapter` implementation. |
| `SearchPath` (optional) | An additional directory to search for that assembly, appended to the same search-path mechanism `@(EfcptProviderSearchPath)` uses (the bundled `providers/{key}/` folder next to the task assembly is always searched first, then every configured search path in order). If your assembly is already discoverable via `@(EfcptProviderSearchPath)` (for example, a satellite NuGet package's own `.targets` file already appends its directory), `SearchPath` is optional. |

You can register multiple `@(EfcptCustomProvider)` items; only the one matching the active
`EfcptProvider` value is actually loaded.

### The `EfcptAllowCustomProviders` opt-in

`EfcptAllowCustomProviders` defaults to `false`. If `EfcptProvider` resolves to a registered
custom provider key while this is not `true`, the build fails fast with
[`JD0017`](error-codes.md#jd0017-custom-provider-not-allowed) **before any custom provider
assembly is loaded** - registering a custom provider is not itself enough to activate it. This is
a deliberate, fail-closed security gate: custom providers execute arbitrary third-party code at
build time, so opting in is a conscious decision, not an accident of configuration.

When a custom provider is registered, opted in, and actually selected, the build logs a warning
each time, reminding you that third-party code is executing:

```
warning: Provider 'acme-mongo' is a custom provider. Its assembly executes third-party code at
build time - only enable custom providers you trust.
```

Built-in providers (`mssql`, `postgres`, etc.) are never gated by `EfcptAllowCustomProviders` -
this property has no effect on them.

## Troubleshooting

See [Error Codes Reference](error-codes.md#custom-provider-errors-jd0017-jd0019-jd0040) for the
full list, but in short:

| Code | Cause | Fix |
|---|---|---|
| `JD0017` | Custom provider selected but `EfcptAllowCustomProviders` is not `true` | Set `<EfcptAllowCustomProviders>true</EfcptAllowCustomProviders>` (only if you trust the assembly) |
| `JD0018` | Assembly not found on any search path, or found but failed to load/instantiate | Verify `AssemblyName`/`SearchPath` metadata; check the inner exception for the underlying load failure |
| `JD0019` | Custom provider key collides with a built-in provider key/alias | Rename the `@(EfcptCustomProvider)` item's identity |
| `JD0040` | Assembly loaded, but contains no concrete `IProviderAdapter` | Ensure exactly one public, concrete `IProviderAdapter` type with a public parameterless constructor |

## See Also

- [Provider Support](provider-support.md) - built-in providers and their satellite packages
- [Error Codes Reference](error-codes.md) - full error code catalog
- [Connection-String Sources](connection-string-sources.md) - the analogous pluggable-source
  design for connection strings (#188), which this feature's registry design mirrors
