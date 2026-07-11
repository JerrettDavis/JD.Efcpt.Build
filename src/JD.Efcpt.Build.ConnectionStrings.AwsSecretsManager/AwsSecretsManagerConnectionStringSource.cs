using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager.Model;
using JD.Efcpt.Build.Core.ConnectionStrings;

namespace JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager;

/// <summary>
/// An <see cref="IConnectionStringSource"/> that resolves the connection string from an AWS
/// Secrets Manager secret.
/// </summary>
/// <remarks>
/// <para>
/// Reads <c>Settings["secretId"]</c> (required, name or ARN), <c>Settings["region"]</c>
/// (required, AWS region system name e.g. <c>us-east-1</c>), and optionally
/// <c>Settings["secretJsonKey"]</c> - populated from the
/// <c>EfcptAwsSecretId</c>/<c>EfcptAwsRegion</c>/<c>EfcptAwsSecretJsonKey</c> MSBuild properties
/// by <c>ResolveSqlProjAndInputs</c>. When <c>secretJsonKey</c> is set, the secret's raw value is
/// parsed as a JSON object and the named field is extracted as the connection string; otherwise
/// the secret's raw string value is used as-is.
/// </para>
/// <para>
/// Fail-closed: offline mode is checked <b>before</b> any <see cref="ISecretsManagerClient"/> is
/// constructed (never touches the network when offline); missing/invalid settings map to
/// <see cref="ConnectionStringSourceOutcome.Misconfigured"/>; a missing secret maps to
/// <see cref="ConnectionStringSourceOutcome.NotFound"/>; authentication/throttling/other AWS
/// errors and any other unexpected error map to <see cref="ConnectionStringSourceOutcome.Failed"/>.
/// A bounded ~30s timeout ensures an unreachable/misconfigured account fails closed instead of
/// hanging the build.
/// </para>
/// </remarks>
public sealed class AwsSecretsManagerConnectionStringSource : IConnectionStringSource
{
    /// <summary>The bounded timeout applied to the Secrets Manager request.</summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly Func<RegionEndpoint, ISecretsManagerClient> _clientFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="AwsSecretsManagerConnectionStringSource"/> using
    /// the production <see cref="AwsSecretsManagerClientAdapter"/>. Required for satellite
    /// discovery, which instantiates this type via its parameterless constructor - see
    /// <c>SatelliteConnectionStringSourceResolver.CreateSourceInstance</c>.
    /// </summary>
    public AwsSecretsManagerConnectionStringSource()
        : this(region => new AwsSecretsManagerClientAdapter(region))
    {
    }

    /// <summary>
    /// Testability seam: initializes a new instance with an injectable
    /// <see cref="ISecretsManagerClient"/> factory, so tests can substitute a fake client without
    /// a real AWS account.
    /// </summary>
    internal AwsSecretsManagerConnectionStringSource(Func<RegionEndpoint, ISecretsManagerClient> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    /// <inheritdoc />
    public string Key => "aws-secrets";

    /// <inheritdoc />
    public int Priority => 10;

    /// <inheritdoc />
    public ConnectionStringSourceResult Resolve(in ConnectionStringSourceContext context)
    {
        if (!context.Settings.TryGetValue("secretId", out var secretId) || string.IsNullOrWhiteSpace(secretId))
        {
            return ConnectionStringSourceResult.Misconfigured(Key,
                "Missing required setting 'secretId' - set EfcptAwsSecretId to the secret's name or ARN.");
        }

        if (!context.Settings.TryGetValue("region", out var regionName) || string.IsNullOrWhiteSpace(regionName))
        {
            return ConnectionStringSourceResult.Misconfigured(Key,
                "Missing required setting 'region' - set EfcptAwsRegion to the AWS region containing the secret (e.g. us-east-1).");
        }

        RegionEndpoint region;
        try
        {
            region = RegionEndpoint.GetBySystemName(regionName);
        }
        catch (Exception ex)
        {
            return ConnectionStringSourceResult.Misconfigured(Key,
                $"Setting 'region' ('{regionName}') is not a valid AWS region: {ex.Message}");
        }

        // Offline check happens BEFORE constructing any client - this is a network-backed
        // source, so it must never attempt a request while offline.
        if (context.Offline)
        {
            return ConnectionStringSourceResult.OfflineBlocked(Key,
                "AWS Secrets Manager is network-backed; offline mode (EfcptOfflineMode/EFCPT_OFFLINE) blocked it before any request was made.");
        }

        context.Settings.TryGetValue("secretJsonKey", out var secretJsonKey);

        using var cts = new CancellationTokenSource(RequestTimeout);
        string rawValue;
        try
        {
            var client = _clientFactory(region);
            rawValue = client.GetSecretValue(secretId, cts.Token);
        }
        catch (ResourceNotFoundException ex)
        {
            // ResourceNotFoundException is a subclass of AmazonServiceException, so this specific
            // catch MUST precede the AmazonServiceException catch below: only a genuine
            // "secret not found" (never an auth/decryption/throttling failure) maps to NotFound.
            return ConnectionStringSourceResult.NotFound(Key, $"Secret '{secretId}' was not found in region '{regionName}'. {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            // The adapter signals an unsupported secret shape (e.g. a binary-only secret) via
            // NotSupportedException - a user misconfiguration, not a transient service failure.
            return ConnectionStringSourceResult.Misconfigured(Key, $"Secret '{secretId}' in region '{regionName}' cannot be used: {ex.Message}");
        }
        catch (AmazonServiceException ex)
        {
            return ConnectionStringSourceResult.Failed(Key, $"AWS Secrets Manager request failed for secret '{secretId}' in region '{regionName}': {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return ConnectionStringSourceResult.Failed(Key, $"Timed out after {RequestTimeout.TotalSeconds:F0}s resolving secret '{secretId}' from region '{regionName}'.");
        }
        catch (Exception ex)
        {
            return ConnectionStringSourceResult.Failed(Key, $"Failed to resolve secret '{secretId}' from region '{regionName}': {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(rawValue))
            return ConnectionStringSourceResult.NotFound(Key, $"Secret '{secretId}' in region '{regionName}' has an empty value.");

        if (string.IsNullOrWhiteSpace(secretJsonKey))
            return ConnectionStringSourceResult.Found(Key, rawValue);

        return ExtractJsonField(rawValue, secretJsonKey!, secretId, regionName);
    }

    private ConnectionStringSourceResult ExtractJsonField(string rawValue, string jsonKey, string secretId, string regionName)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawValue);
        }
        catch (JsonException ex)
        {
            return ConnectionStringSourceResult.Failed(Key,
                $"secretJsonKey='{jsonKey}' was set, but secret '{secretId}' in region '{regionName}' is not valid JSON: {ex.Message}");
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty(jsonKey, out var element) || element.ValueKind != JsonValueKind.String)
            {
                return ConnectionStringSourceResult.NotFound(Key,
                    $"secretJsonKey='{jsonKey}' was not found (or is not a string) in secret '{secretId}' in region '{regionName}'.");
            }

            var value = element.GetString();
            return string.IsNullOrWhiteSpace(value)
                ? ConnectionStringSourceResult.NotFound(Key, $"secretJsonKey='{jsonKey}' in secret '{secretId}' has an empty value.")
                : ConnectionStringSourceResult.Found(Key, value!);
        }
    }
}
