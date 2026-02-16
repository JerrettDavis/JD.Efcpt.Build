# Regenerating MSBuild Files

The `buildTransitive/` MSBuild files (`.props` and `.targets`) are generated from C# definitions using [JD.MSBuild.Fluent](https://www.nuget.org/packages/JD.MSBuild.Fluent).

## Why Generation is Disabled

Automatic generation is **disabled by default** due to a file locking issue when multi-targeting:

1. JD.MSBuild.Fluent loads `JD.Efcpt.Build.dll` to call `DefinitionFactory.Create()`
2. The loaded DLL is locked by the .NET host process
3. MSBuild tries to copy the DLL to `bin/` but fails because it's locked
4. Build fails after 10 retries

This affects both local builds and CI.

## When to Regenerate

Regenerate the MSBuild files when you modify:

- `Definitions/BuildTransitivePropsFactory.cs`
- `Definitions/BuildTransitiveTargetsFactory.cs`
- `Definitions/Registry/UsingTasksRegistry.cs`
- `Definitions/Shared/SharedPropertyGroups.cs`

## How to Regenerate

### Option 1: Single Target Framework (Recommended)

Build only `net8.0` to avoid multi-targeting file locks:

```powershell
cd src/JD.Efcpt.Build
dotnet msbuild -t:JDMSBuildFluentGenerate -p:TargetFramework=net8.0 -p:JDMSBuildFluentGenerateEnabled=true
```

This generates files to `obj/msbuild_fluent_generated/` which are then copied to `buildTransitive/`.

### Option 2: Enable in Project File

Temporarily enable generation in `JD.Efcpt.Build.csproj`:

```xml
<JDMSBuildFluentGenerateEnabled>true</JDMSBuildFluentGenerateEnabled>
```

Then build:

```powershell
dotnet build -p:TargetFramework=net8.0
```

**Warning:** Don't commit with generation enabled, or CI will fail!

### Option 3: Clean Rebuild

If you don't have stale processes locking files:

```powershell
dotnet clean
dotnet build -p:TargetFramework=net8.0 -p:JDMSBuildFluentGenerateEnabled=true
```

## Verifying Generated Files

After regeneration, check that these files exist and are updated:

```
buildTransitive/
├── JD.Efcpt.Build.props
└── JD.Efcpt.Build.targets
```

Commit the regenerated files:

```powershell
git add buildTransitive/
git commit -m "chore: regenerate MSBuild files from definitions"
```

## CI Considerations

CI always has `JDMSBuildFluentGenerateEnabled=false` because:

1. Generated files are tracked in git (source of truth)
2. Multi-targeting causes file locking failures
3. Faster CI builds (no generation overhead)

The generated files should be reviewed in PRs like any other code.
