# Error Codes Reference

This page documents all error codes that can be emitted by JD.Efcpt.Build tasks.

## Error Code Format

Error codes follow the format `JDxxxx` where `xxxx` is a four-digit number.

## Configuration and Connection String Errors (JD0001-JD0019)

### JD0001: Configuration File Type Warning
**Severity**: Warning
**Task**: ConfigurationFileTypeValidator

The configuration file type doesn't match the expected format.

**Example**:
```
warning JD0001: Configuration file 'appsettings.json' should use .json extension for JSON files
```

**Resolution**: Rename the configuration file to use the correct extension, or verify you're using the correct configuration file type.

---

### JD0002: Connection String Missing
**Severity**: Warning
**Task**: AppConfigConnectionStringParser, AppSettingsConnectionStringParser

The specified connection string name was not found in the configuration file.

**Example**:
```
warning JD0002: Connection string 'MyDatabase' not found in appsettings.json
```

**Resolution**: Add the connection string to your configuration file, or verify the connection string name is correct.

---

### JD0003: Connection String Resolution Warning
**Severity**: Warning
**Task**: ConnectionStringResolutionChain

General warning during connection string resolution process.

**Example**:
```
warning JD0003: Failed to resolve connection string from configuration
```

**Resolution**: Check that your configuration file exists and contains valid connection string settings.

---

### JD0011: Configuration File Parse/Read Error
**Severity**: Error
**Task**: AppConfigConnectionStringParser, AppSettingsConnectionStringParser

Failed to parse or read the configuration file.

**Example**:
```
error JD0011: Failed to parse configuration file 'appsettings.json': Unexpected character encountered
```

**Resolution**: Verify the configuration file is valid JSON/XML and not corrupted.

---

### JD0012: Connection String Null or Empty
**Severity**: Error
**Task**: AppSettingsConnectionStringParser

The connection string exists but is null or empty.

**Example**:
```
error JD0012: Connection string 'DefaultConnection' in appsettings.json is null or empty
```

**Resolution**: Provide a valid connection string value in your configuration file.

---

### JD0013: Query Schema Metadata Error (Specific)
**Severity**: Error
**Task**: QuerySchemaMetadata

Specific error when querying database schema metadata.

**Resolution**: Check database connectivity and permissions. Verify the database exists and is accessible.

---

### JD0014: Query Schema Metadata Error (General)
**Severity**: Error
**Task**: QuerySchemaMetadata

General exception when querying database schema metadata.

**Example**:
```
error JD0014: Failed to query database schema metadata: Timeout expired
```

**Resolution**: Check database connectivity, credentials, and network access.

---

### JD0015: SQL Project Warning
**Severity**: Warning
**Task**: ResolveSqlProjAndInputs

Warning related to SQL project resolution.

**Resolution**: Review the warning message for specific guidance.

---

### JD0016: Explicit Connection String Fallback Warning
**Severity**: Warning
**Task**: ResolveSqlProjAndInputs

Explicit connection string configuration provided but failed to resolve, falling back to .sqlproj detection.

**Resolution**: Verify your explicit connection string configuration is correct.

---

## SqlPackage and SQL Generation Errors (JD0020-JD0029)

### JD0020: Explicit Tool Path Does Not Exist
**Severity**: Error
**Task**: RunSqlPackage

The explicitly specified sqlpackage tool path does not exist.

**Example**:
```
error JD0020: Explicit tool path does not exist: C:\tools\sqlpackage.exe
```

**Resolution**:
- Verify the path to sqlpackage.exe is correct
- Install sqlpackage using `dotnet tool install --global microsoft.sqlpackage`
- Remove the `ToolPath` property to use automatic tool resolution

---

### JD0021: Failed to Start SqlPackage Process
**Severity**: Error
**Task**: RunSqlPackage

Failed to start the sqlpackage process.

**Example**:
```
error JD0021: Failed to start sqlpackage process
```

**Resolution**:
- Verify sqlpackage is installed: `dotnet tool list --global`
- Install if missing: `dotnet tool install --global microsoft.sqlpackage`
- Check system PATH includes the dotnet tools directory

---

### JD0022: SqlPackage Failed with Exit Code
**Severity**: Error
**Task**: RunSqlPackage

SqlPackage exited with a non-zero exit code, indicating an error during execution.

**Example**:
```
error JD0022: SqlPackage failed with exit code 1
```

**Resolution**:
- Check the detailed output for specific sqlpackage errors
- Verify the connection string is correct and the database is accessible
- Ensure you have sufficient permissions to read the database schema
- Check network connectivity to the database server

**Common Causes**:
- Invalid connection string
- Database does not exist
- Insufficient permissions
- Network connectivity issues
- Database server is offline

---

### JD0023: SqlPackage Execution Failed (Exception)
**Severity**: Error
**Task**: RunSqlPackage

An unexpected exception occurred while executing sqlpackage.

**Example**:
```
error JD0023: SqlPackage execution failed: Access to the path 'C:\output' is denied
```

**Resolution**:
- Check the exception details in the build output
- Verify file system permissions
- Ensure target directories are writable
- Check for disk space issues

---

### JD0024: Failed to Create Target Directory
**Severity**: Error
**Task**: RunSqlPackage

Failed to create the target directory for SQL script extraction.

**Example**:
```
error JD0024: Failed to create target directory 'C:\output\scripts': Access denied
```

**Resolution**:
- Verify you have write permissions to the parent directory
- Check if the path contains invalid characters
- Ensure the disk has sufficient space
- Check if the path length exceeds system limits (260 characters on Windows)

---

### JD0025: Failed to Add SQL File Warnings
**Severity**: Error
**Task**: AddSqlFileWarnings

An exception occurred while adding auto-generation warning headers to SQL files.

**Example**:
```
error JD0025: Failed to add SQL file warnings: Access to the path is denied
```

**Resolution**:
- Verify file system permissions on the SQL scripts directory
- Ensure SQL files are not read-only or locked by another process
- Check disk space
- Verify the scripts directory path is valid

---

### JD0026: Efcpt Tool Not Available Offline
**Severity**: Error
**Task**: RunEfcpt

`EfcptOfflineMode` (or the `EFCPT_OFFLINE` environment variable) is enabled, but the efcpt tool
is not guaranteed to run without a network call - no explicit, existing `EfcptToolPath` was
provided, no tool manifest was discovered, and no global tool was found on `PATH`. Offline mode
never spawns `dnx`, restores a tool manifest, or updates a global tool, since all three require
network access.

**Example**:
```
error JD0026: EfcptOfflineMode is enabled, but the efcpt tool is not guaranteed to run without a
network call. Offline mode will not spawn dnx, restore a tool manifest, or update a global tool,
since all three require network access. Pre-provision the tool before building offline using one
of the following: (1) a local tool manifest - run: dotnet new tool-manifest && dotnet tool
install ErikEJ.EFCorePowerTools.Cli --version 10.*; (2) a global tool - run: dotnet tool install
--global ErikEJ.EFCorePowerTools.Cli --version 10.*; or (3) set EfcptToolPath to an explicit,
pre-installed efcpt executable. ...
```

**Resolution**:
- Pre-provision a local tool manifest and restore it before the offline build runs: `dotnet new
  tool-manifest && dotnet tool install ErikEJ.EFCorePowerTools.Cli --version 10.*`
- Or install the tool as a global tool ahead of time: `dotnet tool install --global
  ErikEJ.EFCorePowerTools.Cli --version 10.*`
- Or set `EfcptToolPath` to an explicit, pre-installed efcpt executable
- See [offline.md](offline.md) for the full offline/air-gapped build workflow

---

### JD0027: Tool Auto-Acquisition Failed
**Severity**: Error
**Task**: RunEfcpt

`EfcptAutoAcquireTool` (default `true`) attempted to bootstrap an obj-local tool manifest and
install the efcpt tool into it - because no hermetic, network-free way to run the tool was
otherwise available (typically: targeting .NET 8/9, where `dnx` is not usable, with no explicit
`EfcptToolPath`, no already-usable tool manifest, and no global tool already resolvable on
`PATH`) - but the underlying `dotnet new tool-manifest` / `dotnet tool install` step failed.

**Example**:
```
error JD0027: EfcptAutoAcquireTool attempted to bootstrap a local dotnet tool manifest and
install 'ErikEJ.EFCorePowerTools.Cli --version 10.*' into it, but the acquisition step failed.
Attempted manifest directory: 'C:\repo\src\MyProject\obj\efcpt'. Details: dotnet tool install
ErikEJ.EFCorePowerTools.Cli --version 10.* exited with code 1. ... Fix options: (1) install the
tool globally ahead of time - run: dotnet tool install --global ErikEJ.EFCorePowerTools.Cli
--version 10.*; (2) commit a pre-restored tool manifest to source control ... and set
EfcptAutoAcquireTool=false to use it as-is; or (3) enable EfcptOfflineMode and pre-provision the
tool via one of the above before building offline. ...
```

**Resolution**:
- Check the captured `dotnet tool install` output in the error message for the underlying cause
  (network access, NuGet feed configuration, a bad `EfcptToolVersion` constraint, etc.)
- Install the tool as a global tool ahead of time: `dotnet tool install --global
  ErikEJ.EFCorePowerTools.Cli --version 10.*`
- Or commit a pre-restored tool manifest to source control and set `EfcptAutoAcquireTool=false`
  so the build uses it as-is instead of trying to bootstrap a new one
- Or enable `EfcptOfflineMode` and pre-provision the tool via one of the above before building
  offline (offline mode never attempts auto-acquisition - see JD0026)
- Run `dotnet build -t:EfcptDoctor` for a diagnostic report of which execution path would be
  used and why
- See [tool-acquisition.md](tool-acquisition.md) for the full auto-acquisition workflow

---

### JD0028: Tool Manifest Resolution Not Configured
**Severity**: Error
**Task**: RunEfcpt

Tool resolution would use a local dotnet tool manifest for the efcpt tool (`EfcptToolMode`
resolves to `tool-manifest`, or `auto` with a discovered manifest directory), but that manifest is
either absent or does not list the efcpt tool, and `EfcptAutoAcquireTool` is disabled (or cannot
run because no `EfcptToolPackageId` is configured) - so the build stops with an actionable error
instead of falling through to a guaranteed-failing `dotnet tool run` invocation.

**Example**:
```
error JD0028: Tool resolution would use a local dotnet tool manifest for the efcpt tool, but a
tool manifest was discovered at 'C:\repo\src\MyProject\obj\efcpt', but it does not list
'ErikEJ.EFCorePowerTools.Cli'. EfcptAutoAcquireTool is 'false' (disabled, or no
EfcptToolPackageId was configured to install), so it cannot bootstrap or complete the manifest
automatically. Proceeding would guarantee a failing 'dotnet tool run' invocation, so the build is
stopping now instead. Fix options: (1) run: dotnet tool install ErikEJ.EFCorePowerTools.Cli
--version 10.* in 'C:\repo\src\MyProject\obj\efcpt' (or dotnet new tool-manifest && dotnet tool
install ErikEJ.EFCorePowerTools.Cli --version 10.* if no manifest exists yet there); (2) set
EfcptAutoAcquireTool=true so the build bootstraps/installs it automatically; or (3) set an
explicit EfcptToolPath to a pre-installed efcpt executable. ...
```

**Resolution**:
- Install the tool into the existing (or a new) manifest: `dotnet tool install
  ErikEJ.EFCorePowerTools.Cli --version 10.*` in the manifest directory shown in the error (or
  `dotnet new tool-manifest && dotnet tool install ErikEJ.EFCorePowerTools.Cli --version 10.*` if
  no manifest exists there yet)
- Or set `EfcptAutoAcquireTool=true` so the build bootstraps/installs it automatically at build
  time (see JD0027 if that install itself then fails)
- Or set `EfcptToolPath` to an explicit, pre-installed efcpt executable
- Run `dotnet build -t:EfcptDoctor` for a diagnostic report of which execution path would be used
  and why
- See [tool-acquisition.md](tool-acquisition.md) for the full auto-acquisition workflow

---

## Troubleshooting Tips

### General Troubleshooting Steps

1. **Check Build Verbosity**: Increase MSBuild verbosity to get more details
   ```
   dotnet build -v:detailed
   ```

2. **Enable Efcpt Logging**: Set `EfcptLogVerbosity` to `detailed` in your project file
   ```xml
   <PropertyGroup>
     <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
   </PropertyGroup>
   ```

3. **Clean and Rebuild**: Sometimes cached state can cause issues
   ```
   dotnet clean
   dotnet build
   ```

4. **Check Permissions**: Ensure you have read/write permissions to all necessary directories

5. **Verify Tool Installation**: For SqlPackage errors, verify the tool is installed
   ```
   dotnet tool list --global
   dotnet tool install --global microsoft.sqlpackage
   ```

### Getting Help

If you encounter an error that isn't documented here or need additional help:

1. Check the [Troubleshooting Guide](troubleshooting.md)
2. Search [existing issues](https://github.com/jerrettdavis/JD.Efcpt.Build/issues)
3. Create a [new issue](https://github.com/jerrettdavis/JD.Efcpt.Build/issues/new) with:
   - The full error message including error code
   - Build output with verbosity set to `detailed`
   - Your project file configuration
   - Environment details (OS, .NET version, etc.)
