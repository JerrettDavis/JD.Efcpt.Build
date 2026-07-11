namespace JD.Efcpt.Build.Tests.Infrastructure;

/// <summary>
/// Walks up from a starting directory to locate the repository root by matching
/// JD.Efcpt.Build.sln rather than a ".git" directory. In a git worktree, ".git" at
/// the root is a FILE (a gitdir pointer), not a directory, so a Directory.Exists(".git")
/// check fails and a walk-up keyed on it never matches. Matching on the solution file
/// works identically in full clones and worktrees.
/// </summary>
internal static class RepoRootLocator
{
    /// <summary>
    /// Walks up from <paramref name="startDir"/> looking for JD.Efcpt.Build.sln.
    /// Returns the containing directory, or null if none is found.
    /// </summary>
    internal static string? FindRepoRootFrom(string startDir)
    {
        var current = startDir;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "JD.Efcpt.Build.sln")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }
}
