#if NETFRAMEWORK
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace JD.Efcpt.Build.Tasks.Compatibility;

/// <summary>
/// Provides polyfills for APIs not available in .NET Framework 4.7.2.
/// </summary>
public static class NetFrameworkPolyfills
{
    /// <summary>
    /// Throws ArgumentNullException if argument is null.
    /// Polyfill for ArgumentNullException.ThrowIfNull (introduced in .NET 6).
    /// </summary>
    public static void ThrowIfNull(object argument, string paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws ArgumentException if argument is null or whitespace.
    /// Polyfill for ArgumentException.ThrowIfNullOrWhiteSpace (introduced in .NET 7).
    /// </summary>
    public static void ThrowIfNullOrWhiteSpace(string argument, string paramName = null)
    {
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
    }

    /// <summary>
    /// Gets a relative path from one path to another.
    /// Polyfill for Path.GetRelativePath (introduced in .NET Standard 2.1).
    /// </summary>
    public static string GetRelativePath(string relativeTo, string path)
    {
        if (string.IsNullOrEmpty(relativeTo))
            throw new ArgumentNullException(nameof(relativeTo));
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        relativeTo = Path.GetFullPath(relativeTo);
        path = Path.GetFullPath(path);

        // Ensure relativeTo ends with directory separator
        if (!relativeTo.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
            !relativeTo.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
        {
            relativeTo += Path.DirectorySeparatorChar;
        }

        var relativeToUri = new Uri(relativeTo);
        var pathUri = new Uri(path);

        if (relativeToUri.Scheme != pathUri.Scheme)
            return path;

        var relativeUri = relativeToUri.MakeRelativeUri(pathUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(pathUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    /// <summary>
    /// Converts byte array to hex string.
    /// Polyfill for Convert.ToHexString (introduced in .NET 5).
    /// </summary>
    public static string ToHexString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));
        return sb.ToString();
    }
}

/// <summary>
/// Polyfill for OperatingSystem static methods (introduced in .NET 5).
/// </summary>
public static class OperatingSystemPolyfill
{
    /// <summary>
    /// Determines whether the current OS platform is Windows.
    /// </summary>
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Determines whether the current OS platform is Linux.
    /// </summary>
    public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Determines whether the current OS platform is macOS.
    /// </summary>
    public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}

/// <summary>
/// Extension methods for KeyValuePair deconstruction (not available in .NET Framework).
/// </summary>
public static class KeyValuePairExtensions
{
    /// <summary>
    /// Deconstructs a <see cref="KeyValuePair{TKey,TValue}"/> into its key and value.
    /// Polyfill for the built-in deconstruction support available on newer target frameworks.
    /// </summary>
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> kvp,
        out TKey key,
        out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }
}

/// <summary>
/// Extension methods for string operations not available in .NET Framework.
/// </summary>
public static class StringPolyfillExtensions
{
    /// <summary>
    /// Splits a string using StringSplitOptions.
    /// Polyfill for string.Split(char, StringSplitOptions) overload.
    /// </summary>
    public static string[] Split(this string str, char separator, StringSplitOptions options)
    {
        return str.Split(new[] { separator }, options);
    }
}
#endif
