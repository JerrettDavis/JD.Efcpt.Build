namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Helper methods for working with <see cref="MessageLevel"/>.
/// </summary>
public static class MessageLevelHelpers
{
    /// <summary>
    /// Parses a string into a <see cref="MessageLevel"/>.
    /// </summary>
    /// <param name="value">The string value to parse (case-insensitive).</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>The parsed <see cref="MessageLevel"/>.</returns>
    public static MessageLevel Parse(string? value, MessageLevel defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.Trim().ToLowerInvariant() switch
        {
            "none" => MessageLevel.None,
            "info" => MessageLevel.Info,
            "warn" or "warning" => MessageLevel.Warn,
            "error" => MessageLevel.Error,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Tries to parse a string into a <see cref="MessageLevel"/>.
    /// </summary>
    /// <param name="value">The string value to parse (case-insensitive).</param>
    /// <param name="result">The parsed <see cref="MessageLevel"/>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out MessageLevel result)
    {
        result = MessageLevel.None;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "none":
                result = MessageLevel.None;
                return true;
            case "info":
                result = MessageLevel.Info;
                return true;
            case "warn":
            case "warning":
                result = MessageLevel.Warn;
                return true;
            case "error":
                result = MessageLevel.Error;
                return true;
            default:
                return false;
        }
    }
}
