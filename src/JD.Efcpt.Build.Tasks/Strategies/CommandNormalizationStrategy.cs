using PatternKit.Behavioral.Strategy;

namespace JD.Efcpt.Build.Tasks.Strategies;

/// <summary>
/// Record representing a process command with its executable and arguments.
/// </summary>
public readonly record struct ProcessCommand(string FileName, string Args);

/// <summary>
/// Strategy for normalizing process commands, particularly handling shell scripts across platforms.
/// </summary>
/// <remarks>
/// On Windows, .cmd and .bat files cannot be executed directly and must be invoked through cmd.exe /c.
/// On Linux/macOS, .sh files can be executed directly if they have execute permissions and a shebang.
/// This strategy handles that normalization transparently.
/// </remarks>
internal static class CommandNormalizationStrategy
{
    private static readonly Lazy<Strategy<ProcessCommand, ProcessCommand>> Strategy = new(() =>
        Strategy<ProcessCommand, ProcessCommand>.Create()
            // Windows: Wrap .cmd and .bat files with cmd.exe
            .When(static (in cmd)
                => OperatingSystem.IsWindows() &&
                   (cmd.FileName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                    cmd.FileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)))
            .Then(static (in cmd)
                => new ProcessCommand("cmd.exe", $"/c {cmd.FileName} {cmd.Args}"))
            // Linux/macOS: Shell scripts should be executable, no wrapper needed
            .Default(static (in cmd) => cmd)
            .Build());

    /// <summary>
    /// Normalizes a command, wrapping shell scripts appropriately for the platform.
    /// </summary>
    /// <param name="fileName">The executable or script file to run.</param>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A normalized ProcessCommand ready for execution.</returns>
    public static ProcessCommand Normalize(string fileName, string args)
        => Strategy.Value.Execute(new ProcessCommand(fileName, args));
}