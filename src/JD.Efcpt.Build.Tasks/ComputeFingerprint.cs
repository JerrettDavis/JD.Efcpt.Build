using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using System.Text;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that computes a deterministic fingerprint for efcpt inputs and detects when generation is needed.
/// </summary>
/// <remarks>
/// <para>
/// The fingerprint is derived from the contents of the DACPAC, configuration JSON, renaming JSON, and
/// every file under the template directory. For each input, an XxHash64 hash is computed and written into
/// an internal manifest string, which is itself hashed using XxHash64 to produce the final
/// <see cref="Fingerprint"/>.
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
    [Required] public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Path to the efcpt renaming JSON file to include in the fingerprint.
    /// </summary>
    [Required] public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Root directory containing template files to include in the fingerprint.
    /// </summary>
    [Required] public string TemplateDir { get; set; } = "";

    /// <summary>
    /// Path to the file that stores the last computed fingerprint.
    /// </summary>
    [Required] public string FingerprintFile { get; set; } = "";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Newly computed fingerprint value for the current inputs.
    /// </summary>
    [Output] public string Fingerprint { get; set; } = "";

    /// <summary>
    /// Indicates whether the fingerprint has changed compared to the last recorded value.
    /// </summary>
    /// <value>
    /// The string <c>true</c> if the fingerprint differs from the value stored in
    /// <see cref="FingerprintFile"/>, or the file is missing; otherwise <c>false</c>.
    /// </value>
    [Output] public string HasChanged { get; set; } = "true";

    /// <inheritdoc />
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);
        try
        {
            var manifest = new StringBuilder();

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

            var templateFiles = Directory.EnumerateFiles(TemplateDir, "*", SearchOption.AllDirectories)
                                         .Select(p => p.Replace('\u005C', '/'))
                                         .OrderBy(p => p, StringComparer.Ordinal);

            foreach (var file in templateFiles)
            {
                var rel = Path.GetRelativePath(TemplateDir, file).Replace('\u005C', '/');
                var h = FileHash.HashFile(file);
                manifest.Append("template/").Append(rel).Append('\0').Append(h).Append('\n');
            }

            Fingerprint = FileHash.HashString(manifest.ToString());

            var prior = File.Exists(FingerprintFile) ? File.ReadAllText(FingerprintFile).Trim() : "";
            HasChanged = prior.EqualsIgnoreCase(Fingerprint) ? "false" : "true";

            if (HasChanged == "true")
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
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private static void Append(StringBuilder manifest, string path, string label)
    {
        var full = Path.GetFullPath(path);
        var h = FileHash.HashFile(full);
        manifest.Append(label).Append('\0').Append(h).Append('\n');
    }
}
