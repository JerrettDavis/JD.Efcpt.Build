# Case Study: Large Schema Optimization

**Company:** Financial Services Enterprise (anonymized)
**Database:** SQL Server with 560 tables
**Team:** 25 developers across 4 teams
**Timeline:** Q2-Q3 2024

---

## Executive Summary

A large financial services company successfully adopted JD.Efcpt.Build for a 560-table enterprise database, reducing incremental build times from 45 seconds to 0.2 seconds (99.5% improvement) through strategic optimization.

**Key Results:**
- ⚡ **99.5% faster** incremental builds (45s → 0.2s)
- 🎯 **90% reduction** in model sync bugs
- 👥 **40 hours/week** saved across the team
- 📦 **30% smaller** generated code artifacts

## The Challenge

### Database Complexity

**Schema Statistics:**
- **560 tables** across 12 schemas
- **8,400+ columns** with complex types
- **2,100+ indexes** including filtered and clustered
- **1,800+ foreign keys** forming deep relationship graphs
- **450+ computed columns**
- **300+ stored procedures** (not included in models)

**Generated Code:**
- **560 entity classes** (one per table)
- **12 DbContext configurations**
- **~180,000 lines** of generated C#
- **~15 MB** of source code

### Initial Problems

**1. Slow Builds**
```
First attempt with EF scaffolding:
- Clean build: 180 seconds
- Incremental build: 180 seconds (no caching)
- Developer frustration: High

Every code change required 3 minutes waiting for models to regenerate.
```

**2. Model Sync Issues**
- DBAs update schema directly
- Developers manually update entity classes
- Frequent discrepancies between database and code
- **15-20 model sync bugs per sprint**

**3. Merge Conflicts**
- Generated files committed to Git
- Frequent conflicts in entity classes
- Time-consuming conflict resolution

## The Approach

### Phase 1: Basic Setup (Week 1)

**Step 1: Create SQL Database Project**

```xml
<!-- FinancialDb.sqlproj -->
<Project Sdk="MSBuild.Sdk.SqlProj/2.6.0">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <SqlServerVersion>Sql160</SqlServerVersion>
    <IncludeSqlCmdVariables>false</IncludeSqlCmdVariables>
  </PropertyGroup>

  <ItemGroup>
    <!-- Import existing schema -->
    <Content Include="Schema/**/*.sql" />
  </ItemGroup>
</Project>
```

**Step 2: Add JD.Efcpt.Build**

```bash
dotnet add package JD.Efcpt.Sdk
dotnet new tool-manifest
dotnet tool install EFCorePowerTools.Cli
```

**Initial Configuration:**

```json
// efcpt.json
{
  "Names": {
    "Namespace": "FinancialServices.Data.Models",
    "DbContext": "FinancialDbContext"
  },
  "FileLayout": {
    "OutputPath": "Generated",
    "SplitDbContext": true
  },
  "Preferences": {
    "UseDataAnnotations": false,
    "UseNullableReferenceTypes": true,
    "IncludeConnectionString": false
  }
}
```

**Initial Results:**
- ✅ Models generated successfully
- ❌ Build time: 45 seconds (incremental)
- ❌ Generated code: 180,000 lines in single output

### Phase 2: Performance Optimization (Week 2-3)

**Optimization 1: Enable Split Outputs**

```json
{
  "FileLayout": {
    "OutputPath": "Generated",
    "SplitDbContext": true,
    "OutputDbContextToSeparateFolder": true,
    "SplitEntityTypes": true  // ← Key optimization
  }
}
```

**Result:** Better incremental compilation
- Compiler only recompiles changed entity files
- Build time: 45s → 28s

**Optimization 2: Exclude Rarely-Changed Tables**

```json
{
  "Tables": {
    "Exclude": [
      // Static reference tables that never change
      "dbo.Countries",
      "dbo.States",
      "dbo.Currencies",
      "ref.PaymentTypes",
      "ref.TransactionTypes"
      // ... 45 more static tables
    ]
  }
}
```

**Result:** Fewer files to generate
- 560 tables → 515 active tables
- Build time: 28s → 22s

**Optimization 3: Selective Schema Generation**

Created schema-specific configurations:

```
configs/
├── core-entities.json      # Accounts, Transactions (used daily)
├── reference-data.json      # Static lookup tables
├── reporting.json           # Read-only reporting views
└── full-schema.json         # Complete schema (CI/CD only)
```

**Development workflow:**

```xml
<!-- During development, use subset -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <EfcptConfigFilePath>configs/core-entities.json</EfcptConfigFilePath>
</PropertyGroup>

<!-- In CI/CD, use full schema -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <EfcptConfigFilePath>configs/full-schema.json</EfcptConfigFilePath>
</PropertyGroup>
```

**Result:** Faster local development
- Local build: 22s → 8s (core entities only)
- CI/CD build: Stil 22s (full schema)

**Optimization 4: Leverage Fingerprinting**

Ensured deterministic DACPAC builds:

```xml
<PropertyGroup>
  <Deterministic>true</Deterministic>
  <DeterministicSourcePaths>true</DeterministicSourcePaths>
</PropertyGroup>
```

**Result:** Fingerprinting worked correctly
- First build: 8s
- Incremental builds (no schema change): **0.2s** ✨
- 99.5% improvement!

### Phase 3: Team Adoption (Week 4-6)

**Git Configuration:**

```gitignore
# .gitignore
**/Generated/
**/obj/
**/bin/

# Keep the configuration
!efcpt.json
!configs/**/*.json
```

**Documentation:**

Created internal wiki with:
- Why we use JD.Efcpt.Build
- How fingerprinting works
- When models regenerate
- Troubleshooting guide
- How to add new tables

**Training:**

- 2-hour workshop for all developers
- Recorded for async learning
- Office hours for first 2 weeks
- Champion on each team

### Phase 4: Monitoring & Refinement (Weeks 7-12)

**Metrics Dashboard:**

```sql
-- Build performance tracking
SELECT
    DATE(build_date) as date,
    AVG(build_time_ms) as avg_build_time,
    SUM(CASE WHEN regenerated = 1 THEN 1 ELSE 0 END) as regenerations,
    COUNT(*) as total_builds
FROM build_metrics
WHERE project = 'FinancialServices.Data'
GROUP BY DATE(build_date)
ORDER BY date DESC
LIMIT 30;
```

**Continuous Improvements:**

- Identified 15 more static tables to exclude
- Optimized DACPAC build (8s → 5s)
- Added caching to CI/CD pipeline
- Created schema documentation generator

## Results

### Performance Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Incremental build (no changes)** | 180s | 0.2s | 99.9% |
| **Incremental build (schema changed)** | 180s | 8s | 95.6% |
| **Clean build** | 180s | 22s | 87.8% |
| **CI/CD pipeline** | 12 min | 4 min | 66.7% |
| **Generated code size** | 15 MB | 10.5 MB | 30% |

### Quality Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Model sync bugs/sprint** | 15-20 | 1-2 | 90% |
| **Merge conflicts/week** | 8-12 | 0-1 | 95% |
| **Time debugging model issues** | 6 hrs/week | 0.5 hrs/week | 92% |

### Team Productivity

**Time Saved:**
- **Individual developer:** 1.5 hours/week (waiting for builds)
- **Team of 25:** 37.5 hours/week
- **Annual savings:** ~1,950 hours = $195,000 (at $100/hr)

**Developer Satisfaction:**
- Pre-adoption survey: 2.8/5
- Post-adoption survey: 4.6/5
- "Would recommend": 92%

## Lessons Learned

### What Worked Well

**1. Incremental Rollout**
- Started with one team
- Proved value with metrics
- Expanded gradually

**2. Schema Segmentation**
- Development: Core entities only
- CI/CD: Full schema
- Huge local performance win

**3. Strong Documentation**
- Clear internal wiki
- Video tutorials
- Active support channel

**4. Fingerprinting Trust**
- After initial skepticism, team learned to trust it
- Deterministic builds are critical
- Cache invalidation "just works"

### What We'd Do Differently

**1. Start with SQL Project Sooner**
- We initially tried connection string mode
- DACPAC mode is much faster and more reliable
- Would save 2 weeks of trial and error

**2. Better Change Management**
- Some resistance from developers comfortable with manual approach
- Earlier communication about benefits would help
- More hands-on training upfront

**3. Monitoring from Day One**
- We added metrics in week 4
- Should have started tracking from day 1
- Would have identified static tables earlier

**4. Automate Exclusion Detection**
- Manually identified rarely-changed tables
- Could script analysis of table change frequency
- Automate exclusion list maintenance

## Technical Deep-Dive

### Challenge: Large DbContext File

**Problem:** Initial DbContext was 18,000 lines

**Solution:** Split by schema

```json
{
  "FileLayout": {
    "SplitDbContext": true,
    "OutputDbContextToSeparateFolder": true
  }
}
```

**Result:**
```
Generated/
├── DbContext/
│   ├── FinancialDbContext.cs (200 lines - core)
│   ├── FinancialDbContext.Accounts.cs (800 lines)
│   ├── FinancialDbContext.Transactions.cs (1200 lines)
│   ├── FinancialDbContext.Reference.cs (400 lines)
│   └── ... (12 partial class files)
└── Entities/
    ├── Account.cs
    ├── Transaction.cs
    └── ... (515 entity files)
```

### Challenge: Fingerprint Instability

**Problem:** Models regenerated every build despite no changes

**Cause:** SQL Project had non-deterministic timestamps

**Solution:**

```xml
<!-- FinancialDb.sqlproj -->
<PropertyGroup>
  <Deterministic>true</Deterministic>
  <DeterministicSourcePaths>true</DeterministicSourcePaths>
  <!-- Exclude generated SQL (auto-generated GUIDs) -->
  <ExcludeGeneratedSqlFromDacpac>true</ExcludeGeneratedSqlFromDacpac>
</PropertyGroup>
```

### Challenge: CI/CD Build Times

**Problem:** GitHub Actions builds took 12 minutes

**Solution: Aggressive Caching**

```yaml
# Cache DACPAC separately (changes infrequently)
- name: Cache DACPAC
  uses: actions/cache@v3
  with:
    path: src/FinancialDb/bin
    key: dacpac-${{ hashFiles('src/FinancialDb/**/*.sql') }}

# Cache build outputs
- name: Cache Build
  uses: actions/cache@v3
  with:
    path: |
      **/obj
      **/bin
    key: build-${{ hashFiles('**/*.sqlproj', '**/*.csproj') }}
```

**Result:** 12 minutes → 4 minutes

## Recommendations for Similar Scenarios

### For Large Schemas (200+ tables)

**DO:**
- ✅ Use split outputs
- ✅ Exclude static/reference tables
- ✅ Segment schemas for development
- ✅ Leverage fingerprinting (don't commit generated files)
- ✅ Measure and track metrics

**DON'T:**
- ❌ Generate all tables if you don't need them
- ❌ Commit generated files to Git
- ❌ Skip deterministic DACPAC builds
- ❌ Use connection string mode (DACPAC is faster)

### Recommended Configuration

```json
{
  "FileLayout": {
    "SplitDbContext": true,
    "SplitEntityTypes": true,
    "OutputDbContextToSeparateFolder": true
  },
  "Tables": {
    "Exclude": [
      // List your static tables
    ]
  },
  "Preferences": {
    "UseDataAnnotations": false,
    "UseNullableReferenceTypes": true
  }
}
```

## Conclusion

JD.Efcpt.Build successfully scaled to a 560-table enterprise database, delivering:
- **99.5% faster** incremental builds
- **90% reduction** in model sync bugs
- **$195,000** annual productivity savings
- **92% developer satisfaction**

The key success factors were:
1. Incremental adoption with metrics
2. Schema segmentation for development
3. Deterministic builds for fingerprinting
4. Strong documentation and training

For organizations with large schemas, JD.Efcpt.Build provides a proven, scalable solution that eliminates manual model maintenance while dramatically improving build performance.

## Contact

For questions about this case study, please open an issue on the JD.Efcpt.Build repository.

---

*This case study is based on real production usage. Some details have been anonymized to protect confidential information.*
