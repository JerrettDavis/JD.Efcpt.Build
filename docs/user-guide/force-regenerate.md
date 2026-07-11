# Forcing Regeneration (`EfcptForceRegenerate`)

By default, `EfcptGenerateModels` only regenerates code when it needs to: when the computed
schema/config fingerprint changed, or when the stamp file (`EfcptStampFile`) is missing (see
[Fingerprinting](../architecture/FINGERPRINTING.md)). That incremental gate is what makes repeat
builds fast, but it also means there was previously no first-class, stable way to say "regenerate
right now, regardless of the cache" - the only options were manually deleting
`obj/efcpt/fingerprint.txt` / `obj/efcpt/.efcpt.stamp`, or `dotnet clean`, both of which reach
into an intermediate-output implementation detail that isn't a supported contract.

`EfcptForceRegenerate` is that first-class contract.

## Usage

```bash
dotnet build -p:EfcptForceRegenerate=true
```

or from MSBuild:

```xml
<PropertyGroup>
  <EfcptForceRegenerate>true</EfcptForceRegenerate>
</PropertyGroup>
```

Default: `false`. When unset or `false`, generation behaves exactly as it does today -
incremental, fingerprint-gated. Setting `EfcptForceRegenerate=true` bypasses the
fingerprint/incremental cache **for that one build** and always re-runs `EfcptGenerateModels`,
even if nothing changed. It is not a persistent setting the pipeline remembers; pass it again on
the next build if you need another forced regeneration.

## Why this exists

This property is the supported integration point for tooling that needs to trigger regeneration
programmatically - most notably IDE extensions (Visual Studio, VS Code) and CLI wrappers such as
`jd-efcpt` - without depending on `obj/`-relative file paths that are an implementation detail and
could change. "Right-click → Regenerate Models" (or the CLI equivalent) is expected to invoke a
build with `EfcptForceRegenerate=true` set, rather than deleting cache files by hand.

## How it works

`EfcptGenerateModels` is gated by both a `Condition` and MSBuild `Inputs`/`Outputs` incremental
evaluation:

```xml
<Target Name="EfcptGenerateModels"
        BeforeTargets="CoreCompile"
        DependsOnTargets="BeforeEfcptGeneration"
        Inputs="$(_EfcptDacpacPath);$(_EfcptStagedConfig);$(_EfcptStagedRenaming)"
        Outputs="$(EfcptStampFile)"
        Condition="'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true' and
                   ('$(_EfcptFingerprintChanged)' == 'true' or !Exists('$(EfcptStampFile)') or
                    '$(EfcptForceRegenerate)' == 'true')">
```

Adding `'$(EfcptForceRegenerate)' == 'true'` to the `Condition` is necessary but not sufficient:
even with a true `Condition`, MSBuild's `Inputs`/`Outputs` comparison can still decide the target's
outputs are up to date and skip its tasks. To make forcing genuinely force a re-run, a small
internal target, `_EfcptForceRegenerateInvalidateStamp`, is hooked in with
`BeforeTargets="EfcptGenerateModels"` and - only when `EfcptForceRegenerate=true` - deletes the
stamp file before `EfcptGenerateModels`'s own up-to-date check runs:

```xml
<Target Name="_EfcptForceRegenerateInvalidateStamp" BeforeTargets="EfcptGenerateModels"
        Condition="'$(EfcptEnabled)' == 'true' and '$(_EfcptIsSqlProject)' != 'true' and
                   '$(EfcptForceRegenerate)' == 'true'">
  <Delete Condition="Exists('$(EfcptStampFile)')" Files="$(EfcptStampFile)" />
</Target>
```

`BeforeTargets`-hooked targets run after their target's `DependsOnTargets` chain but before the
target's own `Inputs`/`Outputs` evaluation, so by the time MSBuild checks whether
`EfcptGenerateModels`'s declared `Outputs` (`$(EfcptStampFile)`) are up to date, the stamp file no
longer exists - the outputs are missing, so the target is never considered up to date. This is the
same mechanism MSBuild itself uses for "clean and rebuild" semantics, applied surgically to one
target for one build.

## What it does *not* do

- It does not disable fingerprint computation - `EfcptComputeFingerprint` still runs; the
  fingerprint file is rewritten only if the computed value actually changed (when it's unchanged,
  the existing correct value is left in place, not overwritten). Either way the *next* build
  (without `EfcptForceRegenerate`) is correctly incremental again.
- It does not affect `EfcptDetectGeneratedFileChanges`, `EfcptOfflineMode`, or any other cache/gate
  outside the fingerprint+stamp pair.
- It is scoped to `EfcptGenerateModels`; it does not force-rebuild the referenced `.sqlproj` or
  re-extract a SQL-project schema.

## See also

- [Fingerprinting](../architecture/FINGERPRINTING.md) - how the fingerprint/stamp incremental gate
  works day-to-day.
