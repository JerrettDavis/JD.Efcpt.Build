# Comprehensive Refactoring Plan: DRY, SOLID, and Boilerplate Elimination

## Executive Summary

Current codebase has significant boilerplate and repetition that can be eliminated using:
1. **PatternKit patterns** (Strategy, BranchBuilder, Composer) for declarative logic
2. **Source generators** for repetitive task structures  
3. **Factory methods** for common MSBuild target patterns
4. **Strongly-typed builders** to eliminate magic strings
5. **Template Method pattern** for task boilerplate

---

## Critical Issues Identified

### 1. BuildTransitiveTargetsFactory.cs (827 lines)
**Problem**: Massive monolithic factory with repetitive target creation patterns

**Patterns Found** (70+ occurrences each):
- Target creation with BeforeTargets/AfterTargets/DependsOnTargets
- Task parameter assignment (106+ UsingTask lines)
- PropertyGroup creation with conditions
- Message logging with importance levels
- Error handling with conditions

**Solution**:
- **Target Builder Pattern**: Create fluent DSL for common target structures
- **Task Registry Pattern**: Use PatternKit's `BranchBuilder` for task parameter mapping
- **Message Strategy**: PatternKit `ActionStrategy` for logging based on verbosity
- **Property Group Composer**: Use `Composer` pattern to build property groups declaratively

### 2. Task Classes (40+ files)
**Problem**: Repeated boilerplate in every task class

**Boilerplate Found**:
```csharp
// Every task has this:
[Required]
public string PropertyName { get; set; } = "";

[ProfileInput]
public string AnotherProperty { get; set; } = "";

public override bool Execute()
{
    // Logging setup
    // Validation
    // Error handling
    // Actual logic
}
```

**Solution**:
- **Base Task Template**: Abstract base with Template Method pattern
- **Property Validation Strategy**: PatternKit `TryStrategy` for validation chains
- **Logging Decorator**: PatternKit's existing `ProfilingBehavior` pattern extended
- **Error Handling Chain**: PatternKit `ResultChain` for error recovery

### 3. Magic Strings (100+)
**Problem**: Despite creating constants infrastructure, still using raw strings

**Found**:
- Task names: "ResolveSqlProjAndInputs", "EnsureDacpacBuilt", etc.
- Property names: "_EfcptDacpacPath", "MSBuildProjectFullPath", etc.
- Target names: "BeforeBuild", "CoreCompile", etc.
- Item names: "ProjectReference", "Compile", etc.

**Solution**: Already partially done, needs completion
- Use `MsBuildNames.cs` structs everywhere
- Use `EfcptTaskParameters.cs` for task param names
- Source generator to validate at compile-time

### 4. Task Parameter Mapping
**Problem**: 106-121 lines of repetitive UsingTask declarations

**Current**:
```csharp
t.UsingTask("JD.Efcpt.Build.Tasks.ResolveSqlProjAndInputs", "$(_EfcptTaskAssembly)");
t.UsingTask("JD.Efcpt.Build.Tasks.EnsureDacpacBuilt", "$(_EfcptTaskAssembly)");
// ... 14 more identical lines
```

**Solution**: Already done but not used!
- `UsingTasksRegistry.cs` exists but not utilized in BuildTransitiveTargetsFactory
- Apply it immediately

### 5. Property Group Duplication
**Problem**: Repeated property group patterns

**Solution**: Already done but not used!
- `SharedPropertyGroups.cs` exists with:
  - `ConfigureTaskAssemblyResolution()`
  - `ConfigureNullableReferenceTypes()`
- Create more shared methods for other patterns

---

## Implementation Strategy

### Phase 1: Apply Existing Infrastructure (IMMEDIATE)
✅ Already created but NOT applied:
1. Replace manual `UsingTask` calls with `UsingTasksRegistry.RegisterAll(t)`
2. Replace property groups with `SharedPropertyGroups` methods
3. Apply `MsBuildNames` and `EfcptTaskParameters` constants

### Phase 2: PatternKit Integration (HIGH VALUE)
Use PatternKit patterns to eliminate boilerplate:

**A. Target Creation Strategy**
```csharp
var targetBuilder = TargetCreationStrategy.Create()
    .For("Simple targets with single task")
        .Then(CreateSimpleTaskTarget)
    .For("Pipeline targets with dependencies")
        .Then(CreatePipelineTarget)
    .For("Lifecycle hooks")
        .Then(CreateLifecycleHook)
    .Build();
```

**B. Task Parameter Composer**
```csharp
var applyConfigTask = TaskParameterComposer.For("ApplyConfigOverrides")
    .WithRequiredParam("StagedConfigPath", "$(_EfcptStagedConfig)")
    .WithOptionalParam("LogVerbosity", "$(EfcptLogVerbosity)")
    .WithManyParams(EfcptTaskParameters.ApplyConfigOverrides.AllConfigParams)
    .Build();
```

**C. Message Logging Strategy**
```csharp
var messageStrategy = ActionStrategy<(string msg, string importance)>.Create()
    .When(x => x.importance == "high" && verbosity == "detailed")
        .Then(x => target.Message(x.msg, x.importance))
    .When(x => x.importance == "normal")
        .Then(x => target.Message(x.msg, x.importance))
    .Build();
```

### Phase 3: Task Base Class Hierarchy (SOLID)
Extract common task patterns into base classes:

```csharp
// Template Method pattern
public abstract class EfcptTask : Task
{
    [Required]
    public string ProjectPath { get; set; } = "";
    
    [ProfileInput]
    public string LogVerbosity { get; set; } = "";
    
    public sealed override bool Execute()
    {
        if (!ValidateInputs(out var errors))
        {
            LogErrors(errors);
            return false;
        }
        
        return ExecuteCore();
    }
    
    protected virtual bool ValidateInputs(out string[] errors) => ...;
    protected abstract bool ExecuteCore();
}

// Specialized bases
public abstract class PathResolvingTask : EfcptTask { }
public abstract class ExternalToolTask : EfcptTask { }
public abstract class FingerprintingTask : EfcptTask { }
```

### Phase 4: Source Generator for Task Registration (ADVANCED)
Generate task classes and registration from declarations:

```csharp
[EfcptTask("ResolveSqlProjAndInputs")]
public partial class ResolveSqlProjAndInputs
{
    [Required]
    public string ProjectFullPath { get; set; }
    
    // Generator creates: boilerplate, validation, profiling hooks
}
```

---

## Metrics

### Current State
- BuildTransitiveTargetsFactory.cs: 827 lines
- Task files: 40+ files averaging 200 lines each
- Magic strings: 100+
- Repeated patterns: 70+
- UsingTask declarations: 16 manual
- Property groups: 5+ duplicated structures

### Target State (after all phases)
- BuildTransitiveTargetsFactory.cs: <400 lines (50% reduction)
- Task base classes: 3 bases covering 80% of boilerplate
- Magic strings: 0 (100% constants)
- Repeated patterns: <10 (reused via PatternKit)
- UsingTask declarations: 1 call to Registry
- Property groups: Reused from SharedPropertyGroups

### Code Quality Improvements
- **DRY**: Eliminate 70+ repetitive patterns
- **SOLID**: 
  - SRP: Split monolithic factory
  - OCP: Extensible via strategies
  - LSP: Proper task hierarchy
  - ISP: Focused interfaces
  - DIP: Depend on abstractions (PatternKit patterns)
- **Cognitive Load**: Reduce from "extremely complex" to "straightforward"
- **Testability**: PatternKit patterns are inherently testable
- **Maintainability**: Change in one place, affect many

---

## Next Steps

1. ✅ **IMMEDIATE**: Apply Phase 1 (existing infrastructure)
2. 🔄 **TODAY**: Implement Phase 2A (TargetCreationStrategy)
3. 📅 **THIS WEEK**: Complete Phase 2 (PatternKit integration)
4. 📅 **NEXT SPRINT**: Phase 3 (Task hierarchy refactor)
5. 🔮 **FUTURE**: Phase 4 (Source generators)

---

## PatternKit Patterns to Use

From https://github.com/jerrettdavis/patternkit:

✅ **Strategy Pattern** - For target creation logic branches
✅ **BranchBuilder** - For first-match routing (task type selection)
✅ **ChainBuilder** - For collecting and projecting target sequences
✅ **Composer** - For building complex task parameter sets
✅ **ActionStrategy** - For logging and side-effect patterns
✅ **ResultChain** - For error handling with fallback
✅ **TryStrategy** - For validation chains

All patterns support:
- `in` parameters for struct efficiency
- Zero allocation hot paths
- Fluent, declarative syntax
- Compile-time safety
