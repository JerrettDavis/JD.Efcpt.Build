namespace JD.Efcpt.Build.Tasks.Extensions;

/// <summary>
/// Contains extension methods for performing operations on strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Compares two strings for equality, ignoring case.
    /// </summary>
    /// <param name="other">The string to compare with the current string.</param>
    /// <returns>
    /// True if the strings are equal, ignoring case; otherwise, false.
    /// </returns>
    public static bool EqualsIgnoreCase(this string? str, string? other)
        => string.Equals(str, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines whether the string represents a true value.
    /// </summary>
    /// <returns>
    /// True if the string equals "true", "yes", or "1", ignoring case; otherwise, false.
    /// </returns>
    public static bool IsTrue(this string? str)
        => str.EqualsIgnoreCase("true") ||
           str.EqualsIgnoreCase("yes") ||
           str.EqualsIgnoreCase("on") ||
           str.EqualsIgnoreCase("1") ||
           str.EqualsIgnoreCase("enable") ||
           str.EqualsIgnoreCase("enabled") ||
           str.EqualsIgnoreCase("y");
}