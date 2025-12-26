using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Abstraction for build logging operations.
/// </summary>
/// <remarks>
/// This interface enables testability by allowing log implementations to be substituted
/// in unit tests without requiring MSBuild infrastructure.
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
}

/// <summary>
/// MSBuild-backed implementation of <see cref="IBuildLog"/>.
/// </summary>
/// <remarks>
/// This is the production implementation that writes to the MSBuild task logging helper.
/// </remarks>
internal sealed class BuildLog(TaskLoggingHelper log, string verbosity) : IBuildLog
{
    private readonly string _verbosity = string.IsNullOrWhiteSpace(verbosity) ? "minimal" : verbosity;

    /// <inheritdoc />
    public void Info(string message) => log.LogMessage(MessageImportance.High, message);

    /// <inheritdoc />
    public void Detail(string message)
    {
        if (_verbosity.EqualsIgnoreCase("detailed"))
            log.LogMessage(MessageImportance.Normal, message);
    }

    /// <inheritdoc />
    public void Warn(string message) => log.LogWarning(message);

    /// <inheritdoc />
    public void Warn(string code, string message)
        => log.LogWarning(subcategory: null, code, helpKeyword: null,
                          file: null, lineNumber: 0, columnNumber: 0,
                          endLineNumber: 0, endColumnNumber: 0, message);

    /// <inheritdoc />
    public void Error(string message) => log.LogError(message);

    /// <inheritdoc />
    public void Error(string code, string message)
        => log.LogError(subcategory: null, code, helpKeyword: null,
                        file: null, lineNumber: 0, columnNumber: 0,
                        endLineNumber: 0, endColumnNumber: 0, message);
}

/// <summary>
/// No-op implementation of <see cref="IBuildLog"/> for testing scenarios.
/// </summary>
/// <remarks>
/// Use this implementation when testing code that requires an <see cref="IBuildLog"/>
/// but where actual logging output is not needed.
/// </remarks>
internal sealed class NullBuildLog : IBuildLog
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
}
