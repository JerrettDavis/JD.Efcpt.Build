# Eliminating Magic Strings with Strongly-Typed MSBuild Names

## Overview

This directory provides infrastructure to eliminate magic strings in MSBuild fluent definitions by using strongly-typed constants that implement JD.MSBuild.Fluent's type-safe interfaces.

## Problem

The original code had many magic strings scattered throughout:

```csharp
target.BeforeTargets("BeforeBuild", "BeforeRebuild");
target.Task("DetectSqlProject", task =>
{
    task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
    task.Param("SqlServerVersion", "$(SqlServerVersion)");
    task.OutputProperty<IsSqlProject, EfcptIsSqlProject>();
});
```

Issues with magic strings:
- **Typos**: Easy to misspell "BeforeBuild" as "BeforeBuld"
- **Refactoring**: Renaming requires find/replace across multiple files
- **Discoverability**: No IntelliSense completion
- **Type safety**: No compile-time validation

## Solution

Use strongly-typed structs implementing JD.MSBuild.Fluent interfaces:

```csharp
// In MsBuildNames.cs or EfcptTaskParameters.cs
public readonly struct BeforeBuildTarget : IMsBuildTargetName
{
    public string Name => "BeforeBuild";
}

public readonly struct DetectSqlProjectTask : IMsBuildTaskName
{
    public string Name => "DetectSqlProject";
}

public readonly struct ProjectPathParameter : IMsBuildTaskParameterName
{
    public string Name => "ProjectPath";
}
```

Usage:
```csharp
target.BeforeTargets(new MsBuildNames.BeforeBuildTarget(), new MsBuildNames.BeforeRebuildTarget());
target.Task(new MsBuildNames.DetectSqlProjectTask(), task =>
{
    task.Param(new EfcptTaskParameters.ProjectPathParameter(), "$(MSBuildProjectFullPath)");
    task.Param(new EfcptTaskParameters.SqlServerVersionParameter(), "$(SqlServerVersion)");
    task.OutputProperty<EfcptTaskParameters.IsSqlProjectParameter, EfcptIsSqlProject>();
});
```

## Files

### MsBuildNames.cs
Contains well-known MSBuild constants from Microsoft.Common.targets:
- **Targets**: `BeforeBuild`, `Build`, `CoreCompile`, `Clean`, etc.
- **Properties**: `Configuration`, `MSBuildProjectFullPath`, `MSBuildVersion`, `Nullable`, etc.
- **Tasks**: `Message`, `Error`, `Warning`, `Copy`, `Delete`, `Touch`, etc.
- **Task Parameters**: Common parameters like `Text`, `Importance`, `Condition`, `Code`

### EfcptTaskParameters.cs
Contains JD.Efcpt.Build-specific task parameter names:
- Parameters for `DetectSqlProject`, `ResolveSqlProjAndInputs`, `StageEfcptInputs`, etc.
- Input parameters: `ProjectPath`, `SqlProj`, `ConnectionString`, `DacpacPath`
- Output parameters: `IsSqlProject`, `UseConnectionString`, `ResolvedConfig`, `FingerprintChanged`

### BuildTransitivePropsFactory.cs & BuildTransitiveTargetsFactory.cs
Already define property, target, and item type names inline:
- `EfcptEnabled`, `EfcptDacpac`, `EfcptConnectionString` (properties)
- `EfcptResolveInputsTarget`, `EfcptGenerateModelsTarget` (targets)
- `CompileItem`, `EfcptGeneratedScriptsItem` (item types)

## Benefits

### 1. **Compile-Time Safety**
Misspelling a target name causes a compile error instead of a runtime failure:
```csharp
// ✅ Compile error - no such type
target.BeforeTargets(new MsBuildNames.BeforeBuld());

// ❌ Runtime failure - target never runs
target.BeforeTargets("BeforeBuld");
```

### 2. **IntelliSense & Discoverability**
Type `new MsBuildNames.` and IntelliSense shows all available targets, properties, and tasks.

### 3. **Refactoring**
Renaming a constant is a simple "Rename Symbol" operation that updates all usages:
```csharp
// Rename BeforeBuildTarget → PreBuildTarget
// All usages automatically updated by IDE
```

### 4. **Documentation**
Constants can have XML documentation:
```csharp
/// <summary>
/// Standard MSBuild target that runs before the Build target.
/// Use this to perform pre-build validation or setup.
/// </summary>
public readonly struct BeforeBuildTarget : IMsBuildTargetName
{
    public string Name => "BeforeBuild";
}
```

### 5. **DRY Principle**
Each name is defined once, used everywhere:
```csharp
// Single source of truth
public readonly struct DetectSqlProjectTask : IMsBuildTaskName
{
    public string Name => "DetectSqlProject";
}

// Used in multiple places without repetition
t.UsingTask("JD.Efcpt.Build.Tasks.DetectSqlProject", "$(_EfcptTaskAssembly)");
target.Task(new MsBuildNames.DetectSqlProjectTask(), task => { ... });
```

## Migration Strategy

### Phase 1: Infrastructure (✅ Complete)
- [x] Create `MsBuildNames.cs` with common MSBuild names
- [x] Create `EfcptTaskParameters.cs` with task-specific parameters
- [x] Add XML documentation to all types

### Phase 2: Gradual Adoption (In Progress)
Migrate code incrementally to avoid breaking changes:
1. Start with new code - use typed names for all new targets/tasks
2. Migrate high-risk areas - targets that frequently change
3. Migrate during refactoring - when touching existing code
4. Full migration - systematically replace all remaining magic strings

### Phase 3: Source Generator (Future)
Create a source generator to auto-generate these types from:
- MSBuild task assembly metadata (task names and parameters)
- Central YAML/JSON definition file
- Existing XML targets files (reverse engineering)

Example generator input (efcpt-names.yaml):
```yaml
tasks:
  - name: DetectSqlProject
    assembly: JD.Efcpt.Build.Tasks
    parameters:
      inputs:
        - ProjectPath
        - SqlServerVersion
        - DSP
      outputs:
        - IsSqlProject

targets:
  - name: EfcptResolveInputs
    dependsOn: [EfcptDetectSqlProject]
    condition: "'$(EfcptEnabled)' == 'true'"
```

Generated output:
```csharp
// Auto-generated from efcpt-names.yaml
public readonly struct DetectSqlProjectTask : IMsBuildTaskName
{
    public string Name => "DetectSqlProject";
}

public readonly struct EfcptResolveInputsTarget : IMsBuildTargetName
{
    public string Name => "EfcptResolveInputs";
}
```

## Usage Examples

### Defining a Target
```csharp
t.Target("EfcptResolveInputs", target =>
{
    target.BeforeTargets(new MsBuildNames.CoreCompileTarget());
    target.Condition("'$(EfcptEnabled)' == 'true'");
    target.Task(new MsBuildNames.MessageTask(), task =>
    {
        task.Param(new EfcptTaskParameters.TextParameter(), "Resolving EFCPT inputs...");
        task.Param(new EfcptTaskParameters.ImportanceParameter(), "high");
    });
});
```

### Defining a Task Invocation
```csharp
target.Task(new MsBuildNames.DetectSqlProjectTask(), task =>
{
    task.Param(new EfcptTaskParameters.ProjectPathParameter(), "$(MSBuildProjectFullPath)");
    task.Param(new EfcptTaskParameters.SqlServerVersionParameter(), "$(SqlServerVersion)");
    task.OutputProperty<EfcptTaskParameters.IsSqlProjectParameter, EfcptIsSqlProject>();
});
```

### Using Existing Typed Names
```csharp
// Already defined at the bottom of BuildTransitiveTargetsFactory.cs
target.Task(new MsBuildNames.ResolveSqlProjAndInputsTask(), task =>
{
    task.OutputProperty<EfcptTaskParameters.SqlProjParameter, EfcptSqlProj>();
    task.OutputProperty<EfcptTaskParameters.UseConnectionStringParameter, EfcptUseConnectionString>();
});
```

## Best Practices

### 1. **Use Typed Names for New Code**
Always use typed names when writing new definitions:
```csharp
// ✅ Good - type-safe
target.BeforeTargets(new MsBuildNames.BuildTarget());

// ❌ Bad - magic string
target.BeforeTargets("Build");
```

### 2. **Group Related Types**
Keep task parameters together with their task:
```csharp
// In EfcptTaskParameters.cs
public static class EfcptTaskParameters
{
    // DetectSqlProject task parameters
    public readonly struct ProjectPathParameter : IMsBuildTaskParameterName { }
    public readonly struct SqlServerVersionParameter : IMsBuildTaskParameterName { }
    public readonly struct IsSqlProjectParameter : IMsBuildTaskParameterName { }
    
    // ResolveSqlProjAndInputs task parameters
    public readonly struct SqlProjParameter : IMsBuildTaskParameterName { }
    // ...
}
```

### 3. **Add XML Documentation**
Document purpose and usage of each constant:
```csharp
/// <summary>
/// Runs before the Build target to detect if the current project is a SQL database project.
/// Sets the _EfcptIsSqlProject property based on SDK references and MSBuild properties.
/// </summary>
public readonly struct EfcptDetectSqlProjectTarget : IMsBuildTargetName
{
    public string Name => "_EfcptDetectSqlProject";
}
```

### 4. **Prefer Readonly Structs**
Use readonly structs (not classes) for zero allocation:
```csharp
// ✅ Good - readonly struct (zero heap allocation)
public readonly struct BuildTarget : IMsBuildTargetName
{
    public string Name => "Build";
}

// ❌ Bad - class (heap allocation)
public class BuildTarget : IMsBuildTargetName
{
    public string Name => "Build";
}
```

## Future Enhancements

### 1. **Source Generator**
Auto-generate typed names from task assembly metadata or YAML definitions.

### 2. **Validation**
Add analyzer to warn about magic strings:
```csharp
// Analyzer warning: Use MsBuildNames.BuildTarget instead
target.BeforeTargets("Build");
```

### 3. **Central Registry**
Create a central registry of all MSBuild names across the ecosystem:
- Microsoft.Common.targets
- Microsoft.Build.Sql
- MSBuild.Sdk.SqlProj
- Custom tasks

### 4. **Shared NuGet Package**
Publish common MSBuild names as a shared NuGet package:
```xml
<PackageReference Include="MSBuild.Common.TypedNames" Version="1.0.0" />
```

## Contributing

When adding new tasks or targets:
1. Add the task/target name to `MsBuildNames.cs` or `EfcptTaskParameters.cs`
2. Add XML documentation explaining purpose and usage
3. Use the typed name in your fluent definitions
4. Update this README if adding new patterns

## Related

- JD.MSBuild.Fluent: https://github.com/JerrettDavis/JD.MSBuild.Fluent
- MSBuild Typed Names RFC: [Link to design doc]
- Source Generator Design: [Link to design doc]
