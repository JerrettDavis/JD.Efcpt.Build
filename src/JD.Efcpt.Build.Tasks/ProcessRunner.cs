using System.Diagnostics;
using JD.Efcpt.Build.Tasks.Strategies;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Encapsulates the result of a process execution.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StdOut">Standard output from the process.</param>
/// <param name="StdErr">Standard error output from the process.</param>
public readonly record struct ProcessResult(
    int ExitCode,
    string StdOut,
    string StdErr
)
{
    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Helper for running external processes with consistent logging and error handling.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a unified process execution mechanism used by <see cref="RunEfcpt"/>
/// and <see cref="EnsureDacpacBuilt"/> tasks, eliminating code duplication.
/// </para>
/// <para>
/// All commands are normalized using <see cref="CommandNormalizationStrategy"/> to handle
/// cross-platform differences (e.g., cmd.exe wrapping on Windows).
/// </para>
/// </remarks>
internal static class ProcessRunner
{
    /// <summary>
    /// Runs a process and returns the result without throwing on non-zero exit code.
    /// </summary>
    /// <param name="log">Build log for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <returns>A <see cref="ProcessResult"/> containing exit code and captured output.</returns>
    public static ProcessResult Run(
        IBuildLog log,
        string fileName,
        string args,
        string workingDir,
        IDictionary<string, string>? environmentVariables = null)
    {
        var normalized = CommandNormalizationStrategy.Normalize(fileName, args);
        log.Info($"> {normalized.FileName} {normalized.Args}");

        var psi = new ProcessStartInfo
        {
            FileName = normalized.FileName,
            Arguments = normalized.Args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Apply test environment variable if set (for testing scenarios)
        var testDac = Environment.GetEnvironmentVariable("EFCPT_TEST_DACPAC");
        if (!string.IsNullOrWhiteSpace(testDac))
            psi.Environment["EFCPT_TEST_DACPAC"] = testDac;

        // Apply any additional environment variables
        if (environmentVariables != null)
        {
            foreach (var (key, value) in environmentVariables)
                psi.Environment[key] = value;
        }

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {normalized.FileName}");

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        return new ProcessResult(p.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Runs a process and throws if it fails (non-zero exit code).
    /// </summary>
    /// <param name="log">Build log for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when the process exits with a non-zero code.</exception>
    public static void RunOrThrow(
        IBuildLog log,
        string fileName,
        string args,
        string workingDir,
        IDictionary<string, string>? environmentVariables = null)
    {
        var result = Run(log, fileName, args, workingDir, environmentVariables);

        if (!string.IsNullOrWhiteSpace(result.StdOut)) log.Info(result.StdOut);
        if (!string.IsNullOrWhiteSpace(result.StdErr)) log.Error(result.StdErr);

        if (!result.Success)
            throw new InvalidOperationException(
                $"Process failed ({result.ExitCode}): {fileName} {args}");
    }

    /// <summary>
    /// Runs a build process and throws if it fails, with detailed output logging.
    /// </summary>
    /// <param name="log">Build log for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="errorMessage">Custom error message for failures.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when the process exits with a non-zero code.</exception>
    public static void RunBuildOrThrow(
        IBuildLog log,
        string fileName,
        string args,
        string workingDir,
        string? errorMessage = null,
        IDictionary<string, string>? environmentVariables = null)
    {
        var result = Run(log, fileName, args, workingDir, environmentVariables);

        if (!result.Success)
        {
            log.Error(result.StdOut);
            log.Error(result.StdErr);
            throw new InvalidOperationException(
                errorMessage ?? $"Build failed with exit code {result.ExitCode}");
        }

        if (!string.IsNullOrWhiteSpace(result.StdOut)) log.Detail(result.StdOut);
        if (!string.IsNullOrWhiteSpace(result.StdErr)) log.Detail(result.StdErr);
    }
}
