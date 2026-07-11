using JD.Efcpt.Build.Core.Logging;

namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// Context passed to an <see cref="IConnectionStringSource"/> when resolving a connection
/// string.
/// </summary>
/// <param name="SourceKey">
/// The key of the source being invoked (matches <see cref="IConnectionStringSource.Key"/>).
/// </param>
/// <param name="Settings">
/// Source-specific settings (for example <c>keyVaultUri</c>, <c>secretName</c>, <c>region</c>,
/// <c>envVar</c>). Never <see langword="null"/>; sources should treat a missing key the same as
/// an absent/unset setting.
/// </param>
/// <param name="Offline">
/// <see langword="true"/> when the build is running in offline mode. Network-backed sources
/// must check this before making any network call and return
/// <see cref="ConnectionStringSourceOutcome.OfflineBlocked"/> if it is <see langword="true"/>.
/// </param>
/// <param name="Log">The build log to use for diagnostic output.</param>
public readonly record struct ConnectionStringSourceContext(
    string SourceKey,
    IReadOnlyDictionary<string, string> Settings,
    bool Offline,
    IBuildLog Log
);
