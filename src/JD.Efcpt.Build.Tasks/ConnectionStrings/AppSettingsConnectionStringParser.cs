using System.Text.Json;

namespace JD.Efcpt.Build.Tasks.ConnectionStrings;

/// <summary>
/// Parses connection strings from appsettings.json files.
/// </summary>
internal sealed class AppSettingsConnectionStringParser
{
    /// <summary>
    /// Attempts to parse a connection string from an appsettings.json file.
    /// </summary>
    /// <param name="filePath">The path to the appsettings.json file.</param>
    /// <param name="connectionStringName">The name of the connection string to retrieve.</param>
    /// <param name="log">The build log for warnings and errors.</param>
    /// <returns>A result indicating success or failure, along with the connection string if found.</returns>
    public ConnectionStringResult Parse(string filePath, string connectionStringName, BuildLog log)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings))
                return ConnectionStringResult.NotFound();

            // Try requested key
            if (connStrings.TryGetProperty(connectionStringName, out var value))
            {
                var connString = value.GetString();
                if (string.IsNullOrWhiteSpace(connString))
                {
                    log.Error("JD0012", $"Connection string '{connectionStringName}' in {filePath} is null or empty.");
                    return ConnectionStringResult.Failed();
                }
                return ConnectionStringResult.WithSuccess(connString, filePath, connectionStringName);
            }

            // Fallback to first available
            if (TryGetFirstConnectionString(connStrings, out var firstKey, out var firstValue))
            {
                log.Warn("JD0002",
                    $"Connection string key '{connectionStringName}' not found in {filePath}. " +
                    $"Using first available connection string '{firstKey}'.");
                return ConnectionStringResult.WithSuccess(firstValue, filePath, firstKey);
            }

            return ConnectionStringResult.NotFound();
        }
        catch (JsonException ex)
        {
            log.Error("JD0011", $"Failed to parse configuration file '{filePath}': {ex.Message}");
            return ConnectionStringResult.Failed();
        }
        catch (IOException ex)
        {
            log.Error("JD0011", $"Failed to read configuration file '{filePath}': {ex.Message}");
            return ConnectionStringResult.Failed();
        }
    }

    private static bool TryGetFirstConnectionString(
        JsonElement connStrings,
        out string key,
        out string value)
    {
        foreach (var prop in connStrings.EnumerateObject())
        {
            var str = prop.Value.GetString();
            if (!string.IsNullOrWhiteSpace(str))
            {
                key = prop.Name;
                value = str;
                return true;
            }
        }
        key = "";
        value = "";
        return false;
    }
}
