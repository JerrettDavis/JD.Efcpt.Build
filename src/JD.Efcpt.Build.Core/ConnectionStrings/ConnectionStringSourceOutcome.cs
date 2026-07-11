namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// The outcome of a single <see cref="IConnectionStringSource"/> resolution attempt.
/// </summary>
public enum ConnectionStringSourceOutcome
{
    /// <summary>
    /// The source was not selected/configured for this resolution attempt.
    /// </summary>
    NotConfigured,

    /// <summary>
    /// The source successfully resolved a connection string.
    /// </summary>
    Found,

    /// <summary>
    /// The source was reached (e.g. the secret store responded) but no matching value was
    /// present, or an explicitly selected env-var source was unset/empty.
    /// </summary>
    NotFound,

    /// <summary>
    /// The source failed unexpectedly - an unhandled exception, an authentication failure, a
    /// timeout, or any other error that isn't cleanly one of the other outcomes.
    /// </summary>
    Failed,

    /// <summary>
    /// The source is network-backed and offline mode blocked it before any network access was
    /// attempted.
    /// </summary>
    OfflineBlocked,

    /// <summary>
    /// The source's required settings (for example a vault URI, secret name, or region) are
    /// missing or invalid, so the source could not even attempt resolution.
    /// </summary>
    Misconfigured
}
