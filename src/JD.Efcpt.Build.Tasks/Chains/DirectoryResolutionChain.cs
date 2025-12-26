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
)
{
    /// <summary>
    /// Converts this context to a <see cref="ResourceResolutionContext"/> for use with the unified resolver.
    /// </summary>
    internal ResourceResolutionContext ToResourceContext() => new(
        OverridePath,
        ProjectDirectory,
        SolutionDir,
        ProbeSolutionDir,
        DefaultsRoot,
        DirNames
    );
}

/// <summary>
/// ResultChain for resolving directories with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// <para>
/// This class provides directory-specific resolution using <see cref="ResourceResolutionChain"/>
/// with <see cref="Directory.Exists"/> as the existence predicate.
/// </para>
/// <para>
/// Resolution order:
/// <list type="number">
/// <item>Explicit override path (if rooted or contains directory separator)</item>
/// <item>Project directory</item>
/// <item>Solution directory (if ProbeSolutionDir is true)</item>
/// <item>Defaults root</item>
/// </list>
/// Throws <see cref="DirectoryNotFoundException"/> if directory cannot be found in any location.
/// </para>
/// </remarks>
internal static class DirectoryResolutionChain
{
    /// <summary>
    /// Builds a resolution chain for directories.
    /// </summary>
    /// <returns>A configured ResultChain for directory resolution.</returns>
    public static ResultChain<DirectoryResolutionContext, string> Build()
        => ResultChain<DirectoryResolutionContext, string>.Create()
            .When(static (in _) => true)
            .Then(ctx =>
            {
                var resourceCtx = ctx.ToResourceContext();
                return ResourceResolutionChain.Resolve(
                    in resourceCtx,
                    exists: Directory.Exists,
                    overrideNotFound: (msg, _) => new DirectoryNotFoundException(msg),
                    notFound: (msg, _) => new DirectoryNotFoundException(msg));
            })
            .Build();
}
