using JD.Efcpt.Build.Core.Logging;
using PatternKit.Behavioral.Chain;

namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// Context for connection string resolution containing all configuration sources and search locations.
/// </summary>
/// <param name="ExplicitConnectionString">Explicit <c>EfcptConnectionString</c> property value.</param>
/// <param name="EfcptAppSettings">Explicit <c>EfcptAppSettings</c> file path override.</param>
/// <param name="EfcptAppConfig">Explicit <c>EfcptAppConfig</c> file path override.</param>
/// <param name="ConnectionStringName">The connection string key/name to look up.</param>
/// <param name="ProjectDirectory">The consuming project's directory.</param>
/// <param name="Log">The build log to use for diagnostic output.</param>
/// <param name="ConnectionStringSource">
/// Optional pluggable connection-string source key (for example <c>env</c>,
/// <c>azure-keyvault</c>, <c>aws-secrets</c>) selected via <c>EfcptConnectionStringSource</c>.
/// When non-empty, source-based resolution (Branch 0) takes over entirely - see
/// <see cref="ConnectionStringResolutionChain"/> remarks. Defaults to <c>""</c> (not selected),
/// which preserves today's file/.sqlproj resolution behavior unchanged.
/// </param>
/// <param name="SourceSettings">
/// Source-specific settings (for example <c>keyVaultUri</c>, <c>secretName</c>, <c>envVar</c>)
/// passed to the selected source. Defaults to an empty dictionary.
/// </param>
/// <param name="Offline">
/// <see langword="true"/> when the build is running in offline mode. Passed through to the
/// selected <see cref="IConnectionStringSource"/> so network-backed sources can fail closed.
/// Defaults to <see langword="false"/>.
/// </param>
/// <param name="SourceResolver">
/// Resolves <see cref="ConnectionStringSource"/> to an <see cref="IConnectionStringSource"/>.
/// Required (non-null) whenever <see cref="ConnectionStringSource"/> is non-empty; defaults to
/// <see langword="null"/> otherwise, since it is never consulted in that case.
/// </param>
public readonly record struct ConnectionStringResolutionContext(
    string ExplicitConnectionString,
    string EfcptAppSettings,
    string EfcptAppConfig,
    string ConnectionStringName,
    string ProjectDirectory,
    IBuildLog Log,
    string ConnectionStringSource = "",
    IReadOnlyDictionary<string, string>? SourceSettings = null,
    bool Offline = false,
    IConnectionStringSourceResolver? SourceResolver = null
)
{
    /// <summary>
    /// Gets the source-specific settings passed to the selected <see cref="IConnectionStringSource"/>.
    /// Never <see langword="null"/> - defaults to an empty dictionary when not supplied.
    /// </summary>
    public IReadOnlyDictionary<string, string> SourceSettings { get; init; } = SourceSettings ?? new Dictionary<string, string>();
}

/// <summary>
/// ResultChain for resolving connection strings with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
/// <item>Pluggable connection-string source (<c>EfcptConnectionStringSource</c>) - highest priority, fail-closed (see below)</item>
/// <item>Explicit EfcptConnectionString property</item>
/// <item>Explicit EfcptAppSettings file path</item>
/// <item>Explicit EfcptAppConfig file path</item>
/// <item>Auto-discovered appsettings*.json in project directory</item>
/// <item>Auto-discovered app.config/web.config in project directory</item>
/// <item>Returns null if no connection string found (fallback to .sqlproj mode)</item>
/// </list>
/// Uses ConfigurationFileTypeValidator to ensure proper file types.
/// Uses AppSettingsConnectionStringParser and AppConfigConnectionStringParser for parsing.
/// <para>
/// <b>Branch 0 - pluggable sources - is fail-closed.</b> When
/// <see cref="ConnectionStringResolutionContext.ConnectionStringSource"/> is non-empty, this
/// branch resolves the named <see cref="IConnectionStringSource"/> via
/// <see cref="ConnectionStringResolutionContext.SourceResolver"/> and either returns the
/// resolved connection string or throws <see cref="ConnectionStringSourceException"/> - it never
/// falls through to the file/.sqlproj branches below. This is deliberate: once a caller has
/// explicitly opted into a secret store, a silent fallback to a stale local file would be a
/// worse outcome than a loud, actionable failure.
/// </para>
/// </remarks>
public static class ConnectionStringResolutionChain
{
    /// <summary>Builds the connection-string resolution chain.</summary>
    public static ResultChain<ConnectionStringResolutionContext, string?> Build()
        => ResultChain<ConnectionStringResolutionContext, string?>.Create()
            // Branch 0: Pluggable connection-string source (env/Key Vault/AWS Secrets/...).
            // Highest priority. Fail-closed: every non-Found outcome throws instead of
            // falling through to the branches below - see the fail-closed remarks above.
            .When(static (in ctx) =>
                PathUtils.HasValue(ctx.ConnectionStringSource))
            .Then(ResolveFromSource)
            // Branch 1: Explicit connection string property
            .When(static (in ctx) =>
                PathUtils.HasValue(ctx.ExplicitConnectionString))
            .Then(ctx =>
            {
                ctx.Log.Detail("Using explicit connection string from EfcptConnectionString property");
                return ctx.ExplicitConnectionString;
            })
            // Branch 2: Explicit EfcptAppSettings path
            .When((in ctx) =>
                HasExplicitConfigFile(ctx.EfcptAppSettings, ctx.ProjectDirectory))
            .Then(ctx =>
                ParseFromExplicitPath(
                    ctx.EfcptAppSettings,
                    "EfcptAppSettings",
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log))
            // Branch 3: Explicit EfcptAppConfig path
            .When((in ctx) =>
                HasExplicitConfigFile(ctx.EfcptAppConfig, ctx.ProjectDirectory))
            .Then(ctx =>
                ParseFromExplicitPath(
                    ctx.EfcptAppConfig,
                    "EfcptAppConfig",
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log))
            // Branch 4: Auto-discover appsettings*.json files
            .When((in ctx) =>
                HasAppSettingsFiles(ctx.ProjectDirectory))
            .Then(ctx =>
                ParseFromAutoDiscoveredAppSettings(
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log))
            // Branch 5: Auto-discover app.config/web.config
            .When((in ctx) =>
                HasAppConfigFiles(ctx.ProjectDirectory))
            .Then(ctx =>
                ParseFromAutoDiscoveredAppConfig(
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log))
            // Final fallback: No connection string found - return null for .sqlproj fallback
            .Finally(static (in _, out result, _) =>
            {
                result = null;
                return true; // Success with null indicates fallback to .sqlproj mode
            })
            .Build();

    #region Pluggable Source Resolution (Branch 0)

    private const string SourceDocsUrl = "https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/connection-string-sources.html";

    private static string? ResolveFromSource(ConnectionStringResolutionContext ctx)
    {
        var source = ctx.SourceResolver?.Resolve(ctx.ConnectionStringSource);
        if (source is null)
            throw ConnectionStringSourceException.SourceNotInstalled(ctx.ConnectionStringSource);

        var sourceContext = new ConnectionStringSourceContext(
            SourceKey: ctx.ConnectionStringSource,
            Settings: ctx.SourceSettings,
            Offline: ctx.Offline,
            Log: ctx.Log);

        ConnectionStringSourceResult result;
        try
        {
            result = source.Resolve(in sourceContext);
        }
        catch (Exception ex)
        {
            throw new ConnectionStringSourceException(
                ConnectionStringSourceException.SourceResolutionFailedCode,
                ctx.ConnectionStringSource,
                $"Connection-string source '{ctx.ConnectionStringSource}' threw an unexpected exception while resolving: {ex.Message} See {SourceDocsUrl} for details.",
                ex);
        }

        return result.Outcome switch
        {
            ConnectionStringSourceOutcome.Found =>
                result.ConnectionString,

            ConnectionStringSourceOutcome.NotFound =>
                throw new ConnectionStringSourceException(
                    ConnectionStringSourceException.SecretNotFoundCode,
                    ctx.ConnectionStringSource,
                    $"Connection-string source '{ctx.ConnectionStringSource}' did not find a value.{Describe(result.Diagnostic)} See {SourceDocsUrl} for details."),

            ConnectionStringSourceOutcome.Failed =>
                throw new ConnectionStringSourceException(
                    ConnectionStringSourceException.SourceResolutionFailedCode,
                    ctx.ConnectionStringSource,
                    $"Connection-string source '{ctx.ConnectionStringSource}' failed to resolve.{Describe(result.Diagnostic)} See {SourceDocsUrl} for details."),

            ConnectionStringSourceOutcome.OfflineBlocked =>
                throw new ConnectionStringSourceException(
                    ConnectionStringSourceException.OfflineBlockedCode,
                    ctx.ConnectionStringSource,
                    $"Connection-string source '{ctx.ConnectionStringSource}' is network-backed and was blocked by offline mode (EfcptOfflineMode/EFCPT_OFFLINE).{Describe(result.Diagnostic)} Use the 'env' source for air-gapped builds, or disable offline mode. See {SourceDocsUrl} for details."),

            ConnectionStringSourceOutcome.Misconfigured =>
                throw new ConnectionStringSourceException(
                    ConnectionStringSourceException.SourceMisconfiguredCode,
                    ctx.ConnectionStringSource,
                    $"Connection-string source '{ctx.ConnectionStringSource}' is missing required settings.{Describe(result.Diagnostic)} See {SourceDocsUrl} for details."),

            _ =>
                throw new ConnectionStringSourceException(
                    ConnectionStringSourceException.SourceResolutionFailedCode,
                    ctx.ConnectionStringSource,
                    $"Connection-string source '{ctx.ConnectionStringSource}' returned an unrecognized outcome '{result.Outcome}'.{Describe(result.Diagnostic)} See {SourceDocsUrl} for details.")
        };
    }

    private static string Describe(string? diagnostic)
        => string.IsNullOrWhiteSpace(diagnostic) ? "" : $" {diagnostic}";

    #endregion

    #region Existence Checks (for When clauses)

    private static bool HasExplicitConfigFile(string explicitPath, string projectDirectory)
    {
        if (!PathUtils.HasValue(explicitPath))
            return false;

        var fullPath = PathUtils.FullPath(explicitPath, projectDirectory);
        return File.Exists(fullPath);
    }

    private static bool HasAppSettingsFiles(string projectDirectory)
    {
        // Guard against null - can occur on .NET Framework MSBuild
        if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            return false;

        return Directory.GetFiles(projectDirectory, "appsettings*.json").Length > 0;
    }

    private static bool HasAppConfigFiles(string projectDirectory)
    {
        // Guard against null - can occur on .NET Framework MSBuild
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return false;

        return File.Exists(Path.Combine(projectDirectory, "app.config")) ||
               File.Exists(Path.Combine(projectDirectory, "web.config"));
    }

    #endregion

    #region Parsing (for Then clauses)

    private static string? ParseFromExplicitPath(
        string explicitPath,
        string propertyName,
        string projectDirectory,
        string connectionStringName,
        IBuildLog log)
    {
        var fullPath = PathUtils.FullPath(explicitPath, projectDirectory);

        ConfigurationFileTypeValidator.ValidateAndWarn(fullPath, propertyName, log);

        var result = ParseConnectionStringFromFile(fullPath, connectionStringName, log);
        return result.Success ? result.ConnectionString : null;
    }

    private static string? ParseFromAutoDiscoveredAppSettings(
        string projectDirectory,
        string connectionStringName,
        IBuildLog log)
    {
        // Guard against null - can occur on .NET Framework MSBuild
        if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            return null;

        var appSettingsFiles = Directory.GetFiles(projectDirectory, "appsettings*.json");

        if (appSettingsFiles.Length > 1)
        {
            log.Warn("JD0003",
                $"Multiple appsettings files found in project directory: {string.Join(", ", appSettingsFiles.Select(Path.GetFileName))}. " +
                $"Using '{Path.GetFileName(appSettingsFiles[0])}'. Specify EfcptAppSettings explicitly to avoid ambiguity.");
        }

        foreach (var file in appSettingsFiles.OrderBy(f => f == Path.Combine(projectDirectory, "appsettings.json") ? 0 : 1))
        {
            var result = AppSettingsConnectionStringParser.Parse(file, connectionStringName, log);
            if (!result.Success || string.IsNullOrWhiteSpace(result.ConnectionString))
                continue;

            log.Detail($"Resolved connection string from auto-discovered file: {Path.GetFileName(file)}");
            return result.ConnectionString;
        }

        return null;
    }

    private static string? ParseFromAutoDiscoveredAppConfig(
        string projectDirectory,
        string connectionStringName,
        IBuildLog log)
    {
        // Guard against null - can occur on .NET Framework MSBuild
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return null;

        var configFiles = new[] { "app.config", "web.config" };
        foreach (var configFile in configFiles)
        {
            var path = Path.Combine(projectDirectory, configFile);
            if (!File.Exists(path))
                continue;

            var result = AppConfigConnectionStringParser.Parse(path, connectionStringName, log);
            if (result.Success && !string.IsNullOrWhiteSpace(result.ConnectionString))
            {
                log.Detail($"Resolved connection string from auto-discovered file: {configFile}");
                return result.ConnectionString;
            }
        }

        return null;
    }

    private static ConnectionStringResult ParseConnectionStringFromFile(
        string filePath,
        string connectionStringName,
        IBuildLog log)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".json" => AppSettingsConnectionStringParser.Parse(filePath, connectionStringName, log),
            ".config" => AppConfigConnectionStringParser.Parse(filePath, connectionStringName, log),
            _ => ConnectionStringResult.Failed()
        };
    }

    #endregion
}
