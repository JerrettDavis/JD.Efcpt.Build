using System.Diagnostics;
using System.Text;
using JD.Efcpt.Build.Core.Logging;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Core.Processes;

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
/// This class provides a unified process execution mechanism used by
/// <c>JD.Efcpt.Build.Tasks.RunEfcpt</c> and <c>JD.Efcpt.Build.Tasks.EnsureDacpacBuilt</c>, and by
/// this assembly's <c>DefaultToolAcquirer</c>, eliminating code duplication.
/// </para>
/// <para>
/// All commands are normalized using <see cref="CommandNormalizationStrategy"/> to handle
/// cross-platform differences (e.g., cmd.exe wrapping on Windows).
/// </para>
/// </remarks>
public static class ProcessRunner
{
    /// <summary>
    /// Runs a process and returns the result without throwing on non-zero exit code.
    /// </summary>
    /// <param name="log">Build log for diagnostic output.</param>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="args">Command line arguments.</param>
    /// <param name="workingDir">Working directory for the process.</param>
    /// <param name="environmentVariables">Optional environment variables to set.</param>
    /// <param name="timeoutMs">
    /// Optional bounded timeout in milliseconds. When <see langword="null"/> (the default), the
    /// process is awaited with no timeout - identical to this method's original behavior, so
    /// existing callers are unaffected. When specified, the process is force-killed and a
    /// <see cref="ProcessResult"/> with a non-zero exit code and a descriptive
    /// <see cref="ProcessResult.StdErr"/> message is returned if it has not exited within
    /// <paramref name="timeoutMs"/>, instead of blocking indefinitely.
    /// </param>
    /// <returns>A <see cref="ProcessResult"/> containing exit code and captured output.</returns>
    public static ProcessResult Run(
        IBuildLog log,
        string fileName,
        string args,
        string workingDir,
        IDictionary<string, string>? environmentVariables = null,
        int? timeoutMs = null)
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

        if (timeoutMs is not int timeout)
        {
            // Unbounded (legacy) path - unchanged behavior for existing callers that don't pass
            // a timeout.
            var legacyStdout = p.StandardOutput.ReadToEnd();
            var legacyStderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            return new ProcessResult(p.ExitCode, legacyStdout, legacyStderr);
        }

        // Bounded path: read output asynchronously via events so a hung/slow process (e.g. a
        // blocked/slow NuGet feed during 'dotnet tool install') can be detected and killed after
        // `timeout` ms instead of blocking the build forever - synchronous ReadToEnd() would
        // never return if the process never writes/closes its output streams.
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        if (!p.WaitForExit(timeout))
        {
            try { p.Kill(); } catch { /* best effort */ }
            try { p.WaitForExit(2000); } catch { /* best effort */ }

            return new ProcessResult(
                -1,
                stdoutBuilder.ToString(),
                $"Process timed out after {timeout / 1000.0:0.#}s and was killed: " +
                $"{normalized.FileName} {normalized.Args}");
        }

        // Ensure the async redirected-stream event handlers have finished flushing before we
        // read the accumulated builders (recommended pattern per Process.WaitForExit docs).
        p.WaitForExit();

        return new ProcessResult(p.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
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
