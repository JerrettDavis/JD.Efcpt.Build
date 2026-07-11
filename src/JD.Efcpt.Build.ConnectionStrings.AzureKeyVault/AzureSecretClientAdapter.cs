using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace JD.Efcpt.Build.ConnectionStrings.AzureKeyVault;

/// <summary>
/// Production <see cref="ISecretClient"/> implementation wrapping
/// <see cref="SecretClient"/>, authenticated via <see cref="DefaultAzureCredential"/>.
/// </summary>
internal sealed class AzureSecretClientAdapter(Uri vaultUri) : ISecretClient
{
    private readonly Lazy<SecretClient> _client = new(() => new SecretClient(vaultUri, new DefaultAzureCredential()));

    /// <inheritdoc />
    public string GetSecretValue(string secretName, string? version, CancellationToken cancellationToken)
    {
        var response = string.IsNullOrEmpty(version)
            ? _client.Value.GetSecret(secretName, cancellationToken: cancellationToken)
            : _client.Value.GetSecret(secretName, version, cancellationToken);

        return response.Value.Value;
    }
}
