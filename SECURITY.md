# Security Policy

## Supported Versions

Security updates are provided for the latest minor version of JD.Efcpt.Build.

| Version | Supported          |
|---------|--------------------|
| 0.15.x  | Yes (latest)       |
| 0.14.x  | No                 |
| < 0.14  | No                 |

## Reporting a Vulnerability

If you discover a security vulnerability in JD.Efcpt.Build, please report it responsibly by using GitHub's private vulnerability reporting feature:

1. Navigate to the [Security](https://github.com/JerrettDavis/JD.Efcpt.Build/security) tab on the repository
2. Click on **Advisories** and then **Report a vulnerability**
3. Provide a clear description of the vulnerability, including:
   - The affected version(s)
   - Steps to reproduce (if applicable)
   - Potential impact
   - Any suggested fixes

Do not open public issues for security vulnerabilities.

## Security Response Expectation

- Initial response: We aim to acknowledge reports within 5 business days
- Patch release: Within 1 week for critical vulnerabilities
- Public disclosure: After a patch is available or 90 days, whichever comes first

## Security Considerations

When using JD.Efcpt.Build:

- **Connection Strings**: Keep database connection strings secure (use environment variables or secure configuration, never commit to source control)
- **Configuration Files**: Protect `efcpt-config.json` if it contains sensitive information
- **Dependencies**: Keep your .NET SDK and NuGet dependencies up to date
- **CI/CD Secrets**: When running in CI/CD pipelines, ensure database credentials are stored securely as secrets

## Security Best Practices

- Use [GitHub's encrypted secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets) for sensitive data in GitHub Actions
- Use [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/) or similar secure configuration providers in production
- Regularly review and update your dependencies using `dotnet list package --outdated` or NuGet security advisories
- Follow the [OWASP Top 10](https://owasp.org/Top10/) security guidelines in your applications

## License

This security policy applies to JD.Efcpt.Build and is provided under the terms of the [MIT License](LICENSE).
