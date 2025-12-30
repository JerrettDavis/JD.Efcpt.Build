using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Module initializer that runs before any other code in this assembly.
/// This is critical for .NET Framework MSBuild hosts where the assembly resolver
/// must be registered before any types that depend on external assemblies (like PatternKit) are loaded.
/// </summary>
/// <remarks>
/// The module initializer ensures that <see cref="TaskAssemblyResolver"/> is registered
/// at the earliest possible moment - before any JIT compilation of types that reference
/// dependencies like PatternKit.Core.dll. This solves the chicken-and-egg problem where
/// the assembly resolver was previously initialized in <see cref="Decorators.TaskExecutionDecorator"/>'s
/// static constructor, which couldn't run until PatternKit types were already resolved.
/// </remarks>
internal static class ModuleInitializer
{
    /// <summary>
    /// Initializes the assembly resolver before any other code in this assembly runs.
    /// </summary>
    /// <remarks>
    /// CA2255 is suppressed because this is an advanced MSBuild task scenario where
    /// the assembly resolver must be registered before any types are JIT-compiled.
    /// This is exactly the kind of "advanced source generator scenario" the rule mentions.
    /// </remarks>
    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
        Justification = "Required for MSBuild task assembly loading - dependencies must be resolvable before any PatternKit types are JIT compiled")]
    internal static void Initialize()
    {
        TaskAssemblyResolver.Initialize();
    }
}
