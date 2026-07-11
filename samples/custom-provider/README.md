# Custom Provider Sample

This sample shows the minimum shape of a **custom database provider** for JD.Efcpt.Build's
`customProviders` plugin registry (#184): a class library implementing `IProviderAdapter` from the
`JD.Efcpt.Build.Providers.Abstractions` package.

See [Custom Providers](../../docs/user-guide/custom-providers.md) for the full authoring and
registration guide - this sample is intentionally minimal and documentation-focused rather than a
full end-to-end generation harness (it never contacts a real database, so it builds green with no
external dependencies).

## What's here

```
custom-provider/
└── Acme.Efcpt.Mongo/
    ├── Acme.Efcpt.Mongo.csproj
    └── MongoProviderAdapter.cs
```

`Acme.Efcpt.Mongo` is a plain class library that:

- References `JD.Efcpt.Build.Providers.Abstractions` (a `ProjectReference` here, since this
  sample lives in-repo; in your own project use `dotnet add package
  JD.Efcpt.Build.Providers.Abstractions` instead)
- Implements `IProviderAdapter` (namespace `JD.Efcpt.Build.Tasks.Schema`) with exactly one
  public, concrete adapter type and a public parameterless constructor
- Returns `SchemaModel.Empty` from its `ISchemaReader`, so it never needs a real database
  connection - this is what keeps the sample buildable in CI with no external dependencies

Build it like any other class library:

```bash
dotnet build samples/custom-provider/Acme.Efcpt.Mongo
```

## Registering it (documentation only - not run by this sample)

Once you've built your own provider assembly (e.g. `Acme.Efcpt.Mongo.dll`), register it in a
consuming project via `@(EfcptCustomProvider)` and opt in with `EfcptAllowCustomProviders`. This
project intentionally does **not** wire that up as a live, always-on MSBuild target - custom
providers execute third-party code at build time, and this repository's CI has no real MongoDB
(or equivalent) to connect to. Instead, here's the exact configuration you'd add to a real
consuming project:

```xml
<ItemGroup>
  <EfcptCustomProvider Include="acme-mongo"
                        AssemblyName="Acme.Efcpt.Mongo"
                        SearchPath="$(MSBuildThisFileDirectory)providers\mongo" />
</ItemGroup>

<PropertyGroup>
  <EfcptProvider>acme-mongo</EfcptProvider>
  <!-- SECURITY: custom providers execute third-party code at build time. Only enable providers
       whose assembly you trust. -->
  <EfcptAllowCustomProviders>true</EfcptAllowCustomProviders>
  <EfcptConnectionString>...</EfcptConnectionString>
</PropertyGroup>
```

With `EfcptAllowCustomProviders` left at its default (`false`), selecting `acme-mongo` as
`EfcptProvider` fails the build fast with `JD0017` before any custom provider assembly is loaded -
see [Error Codes: Custom Provider Errors](../../docs/user-guide/error-codes.md#custom-provider-errors-jd0017-jd0019-jd0040).
