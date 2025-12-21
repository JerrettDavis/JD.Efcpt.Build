namespace JD.Efcpt.Build.Tasks.ConnectionStrings;

/// <summary>
/// Validates that configuration file paths match the expected parameter type and logs warnings for mismatches.
/// </summary>
internal sealed class ConfigurationFileTypeValidator
{
    /// <summary>
    /// Validates the file extension against the parameter name and logs a warning if they don't match.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <param name="parameterName">The name of the parameter (e.g., "EfcptAppSettings" or "EfcptAppConfig").</param>
    /// <param name="log">The build log for warnings.</param>
    public void ValidateAndWarn(string filePath, string parameterName, BuildLog log)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var isJson = extension == ".json";
        var isConfig = extension == ".config";

        if (parameterName == "EfcptAppSettings" && isConfig)
        {
            log.Warn("JD0001",
                $"EfcptAppSettings received a {extension} file path. " +
                "Consider using EfcptAppConfig for clarity. Proceeding with parsing as XML configuration.");
        }
        else if (parameterName == "EfcptAppConfig" && isJson)
        {
            log.Warn("JD0001",
                $"EfcptAppConfig received a {extension} file path. " +
                "Consider using EfcptAppSettings for clarity. Proceeding with parsing as JSON configuration.");
        }
    }
}
