# Case Studies

Real-world examples of JD.Efcpt.Build in action.

## Available Case Studies

### [Large Schema Optimization](large-schema.md)
**Scenario:** 500+ table enterprise database

How a large financial services company optimized model generation for a massive schema:
- Challenges with 500+ table database
- Performance optimization strategies
- Split output techniques
- Results and lessons learned

**Key Takeaways:**
- Incremental builds reduced from 45s to 0.2s
- Selective generation patterns
- Caching strategies for large schemas

### [Hybrid DACPAC + Connection String Approach](hybrid-approach.md)
**Scenario:** Multi-environment deployment strategy

A SaaS company using both DACPAC and connection string modes:
- Development: Connection string mode for rapid iteration
- CI/CD: DACPAC mode for deterministic builds
- Production: Schema verification without generation
- Migration strategies

**Key Takeaways:**
- Best of both modes
- Environment-specific configuration
- Deployment pipeline optimization

## Submitting Your Own Case Study

Have a success story with JD.Efcpt.Build? We'd love to hear it!

**To submit:**
1. Open an issue describing your scenario
2. Include metrics (build times, schema size, team size)
3. Explain your approach and results
4. We'll work with you to create a case study

**Template:**
```markdown
# [Your Title]

## Overview
- Company/project context
- Database size and complexity
- Team size

## Challenge
What problem were you trying to solve?

## Approach
How did you use JD.Efcpt.Build?

## Results
Quantifiable improvements

## Lessons Learned
What would you do differently?
```
