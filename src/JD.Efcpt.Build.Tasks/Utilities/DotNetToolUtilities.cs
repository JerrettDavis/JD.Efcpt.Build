using System.Diagnostics;
using System.Text;

namespace JD.Efcpt.Build.Tasks.Utilities;

/// <summary>
/// Shared utilities for dotnet tool resolution and framework detection.
/// </summary>
internal static class DotNetToolUtilities
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
    public static bool IsDotNet10SdkInstalled(string dotnetExe)
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
                return false;
            }

            if (process.ExitCode != 0)
                return false;

            var output = outputBuilder.ToString();

            // Parse SDK versions from output like "10.0.100 [C:\Program Files\dotnet\sdk]"
            foreach (var line in output.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                var firstSpace = trimmed.IndexOf(' ');
                if (firstSpace <= 0)
                    continue;

                var versionStr = trimmed.Substring(0, firstSpace);
                if (Version.TryParse(versionStr, out var version) && version.Major >= 10)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if dnx (dotnet native execution) is available by running `dotnet --list-runtimes`.
    /// </summary>
    /// <param name="dotnetExe">Path to the dotnet executable (typically "dotnet" or "dotnet.exe").</param>
    /// <returns>
    /// <c>true</c> if dnx functionality is available; otherwise <c>false</c>.
    /// </returns>
    public static bool IsDnxAvailable(string dotnetExe)
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
                return false;
            }

            if (process.ExitCode != 0)
            {
                return false;
            }

            var output = outputBuilder.ToString();

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
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

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
