using JD.Efcpt.Build.Core.Diagnostics;
using JD.Efcpt.Build.Core.Processes;
using JD.Efcpt.Build.Core.Tests.Infrastructure;
using Xunit;

namespace JD.Efcpt.Build.Core.Tests.Processes;

/// <summary>
/// Exercises the REAL <see cref="ProcessRunner.RunOrThrow"/> failure path (a stub executable that
/// exits non-zero and echoes a planted connection string on stdout/stderr) to prove the secret is
/// scrubbed from BOTH the captured build-log output AND the thrown exception message. This is the
/// path a failing efcpt invocation actually takes; earlier tests only covered fake mode.
/// </summary>
public sealed class ProcessRunnerRedactionTests
{
    private const string Secret = "SUPERSECRET";
    private const string ConnectionString = "Server=db;Database=App;User Id=sa;Password=SUPERSECRET;";

    /// <summary>
    /// Writes a real, cross-platform stub executable that prints the planted connection string to
    /// both stdout and stderr and then exits non-zero. On Windows a <c>.cmd</c> (invoked via
    /// <c>cmd.exe /c</c> by CommandNormalizationStrategy); elsewhere an executable <c>.sh</c>.
    /// </summary>
    private static string CreateFailingSecretEchoScript(TestFolder folder)
    {
        if (OperatingSystem.IsWindows())
        {
            var cmd = Path.Combine(folder.Root, "leaky.cmd");
            File.WriteAllText(cmd,
                "@echo off\r\n" +
                $"echo Connecting with {ConnectionString} now\r\n" +
                $"echo ERROR login failed for {ConnectionString} 1>&2\r\n" +
                "exit /b 7\r\n");
            return cmd;
        }

        var sh = Path.Combine(folder.Root, "leaky.sh");
        File.WriteAllText(sh,
            "#!/bin/sh\n" +
            $"echo \"Connecting with {ConnectionString} now\"\n" +
            $"echo \"ERROR login failed for {ConnectionString}\" 1>&2\n" +
            "exit 7\n");
        File.SetUnixFileMode(
            sh,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        return sh;
    }

    [Fact]
    public void RunOrThrow_redacts_known_secret_from_all_log_output_and_the_exception_message()
    {
        using var folder = new TestFolder();
        var script = CreateFailingSecretEchoScript(folder);
        var log = new RecordingBuildLog();
        // The connection string is the first positional arg, exactly as RunEfcpt builds it.
        var args = $"\"{ConnectionString}\" mssql";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProcessRunner.RunOrThrow(log, script, args, folder.Root, secretToRedact: ConnectionString));

        // The exception message flows to MSBuild's error log via LogErrorFromException — it must
        // not carry the secret, and must show the redaction placeholder.
        Assert.DoesNotContain(Secret, ex.Message);
        Assert.Contains(SecretRedaction.ConnectionStringPlaceholder, ex.Message);

        // No logged message (command echo + captured stdout via Info, captured stderr via Error)
        // may contain the secret.
        var allLogged = log.InfoMessages
            .Concat(log.ErrorMessages.Select(e => e.Message))
            .ToList();
        Assert.NotEmpty(allLogged);
        Assert.All(allLogged, m => Assert.DoesNotContain(Secret, m));

        // Prove redaction actually ran (not merely that the secret happened to be absent).
        Assert.Contains(allLogged, m => m.Contains(SecretRedaction.ConnectionStringPlaceholder));
    }

    [Fact]
    public void RunOrThrow_exception_message_masks_password_even_without_a_known_secret()
    {
        using var folder = new TestFolder();
        var script = CreateFailingSecretEchoScript(folder);
        var log = new RecordingBuildLog();
        var args = $"\"{ConnectionString}\" mssql";

        // No secretToRedact: the generic MaskSecrets fallback must still scrub the password from
        // the thrown exception message and all logged output.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ProcessRunner.RunOrThrow(log, script, args, folder.Root));

        Assert.DoesNotContain(Secret, ex.Message);
        Assert.Contains("Password=***", ex.Message);

        var allLogged = log.InfoMessages
            .Concat(log.ErrorMessages.Select(e => e.Message))
            .ToList();
        Assert.All(allLogged, m => Assert.DoesNotContain(Secret, m));
    }
}
