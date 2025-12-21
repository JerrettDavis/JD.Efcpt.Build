namespace JD.Efcpt.Build.Tasks.ConnectionStrings;

/// <summary>
/// Represents the result of attempting to resolve a connection string from a configuration file.
/// </summary>
internal sealed record ConnectionStringResult
{
    /// <summary>
    /// Gets a value indicating whether the connection string was successfully resolved.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the resolved connection string value, or null if resolution failed.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Gets the source file path from which the connection string was resolved, or null if not applicable.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Gets the key name that was used to locate the connection string in the configuration file, or null if not applicable.
    /// </summary>
    public string? KeyName { get; init; }

    /// <summary>
    /// Creates a successful result with the specified connection string, source, and key name.
    /// </summary>
    public static ConnectionStringResult WithSuccess(string connectionString, string source, string keyName)
        => new() { Success = true, ConnectionString = connectionString, Source = source, KeyName = keyName };

    /// <summary>
    /// Creates a result indicating that no connection string was found.
    /// </summary>
    public static ConnectionStringResult NotFound()
        => new() { Success = false };

    /// <summary>
    /// Creates a result indicating that parsing or resolution failed.
    /// </summary>
    public static ConnectionStringResult Failed()
        => new() { Success = false };
}
