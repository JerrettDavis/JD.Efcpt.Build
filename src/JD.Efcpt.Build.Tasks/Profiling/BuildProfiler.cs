using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace JD.Efcpt.Build.Tasks.Profiling;

/// <summary>
/// Core profiler that captures task execution telemetry during a build run.
/// </summary>
/// <remarks>
/// This class is thread-safe and designed to have near-zero overhead when profiling is disabled.
/// When enabled, it captures timing, inputs, outputs, and diagnostics for all tasks.
/// </remarks>
public sealed class BuildProfiler
{
    private static readonly BuildRunOutput EmptyRunOutput = new();
    
    private readonly BuildRunOutput _runOutput;
    private readonly Stack<BuildGraphNode> _nodeStack = new();
    private readonly object _lock = new();
    private readonly bool _enabled;
    private readonly Stopwatch _buildStopwatch = new();

    /// <summary>
    /// Gets whether profiling is enabled.
    /// </summary>
    public bool Enabled => _enabled;

    /// <summary>
    /// Creates a new build profiler.
    /// </summary>
    /// <param name="enabled">Whether profiling is enabled.</param>
    /// <param name="projectPath">Path to the project being built.</param>
    /// <param name="projectName">Name of the project.</param>
    /// <param name="targetFramework">Target framework.</param>
    /// <param name="configuration">Build configuration.</param>
    public BuildProfiler(bool enabled, string projectPath, string projectName, string? targetFramework = null, string? configuration = null)
    {
        _enabled = enabled;
        if (!_enabled)
        {
            _runOutput = EmptyRunOutput;
            return;
        }

        _runOutput = new BuildRunOutput
        {
            StartTime = DateTimeOffset.UtcNow,
            Status = BuildStatus.Success,
            Project = new ProjectInfo
            {
                Path = projectPath,
                Name = projectName,
                TargetFramework = targetFramework,
                Configuration = configuration
            }
        };

        _buildStopwatch.Start();
    }

    /// <summary>
    /// Starts tracking a task execution.
    /// </summary>
    /// <param name="taskName">Name of the task.</param>
    /// <param name="initiator">What initiated this task.</param>
    /// <param name="inputs">Input parameters to the task.</param>
    /// <returns>A token to complete the task tracking.</returns>
    public IDisposable BeginTask(string taskName, string? initiator = null, Dictionary<string, object?>? inputs = null)
    {
        if (!_enabled)
            return NullDisposable.Instance;

        lock (_lock)
        {
            var node = new BuildGraphNode
            {
                ParentId = _nodeStack.Count > 0 ? _nodeStack.Peek().Id : null,
                Task = new TaskExecution
                {
                    Name = taskName,
                    StartTime = DateTimeOffset.UtcNow,
                    Initiator = initiator,
                    Inputs = inputs ?? new Dictionary<string, object?>(),
                    Type = "MSBuild"
                }
            };

            if (_nodeStack.Count == 0)
            {
                _runOutput.BuildGraph.Nodes.Add(node);
            }
            else
            {
                _nodeStack.Peek().Children.Add(node);
            }

            _nodeStack.Push(node);
            return new TaskTracker(this, node);
        }
    }

    /// <summary>
    /// Completes tracking for a task.
    /// </summary>
    internal void EndTask(BuildGraphNode node, bool success, Dictionary<string, object?>? outputs = null, List<DiagnosticMessage>? diagnostics = null)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            node.Task.EndTime = DateTimeOffset.UtcNow;
            node.Task.Duration = node.Task.EndTime.Value - node.Task.StartTime;
            node.Task.Status = success ? TaskStatus.Success : TaskStatus.Failed;
            node.Task.Outputs = outputs ?? new Dictionary<string, object?>();
            
            if (diagnostics != null && diagnostics.Count > 0)
            {
                node.Task.Diagnostics.AddRange(diagnostics);
            }

            if (_nodeStack.Count > 0 && _nodeStack.Peek() == node)
            {
                _nodeStack.Pop();
            }

            // Update graph statistics
            _runOutput.BuildGraph.TotalTasks++;
            if (success)
                _runOutput.BuildGraph.SuccessfulTasks++;
            else
                _runOutput.BuildGraph.FailedTasks++;

            // Update overall build status if any task failed
            if (!success)
            {
                _runOutput.Status = BuildStatus.Failed;
            }
        }
    }

    /// <summary>
    /// Adds configuration information to the build profile.
    /// </summary>
    public void SetConfiguration(BuildConfiguration config)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            _runOutput.Configuration = config;
        }
    }

    /// <summary>
    /// Adds an artifact to the build profile.
    /// </summary>
    public void AddArtifact(ArtifactInfo artifact)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            _runOutput.Artifacts.Add(artifact);
        }
    }

    /// <summary>
    /// Adds metadata to the build profile.
    /// </summary>
    public void AddMetadata(string key, object? value)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            _runOutput.Metadata[key] = value;
        }
    }

    /// <summary>
    /// Adds a diagnostic message to the build profile.
    /// </summary>
    public void AddDiagnostic(DiagnosticLevel level, string message, string? code = null)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            _runOutput.Diagnostics.Add(new DiagnosticMessage
            {
                Level = level,
                Code = code,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Completes the build profile and writes it to a file.
    /// </summary>
    /// <param name="outputPath">Path where the profile should be written.</param>
    public void Complete(string outputPath)
    {
        if (!_enabled)
            return;

        lock (_lock)
        {
            _buildStopwatch.Stop();
            _runOutput.EndTime = DateTimeOffset.UtcNow;
            _runOutput.Duration = _buildStopwatch.Elapsed;

            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write profile to file with indented JSON for human readability
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(_runOutput, options);
            File.WriteAllText(outputPath, json);
        }
    }

    /// <summary>
    /// Gets the current run output for testing or inspection.
    /// </summary>
    internal BuildRunOutput GetRunOutput() => _runOutput;

    private sealed class TaskTracker : IDisposable
    {
        private readonly BuildProfiler _profiler;
        private readonly BuildGraphNode _node;
        private bool _disposed;
        private Dictionary<string, object?>? _outputs;

        public TaskTracker(BuildProfiler profiler, BuildGraphNode node)
        {
            _profiler = profiler;
            _node = node;
        }

        /// <summary>
        /// Sets the output parameters for this task.
        /// </summary>
        public void SetOutputs(Dictionary<string, object?> outputs)
        {
            _outputs = outputs;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _profiler.EndTask(_node, success: true, outputs: _outputs);
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
