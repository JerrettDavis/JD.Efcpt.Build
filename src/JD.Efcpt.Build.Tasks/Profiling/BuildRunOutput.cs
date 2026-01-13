using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JD.Efcpt.Build.Tasks.Profiling;

/// <summary>
/// Represents the canonical, versioned output of a single JD.Efcpt.Build run.
/// </summary>
/// <remarks>
/// This is the root object for profiling data. It captures a complete view of 
/// the build orchestration, all tasks executed, their timing, and all artifacts generated.
/// The schema is versioned to support backward compatibility.
/// </remarks>
public sealed class BuildRunOutput
{
    /// <summary>
    /// Schema version for this build run output.
    /// </summary>
    /// <remarks>
    /// Uses semantic versioning (MAJOR.MINOR.PATCH).
    /// - MAJOR: Breaking changes to the schema
    /// - MINOR: Backward-compatible additions
    /// - PATCH: Bug fixes or clarifications
    /// </remarks>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Unique identifier for this build run.
    /// </summary>
    [JsonPropertyName("runId")]
    public string RunId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// UTC timestamp when the build started.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// UTC timestamp when the build completed.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Total duration of the build.
    /// </summary>
    [JsonPropertyName("duration")]
    [JsonConverter(typeof(JsonTimeSpanConverter))]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Overall build status.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BuildStatus Status { get; set; }

    /// <summary>
    /// The MSBuild project that was built.
    /// </summary>
    [JsonPropertyName("project")]
    public ProjectInfo Project { get; set; } = new();

    /// <summary>
    /// Configuration inputs for this build.
    /// </summary>
    [JsonPropertyName("configuration")]
    public BuildConfiguration Configuration { get; set; } = new();

    /// <summary>
    /// The build graph representing all orchestrated steps and tasks.
    /// </summary>
    [JsonPropertyName("buildGraph")]
    public BuildGraph BuildGraph { get; set; } = new();

    /// <summary>
    /// All artifacts generated during this build.
    /// </summary>
    [JsonPropertyName("artifacts")]
    public List<ArtifactInfo> Artifacts { get; set; } = new();

    /// <summary>
    /// Global metadata and telemetry for this build.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>
    /// Diagnostics and messages captured during the build.
    /// </summary>
    [JsonPropertyName("diagnostics")]
    public List<DiagnosticMessage> Diagnostics { get; set; } = new();

    /// <summary>
    /// Extension data for custom properties from plugins or extensions.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// Overall status of the build run.
/// </summary>
public enum BuildStatus
{
    /// <summary>
    /// Build completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Build failed with errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Build was skipped (e.g., up-to-date check).
    /// </summary>
    Skipped,

    /// <summary>
    /// Build was canceled.
    /// </summary>
    Canceled
}

/// <summary>
/// Information about the MSBuild project being built.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    /// Full path to the project file.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Project name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Target framework (e.g., "net8.0").
    /// </summary>
    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Build configuration (e.g., "Debug", "Release").
    /// </summary>
    [JsonPropertyName("configuration")]
    public string? Configuration { get; set; }

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// Configuration inputs for the build.
/// </summary>
public sealed class BuildConfiguration
{
    /// <summary>
    /// Path to the efcpt configuration JSON file.
    /// </summary>
    [JsonPropertyName("configPath")]
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Path to the efcpt renaming JSON file.
    /// </summary>
    [JsonPropertyName("renamingPath")]
    public string? RenamingPath { get; set; }

    /// <summary>
    /// Path to the template directory.
    /// </summary>
    [JsonPropertyName("templateDir")]
    public string? TemplateDir { get; set; }

    /// <summary>
    /// Path to the SQL project (if used).
    /// </summary>
    [JsonPropertyName("sqlProjectPath")]
    public string? SqlProjectPath { get; set; }

    /// <summary>
    /// Path to the DACPAC file (if used).
    /// </summary>
    [JsonPropertyName("dacpacPath")]
    public string? DacpacPath { get; set; }

    /// <summary>
    /// Connection string (if used in connection string mode).
    /// </summary>
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Database provider (e.g., "mssql", "postgresql").
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// Information about an artifact generated during the build.
/// </summary>
public sealed class ArtifactInfo
{
    /// <summary>
    /// Full path to the artifact.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Type of artifact (e.g., "GeneratedModel", "DACPAC", "Configuration").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// File hash (if applicable).
    /// </summary>
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    /// <summary>
    /// Size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// A diagnostic message captured during the build.
/// </summary>
public sealed class DiagnosticMessage
{
    /// <summary>
    /// Severity level of the message.
    /// </summary>
    [JsonPropertyName("level")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DiagnosticLevel Level { get; set; }

    /// <summary>
    /// Message code (if applicable).
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// The diagnostic message text.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the message was logged.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// Severity level for diagnostic messages.
/// </summary>
public enum DiagnosticLevel
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}
