using System.Reflection;
using System.Text;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that computes a deterministic fingerprint for efcpt inputs and detects when generation is needed.
/// </summary>
/// <remarks>
/// <para>
/// The fingerprint is derived from multiple sources to ensure regeneration when any relevant input changes:
/// <list type="bullet">
///   <item><description>Library version (JD.Efcpt.Build.Tasks assembly)</description></item>
///   <item><description>Tool version (EF Core Power Tools CLI version)</description></item>
///   <item><description>Database schema (DACPAC or connection string schema fingerprint)</description></item>
///   <item><description>Configuration JSON file contents</description></item>
///   <item><description>Renaming JSON file contents</description></item>
///   <item><description>MSBuild config property overrides (EfcptConfig* properties)</description></item>
///   <item><description>All template files under the template directory</description></item>
///   <item><description>Generated files (optional, via <c>EfcptDetectGeneratedFileChanges</c>)</description></item>
/// </list>
/// For each input, an XxHash64 hash is computed and written into an internal manifest string,
/// which is itself hashed using XxHash64 to produce the final <see cref="Fingerprint"/>.
/// </para>
/// <para>
/// The computed fingerprint is compared to the existing value stored in <see cref="FingerprintFile"/>.
/// If the file is missing or contains a different value, <see cref="HasChanged"/> is set to <c>true</c>,
/// the fingerprint is written back to <see cref="FingerprintFile"/>, and a log message indicates that
/// generation should proceed. Otherwise <see cref="HasChanged"/> is set to <c>false</c> and a message is
/// logged indicating that generation can be skipped.
/// </para>
/// </remarks>
public sealed class ComputeFingerprint : Task
{
    /// <summary>
    /// Path to the DACPAC file to include in the fingerprint (used in .sqlproj mode).
    /// </summary>
    public string DacpacPath { get; set; } = "";

    /// <summary>
    /// Schema fingerprint from QuerySchemaMetadata (used in connection string mode).
    /// </summary>
    public string SchemaFingerprint { get; set; } = "";

    /// <summary>
    /// Indicates whether we're in connection string mode.
    /// </summary>
    public string UseConnectionStringMode { get; set; } = "false";

    /// <summary>
    /// Path to the efcpt configuration JSON file to include in the fingerprint.
    /// </summary>
    [Required]
    public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Path to the efcpt renaming JSON file to include in the fingerprint.
    /// </summary>
    [Required]
    public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Root directory containing template files to include in the fingerprint.
    /// </summary>
    [Required]
    public string TemplateDir { get; set; } = "";

    /// <summary>
    /// Path to the file that stores the last computed fingerprint.
    /// </summary>
    [Required]
    public string FingerprintFile { get; set; } = "";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Version of the EF Core Power Tools CLI tool package being used.
    /// </summary>
    public string ToolVersion { get; set; } = "";

    /// <summary>
    /// Directory containing generated files to optionally include in the fingerprint.
    /// </summary>
    public string GeneratedDir { get; set; } = "";

    /// <summary>
    /// Indicates whether to detect changes to generated files (default: false to avoid overwriting manual edits).
    /// </summary>
    public string DetectGeneratedFileChanges { get; set; } = "false";

    /// <summary>
    /// Serialized JSON string containing MSBuild config property overrides.
    /// </summary>
    public string ConfigPropertyOverrides { get; set; } = "";

    /// <summary>
    /// Newly computed fingerprint value for the current inputs.
    /// </summary>
    [Output]
    public string Fingerprint { get; set; } = "";

    /// <summary>
    /// Indicates whether the fingerprint has changed compared to the last recorded value.
    /// </summary>
    /// <value>
    /// The string <c>true</c> if the fingerprint differs from the value stored in
    /// <see cref="FingerprintFile"/>, or the file is missing; otherwise <c>false</c>.
    /// </value>
    [Output]
    public string HasChanged { get; set; } = "true";

    /// <inheritdoc />
    public override bool Execute()
    {
        var decorator = TaskExecutionDecorator.Create(ExecuteCore);
        var ctx = new TaskExecutionContext(Log, nameof(ComputeFingerprint));
        return decorator.Execute(in ctx);
    }

    private bool ExecuteCore(TaskExecutionContext ctx)
    {
        var log = new BuildLog(ctx.Logger, LogVerbosity);
        var manifest = new StringBuilder();

        // Library version (JD.Efcpt.Build.Tasks assembly)
        var libraryVersion = GetLibraryVersion();
        if (!string.IsNullOrWhiteSpace(libraryVersion))
        {
            manifest.Append("library\0").Append(libraryVersion).Append('\n');
            log.Detail($"Library version: {libraryVersion}");
        }

        // Tool version (EF Core Power Tools CLI)
        if (!string.IsNullOrWhiteSpace(ToolVersion))
        {
            manifest.Append("tool\0").Append(ToolVersion).Append('\n');
            log.Detail($"Tool version: {ToolVersion}");
        }

        // Source fingerprint (DACPAC OR schema fingerprint)
        if (UseConnectionStringMode.IsTrue())
        {
            if (!string.IsNullOrWhiteSpace(SchemaFingerprint))
            {
                manifest.Append("schema\0").Append(SchemaFingerprint).Append('\n');
                log.Detail($"Using schema fingerprint: {SchemaFingerprint}");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(DacpacPath) && File.Exists(DacpacPath))
            {
                // Use schema-based fingerprinting instead of raw file hash
                // This produces consistent hashes for identical schemas even when
                // build-time metadata (paths, timestamps) differs
                var dacpacHash = DacpacFingerprint.Compute(DacpacPath);
                manifest.Append("dacpac").Append('\0').Append(dacpacHash).Append('\n');
                log.Detail($"Using DACPAC (schema fingerprint): {DacpacPath}");
            }
        }

        Append(manifest, ConfigPath, "config");
        Append(manifest, RenamingPath, "renaming");

        // Config property overrides (MSBuild properties that override efcpt-config.json)
        if (!string.IsNullOrWhiteSpace(ConfigPropertyOverrides))
        {
            manifest.Append("config-overrides\0").Append(ConfigPropertyOverrides).Append('\n');
            log.Detail("Including MSBuild config property overrides in fingerprint");
        }

        manifest = Directory
            .EnumerateFiles(TemplateDir, "*", SearchOption.AllDirectories)
            .Select(p => p.Replace('\u005C', '/'))
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(file => (
#if NETFRAMEWORK
                rel: NetFrameworkPolyfills.GetRelativePath(TemplateDir, file).Replace('\u005C', '/'),
#else
                rel: Path.GetRelativePath(TemplateDir, file).Replace('\u005C', '/'),
#endif
                h: FileHash.HashFile(file)))
            .Aggregate(manifest, (builder, data)
                => builder.Append("template/")
                    .Append(data.rel).Append('\0')
                    .Append(data.h).Append('\n'));

        // Generated files (optional, off by default to avoid overwriting manual edits)
        if (!string.IsNullOrWhiteSpace(GeneratedDir) && Directory.Exists(GeneratedDir) && DetectGeneratedFileChanges.IsTrue())
        {
            log.Detail("Detecting generated file changes (EfcptDetectGeneratedFileChanges=true)");
            manifest = Directory
                .EnumerateFiles(GeneratedDir, "*.g.cs", SearchOption.AllDirectories)
                .Select(p => p.Replace('\u005C', '/'))
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(file => (
#if NETFRAMEWORK
                    rel: NetFrameworkPolyfills.GetRelativePath(GeneratedDir, file).Replace('\u005C', '/'),
#else
                    rel: Path.GetRelativePath(GeneratedDir, file).Replace('\u005C', '/'),
#endif
                    h: FileHash.HashFile(file)))
                .Aggregate(manifest, (builder, data)
                    => builder.Append("generated/")
                        .Append(data.rel).Append('\0')
                        .Append(data.h).Append('\n'));
        }

        Fingerprint = FileHash.HashString(manifest.ToString());

        var prior = File.Exists(FingerprintFile) ? File.ReadAllText(FingerprintFile).Trim() : "";
        HasChanged = prior.EqualsIgnoreCase(Fingerprint) ? "false" : "true";

        if (HasChanged.IsTrue())
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FingerprintFile)!);
            File.WriteAllText(FingerprintFile, Fingerprint);
            log.Info($"efcpt fingerprint changed: {Fingerprint}");
        }
        else
        {
            log.Info("efcpt fingerprint unchanged; skipping generation.");
        }

        return true;
    }

    private static string GetLibraryVersion()
    {
        try
        {
            var assembly = typeof(ComputeFingerprint).Assembly;
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString()
                          ?? "";
            return version;
        }
        catch
        {
            return "";
        }
    }

    private static void Append(StringBuilder manifest, string path, string label)
    {
        var full = Path.GetFullPath(path);
        var h = FileHash.HashFile(full);
        manifest.Append(label).Append('\0').Append(h).Append('\n');
    }
}