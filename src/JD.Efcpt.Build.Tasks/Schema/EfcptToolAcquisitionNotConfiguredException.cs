namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Thrown (surfaced as error <c>JD0028</c> via <see cref="RunEfcpt"/>, never actually thrown
/// across the task boundary - see <see cref="IBuildLog.Error(string, string)"/>) when tool
/// resolution would use a local tool manifest (<c>EfcptToolMode="tool-manifest"</c>, or
/// <c>"auto"</c> with a discovered manifest directory), but that manifest is either absent or
/// does not list the efcpt tool, and <c>EfcptAutoAcquireTool</c> is disabled (or cannot run - no
/// <c>EfcptToolPackageId</c> configured).
/// </summary>
/// <remarks>
/// <para>
/// Without this check, <see cref="RunEfcpt"/> would fall through to
/// <c>dotnet tool run &lt;ToolCommand&gt;</c> against a manifest that cannot resolve the
/// command, guaranteeing a cryptic dotnet-tool failure instead of an actionable, coded error.
/// This exception's message mirrors the shape of <see cref="EfcptToolNotAvailableOfflineException"/>
/// and <see cref="EfcptToolAcquisitionFailedException"/>: it states the problem, the resolved
/// manifest state, and concrete fix options.
/// </para>
/// </remarks>
internal sealed class EfcptToolAcquisitionNotConfiguredException : Exception
{
    /// <summary>
    /// The URL of the documentation page describing tool acquisition behavior.
    /// </summary>
    private const string DocsUrl = "https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/tool-acquisition.html";

    /// <summary>
    /// Initializes a new instance of <see cref="EfcptToolAcquisitionNotConfiguredException"/>,
    /// building an actionable message from the resolved state at the point the pre-flight check
    /// determined the manifest tool resolution would use is not usable and cannot be fixed
    /// automatically.
    /// </summary>
    /// <param name="workingDir">The (already resolved) working directory used for manifest discovery.</param>
    /// <param name="manifestDir">
    /// The directory containing a discovered <c>.config/dotnet-tools.json</c> manifest that does
    /// not list the tool, or <see langword="null"/> if no manifest was found at all.
    /// </param>
    /// <param name="toolPackageId">The configured <c>EfcptToolPackageId</c> value.</param>
    /// <param name="toolVersion">The configured <c>EfcptToolVersion</c> value (may be empty).</param>
    /// <param name="autoAcquireTool">The configured (unparsed) <c>EfcptAutoAcquireTool</c> value.</param>
    public EfcptToolAcquisitionNotConfiguredException(
        string workingDir,
        string? manifestDir,
        string toolPackageId,
        string toolVersion,
        string autoAcquireTool)
        : base(BuildMessage(workingDir, manifestDir, toolPackageId, toolVersion, autoAcquireTool))
    {
    }

    private static string BuildMessage(
        string workingDir,
        string? manifestDir,
        string toolPackageId,
        string toolVersion,
        string autoAcquireTool)
    {
        var versionArg = string.IsNullOrWhiteSpace(toolVersion) ? "" : $" --version {toolVersion}";
        var installDir = manifestDir ?? workingDir;

        var manifestState = manifestDir is null
            ? "No tool manifest was discovered."
            : $"A tool manifest was discovered at '{manifestDir}', but it does not list '{toolPackageId}'.";

        return
            $"Tool resolution would use a local dotnet tool manifest for the efcpt tool, but {manifestState} " +
            $"EfcptAutoAcquireTool is '{autoAcquireTool}' (disabled, or no EfcptToolPackageId was configured to " +
            "install), so it cannot bootstrap or complete the manifest automatically. Proceeding would guarantee " +
            "a failing 'dotnet tool run' invocation, so the build is stopping now instead. Fix options: " +
            $"(1) run: dotnet tool install {toolPackageId}{versionArg} in '{installDir}' " +
            $"(or dotnet new tool-manifest && dotnet tool install {toolPackageId}{versionArg} if no manifest " +
            "exists yet there); " +
            "(2) set EfcptAutoAcquireTool=true so the build bootstraps/installs it automatically; " +
            "or (3) set an explicit EfcptToolPath to a pre-installed efcpt executable. " +
            $"See {DocsUrl} for details.";
    }
}
