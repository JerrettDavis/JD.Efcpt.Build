# Build Scripts

This directory contains build-time scripts and tools used during the CI/CD process.

## replace-version.ps1

PowerShell script that replaces version placeholders in documentation files with actual version numbers from GitVersion.

### Purpose

Ensures that all documentation (README, docs, samples) shows the current package version without requiring manual updates. This prevents version drift and user confusion.

### Usage

```powershell
# Dry run - shows what would be replaced without making changes
./replace-version.ps1 -Version "1.2.3" -DryRun

# Replace versions in current directory
./replace-version.ps1 -Version "1.2.3"

# Replace versions in specific path
./replace-version.ps1 -Version "1.2.3" -Path "../docs"
```

### Parameters

- **Version** (required): The version string to use for replacement (e.g., "1.2.3")
- **Path** (optional): The root path to search for files (defaults to current directory)
- **DryRun** (optional): If specified, shows what would be replaced without making changes

### Placeholders

The script recognizes and replaces the following patterns:

1. **SDK version in Sdk attribute**: `Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION"`
2. **PackageReference Version attribute**: `Version="PACKAGE_VERSION"`
3. **Inline text placeholder**: `PACKAGE_VERSION` (word boundary)

### CI/CD Integration

This script is automatically executed during the release build in the CI/CD workflow:

1. GitVersion calculates the version based on commits and tags
2. The version is stored in `PACKAGE_VERSION` environment variable
3. `replace-version.ps1` is executed to update all documentation
4. The build continues with the updated documentation
5. NuGet packages are created with the correct version in all docs

### Testing

```bash
# Test in dry run mode
pwsh ./build/replace-version.ps1 -Version "1.2.3" -DryRun

# Test actual replacement (remember to revert after)
pwsh ./build/replace-version.ps1 -Version "1.2.3"

# Revert test changes
git checkout README.md docs/ samples/
```

### Adding Version Placeholders

When adding new documentation:

1. Use `PACKAGE_VERSION` instead of hardcoded version numbers
2. Place the placeholder where users would see version numbers
3. Test with the script to ensure replacement works correctly

**Example:**

```xml
<!-- Good - uses placeholder -->
<Project Sdk="JD.Efcpt.Sdk/PACKAGE_VERSION">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="PACKAGE_VERSION" />
  </ItemGroup>
</Project>

<!-- Bad - hardcoded version -->
<Project Sdk="JD.Efcpt.Sdk/1.0.0">
  <ItemGroup>
    <PackageReference Include="JD.Efcpt.Build" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Notes

- The script only processes markdown (.md) files
- Files in `.git` and `node_modules` directories are excluded
- All replacements use regex for precise pattern matching
- The script preserves file encoding and line endings
