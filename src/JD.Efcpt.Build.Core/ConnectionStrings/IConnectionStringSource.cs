namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// A pluggable source that can resolve a connection string from somewhere other than a
/// project-local file (appsettings.json/app.config) or an explicit MSBuild property - for
/// example an environment variable, Azure Key Vault, or AWS Secrets Manager.
/// </summary>
/// <remarks>
/// <para>
/// <b>Synchronous by design.</b> The connection-string resolution chain and the MSBuild tasks
/// that drive it are synchronous, so <see cref="Resolve"/> is synchronous too. Implementations
/// backed by an async-only SDK should call <c>.GetAwaiter().GetResult()</c> internally.
/// </para>
/// <para>
/// <b>Must apply a bounded timeout.</b> Network-backed implementations (Azure Key Vault, AWS
/// Secrets Manager, etc.) must apply an internal timeout of roughly 30 seconds so that an
/// unreachable store fails closed with <see cref="ConnectionStringSourceOutcome.Failed"/>
/// (surfaced as error <c>JD0030</c>) rather than hanging the build indefinitely.
/// </para>
/// <para>
/// <b>Fail-closed.</b> A source must never silently return an empty/null connection string as
/// though it were <see cref="ConnectionStringSourceOutcome.Found"/>; every failure mode maps to
/// an explicit <see cref="ConnectionStringSourceOutcome"/> so the resolution chain can throw an
/// actionable, JD-coded exception instead of continuing with a bad connection string.
/// </para>
/// </remarks>
public interface IConnectionStringSource
{
    /// <summary>
    /// Gets the stable key that identifies this source (for example <c>env</c>,
    /// <c>azure-keyvault</c>, <c>aws-secrets</c>). Matched against the
    /// <c>EfcptConnectionStringSource</c> MSBuild property.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the priority of this source when multiple sources could apply. Lower values are
    /// higher priority. Currently only a single source is selected at a time (via
    /// <c>EfcptConnectionStringSource</c>), but this is retained for future multi-source
    /// resolution strategies.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to resolve a connection string using <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The resolution context, including settings and offline state.</param>
    /// <returns>
    /// A <see cref="ConnectionStringSourceResult"/> describing the outcome. Implementations must
    /// never throw for expected failure modes (secret not found, offline, misconfigured, etc.) -
    /// those must be mapped to the corresponding <see cref="ConnectionStringSourceOutcome"/>
    /// instead. Only truly unexpected failures (a bug) should propagate as exceptions.
    /// </returns>
    ConnectionStringSourceResult Resolve(in ConnectionStringSourceContext context);
}
