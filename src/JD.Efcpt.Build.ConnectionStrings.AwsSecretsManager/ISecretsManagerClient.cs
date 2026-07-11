namespace JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager;

/// <summary>
/// Thin seam over the AWS Secrets Manager SDK so
/// <see cref="AwsSecretsManagerConnectionStringSource"/> can be unit tested without a real AWS
/// account.
/// </summary>
/// <remarks>
/// The production implementation (<see cref="AwsSecretsManagerClientAdapter"/>) wraps
/// <c>Amazon.SecretsManager.AmazonSecretsManagerClient</c>, authenticated via the AWS SDK's
/// default credential provider chain. Tests substitute a fake implementation to assert the exact
/// secret-id request and to simulate SDK exceptions without any network access.
/// </remarks>
public interface ISecretsManagerClient
{
    /// <summary>
    /// Retrieves the raw string value of the secret identified by <paramref name="secretId"/>
    /// (name or ARN).
    /// </summary>
    /// <param name="secretId">The secret name or ARN.</param>
    /// <param name="cancellationToken">Cancellation token honoring the source's bounded timeout.</param>
    /// <returns>The secret's raw string value.</returns>
    /// <exception cref="Amazon.SecretsManager.Model.ResourceNotFoundException">
    /// Thrown when the secret does not exist.
    /// </exception>
    string GetSecretValue(string secretId, CancellationToken cancellationToken);
}
