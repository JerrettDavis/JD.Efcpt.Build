namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// Thrown by <see cref="ConnectionStringResolutionChain"/> when a pluggable
/// <see cref="IConnectionStringSource"/> was explicitly selected (via
/// <see cref="ConnectionStringResolutionContext.ConnectionStringSource"/>) and resolution did
/// not succeed. Carries a JD-coded <see cref="Code"/> (<c>JD0030</c>-<c>JD0034</c>) and an
/// actionable message, analogous to
/// <c>JD.Efcpt.Build.Tasks.Schema.ProviderDriverNotFoundException</c> for provider drivers.
/// </summary>
/// <remarks>
/// Once a connection-string source is explicitly selected, resolution is fail-closed: any
/// non-<see cref="ConnectionStringSourceOutcome.Found"/> outcome throws this exception instead
/// of silently falling through to file-based or <c>.sqlproj</c> resolution. See
/// <c>docs/user-guide/connection-string-sources.md</c>.
/// </remarks>
public sealed class ConnectionStringSourceException : Exception
{
    /// <summary>Error code JD0030: the source threw or otherwise failed unexpectedly.</summary>
    public const string SourceResolutionFailedCode = "JD0030";

    /// <summary>Error code JD0031: the source was reached, but the secret/value was not found.</summary>
    public const string SecretNotFoundCode = "JD0031";

    /// <summary>Error code JD0032: offline mode blocked a network-backed source.</summary>
    public const string OfflineBlockedCode = "JD0032";

    /// <summary>Error code JD0033: the satellite package for the selected source is not installed.</summary>
    public const string SourceNotInstalledCode = "JD0033";

    /// <summary>Error code JD0034: the selected source is missing required settings.</summary>
    public const string SourceMisconfiguredCode = "JD0034";

    /// <summary>
    /// Gets the JD-coded error code (one of <see cref="SourceResolutionFailedCode"/>,
    /// <see cref="SecretNotFoundCode"/>, <see cref="OfflineBlockedCode"/>,
    /// <see cref="SourceNotInstalledCode"/>, or <see cref="SourceMisconfiguredCode"/>).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the connection-string source key that was selected (for example <c>azure-keyvault</c>).
    /// </summary>
    public string SourceKey { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionStringSourceException"/>.
    /// </summary>
    /// <param name="code">The JD-coded error code.</param>
    /// <param name="sourceKey">The connection-string source key that was selected.</param>
    /// <param name="message">The actionable error message.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ConnectionStringSourceException(string code, string sourceKey, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        SourceKey = sourceKey;
    }

    /// <summary>
    /// Builds the <see cref="SourceNotInstalledCode"/> (JD0033) exception for a source key with
    /// no matching bundled or satellite <see cref="IConnectionStringSource"/>, including the
    /// <c>dotnet add package</c> guidance for known first-party satellites.
    /// </summary>
    /// <param name="sourceKey">The unresolved connection-string source key.</param>
    public static ConnectionStringSourceException SourceNotInstalled(string sourceKey)
    {
        var packageId = KnownSatellitePackagesByKey.TryGetValue(sourceKey, out var id) ? id : null;
        var installInstruction = packageId is not null
            ? $"Install it with: dotnet add package {packageId}"
            : $"No built-in or satellite connection-string source is registered for key '{sourceKey}'. " +
              "Check EfcptConnectionStringSource for typos, or install the satellite package that provides it.";

        return new ConnectionStringSourceException(
            SourceNotInstalledCode,
            sourceKey,
            $"Connection-string source '{sourceKey}' is not available. {installInstruction} " +
            "See https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/connection-string-sources.html for details.");
    }

    /// <summary>
    /// Maps known first-party connection-string source keys to their satellite package id, for
    /// use in <see cref="SourceNotInstalled"/>'s actionable message.
    /// </summary>
    public static IReadOnlyDictionary<string, string> KnownSatellitePackagesByKey { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["azure-keyvault"] = "JD.Efcpt.Build.ConnectionStrings.AzureKeyVault",
            ["aws-secrets"] = "JD.Efcpt.Build.ConnectionStrings.AwsSecretsManager"
        };
}
