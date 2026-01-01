# CI/CD Integration Patterns

**Audience:** DevOps Engineers, Build Engineers
**Scenario:** Integrating JD.Efcpt.Build in continuous integration and deployment pipelines

---

## Overview

JD.Efcpt.Build integrates seamlessly with modern CI/CD systems. This guide covers:
- Platform-specific configurations (GitHub Actions, Azure DevOps, GitLab CI)
- Build caching strategies
- Deployment patterns
- Performance optimization
- Troubleshooting CI/CD issues

## Quick Start Examples

### GitHub Actions

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      # Build SQL project first (generates DACPAC)
      - name: Build Database Project
        run: dotnet build src/Database/Database.sqlproj

      # Restore dotnet tools (includes efcpt CLI)
      - name: Restore .NET Tools
        run: dotnet tool restore

      # Build application (auto-generates models)
      - name: Build Application
        run: dotnet build --configuration Release

      - name: Test
        run: dotnet test --no-build

      - name: Publish
        run: dotnet publish --no-build --output ./publish
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    displayName: 'Use .NET 8'
    inputs:
      version: '8.x'

  - task: DotNetCoreCLI@2
    displayName: 'Build Database'
    inputs:
      command: 'build'
      projects: 'src/Database/Database.sqlproj'

  - script: dotnet tool restore
    displayName: 'Restore Tools'

  - task: DotNetCoreCLI@2
    displayName: 'Build'
    inputs:
      command: 'build'
      arguments: '--configuration Release'

  - task: DotNetCoreCLI@2
    displayName: 'Test'
    inputs:
      command: 'test'
      arguments: '--no-build'
```

### GitLab CI

```yaml
image: mcr.microsoft.com/dotnet/sdk:8.0

stages:
  - build
  - test
  - deploy

build:
  stage: build
  script:
    - dotnet build src/Database/Database.sqlproj
    - dotnet tool restore
    - dotnet build --configuration Release
  artifacts:
    paths:
      - src/*/bin/
      - src/*/obj/
    expire_in: 1 hour

test:
  stage: test
  dependencies:
    - build
  script:
    - dotnet test --no-build

deploy:
  stage: deploy
  dependencies:
    - build
  script:
    - dotnet publish --no-build --output ./publish
  only:
    - main
```

## Build Caching Strategies

### Why Cache?

**Without caching:**
```
Build 1: Generate models (5s) + Compile (10s) = 15s
Build 2: Generate models (5s) + Compile (10s) = 15s
Build 3: Generate models (5s) + Compile (10s) = 15s
```

**With caching:**
```
Build 1: Generate models (5s) + Compile (10s) = 15s
Build 2: Skip generation (0.1s) + Compile (3s cached) = 3.1s
Build 3: Skip generation (0.1s) + Compile (3s cached) = 3.1s
```

### GitHub Actions Caching

```yaml
jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      # Cache NuGet packages
      - name: Cache NuGet
        uses: actions/cache@v3
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      # Cache build outputs (includes fingerprint)
      - name: Cache Build Outputs
        uses: actions/cache@v3
        with:
          path: |
            **/obj
            **/bin
          key: ${{ runner.os }}-build-${{ hashFiles('**/*.sqlproj', '**/*.csproj', '**/efcpt.json') }}
          restore-keys: |
            ${{ runner.os }}-build-

      # Cache dotnet tools
      - name: Cache Dotnet Tools
        uses: actions/cache@v3
        with:
          path: ~/.dotnet/tools
          key: ${{ runner.os }}-dotnet-tools-${{ hashFiles('**/.config/dotnet-tools.json') }}

      - name: Build
        run: dotnet build
```

### Azure DevOps Caching

```yaml
steps:
  # Cache NuGet packages
  - task: Cache@2
    inputs:
      key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
      restoreKeys: |
        nuget | "$(Agent.OS)"
      path: $(NUGET_PACKAGES)
    displayName: 'Cache NuGet packages'

  # Cache build outputs
  - task: Cache@2
    inputs:
      key: 'build | "$(Agent.OS)" | **/*.sqlproj, **/*.csproj, **/efcpt.json'
      restoreKeys: |
        build | "$(Agent.OS)"
      path: |
        **/obj
        **/bin
    displayName: 'Cache Build Outputs'

  - task: DotNetCoreCLI@2
    inputs:
      command: 'build'
```

### GitLab CI Caching

```yaml
variables:
  NUGET_PACKAGES_DIRECTORY: '.nuget'

cache:
  key: ${CI_COMMIT_REF_SLUG}
  paths:
    - .nuget/
    - '**/obj/'
    - '**/bin/'

before_script:
  - export NUGET_PACKAGES=$CI_PROJECT_DIR/$NUGET_PACKAGES_DIRECTORY

build:
  stage: build
  script:
    - dotnet restore
    - dotnet build
```

## Advanced Patterns

### Pattern 1: Multi-Stage Builds

Separate DACPAC build from application build for better caching:

```yaml
# GitHub Actions
jobs:
  build-database:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Cache DACPAC
        id: cache-dacpac
        uses: actions/cache@v3
        with:
          path: src/Database/bin
          key: dacpac-${{ hashFiles('src/Database/**/*.sql') }}

      - name: Build DACPAC
        if: steps.cache-dacpac.outputs.cache-hit != 'true'
        run: dotnet build src/Database/Database.sqlproj

      - name: Upload DACPAC
        uses: actions/upload-artifact@v3
        with:
          name: dacpac
          path: src/Database/bin/**/*.dacpac

  build-application:
    needs: build-database
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Download DACPAC
        uses: actions/download-artifact@v3
        with:
          name: dacpac
          path: src/Database/bin

      - name: Build Application
        run: dotnet build src/WebApi/WebApi.csproj
```

### Pattern 2: Connection String Mode in CI/CD

Use live database connection instead of DACPAC:

```yaml
jobs:
  build:
    runs-on: ubuntu-latest

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: Your_password123
        ports:
          - 1433:1433

    steps:
      - uses: actions/checkout@v4

      # Deploy schema to test database
      - name: Deploy Database Schema
        run: |
          sqlcmd -S localhost -U sa -P Your_password123 \
            -Q "CREATE DATABASE TestDb"
          sqlcmd -S localhost -U sa -P Your_password123 \
            -d TestDb -i schema.sql

      # Build with connection string
      - name: Build Application
        env:
          ConnectionStrings__DefaultConnection: "Server=localhost;Database=TestDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true"
        run: dotnet build
```

### Pattern 3: Matrix Builds

Test against multiple database providers:

```yaml
strategy:
  matrix:
    provider:
      - sqlserver
      - postgresql
      - mysql

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    # ... config
  postgresql:
    image: postgres:15
    # ... config
  mysql:
    image: mysql:8
    # ... config

steps:
  - name: Build with ${{ matrix.provider }}
    env:
      DB_PROVIDER: ${{ matrix.provider }}
    run: dotnet build
```

### Pattern 4: Conditional Generation

Skip model generation on documentation-only changes:

```yaml
jobs:
  changes:
    runs-on: ubuntu-latest
    outputs:
      schema: ${{ steps.filter.outputs.schema }}
    steps:
      - uses: actions/checkout@v4
      - uses: dorny/paths-filter@v2
        id: filter
        with:
          filters: |
            schema:
              - 'src/Database/**'
              - '**/*.sqlproj'
              - '**/efcpt.json'

  build:
    needs: changes
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Use Cached Models
        if: needs.changes.outputs.schema != 'true'
        uses: actions/cache@v3
        with:
          path: '**/Generated/'
          key: models-${{ github.sha }}
          restore-keys: models-

      - name: Build
        run: dotnet build
```

## Deployment Patterns

### Pattern: Blue/Green Deployment

```yaml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Build
        run: dotnet build

      - name: Publish
        run: dotnet publish --output ./publish

      # Deploy to blue slot
      - name: Deploy to Blue
        run: |
          az webapp deployment slot create \
            --name myapp \
            --resource-group mygroup \
            --slot blue

          az webapp deployment source config-zip \
            --name myapp \
            --resource-group mygroup \
            --slot blue \
            --src ./publish.zip

      # Smoke test blue slot
      - name: Test Blue Slot
        run: |
          curl https://myapp-blue.azurewebsites.net/health

      # Swap blue to production
      - name: Swap Slots
        run: |
          az webapp deployment slot swap \
            --name myapp \
            --resource-group mygroup \
            --slot blue
```

### Pattern: Database Migrations + Model Generation

```yaml
jobs:
  deploy:
    steps:
      # 1. Apply database migrations
      - name: Run Migrations
        run: dotnet ef database update --connection "${{ secrets.DB_CONNECTION }}"

      # 2. Rebuild DACPAC from updated schema
      - name: Extract Updated Schema
        run: |
          sqlpackage /Action:Extract \
            /SourceConnectionString:"${{ secrets.DB_CONNECTION }}" \
            /TargetFile:updated.dacpac

      # 3. Regenerate models
      - name: Regenerate Models
        env:
          EfcptDacpacPath: updated.dacpac
        run: dotnet build

      # 4. Deploy application
      - name: Deploy
        run: dotnet publish
```

## Performance Optimization

### Optimization 1: Parallel Builds

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Build in Parallel
        run: dotnet build --parallel --maxcpucount:4
```

### Optimization 2: Minimal Rebuilds

```yaml
steps:
  - name: Restore Only Changed Projects
    run: dotnet restore --no-dependencies

  - name: Build Only Changed Projects
    run: |
      git diff --name-only HEAD^ HEAD | \
        grep '\.csproj$' | \
        xargs -r dotnet build --no-restore
```

### Optimization 3: Artifact Reuse

```yaml
jobs:
  build:
    outputs:
      fingerprint: ${{ steps.compute.outputs.fingerprint }}
    steps:
      - id: compute
        run: |
          FP=$(cat obj/Debug/net8.0/.efcpt/fingerprint.txt)
          echo "fingerprint=$FP" >> $GITHUB_OUTPUT

  test:
    needs: build
    if: needs.build.outputs.fingerprint != ''
    steps:
      - name: Use Cached Models
        run: echo "Using cached models"
```

## Troubleshooting CI/CD Issues

### Issue: Models Not Generated in CI

**Symptoms:**
- Build succeeds locally
- Fails in CI with "Type or namespace not found"

**Diagnosis:**
```yaml
- name: Debug Efcpt
  run: |
    echo "=== DACPAC exists? ==="
    ls -la src/Database/bin/**/*.dacpac || echo "DACPAC not found!"

    echo "=== Fingerprint ==="
    cat obj/Debug/net8.0/.efcpt/fingerprint.txt || echo "No fingerprint!"

    echo "=== Generated files ==="
    ls -la src/WebApi/Generated/ || echo "No generated files!"
```

**Solutions:**
1. Ensure SQL project builds before application project
2. Check DACPAC path is correct
3. Verify `dotnet tool restore` ran
4. Check file permissions

### Issue: Slow CI Builds

**Symptoms:**
- Builds take 5+ minutes
- Model generation happens every time

**Diagnosis:**
```yaml
- name: Measure Build Times
  run: |
    time dotnet build src/Database/Database.sqlproj
    time dotnet tool restore
    time dotnet build src/WebApi/WebApi.csproj
```

**Solutions:**
1. Implement caching (see above)
2. Use DACPAC mode instead of connection string mode
3. Cache dotnet tools
4. Use matrix builds for parallel testing

### Issue: Fingerprint Not Persisting

**Symptoms:**
- Cache hit but models still regenerate
- Fingerprint file missing after cache restore

**Solution:**
```yaml
# Ensure obj/ is in cache path
- uses: actions/cache@v3
  with:
    path: |
      **/obj  # ← Must include this
    key: build-${{ hashFiles('**/*.sqlproj') }}
```

## Best Practices

### ✅ DO

- **Cache aggressively** - NuGet, build outputs, tools
- **Build SQL project first** - DACPAC must exist before app build
- **Use artifacts** - Pass DACPACs between jobs
- **Monitor build times** - Track performance metrics
- **Test in CI** - Don't rely solely on local builds
- **Version lock tools** - Pin efcpt version in tool manifest

### ❌ DON'T

- **Don't skip restore** - Always run `dotnet tool restore`
- **Don't ignore cache misses** - Investigate why caches aren't hitting
- **Don't hardcode paths** - Use MSBuild properties
- **Don't commit generated files** - Keep them in .gitignore
- **Don't run clean every time** - Defeats incremental builds

## See Also

- [Enterprise Adoption Guide](enterprise.md)
- [Build Pipeline Architecture](../../architecture/PIPELINE.md)
- [Fingerprinting Deep Dive](../../architecture/FINGERPRINTING.md)
- [GitHub Actions Docs](https://docs.github.com/actions)
- [Azure DevOps Pipelines](https://learn.microsoft.com/azure/devops/pipelines/)
