# JD.Efcpt.Build for VS Code

Bring the [JD.Efcpt.Build](https://github.com/JerrettDavis/JD.Efcpt.Build) MSBuild pipeline into VS Code and C# Dev Kit. Trigger model regeneration, watch build status update in real time, and see `JDxxxx` diagnostics without leaving the editor.

This extension is a thin client over the existing MSBuild pipeline — it does not reimplement EF Core Power Tools generation. It shells out to `dotnet build` with the documented [Force Regenerate](https://github.com/JerrettDavis/JD.Efcpt.Build/blob/main/docs/user-guide/force-regenerate.md) contract and reads the [build profiling](https://github.com/JerrettDavis/JD.Efcpt.Build/blob/main/docs/user-guide/build-profiling.md) output that the pipeline already produces.

## Features

- **Command Palette**
  - `JD.Efcpt: Regenerate Models` — force-regenerates EF Core models for a `.csproj` referencing `JD.Efcpt.Build`. If more than one project in the workspace references the package, you'll be prompted to pick one.
  - `JD.Efcpt: Show Build Status` — opens the JD.Efcpt.Build activity-bar view.
- **Status bar indicator** — shows `$(sync~spin) EF: building…` while a regeneration runs, then `$(check) EF: N models` or `$(error) EF: failed`. Hover for the last run time, duration, and diagnostic count. Click to open the status view.
- **Build Status view** (activity bar) — a TreeView driven by `obj/efcpt/build-profile.json`: status, model count, last run time, duration, and any diagnostics.
- **Diagnostics** — `warning JDxxxx: ...` / `error JDxxxx: ...` lines emitted by JD.Efcpt.Build tasks (see [error codes](https://github.com/JerrettDavis/JD.Efcpt.Build/blob/main/docs/user-guide/error-codes.md)) are parsed and surfaced in the Problems panel, keyed to the owning project file.
- **Plain CLI builds are picked up too** — a `FileSystemWatcher` on `**/obj/efcpt/build-profile.json` refreshes the status bar/view even if you ran `dotnet build` yourself in a terminal.
- **C# Dev Kit friendly** — soft-detects the C# Dev Kit extension and offers a one-time nudge to install it if it's missing; no hard dependency or private API usage.

## Requirements

- A workspace containing at least one `.csproj` with a `PackageReference` to `JD.Efcpt.Build`.
- The .NET SDK and `dotnet` CLI available on `PATH` (or configured via `jdEfcpt.dotnetPath`).

## Extension Settings

| Setting | Default | Description |
|---|---|---|
| `jdEfcpt.enableProfiling` | `true` | Passes `-p:EfcptEnableProfiling=true`, so builds write `obj/efcpt/build-profile.json` for the status bar/view to read. |
| `jdEfcpt.buildVerbosity` | `"minimal"` | Value passed as `-p:EfcptLogVerbosity`. Defaults to `minimal` to reduce the chance of sensitive values appearing in build output. |
| `jdEfcpt.dotnetPath` | `"dotnet"` | Path to the `dotnet` executable. |

## How regeneration works

Running `JD.Efcpt: Regenerate Models` executes:

```
dotnet build <proj> -p:EfcptForceRegenerate=true -p:EfcptEnableProfiling=true -p:EfcptLogVerbosity=minimal
```

as a VS Code task, with the `$jdEfcpt-msbuild` problem matcher attached for standard `path(line,col): error CSxxxx: msg` compiler diagnostics. Task output is also scanned for `JDxxxx` warning/error lines, which are added to the Problems panel separately from compiler diagnostics. Captured output is passed through a secret-redactor (masking `Password`/`Pwd`/`User ID`/`Uid` values) before it is written to the Output Channel, echoed to the terminal, or turned into diagnostics.

Connection strings and other sensitive values are automatically redacted by JD.Efcpt.Build's profiling framework before they ever reach `build-profile.json` — see [Build Profiling: Security Considerations](https://github.com/JerrettDavis/JD.Efcpt.Build/blob/main/docs/user-guide/build-profiling.md#security-considerations).

## Known Limitations

- The status bar/view reflect the *most recently written* `build-profile.json`; in a multi-project workspace with several `JD.Efcpt.Build` consumers, only the most recently updated profile is shown.
- Profiling must be enabled (`jdEfcpt.enableProfiling`, on by default) for the status bar/view to populate — without it, only the Problems-panel diagnostics from task output are available.

## Release Notes

See [CHANGELOG.md](./CHANGELOG.md).
