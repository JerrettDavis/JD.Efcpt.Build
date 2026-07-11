namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// Represents the result of attempting to resolve a connection string from an
/// <see cref="IConnectionStringSource"/>.
/// </summary>
/// <remarks>
/// This type is distinct from <see cref="ConnectionStringResult"/>, which is specific to the
/// file-based (appsettings.json/app.config) resolution chain. Use the static factory methods
/// to construct instances rather than the implicit constructor.
/// </remarks>
public sealed record ConnectionStringSourceResult
{
    /// <summary>
    /// Gets the outcome of the resolution attempt.
    /// </summary>
    public ConnectionStringSourceOutcome Outcome { get; init; }

    /// <summary>
    /// Gets the resolved connection string, or <see langword="null"/> unless
    /// <see cref="Outcome"/> is <see cref="ConnectionStringSourceOutcome.Found"/>.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Gets the key of the <see cref="IConnectionStringSource"/> that produced this result.
    /// </summary>
    public string SourceKey { get; init; } = "";

    /// <summary>
    /// Gets an optional human-readable diagnostic message providing additional context about
    /// the outcome (for example, the underlying exception message).
    /// </summary>
    public string? Diagnostic { get; init; }

    /// <summary>
    /// Creates a result indicating that <paramref name="sourceKey"/> was not selected/configured.
    /// </summary>
    public static ConnectionStringSourceResult NotConfigured(string sourceKey)
        => new() { Outcome = ConnectionStringSourceOutcome.NotConfigured, SourceKey = sourceKey };

    /// <summary>
    /// Creates a result indicating that <paramref name="sourceKey"/> successfully resolved
    /// <paramref name="connectionString"/>.
    /// </summary>
    public static ConnectionStringSourceResult Found(string sourceKey, string connectionString)
        => new() { Outcome = ConnectionStringSourceOutcome.Found, SourceKey = sourceKey, ConnectionString = connectionString };

    /// <summary>
    /// Creates a result indicating that <paramref name="sourceKey"/> was reached but no
    /// matching value was found.
    /// </summary>
    public static ConnectionStringSourceResult NotFound(string sourceKey, string? diagnostic = null)
        => new() { Outcome = ConnectionStringSourceOutcome.NotFound, SourceKey = sourceKey, Diagnostic = diagnostic };

    /// <summary>
    /// Creates a result indicating that <paramref name="sourceKey"/> failed unexpectedly.
    /// </summary>
    public static ConnectionStringSourceResult Failed(string sourceKey, string? diagnostic = null)
        => new() { Outcome = ConnectionStringSourceOutcome.Failed, SourceKey = sourceKey, Diagnostic = diagnostic };

    /// <summary>
    /// Creates a result indicating that offline mode blocked <paramref name="sourceKey"/>
    /// before any network access was attempted.
    /// </summary>
    public static ConnectionStringSourceResult OfflineBlocked(string sourceKey, string? diagnostic = null)
        => new() { Outcome = ConnectionStringSourceOutcome.OfflineBlocked, SourceKey = sourceKey, Diagnostic = diagnostic };

    /// <summary>
    /// Creates a result indicating that <paramref name="sourceKey"/> is missing required
    /// settings and could not attempt resolution.
    /// </summary>
    public static ConnectionStringSourceResult Misconfigured(string sourceKey, string? diagnostic = null)
        => new() { Outcome = ConnectionStringSourceOutcome.Misconfigured, SourceKey = sourceKey, Diagnostic = diagnostic };
}
