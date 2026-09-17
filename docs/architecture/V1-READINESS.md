# v1.0 readiness evidence

This records the acceptance evidence for [issue #191](https://github.com/JerrettDavis/JD.Efcpt.Build/issues/191). Its September 2026 stale-bot closure was administrative, not a verification of readiness. The final issue closeout must link passing checks on the merged main commit.

| Required item | Implementation and verification |
|---|---|
| Design-time guard | [PR #180](https://github.com/JerrettDavis/JD.Efcpt.Build/pull/180); `JD.Efcpt.Build.targets` disables the pipeline for `DesignTimeBuild=true` unless explicitly opted in. `DesignTimeBuildTests` verifies the guarded build and opt-in path. |
| Compatibility matrix | `provider-compat.yml`: .NET 8/9/10 × Windows/Linux/macOS, with all seven providers verified from TRX results in every job. The full non-integration task suite runs in each job; Linux CI retains live integration coverage. `efcpt-compat.yml` separately executes upstream tool versions. |
| API and property stability | [PR #210](https://github.com/JerrettDavis/JD.Efcpt.Build/pull/210); `build/PublicApi.props` imports PublicApiAnalyzers and checked-in `PublicAPI.Shipped.txt` baselines, including `RunEfcpt`. Builds treat analyzer warnings as errors. `EfcptConfigGeneratorTests` exercises the checked-in upstream schema. The review below records the public configuration contract. |
| Documentation | User guides cover provider installation, configuration, acquisition, offline mode, custom providers, connection-string sources, diagnostics, CI and IDEs. `FINGERPRINTING.md` describes both DACPAC and schema fingerprinting. This closeout corrects provider alias, incremental generation, and CI-disable examples. DocFX runs on PRs and deploys from main. |
| Seven provider samples | `samples/provider-{mssql,postgres,mysql,oracle,snowflake,firebird,sqlite}` each contains a `.csproj`, config, and committed reference output. `samples-build` compiles every reference with generation disabled. Snowflake explicitly documents entities-only output. |
| SDK probe cache | [PR #193](https://github.com/JerrettDavis/JD.Efcpt.Build/pull/193), [exception-safety follow-up #195](https://github.com/JerrettDavis/JD.Efcpt.Build/pull/195); `SdkProbeCacheTests` covers reuse and failure recovery. |
| .NET 8/9 acquisition | [PR #198](https://github.com/JerrettDavis/JD.Efcpt.Build/pull/198); `RunEfcptAutoAcquireTests`, `EfcptDoctorTests`, and the tool-acquisition guide cover fallback modes and diagnostics. |
| Offline mode | [PR #197](https://github.com/JerrettDavis/JD.Efcpt.Build/pull/197); `OfflineModeTests` and `RunEfcptAutoAcquireTests` cover avoiding acquisition/network probes and requiring preinstalled tools. |
| Listing submissions | [awesome-dotnet #1514](https://github.com/quozd/awesome-dotnet/pull/1514) and [awesome-entity-framework-core #8](https://github.com/zzzprojects/awesome-entity-framework-core/pull/8). The requirement is submission; upstream acceptance is controlled by those maintainers. |

## API and configuration review

- Preserve the existing `RunEfcpt` task surface and its checked-in API baseline. No public task/property rename is part of this closeout. New public surface must update the baseline deliberately; breaking changes require a major version after 1.0.
- Canonical providers remain `mssql`, `postgres`, `mysql`, `oracle`, `snowflake`, `firebird`, and `sqlite`. Aliases in `ProviderNames` remain supported; the documentation now reflects `sqlserver` and `pgsql` correctly.
- Preserve the `Efcpt` prefix for consumer properties. Generation controls include `EfcptEnabled`, `EfcptRunDuringDesignTimeBuild`, `EfcptForceRegenerate`, and `EfcptOfflineMode`. Tool controls include `EfcptToolPath`, `EfcptToolVersion`, `EfcptToolMode`, and `EfcptAutoAcquireTool`. `EfcptTestFramework` is repository test infrastructure, not a shipped consumer property.
- Keep the checked-in `lib/efcpt-config.schema.json` aligned with upstream efcpt; schema changes arrive through reviewed PRs and config-generation tests. The upstream schema is not owned by this package. `EfcptToolPath` pins an exact binary; the current .NET 10 `dnx` path does not honor `EfcptToolVersion`, as documented in the compatibility guide.
- Core, Tasks, provider contracts, CLI, and IDE-core API baselines are enforced on their canonical target frameworks. The runtime matrix supplements that compile-time API check; it does not claim identical Framework/.NET implementation details.

## Merge automation

`Build, package` was required on every PR while `vsix.yml` filtered PR events by path. Unrelated changes therefore could never produce the required check. The workflow now runs for every PR, preserving the VSIX gate. GitHub documents this failure mode under [troubleshooting required status checks](https://docs.github.com/en/pull-requests/how-tos/merge-and-close-pull-requests/troubleshooting-required-status-checks).

Repository-native auto-merge is enabled. Opt in on a reviewed PR with `gh pr merge --auto --squash`; GitHub waits for the required checks. This closeout uses that path to verify merging without an admin bypass and then checks the resulting main workflows. When renaming or filtering workflows, check required contexts against actual PR checks to avoid another indefinitely expected status.

## Separately tracked roadmap

The final dependency audit also found a blocked Dependabot security update in the VS Code test toolchain: Mocha's `serialize-javascript` range selected a vulnerable release. The extension now overrides that development dependency to `^7.1.1`, and compatible transitive patches resolve the remaining npm audit findings. Remove the override only when the upstream test tools select a fixed version themselves. Extension lint, compilation, unit/integration tests, packaging, and `npm audit` verify this maintenance path.

The issue's post-v1.0 items remain non-blocking: CLI (#181), Visual Studio (#182), VS Code (#183), custom providers (#184), secret sources (#188), split drivers (#189), and worktree test compatibility (#190). Their merged implementations do not replace any of the required gates above.
