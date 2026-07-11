using JD.Efcpt.Build.Core.Logging;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.Efcpt.Build.Tasks;

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

    /// <inheritdoc />
    public void Log(MessageLevel level, string message, string? code = null)
    {
        switch (level)
        {
            case MessageLevel.None:
                // Do nothing
                break;
            case MessageLevel.Info:
                log.LogMessage(MessageImportance.High, message);
                break;
            case MessageLevel.Warn:
                if (!string.IsNullOrEmpty(code))
                    Warn(code, message);
                else
                    Warn(message);
                break;
            case MessageLevel.Error:
                if (!string.IsNullOrEmpty(code))
                    Error(code, message);
                else
                    Error(message);
                break;
        }
    }
}
