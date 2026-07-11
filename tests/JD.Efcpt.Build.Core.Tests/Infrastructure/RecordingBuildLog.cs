using JD.Efcpt.Build.Core.Logging;

namespace JD.Efcpt.Build.Core.Tests.Infrastructure;

/// <summary>
/// A simple <see cref="IBuildLog"/> fake that records every call, for tests that need to assert
/// on logged messages/warnings/errors without any MSBuild logging infrastructure.
/// </summary>
internal sealed class RecordingBuildLog : IBuildLog
{
    public List<string> InfoMessages { get; } = [];
    public List<string> DetailMessages { get; } = [];
    public List<(string? Code, string Message)> WarnMessages { get; } = [];
    public List<(string? Code, string Message)> ErrorMessages { get; } = [];

    public void Info(string message) => InfoMessages.Add(message);

    public void Detail(string message) => DetailMessages.Add(message);

    public void Warn(string message) => WarnMessages.Add((null, message));

    public void Warn(string code, string message) => WarnMessages.Add((code, message));

    public void Error(string message) => ErrorMessages.Add((null, message));

    public void Error(string code, string message) => ErrorMessages.Add((code, message));

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
                if (!string.IsNullOrEmpty(code)) Warn(code, message); else Warn(message);
                break;
            case MessageLevel.Error:
                if (!string.IsNullOrEmpty(code)) Error(code, message); else Error(message);
                break;
        }
    }
}
