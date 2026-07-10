namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Thrown when the ADO.NET driver for a normalized provider cannot be resolved to an
/// <see cref="IProviderAdapter"/>.
/// </summary>
/// <remarks>
/// <para>
/// In Phase 1, every provider driver is still bundled directly in this assembly, so this
/// exception is only reachable defensively (a normalized provider with no registered
/// adapter). Once drivers are extracted into satellite packages, this becomes the primary
/// error path when a project references a provider but hasn't installed the matching
/// package — the message below is written for that future, with an exact install command.
/// </para>
/// </remarks>
internal sealed class ProviderDriverNotFoundException : Exception
{
    /// <summary>
    /// The URL of the documentation page describing provider driver installation.
    /// </summary>
    private const string DocsUrl = "https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/provider-support.html";

    /// <summary>
    /// Maps each normalized provider name to the suffix of the satellite package that
    /// contains its driver (e.g. <c>postgres</c> → <c>PostgreSQL</c> installs as
    /// <c>JD.Efcpt.Build.PostgreSQL</c>). <c>mssql</c> maps to <c>null</c> because its
    /// driver is bundled with the core package and never needs a separate install.
    /// </summary>
    /// <remarks>
    /// This table is the single source of truth for the provider-to-package mapping and
    /// is intended to be reused by later phases when satellite packages are introduced.
    /// </remarks>
    public static IReadOnlyDictionary<string, string?> PackageSuffixesByProvider { get; } =
        new Dictionary<string, string?>
        {
            ["mssql"] = null,
            ["postgres"] = "PostgreSQL",
            ["mysql"] = "MySQL",
            ["sqlite"] = "Sqlite",
            ["oracle"] = "Oracle",
            ["firebird"] = "Firebird",
            ["snowflake"] = "Snowflake"
        };

    /// <summary>
    /// The normalized provider name whose driver could not be resolved.
    /// </summary>
    public string Provider { get; } = "";

    /// <summary>
    /// Initializes a new instance of <see cref="ProviderDriverNotFoundException"/> for the
    /// given normalized provider, building an actionable message with the exact
    /// <c>dotnet add package</c> command needed (when the provider ships as a satellite
    /// package) and a link to the provider support documentation.
    /// </summary>
    /// <param name="provider">The normalized provider name (e.g. <c>postgres</c>).</param>
    public ProviderDriverNotFoundException(string provider)
        : base(BuildMessage(provider))
    {
        Provider = provider;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ProviderDriverNotFoundException"/> for the given
    /// normalized provider, wrapping an underlying failure encountered while loading or
    /// instantiating that provider's satellite adapter assembly (e.g. a corrupt DLL, a missing
    /// parameterless constructor, or a type-load failure). <paramref name="innerException"/> is
    /// preserved as <see cref="Exception.InnerException"/> for diagnostics, while
    /// <see cref="Exception.Message"/> still surfaces the actionable <c>dotnet add package</c>
    /// guidance instead of the raw CLR error text - this is the primary reason this exception
    /// type exists once a provider's driver is truly a satellite package rather than merely
    /// missing.
    /// </summary>
    /// <param name="provider">The normalized provider name (e.g. <c>postgres</c>).</param>
    /// <param name="innerException">The underlying exception that caused resolution to fail.</param>
    public ProviderDriverNotFoundException(string provider, Exception innerException)
        : base(BuildMessage(provider), innerException)
    {
        Provider = provider;
    }

    private static string BuildMessage(string provider)
    {
        var hasSuffix = PackageSuffixesByProvider.TryGetValue(provider, out var suffix) && suffix is not null;

        var installInstruction = hasSuffix
            ? $"Install it with: dotnet add package JD.Efcpt.Build.{suffix}"
            : "It ships with the core JD.Efcpt.Build package and should always be available; " +
              "this likely indicates a corrupted or incomplete install.";

        return $"Driver for provider '{provider}' is not available. {installInstruction} " +
               $"See {DocsUrl} for details.";
    }
}
