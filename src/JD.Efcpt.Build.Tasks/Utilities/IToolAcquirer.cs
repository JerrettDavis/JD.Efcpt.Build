using JD.Efcpt.Build.Core.Logging;

namespace JD.Efcpt.Build.Tasks.Utilities;

/// <summary>
/// A request to bootstrap an obj-local dotnet tool manifest and install a specific tool
/// package into it. See <see cref="IToolAcquirer"/>.
/// </summary>
/// <param name="ManifestDir">
/// The directory in which to create (or reuse) <c>.config/dotnet-tools.json</c> - normally
/// <see cref="RunEfcpt.WorkingDirectory"/> (obj/efcpt).
/// </param>
/// <param name="DotNetExe">Path to (or bare name of) the <c>dotnet</c> executable to invoke.</param>
/// <param name="ToolPackageId">The dotnet tool package id to install (e.g. <c>ErikEJ.EFCorePowerTools.Cli</c>).</param>
/// <param name="ToolVersion">Optional version constraint; empty means "latest".</param>
internal readonly record struct ToolAcquisitionRequest(
    string ManifestDir,
    string DotNetExe,
    string ToolPackageId,
    string ToolVersion);

/// <summary>
/// The result of a <see cref="IToolAcquirer.Acquire"/> attempt.
/// </summary>
/// <param name="Success"><see langword="true"/> if the manifest bootstrap and tool install both succeeded.</param>
/// <param name="ErrorMessage">
/// When <paramref name="Success"/> is <see langword="false"/>, a human-readable description of
/// what failed (captured process output, or an exception message); otherwise <see langword="null"/>.
/// </param>
internal readonly record struct ToolAcquisitionOutcome(bool Success, string? ErrorMessage)
{
    /// <summary>Creates a successful outcome.</summary>
    public static ToolAcquisitionOutcome Ok() => new(true, null);

    /// <summary>Creates a failed outcome carrying a diagnostic message.</summary>
    public static ToolAcquisitionOutcome Failed(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Testability seam over dotnet tool acquisition (obj-local manifest bootstrap + install), used
/// by <see cref="RunEfcpt"/>'s auto-acquisition path (<c>EfcptAutoAcquireTool</c>) for .NET 8/9
/// projects where dnx is not usable.
/// </summary>
/// <remarks>
/// Extracting this behind an interface lets tests substitute a fake implementation to assert the
/// exact acquisition request that would have been issued - and simulate the manifest it would
/// have produced - without spawning any process or touching the network, mirroring the role
/// <see cref="ISdkProbe"/> plays for the SDK/dnx/global-tool capability probes.
/// </remarks>
internal interface IToolAcquirer
{
    /// <summary>
    /// Bootstraps a local tool manifest in <see cref="ToolAcquisitionRequest.ManifestDir"/> (via
    /// <c>dotnet new tool-manifest</c>, if one doesn't already exist there) and installs
    /// <see cref="ToolAcquisitionRequest.ToolPackageId"/> into it (via <c>dotnet tool install</c>).
    /// </summary>
    /// <param name="request">The acquisition request.</param>
    /// <param name="log">Build log for diagnostic output.</param>
    ToolAcquisitionOutcome Acquire(ToolAcquisitionRequest request, IBuildLog log);
}

/// <summary>
/// Production <see cref="IToolAcquirer"/> implementation that shells out via
/// <see cref="ProcessRunner"/> - first <c>dotnet new tool-manifest</c> (only if no manifest
/// already exists at the target directory), then <c>dotnet tool install</c>. Uses the
/// non-throwing <see cref="ProcessRunner.Run"/> (not <c>RunOrThrow</c>) so failures can be
/// translated by the caller into the actionable, coded <c>JD0027</c> error rather than an
/// exception with a raw stack trace.
/// </summary>
internal sealed class DefaultToolAcquirer : IToolAcquirer
{
    /// <summary>
    /// Bounded timeout, in milliseconds, applied to the <c>dotnet new tool-manifest</c> and
    /// <c>dotnet tool install</c> process calls issued by <see cref="Acquire"/>. Generous enough
    /// to accommodate legitimate installs against a slow NuGet feed, while guaranteeing a blocked
    /// or unreachable feed fails the build (as JD0027) instead of hanging it forever - since
    /// <see cref="RunEfcpt.AutoAcquireTool"/> defaults to <c>true</c>, a hang here would silently
    /// hang otherwise-unrelated builds.
    /// </summary>
    private const int AcquisitionTimeoutMs = 180_000;

    /// <inheritdoc />
    public ToolAcquisitionOutcome Acquire(ToolAcquisitionRequest request, IBuildLog log)
    {
        try
        {
            Directory.CreateDirectory(request.ManifestDir);

            var manifestPath = Path.Combine(request.ManifestDir, ".config", "dotnet-tools.json");
            if (!File.Exists(manifestPath))
            {
                var initResult = ProcessRunner.Run(
                    log, request.DotNetExe, "new tool-manifest", request.ManifestDir, timeoutMs: AcquisitionTimeoutMs);
                if (!initResult.Success)
                    return ToolAcquisitionOutcome.Failed(DescribeFailure("dotnet new tool-manifest", initResult));
            }

            var versionArg = string.IsNullOrWhiteSpace(request.ToolVersion) ? "" : $" --version \"{request.ToolVersion}\"";
            var installArgs = $"tool install {request.ToolPackageId}{versionArg}";
            var installResult = ProcessRunner.Run(
                log, request.DotNetExe, installArgs, request.ManifestDir, timeoutMs: AcquisitionTimeoutMs);
            if (!installResult.Success)
                return ToolAcquisitionOutcome.Failed(DescribeFailure($"dotnet {installArgs}", installResult));

            return ToolAcquisitionOutcome.Ok();
        }
        catch (Exception ex)
        {
            // Guard against any unexpected failure escaping to the generic
            // TaskExecutionDecorator stack-trace path (IOException, UnauthorizedAccessException,
            // process-launch failures, etc.) - translate it into a Failed outcome so the caller
            // can surface it as the actionable, coded JD0027 error instead.
            return ToolAcquisitionOutcome.Failed(ex.Message);
        }
    }

    private static string DescribeFailure(string command, ProcessResult result)
    {
        var detail = !string.IsNullOrWhiteSpace(result.StdErr) ? result.StdErr : result.StdOut;
        return $"{command} exited with code {result.ExitCode}." +
               (string.IsNullOrWhiteSpace(detail) ? "" : $" {detail.Trim()}");
    }
}
