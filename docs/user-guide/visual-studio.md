# Visual Studio Extension

`JD.Efcpt.VsExtension` is a Visual Studio 2022 (17.0+) extension that surfaces the
`JD.Efcpt.Build` MSBuild pipeline directly in the IDE: a `Regenerate Models` command, live
build-diagnostic output, and a tool window showing the status of the last profiled build. It does
not replace the MSBuild pipeline or the [`jd-efcpt` CLI](cli.md) - it drives the exact same
`dotnet build -p:EfcptForceRegenerate=true` invocation you'd run yourself, just from a menu.

## Install

The extension ships as a `.vsix` built by the `.github/workflows/vsix.yml` CI workflow (Windows
only - it never runs as part of the main [`ci.yml`](ci-cd.md) pipeline, since VSSDK/net48 tooling
only exists on Windows). Until it is published to the Visual Studio Marketplace:

1. Download the `.vsix` artifact from a `vsix.yml` workflow run (or a GitHub Release, once tagged).
2. Double-click the `.vsix` to install it into Visual Studio 2022.
3. Restart Visual Studio when prompted.

Marketplace publishing is manual and secret-gated (see [CI](#ci)) - there is no automatic install
today.

## Commands

Both commands live under **Tools > Entity Framework**, prefixed with `JD.Efcpt:` so they are never
confused with `ErikEJ.EFCorePowerTools.Cli`'s own menu (see [Avoiding conflicts](#avoiding-conflicts-with-ef-core-power-tools)):

- **JD.Efcpt: Regenerate Models** - resolves the target project (the active project if it
  references the `JD.Efcpt.Build` NuGet package, otherwise the first matching project found
  anywhere in the solution - so this works for both single- and multi-project solutions), then
  shells out to:

  ```
  dotnet build <project> -p:EfcptForceRegenerate=true -p:EfcptEnableProfiling=true -p:EfcptLogVerbosity=minimal
  ```

  exactly the invocation documented in [Forcing Regeneration](force-regenerate.md), just triggered
  from the IDE instead of a terminal. Output streams live into a dedicated **JD.Efcpt** pane in the
  Output window (separate from the Build pane), and every `JDxxxx` warning/error found in that
  output is parsed and summarized when the build finishes.

- **JD.Efcpt: Show Build Status** - opens the build-status tool window (see below).

Shelling out to `dotnet build` - rather than calling into MSBuild task APIs directly - is
deliberate: it avoids design-time-build entanglement inside Visual Studio's process and guarantees
the extension behaves identically to what CI and the CLI already do.

## Build-status tool window

The **JD.Efcpt Build Status** tool window (`Tools > Entity Framework > JD.Efcpt: Show Build
Status`) reads `obj/efcpt/build-profile.json` - the same profiling output documented in
[Build Profiling](build-profiling.md) - and shows:

- Model count (artifacts of type `GeneratedModel`)
- Last-run status, timestamp, and duration
- Warning/error counts, with the full diagnostic list

A `FileSystemWatcher` on `obj/efcpt/build-profile.json` refreshes the window automatically after
*any* profiled build - whether triggered by `JD.Efcpt: Regenerate Models`, a normal IDE build, or
the `jd-efcpt` CLI - not just builds started from the extension itself. If no profile exists yet
(profiling wasn't enabled, or nothing has built yet), the window explains what to run instead of
showing empty data.

## Redaction

Build output shown in the Output pane is redacted before display: connection strings and
credential-shaped key/value pairs (`Password=`, `Pwd=`, `User Id=`, access tokens, SAS tokens,
etc.) are masked the same way the MSBuild pipeline's own
`JD.Efcpt.Build.Core.Diagnostics.SecretRedaction` redacts them server-side and the VS Code
extension redacts them client-side - see
[`ide/vs/JD.Efcpt.Ide.Core/SecretRedaction.cs`](https://github.com/JerrettDavis/JD.Efcpt.Build/blob/main/ide/vs/JD.Efcpt.Ide.Core/SecretRedaction.cs).
Verbosity defaults to `minimal` for the same reason - less captured output is less that could ever
leak.

## Avoiding conflicts with EF Core Power Tools

This extension is designed to coexist with `ErikEJ.EFCorePowerTools.Cli` (EF Core Power Tools):

- Its own VSIX package GUID, command-set GUID, and tool-window GUID are all distinct from EF Core
  Power Tools' - see `JdEfcptCommandTable.vsct` and `PackageGuids.cs`. Nothing is shared or reused.
- Its menu commands are prefixed `JD.Efcpt:` and live in their own group under **Tools > Entity
  Framework**, not inside EF Core Power Tools' UI.
- It never scaffolds or reverse-engineers a model itself; it only triggers the `JD.Efcpt.Build`
  MSBuild pipeline, which is a completely separate code path from EF Core Power Tools' GUI-driven
  scaffolding.

## Architecture

The extension is split into two projects under `ide/vs/`:

| Project | Purpose |
|---|---|
| `JD.Efcpt.Ide.Core` | `netstandard2.0`, IDE-agnostic. Pure C# ports of the same logic the [VS Code extension](https://github.com/JerrettDavis/JD.Efcpt.Build/tree/main/ide/vscode) implements in TypeScript: the `JDxxxx` diagnostic parser, the `build-profile.json` reader, `.csproj` discovery, and secret redaction. No Visual Studio SDK dependency, so it builds and is unit-tested on the same ubuntu CI as the rest of the repo. |
| `JD.Efcpt.VsExtension` | `net48`, Windows-only VSIX. Built with [Community.VisualStudio.Toolkit.17](https://github.com/VsixCommunity/Community.VisualStudio.Toolkit). Contains only VS SDK-specific glue (commands, the tool window, process-shelling) - all parsing/reading logic is delegated to `JD.Efcpt.Ide.Core`. |

`JD.Efcpt.Ide.Core` and its test project are part of the main `JD.Efcpt.Build.sln` (and therefore
the ubuntu `ci.yml` build+test job). `JD.Efcpt.VsExtension` is deliberately **not** in that
solution - its VSSDK/net48 dependency chain would break the ubuntu build - and instead lives in its
own `ide/vs/JD.Efcpt.Vs.sln`.

## CI

`.github/workflows/vsix.yml` runs on `windows-latest` only when files under `ide/vs/` (or the
workflow itself) change:

1. Builds `JD.Efcpt.VsExtension.csproj` in Release with `msbuild` (via
   [`microsoft/setup-msbuild`](https://github.com/microsoft/setup-msbuild)) and
   `/p:DeployExtension=false`.
2. Uploads the resulting `.vsix` as a build artifact.
3. On a push to `main` or a tag - and only when the `VS_MARKETPLACE_PAT` repository secret is
   set - publishes the `.vsix` to the Visual Studio Marketplace via `VsixPublisher.exe`. Without
   the secret, this step is skipped cleanly (it does not fail the run), mirroring the NuGet
   publish guard in [`ci.yml`](ci-cd.md). No token is ever hardcoded.

## Building locally

`JD.Efcpt.VsExtension` requires the Visual Studio SDK / VSSDK build tools, which are not available
everywhere `dotnet build` runs. To build it, either:

- Open `ide/vs/JD.Efcpt.Vs.sln` in Visual Studio 2022 with the **Visual Studio extension
  development** workload installed, or
- Run `msbuild ide/vs/JD.Efcpt.VsExtension/JD.Efcpt.VsExtension.csproj /p:Configuration=Release`
  from a Developer Command Prompt (or any machine with `Microsoft.VSSDK.BuildTools` resolvable).

`JD.Efcpt.Ide.Core` and `JD.Efcpt.Ide.Core.Tests` build and run anywhere `dotnet build`/`dotnet
test` runs (including as part of `dotnet build JD.Efcpt.Build.sln`), with no VS SDK required.
