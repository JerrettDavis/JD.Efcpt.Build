namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Thrown (surfaced as error <c>JD0026</c> via <see cref="RunEfcpt"/>, never actually thrown
/// across the task boundary - see <see cref="IBuildLog.Error(string, string)"/>) when
/// <c>EfcptOfflineMode</c> is enabled but the efcpt tool cannot be guaranteed to run without a
/// network call.
/// </summary>
/// <remarks>
/// <para>
/// Offline mode refuses to spawn any of the three network-dependent tool resolution/restore
/// branches (dnx execution, tool-manifest restore, global tool <c>update</c>). That only works
/// if the tool is already available through one of three network-free paths: an explicit,
/// existing <c>EfcptToolPath</c>; a local tool manifest that has already been restored (offline
/// mode cannot restore it now); or a global tool already resolvable on <c>PATH</c>. When none of
/// those apply, this exception's message gives the exact pre-provisioning commands needed,
/// mirroring the actionable shape of <see cref="ProviderDriverNotFoundException"/>.
/// </para>
/// </remarks>
internal sealed class EfcptToolNotAvailableOfflineException : Exception
{
    /// <summary>
    /// The URL of the documentation page describing offline/air-gapped usage.
    /// </summary>
    private const string DocsUrl = "https://jerrettdavis.github.io/JD.Efcpt.Build/user-guide/offline.html";

    /// <summary>
    /// Initializes a new instance of <see cref="EfcptToolNotAvailableOfflineException"/>,
    /// building an actionable message from the resolved state at the point offline pre-flight
    /// validation failed.
    /// </summary>
    /// <param name="targetFramework">The detected target framework moniker (e.g. <c>net10.0</c>).</param>
    /// <param name="manifestDir">
    /// The directory containing a discovered <c>.config/dotnet-tools.json</c> manifest, or
    /// <see langword="null"/> if none was found.
    /// </param>
    /// <param name="toolPath">The configured <c>EfcptToolPath</c> value (may be empty).</param>
    /// <param name="toolPackageId">The configured <c>EfcptToolPackageId</c> value.</param>
    /// <param name="toolVersion">The configured <c>EfcptToolVersion</c> value (may be empty).</param>
    public EfcptToolNotAvailableOfflineException(
        string targetFramework,
        string? manifestDir,
        string toolPath,
        string toolPackageId,
        string toolVersion)
        : base(BuildMessage(targetFramework, manifestDir, toolPath, toolPackageId, toolVersion))
    {
    }

    private static string BuildMessage(
        string targetFramework,
        string? manifestDir,
        string toolPath,
        string toolPackageId,
        string toolVersion)
    {
        var versionArg = string.IsNullOrWhiteSpace(toolVersion) ? "" : $" --version {toolVersion}";
        var tfmDisplay = string.IsNullOrWhiteSpace(targetFramework) ? "(not specified)" : targetFramework;
        var manifestDisplay = manifestDir ?? "(none found)";
        var toolPathDisplay = string.IsNullOrWhiteSpace(toolPath) ? "(not set)" : toolPath;

        return
            "EfcptOfflineMode is enabled, but the efcpt tool is not guaranteed to run without a " +
            "network call. Offline mode will not spawn dnx, restore a tool manifest, or update a " +
            "global tool, since all three require network access. Pre-provision the tool before " +
            "building offline using one of the following: " +
            $"(1) a local tool manifest - run: dotnet new tool-manifest && dotnet tool install {toolPackageId}{versionArg}; " +
            $"(2) a global tool - run: dotnet tool install --global {toolPackageId}{versionArg}; " +
            "or (3) set EfcptToolPath to an explicit, pre-installed efcpt executable. " +
            $"Detected state: TargetFramework='{tfmDisplay}', discovered tool manifest directory='{manifestDisplay}', EfcptToolPath='{toolPathDisplay}'. " +
            $"See {DocsUrl} for details.";
    }
}
