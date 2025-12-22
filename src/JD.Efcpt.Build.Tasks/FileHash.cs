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
}
