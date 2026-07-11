using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager;

/// <summary>
/// Production <see cref="ISecretsManagerClient"/> implementation wrapping
/// <see cref="AmazonSecretsManagerClient"/>, authenticated via the AWS SDK's default credential
/// provider chain (environment variables, shared credentials file, IAM role, etc.).
/// </summary>
/// <remarks>
/// Uses <c>BatchGetSecretValueAsync</c> with a single-entry <c>SecretIdList</c> rather than the
/// classic <c>GetSecretValueAsync</c>: current AWSSDK.SecretsManager releases no longer expose a
/// single-secret <c>GetSecretValue</c> client method - <c>BatchGetSecretValue</c> (1-20 secrets
/// per call) is the only retrieval operation available. A single-entry request is
/// request-for-request equivalent to the classic API, and this adapter translates a "not found"
/// batch error back into <see cref="ResourceNotFoundException"/> so callers can treat it exactly
/// like the classic single-secret API would have.
/// </remarks>
internal sealed class AwsSecretsManagerClientAdapter(RegionEndpoint region) : ISecretsManagerClient
{
    private readonly Lazy<IAmazonSecretsManager> _client = new(() => new AmazonSecretsManagerClient(region));

    /// <inheritdoc />
    public string GetSecretValue(string secretId, CancellationToken cancellationToken)
    {
        var request = new BatchGetSecretValueRequest
        {
            SecretIdList = [secretId]
        };

        var response = _client.Value.BatchGetSecretValueAsync(request, cancellationToken).GetAwaiter().GetResult();

        var error = response.Errors?.Find(e => string.Equals(e.SecretId, secretId, StringComparison.Ordinal))
                    ?? response.Errors?.Find(_ => true);
        if (error is not null)
            throw new ResourceNotFoundException($"{error.ErrorCode}: {error.Message}");

        var entry = response.SecretValues?.Find(v => string.Equals(v.Name, secretId, StringComparison.Ordinal) || string.Equals(v.ARN, secretId, StringComparison.Ordinal))
                    ?? response.SecretValues?.Find(_ => true);
        if (entry is null)
            throw new ResourceNotFoundException($"Secret '{secretId}' was not returned by BatchGetSecretValue.");

        return entry.SecretString;
    }
}
