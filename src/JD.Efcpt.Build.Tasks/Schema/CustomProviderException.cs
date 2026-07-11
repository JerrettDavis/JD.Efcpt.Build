namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Thrown for failures specific to the <c>customProviders</c> plugin registry (#184): a custom
/// provider key configured without the security opt-in, a collision with a built-in provider
/// key/alias, a custom provider assembly that could not be found or loaded, or a custom provider
/// assembly that does not contain a concrete <see cref="IProviderAdapter"/>. Carries a JD-coded
/// <see cref="Code"/> (<c>JD0017</c>-<c>JD0019</c>, <c>JD0040</c>), analogous to
/// <see cref="ProviderDriverNotFoundException"/> for built-in provider drivers and
/// <c>JD.Efcpt.Build.Core.ConnectionStrings.ConnectionStringSourceException</c> for pluggable
/// connection-string sources (#188).
/// </summary>
public sealed class CustomProviderException : Exception
{
    /// <summary>
    /// Error code JD0017: a custom provider key was configured (selected via
    /// <c>EfcptProvider</c>/<c>@(EfcptCustomProvider)</c>) but <c>EfcptAllowCustomProviders</c>
    /// was not enabled. Custom providers execute third-party code at build time and are
    /// fail-closed (disabled) by default.
    /// </summary>
    public const string NotAllowedCode = "JD0017";

    /// <summary>
    /// Error code JD0018: a custom provider's assembly could not be found on any search path,
    /// or was found but failed to load or instantiate.
    /// </summary>
    public const string AssemblyLoadFailedCode = "JD0018";

    /// <summary>
    /// Error code JD0019: a custom provider key collides with a built-in provider key or alias.
    /// </summary>
    public const string CollidesWithBuiltInCode = "JD0019";

    /// <summary>
    /// Error code JD0040: a custom provider assembly was loaded successfully but does not
    /// contain a concrete <see cref="IProviderAdapter"/> implementation.
    /// </summary>
    public const string NoAdapterFoundCode = "JD0040";

    /// <summary>
    /// Error code JD0041: a custom provider registration is malformed - a blank provider key
    /// (item identity), a missing <c>AssemblyName</c> metadata value, or a duplicate provider key.
    /// </summary>
    public const string MisconfiguredRegistrationCode = "JD0041";

    /// <summary>
    /// Gets the JD-coded error code (<see cref="NotAllowedCode"/>, <see cref="AssemblyLoadFailedCode"/>,
    /// <see cref="CollidesWithBuiltInCode"/>, <see cref="NoAdapterFoundCode"/>, or
    /// <see cref="MisconfiguredRegistrationCode"/>).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomProviderException"/>.
    /// </summary>
    /// <param name="code">The JD-coded error code.</param>
    /// <param name="message">The actionable error message.</param>
    public CustomProviderException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CustomProviderException"/>, preserving the
    /// underlying failure as <see cref="Exception.InnerException"/>.
    /// </summary>
    /// <param name="code">The JD-coded error code.</param>
    /// <param name="message">The actionable error message.</param>
    /// <param name="innerException">The underlying exception that caused this failure.</param>
    public CustomProviderException(string code, string message, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
