# Build Profiling

JD.Efcpt.Build includes an optional, configurable profiling framework that captures detailed timing, task execution, and diagnostics during the build process. This feature enables performance analysis, benchmarking, diagnostics, and long-term evolution of the build pipeline.

## Overview

When enabled, build profiling captures:
- **Complete build graph** of all orchestrated steps and tasks
- **Task-level telemetry** including timing, inputs, outputs, and status
- **Configuration inputs** including paths and settings
- **Generated artifacts** with metadata
- **Diagnostics and messages** captured during execution
- **Global metadata** for the build run

The profiling output is deterministic, versioned (using semantic versioning), and written as a single JSON file per build.

## Quick Start

### Enable Profiling

Add the following property to your project file:

```xml
<PropertyGroup>
  <EfcptEnableProfiling>true</EfcptEnableProfiling>
</PropertyGroup>
```

### Run a Build

```bash
dotnet build
```

### Find the Profile Output

By default, the profile is written to:
```
obj/efcpt/build-profile.json
```

## Configuration Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EfcptEnableProfiling` | `false` | Enable or disable build profiling |
| `EfcptProfilingOutput` | `$(EfcptOutput)build-profile.json` | Path where the profiling JSON file will be written |
| `EfcptProfilingVerbosity` | `minimal` | Controls the level of detail captured (values: `minimal`, `detailed`) |

## Example Configuration

```xml
<PropertyGroup>
  <!-- Enable profiling -->
  <EfcptEnableProfiling>true</EfcptEnableProfiling>
  
  <!-- Custom output location -->
  <EfcptProfilingOutput>$(MSBuildProjectDirectory)\build-metrics\profile.json</EfcptProfilingOutput>
  
  <!-- Detailed profiling (captures more data) -->
  <EfcptProfilingVerbosity>detailed</EfcptProfilingVerbosity>
</PropertyGroup>
```

## Output Schema

The profiling output follows a versioned JSON schema (currently `1.0.0`). Here's an example structure showing the complete workflow:

```json
{
  "schemaVersion": "1.0.0",
  "runId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "startTime": "2024-01-11T12:00:00Z",
  "endTime": "2024-01-11T12:01:30Z",
  "duration": "PT1M30S",
  "status": "Success",
  "project": {
    "path": "/path/to/MyProject.csproj",
    "name": "MyProject",
    "targetFramework": "net8.0",
    "configuration": "Debug"
  },
  "configuration": {
    "configPath": "/path/to/efcpt-config.json",
    "renamingPath": "/path/to/efcpt.renaming.json",
    "templateDir": "/path/to/Template",
    "dacpacPath": "/path/to/Database.dacpac",
    "provider": "mssql"
  },
  "buildGraph": {
    "nodes": [
      {
        "id": "node-1",
        "parentId": null,
        "task": {
          "name": "ResolveSqlProjAndInputs",
          "type": "MSBuild",
          "startTime": "2024-01-11T12:00:00Z",
          "endTime": "2024-01-11T12:00:05Z",
          "duration": "PT5S",
          "status": "Success",
          "initiator": "EfcptPipeline",
          "inputs": {
            "ProjectFullPath": "/path/to/MyProject.csproj",
            "Configuration": "Debug",
            "SqlProjOverride": "",
            "ConfigOverride": ""
          },
          "outputs": {
            "SqlProjPath": "/path/to/Database.sqlproj",
            "ResolvedConfigPath": "/path/to/efcpt-config.json",
            "ResolvedRenamingPath": "/path/to/efcpt.renaming.json",
            "ResolvedTemplateDir": "/path/to/Template",
            "UseConnectionString": "false"
          }
        },
        "children": []
      },
      {
        "id": "node-2",
        "parentId": null,
        "task": {
          "name": "RunEfcpt",
          "type": "MSBuild",
          "startTime": "2024-01-11T12:00:30Z",
          "endTime": "2024-01-11T12:01:00Z",
          "duration": "PT30S",
          "status": "Success",
          "initiator": "EfcptGenerateModels",
          "inputs": {
            "ToolMode": "auto",
            "Provider": "mssql",
            "DacpacPath": "/path/to/Database.dacpac",
            "ConfigPath": "/staged/efcpt-config.json",
            "OutputDir": "/output/Generated"
          },
          "outputs": {}
        },
        "children": []
      }
    ],
    "totalTasks": 2,
    "successfulTasks": 2,
    "failedTasks": 0,
    "skippedTasks": 0
  },
  "artifacts": [
    {
      "path": "/output/Model.g.cs",
      "type": "GeneratedModel",
      "size": 2048
    }
  ],
  "metadata": {},
  "diagnostics": []
}
```

### Schema Components

- **schemaVersion**: Semantic version of the schema (MAJOR.MINOR.PATCH)
- **runId**: Unique identifier for this build run
- **startTime** / **endTime**: UTC timestamps in ISO 8601 format (DateTimeOffset)
- **duration**: ISO 8601 duration format (e.g., `PT1M30S` for 1 minute 30 seconds)
- **status**: Overall build status (`Success`, `Failed`, `Skipped`, `Canceled`)
- **project**: Information about the project being built
- **configuration**: Build configuration inputs
- **buildGraph**: Complete graph of all tasks executed
  - **nodes**: Array of task execution nodes with full workflow visibility
  - Each node includes:
    - **inputs**: Dictionary of all input parameters passed to the task
    - **outputs**: Dictionary of all output parameters produced by the task
    - **startTime** / **endTime**: Task-level UTC timestamps
    - **duration**: Task execution time
    - **initiator**: What triggered this task (target, parent task, or orchestration stage)
    - **children**: Nested sub-tasks showing execution hierarchy
- **artifacts**: All generated files and outputs
- **metadata**: Custom key-value pairs for extensibility
- **diagnostics**: Warnings and errors captured during the build

### Workflow Traceability

The profiling output provides **complete workflow visibility**. Reviewers can trace:

1. **Execution order**: Tasks appear in the build graph in the order they executed
2. **Input/output flow**: Each task's outputs become inputs to downstream tasks
3. **Decision points**: Input parameters show configuration choices that affected execution
4. **Timing breakdown**: Start/end times show exactly when each step ran and how long it took
5. **Hierarchy**: Parent/child relationships show nested task execution

**Example workflow analysis**:
```json
{
  "buildGraph": {
    "nodes": [
      {
        "task": {
          "name": "ResolveSqlProjAndInputs",
          "inputs": { "ProjectFullPath": "..." },
          "outputs": { "SqlProjPath": "/path/Database.sqlproj" }
        }
      },
      {
        "task": {
          "name": "RunEfcpt",
          "inputs": { "DacpacPath": "/path/Database.dacpac" },
          "outputs": {}
        }
      }
    ]
  }
}
```

This shows ResolveSqlProjAndInputs resolved the SQL project path, which was then used by RunEfcpt.

## Use Cases

### Performance Analysis

Analyze timing data to identify bottlenecks:

```bash
# Parse the profile to find slowest tasks
cat obj/efcpt/build-profile.json | jq '.buildGraph.nodes[].task | select(.duration > "PT30S")'
```

### Benchmarking

Track build times over commits for regression detection:

```bash
# Extract total duration
cat obj/efcpt/build-profile.json | jq -r '.duration'
```

### CI/CD Integration

Upload profiles to your CI system for historical tracking:

```yaml
# GitHub Actions example
- name: Upload build profile
  uses: actions/upload-artifact@v3
  with:
    name: build-profile
    path: obj/efcpt/build-profile.json
```

### Diagnostics

Capture detailed execution data for troubleshooting:

```bash
# View all diagnostics
cat obj/efcpt/build-profile.json | jq '.diagnostics[]'
```

## Extensibility

The schema supports extensibility through the `extensions` field at multiple levels:

- **Root level**: Global extensions for the build run
- **Task level**: Task-specific extensions
- **Other objects**: Project, configuration, artifacts, etc.

Extensions use JSON Extension Data (`[JsonExtensionData]`) and can be added by:
- Custom MSBuild tasks
- Third-party packages
- Future versions of JD.Efcpt.Build

## Performance Overhead

When **disabled** (default), profiling incurs **near-zero overhead** due to early-exit checks.

When **enabled**, profiling adds minimal overhead:
- Timing measurements use high-resolution `Stopwatch`
- Thread-safe collections minimize contention
- JSON serialization only occurs once at build completion

## Schema Versioning

The profiling schema follows semantic versioning:

- **MAJOR**: Breaking changes to the schema structure
- **MINOR**: Backward-compatible additions (new fields)
- **PATCH**: Bug fixes or clarifications

Tools consuming the profile should check `schemaVersion` and handle compatibility accordingly.

## Backward Compatibility

Future schema versions will:
- Maintain backward compatibility for MINOR and PATCH updates
- Document any breaking changes in MAJOR version updates
- Use optional fields for new features
- Preserve core structure across versions

## Known Limitations

- **v1.0.0 Scope**: Initial release focuses on core task profiling
- **Single Build**: Profiles one build invocation (no cross-process aggregation)
- **Local Output**: Writes to local file system only (no built-in telemetry exporters)

Future releases may add:
- Real-time profiling visualization
- Telemetry exporters (Application Insights, OpenTelemetry, etc.)
- Cross-build aggregation
- More detailed metadata collection

## Troubleshooting

### Profile Not Generated

1. Ensure `EfcptEnableProfiling=true`
2. Check that the output directory exists or can be created
3. Review build output for profiling-related messages

### Large Profile Files

If profiles are unexpectedly large:
- Set `EfcptProfilingVerbosity=minimal` (default)
- Reduce captured metadata in custom tasks
- Consider compressing profiles in CI/CD pipelines

### Schema Compatibility

If you're using tools that consume profiles:
- Always check `schemaVersion` field
- Handle unknown fields gracefully (they may be extensions)
- Update tools when schema MAJOR version changes

## Examples

See the [samples directory](../../samples/) for projects with profiling enabled.

## Contributing

To add profiling to your custom MSBuild tasks:

```csharp
public override bool Execute()
{
    var profiler = ProfilingHelper.GetProfiler(ProjectPath);
    
    using var taskTracker = profiler?.BeginTask(
        nameof(MyTask),
        initiator: "MyTarget",
        inputs: new Dictionary<string, object?> { ["Input1"] = "value" });
    
    // Your task logic here
    
    return true;
}
```

## Security Considerations

### Sensitive Data Protection

JD.Efcpt.Build automatically excludes sensitive data from profiling output:

- **Connection Strings**: All database connection strings are automatically redacted in profiling output. Properties containing connection strings show `"<redacted>"` instead of the actual value.
- **Passwords**: Any properties marked with `[ProfileInput(Exclude = true)]` or `[ProfileOutput(Exclude = true)]` are excluded from capture.
- **Custom Exclusions**: Use `[ProfileInput(Exclude = true)]` on task properties to prevent them from being captured in profiling output.

**Example - Redacted Connection String:**
```json
{
  "task": {
    "name": "RunEfcpt",
    "inputs": {
      "ConnectionString": "<redacted>"
    }
  }
}
```

### Best Practices

1. **Review Profile Output**: Before sharing profiling output (e.g., as CI artifacts), review the JSON file to ensure no sensitive data is present.
2. **Restrict Access**: Treat profiling output files with the same security level as build logs.
3. **Custom Properties**: For custom tasks, use `[ProfileInput(Exclude = true)]` or `[ProfileOutput(Exclude = true)]` to exclude sensitive properties.

## Related Documentation

- [API Reference](api-reference.md)
- [Core Concepts](core-concepts.md)
- [Configuration Guide](configuration.md)
