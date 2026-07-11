# Tool Acquisition (.NET 8/9 Auto-Acquire)

On .NET 10+, `RunEfcpt` can run the efcpt CLI on demand via `dnx <package> --yes -- ...` -
no install step required. On .NET 8 and .NET 9, `dnx` is not usable, so the tool has to already
be installed somewhere the task can find it: a restored local tool manifest, a global tool on
`PATH`, or an explicit `EfcptToolPath`. Historically, if none of those were present, the task
fell back to `dotnet tool update --global <package>` - which frequently fails in practice because
`~/.dotnet/tools` isn't on `PATH` in many CI images and fresh dev machines, and a global install
also isn't hermetic (it's shared, mutable state outside the project).

`EfcptAutoAcquireTool` (default `true`) fixes this by bootstrapping a **project-local, obj-scoped
tool manifest** instead - hermetic, self-cleaning (it lives under `obj/`, so it's wiped by
`dotnet clean`), and independent of `PATH`.

## What it does

When all of the following are true, `RunEfcpt` bootstraps an obj-local tool manifest and installs
the efcpt tool into it, before tool resolution runs:

1. The project targets .NET 8 or .NET 9 (or, more precisely, dnx is not usable - see
   [What "dnx not usable" means](#what-dnx-not-usable-means) below).
2. `EfcptAutoAcquireTool` is `true` (the default).
3. `EfcptOfflineMode` is **not** enabled (see [Interaction with offline mode](#interaction-with-offline-mode)).
4. No explicit `EfcptToolPath` is set.
5. No already-usable tool manifest was discovered (walking up from `WorkingDirectory`) that
   actually lists the efcpt tool.
6. No global tool is already resolvable on `PATH`.
7. `EfcptToolPackageId` has a value (it does by default: `ErikEJ.EFCorePowerTools.Cli`).

The bootstrap itself is equivalent to running, in `$(EfcptOutput)` (typically `obj\efcpt\`):

```bash
dotnet new tool-manifest
dotnet tool install ErikEJ.EFCorePowerTools.Cli --version 10.*
```

`dotnet new tool-manifest` is only run if `.config/dotnet-tools.json` doesn't already exist at
that location - so a second build reuses the manifest from the first (as long as `obj/` wasn't
cleaned). Once the manifest and tool are in place, tool resolution proceeds exactly as it would
for a project that came with its own committed manifest: `dotnet tool run efcpt -- ...`.

## What "dnx not usable" means

The same condition `RunEfcpt` uses for the dnx resolution branch: the project does **not**
simultaneously satisfy target framework `net10.0`+, a .NET 10+ SDK being installed, and `dnx`
being available. If any of those three is false - including simply targeting `net8.0` or
`net9.0` - dnx is not usable and auto-acquisition (if otherwise enabled) can kick in.

If dnx *is* usable, auto-acquisition never runs - dnx already handles execution without any
install step, so there is nothing for it to do. Auto-acquisition is unaffected on .NET 10+.

## Opting out

Set `EfcptAutoAcquireTool=false` to disable auto-acquisition and restore the legacy behavior
(fall back to the global tool path / `dotnet tool update --global`):

```xml
<PropertyGroup>
  <EfcptAutoAcquireTool>false</EfcptAutoAcquireTool>
</PropertyGroup>
```

This is a reasonable choice if you already commit a tool manifest to source control, or if you
intentionally manage the efcpt tool as a global install across your team/CI images.

## Interaction with offline mode

`EfcptOfflineMode` always wins: when offline mode is enabled, auto-acquisition **never** runs,
regardless of `EfcptAutoAcquireTool`. Installing a tool is itself a network operation, and
offline mode's whole contract is "never spawn a network-dependent step." This precedence is
enforced inside the `RunEfcpt` task itself (not merely via an MSBuild condition), so it can't be
bypassed by property ordering or overrides.

In practice, this means offline builds still need one of the network-free tool sources
documented in [offline.md](offline.md) (a pre-restored manifest, a pre-installed global tool, or
an explicit `EfcptToolPath`) - auto-acquisition cannot substitute for that pre-provisioning step,
and offline builds on .NET 8/9 with none of those available still fail fast with `JD0026`.

## When acquisition fails

If auto-acquisition runs but `dotnet new tool-manifest` or `dotnet tool install` fails (network
issue, bad `EfcptToolVersion` constraint, misconfigured NuGet feed, etc.), the build fails with
error `JD0027` - see
[error-codes.md](error-codes.md#jd0027-tool-auto-acquisition-failed) for the full message shape
and remediation options.

## EfcptDoctor

`EfcptDoctor` is an opt-in diagnostic target that reports exactly which execution path `RunEfcpt`
would take for the current project - without running (or installing) anything:

```bash
dotnet build -t:EfcptDoctor
```

It reports:

- The target framework and its parsed major version
- Whether the .NET 10+ SDK and `dnx` are available
- Whether a tool manifest was discovered (and whether it lists the efcpt tool)
- Whether a global tool is resolvable on `PATH`
- The explicit `EfcptToolPath` state
- The effective `EfcptOfflineMode` / `EfcptAutoAcquireTool` settings
- A **verdict**: either the execution path that will be used, or the exact remediation needed if
  none is viable

`EfcptDoctor` never fails the build by default - it's purely informational. Pass
`-p:EfcptDoctorStrict=true` to make it fail (non-zero exit) when no viable execution path was
found, which is useful as a CI gate to catch tool-resolution misconfiguration before it surfaces
as a confusing failure mid-build:

```bash
dotnet build -t:EfcptDoctor -p:EfcptDoctorStrict=true
```

## Summary

| Scenario | Behavior |
|---|---|
| .NET 10+, dnx usable | dnx execution; auto-acquisition never runs |
| .NET 8/9, manifest already present and lists the tool | Manifest resolution; auto-acquisition never runs |
| .NET 8/9, global tool already on `PATH` | Global tool resolution; auto-acquisition never runs |
| .NET 8/9, nothing usable, `EfcptAutoAcquireTool=true` (default), online | Obj-local manifest bootstrapped and installed, then `dotnet tool run` |
| .NET 8/9, nothing usable, `EfcptAutoAcquireTool=false` | Legacy global-tool fallback (`dotnet tool update --global`) |
| `EfcptOfflineMode=true`, nothing pre-provisioned | Fails fast with `JD0026`; auto-acquisition never attempted |
| Auto-acquisition attempted but `dotnet tool install` fails | Fails with `JD0027` |
