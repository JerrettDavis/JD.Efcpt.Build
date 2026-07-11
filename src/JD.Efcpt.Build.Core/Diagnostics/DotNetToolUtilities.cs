using System.Diagnostics;
using System.Text;

namespace JD.Efcpt.Build.Core.Diagnostics;

/// <summary>
/// Shared utilities for dotnet tool resolution and framework detection.
/// </summary>
public static class DotNetToolUtilities
{
    /// <summary>
    /// Timeout in milliseconds for external process operations (SDK checks, dnx availability).
    /// </summary>
    private const int ProcessTimeoutMs = 5000;

    private static readonly char[] NewLineSeparator = ['\n'];
    private static readonly char[] SpaceSeparator = [' ', '\t'];

    /// <summary>
    /// Checks if the .NET 10.0 (or later) SDK is installed by running `dotnet --list-sdks`.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable (typically "dotnet" or "dotnet.exe").</param>
    /// <returns>
    /// <c>true</c> if a listed SDK version is &gt;= 10.0; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The underlying process spawn is memoized via <see cref="SdkProbeCache"/> under the
    /// <c>"list-sdks"</c> probe name, shared with <c>JD.Efcpt.Build.Tasks.RunEfcpt</c>'s
    /// equivalent probe so callers in both assemblies reuse a single result within the same
    /// build session. Only a determinate probe result is memoized - a transient failure (process
    /// launch hiccup, timeout) is retried on the next call rather than being cached as a
    /// permanent negative.
    /// </remarks>
    public static bool IsDotNet10SdkInstalled(string dotnetExe) =>
        SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, () => ProbeDotNet10SdkInstalled(dotnetExe));

    /// <summary>
    /// Performs the actual `dotnet --list-sdks` process spawn and output parsing. Not memoized
    /// itself - callers should go through <see cref="IsDotNet10SdkInstalled"/>.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable (typically "dotnet" or "dotnet.exe").</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if a listed SDK version is &gt;= 10.0;
    /// <see cref="ProbeOutcome.Unavailable"/> if the probe ran to completion but no such SDK was
    /// found; or <see cref="ProbeOutcome.Transient"/> if the probe could not produce a
    /// determinate answer (launch failure, timeout, unexpected exception).
    /// </returns>
    private static ProbeOutcome ProbeDotNet10SdkInstalled(string dotnetExe)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = dotnetExe,
                    Arguments = "--list-sdks",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            if (!process.WaitForExit(ProcessTimeoutMs))
            {
                try { process.Kill(); } catch { /* best effort */ }
                return ProbeOutcome.Transient;
            }

            return MapListSdksOutcome(process.ExitCode, outputBuilder.ToString());
        }
        catch
        {
            return ProbeOutcome.Transient;
        }
    }

    /// <summary>
    /// Pure decision logic for a completed <c>dotnet --list-sdks</c> invocation: given its exit
    /// code and captured standard output, determines whether a qualifying SDK is listed.
    /// Extracted from <see cref="ProbeDotNet10SdkInstalled"/> so it can be unit tested without
    /// spawning a process; the timeout/launch-failure/exception paths remain in the caller since
    /// they are inherent to the process spawn itself.
    /// </summary>
    /// <param name="exitCode">The exit code of the completed process.</param>
    /// <param name="output">The captured standard output of the completed process.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if a listed SDK version is &gt;= 10.0;
    /// <see cref="ProbeOutcome.Unavailable"/> otherwise (including a non-zero exit code).
    /// </returns>
    internal static ProbeOutcome MapListSdksOutcome(int exitCode, string output)
    {
        if (exitCode != 0)
            return ProbeOutcome.Unavailable;

        // Parse SDK versions from output like "10.0.100 [C:\Program Files\dotnet\sdk]"
        foreach (var line in output.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            var firstSpace = trimmed.IndexOf(' ');
            if (firstSpace <= 0)
                continue;

            var versionStr = trimmed.Substring(0, firstSpace);
            if (Version.TryParse(versionStr, out var version) && version.Major >= 10)
                return ProbeOutcome.Available;
        }

        return ProbeOutcome.Unavailable;
    }

    /// <summary>
    /// Checks if dnx (dotnet native execution) is available by running `dotnet --list-runtimes`.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable (typically "dotnet" or "dotnet.exe").</param>
    /// <returns>
    /// <c>true</c> if dnx functionality is available; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The underlying process spawn is memoized via <see cref="SdkProbeCache"/> under the
    /// <c>"list-runtimes"</c> probe name. Only a determinate probe result is memoized - a
    /// transient failure (process launch hiccup, timeout) is retried on the next call rather
    /// than being cached as a permanent negative.
    /// </remarks>
    public static bool IsDnxAvailable(string dotnetExe) =>
        SdkProbeCache.GetOrProbe("list-runtimes", dotnetExe, () => ProbeDnxAvailable(dotnetExe));

    /// <summary>
    /// Performs the actual `dotnet --list-runtimes` process spawn and output parsing. Not
    /// memoized itself - callers should go through <see cref="IsDnxAvailable"/>.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable (typically "dotnet" or "dotnet.exe").</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if dnx functionality is available;
    /// <see cref="ProbeOutcome.Unavailable"/> if the probe ran to completion but no qualifying
    /// runtime was found; or <see cref="ProbeOutcome.Transient"/> if the probe could not
    /// produce a determinate answer (launch failure, timeout, unexpected exception).
    /// </returns>
    private static ProbeOutcome ProbeDnxAvailable(string dotnetExe)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = dotnetExe,
                    Arguments = "--list-runtimes",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();

            if (!process.WaitForExit(ProcessTimeoutMs))
            {
                try { process.Kill(); } catch { /* best effort */ }
                return ProbeOutcome.Transient;
            }

            return MapListRuntimesOutcome(process.ExitCode, outputBuilder.ToString());
        }
        catch
        {
            return ProbeOutcome.Transient;
        }
    }

    /// <summary>
    /// Pure decision logic for a completed <c>dotnet --list-runtimes</c> invocation: given its
    /// exit code and captured standard output, determines whether a qualifying (dnx-capable)
    /// runtime is listed. Extracted from <see cref="ProbeDnxAvailable"/> so it can be unit
    /// tested without spawning a process; the timeout/launch-failure/exception paths remain in
    /// the caller since they are inherent to the process spawn itself.
    /// </summary>
    /// <param name="exitCode">The exit code of the completed process.</param>
    /// <param name="output">The captured standard output of the completed process.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if a qualifying (&gt;= 10.0) runtime is listed;
    /// <see cref="ProbeOutcome.Unavailable"/> otherwise (including a non-zero exit code).
    /// </returns>
    internal static ProbeOutcome MapListRuntimesOutcome(int exitCode, string output)
    {
        if (exitCode != 0)
            return ProbeOutcome.Unavailable;

        // If we can list runtimes and at least one .NET 10 runtime is present, dnx is available
        foreach (var line in output.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // Expected format: "<runtimeName> <version> [path]"
            var parts = trimmed.Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var versionStr = parts[1];
            if (Version.TryParse(versionStr, out var version) && version.Major >= 10)
            {
                return ProbeOutcome.Available;
            }
        }

        return ProbeOutcome.Unavailable;
    }

    /// <summary>
    /// Checks if dnx (dotnet native execution) is available by running `dotnet dnx --help`.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable (typically "dotnet" or "dotnet.exe").</param>
    /// <returns><c>true</c> if the <c>dnx --help</c> invocation exits with code 0; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This is a distinct probe from <see cref="IsDnxAvailable"/> - memoized under a different
    /// cache key (<c>"dnx-help"</c>, matching the command actually run: <c>dotnet dnx --help</c>,
    /// vs. <c>"list-runtimes"</c>'s <c>dotnet --list-runtimes</c>). It exists so
    /// <c>DefaultSdkProbe</c> (used by both <c>RunEfcpt</c> and <c>EfcptDoctor</c>) can
    /// reuse the exact same probe mechanics and cache key that
    /// <c>JD.Efcpt.Build.Tasks.RunEfcpt</c>'s own (pre-#181) private dnx-availability check used,
    /// preserving #186 behavior exactly across the Core extraction.
    /// </para>
    /// </remarks>
    public static bool IsDnxHelpAvailable(string dotnetExe) =>
        SdkProbeCache.GetOrProbe("dnx-help", dotnetExe, () => ProbeDnxHelpAvailable(dotnetExe));

    /// <summary>
    /// Performs the actual `dotnet dnx --help` process spawn. Not memoized itself - callers
    /// should go through <see cref="IsDnxHelpAvailable"/>.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if dnx responds successfully;
    /// <see cref="ProbeOutcome.Unavailable"/> if the probe ran to completion with a non-zero
    /// exit code; or <see cref="ProbeOutcome.Transient"/> if the probe could not produce a
    /// determinate answer (launch failure, timeout, unexpected exception).
    /// </returns>
    private static ProbeOutcome ProbeDnxHelpAvailable(string dotnetExe)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = "dnx --help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);
            if (p is null) return ProbeOutcome.Transient;

            if (!p.WaitForExit(ProcessTimeoutMs))
            {
                try { p.Kill(); } catch { /* best effort */ }
                return ProbeOutcome.Transient;
            }

            return MapExitCodeOutcome(p.ExitCode);
        }
        catch
        {
            return ProbeOutcome.Transient;
        }
    }

    /// <summary>
    /// Pure decision logic for a completed <c>dotnet dnx --help</c> invocation: maps its exit
    /// code to a probe outcome. Extracted from <see cref="ProbeDnxHelpAvailable"/> so it can be
    /// unit tested without spawning a process; the timeout/launch-failure/exception paths remain
    /// in the caller since they are inherent to the process spawn itself.
    /// </summary>
    /// <param name="exitCode">The exit code of the completed process.</param>
    /// <returns>
    /// <see cref="ProbeOutcome.Available"/> if <paramref name="exitCode"/> is zero;
    /// otherwise <see cref="ProbeOutcome.Unavailable"/>.
    /// </returns>
    internal static ProbeOutcome MapExitCodeOutcome(int exitCode) =>
        exitCode == 0 ? ProbeOutcome.Available : ProbeOutcome.Unavailable;

    /// <summary>
    /// Determines if the target framework is .NET 10.0 or later.
    /// </summary>
    /// <param name="targetFramework">Target framework moniker (e.g., "net10.0", "net8.0", "netstandard2.0").</param>
    /// <returns>
    /// <c>true</c> if the framework is .NET 10.0 or later; otherwise <c>false</c>.
    /// </returns>
    public static bool IsDotNet10OrLater(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            return false;

        // Handle various TFM formats:
        // - net10.0, net9.0, net8.0
        // - netcoreapp3.1
        // - netstandard2.0, netstandard2.1
        // - net48, net472

        var tfm = targetFramework.ToLowerInvariant().Trim();

        // .NET 5+ uses "netX.Y" format
        if (tfm.StartsWith("net") && !tfm.StartsWith("netstandard") && !tfm.StartsWith("netcoreapp"))
        {
            // Extract version number
            var versionPart = tfm.Substring(3); // Remove "net" prefix

            // Handle "net10.0" or "net10"
            var dotIndex = versionPart.IndexOf('.');
            var majorStr = dotIndex > 0 ? versionPart.Substring(0, dotIndex) : versionPart;

            if (int.TryParse(majorStr, out var major) && major >= 5 && major < 40)
            {
                // .NET 5+ uses single-digit or low double-digit major versions (5, 6, 7, 8, 9, 10, 11...)
                // .NET Framework uses higher numbers (46 for 4.6, 48 for 4.8, 472 for 4.7.2, etc.)
                // Filter out .NET Framework by checking if major is in the valid .NET 5+ range
                // .NET Framework versions are >= 40, so we reject those
                return major >= 10;
            }
        }

        return false;
    }

    /// <summary>
    /// Parses the major version number from a target framework moniker.
    /// </summary>
    /// <param name="targetFramework">Target framework moniker (e.g., "net10.0", "net8.0").</param>
    /// <returns>
    /// The major version number, or <c>null</c> if parsing fails.
    /// </returns>
    public static int? ParseTargetFrameworkVersion(string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(targetFramework))
            return null;

        var tfm = targetFramework.ToLowerInvariant().Trim();

        // .NET 5+ uses "netX.Y" format
        if (tfm.StartsWith("net") && !tfm.StartsWith("netstandard") && !tfm.StartsWith("netcoreapp"))
        {
            var versionPart = tfm.Substring(3);
            var dotIndex = versionPart.IndexOf('.');
            var majorStr = dotIndex > 0 ? versionPart.Substring(0, dotIndex) : versionPart;

            if (int.TryParse(majorStr, out var major))
            {
                return major;
            }
        }
        // .NET Core uses "netcoreappX.Y" format
        else if (tfm.StartsWith("netcoreapp"))
        {
            var versionPart = tfm.Substring(10); // Remove "netcoreapp"
            var dotIndex = versionPart.IndexOf('.');
            var majorStr = dotIndex > 0 ? versionPart.Substring(0, dotIndex) : versionPart;

            if (int.TryParse(majorStr, out var major))
            {
                return major;
            }
        }

        return null;
    }
}
