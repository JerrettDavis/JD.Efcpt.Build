using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JD.Efcpt.Build.Tasks.Profiling;

/// <summary>
/// Represents the complete build graph of orchestrated steps and tasks.
/// </summary>
public sealed class BuildGraph
{
    /// <summary>
    /// Root nodes in the build graph (top-level orchestration steps).
    /// </summary>
    [JsonPropertyName("nodes")]
    public List<BuildGraphNode> Nodes { get; set; } = new();

    /// <summary>
    /// Total number of tasks executed.
    /// </summary>
    [JsonPropertyName("totalTasks")]
    public int TotalTasks { get; set; }

    /// <summary>
    /// Number of tasks that succeeded.
    /// </summary>
    [JsonPropertyName("successfulTasks")]
    public int SuccessfulTasks { get; set; }

    /// <summary>
    /// Number of tasks that failed.
    /// </summary>
    [JsonPropertyName("failedTasks")]
    public int FailedTasks { get; set; }

    /// <summary>
    /// Number of tasks that were skipped.
    /// </summary>
    [JsonPropertyName("skippedTasks")]
    public int SkippedTasks { get; set; }

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// A node in the build graph representing a task or orchestration step.
/// </summary>
public sealed class BuildGraphNode
{
    /// <summary>
    /// Unique identifier for this node.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Parent node ID (null for root nodes).
    /// </summary>
    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    /// <summary>
    /// Task execution details.
    /// </summary>
    [JsonPropertyName("task")]
    public TaskExecution Task { get; set; } = new();

    /// <summary>
    /// Child nodes (sub-tasks or dependent tasks).
    /// </summary>
    [JsonPropertyName("children")]
    public List<BuildGraphNode> Children { get; set; } = new();

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// Detailed information about a task execution.
/// </summary>
public sealed class TaskExecution
{
    /// <summary>
    /// Task name (e.g., "RunEfcpt", "ResolveSqlProjAndInputs").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task version (if applicable).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Task type (e.g., "MSBuild", "Internal", "External").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "MSBuild";

    /// <summary>
    /// UTC timestamp when the task started.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// UTC timestamp when the task completed.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// Task execution duration.
    /// </summary>
    [JsonPropertyName("duration")]
    [JsonConverter(typeof(JsonTimeSpanConverter))]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Task execution status.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TaskStatus Status { get; set; }

    /// <summary>
    /// What initiated this task (e.g., "EfcptGenerateModels", "User").
    /// </summary>
    [JsonPropertyName("initiator")]
    public string? Initiator { get; set; }

    /// <summary>
    /// Input parameters to the task.
    /// </summary>
    [JsonPropertyName("inputs")]
    public Dictionary<string, object?> Inputs { get; set; } = new();

    /// <summary>
    /// Output parameters from the task.
    /// </summary>
    [JsonPropertyName("outputs")]
    public Dictionary<string, object?> Outputs { get; set; } = new();

    /// <summary>
    /// Task-specific metadata and telemetry.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>
    /// Diagnostics captured during task execution.
    /// </summary>
    [JsonPropertyName("diagnostics")]
    public List<DiagnosticMessage> Diagnostics { get; set; } = new();

    /// <summary>
    /// Extension data for custom properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Extensions { get; set; }
}

/// <summary>
/// Status of a task execution.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Task failed with errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Task was skipped (e.g., condition not met).
    /// </summary>
    Skipped,

    /// <summary>
    /// Task was canceled.
    /// </summary>
    Canceled
}
