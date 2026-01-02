using System.Net.Http;
using System.Text.Json;
using Microsoft.Build.Framework;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that checks NuGet for newer SDK versions and warns if an update is available.
/// </summary>
/// <remarks>
/// <para>
/// This task helps users stay up-to-date with SDK versions since NuGet's SDK resolver
/// doesn't support floating versions or automatic update notifications.
/// </para>
/// <para>
/// The task caches results to avoid network calls on every build:
/// - Cache file: %TEMP%/JD.Efcpt.Sdk.version-cache.json
/// - Cache duration: 24 hours (configurable via CacheHours)
/// </para>
/// </remarks>
public class CheckSdkVersion : Microsoft.Build.Utilities.Task
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    /// <summary>
    /// The current SDK version being used.
    /// </summary>
    [Required]
    public string CurrentVersion { get; set; } = "";

    /// <summary>
    /// The NuGet package ID to check.
    /// </summary>
    public string PackageId { get; set; } = "JD.Efcpt.Sdk";

    /// <summary>
    /// Hours to cache the version check result. Default is 24.
    /// </summary>
    public int CacheHours { get; set; } = 24;

    /// <summary>
    /// If true, always check regardless of cache. Default is false.
    /// </summary>
    public bool ForceCheck { get; set; }

    /// <summary>
    /// Controls the severity level for SDK version update messages.
    /// Valid values: "None", "Info", "Warn", "Error". Defaults to "Warn".
    /// </summary>
    public string WarningLevel { get; set; } = "Warn";

    /// <summary>
    /// The latest version available on NuGet (output).
    /// </summary>
    [Output]
    public string LatestVersion { get; set; } = "";

    /// <summary>
    /// Whether an update is available (output).
    /// </summary>
    [Output]
    public bool UpdateAvailable { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        try
        {
            // Check cache first
            var cacheFile = GetCacheFilePath();
            if (!ForceCheck && TryReadCache(cacheFile, out var cachedVersion, out var cachedTime))
            {
                if (DateTime.UtcNow - cachedTime < TimeSpan.FromHours(CacheHours))
                {
                    LatestVersion = cachedVersion;
                    CheckAndWarn();
                    return true;
                }
            }

            // Query NuGet API
            LatestVersion = GetLatestVersionFromNuGet().GetAwaiter().GetResult();

            // Update cache
            WriteCache(cacheFile, LatestVersion);

            CheckAndWarn();
            return true;
        }
        catch (Exception ex)
        {
            // Don't fail the build for version check issues - just log and continue
            Log.LogMessage(MessageImportance.Low,
                $"EFCPT: Unable to check for SDK updates: {ex.Message}");
            return true;
        }
    }

    private void CheckAndWarn()
    {
        if (string.IsNullOrEmpty(LatestVersion) || string.IsNullOrEmpty(CurrentVersion))
            return;

        if (TryParseVersion(CurrentVersion, out var current) &&
            TryParseVersion(LatestVersion, out var latest) &&
            latest > current)
        {
            UpdateAvailable = true;
            EmitVersionUpdateMessage();
        }
    }

    /// <summary>
    /// Emits the version update message at the configured severity level.
    /// Protected virtual to allow testing without reflection.
    /// </summary>
    protected virtual void EmitVersionUpdateMessage()
    {
        var level = MessageLevelHelpers.Parse(WarningLevel, MessageLevel.Warn);
        var message = $"A newer version of JD.Efcpt.Sdk is available: {LatestVersion} (current: {CurrentVersion}). " +
                     $"Update your project's Sdk attribute or global.json to use the latest version.";
        
        switch (level)
        {
            case MessageLevel.None:
                // Do nothing
                break;
            case MessageLevel.Info:
                Log.LogMessage(
                    subcategory: null,
                    code: "EFCPT002",
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    importance: MessageImportance.High,
                    message: message);
                break;
            case MessageLevel.Warn:
                Log.LogWarning(
                    subcategory: null,
                    warningCode: "EFCPT002",
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: message);
                break;
            case MessageLevel.Error:
                Log.LogError(
                    subcategory: null,
                    errorCode: "EFCPT002",
                    helpKeyword: null,
                    file: null,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: message);
                break;
        }
    }

    private async System.Threading.Tasks.Task<string> GetLatestVersionFromNuGet()
    {
        var url = $"https://api.nuget.org/v3-flatcontainer/{PackageId.ToLowerInvariant()}/index.json";
        var response = await HttpClient.GetStringAsync(url);

        using var doc = JsonDocument.Parse(response);
        var versions = doc.RootElement.GetProperty("versions");

        // Get the last (latest) stable version
        string? latestStable = null;
        foreach (var version in versions.EnumerateArray())
        {
            var versionString = version.GetString();
            if (versionString != null && !versionString.Contains('-'))
            {
                latestStable = versionString;
            }
        }

        return latestStable ?? "";
    }

    private static string GetCacheFilePath()
    {
        return Path.Combine(Path.GetTempPath(), "JD.Efcpt.Sdk.version-cache.json");
    }

    private static bool TryReadCache(string path, out string version, out DateTime cacheTime)
    {
        version = "";
        cacheTime = DateTime.MinValue;

        if (!File.Exists(path))
            return false;

        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            version = doc.RootElement.GetProperty("version").GetString() ?? "";
            cacheTime = doc.RootElement.GetProperty("timestamp").GetDateTime();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteCache(string path, string version)
    {
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                version,
                timestamp = DateTime.UtcNow
            });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore cache write failures
        }
    }

    private static bool TryParseVersion(string versionString, out Version version)
    {
        // Handle versions like "1.0.0" or "1.0.0-preview"
        var cleanVersion = versionString.Split('-')[0];
        return Version.TryParse(cleanVersion, out version!);
    }
}
