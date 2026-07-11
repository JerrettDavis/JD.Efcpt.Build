# Connection-String Sources

JD.Efcpt.Build can resolve the build-time database connection string from a **pluggable
source** instead of a project-local file (`appsettings.json`/`app.config`) or an explicit
`EfcptConnectionString` value. This lets you keep real credentials out of source control and
out of MSBuild logs entirely, pulling them from an environment variable, Azure Key Vault, or
AWS Secrets Manager at build time.

## Overview

Set `EfcptConnectionStringSource` to a source key to opt in:

```xml
<PropertyGroup>
  <EfcptConnectionStringSource>env</EfcptConnectionStringSource>
</PropertyGroup>
```

| Source key | Package | Network-backed |
|---|---|---|
| `env` | Built into `JD.Efcpt.Build` (no extra install) | No |
| `azure-keyvault` | `JD.Efcpt.Build.ConnectionStrings.AzureKeyVault` | Yes |
| `aws-secrets` | `JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager` | Yes |

When `EfcptConnectionStringSource` is **empty** (the default), behavior is unchanged from
today: the existing `EfcptConnectionString` / `EfcptAppSettings` / `EfcptAppConfig` /
auto-discovered file / `.sqlproj` resolution chain runs exactly as documented in
[Connection String Mode](connection-string-mode.md).

## Fail-closed semantics

**Selecting a source is an all-or-nothing switch.** Once `EfcptConnectionStringSource` is set,
the build resolves the connection string from that source *only* - it never silently falls
back to a file or `.sqlproj` if the source fails. If the source can't produce a connection
string for any reason, the build fails with an actionable, JD-coded error (see
[Error Codes](#error-codes) below) instead of continuing with a stale or wrong connection
string.

This is deliberate: once you've opted into a secret store, a silent fallback to a local file
would be a worse outcome than a loud failure - it could mean the build quietly used a
developer's local database instead of the one your secret store actually points at.

## Offline-gated

`azure-keyvault` and `aws-secrets` are network-backed. When offline mode is enabled
(`EfcptOfflineMode=true` or the `EFCPT_OFFLINE` environment variable - see
[Offline / Air-Gapped Builds](offline.md)), both sources are blocked *before* any network
request is attempted - the build fails with `JD0032` rather than hanging or intermittently
timing out. The `env` source is never network-backed and always works offline.

**For air-gapped or secure builds, use the `env` source** - pre-provision the connection
string into an environment variable ahead of time, and offline builds can still resolve it
without any network access.

## The `env` source

Reads the connection string from an environment variable. Ships in the core package - no
extra install needed.

```xml
<PropertyGroup>
  <EfcptConnectionStringSource>env</EfcptConnectionStringSource>
  <!-- Optional: defaults to EFCPT_CONNECTION_STRING -->
  <EfcptConnectionStringEnvVar>MY_APP_CONNECTION_STRING</EfcptConnectionStringEnvVar>
</PropertyGroup>
```

```bash
set EFCPT_CONNECTION_STRING=Server=localhost;Database=MyDb;Integrated Security=True;
dotnet build
```

If the variable is unset or empty, the build fails with `JD0031`.

## Azure Key Vault

```bash
dotnet add package JD.Efcpt.Build.ConnectionStrings.AzureKeyVault
```

```xml
<PropertyGroup>
  <EfcptConnectionStringSource>azure-keyvault</EfcptConnectionStringSource>
  <EfcptKeyVaultUri>https://my-vault.vault.azure.net/</EfcptKeyVaultUri>
  <EfcptKeyVaultSecretName>MyAppConnectionString</EfcptKeyVaultSecretName>
  <!-- Optional: pin a specific secret version -->
  <EfcptKeyVaultSecretVersion></EfcptKeyVaultSecretVersion>
</PropertyGroup>
```

Authentication uses `DefaultAzureCredential` (the standard Azure SDK credential chain -
environment variables, managed identity, Visual Studio/Azure CLI login, etc.). No credentials
are configured through JD.Efcpt.Build properties; use whichever `DefaultAzureCredential` leg
fits your environment (a managed identity on a build agent, `az login` locally, etc.).

**Least privilege:** grant the build identity only the **Get** permission on secrets for the
specific vault (Key Vault RBAC role `Key Vault Secrets User`, or a legacy access policy with
only `Get` under Secret permissions). No `List`, `Set`, or `Delete` access is needed.

## AWS Secrets Manager

```bash
dotnet add package JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager
```

```xml
<PropertyGroup>
  <EfcptConnectionStringSource>aws-secrets</EfcptConnectionStringSource>
  <EfcptAwsSecretId>my-app/connection-string</EfcptAwsSecretId>
  <EfcptAwsRegion>us-east-1</EfcptAwsRegion>
  <!-- Optional: the secret value is a JSON object - extract this field as the connection string -->
  <EfcptAwsSecretJsonKey>connectionString</EfcptAwsSecretJsonKey>
</PropertyGroup>
```

Authentication uses the AWS SDK's default credential provider chain (environment variables,
shared credentials file, an EC2/ECS/Lambda IAM role, etc.). No credentials are configured
through JD.Efcpt.Build properties.

**Least privilege:** grant the build identity only `secretsmanager:GetSecretValue` and
`secretsmanager:BatchGetSecretValue`, scoped to the specific secret's ARN:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:BatchGetSecretValue"
      ],
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789012:secret:my-app/connection-string-??????"
    }
  ]
}
```

> **Note:** this satellite retrieves the secret via the AWS `BatchGetSecretValue` API (current
> AWS SDK releases no longer expose a single-secret `GetSecretValue` client method), so the
> `secretsmanager:BatchGetSecretValue` action is required. `secretsmanager:GetSecretValue` is
> listed alongside it for forward compatibility.

## Property reference

| Property | Applies to | Description |
|---|---|---|
| `EfcptConnectionStringSource` | All | Source key: `env`, `azure-keyvault`, or `aws-secrets`. Empty = disabled (default). |
| `EfcptConnectionStringEnvVar` | `env` | Environment variable name. Defaults to `EFCPT_CONNECTION_STRING`. |
| `EfcptKeyVaultUri` | `azure-keyvault` | Required. The vault's URI. |
| `EfcptKeyVaultSecretName` | `azure-keyvault` | Required. The secret's name. |
| `EfcptKeyVaultSecretVersion` | `azure-keyvault` | Optional. Pins a specific secret version. |
| `EfcptAwsSecretId` | `aws-secrets` | Required. The secret's name or ARN. |
| `EfcptAwsRegion` | `aws-secrets` | Required. The AWS region containing the secret. |
| `EfcptAwsSecretJsonKey` | `aws-secrets` | Optional. Extracts one field from a JSON secret value. |
| `EfcptConnectionStringSourceSearchPath` (item) | `azure-keyvault`, `aws-secrets` | Additional directories to search for a satellite source assembly; populated automatically when you `dotnet add package` the satellite. |

## Fail-closed + offline matrix

| Outcome | Error code | Meaning |
|---|---|---|
| Secret/variable not found | `JD0031` | The store was reached (or, for `env`, the variable was checked) but no value was present. |
| Source resolution failed | `JD0030` | The source threw an unexpected exception (auth failure, timeout, transient error, etc.). |
| Offline blocked | `JD0032` | A network-backed source (`azure-keyvault`, `aws-secrets`) was blocked by offline mode before any request was made. |
| Satellite not installed | `JD0033` | `EfcptConnectionStringSource` names a satellite key but the matching package isn't installed. |
| Misconfigured | `JD0034` | Required settings are missing or invalid (vault URI, secret name, region, secret id). |

See [Error Codes](error-codes.md#connection-string-source-errors-jd0030-jd0039) for full
details and resolution steps for each code.

## Security notes

- The resolved connection string is always excluded from build profiling output
  (`[ProfileOutput(Exclude = true)]`), regardless of source.
- When `DumpResolvedInputs=true` writes `resolved-inputs.json` for debugging, a connection
  string that came from a pluggable source is written as `(redacted: sourced from <key>)`
  instead of its raw value.
- No source ever logs the resolved connection string value itself, only diagnostic context
  (secret name, vault URI, region) needed to troubleshoot a failure.
