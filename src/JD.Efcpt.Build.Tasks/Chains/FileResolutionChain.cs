using PatternKit.Behavioral.Chain;

namespace JD.Efcpt.Build.Tasks.Chains;

/// <summary>
/// Context for file resolution containing all search locations and file name candidates.
/// </summary>
public readonly record struct FileResolutionContext(
    string OverridePath,
    string ProjectDirectory,
    string SolutionDir,
    bool ProbeSolutionDir,
    string DefaultsRoot,
    IReadOnlyList<string> FileNames
);

/// <summary>
/// ResultChain for resolving files with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
/// <item>Explicit override path (if rooted or contains directory separator)</item>
/// <item>Project directory</item>
/// <item>Solution directory (if ProbeSolutionDir is true)</item>
/// <item>Defaults root</item>
/// </list>
/// Throws FileNotFoundException if file cannot be found in any location.
/// </remarks>
internal static class FileResolutionChain
{
    public static ResultChain<FileResolutionContext, string> Build()
        => ResultChain<FileResolutionContext, string>.Create()
            // Branch 1: Explicit override path (rooted or contains directory separator)
            .When(static (in ctx) =>
                PathUtils.HasExplicitPath(ctx.OverridePath))
            .Then(ctx =>
            {
                var path = PathUtils.FullPath(ctx.OverridePath, ctx.ProjectDirectory);
                return File.Exists(path)
                    ? path
                    : throw new FileNotFoundException($"Override not found", path);
            })
            // Branch 2: Search project directory
            .When(static (in ctx) =>
                TryFindInDirectory(ctx.ProjectDirectory, ctx.FileNames, out _))
            .Then(ctx =>
                TryFindInDirectory(ctx.ProjectDirectory, ctx.FileNames, out var found)
                    ? found
                    : throw new InvalidOperationException("Should not reach here"))
            // Branch 3: Search solution directory (if enabled)
            .When((in ctx) =>
                ctx.ProbeSolutionDir &&
                !string.IsNullOrWhiteSpace(ctx.SolutionDir) &&
                TryFindInDirectory(
                    PathUtils.FullPath(ctx.SolutionDir, ctx.ProjectDirectory),
                    ctx.FileNames,
                    out _))
            .Then(ctx =>
            {
                var solDir = PathUtils.FullPath(ctx.SolutionDir, ctx.ProjectDirectory);
                return TryFindInDirectory(solDir, ctx.FileNames, out var found)
                    ? found
                    : throw new InvalidOperationException("Should not reach here");
            })
            // Branch 4: Search defaults root
            .When((in ctx) =>
                !string.IsNullOrWhiteSpace(ctx.DefaultsRoot) &&
                TryFindInDirectory(ctx.DefaultsRoot, ctx.FileNames, out _))
            .Then(ctx =>
                TryFindInDirectory(ctx.DefaultsRoot, ctx.FileNames, out var found)
                    ? found
                    : throw new InvalidOperationException("Should not reach here"))
            // Final fallback: throw descriptive error
            .Finally(static (in ctx, out  result, _) =>
            {
                result = null;
                throw new FileNotFoundException(
                    $"Unable to locate {string.Join(" or ", ctx.FileNames)}. " +
                    $"Provide explicit path, place next to project, in solution dir, or ensure defaults are present.");
            })
            .Build();

    private static bool TryFindInDirectory(
        string directory,
        IReadOnlyList<string> fileNames,
        out string foundPath)
    {
        foreach (var name in fileNames)
        {
            var candidate = Path.Combine(directory, name);
            if (File.Exists(candidate))
            {
                foundPath = candidate;
                return true;
            }
        }

        foundPath = string.Empty;
        return false;
    }
}
