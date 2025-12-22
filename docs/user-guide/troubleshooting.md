# Troubleshooting

This guide helps you diagnose and resolve common issues with JD.Efcpt.Build.

## Diagnostic Tools

### Enable Detailed Logging

Add these properties to your `.csproj` for maximum visibility:

```xml
<PropertyGroup>
  <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
  <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
</PropertyGroup>
```

### Inspect Build Output

Run with detailed MSBuild logging:

```bash
dotnet build /v:detailed > build.log 2>&1
```

Search for `JD.Efcpt.Build` entries in the log.

### Check Resolved Inputs

When `EfcptDumpResolvedInputs` is `true`, check `obj/efcpt/resolved-inputs.json`:

```json
{
  "sqlProjPath": "..\\database\\MyDatabase.sqlproj",
  "configPath": "efcpt-config.json",
  "renamingPath": "efcpt.renaming.json",
  "templateDir": "Template",
  "connectionString": null,
  "useConnectionString": false
}
```

## Common Issues

### Generated Files Don't Appear

**Symptoms:**
- No files in `obj/efcpt/Generated/`
- Build succeeds but no DbContext available

**Solutions:**

1. **Verify package is referenced:**
   ```bash
   dotnet list package | findstr JD.Efcpt.Build
   ```

2. **Check if EfcptEnabled is true:**
   ```xml
   <PropertyGroup>
     <EfcptEnabled>true</EfcptEnabled>
   </PropertyGroup>
   ```

3. **Check if database project is found:**
   - Enable `EfcptDumpResolvedInputs`
   - Look for `sqlProjPath` in `resolved-inputs.json`
   - Set `EfcptSqlProj` explicitly if needed

4. **Force regeneration:**
   ```bash
   rmdir /s /q obj\efcpt
   dotnet build
   ```

### Database Project Not Found

**Symptoms:**
- Build warning: "Could not find SQL project"
- `sqlProjPath` is empty in resolved inputs

**Solutions:**

1. **Set path explicitly:**
   ```xml
   <PropertyGroup>
     <EfcptSqlProj>..\database\MyDatabase.sqlproj</EfcptSqlProj>
   </PropertyGroup>
   ```

2. **Add project reference:**
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\database\MyDatabase.sqlproj" />
   </ItemGroup>
   ```

3. **Check solution directory probing:**
   ```xml
   <PropertyGroup>
     <EfcptProbeSolutionDir>true</EfcptProbeSolutionDir>
     <EfcptSolutionDir>$(SolutionDir)</EfcptSolutionDir>
   </PropertyGroup>
   ```

### efcpt CLI Not Found

**Symptoms:**
- Error: "efcpt command not found"
- Error: "dotnet tool run efcpt failed"

**Solutions for .NET 10+:**
- This should not occur on .NET 10+ (uses `dnx`)
- Verify .NET version: `dotnet --version`

**Solutions for .NET 8-9:**

1. **Verify installation:**
   ```bash
   dotnet tool list --global
   dotnet tool list
   ```

2. **Reinstall globally:**
   ```bash
   dotnet tool uninstall -g ErikEJ.EFCorePowerTools.Cli
   dotnet tool install -g ErikEJ.EFCorePowerTools.Cli --version "10.*"
   ```

3. **Use tool manifest:**
   ```bash
   dotnet new tool-manifest
   dotnet tool install ErikEJ.EFCorePowerTools.Cli --version "10.*"
   ```
   ```xml
   <PropertyGroup>
     <EfcptToolMode>tool-manifest</EfcptToolMode>
   </PropertyGroup>
   ```

### DACPAC Build Fails

**Symptoms:**
- Error during `EfcptEnsureDacpac` target
- MSBuild errors related to SQL project

**Solutions:**

1. **Verify SQL project builds independently:**
   ```bash
   dotnet build path\to\Database.sqlproj
   ```

2. **Install SQL Server Data Tools:**
   - On Windows, install Visual Studio with SQL Server Data Tools workload
   - Or install the standalone SSDT

3. **Use pre-built DACPAC:**
   ```xml
   <PropertyGroup>
     <EfcptSqlProj>path\to\MyDatabase.dacpac</EfcptSqlProj>
   </PropertyGroup>
   ```

4. **Check MSBuild/dotnet path:**
   ```xml
   <PropertyGroup>
     <EfcptDotNetExe>C:\Program Files\dotnet\dotnet.exe</EfcptDotNetExe>
   </PropertyGroup>
   ```

### Build Doesn't Detect Schema Changes

**Symptoms:**
- Schema changed but models not regenerated
- Same fingerprint despite changes

**Solutions:**

1. **Delete fingerprint cache:**
   ```bash
   rmdir /s /q obj\efcpt
   dotnet build
   ```

2. **Verify DACPAC was rebuilt:**
   - Check DACPAC file timestamp
   - Ensure SQL project sources are newer

3. **Check fingerprint file:**
   - Look at `obj/efcpt/fingerprint.txt`
   - Compare with expected hash

### Connection String Issues

**Symptoms:**
- "Connection refused" errors
- "Authentication failed" errors
- No tables generated

**Solutions:**

1. **Test connection manually:**
   ```bash
   sqlcmd -S localhost -d MyDb -E -Q "SELECT 1"
   ```

2. **Check connection string format:**
   ```xml
   <EfcptConnectionString>Server=localhost;Database=MyDb;Integrated Security=True;TrustServerCertificate=True;</EfcptConnectionString>
   ```

3. **Verify appsettings.json path:**
   ```xml
   <PropertyGroup>
     <EfcptAppSettings>appsettings.json</EfcptAppSettings>
     <EfcptConnectionStringName>DefaultConnection</EfcptConnectionStringName>
   </PropertyGroup>
   ```

4. **Enable detailed logging to see resolved connection:**
   ```xml
   <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
   ```

### Templates Not Being Used

**Symptoms:**
- Custom templates exist but default output generated
- Template changes not reflected

**Solutions:**

1. **Verify T4 is enabled:**
   ```json
   {
     "code-generation": {
       "use-t4": true,
       "t4-template-path": "."
     }
   }
   ```

2. **Check template location:**
   - Verify `Template/CodeTemplates/EFCore/` structure
   - Check `EfcptDumpResolvedInputs` for resolved path

3. **Force regeneration:**
   ```bash
   rmdir /s /q obj\efcpt
   dotnet build
   ```

### Compilation Errors in Generated Code

**Symptoms:**
- Build errors in `.g.cs` files
- Missing types or namespaces

**Solutions:**

1. **Check EF Core package version compatibility:**
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
   ```

2. **Verify efcpt version matches:**
   ```xml
   <EfcptToolVersion>10.*</EfcptToolVersion>
   ```

3. **Check nullable reference types setting:**
   ```xml
   <Nullable>enable</Nullable>
   ```
   ```json
   {
     "code-generation": {
       "use-nullable-reference-types": true
     }
   }
   ```

### Slow Builds

**Symptoms:**
- Build takes long even without schema changes
- DACPAC rebuilds unnecessarily

**Solutions:**

1. **Preserve fingerprint cache:**
   - Don't delete `obj/efcpt/` between builds
   - Cache in CI/CD pipelines

2. **Use connection string mode:**
   - Skips DACPAC compilation
   - Faster for development

3. **Select specific tables:**
   ```json
   {
     "table-selection": [
       {
         "schema": "dbo",
         "tables": ["Users", "Orders"],
         "include": true
       }
     ]
   }
   ```

### Files Generated in Wrong Location

**Symptoms:**
- Files appear in unexpected directory
- Multiple copies of generated files

**Solutions:**

1. **Check output properties:**
   ```xml
   <PropertyGroup>
     <EfcptOutput>$(BaseIntermediateOutputPath)efcpt\</EfcptOutput>
     <EfcptGeneratedDir>$(EfcptOutput)Generated\</EfcptGeneratedDir>
   </PropertyGroup>
   ```

2. **Verify no conflicting configurations:**
   - Check `Directory.Build.props`
   - Check for inherited properties
   
3. **Check efcpt-config.json T4 Template Path:**
   - Check `"code-generation": { "t4-template-path": "..." }` setting for a correct path. At generation time, it is relative to Generation output directory.

## Error Messages

### "The database provider 'X' is not supported"

Currently only SQL Server (`mssql`) is supported. PostgreSQL, MySQL, and other providers are planned for future releases.

### "Could not find configuration file"

The package couldn't find `efcpt-config.json`. Either:
- Create the file in your project directory
- Set `EfcptConfig` property explicitly
- Use package defaults (no action needed)

### "Fingerprint file not found"

This is normal on first build. The fingerprint is created after successful generation.

### "Failed to query schema metadata"

In connection string mode, the database connection failed. Check:
- Connection string syntax
- Database server availability
- Authentication credentials
- Firewall rules

## Getting Help

If you're still stuck:

1. **Enable full diagnostics:**
   ```xml
   <PropertyGroup>
     <EfcptLogVerbosity>detailed</EfcptLogVerbosity>
     <EfcptDumpResolvedInputs>true</EfcptDumpResolvedInputs>
   </PropertyGroup>
   ```

2. **Capture MSBuild log:**
   ```bash
   dotnet build /v:detailed > build.log 2>&1
   ```

3. **Report an issue** with:
   - .NET version (`dotnet --info`)
   - JD.Efcpt.Build version
   - EF Core Power Tools CLI version
   - Relevant MSBuild log sections
   - Contents of `resolved-inputs.json`

## Next Steps

- [Configuration](configuration.md) - Review all configuration options
- [API Reference](api-reference.md) - Complete MSBuild task reference
- [CI/CD Integration](ci-cd.md) - Pipeline-specific troubleshooting
