# Changelog

All notable changes to the JD.Efcpt.Build VS Code extension are documented here.
This project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
