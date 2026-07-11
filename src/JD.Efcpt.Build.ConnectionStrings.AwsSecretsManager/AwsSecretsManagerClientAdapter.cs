using Amazon;
using Amazon.Runtime;
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
/// request-for-request equivalent to the classic API. Response interpretation lives in the
/// testable static <see cref="ExtractSecretString"/> so error-code discrimination and binary
/// handling can be unit tested against a mocked <see cref="BatchGetSecretValueResponse"/> without
/// a real AWS account.
/// </remarks>
internal sealed class AwsSecretsManagerClientAdapter : ISecretsManagerClient
{
    private readonly Lazy<IAmazonSecretsManager> _client;

    /// <summary>
    /// Initializes the adapter for the given <paramref name="region"/>, constructing a real
    /// <see cref="AmazonSecretsManagerClient"/> lazily on first use.
    /// </summary>
    public AwsSecretsManagerClientAdapter(RegionEndpoint region)
        : this(() => new AmazonSecretsManagerClient(region))
    {
    }

    /// <summary>
    /// Testability seam: initializes the adapter with an injectable
    /// <see cref="IAmazonSecretsManager"/> factory so the request/response path can be exercised
    /// without a real AWS client.
    /// </summary>
    internal AwsSecretsManagerClientAdapter(Func<IAmazonSecretsManager> clientFactory)
    {
        _client = new Lazy<IAmazonSecretsManager>(clientFactory);
    }

    /// <inheritdoc />
    public string GetSecretValue(string secretId, CancellationToken cancellationToken)
    {
        var request = new BatchGetSecretValueRequest
        {
            SecretIdList = [secretId]
        };

        var response = _client.Value.BatchGetSecretValueAsync(request, cancellationToken).GetAwaiter().GetResult();

        return ExtractSecretString(secretId, response);
    }

    /// <summary>
    /// The AWS <c>ResourceNotFoundException</c> error code, as returned in a batch
    /// <see cref="APIErrorType.ErrorCode"/> when a requested secret does not exist.
    /// </summary>
    private const string ResourceNotFoundErrorCode = "ResourceNotFoundException";

    /// <summary>
    /// Interprets a <see cref="BatchGetSecretValueResponse"/> for a single-secret request,
    /// returning the secret's <c>SecretString</c> or throwing an exception that
    /// <see cref="AwsSecretsManagerConnectionStringSource"/> maps to the correct
    /// <c>IConnectionStringSource</c> outcome.
    /// </summary>
    /// <remarks>
    /// Error discrimination is the whole point of this method: a batch <see cref="APIErrorType"/>
    /// with error code <c>ResourceNotFoundException</c> is a genuine "secret not found" and throws
    /// <see cref="ResourceNotFoundException"/> (mapped to <c>NotFound</c>/JD0031); every other
    /// error code (AccessDenied, DecryptionFailure, ThrottlingException, InvalidParameter, etc.)
    /// throws a generic <see cref="AmazonServiceException"/> carrying the error code and message
    /// (mapped to <c>Failed</c>/JD0030) so an authorization or transient failure is never
    /// misreported as "secret not found".
    /// </remarks>
    /// <exception cref="ResourceNotFoundException">The secret was not found.</exception>
    /// <exception cref="AmazonServiceException">Any other batch error (auth, decryption, throttling, etc.).</exception>
    /// <exception cref="NotSupportedException">The secret has only a binary value (no <c>SecretString</c>).</exception>
    internal static string ExtractSecretString(string secretId, BatchGetSecretValueResponse response)
    {
        var error = response.Errors?.Find(e => string.Equals(e.SecretId, secretId, StringComparison.Ordinal))
                    ?? response.Errors?.Find(_ => true);
        if (error is not null)
        {
            var message = $"{error.ErrorCode}: {error.Message}";
            throw string.Equals(error.ErrorCode, ResourceNotFoundErrorCode, StringComparison.Ordinal)
                ? new ResourceNotFoundException(message)
                : new AmazonServiceException(message);
        }

        var entry = response.SecretValues?.Find(v => string.Equals(v.Name, secretId, StringComparison.Ordinal) || string.Equals(v.ARN, secretId, StringComparison.Ordinal))
                    ?? response.SecretValues?.Find(_ => true);
        if (entry is null)
            throw new ResourceNotFoundException($"Secret '{secretId}' was not returned by BatchGetSecretValue.");

        if (entry.SecretString is null)
        {
            if (entry.SecretBinary is not null)
            {
                throw new NotSupportedException(
                    $"Secret '{secretId}' is stored as a binary value (SecretBinary). Connection strings must be stored as a plain-text SecretString.");
            }

            throw new ResourceNotFoundException($"Secret '{secretId}' was returned with neither a SecretString nor a SecretBinary value.");
        }

        return entry.SecretString;
    }
}
