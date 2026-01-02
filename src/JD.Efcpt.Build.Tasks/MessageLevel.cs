namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Defines the severity level for build messages.
/// </summary>
public enum MessageLevel
{
    /// <summary>
    /// No message is emitted.
    /// </summary>
    None,

    /// <summary>
    /// Message is emitted as informational (low priority).
    /// </summary>
    Info,

    /// <summary>
    /// Message is emitted as a warning.
    /// </summary>
    Warn,

    /// <summary>
    /// Message is emitted as an error.
    /// </summary>
    Error
}
