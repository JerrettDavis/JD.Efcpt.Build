namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Shared utility types for integration tests.
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Finds the repository root directory.
    /// </summary>
    public static string FindRepoRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "JD.Efcpt.Build.sln")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        var assemblyLocation = typeof(TestUtilities).Assembly.Location;
        current = Path.GetDirectoryName(assemblyLocation);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "JD.Efcpt.Build.sln")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Could not find repository root");
    }

    /// <summary>
    /// Result of executing a dotnet command.
    /// </summary>
    public record CommandResult(bool Success, string Output, string Error, int ExitCode)
    {
        public override string ToString() =>
            $"Exit Code: {ExitCode}\nOutput:\n{Output}\nError:\n{Error}";
    }
}
