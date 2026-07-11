using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using JD.Efcpt.Ide.Core;

namespace JD.Efcpt.VsExtension.Services;

/// <summary>
/// The outcome of a <see cref="RegenerateModelsService.RunAsync"/> invocation.
/// </summary>
internal sealed class RegenerateResult
{
    /// <summary>Initializes a new <see cref="RegenerateResult"/>.</summary>
    public RegenerateResult(int exitCode, IReadOnlyList<JdDiagnostic> diagnostics, bool startFailed)
    {
        ExitCode = exitCode;
        Diagnostics = diagnostics;
        StartFailed = startFailed;
    }

    /// <summary>The <c>dotnet build</c> process exit code (meaningless when <see cref="StartFailed"/> is true).</summary>
    public int ExitCode { get; }

    /// <summary>Every JDxxxx diagnostic found in the (redacted) build output, in order.</summary>
    public IReadOnlyList<JdDiagnostic> Diagnostics { get; }

    /// <summary>True when the <c>dotnet</c> process could not even be started (e.g. not on PATH).</summary>
    public bool StartFailed { get; }
}

/// <summary>
/// Runs the JD.Efcpt.Build regeneration build by shelling out to <c>dotnet build</c> - rather
/// than calling into MSBuild/the task pipeline in-process - which avoids design-time-build
/// entanglement and matches the exact behavior of the CLI and CI pipeline. Streams every line of
/// (redacted) output into a VS Output Window pane and parses JDxxxx diagnostics as they arrive.
/// </summary>
internal static class RegenerateModelsService
{
    /// <summary>
    /// Runs <c>dotnet build &lt;projectPath&gt; -p:EfcptForceRegenerate=true
    /// -p:EfcptEnableProfiling=true -p:EfcptLogVerbosity=minimal</c>, streaming redacted output
    /// into <paramref name="pane"/>.
    /// </summary>
    /// <param name="projectPath">Full path to the target <c>.csproj</c>.</param>
    /// <param name="pane">The Output Window pane to stream (redacted) build output into.</param>
    /// <param name="cancellationToken">Cancels the build by killing the <c>dotnet</c> process.</param>
    public static async Task<RegenerateResult> RunAsync(string projectPath, OutputWindowPane pane, CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory;
        var arguments =
            $"build \"{projectPath}\" -p:EfcptForceRegenerate=true -p:EfcptEnableProfiling=true -p:EfcptLogVerbosity=minimal";

        await pane.WriteLineAsync($"> dotnet {arguments}").ConfigureAwait(false);

        var diagnostics = new List<JdDiagnostic>();
        var writeLock = new SemaphoreSlim(1, 1);

        async Task WriteRedactedLineAsync(string line)
        {
            var redacted = SecretRedaction.MaskSecrets(line);

            await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await pane.WriteLineAsync(redacted).ConfigureAwait(false);
            }
            finally
            {
                writeLock.Release();
            }

            var diagnostic = JdDiagnosticParser.TryParseLine(redacted);
            if (diagnostic != null)
                diagnostics.Add(diagnostic);
        }

        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = projectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            // Spawn failures (e.g. dotnet not on PATH) surface here rather than throwing, so the
            // caller can point the user at the Output pane instead of an unhandled exception.
            await pane.WriteLineAsync($"Failed to start 'dotnet': {ex.Message}").ConfigureAwait(false);
            return new RegenerateResult(-1, diagnostics, startFailed: true);
        }

        async Task PumpAsync(StreamReader reader)
        {
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                await WriteRedactedLineAsync(line).ConfigureAwait(false);
            }
        }

        using (cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch (InvalidOperationException)
            {
                // The process already exited between the HasExited check and Kill() - ignore.
            }
        }))
        {
            var stdoutTask = PumpAsync(process.StandardOutput);
            var stderrTask = PumpAsync(process.StandardError);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

            // net472 predates Process.WaitForExitAsync; run the blocking wait off the UI thread.
            await Task.Run(() => process.WaitForExit(), CancellationToken.None).ConfigureAwait(false);
        }

        return new RegenerateResult(process.ExitCode, diagnostics, startFailed: false);
    }
}
