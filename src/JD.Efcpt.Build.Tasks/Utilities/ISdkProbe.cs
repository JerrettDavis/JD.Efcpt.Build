using JD.Efcpt.Build.Core.Diagnostics;

namespace JD.Efcpt.Build.Tasks.Utilities;

/// <summary>
/// Testability seam over the SDK/dnx/global-tool capability probes used during
/// <see cref="RunEfcpt"/> tool resolution and restore.
/// </summary>
/// <remarks>
/// <para>
/// Extracting these probes behind an interface lets tests substitute a fake implementation
/// (e.g. one that throws if invoked) to assert that offline mode - or any other code path that
/// must avoid spawning processes - genuinely never calls into the underlying SDK checks,
/// without needing to mock <c>Process.Start</c> itself.
/// </para>
/// <para>
/// The production implementation, <see cref="DefaultSdkProbe"/>, does not reimplement probing
/// logic; it simply delegates to the existing memoized probes on <see cref="RunEfcpt"/> (which
/// are backed by <see cref="SdkProbeCache"/>, introduced in #187) so behavior and caching are
/// unchanged by introducing this seam.
/// </para>
/// </remarks>
internal interface ISdkProbe
{
    /// <summary>
    /// Checks if the .NET 10.0 (or later) SDK is installed for the given <c>dotnet</c> executable.
    /// </summary>
    /// <param name="dotnetExe">Path to (or bare name of) the dotnet executable.</param>
    bool IsDotNet10SdkInstalled(string dotnetExe);

    /// <summary>
    /// Checks if dnx (dotnet native execution) is available for the given <c>dotnet</c> executable.
    /// </summary>
    /// <param name="dotnetExe">Path to (or bare name of) the dotnet executable.</param>
    bool IsDnxAvailable(string dotnetExe);

    /// <summary>
    /// Checks whether a global dotnet tool command is resolvable on <c>PATH</c>, without spawning
    /// a process. Used for offline pre-flight validation - see <see cref="RunEfcpt"/>'s
    /// <c>JD0026</c> guard.
    /// </summary>
    /// <param name="toolCommand">The tool command name (e.g. <c>efcpt</c>).</param>
    bool IsGlobalToolInstalled(string toolCommand);
}

/// <summary>
/// Production <see cref="ISdkProbe"/> implementation that delegates to the existing, memoized
/// SDK/dnx probes on <see cref="RunEfcpt"/> (backed by <see cref="SdkProbeCache"/>) and to
/// <see cref="SdkProbeCache"/>'s <c>PATH</c> resolution helper for the global-tool check.
/// </summary>
internal sealed class DefaultSdkProbe : ISdkProbe
{
    /// <inheritdoc />
    public bool IsDotNet10SdkInstalled(string dotnetExe) => RunEfcpt.IsDotNet10SdkInstalled(dotnetExe);

    /// <inheritdoc />
    public bool IsDnxAvailable(string dotnetExe) => RunEfcpt.IsDnxAvailable(dotnetExe);

    /// <inheritdoc />
    public bool IsGlobalToolInstalled(string toolCommand) =>
        SdkProbeCache.ResolveDotnetExecutable(toolCommand) is not null;
}
