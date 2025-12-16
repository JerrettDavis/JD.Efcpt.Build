using System.Security.Cryptography;
using System.Text;

namespace JD.Efcpt.Build.Tasks;

internal static class FileHash
{
    public static string Sha256File(string path)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(path);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Sha256Bytes(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Sha256String(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return Sha256Bytes(bytes);
    }
}
