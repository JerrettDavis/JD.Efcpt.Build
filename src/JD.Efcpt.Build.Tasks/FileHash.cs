using System.IO.Hashing;
using System.Text;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Provides fast, non-cryptographic hashing utilities using XxHash64.
/// </summary>
internal static class FileHash
{
    public static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = new XxHash64();
        hash.Append(stream);
        return hash.GetCurrentHashAsUInt64().ToString("x16");
    }

    public static string HashBytes(byte[] bytes)
    {
        return XxHash64.HashToUInt64(bytes).ToString("x16");
    }

    public static string HashString(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return HashBytes(bytes);
    }

    /// <summary>
    /// Computes a hash of a file with whitespace normalized to detect only material changes.
    /// </summary>
    /// <param name="path">Path to the file to hash.</param>
    /// <returns>A 16-character hexadecimal hash string.</returns>
    /// <remarks>
    /// This method normalizes whitespace (spaces, tabs, line endings) before hashing,
    /// so that only non-whitespace changes trigger a different hash. This is useful
    /// for SQL files where formatting changes shouldn't trigger regeneration.
    /// </remarks>
    public static string HashFileNormalized(string path)
    {
        var content = File.ReadAllText(path);
        // Normalize whitespace: replace all sequences of whitespace with a single space
        var normalized = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();
        return HashString(normalized);
    }
}
