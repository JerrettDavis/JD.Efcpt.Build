namespace JD.Efcpt.Build.Core.Diagnostics;

/// <summary>
/// Testability seam over the SDK/dnx/global-tool capability probes used during
/// <c>JD.Efcpt.Build.Tasks.RunEfcpt</c> tool resolution and restore, and by
/// <c>JD.Efcpt.Build.Tasks.EfcptDoctor</c>/<see cref="DoctorEngine"/>.
/// </summary>
/// <remarks>
/// <para>
/// Extracting these probes behind an interface lets tests substitute a fake implementation
/// (e.g. one that throws if invoked) to assert that offline mode - or any other code path that
/// must avoid spawning processes - genuinely never calls into the underlying SDK checks,
/// without needing to mock <c>Process.Start</c> itself.
/// </para>
/// <para>
/// The production implementation, <see cref="DefaultSdkProbe"/>, delegates to
/// <see cref="DotNetToolUtilities"/> (backed by <see cref="SdkProbeCache"/>) rather than to
/// <c>RunEfcpt</c>'s own probe methods, since this assembly cannot reference
/// <c>JD.Efcpt.Build.Tasks</c>. <see cref="DotNetToolUtilities.IsDotNet10SdkInstalled"/> shares
/// the exact same <c>"list-sdks"</c> cache key <c>RunEfcpt</c>'s own probe uses, and
/// <see cref="DotNetToolUtilities.IsDnxHelpAvailable"/> shares the exact same <c>"dnx-help"</c>
/// cache key/command <c>RunEfcpt</c>'s own probe uses - so behavior and caching are unchanged by
/// introducing this seam and moving it into this assembly (see #181).
/// </para>
/// </remarks>
public interface ISdkProbe
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
    /// a process. Used for offline pre-flight validation - see <c>RunEfcpt</c>'s <c>JD0026</c> guard.
    /// </summary>
    /// <param name="toolCommand">The tool command name (e.g. <c>efcpt</c>).</param>
    bool IsGlobalToolInstalled(string toolCommand);
}

/// <summary>
/// Production <see cref="ISdkProbe"/> implementation that delegates to
/// <see cref="DotNetToolUtilities"/> (backed by <see cref="SdkProbeCache"/>).
/// </summary>
public sealed class DefaultSdkProbe : ISdkProbe
{
    /// <inheritdoc />
    public bool IsDotNet10SdkInstalled(string dotnetExe) => DotNetToolUtilities.IsDotNet10SdkInstalled(dotnetExe);

    /// <inheritdoc />
    public bool IsDnxAvailable(string dotnetExe) => DotNetToolUtilities.IsDnxHelpAvailable(dotnetExe);

    /// <inheritdoc />
    public bool IsGlobalToolInstalled(string toolCommand) =>
        SdkProbeCache.ResolveDotnetExecutable(toolCommand) is not null;
}
