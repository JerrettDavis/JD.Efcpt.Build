# CI/CD Integration

JD.Efcpt.Build is designed to work seamlessly in continuous integration and deployment pipelines. This guide covers integration with popular CI/CD platforms.

## Overview

The package requires no special configuration for CI/CD. Models are generated deterministically from your database project or connection, ensuring consistent results across environments.

## Prerequisites

Ensure your CI/CD environment has:

- .NET SDK 8.0 or later
- EF Core Power Tools CLI (not required for .NET 10+)
- For DACPAC mode: SQL Server Data Tools components

## GitHub Actions

### .NET 10+ (Recommended)

No tool installation required - the CLI is executed via `dnx`:

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build
```

### .NET 8-9

Requires tool installation:

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'

    - name: Restore tools
      run: dotnet tool restore

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build
```

### With Caching

Speed up builds by caching the efcpt intermediate directory:

```yaml
name: Build with Cache

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Cache efcpt outputs
      uses: actions/cache@v4
      with:
        path: |
          **/obj/efcpt/
        key: efcpt-${{ runner.os }}-${{ hashFiles('**/*.sqlproj', '**/efcpt-config.json') }}
        restore-keys: |
          efcpt-${{ runner.os }}-

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --configuration Release --no-restore

    - name: Test
      run: dotnet test --configuration Release --no-build
```

## Azure DevOps

### Basic Pipeline

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  displayName: 'Setup .NET SDK'
  inputs:
    version: '10.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--configuration Release --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Test'
  inputs:
    command: 'test'
    arguments: '--configuration Release --no-build'
```

### With Tool Manifest (.NET 8-9)

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  displayName: 'Setup .NET SDK'
  inputs:
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore tools'
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--configuration Release --no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Test'
  inputs:
    command: 'test'
    arguments: '--configuration Release --no-build'
```

### With Caching

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

variables:
  NUGET_PACKAGES: $(Pipeline.Workspace)/.nuget/packages

steps:
- task: Cache@2
  displayName: 'Cache NuGet packages'
  inputs:
    key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
    restoreKeys: |
      nuget | "$(Agent.OS)"
    path: $(NUGET_PACKAGES)

- task: Cache@2
  displayName: 'Cache efcpt outputs'
  inputs:
    key: 'efcpt | "$(Agent.OS)" | **/*.sqlproj | **/efcpt-config.json'
    restoreKeys: |
      efcpt | "$(Agent.OS)"
    path: '**/obj/efcpt'

- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--configuration Release --no-restore'
```

## Docker

### Multi-Stage Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY src/**/*.csproj ./src/
COPY database/**/*.sqlproj ./database/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build
RUN dotnet build --configuration Release --no-restore

# Publish
RUN dotnet publish src/MyApp/MyApp.csproj --configuration Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### With Tool Manifest (.NET 8-9)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy tool manifest and restore tools
COPY .config/dotnet-tools.json .config/
RUN dotnet tool restore

# Copy and restore
COPY *.sln .
COPY src/**/*.csproj ./src/
COPY database/**/*.sqlproj ./database/
RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet build --configuration Release --no-restore

# Publish
RUN dotnet publish src/MyApp/MyApp.csproj --configuration Release --no-build -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

## GitLab CI

```yaml
stages:
  - build
  - test

variables:
  DOTNET_VERSION: "10.0"

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet restore
    - dotnet build --configuration Release --no-restore
  artifacts:
    paths:
      - "**/bin/"
      - "**/obj/"
    expire_in: 1 hour

test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:10.0
  dependencies:
    - build
  script:
    - dotnet test --configuration Release --no-build
```

## Jenkins

### Jenkinsfile (Declarative)

```groovy
pipeline {
    agent {
        docker {
            image 'mcr.microsoft.com/dotnet/sdk:10.0'
        }
    }

    stages {
        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --configuration Release --no-build'
            }
        }
    }
}
```

## Connection String Mode in CI/CD

When using connection string mode, you'll need a database available during build.

### Using Environment Variables

```yaml
# GitHub Actions
env:
  DB_CONNECTION_STRING: ${{ secrets.DB_CONNECTION_STRING }}

steps:
- name: Build
  run: dotnet build --configuration Release
```

```xml
<!-- .csproj -->
<PropertyGroup>
  <EfcptConnectionString>$(DB_CONNECTION_STRING)</EfcptConnectionString>
</PropertyGroup>
```

### Using a Container Database

```yaml
# GitHub Actions with SQL Server container
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    env:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourStrong!Passw0rd
    ports:
      - 1433:1433
    options: >-
      --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong!Passw0rd -Q 'SELECT 1'"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5

steps:
- name: Setup database
  run: |
    sqlcmd -S localhost -U sa -P YourStrong!Passw0rd -i scripts/setup.sql

- name: Build
  env:
    EfcptConnectionString: "Server=localhost;Database=MyDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  run: dotnet build --configuration Release
```

## Windows vs Linux Agents

### DACPAC Mode Requirements

Building `.sqlproj` to DACPAC typically requires Windows agents with SQL Server Data Tools installed.

```yaml
# GitHub Actions - Windows for DACPAC
jobs:
  build:
    runs-on: windows-latest
```

### Connection String Mode

Connection string mode works on both Windows and Linux:

```yaml
# GitHub Actions - Linux is fine for connection string mode
jobs:
  build:
    runs-on: ubuntu-latest
```

## Troubleshooting CI/CD

### Build fails with "efcpt not found"

For .NET 8-9, ensure tool restore runs before build:

```yaml
- name: Restore tools
  run: dotnet tool restore
```

### DACPAC build fails

Ensure Windows agent with SQL Server Data Tools:

```yaml
pool:
  vmImage: 'windows-latest'
```

### Inconsistent generated code

Clear the cache to force regeneration:

```yaml
- name: Clear efcpt cache
  run: rm -rf **/obj/efcpt
```

### Slow builds

Enable caching for the efcpt intermediate directory to skip regeneration when schema hasn't changed.

## Best Practices

1. **Use .NET 10+** when possible to eliminate tool installation steps
2. **Use local tool manifests** (.NET 8-9) for version consistency
3. **Cache intermediate directories** to speed up incremental builds
4. **Use Windows agents** for DACPAC mode
5. **Use environment variables** for connection strings
6. **Never commit credentials** to source control

## Next Steps

- [Troubleshooting](troubleshooting.md) - Solve common problems
- [Configuration](configuration.md) - Complete configuration reference
- [Advanced Topics](advanced.md) - Complex scenarios
