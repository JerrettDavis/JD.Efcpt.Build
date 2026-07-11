# Changelog

All notable changes to the JD.Efcpt.Build VS Code extension are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed
- `jdEfcpt.buildVerbosity` now defaults to `minimal` (was `detailed`) to reduce the chance of sensitive values appearing in build output.

### Security
- Captured build output is redacted (masking `Password`/`Pwd`/`User ID`/`Uid` values) before it is written to the Output Channel, echoed to the terminal, or embedded in diagnostics.

### Fixed
- Regeneration no longer hangs if the task system does not preserve `Task` object identity across events — completion is matched against the returned `TaskExecution`, with a safety timeout.
- A failed/aborted build no longer shows a stale prior run's profile as a fresh success; `build-profile.json` is only trusted when this run wrote it (mtime check).
- `dotnet` spawn failures (e.g. not on `PATH`) are recorded in the captured output so the error toast's Output Channel reference is not empty, and no longer wipe diagnostics from a previous real run.
- Diagnostics read the real profile schema field `level` (was `severity`).
- `JDxxxx` diagnostic messages have the trailing MSBuild ` [project.csproj]` suffix stripped.

## [0.1.0] - 2026-07-11

### Added
- `JD.Efcpt: Regenerate Models` command — runs `dotnet build <proj> -p:EfcptForceRegenerate=true -p:EfcptEnableProfiling=true -p:EfcptLogVerbosity=<verbosity>` as a VS Code task with the `$jdEfcpt-msbuild` problem matcher.
- `JD.Efcpt: Show Build Status` command — reveals the JD.Efcpt.Build activity-bar view.
- Status bar indicator showing build progress, resulting model count, and failures, with a tooltip summarizing the last run.
- "Build Status" TreeView populated from `obj/efcpt/build-profile.json` (status, model count, duration, diagnostics).
- `JDxxxx` diagnostics (from `docs/user-guide/error-codes.md`) surfaced in the Problems panel via a `DiagnosticCollection` keyed by the owning `.csproj`.
- FileSystemWatcher on `**/obj/efcpt/build-profile.json` so status updates even when a plain CLI `dotnet build` is run outside the extension.
- Settings: `jdEfcpt.enableProfiling`, `jdEfcpt.buildVerbosity`, `jdEfcpt.dotnetPath`.
- One-time, soft-detect recommendation to install the C# Dev Kit extension when it isn't present.
