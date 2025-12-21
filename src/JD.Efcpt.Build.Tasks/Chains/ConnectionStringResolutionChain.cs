using JD.Efcpt.Build.Tasks.ConnectionStrings;
using PatternKit.Behavioral.Chain;

namespace JD.Efcpt.Build.Tasks.Chains;

/// <summary>
/// Context for connection string resolution containing all configuration sources and search locations.
/// </summary>
internal readonly record struct ConnectionStringResolutionContext(
    string ExplicitConnectionString,
    string EfcptAppSettings,
    string EfcptAppConfig,
    string ConnectionStringName,
    string ProjectDirectory,
    BuildLog Log
);

/// <summary>
/// ResultChain for resolving connection strings with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
/// <item>Explicit EfcptConnectionString property (highest priority)</item>
/// <item>Explicit EfcptAppSettings file path</item>
/// <item>Explicit EfcptAppConfig file path</item>
/// <item>Auto-discovered appsettings*.json in project directory</item>
/// <item>Auto-discovered app.config/web.config in project directory</item>
/// <item>Returns null if no connection string found (fallback to .sqlproj mode)</item>
/// </list>
/// Uses ConfigurationFileTypeValidator to ensure proper file types.
/// Uses AppSettingsConnectionStringParser and AppConfigConnectionStringParser for parsing.
/// </remarks>
internal static class ConnectionStringResolutionChain
{
    public static ResultChain<ConnectionStringResolutionContext, string?> Build()
        => ResultChain<ConnectionStringResolutionContext, string?>.Create()
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
                TryParseFromExplicitPath(
                    ctx.EfcptAppSettings,
                    "EfcptAppSettings",
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out _))
            .Then(ctx =>
                TryParseFromExplicitPath(
                    ctx.EfcptAppSettings,
                    "EfcptAppSettings",
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out var result)
                    ? result
                    : null)
            // Branch 3: Explicit EfcptAppConfig path
            .When((in ctx) =>
                TryParseFromExplicitPath(
                    ctx.EfcptAppConfig,
                    "EfcptAppConfig",
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out _))
            .Then(ctx =>
                TryParseFromExplicitPath(
                    ctx.EfcptAppConfig,
                    "EfcptAppConfig",
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out var result)
                    ? result
                    : null)
            // Branch 4: Auto-discover appsettings*.json files
            .When((in ctx) =>
                TryAutoDiscoverAppSettings(
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out _))
            .Then(ctx =>
                TryAutoDiscoverAppSettings(
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out var result)
                    ? result
                    : null)
            // Branch 5: Auto-discover app.config/web.config
            .When((in ctx) =>
                TryAutoDiscoverAppConfig(
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out _))
            .Then(ctx =>
                TryAutoDiscoverAppConfig(
                    ctx.ProjectDirectory,
                    ctx.ConnectionStringName,
                    ctx.Log,
                    out var result)
                    ? result
                    : null)
            // Final fallback: No connection string found - return null for .sqlproj fallback
            .Finally(static (in ctx, out result, _) =>
            {
                result = null;
                return true; // Success with null indicates fallback to .sqlproj mode
            })
            .Build();

    private static bool TryParseFromExplicitPath(
        string explicitPath,
        string propertyName,
        string projectDirectory,
        string connectionStringName,
        BuildLog log,
        out string? connectionString)
    {
        connectionString = null;

        if (!PathUtils.HasValue(explicitPath))
            return false;

        var fullPath = PathUtils.FullPath(explicitPath, projectDirectory);
        if (!File.Exists(fullPath))
            return false;

        var validator = new ConfigurationFileTypeValidator();
        validator.ValidateAndWarn(fullPath, propertyName, log);

        var result = ParseConnectionStringFromFile(fullPath, connectionStringName, log);
        if (result.Success && !string.IsNullOrWhiteSpace(result.ConnectionString))
        {
            connectionString = result.ConnectionString;
            return true;
        }

        return false;
    }

    private static bool TryAutoDiscoverAppSettings(
        string projectDirectory,
        string connectionStringName,
        BuildLog log,
        out string? connectionString)
    {
        connectionString = null;

        var appSettingsFiles = Directory.GetFiles(projectDirectory, "appsettings*.json");
        if (appSettingsFiles.Length == 0)
            return false;

        if (appSettingsFiles.Length > 1)
        {
            log.Warn("JD0003",
                $"Multiple appsettings files found in project directory: {string.Join(", ", appSettingsFiles.Select(Path.GetFileName))}. " +
                $"Using '{Path.GetFileName(appSettingsFiles[0])}'. Specify EfcptAppSettings explicitly to avoid ambiguity.");
        }

        foreach (var file in appSettingsFiles.OrderBy(f => f == Path.Combine(projectDirectory, "appsettings.json") ? 0 : 1))
        {
            var parser = new AppSettingsConnectionStringParser();
            var result = parser.Parse(file, connectionStringName, log);
            if (result.Success && !string.IsNullOrWhiteSpace(result.ConnectionString))
            {
                log.Detail($"Resolved connection string from auto-discovered file: {Path.GetFileName(file)}");
                connectionString = result.ConnectionString;
                return true;
            }
        }

        return false;
    }

    private static bool TryAutoDiscoverAppConfig(
        string projectDirectory,
        string connectionStringName,
        BuildLog log,
        out string? connectionString)
    {
        connectionString = null;

        var configFiles = new[] { "app.config", "web.config" };
        foreach (var configFile in configFiles)
        {
            var path = Path.Combine(projectDirectory, configFile);
            if (File.Exists(path))
            {
                var parser = new AppConfigConnectionStringParser();
                var result = parser.Parse(path, connectionStringName, log);
                if (result.Success && !string.IsNullOrWhiteSpace(result.ConnectionString))
                {
                    log.Detail($"Resolved connection string from auto-discovered file: {configFile}");
                    connectionString = result.ConnectionString;
                    return true;
                }
            }
        }

        return false;
    }

    private static ConnectionStringResult ParseConnectionStringFromFile(
        string filePath,
        string connectionStringName,
        BuildLog log)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".json" => new AppSettingsConnectionStringParser().Parse(filePath, connectionStringName, log),
            ".config" => new AppConfigConnectionStringParser().Parse(filePath, connectionStringName, log),
            _ => ConnectionStringResult.Failed()
        };
    }
}
