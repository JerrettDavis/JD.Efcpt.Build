namespace JD.Efcpt.Build.ConnectionStrings.AzureKeyVault;

/// <summary>
/// Thin seam over the Azure Key Vault secrets SDK so
/// <see cref="AzureKeyVaultConnectionStringSource"/> can be unit tested without a real Key Vault.
/// </summary>
/// <remarks>
/// The production implementation (<see cref="AzureSecretClientAdapter"/>) wraps
/// <c>Azure.Security.KeyVault.Secrets.SecretClient</c> authenticated via
/// <c>Azure.Identity.DefaultAzureCredential</c>. Tests substitute a fake implementation to
/// assert the exact secret-name/version request and to simulate SDK exceptions without any
/// network access.
/// </remarks>
public interface ISecretClient
{
    /// <summary>
    /// Retrieves the value of the secret named <paramref name="secretName"/>, optionally at a
    /// specific <paramref name="version"/>.
    /// </summary>
    /// <param name="secretName">The secret name.</param>
    /// <param name="version">The optional secret version; <see langword="null"/> or empty for the latest version.</param>
    /// <param name="cancellationToken">Cancellation token honoring the source's bounded timeout.</param>
    /// <returns>The secret's string value.</returns>
    string GetSecretValue(string secretName, string? version, CancellationToken cancellationToken);
}
