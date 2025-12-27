using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Custom assembly resolver that loads dependencies from the task assembly's directory.
/// This is necessary because MSBuild loads task assemblies in its own context,
/// which may not have access to the task's dependencies.
/// </summary>
/// <remarks>
/// This class is excluded from code coverage because it's MSBuild infrastructure code
/// that only activates during assembly resolution failures in the MSBuild host process.
/// Testing would require complex integration scenarios with actual assembly loading failures.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class TaskAssemblyResolver
{
    private static readonly string TaskDirectory = Path.GetDirectoryName(typeof(TaskAssemblyResolver).Assembly.Location)!;
    private static bool _initialized;

    /// <summary>
    /// Initializes the assembly resolver. Call this from static constructors of task classes.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        AssemblyLoadContext.Default.Resolving += OnResolving;
    }

    private static Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        // Try to find the assembly in the task's directory
        var assemblyPath = Path.Combine(TaskDirectory, $"{name.Name}.dll");

        if (File.Exists(assemblyPath))
        {
            try
            {
                return context.LoadFromAssemblyPath(assemblyPath);
            }
            catch
            {
                // If loading fails, let other resolvers try
            }
        }

        return null;
    }
}
