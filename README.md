# JD.Efcpt.Build

MSBuild integration for the EF Core Power Tools CLI (`efcpt`).

`JD.Efcpt.Build` turns EF Core Power Tools into a repeatable, CI-friendly build step: it compiles your SQL Server Database Project (`.sqlproj`) to a DACPAC, stages EF Core Power Tools configuration, runs `efcpt`, and adds the generated EF Core model code to your application project during `dotnet build`.

This repository contains two projects:

- `JD.Efcpt.Build.Tasks` – MSBuild tasks assembly with the implementation of the pipeline.
- `JD.Efcpt.Build` – NuGet packaging project that ships `.props`/`.targets` files and default configuration assets.

The package is designed to be thin MSBuild XML on top of real, test-covered C# code.

---

## 1. When to use this package

Use `JD.Efcpt.Build` when:

- You have a SQL Server database described by a Database Project (`.sqlproj`) and want EF Core DbContext and entity classes generated from it.
- You want EF Core Power Tools generation to run as part of `dotnet build` instead of being a manual step in Visual Studio.
- You need deterministic, source-controlled model generation that works the same way on developer machines and in CI/CD.

The package focuses on database-first modeling using EF Core Power Tools CLI (`ErikEJ.EFCorePowerTools.Cli`).

---

## 2. Installation

### 2.1 Add the NuGet package

Add a package reference to your application project (the project that should contain the generated DbContext and entity classes):

```xml
<ItemGroup>
  <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
</ItemGroup>
```

Or enable it solution-wide via `Directory.Build.props`:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
  </ItemGroup>
</Project>
```

### 2.2 Install EF Core Power Tools CLI

`JD.Efcpt.Build` drives the EF Core Power Tools CLI (`efcpt`). You must ensure the CLI is available on all machines that run your build.

Global tool example:

```powershell
# PowerShell
 dotnet tool install -g ErikEJ.EFCorePowerTools.Cli
```

Local tool (recommended for shared/CI environments):

```powershell
# From your solution root
 dotnet new tool-manifest
 dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
```

By default the build uses `dotnet tool run efcpt` when a local tool manifest is present, or falls back to running `efcpt` directly when it is globally installed. These behaviors can be controlled using the properties described later.

### 2.3 Prerequisites

- .NET SDK 8.0 or newer.
- EF Core Power Tools CLI installed as a .NET tool (global or local).
- A SQL Server Database Project (`.sqlproj`) that can be built to a DACPAC. On build agents this usually requires the appropriate SQL Server Data Tools / build tools components.

---

## 3. High-level architecture

`JD.Efcpt.Build` wires a set of MSBuild targets into your project. When `EfcptEnabled` is `true` (the default), the following pipeline runs as part of `dotnet build`:

1. **EfcptResolveInputs** – locates the `.sqlproj` and resolves configuration inputs.
2. **EfcptEnsureDacpac** – builds the database project to a DACPAC if needed.
3. **EfcptStageInputs** – stages the EF Core Power Tools configuration, renaming rules, and templates into an intermediate directory.
4. **EfcptComputeFingerprint** – computes a fingerprint across the DACPAC and staged inputs.
5. **EfcptGenerateModels** – runs `efcpt` and renames generated files to `.g.cs` when the fingerprint changes.
6. **EfcptAddToCompile** – adds the generated `.g.cs` files to the `Compile` item group so they are part of your build.

The underlying targets and tasks live in `build/JD.Efcpt.Build.targets` and `JD.Efcpt.Build.Tasks.dll`.

---

## 4. Minimal usage

### 4.1 Typical solution layout

A common setup looks like this:

- `MyApp.csproj` – application project where you want the EF Core DbContext and entities.
- `Database/Database.sqlproj` – SQL Server Database Project that produces a DACPAC.
- `Directory.Build.props` – optional solution-wide configuration.

### 4.2 Quick start

1. Add `JD.Efcpt.Build` to your application project (or to `Directory.Build.props`).
2. Ensure a `.sqlproj` exists somewhere in the solution that builds to a DACPAC.
3. Optionally copy the default `efcpt-config.json` from the package (see below) into your application project to customize namespaces and options.
4. Run:

```powershell
 dotnet build
```

On the first run the build will:

- Build the `.sqlproj` to a DACPAC.
- Stage EF Core Power Tools configuration.
- Run `efcpt` to generate DbContext and entity types.
- Place generated code under the directory specified by `EfcptGeneratedDir` (by default under `obj/efcpt/Generated` in the sample tests).

Subsequent builds will only re-run `efcpt` when the DACPAC or staged configuration changes.

---

## 5. Configuration via MSBuild properties

The behavior of the pipeline is controlled by a set of MSBuild properties. You can define these in your project file or in `Directory.Build.props`.

### 5.1 Core properties

- `EfcptEnabled` (default: `true`)
  - Master on/off switch for the entire pipeline.

- `EfcptOutput`
  - Intermediate directory used to stage configuration and compute fingerprints.
  - If not set, a reasonable default is chosen relative to the project.

- `EfcptGeneratedDir`
  - Directory where generated C# files are written.
  - Used by `EfcptGenerateModels` and `EfcptAddToCompile`.

- `EfcptSqlProj`
  - Optional override for the path to the Database Project (`.sqlproj`).
  - When not set, `ResolveSqlProjAndInputs` attempts to discover the project based on project references and solution layout.

- `EfcptConfig`
  - Optional override for the EF Core Power Tools configuration file (defaults to `efcpt-config.json` in the project directory when present).

- `EfcptRenaming`
  - Optional override for the renaming configuration (defaults to `efcpt.renaming.json` in the project directory when present).

- `EfcptTemplateDir`
  - Optional override for the template directory (defaults to `Template` in the project directory when present).

- `EfcptSolutionDir`
  - Root directory used when probing for related projects, if automatic discovery needs help.

- `EfcptProbeSolutionDir`
  - Controls whether solution probing is performed. Use this if your layout is non-standard.

- `EfcptLogVerbosity`
  - Controls task logging (`minimal` or `detailed`).

### 5.2 Tool resolution properties

These properties control how the `RunEfcpt` task finds and invokes the EF Core Power Tools CLI:

- `EfcptToolMode`
  - Controls the strategy used to locate the tool. Common values:
    - `auto` – use a local tool if a manifest is present, otherwise fall back to a global tool.
    - `tool-manifest` – require a local tool manifest and fail if one is not present.

- `EfcptToolPackageId`
  - NuGet package ID for the CLI. Defaults to `ErikEJ.EFCorePowerTools.Cli`.

- `EfcptToolVersion`
  - Requested CLI version or version range (for example, `10.*`).

- `EfcptToolRestore`
  - When `true`, the task may restore or update the tool as part of the build.

- `EfcptToolCommand`
  - The command to execute when running the tool (defaults to `efcpt`).

- `EfcptToolPath`
  - Optional explicit path to the `efcpt` executable. When set, this takes precedence over `dotnet tool run`.

- `EfcptDotNetExe`
  - Optional explicit path to the `dotnet` host used for tool invocations and `.sqlproj` builds.

### 5.3 Fingerprinting and diagnostics

- `EfcptFingerprintFile`
  - Path to the fingerprint file produced by `ComputeFingerprint`.

- `EfcptStampFile`
  - Path to the stamp file written by `EfcptGenerateModels` to record the last successful fingerprint.

- `EfcptDumpResolvedInputs`
  - When `true`, `ResolveSqlProjAndInputs` logs the resolved inputs to help diagnose discovery and configuration issues.

---

## 6. Configuration files and defaults

The NuGet package ships default configuration assets under a `Defaults` folder. These defaults are used when you do not provide your own, and they can be copied into your project and customized.

### 6.1 `efcpt-config.json`

`efcpt-config.json` is the main configuration file for EF Core Power Tools. The version shipped by this package sets sensible defaults for code generation, including:

- Enabling nullable reference types.
- Enabling `DateOnly`/`TimeOnly` where appropriate.
- Controlling which schemas and tables are included.
- Controlling namespaces, DbContext name, and output folder structure.

Typical sections you might customize include:

- `code-generation` – toggles for features such as data annotations, T4 usage, or using `DbContextFactory`.
- `names` – default namespace, DbContext name, and related name settings.
- `file-layout` – where files are written relative to the project and how they are grouped.
- `replacements` and `type-mappings` – table/column renaming rules and type overrides.

You can start with the default `efcpt-config.json` from the package and adjust these sections to match your conventions.

### 6.2 `efcpt.renaming.json`

`efcpt.renaming.json` is an optional JSON file that contains additional renaming rules for database objects and generated code. Use it to:

- Apply custom naming conventions beyond those specified in `efcpt-config.json`.
- Normalize table, view, or schema names.

If a project-level `efcpt.renaming.json` is present, it will be preferred over the default shipped with the package.

### 6.3 Template folder

The package also ships a `Template` folder containing template files used by EF Core Power Tools when T4-based generation is enabled.

If you need to customize templates:

1. Copy the `Template` folder from the package into your project or a shared location.
2. Update `EfcptTemplateDir` (or the corresponding setting in `efcpt-config.json`) to point to your customized templates.

During a build, the `StageEfcptInputs` task stages the effective config, renaming file, and template folder into `EfcptOutput` before running `efcpt`.

---

## 7. Examples

### 7.1 Basic project-level configuration

Application project (`MyApp.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
  </ItemGroup>

  <!-- Optional: point directly at a specific .sqlproj -->
  <PropertyGroup>
    <EfcptSqlProj>..\Database\Database.sqlproj</EfcptSqlProj>
  </PropertyGroup>
</Project>
```

Place `efcpt-config.json` and (optionally) `efcpt.renaming.json` in the same directory as `MyApp.csproj`, then run `dotnet build`. Generated DbContext and entities are automatically included in the compilation.

### 7.2 Solution-wide configuration via `Directory.Build.props`

To enable the pipeline across multiple application projects, you can centralize configuration in `Directory.Build.props` at the solution root:

```xml
<Project>
  <PropertyGroup>
    <!-- Enable EF Core Power Tools generation for all projects by default -->
    <EfcptEnabled>true</EfcptEnabled>

    <!-- Use a consistent intermediate and output layout across the solution -->
    <EfcptOutput>$(MSBuildProjectDirectory)\obj\efcpt\</EfcptOutput>
    <EfcptGeneratedDir>$(MSBuildProjectDirectory)\obj\efcpt\Generated\</EfcptGeneratedDir>

    <!-- Prefer local dotnet tool manifests for the CLI -->
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolPackageId>ErikEJ.EFCorePowerTools.Cli</EfcptToolPackageId>
    <EfcptToolVersion>10.*</EfcptToolVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="x.y.z" />
  </ItemGroup>
</Project>
```

Individual projects can then override `EfcptSqlProj`, `EfcptConfig`, or other properties when they diverge from the solution defaults.

### 7.3 CI / build pipeline integration

No special steps are required beyond installing the prerequisites. A typical CI job includes:

```powershell
# Restore tools (if using a local manifest)
 dotnet tool restore

# Restore and build the solution
 dotnet restore
 dotnet build --configuration Release
```

On each run the EF Core models are regenerated only when the DACPAC or EF Core Power Tools inputs change.

Ensure that the build agent has the necessary SQL Server Data Tools components to build the `.sqlproj` to a DACPAC.

---

## 8. Troubleshooting

### 8.1 Generated models do not appear

- Confirm that `EfcptEnabled` is `true` for the project.
- Verify that the `.sqlproj` can be built independently (for example, by opening it in Visual Studio or running `dotnet msbuild` directly).
- If discovery fails, set `EfcptSqlProj` explicitly to the full path of the `.sqlproj`.
- Increase logging verbosity by setting `EfcptLogVerbosity` to `detailed` and inspect the build output.
- Check that `EfcptGeneratedDir` exists after the build and that it contains `.g.cs` files.

### 8.2 DACPAC build problems

- Ensure that either `msbuild.exe` (Windows) or `dotnet msbuild` is available.
- Install the SQL Server Data Tools / database build components on the machine running the build.
- Review the detailed build log from the `EnsureDacpacBuilt` task for underlying MSBuild errors.

### 8.3 `efcpt` CLI issues

- Run `dotnet tool list -g` or `dotnet tool list` (with a manifest) to confirm that `ErikEJ.EFCorePowerTools.Cli` is installed.
- If using a local tool manifest, set `EfcptToolMode` to `tool-manifest` to enforce its use.
- If needed, provide an explicit `EfcptToolPath` to the `efcpt` executable.
- Make sure the CLI version requested by `EfcptToolVersion` is compatible with your EF Core version.

### 8.4 Inspecting inputs and intermediate outputs

- Set `EfcptDumpResolvedInputs` to `true` to log how the `.sqlproj`, config, renaming file, and templates are resolved.
- Inspect the directory specified by `EfcptOutput` to see:
  - The staged `efcpt-config.json`.
  - The staged `efcpt.renaming.json`.
  - The staged `Template` folder used by EF Core Power Tools.
  - The fingerprint and stamp files that control incremental generation.

### 8.5 Test-only environment variables

This repository’s own tests use a few environment variables to simulate external tools and speed up test runs:

- `EFCPT_FAKE_BUILD` – simulates building the DACPAC without invoking a real database build.
- `EFCPT_FAKE_EFCPT` – simulates the `efcpt` CLI and writes deterministic sample output.
- `EFCPT_TEST_DACPAC` – points tests at a specific DACPAC.

These variables are intended for internal tests and should not be used in production builds.

---

## 9. Development and testing

To run the repository’s test suite:

```powershell
 dotnet test
```

The tests include end-to-end coverage that:

- Builds a real SQL Server Database Project from `tests/TestAssets/SampleDatabase` to a DACPAC.
- Runs the EF Core Power Tools CLI through the `JD.Efcpt.Build` MSBuild tasks.
- Generates EF Core model code into a sample application under `obj/efcpt/Generated`.
- Verifies that the generated models contain DbSets and entities for multiple schemas and tables.

---

## 10. Support and feedback

For issues, questions, or feature requests, please open an issue in the Git repository where this project is hosted. Include relevant information such as:

- A short description of the problem.
- The `dotnet --info` output.
- The versions of `JD.Efcpt.Build` and `ErikEJ.EFCorePowerTools.Cli` you are using.
- Relevant sections of the MSBuild log with `EfcptLogVerbosity` set to `detailed`.

`JD.Efcpt.Build` is intended to be suitable for enterprise and FOSS usage. Contributions in the form of bug reports, documentation improvements, and pull requests are welcome, subject to the project’s contribution guidelines and license.
