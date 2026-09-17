# v1.0 readiness closeout plan

> Execute inline with the executing-plans workflow. Preserve existing required checks and verify completion against issue #191.

**Goal:** Finish every required readiness item, land green on main, and demonstrate automatic merging without bypassing checks.

**Architecture:** Retain the existing integration suite, API baselines, and provider samples. Extend the task test project to select its runtime, run all seven providers on .NET 8/9/10 across Windows/Linux/macOS, and retain machine-readable results. Make the required VSIX check unconditional for PRs and verify repository-native auto-merge.

**Tech stack:** .NET, xUnit, MSBuild, GitHub Actions, DocFX.

**Specification:** https://github.com/JerrettDavis/JD.Efcpt.Build/issues/191

## Tasks and verification

- [x] Audit the issue, merged PRs, source, CI history, repository merge settings, and required checks. The stale closure is not completion evidence.
- [ ] Expand runtime testing in `tests/JD.Efcpt.Build.Tests` and its test provider fixture using `EfcptTestFramework`. Run the non-integration suite on net8.0/net9.0/net10.0. Add `.github/workflows/provider-compat.yml` with three OSes and three runtimes; assert passing connection and schema-reader tests for all seven providers in every TRX report.
- [ ] Remove PR path filtering from `.github/workflows/vsix.yml` so required `Build, package` is always reported. Verify an actual PR automatically merges after checks succeed with all required gates in force.
- [ ] Review API/config stability, design-time guard, SDK cache, acquisition, offline behavior, all seven samples, and docs. Correct documentation drift and record concrete evidence in `docs/architecture/V1-READINESS.md`.
- [ ] Submit project listings to awesome-dotnet and awesome-entity-framework-core following their contribution guidelines; record submission URLs. Acceptance requires submission, not upstream approval.
- [ ] Run local tests and workflow validation. Open a PR, enable auto-merge, fix any failing checks, and verify merged main CI, documentation, packaging, and compatibility runs.
- [ ] Update issue #191 with an evidence-backed checked checklist and close as completed. Preserve the separately tracked post-v1.0 roadmap.

## Coverage boundaries

The 63 runtime/provider/OS combinations exercise real driver loading, connection construction, and schema-reader behavior without live servers. Existing Linux integration tests cover container databases; Snowflake live integration requires a configured account. Do not describe constructor/unit coverage as live end-to-end database coverage. Preserve the weekly upstream efcpt version workflow as a separate tool-compatibility gate.
