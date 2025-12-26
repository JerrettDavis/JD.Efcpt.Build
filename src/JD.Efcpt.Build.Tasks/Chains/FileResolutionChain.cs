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
        FileNames
    );
}

/// <summary>
/// ResultChain for resolving files with a multi-tier fallback strategy.
/// </summary>
/// <remarks>
/// <para>
/// This class provides file-specific resolution using <see cref="ResourceResolutionChain"/>
/// with <see cref="File.Exists"/> as the existence predicate.
/// </para>
/// <para>
/// Resolution order:
/// <list type="number">
/// <item>Explicit override path (if rooted or contains directory separator)</item>
/// <item>Project directory</item>
/// <item>Solution directory (if ProbeSolutionDir is true)</item>
/// <item>Defaults root</item>
/// </list>
/// Throws <see cref="FileNotFoundException"/> if file cannot be found in any location.
/// </para>
/// </remarks>
internal static class FileResolutionChain
{
    /// <summary>
    /// Builds a resolution chain for files.
    /// </summary>
    /// <returns>A configured ResultChain for file resolution.</returns>
    public static ResultChain<FileResolutionContext, string> Build()
        => ResultChain<FileResolutionContext, string>.Create()
            .When(static (in _) => true)
            .Then(ctx =>
            {
                var resourceCtx = ctx.ToResourceContext();
                return ResourceResolutionChain.Resolve(
                    in resourceCtx,
                    exists: File.Exists,
                    overrideNotFound: (msg, path) => new FileNotFoundException(msg, path),
                    notFound: (msg, _) => new FileNotFoundException(msg));
            })
            .Build();
}
