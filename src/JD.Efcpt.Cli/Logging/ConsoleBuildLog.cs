using JD.Efcpt.Build.Core.Logging;

namespace JD.Efcpt.Cli.Logging;

/// <summary>
/// Console-backed <see cref="IBuildLog"/> implementation used by the jd-efcpt CLI.
/// </summary>
/// <remarks>
/// Informational and detail messages go to <see cref="Console.Out"/>; warnings and errors go to
/// <see cref="Console.Error"/>, so a caller piping stdout (e.g. into a file or another tool)
/// still sees diagnostics on the terminal, and scripts can distinguish "just output" from
/// "something needs attention" via stream redirection alone.
/// </remarks>
public sealed class ConsoleBuildLog : IBuildLog
{
    /// <summary>
    /// When <see langword="true"/>, <see cref="Detail"/> messages are also written; otherwise
    /// they are suppressed (mirrors the MSBuild "detailed" verbosity gate).
    /// </summary>
    public bool Verbose { get; init; }

    /// <inheritdoc />
    public void Info(string message) => Console.Out.WriteLine(message);

    /// <inheritdoc />
    public void Detail(string message)
    {
        if (Verbose)
            Console.Out.WriteLine(message);
    }

    /// <inheritdoc />
    public void Warn(string message) => Console.Error.WriteLine($"warning: {message}");

    /// <inheritdoc />
    public void Warn(string code, string message) => Console.Error.WriteLine($"warning {code}: {message}");

    /// <inheritdoc />
    public void Error(string message) => Console.Error.WriteLine($"error: {message}");

    /// <inheritdoc />
    public void Error(string code, string message) => Console.Error.WriteLine($"error {code}: {message}");

    /// <inheritdoc />
    public void Log(MessageLevel level, string message, string? code = null)
    {
        switch (level)
        {
            case MessageLevel.None:
                break;
            case MessageLevel.Info:
                Info(message);
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
