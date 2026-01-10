using System.IO.Hashing;
using System.Text;
using JD.Efcpt.Build.Tasks.Utilities;

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
    /// Computes a hash of a SQL file with content normalized to detect only material changes.
    /// </summary>
    /// <param name="path">Path to the SQL file to hash.</param>
    /// <returns>A 16-character hexadecimal hash string.</returns>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="SqlContentNormalizer"/> to normalize SQL content before hashing.
    /// This ensures that only material SQL changes (schema modifications, data changes) trigger a
    /// different hash, while formatting changes (whitespace, comments) are ignored.
    /// </para>
    /// <para>
    /// String literals are preserved exactly as they appear in the original SQL,
    /// so changes to string content will trigger regeneration.
    /// </para>
    /// </remarks>
    public static string HashSqlFileNormalized(string path)
    {
        var content = File.ReadAllText(path);
        var normalized = SqlContentNormalizer.Normalize(content);
        return HashString(normalized);
    }
}
