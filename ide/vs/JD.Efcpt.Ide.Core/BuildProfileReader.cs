using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JD.Efcpt.Ide.Core;

/// <summary>
/// Overall status of a build run, as reported in <c>obj/efcpt/build-profile.json</c>.
/// </summary>
/// <remarks>
/// Mirrors <c>JD.Efcpt.Build.Tasks.Profiling.BuildStatus</c>. A raw status value this extension
/// does not recognize (e.g. from a newer schema major) maps to <see cref="Unknown"/> rather than
/// throwing, so an out-of-date extension degrades gracefully instead of crashing the tool window.
/// </remarks>
public enum BuildProfileStatus
{
    /// <summary>The status could not be determined or was not recognized.</summary>
    Unknown,

    /// <summary>Build completed successfully.</summary>
    Success,

    /// <summary>Build failed with errors.</summary>
    Failed,

    /// <summary>Build was skipped (e.g., up-to-date check).</summary>
    Skipped,

    /// <summary>Build was canceled.</summary>
    Canceled
}

/// <summary>
/// A generated artifact, as reported in the <c>artifacts</c> array of
/// <c>obj/efcpt/build-profile.json</c>.
/// </summary>
public sealed class BuildProfileArtifact
{
    /// <summary>Full path to the artifact.</summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Type of artifact (e.g. <c>"GeneratedModel"</c>, <c>"GeneratedDbContext"</c>, <c>"DACPAC"</c>).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Size in bytes, when known.</summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }
}

/// <summary>
/// A single diagnostic message, normalized from the raw <c>diagnostics</c> entry shape written by
/// <c>JD.Efcpt.Build.Tasks.Profiling.DiagnosticMessage</c> (severity is named <c>level</c> in the
/// JSON contract).
/// </summary>
public sealed class BuildProfileDiagnostic
{
    /// <summary>
    /// Lower-cased severity (<c>"info"</c> / <c>"warning"</c> / <c>"error"</c>), for consistent
    /// display regardless of the casing used by the schema's <c>level</c> field.
    /// </summary>
    public string Severity { get; set; } = "info";

    /// <summary>The <c>JDxxxx</c> diagnostic code, when present.</summary>
    public string? Code { get; set; }

    /// <summary>The diagnostic message text.</summary>
    public string? Message { get; set; }

    /// <summary>UTC timestamp when the message was logged, when present.</summary>
    public DateTimeOffset? Timestamp { get; set; }
}

/// <summary>
/// The subset of <c>obj/efcpt/build-profile.json</c> the Visual Studio extension needs, parsed
/// and normalized from the raw schema into UI-friendly shapes.
/// </summary>
public sealed class BuildProfile
{
    /// <summary>The raw <c>schemaVersion</c> string (e.g. <c>"1.0.0"</c>).</summary>
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// <see langword="true"/> when this extension's schema understanding covers the profile's
    /// MAJOR version (see <see cref="BuildProfileReader.SupportedSchemaMajor"/>).
    /// </summary>
    public bool SchemaSupported { get; set; }

    /// <summary>Unique identifier for the build run, when present.</summary>
    public string? RunId { get; set; }

    /// <summary>Raw overall status string as written by the build (e.g. <c>"Success"</c>).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary><see cref="Status"/> parsed into <see cref="BuildProfileStatus"/>, or <see cref="BuildProfileStatus.Unknown"/> when unrecognized.</summary>
    public BuildProfileStatus StatusValue { get; set; } = BuildProfileStatus.Unknown;

    /// <summary>UTC timestamp when the build started, when present.</summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>UTC timestamp when the build completed, when present.</summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>Total build duration, when the <c>duration</c> field parses as an ISO 8601 duration.</summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Count of <see cref="Artifacts"/> whose <see cref="BuildProfileArtifact.Type"/> is
    /// <c>"GeneratedModel"</c> - the headline "models generated" figure shown in the tool window.
    /// </summary>
    public int ModelCount { get; set; }

    /// <summary>All artifacts reported by the build.</summary>
    public IReadOnlyList<BuildProfileArtifact> Artifacts { get; set; } = Array.Empty<BuildProfileArtifact>();

    /// <summary>All diagnostics reported by the build, normalized.</summary>
    public IReadOnlyList<BuildProfileDiagnostic> Diagnostics { get; set; } = Array.Empty<BuildProfileDiagnostic>();

    /// <summary>The built project's name, when present.</summary>
    public string? ProjectName { get; set; }
}

/// <summary>
/// Thrown by <see cref="BuildProfileReader"/> when <c>obj/efcpt/build-profile.json</c> is
/// malformed or missing a required field.
/// </summary>
public sealed class BuildProfileParseException : Exception
{
    /// <summary>
    /// Initializes a new <see cref="BuildProfileParseException"/>.
    /// </summary>
    /// <param name="message">A message describing why parsing failed.</param>
    public BuildProfileParseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="BuildProfileParseException"/> wrapping an underlying cause.
    /// </summary>
    /// <param name="message">A message describing why parsing failed.</param>
    /// <param name="innerException">The underlying exception (e.g. a <see cref="JsonException"/>).</param>
    public BuildProfileParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Reads and parses <c>obj/efcpt/build-profile.json</c> - the build-profiling output written by
/// <c>JD.Efcpt.Build.Tasks.Profiling.BuildRunOutput</c> when <c>EfcptEnableProfiling=true</c> -
/// into the normalized <see cref="BuildProfile"/> shape consumed by the Visual Studio tool window.
/// </summary>
/// <remarks>
/// Mirrors <c>ide/vscode/src/buildProfile.ts</c>. The exact JSON property names below (<c>level</c>
/// for diagnostic severity, <c>path</c>/<c>type</c>/<c>size</c> for artifacts, etc.) are matched
/// against <c>src/JD.Efcpt.Build.Tasks/Profiling/BuildRunOutput.cs</c>, the canonical schema
/// source, not guessed.
/// </remarks>
public static class BuildProfileReader
{
    /// <summary>Highest schema MAJOR version this extension understands.</summary>
    public const int SupportedSchemaMajor = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Reads and parses <c>obj/efcpt/build-profile.json</c> from disk.
    /// </summary>
    /// <param name="path">Full path to the <c>build-profile.json</c> file.</param>
    /// <returns>The parsed, normalized <see cref="BuildProfile"/>.</returns>
    /// <exception cref="BuildProfileParseException">The file could not be read or parsed.</exception>
    public static BuildProfile ReadFile(string path)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new BuildProfileParseException($"Failed to read '{path}': {ex.Message}", ex);
        }

        return Parse(text);
    }

    /// <summary>
    /// Parses raw JSON text into a <see cref="BuildProfile"/>.
    /// </summary>
    /// <param name="jsonText">The full text content of a <c>build-profile.json</c> file.</param>
    /// <returns>The parsed, normalized <see cref="BuildProfile"/>.</returns>
    /// <exception cref="BuildProfileParseException">
    /// <paramref name="jsonText"/> is not valid JSON, is not a JSON object, or is missing a
    /// required field (<c>schemaVersion</c> or <c>status</c>).
    /// </exception>
    public static BuildProfile Parse(string jsonText)
    {
        RawBuildProfile? raw;
        try
        {
            raw = JsonSerializer.Deserialize<RawBuildProfile>(jsonText, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new BuildProfileParseException($"Failed to parse build-profile.json: {ex.Message}", ex);
        }

        if (raw is null)
            throw new BuildProfileParseException("build-profile.json did not contain a JSON object");
        if (string.IsNullOrEmpty(raw.SchemaVersion))
            throw new BuildProfileParseException("build-profile.json is missing \"schemaVersion\"");
        if (string.IsNullOrEmpty(raw.Status))
            throw new BuildProfileParseException("build-profile.json is missing \"status\"");

        var artifacts = raw.Artifacts ?? new List<BuildProfileArtifact>();
        var diagnostics = (raw.Diagnostics ?? new List<RawDiagnostic>())
            .Select(NormalizeDiagnostic)
            .ToList();

        return new BuildProfile
        {
            SchemaVersion = raw.SchemaVersion!,
            SchemaSupported = IsSchemaSupported(raw.SchemaVersion!),
            RunId = raw.RunId,
            Status = raw.Status!,
            StatusValue = Enum.TryParse<BuildProfileStatus>(raw.Status, ignoreCase: true, out var status)
                ? status
                : BuildProfileStatus.Unknown,
            StartTime = raw.StartTime,
            EndTime = raw.EndTime,
            Duration = TryParseDuration(raw.Duration),
            ModelCount = artifacts.Count(a => string.Equals(a.Type, "GeneratedModel", StringComparison.Ordinal)),
            Artifacts = artifacts,
            Diagnostics = diagnostics,
            ProjectName = raw.Project?.Name
        };
    }

    /// <summary>
    /// Normalizes a raw profile diagnostic into the UI shape. The .NET writer emits the severity
    /// as <c>level</c> (<c>"Info"</c> / <c>"Warning"</c> / <c>"Error"</c>); it is lower-cased for
    /// consistent display.
    /// </summary>
    /// <param name="raw">The raw diagnostic entry as deserialized from JSON.</param>
    /// <returns>The normalized <see cref="BuildProfileDiagnostic"/>.</returns>
    public static BuildProfileDiagnostic NormalizeDiagnostic(RawDiagnostic raw)
    {
        var severity = string.IsNullOrEmpty(raw.Level) ? "info" : raw.Level!;
        return new BuildProfileDiagnostic
        {
            Severity = severity.ToLowerInvariant(),
            Code = raw.Code,
            Message = raw.Message,
            Timestamp = raw.Timestamp
        };
    }

    /// <summary>
    /// True when this extension's schema understanding covers the given version's MAJOR component.
    /// </summary>
    /// <param name="schemaVersion">A semantic version string, e.g. <c>"1.0.0"</c>.</param>
    /// <returns><see langword="true"/> when the MAJOR component is present, numeric, and &lt;= <see cref="SupportedSchemaMajor"/>.</returns>
    public static bool IsSchemaSupported(string schemaVersion)
    {
        if (string.IsNullOrEmpty(schemaVersion))
            return false;

        var majorPart = schemaVersion.Split('.')[0];
        return int.TryParse(majorPart, out var major) && major <= SupportedSchemaMajor;
    }

    private static TimeSpan? TryParseDuration(string? duration)
    {
        if (string.IsNullOrEmpty(duration))
            return null;

        try
        {
            return System.Xml.XmlConvert.ToTimeSpan(duration);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Raw deserialization shape of <c>obj/efcpt/build-profile.json</c> (subset this extension
    /// reads). Property names match <c>JD.Efcpt.Build.Tasks.Profiling.BuildRunOutput</c> exactly.
    /// </summary>
    public sealed class RawBuildProfile
    {
        /// <summary>Raw <c>schemaVersion</c> field.</summary>
        [JsonPropertyName("schemaVersion")]
        public string? SchemaVersion { get; set; }

        /// <summary>Raw <c>runId</c> field.</summary>
        [JsonPropertyName("runId")]
        public string? RunId { get; set; }

        /// <summary>Raw <c>startTime</c> field.</summary>
        [JsonPropertyName("startTime")]
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>Raw <c>endTime</c> field.</summary>
        [JsonPropertyName("endTime")]
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>Raw <c>duration</c> field (ISO 8601 duration string, e.g. <c>"PT1M30S"</c>).</summary>
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        /// <summary>Raw <c>status</c> field.</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Raw <c>project</c> field.</summary>
        [JsonPropertyName("project")]
        public RawProject? Project { get; set; }

        /// <summary>Raw <c>artifacts</c> field.</summary>
        [JsonPropertyName("artifacts")]
        public List<BuildProfileArtifact>? Artifacts { get; set; }

        /// <summary>Raw <c>diagnostics</c> field.</summary>
        [JsonPropertyName("diagnostics")]
        public List<RawDiagnostic>? Diagnostics { get; set; }
    }

    /// <summary>Raw shape of the <c>project</c> object in <c>build-profile.json</c> (subset used here).</summary>
    public sealed class RawProject
    {
        /// <summary>Raw <c>name</c> field.</summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Raw shape of an entry in the <c>diagnostics</c> array, as written by
    /// <c>JD.Efcpt.Build.Tasks.Profiling.DiagnosticMessage</c>. Severity is named <c>level</c> in
    /// the JSON contract.
    /// </summary>
    public sealed class RawDiagnostic
    {
        /// <summary>Raw <c>level</c> field (<c>"Info"</c> / <c>"Warning"</c> / <c>"Error"</c>).</summary>
        [JsonPropertyName("level")]
        public string? Level { get; set; }

        /// <summary>Raw <c>code</c> field.</summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>Raw <c>message</c> field.</summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>Raw <c>timestamp</c> field.</summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset? Timestamp { get; set; }
    }
}
