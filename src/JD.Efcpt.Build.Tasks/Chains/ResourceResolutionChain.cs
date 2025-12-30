namespace JD.Efcpt.Build.Tasks.Chains;

/// <summary>
/// Context for resource resolution containing all search locations and resource name candidates.
/// </summary>
/// <remarks>
/// This is the unified context used by <see cref="ResourceResolutionChain"/> to support
/// both file and directory resolution with a single implementation.
/// </remarks>
public readonly record struct ResourceResolutionContext(
    string OverridePath,
    string ProjectDirectory,
    string SolutionDir,
    bool ProbeSolutionDir,
    string DefaultsRoot,
    IReadOnlyList<string> ResourceNames
);

/// <summary>
/// Unified ResultChain for resolving resources (files or directories) with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a generic implementation that can resolve either files or directories,
/// eliminating duplication between <see cref="FileResolutionChain"/> and <see cref="DirectoryResolutionChain"/>.
/// </para>
/// <para>
/// Resolution order:
/// <list type="number">
/// <item>Explicit override path (if rooted or contains directory separator)</item>
/// <item>Project directory</item>
/// <item>Solution directory (if ProbeSolutionDir is true)</item>
/// <item>Defaults root</item>
/// </list>
/// </para>
/// </remarks>
internal static class ResourceResolutionChain
{
    /// <summary>
    /// Delegate that checks whether a resource exists at the given path.
    /// </summary>
    public delegate bool ExistsPredicate(string path);

    /// <summary>
    /// Delegate that creates an exception when a resource is not found.
    /// </summary>
    public delegate Exception NotFoundExceptionFactory(string message, string? path = null);

    /// <summary>
    /// Resolves a resource using the provided existence predicate and exception factories.
    /// </summary>
    /// <param name="context">The resolution context containing search locations and resource names.</param>
    /// <param name="exists">Predicate to check if a resource exists (e.g., File.Exists or Directory.Exists).</param>
    /// <param name="overrideNotFound">Factory for creating exceptions when override path doesn't exist.</param>
    /// <param name="notFound">Factory for creating exceptions when resource cannot be found anywhere.</param>
    /// <returns>The resolved resource path.</returns>
    /// <exception cref="Exception">Thrown via the exception factories when the resource is not found.</exception>
    public static string Resolve(
        in ResourceResolutionContext context,
        ExistsPredicate exists,
        NotFoundExceptionFactory overrideNotFound,
        NotFoundExceptionFactory notFound)
    {
        // Branch 1: Explicit override path (rooted or contains directory separator)
        if (PathUtils.HasExplicitPath(context.OverridePath))
        {
            var path = PathUtils.FullPath(context.OverridePath, context.ProjectDirectory);
            return exists(path)
                ? path
                : throw overrideNotFound($"Override not found: {path}", path);
        }

        // Branch 2: Search project directory (if provided)
        if (!string.IsNullOrWhiteSpace(context.ProjectDirectory) &&
            TryFindInDirectory(context.ProjectDirectory, context.ResourceNames, exists, out var found))
            return found;

        // Branch 3: Search solution directory (if enabled)
        if (context.ProbeSolutionDir && !string.IsNullOrWhiteSpace(context.SolutionDir))
        {
            var solDir = PathUtils.FullPath(context.SolutionDir, context.ProjectDirectory);
            if (TryFindInDirectory(solDir, context.ResourceNames, exists, out found))
                return found;
        }

        // Branch 4: Search defaults root
        if (!string.IsNullOrWhiteSpace(context.DefaultsRoot) &&
            TryFindInDirectory(context.DefaultsRoot, context.ResourceNames, exists, out found))
            return found;

        // Final fallback: throw descriptive error
        throw notFound(
            $"Unable to locate {string.Join(" or ", context.ResourceNames)}. " +
            "Provide explicit path, place next to project, in solution dir, or ensure defaults are present.");
    }

    private static bool TryFindInDirectory(
        string directory,
        IReadOnlyList<string> resourceNames,
        ExistsPredicate exists,
        out string foundPath)
    {
        // Guard against null inputs - can occur on .NET Framework MSBuild
        if (string.IsNullOrWhiteSpace(directory) || resourceNames == null || resourceNames.Count == 0)
        {
            foundPath = string.Empty;
            return false;
        }

        var matchingCandidate = resourceNames
            .Select(name => Path.Combine(directory, name))
            .FirstOrDefault(candidate => exists(candidate));

        if (matchingCandidate is not null)
        {
            foundPath = matchingCandidate;
            return true;
        }

        foundPath = string.Empty;
        return false;
    }
}
