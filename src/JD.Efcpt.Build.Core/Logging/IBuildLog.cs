namespace JD.Efcpt.Build.Core.Logging;

/// <summary>
/// Abstraction for build logging operations.
/// </summary>
/// <remarks>
/// This interface enables testability by allowing log implementations to be substituted
/// in unit tests without requiring MSBuild infrastructure. It is also implemented by
/// <c>JD.Efcpt.Build.Tasks.BuildLog</c> (an MSBuild <c>TaskLoggingHelper</c>-backed
/// implementation) and by the <c>jd-efcpt</c> CLI's console-backed implementation, so shared
/// logic in <see cref="JD.Efcpt.Build.Core"/> never depends on MSBuild or console types directly.
/// </remarks>
public interface IBuildLog
{
    /// <summary>
    /// Logs an informational message with high importance.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Info(string message);

    /// <summary>
    /// Logs a detailed message that only appears when verbosity is set to "detailed".
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Detail(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    void Warn(string message);

    /// <summary>
    /// Logs a warning message with a specific warning code.
    /// </summary>
    /// <param name="code">The warning code.</param>
    /// <param name="message">The warning message.</param>
    void Warn(string code, string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    void Error(string message);

    /// <summary>
    /// Logs an error message with a specific error code.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    void Error(string code, string message);

    /// <summary>
    /// Logs a message at the specified severity level with an optional code.
    /// </summary>
    /// <param name="level">The message severity level.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="code">Optional message code.</param>
    void Log(MessageLevel level, string message, string? code = null);
}

/// <summary>
/// No-op implementation of <see cref="IBuildLog"/> for testing scenarios.
/// </summary>
/// <remarks>
/// Use this implementation when testing code that requires an <see cref="IBuildLog"/>
/// but where actual logging output is not needed.
/// </remarks>
public sealed class NullBuildLog : IBuildLog
{
    /// <summary>
    /// Singleton instance of <see cref="NullBuildLog"/>.
    /// </summary>
    public static readonly NullBuildLog Instance = new();

    private NullBuildLog() { }

    /// <inheritdoc />
    public void Info(string message) { }

    /// <inheritdoc />
    public void Detail(string message) { }

    /// <inheritdoc />
    public void Warn(string message) { }

    /// <inheritdoc />
    public void Warn(string code, string message) { }

    /// <inheritdoc />
    public void Error(string message) { }

    /// <inheritdoc />
    public void Error(string code, string message) { }

    /// <inheritdoc />
    public void Log(MessageLevel level, string message, string? code = null) { }
}
