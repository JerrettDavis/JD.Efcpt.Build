namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// An <see cref="IConnectionStringSource"/> that reads the connection string from an
/// environment variable. This is the only connection-string source that ships in
/// <see cref="JD.Efcpt.Build.Core"/> itself - all other sources (Azure Key Vault, AWS Secrets
/// Manager, etc.) are satellite packages resolved by
/// <c>JD.Efcpt.Build.Tasks.ConnectionStrings.SatelliteConnectionStringSourceResolver</c>.
/// </summary>
/// <remarks>
/// Never returns <see cref="ConnectionStringSourceOutcome.OfflineBlocked"/> - reading an
/// environment variable makes no network call, so it is always safe to attempt even in offline
/// mode.
/// </remarks>
public sealed class EnvironmentVariableConnectionStringSource : IConnectionStringSource
{
    /// <summary>
    /// The <see cref="ConnectionStringSourceContext.Settings"/> key used to override which
    /// environment variable is read. Defaults to <see cref="DefaultEnvVarName"/> when absent.
    /// </summary>
    public const string EnvVarSettingKey = "envVar";

    /// <summary>
    /// The environment variable name read when <see cref="EnvVarSettingKey"/> is not present in
    /// <see cref="ConnectionStringSourceContext.Settings"/>.
    /// </summary>
    public const string DefaultEnvVarName = "EFCPT_CONNECTION_STRING";

    /// <inheritdoc />
    public string Key => "env";

    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public ConnectionStringSourceResult Resolve(in ConnectionStringSourceContext context)
    {
        var envVarName = context.Settings.TryGetValue(EnvVarSettingKey, out var configured) && !string.IsNullOrWhiteSpace(configured)
            ? configured
            : DefaultEnvVarName;

        var value = Environment.GetEnvironmentVariable(envVarName);

        if (string.IsNullOrWhiteSpace(value))
        {
            context.Log.Detail($"Environment variable '{envVarName}' is unset or empty.");
            return ConnectionStringSourceResult.NotFound(Key, $"Environment variable '{envVarName}' is unset or empty.");
        }

        context.Log.Detail($"Resolved connection string from environment variable '{envVarName}'.");
        return ConnectionStringSourceResult.Found(Key, value);
    }
}
