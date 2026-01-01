# Enterprise Adoption Guide

**Audience:** Engineering Managers, Architects, DevOps Leads
**Scenario:** Adopting JD.Efcpt.Build across multiple teams and projects

---

## Overview

This guide helps organizations successfully adopt JD.Efcpt.Build at scale, covering:
- Team onboarding and training
- Standardization across projects
- Centralized configuration management
- Best practices for large codebases

## Adoption Strategy

### Phase 1: Pilot Project (2-4 weeks)

**Goal:** Validate JD.Efcpt.Build with a single team and project.

#### 1.1 Select a Pilot Team

**Ideal characteristics:**
- ✅ Experienced with EF Core
- ✅ Has an existing SQL Server Database Project
- ✅ Medium-sized schema (20-100 tables)
- ✅ Active development (to test incremental builds)
- ✅ Enthusiastic about trying new tools

**Avoid:**
- ❌ Mission-critical production systems (for initial pilot)
- ❌ Projects with unusual schema requirements
- ❌ Teams under tight deadlines

#### 1.2 Initial Setup

```bash
# Install SDK template
dotnet new install JD.Efcpt.Templates

# Navigate to pilot project
cd src/PilotProject

# Add JD.Efcpt.Build SDK
dotnet add package JD.Efcpt.Sdk
```

#### 1.3 Create Standard Configuration

Create a baseline `efcpt.json`:

```json
{
  "Names": {
    "Namespace": "YourCompany.PilotProject.Data",
    "DbContext": "ApplicationDbContext"
  },
  "FileLayout": {
    "OutputPath": "Generated",
    "SplitDbContext": true
  },
  "Preferences": {
    "UseDataAnnotations": false,
    "UseDatabaseNames": false,
    "IncludeConnectionString": false
  }
}
```

#### 1.4 Measure Success Metrics

Track:
- **Build time reduction** (incremental builds)
- **Developer satisfaction** (survey)
- **Bugs related to model sync** (reduction expected)
- **Time to onboard new developers** (should decrease)

### Phase 2: Standardization (4-8 weeks)

**Goal:** Establish organization-wide standards based on pilot learnings.

#### 2.1 Create Configuration Standards

**Establish conventions:**

```jsonc
// company-efcpt-standards.json (template)
{
  "Names": {
    // Standard: Use project name + "DbContext"
    "DbContext": "{{ProjectName}}DbContext",
    // Standard: Align with project's root namespace
    "Namespace": "{{RootNamespace}}.Data"
  },
  "FileLayout": {
    // Standard: Always use "Generated" folder
    "OutputPath": "Generated",
    // Standard: Split DbContext for clarity
    "SplitDbContext": true
  },
  "Preferences": {
    // Standard: Use fluent API (not data annotations)
    "UseDataAnnotations": false,
    // Standard: Use C# conventions (not database names)
    "UseDatabaseNames": false,
    // Standard: Never include connection strings in code
    "IncludeConnectionString": false,
    // Standard: Use nullable reference types
    "UseNullableReferenceTypes": true
  },
  "Overrides": {
    // Company-specific customizations
  }
}
```

#### 2.2 Create Internal Documentation

**Document:**
- Why the organization uses JD.Efcpt.Build
- Step-by-step setup guide (with screenshots)
- Configuration standards
- Common troubleshooting steps
- Who to contact for help

**Example structure:**
```
internal-wiki/
├── why-jd-efcpt-build.md
├── setup-guide.md
├── configuration-standards.md
├── troubleshooting.md
└── faq.md
```

#### 2.3 Create Project Templates

**Option A: Extend existing templates**

```bash
# Create custom template package
dotnet new template create --name YourCompany.AspNet.Template
```

**Include:**
- Pre-configured `efcpt.json`
- `.gitignore` with `Generated/` exclusions
- Standard connection string in `appsettings.json`
- Example SQL project reference

**Option B: Scripted setup**

```bash
#!/bin/bash
# setup-efcpt.sh
PROJECT_NAME=$1
ROOT_NAMESPACE=$2

echo "Setting up JD.Efcpt.Build for $PROJECT_NAME..."

# Add SDK
dotnet add package JD.Efcpt.Sdk

# Create standard efcpt.json
cat > efcpt.json <<EOL
{
  "Names": {
    "DbContext": "${PROJECT_NAME}DbContext",
    "Namespace": "${ROOT_NAMESPACE}.Data"
  },
  "FileLayout": {
    "OutputPath": "Generated",
    "SplitDbContext": true
  }
}
EOL

# Add .gitignore entry
echo "Generated/" >> .gitignore

echo "✅ Setup complete!"
```

### Phase 3: Rollout (3-6 months)

**Goal:** Expand to all teams while providing support.

#### 3.1 Create Rollout Plan

**Prioritization criteria:**
1. Teams with existing SQL projects (easier migration)
2. Projects actively developed (get value immediately)
3. Teams eager to adopt (natural champions)
4. Projects with model sync issues (high pain point)

**Example timeline:**
```
Month 1: Teams A, B, C (early adopters)
Month 2: Teams D, E, F, G
Month 3: Teams H, I, J
Month 4-6: Remaining teams
```

#### 3.2 Provide Training

**Workshop format (2 hours):**

1. **Introduction** (15 mins)
   - What is JD.Efcpt.Build
   - Benefits for our organization
   - Success metrics from pilot

2. **Live Demo** (30 mins)
   - Setup from scratch
   - First build
   - Making schema changes
   - Troubleshooting common issues

3. **Hands-on Exercise** (45 mins)
   - Teams set up in their own projects
   - Instructors provide 1-on-1 help

4. **Standards & Best Practices** (20 mins)
   - Company configuration standards
   - Git workflow
   - CI/CD integration
   - Where to get help

5. **Q&A** (10 mins)

#### 3.3 Establish Support Channels

**Create:**
- **Slack/Teams channel:** `#jd-efcpt-build-help`
- **Office hours:** Weekly drop-in sessions
- **Champion network:** 1-2 people per team
- **Documentation site:** Searchable internal wiki

### Phase 4: Optimization (Ongoing)

**Goal:** Continuously improve based on usage data and feedback.

#### 4.1 Monitor Adoption Metrics

**Track:**
```sql
-- Example metrics query
SELECT
    team_name,
    COUNT(*) as projects_using_efcpt,
    AVG(build_time_incremental_ms) as avg_incremental_build,
    AVG(build_time_clean_ms) as avg_clean_build,
    COUNT(DISTINCT developer) as active_developers
FROM adoption_metrics
GROUP BY team_name
ORDER BY projects_using_efcpt DESC;
```

#### 4.2 Gather Continuous Feedback

**Quarterly survey questions:**
- How satisfied are you with JD.Efcpt.Build? (1-5 scale)
- What pain points have you encountered?
- What features would make it more valuable?
- How can we improve documentation/training?

#### 4.3 Share Success Stories

**Internal blog posts:**
- "Team X reduced build times by 80% with JD.Efcpt.Build"
- "How Team Y eliminated model sync bugs"
- "Best practices learned from 50+ projects"

## Centralized Configuration

### Strategy: Configuration Repository

**Create a shared configuration repository:**

```
company-efcpt-configs/
├── base-config.json              # Shared defaults
├── microservices-config.json     # Microservices template
├── monolith-config.json          # Monolith template
└── README.md                     # Usage guide
```

**Usage in projects:**

```xml
<!-- YourProject.csproj -->
<ItemGroup>
  <!-- Reference shared config -->
  <EfcptConfigBaseFile Include="..\company-efcpt-configs\base-config.json" />
</ItemGroup>
```

### Strategy: MSBuild Directory.Build.props

**Centralize common properties:**

```xml
<!-- Directory.Build.props at solution root -->
<Project>
  <PropertyGroup>
    <!-- Standard settings for all projects -->
    <EfcptToolMode>tool-manifest</EfcptToolMode>
    <EfcptToolPackageId>EFCorePowerTools.Cli</EfcptToolPackageId>
    <EfcptToolVersion>8.0.0</EfcptToolVersion>
  </PropertyGroup>
</Project>
```

**Projects automatically inherit:**

```xml
<!-- Individual project -->
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Inherits EfcptToolMode, etc. from Directory.Build.props -->

  <!-- Project-specific overrides -->
  <PropertyGroup>
    <EfcptDacpacPath>../Database/Database.dacpac</EfcptDacpacPath>
  </PropertyGroup>
</Project>
```

## Multi-Project Best Practices

### Pattern 1: Shared SQL Project

**Structure:**
```
YourSolution/
├── src/
│   ├── Database/
│   │   └── Database.sqlproj → Database.dacpac
│   ├── WebApi/
│   │   └── WebApi.csproj (references Database.dacpac)
│   ├── BackgroundWorker/
│   │   └── BackgroundWorker.csproj (references Database.dacpac)
│   └── AdminPortal/
│       └── AdminPortal.csproj (references Database.dacpac)
└── Directory.Build.props
```

**Shared configuration:**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- All projects reference same DACPAC -->
    <SharedDacpacPath>$(MSBuildThisFileDirectory)src\Database\bin\$(Configuration)\Database.dacpac</SharedDacpacPath>
  </PropertyGroup>
</Project>
```

**Individual projects:**

```xml
<!-- WebApi.csproj -->
<PropertyGroup>
  <EfcptDacpacPath>$(SharedDacpacPath)</EfcptDacpacPath>
  <!-- Project-specific namespace -->
  <RootNamespace>YourCompany.WebApi</RootNamespace>
</PropertyGroup>
```

### Pattern 2: Microservices with Separate Databases

**Structure:**
```
microservices/
├── services/
│   ├── OrderService/
│   │   ├── Database/
│   │   │   └── OrderDb.sqlproj
│   │   └── OrderService/
│   │       └── OrderService.csproj
│   ├── InventoryService/
│   │   ├── Database/
│   │   │   └── InventoryDb.sqlproj
│   │   └── InventoryService/
│   │       └── InventoryService.csproj
│   └── ...
└── shared/
    └── company-efcpt-configs/
        └── microservice-base.json
```

**Each service is independent:**

```json
// OrderService/efcpt.json
{
  "ExtendFrom": "../../shared/company-efcpt-configs/microservice-base.json",
  "Names": {
    "DbContext": "OrderDbContext",
    "Namespace": "OrderService.Data"
  }
}
```

## CI/CD Integration

### GitHub Actions Example

```yaml
# .github/workflows/build.yml
name: Build

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      # Build SQL project first
      - name: Build Database Project
        run: dotnet build src/Database/Database.sqlproj

      # Restore dotnet tools (includes efcpt)
      - name: Restore .NET Tools
        run: dotnet tool restore

      # Build application (triggers EF model generation)
      - name: Build Application
        run: dotnet build src/WebApi/WebApi.csproj

      # Cache obj/ directory for fingerprinting
      - name: Cache Build Outputs
        uses: actions/cache@v3
        with:
          path: |
            **/obj
          key: ${{ runner.os }}-build-${{ hashFiles('**/*.sqlproj', '**/*.csproj') }}
```

### Azure DevOps Example

```yaml
# azure-pipelines.yml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.x'

  - task: DotNetCoreCLI@2
    displayName: 'Build Database Project'
    inputs:
      command: 'build'
      projects: 'src/Database/Database.sqlproj'

  - task: DotNetCoreCLI@2
    displayName: 'Restore .NET Tools'
    inputs:
      command: 'custom'
      custom: 'tool'
      arguments: 'restore'

  - task: DotNetCoreCLI@2
    displayName: 'Build Application'
    inputs:
      command: 'build'
      projects: 'src/**/*.csproj'

  # Cache for performance
  - task: Cache@2
    inputs:
      key: 'dacpac | "$(Agent.OS)" | **/Database.sqlproj'
      path: '**/obj'
```

## Team Onboarding Checklist

### For New Team Members

- [ ] Read internal "Why JD.Efcpt.Build" documentation
- [ ] Complete setup guide with sample project
- [ ] Understand configuration standards
- [ ] Join `#jd-efcpt-build-help` Slack/Teams channel
- [ ] Know how to run local builds
- [ ] Understand fingerprinting behavior
- [ ] Know where to find troubleshooting docs

### For Project Onboarding

- [ ] SQL project exists and builds successfully
- [ ] `efcpt.json` created from company template
- [ ] `.config/dotnet-tools.json` includes efcpt
- [ ] `.gitignore` excludes `Generated/` directory
- [ ] CI/CD pipeline updated to build SQL project first
- [ ] Team has reviewed generated models
- [ ] Documentation updated with setup instructions

## Common Challenges & Solutions

### Challenge: Inconsistent Configurations Across Projects

**Problem:** Each team configures JD.Efcpt.Build differently.

**Solution:**
- Create shared configuration templates
- Use `Directory.Build.props` for common settings
- Automated linting/validation in CI/CD
- Regular audits of project configurations

### Challenge: Build Performance in Large Monorepos

**Problem:** Many projects regenerating models slows builds.

**Solution:**
- Use fingerprinting (should be automatic)
- Cache `obj/` directories in CI/CD
- Consider splitting very large schemas
- Use incremental builds (`dotnet build --no-restore`)

### Challenge: Resistance to Adoption

**Problem:** Some teams reluctant to change existing workflows.

**Solution:**
- Demonstrate time savings with metrics
- Highlight reduced bugs from automated sync
- Start with enthusiastic early adopters
- Provide excellent support during transition
- Allow gradual migration (not all-at-once)

### Challenge: Training at Scale

**Problem:** Hard to train 100+ developers individually.

**Solution:**
- Record training sessions for async learning
- Create interactive sandbox environments
- Champion network for peer-to-peer help
- Office hours for live questions
- Comprehensive written documentation

## Success Metrics

### Key Performance Indicators (KPIs)

**Adoption Metrics:**
- % of projects using JD.Efcpt.Build
- % of developers active on the tool
- Time to onboard new projects (decreasing)

**Performance Metrics:**
- Average incremental build time (decreasing)
- % of builds that are incremental (increasing)
- CI/CD pipeline duration (decreasing)

**Quality Metrics:**
- Bugs related to model sync (decreasing)
- Developer satisfaction (increasing)
- Time spent on manual model updates (decreasing)

### Reporting Dashboard Example

```markdown
## Q4 2024 JD.Efcpt.Build Adoption Report

### Adoption
- **68 projects** now using JD.Efcpt.Build (+15 from Q3)
- **142 active developers** (+28 from Q3)
- **12 minutes** average time to onboard new project (-18 min from Q3)

### Performance
- **0.2s** average incremental build time (-85% from baseline)
- **94%** of builds are incremental
- **3.2 minutes** average CI/CD pipeline (-40% from baseline)

### Quality
- **2 bugs** related to model sync (-12 from Q3)
- **4.6/5** developer satisfaction score (+0.4 from Q3)
- **8 hours/week** saved across organization

### Top Performing Teams
1. Team Falcon - 100% adoption, 0.1s incremental builds
2. Team Phoenix - 100% adoption, 98% incremental build rate
3. Team Eagle - 95% adoption, excellent developer feedback
```

## See Also

- [CI/CD Integration Patterns](ci-cd-patterns.md)
- [Microservices Patterns](microservices.md)
- [Configuration Reference](../configuration.md)
- [Troubleshooting Guide](../troubleshooting.md)
