namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// Default <see cref="IConnectionStringSourceResolver"/> that only resolves the in-assembly
/// <see cref="EnvironmentVariableConnectionStringSource"/> (key <c>env</c>). Sourced this way so
/// that <see cref="JD.Efcpt.Build.Core"/> stays self-contained and usable without any satellite
/// package - see <c>JD.Efcpt.Build.Tasks.ConnectionStrings.SatelliteConnectionStringSourceResolver</c>
/// for the MSBuild task host's resolver, which additionally discovers satellite packages.
/// </summary>
public sealed class CoreConnectionStringSourceResolver : IConnectionStringSourceResolver
{
    private static readonly EnvironmentVariableConnectionStringSource EnvSource = new();

    /// <inheritdoc />
    public IConnectionStringSource? Resolve(string sourceKey)
        => string.Equals(sourceKey, EnvSource.Key, StringComparison.OrdinalIgnoreCase)
            ? EnvSource
            : null;
}
