namespace JD.Efcpt.Build.Core.ConnectionStrings;

/// <summary>
/// Resolves a connection-string source key (for example <c>env</c>, <c>azure-keyvault</c>,
/// <c>aws-secrets</c>) to its <see cref="IConnectionStringSource"/> implementation.
/// </summary>
/// <remarks>
/// <see cref="JD.Efcpt.Build.Core"/> ships <see cref="CoreConnectionStringSourceResolver"/>,
/// which only resolves the in-assembly <c>env</c> source. The MSBuild task host
/// (<c>JD.Efcpt.Build.Tasks</c>) supplies a resolver that additionally discovers and loads
/// satellite connection-string-source packages (Azure Key Vault, AWS Secrets Manager, etc.).
/// </remarks>
public interface IConnectionStringSourceResolver
{
    /// <summary>
    /// Resolves <paramref name="sourceKey"/> to its <see cref="IConnectionStringSource"/>, or
    /// <see langword="null"/> if no source is registered for that key.
    /// </summary>
    /// <param name="sourceKey">The source key to resolve (for example <c>env</c>).</param>
    /// <returns>The resolved source, or <see langword="null"/> when not found.</returns>
    IConnectionStringSource? Resolve(string sourceKey);
}
