namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Thrown (surfaced as error <c>JD0027</c> via <see cref="RunEfcpt"/>, never actually thrown
/// across the task boundary - see <see cref="IBuildLog.Error(string, string)"/>) when
/// <c>EfcptAutoAcquireTool</c> attempted to bootstrap an obj-local tool manifest and install the
/// efcpt tool into it, but the underlying <c>dotnet new tool-manifest</c> / <c>dotnet tool
/// install</c> step failed.
/// </summary>
/// <remarks>
/// The message is modeled on <see cref="ProviderDriverNotFoundException"/> and
/// <see cref="EfcptToolNotAvailableOfflineException"/>: it states the problem, the attempted
/// manifest path, the captured failure detail, and concrete fix options.
/// </remarks>
internal sealed class EfcptToolAcquisitionFailedException : Exception
{
    /// <summary>
    /// The URL of the documentation page describing tool acquisition behavior.
    /// </summary>
    private const string DocsUrl = "https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/tool-acquisition.html";

    /// <summary>
    /// Initializes a new instance of <see cref="EfcptToolAcquisitionFailedException"/>, building
    /// an actionable message from the resolved state at the point acquisition failed.
    /// </summary>
    /// <param name="manifestDir">The directory in which acquisition attempted to bootstrap a tool manifest.</param>
    /// <param name="toolPackageId">The configured <c>EfcptToolPackageId</c> value.</param>
    /// <param name="toolVersion">The configured <c>EfcptToolVersion</c> value (may be empty).</param>
    /// <param name="details">Captured failure detail (process exit code/output, or an exception message).</param>
    public EfcptToolAcquisitionFailedException(
        string manifestDir,
        string toolPackageId,
        string toolVersion,
        string details)
        : base(BuildMessage(manifestDir, toolPackageId, toolVersion, details))
    {
    }

    private static string BuildMessage(string manifestDir, string toolPackageId, string toolVersion, string details)
    {
        var versionArg = string.IsNullOrWhiteSpace(toolVersion) ? "" : $" --version {toolVersion}";

        return
            "EfcptAutoAcquireTool attempted to bootstrap a local dotnet tool manifest and install " +
            $"'{toolPackageId}{versionArg}' into it, but the acquisition step failed. " +
            $"Attempted manifest directory: '{manifestDir}'. Details: {details} " +
            "Fix options: " +
            $"(1) install the tool globally ahead of time - run: dotnet tool install --global {toolPackageId}{versionArg}; " +
            "(2) commit a pre-restored tool manifest to source control - run: dotnet new tool-manifest && " +
            $"dotnet tool install {toolPackageId}{versionArg} - and set EfcptAutoAcquireTool=false to use it as-is; " +
            "or (3) enable EfcptOfflineMode and pre-provision the tool via one of the above before building offline. " +
            $"See {DocsUrl} for details.";
    }
}
