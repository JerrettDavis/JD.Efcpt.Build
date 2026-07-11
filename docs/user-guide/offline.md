# Offline / Air-Gapped Builds

`EfcptOfflineMode` lets you build in environments that have no outbound network access -
air-gapped CI agents, locked-down secure build environments, or any pipeline where a build
should never silently reach out to the internet.

## What it does

When `EfcptOfflineMode` is `true` (or the `EFCPT_OFFLINE` environment variable is set to a
truthy value - see [Enabling offline mode](#enabling-offline-mode) below for the exact accepted
values), the `RunEfcpt` task refuses to spawn any of the three network-dependent code
paths it would otherwise use to resolve or restore the efcpt CLI:

1. **dnx execution** - on .NET 10+ projects, the task would normally run `dotnet dnx <package>
   --yes -- ...` to fetch and execute the tool on demand. This is skipped entirely offline.
2. **Tool-manifest restore** - `dotnet tool restore` against a discovered
   `.config/dotnet-tools.json` manifest. Skipped offline.
3. **Global tool update** - `dotnet tool update --global <package>`. Skipped offline.

It also disables the SDK update-check target (`EfcptCheckForUpdates`), which otherwise makes an
outbound call to check for a newer `JD.Efcpt.Sdk` version.

## What you need to provide

Because none of the network-dependent paths run, the efcpt tool must already be available
through one of three network-free routes:

- **A local tool manifest that has already been restored** before the offline build runs:
  ```bash
  dotnet new tool-manifest
  dotnet tool install ErikEJ.EFCorePowerTools.Cli --version 10.*
  ```
  Restore this once, on a machine with network access (or in an earlier CI stage that does have
  access), then carry the restored `.config/dotnet-tools.json` (and the NuGet tool cache) into
  the offline environment.

- **A global tool install**, performed ahead of time on the build agent image:
  ```bash
  dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version 10.*
  ```

- **An explicit, pre-provisioned executable**, via `EfcptToolPath`:
  ```xml
  <PropertyGroup>
    <EfcptOfflineMode>true</EfcptOfflineMode>
    <EfcptToolPath>C:\tools\efcpt\efcpt.exe</EfcptToolPath>
  </PropertyGroup>
  ```

If none of these are available, the build fails fast with error `JD0026` instead of hanging or
failing obscurely against a missing tool - see [error-codes.md](error-codes.md#jd0026-efcpt-tool-not-available-offline).

## Enabling offline mode

MSBuild property (recommended, per-project or via `Directory.Build.props`):

```xml
<PropertyGroup>
  <EfcptOfflineMode>true</EfcptOfflineMode>
</PropertyGroup>
```

Or via the `EFCPT_OFFLINE` environment variable (useful for CI pipelines that shouldn't need to
touch every consuming project's file):

```bash
set EFCPT_OFFLINE=true
dotnet build
```

Either is sufficient; they are OR-ed together at two levels:

- **MSBuild property default.** `EfcptOfflineMode`'s own default (applied only when the property
  is not explicitly set by the project/`Directory.Build.props`) checks `$(EFCPT_OFFLINE)` -
  MSBuild exposes process environment variables as ordinary properties, so this needs no special
  plumbing. If `$(EFCPT_OFFLINE)` is one of the truthy tokens below, `EfcptOfflineMode` defaults
  to `true`; this is what makes the update-check target (`_EfcptCheckForUpdates`, which reads
  `$(EfcptOfflineMode)` directly) also skip when only the env var is set.
- **Task-level OR.** Independently, the `RunEfcpt` task itself also reads
  `Environment.GetEnvironmentVariable("EFCPT_OFFLINE")` at execution time and OR's it with the
  `EfcptOfflineMode` task property, so offline behavior holds even if something explicitly set
  `EfcptOfflineMode=false` at the MSBuild property level while `EFCPT_OFFLINE` is set in the
  process environment.

**Accepted truthy values** for `EFCPT_OFFLINE` (case-insensitive): `true`, `yes`, `on`, `1`,
`enable`, `enabled`, `y`. Any other value - including `false`, `0`, `no`, `off`, or an empty/unset
variable - does **not** enable offline mode via the environment variable.

## What still runs

Offline mode only gates the three network-dependent tool resolution/restore branches and the
update-check target. It does not change DACPAC building, schema fingerprinting, or model
generation itself - those are already local operations that don't touch the network.
