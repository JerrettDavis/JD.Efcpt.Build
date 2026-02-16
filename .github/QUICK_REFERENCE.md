# PatternKit CI/CD Quick Reference

## 🔄 Development Workflow

### 1️⃣ Create Feature Branch

```bash
git checkout -b feature/my-feature
```

### 2️⃣ Make Changes

```bash
# Write code, add tests
git add .
git commit -m "feat: add my feature"
```

### 3️⃣ Push and Create PR

```bash
git push origin feature/my-feature
# Create PR on GitHub
# Wait for validation 
```

### 4️⃣ Merge to Main

```bash
# Use "Squash and Merge"
# Automatic release triggered on merge
```

## Automated Workflows

### On Pull Request

```
PR Created/Updated
 → Dependency Review (checks for security issues)
 → PR Validation (build, test, package)
 → Auto Label (labels based on files changed)
 → CodeQL Analysis (security scanning)
 → Post summary comment
 → Upload artifacts
```

### On Merge to Main

```
Merge to Main
 → CI Workflow
 → Build & test with coverage
 → Create packages
 → Analyze commits
 → Create tag with GitVersion
 → Create GitHub Release
 → Publish to NuGet.org
 → Publish to GitHub Packages
```

## 📦 What Gets Published?

### NuGet Packages

- JD.Efcpt.Build
- JD.Efcpt.Build.Templates

## Version Format

- **Format**: `v{Major}.{Minor}.{Patch}`
- **Examples**:
 - `v0.1.0` - Initial release
 - `v0.2.0` - Feature added
 - `v0.2.1` - Bug fixed
 - `v1.0.0` - Breaking change

## ⚠️ Important Notes

### DO

- Wait for PR validation to complete before merging
- Squash merge PRs to keep a clean history
- Review dry-run artifacts in PRs
- Write tests for new features
- Follow existing code style

### DON'T

- Force-push to main branch
- Merge PRs with failing checks
- Skip tests
- Introduce breaking changes without discussion

## Build Commands

```bash
# Restore dependencies
dotnet restore --use-lock-file
dotnet tool restore

# Build
dotnet build JD.Efcpt.Build.sln --configuration Release

# Test
dotnet test JD.Efcpt.Build.sln --configuration Release

# Build documentation
docfx metadata docs/docfx.json
docfx build docs/docfx.json

# Pack for NuGet
dotnet pack JD.Efcpt.Build.sln --configuration Release --output ./artifacts
```

## Troubleshooting

### Build Failing

**Check**:
1. Review CI logs in Actions tab
2. Run tests locally: `dotnet test`
3. Check for dependency issues
4. Ensure .NET 9+ SDK is installed

### Tests Failing

**Check**:
1. Run specific test: `dotnet test --filter "FullyQualifiedName~TestName"`
2. Check test output for error details
3. Ensure all dependencies are restored

### Documentation Build Failing

**Check**:
1. Ensure DocFX is installed: `dotnet tool update -g docfx`
2. Check `docs/docfx.json` for configuration issues
3. Verify XML documentation comments in code

## Automated Maintenance

### Dependabot

- Runs weekly to check for package updates
- Creates PRs for outdated dependencies
- Groups updates by category (Microsoft, testing, etc.)

### Update Packages Workflow

- Runs weekly on Sunday
- Uses `dotnet-outdated` tool
- Creates PR with package updates
- Preserves major version compatibility

### Stale Issues/PRs

- Marks issues stale after 60 days of inactivity
- Closes stale issues after 7 more days
- Marks PRs stale after 30 days
- Closes stale PRs after 14 more days
- Exempt labels: pinned, security, bug, enhancement

## More Information

- **Workflows**: [.github/workflows/](.github/workflows/)
- **Contributing**: Check README.md for contribution guidelines
- **Issues**: [GitHub Issues](https://github.com/JerrettDavis/JD.Efcpt.Build/issues)
- **Discussions**: [GitHub Discussions](https://github.com/JerrettDavis/JD.Efcpt.Build/discussions)