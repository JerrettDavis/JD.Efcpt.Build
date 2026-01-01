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

## Quick Reference

### Common Scenarios

| Scenario | Recommended Approach | Guide |
|----------|---------------------|-------|
| Single web application | DACPAC mode with SQL project | [Getting Started](../getting-started.md) |
| Multiple services | Shared DACPAC or connection string mode | [Enterprise](enterprise.md) |
| Monorepo with many projects | Centralized configuration | [Enterprise](enterprise.md) |
| CI/CD deployment | DACPAC mode with caching | [CI/CD Integration](../ci-cd.md) |
| Cloud databases | Connection string mode | [Connection String Mode](../connection-string-mode.md) |

### Mode Selection Guide

```
Do you have a SQL Server Database Project?
    |
    ├── Yes → Use DACPAC Mode (Recommended)
    |
    └── No
        |
        ├── Can you add one?
        |   └── Yes → Create SQL Project + DACPAC Mode
        |
        └── No/Difficult → Use Connection String Mode
```

## See Also

- [Getting Started Guide](../getting-started.md) - Installation and first project
- [Configuration Reference](../configuration.md) - MSBuild properties and JSON config
- [CI/CD Integration](../ci-cd.md) - GitHub Actions, Azure DevOps, Docker
- [Troubleshooting](../troubleshooting.md) - Common issues and solutions
