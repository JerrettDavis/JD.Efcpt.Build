# efcpt Compatibility

JD.Efcpt.Build wraps the [ErikEJ.EFCorePowerTools.Cli](https://www.nuget.org/packages/ErikEJ.EFCorePowerTools.Cli) (`efcpt`) dotnet tool to scaffold EF Core models at build time. Because `efcpt` is an external tool whose release lines track EF Core versions, a new upstream release can change or break scaffolding behavior without notice. This page documents which `efcpt` versions are supported and how the project continuously guards against regressions.

## Supported versions

`efcpt` CLI release lines map directly to EF Core major versions:

| efcpt line | EF Core line | Target frameworks | Status |
| --- | --- | --- | --- |
| `8.x` | EF Core 8 | `net8.0` | Supported |
| `9.x` | EF Core 9 | `net9.0` | Supported |
| `10.x` | EF Core 10 | `net10.0` | Supported (default) |
| `7.x` and earlier | EF Core 7- | `net7.0` | Not tested (end of life) |

The default pin is `<EfcptToolVersion>10.*</EfcptToolVersion>`. On .NET 10+ the CLI is executed via `dnx` with no installation required; on .NET 8/9 it must be installed as a global or manifest tool (see [Getting Started](getting-started.md)). The `10.x` CLI line is backward compatible and can scaffold EF Core 8/9/10 projects, so most consumers should stay on `10.*` regardless of their target framework.

## Pinning a specific version

Override the tool version per project:

```xml
<PropertyGroup>
  <EfcptToolVersion>10.*</EfcptToolVersion>
</PropertyGroup>
```

> **Note:** On .NET 10+, generation runs through `dnx`, which resolves the tool package independently of `EfcptToolVersion`. To force an exact build (for example, to reproduce a specific version), set `EfcptToolPath` to an installed `efcpt` executable — this bypasses `dnx` and uses that binary directly.

## Weekly compatibility matrix

The repository runs a scheduled GitHub Actions workflow, [`efcpt-compat.yml`](https://github.com/jerrettdavis/JD.Efcpt.Build/blob/main/.github/workflows/efcpt-compat.yml), that detects when a newly released `efcpt` version breaks the package:

1. **Resolve** — Queries the nuget.org [flat-container index](https://api.nuget.org/v3-flatcontainer/erikej.efcorepowertools.cli/index.json) for the latest *stable* `efcpt` release in each supported EF Core line (`8`, `9`, `10`).
2. **Build** — For each resolved version, packs the local `JD.Efcpt.Build` package into a local feed, installs that exact `efcpt` version as a global tool, and rebuilds the representative sample `samples/simple-generation` (a database-first `net10.0` / EF Core 10 project that scaffolds offline from a `.sqlproj` / DACPAC). The exact version is pinned via `EfcptToolPath` so `dnx` cannot substitute a different build.
3. **Report** — Each matrix leg runs independently (`fail-fast: false`). A failing leg writes a job summary naming the incompatible `efcpt` version and an excerpt of the build error, so a breaking upstream release is surfaced loudly rather than silently.

The workflow runs weekly (Mondays 07:00 UTC) and can be triggered manually via **workflow_dispatch**.

### Adjusting the tested lines

The set of tested EF Core lines is controlled by the `EFCPT_SUPPORTED_LINES` environment variable in the workflow (default `"8 9 10"`). Add or remove a major line there when the support matrix changes.

## Reacting to a failure

When the compat matrix reports an incompatible version:

1. Review the job summary and uploaded build log to identify the failing `efcpt` version and error.
2. Reproduce locally by installing that version and building the sample with `EfcptToolPath` pinned.
3. If the break is legitimate, pin consumers to the last known-good line (for example `<EfcptToolVersion>10.1.*</EfcptToolVersion>`) and track the upstream change before widening the range again.
