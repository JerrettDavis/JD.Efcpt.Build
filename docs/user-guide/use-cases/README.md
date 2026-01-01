# Use Cases & Patterns

This section provides real-world use cases and patterns for using JD.Efcpt.Build in different scenarios.

## Available Guides

### [Enterprise Adoption Guide](enterprise.md)
**For organizations adopting JD.Efcpt.Build at scale**

Learn how to roll out JD.Efcpt.Build across multiple teams and projects:
- Team onboarding strategies
- Standardizing conventions across projects
- Centralized configuration management
- Best practices for large organizations

**Best for:** Architects, DevOps leads, Engineering managers

---

### [CI/CD Integration Patterns](ci-cd-patterns.md)
**For implementing automated builds and deployments**

Comprehensive guide to integrating JD.Efcpt.Build in CI/CD pipelines:
- GitHub Actions workflows
- Azure DevOps pipelines
- GitLab CI configuration
- Build caching strategies
- Deployment patterns

**Best for:** DevOps engineers, Build engineers

---

### [Microservices Patterns](microservices.md)
**For microservices architectures**

How to use JD.Efcpt.Build effectively in microservices:
- Database-per-service pattern
- Shared database considerations
- Service boundaries and models
- Cross-service dependencies

**Best for:** Backend engineers, Microservices architects

---

### [Multi-Database Scenarios](multi-database.md)
**For applications using multiple databases**

Strategies for managing multiple database connections:
- Multiple DACPACs in one project
- Different providers (SQL Server + PostgreSQL)
- Read/write splitting
- Multi-tenancy patterns

**Best for:** Backend engineers, Database administrators

## Quick Reference

### Common Scenarios

| Scenario | Recommended Approach | Guide |
|----------|---------------------|-------|
| Single web application | DACPAC mode with SQL project | [Getting Started](../getting-started.md) |
| Multiple microservices | Shared DACPAC or connection string mode | [Microservices](microservices.md) |
| Monorepo with many projects | Centralized configuration | [Enterprise](enterprise.md) |
| GitHub Actions deployment | DACPAC mode with caching | [CI/CD Patterns](ci-cd-patterns.md) |
| Development + staging + production | Connection string mode | [Multi-Database](multi-database.md) |

### Mode Selection Guide

```
┌─────────────────────────────────────────────────┐
│  Do you have a SQL Server Database Project?    │
└────────────┬────────────────────────────────────┘
             │
        Yes  │  No
             │
    ┌────────▼──────────┐
    │   DACPAC Mode     │
    │  (Recommended)    │
    └───────────────────┘
             │
             │  Can you add one?
             │
        Yes  │  No/Difficult
             │
    ┌────────▼────────────────┐
    │   Create SQL Project    │
    │  + DACPAC Mode          │
    └─────────────────────────┘
                              │
                              │
                    ┌─────────▼──────────────┐
                    │  Connection String     │
                    │  Mode (Direct DB)      │
                    └────────────────────────┘
```

## See Also

- [Getting Started Guide](../getting-started.md)
- [Configuration Reference](../configuration.md)
- [Troubleshooting](../troubleshooting.md)
