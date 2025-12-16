using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.Efcpt.Build.Tasks;

internal sealed class BuildLog(TaskLoggingHelper log, string verbosity)
{
    private readonly string _verbosity = string.IsNullOrWhiteSpace(verbosity) ? "minimal" : verbosity;

    public void Info(string message) => log.LogMessage(MessageImportance.High, message);

    public void Detail(string message)
    {
        if (string.Equals(_verbosity, "detailed", StringComparison.OrdinalIgnoreCase))
            log.LogMessage(MessageImportance.Normal, message);
    }

    public void Warn(string message) => log.LogWarning(message);

    public void Error(string message) => log.LogError(message);
}
