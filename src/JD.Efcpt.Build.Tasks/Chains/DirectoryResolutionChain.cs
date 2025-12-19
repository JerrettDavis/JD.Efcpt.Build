using PatternKit.Behavioral.Chain;

namespace JD.Efcpt.Build.Tasks.Chains;

/// <summary>
/// Context for directory resolution containing all search locations and directory name candidates.
/// </summary>
public readonly record struct DirectoryResolutionContext(
    string OverridePath,
    string ProjectDirectory,
    string SolutionDir,
    bool ProbeSolutionDir,
    string DefaultsRoot,
    IReadOnlyList<string> DirNames
);

/// <summary>
/// ResultChain for resolving directories with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
/// <item>Explicit override path (if rooted or contains directory separator)</item>
/// <item>Project directory</item>
/// <item>Solution directory (if ProbeSolutionDir is true)</item>
/// <item>Defaults root</item>
/// </list>
/// Throws DirectoryNotFoundException if directory cannot be found in any location.
/// </remarks>
internal static class DirectoryResolutionChain
{
    public static ResultChain<DirectoryResolutionContext, string> Build()
        => ResultChain<DirectoryResolutionContext, string>.Create()
            // Branch 1: Explicit override path (rooted or contains directory separator)
            .When(static (in ctx)
                => PathUtils.HasExplicitPath(ctx.OverridePath))
            .Then(ctx =>
            {
                var path = PathUtils.FullPath(ctx.OverridePath, ctx.ProjectDirectory);
                return Directory.Exists(path)
                    ? path
                    : throw new DirectoryNotFoundException($"Template override not found: {path}");
            })
            // Branch 2: Search project directory
            .When(static (in ctx)
                => TryFindInDirectory(ctx.ProjectDirectory, ctx.DirNames, out _))
            .Then(ctx =>
                TryFindInDirectory(ctx.ProjectDirectory, ctx.DirNames, out var found)
                    ? found
                    : throw new InvalidOperationException("Should not reach here"))
            // Branch 3: Search solution directory (if enabled)
            .When((in ctx)
                => ctx.ProbeSolutionDir &&
                   !string.IsNullOrWhiteSpace(ctx.SolutionDir) &&
                   TryFindInDirectory(
                       PathUtils.FullPath(ctx.SolutionDir, ctx.ProjectDirectory),
                       ctx.DirNames,
                       out _))
            .Then(ctx =>
            {
                var solDir = PathUtils.FullPath(ctx.SolutionDir, ctx.ProjectDirectory);
                return TryFindInDirectory(solDir, ctx.DirNames, out var found)
                    ? found
                    : throw new InvalidOperationException("Should not reach here");
            })
            // Branch 4: Search defaults root
            .When((in ctx)
                => !string.IsNullOrWhiteSpace(ctx.DefaultsRoot) &&
                   TryFindInDirectory(ctx.DefaultsRoot, ctx.DirNames, out _))
            .Then(ctx
                => TryFindInDirectory(ctx.DefaultsRoot, ctx.DirNames, out var found)
                    ? found
                    : throw new InvalidOperationException("Should not reach here"))
            // Final fallback: throw descriptive error
            .Finally(static (in ctx, out result, _) =>
            {
                result = null;
                throw new DirectoryNotFoundException(
                    $"Unable to locate {string.Join(" or ", ctx.DirNames)}. " +
                    $"Provide EfcptTemplateDir, place Template next to project, in solution dir, or ensure defaults are present.");
            })
            .Build();

    private static bool TryFindInDirectory(
        string baseDirectory,
        IReadOnlyList<string> dirNames,
        out string foundPath)
    {
        foreach (var name in dirNames)
        {
            var candidate = Path.Combine(baseDirectory, name);
            if (!Directory.Exists(candidate)) continue;

            foundPath = candidate;
            return true;
        }

        foundPath = string.Empty;
        return false;
    }
}