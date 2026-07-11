using Azure;
using JD.Efcpt.Build.Core.ConnectionStrings;

namespace JD.Efcpt.Build.ConnectionStrings.AzureKeyVault;

/// <summary>
/// An <see cref="IConnectionStringSource"/> that resolves the connection string from an Azure
/// Key Vault secret.
/// </summary>
/// <remarks>
/// <para>
/// Reads <c>Settings["keyVaultUri"]</c> (required), <c>Settings["secretName"]</c> (required),
/// and optionally <c>Settings["secretVersion"]</c> - populated from the
/// <c>EfcptKeyVaultUri</c>/<c>EfcptKeyVaultSecretName</c>/<c>EfcptKeyVaultSecretVersion</c>
/// MSBuild properties by <c>ResolveSqlProjAndInputs</c>.
/// </para>
/// <para>
/// Fail-closed: offline mode is checked <b>before</b> any <see cref="ISecretClient"/> is
/// constructed (never touches the network when offline); missing/invalid settings map to
/// <see cref="ConnectionStringSourceOutcome.Misconfigured"/>; a 404 from Key Vault maps to
/// <see cref="ConnectionStringSourceOutcome.NotFound"/>; authentication failures and any other
/// unexpected error map to <see cref="ConnectionStringSourceOutcome.Failed"/>. A bounded ~30s
/// timeout ensures an unreachable vault fails closed instead of hanging the build.
/// </para>
/// </remarks>
public sealed class AzureKeyVaultConnectionStringSource : IConnectionStringSource
{
    /// <summary>The bounded timeout applied to the Key Vault request.</summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly Func<Uri, ISecretClient> _clientFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="AzureKeyVaultConnectionStringSource"/> using the
    /// production <see cref="AzureSecretClientAdapter"/>. Required for satellite discovery, which
    /// instantiates this type via its parameterless constructor - see
    /// <c>SatelliteConnectionStringSourceResolver.CreateSourceInstance</c>.
    /// </summary>
    public AzureKeyVaultConnectionStringSource()
        : this(uri => new AzureSecretClientAdapter(uri))
    {
    }

    /// <summary>
    /// Testability seam: initializes a new instance with an injectable <see cref="ISecretClient"/>
    /// factory, so tests can substitute a fake client without a real Key Vault.
    /// </summary>
    internal AzureKeyVaultConnectionStringSource(Func<Uri, ISecretClient> clientFactory)
    {
        _clientFactory = clientFactory;
    }

    /// <inheritdoc />
    public string Key => "azure-keyvault";

    /// <inheritdoc />
    public int Priority => 10;

    /// <inheritdoc />
    public ConnectionStringSourceResult Resolve(in ConnectionStringSourceContext context)
    {
        if (!context.Settings.TryGetValue("keyVaultUri", out var vaultUriValue) || string.IsNullOrWhiteSpace(vaultUriValue))
        {
            return ConnectionStringSourceResult.Misconfigured(Key,
                "Missing required setting 'keyVaultUri' - set EfcptKeyVaultUri to the vault's URI (e.g. https://myvault.vault.azure.net/).");
        }

        if (!context.Settings.TryGetValue("secretName", out var secretName) || string.IsNullOrWhiteSpace(secretName))
        {
            return ConnectionStringSourceResult.Misconfigured(Key,
                "Missing required setting 'secretName' - set EfcptKeyVaultSecretName to the name of the secret containing the connection string.");
        }

        if (!Uri.TryCreate(vaultUriValue, UriKind.Absolute, out var vaultUri))
        {
            return ConnectionStringSourceResult.Misconfigured(Key,
                $"Setting 'keyVaultUri' ('{vaultUriValue}') is not a valid absolute URI.");
        }

        // Offline check happens BEFORE constructing any client - this is a network-backed
        // source, so it must never attempt a request (or even build a credential chain that
        // might itself probe the network) while offline.
        if (context.Offline)
        {
            return ConnectionStringSourceResult.OfflineBlocked(Key,
                "Azure Key Vault is network-backed; offline mode (EfcptOfflineMode/EFCPT_OFFLINE) blocked it before any request was made.");
        }

        context.Settings.TryGetValue("secretVersion", out var secretVersion);

        using var cts = new CancellationTokenSource(RequestTimeout);
        try
        {
            var client = _clientFactory(vaultUri);
            var value = client.GetSecretValue(secretName, secretVersion, cts.Token);

            return string.IsNullOrWhiteSpace(value)
                ? ConnectionStringSourceResult.NotFound(Key, $"Secret '{secretName}' in vault '{vaultUri}' has an empty value.")
                : ConnectionStringSourceResult.Found(Key, value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ConnectionStringSourceResult.NotFound(Key, $"Secret '{secretName}' was not found in vault '{vaultUri}'. {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            return ConnectionStringSourceResult.Failed(Key, $"Azure Key Vault request failed (status {ex.Status}) for secret '{secretName}' in vault '{vaultUri}': {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return ConnectionStringSourceResult.Failed(Key, $"Timed out after {RequestTimeout.TotalSeconds:F0}s resolving secret '{secretName}' from vault '{vaultUri}'.");
        }
        catch (Exception ex)
        {
            return ConnectionStringSourceResult.Failed(Key, $"Failed to resolve secret '{secretName}' from vault '{vaultUri}': {ex.Message}");
        }
    }
}
