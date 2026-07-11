# jd-efcpt CLI

`jd-efcpt` is a companion [.NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
for `JD.Efcpt.Build`. It shares the same underlying logic as the MSBuild pipeline
(`JD.Efcpt.Build.Core`) so `jd-efcpt init` and `jd-efcpt doctor` behave identically to what a real
build would do - without needing a project, a build, or even a database connection.

## Install

As a global tool:

```bash
dotnet tool install --global JD.Efcpt.Cli
```

Or as a local (repo-pinned) tool, alongside a committed tool manifest:

```bash
dotnet new tool-manifest   # only if you don't already have one
dotnet tool install JD.Efcpt.Cli
dotnet tool run jd-efcpt -- --help
```

Verify the install:

```bash
jd-efcpt --help
```

## `jd-efcpt init`

Bootstraps an `efcpt-config.json` file - the same file `JD.Efcpt.Build` looks for next to your
project (see [Configuration](configuration.md)).

```bash
jd-efcpt init [<output-dir>] [--provider <name>] [--dbcontext-name <name>] [--namespace <ns>] [--force] [--online]
```

| Option | Description |
|---|---|
| `<output-dir>` | Directory to write `efcpt-config.json` into (positional, optional; defaults to the current directory). |
| `--provider` | Database provider to record for your reference (`mssql`, `postgres`, `mysql`, `sqlite`, `oracle`, `firebird`, `snowflake`, or a recognized alias - see [Provider Support](provider-support.md)). Validated only; unrecognized values fail with a list of supported providers. |
| `--dbcontext-name` | Name of the generated `DbContext` class (default: `ApplicationDbContext`). |
| `--namespace` | Root namespace for generated code (default: `EfcptProject`). |
| `--force` | Overwrite an existing `efcpt-config.json`. Without it, `init` refuses to clobber a file that's already there. |
| `--online` | Fetch the latest `efcpt-config` schema from GitHub instead of the schema bundled with the tool. |

**Offline by default.** `init` generates the config from the JSON schema bundled inside the
`jd-efcpt` package itself - no network access is required, and the command works the same in an
air-gapped CI runner as it does on your laptop. Pass `--online` only if you specifically want the
very latest schema from `ErikEJ/EFCorePowerTools` (matching what
[`EfcptOfflineMode`](offline.md) documents for the MSBuild pipeline's own schema-fetch fallback).

Example:

```bash
jd-efcpt init ./src/MyApp.Data --provider postgres --dbcontext-name AppDbContext --namespace MyApp.Data
```

```
Wrote ./src/MyApp.Data/efcpt-config.json
```

Exit codes: `0` on success, `1` on any error (unsupported `--provider`, existing file without
`--force`, or a schema read/write failure).

## `jd-efcpt doctor`

Reports exactly which execution path the MSBuild pipeline's `RunEfcpt` task would take for a given
target framework - the same report `EfcptDoctor` produces via `dotnet build -t:EfcptDoctor` (see
[Tool Acquisition](tool-acquisition.md#efcptdoctor)), but without needing a project or a build at
all. Both share the exact same diagnosis engine, so the two can never drift apart.

```bash
jd-efcpt doctor [--target-framework <tfm>] [--tool-mode <mode>] [--tool-package-id <id>] [--tool-command <cmd>] [--tool-path <path>] [--offline] [--auto-acquire] [--strict] [--working-dir <dir>]
```

| Option | Description |
|---|---|
| `--target-framework` | Target framework to diagnose, e.g. `net8.0`, `net10.0` (default: empty). |
| `--tool-mode` | Tool resolution mode: `auto`, `tool-manifest`, or `global` (default: `auto`). |
| `--tool-package-id` | The dotnet tool package id (default: `ErikEJ.EFCorePowerTools.Cli`). |
| `--tool-command` | The tool command name (default: `efcpt`). |
| `--tool-path` | Explicit path to the efcpt executable, bypassing automatic resolution. |
| `--offline` | Diagnose as if `EfcptOfflineMode` were enabled. |
| `--auto-acquire` | Diagnose as if `EfcptAutoAcquireTool` were enabled (default: `true`). |
| `--strict` | Exit with code `1` (instead of `2`) when no viable execution path is found - useful as a CI gate. |
| `--working-dir` | Working directory used for tool-manifest discovery (default: current directory). |

Example:

```bash
jd-efcpt doctor --target-framework net10.0
```

```
TargetFramework: 'net10.0' (parsed major version: 10)
SDK 10+ installed: True
dnx available: True
dnx usable for this build: True
Tool manifest discovered: (none found)
Global tool 'efcpt' resolvable on PATH: False
Explicit ToolPath: (not set)
EfcptOfflineMode: False
EfcptAutoAcquireTool: True (effective, offline-adjusted: True)
Verdict: dnx execution will be used: dotnet dnx ErikEJ.EFCorePowerTools.Cli --yes -- ...
```

### Exit codes

| Exit code | Meaning |
|---|---|
| `0` | A viable execution path was found. |
| `2` | No viable execution path was found, and `--strict` was **not** passed - advisory only, useful for a human reading the report. |
| `1` | No viable execution path was found, and `--strict` **was** passed - useful for scripting a CI gate (`jd-efcpt doctor --strict || exit 1`). |

## When to use the CLI vs. `EfcptDoctor`

- Use `jd-efcpt doctor` when you want a fast answer without a full project/build context - e.g.
  scripting a pre-flight check in CI before checking out the rest of the pipeline, or diagnosing a
  teammate's machine over a support call.
- Use `dotnet build -t:EfcptDoctor` when you want the diagnosis to reflect a *specific project's*
  actual MSBuild property values (picked up from its `.csproj`/`Directory.Build.props`) rather than
  values you type on the command line.

Both report the identical verdict strings for the same inputs, since they share
`JD.Efcpt.Build.Core`'s `DoctorEngine`.
