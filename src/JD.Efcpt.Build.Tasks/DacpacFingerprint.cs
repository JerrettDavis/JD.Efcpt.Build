using System.IO.Compression;
using System.IO.Hashing;
using System.Text;
using System.Text.RegularExpressions;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Computes a schema-based fingerprint for DACPAC files.
/// </summary>
/// <remarks>
/// <para>
/// A DACPAC is a ZIP archive containing schema metadata. Simply hashing the entire file
/// produces different results for identical schemas because build-time metadata (file paths,
/// timestamps) is embedded in the archive.
/// </para>
/// <para>
/// This class extracts and normalizes the schema-relevant content:
/// <list type="bullet">
///   <item><description><c>model.xml</c> - The schema definition, with path metadata normalized</description></item>
///   <item><description><c>predeploy.sql</c> - Optional pre-deployment script</description></item>
///   <item><description><c>postdeploy.sql</c> - Optional post-deployment script</description></item>
/// </list>
/// </para>
/// <para>
/// The implementation is based on the approach from ErikEJ/DacDeploySkip.
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER
internal static partial class DacpacFingerprint
#else
internal static class DacpacFingerprint
#endif
{
    private const string ModelXmlEntry = "model.xml";
    private const string PreDeployEntry = "predeploy.sql";
    private const string PostDeployEntry = "postdeploy.sql";

    /// <summary>
    /// Computes a fingerprint for the schema content within a DACPAC file.
    /// </summary>
    /// <param name="dacpacPath">Path to the DACPAC file.</param>
    /// <returns>A 16-character hexadecimal fingerprint string.</returns>
    /// <exception cref="FileNotFoundException">The DACPAC file does not exist.</exception>
    /// <exception cref="InvalidOperationException">The DACPAC does not contain a model.xml file.</exception>
    public static string Compute(string dacpacPath)
    {
        if (!File.Exists(dacpacPath))
            throw new FileNotFoundException("DACPAC file not found.", dacpacPath);

        using var archive = ZipFile.OpenRead(dacpacPath);

        var hash = new XxHash64();

        // Process model.xml (required)
        var modelEntry = archive.GetEntry(ModelXmlEntry)
            ?? throw new InvalidOperationException($"DACPAC does not contain {ModelXmlEntry}");

        var normalizedModel = ReadAndNormalizeModelXml(modelEntry);
        hash.Append(normalizedModel);

        // Process optional pre-deployment script
        var preDeployEntry = archive.GetEntry(PreDeployEntry);
        if (preDeployEntry != null)
        {
            var preDeployContent = ReadEntryBytes(preDeployEntry);
            hash.Append(preDeployContent);
        }

        // Process optional post-deployment script
        var postDeployEntry = archive.GetEntry(PostDeployEntry);
        if (postDeployEntry != null)
        {
            var postDeployContent = ReadEntryBytes(postDeployEntry);
            hash.Append(postDeployContent);
        }

        return hash.GetCurrentHashAsUInt64().ToString("x16");
    }

    /// <summary>
    /// Reads model.xml and normalizes metadata to remove build-specific paths.
    /// </summary>
    private static byte[] ReadAndNormalizeModelXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = reader.ReadToEnd();

        // Normalize metadata values that contain full paths
        // These change between builds on different machines but don't affect the schema
        content = NormalizeMetadataPath(content, "FileName");
        content = NormalizeMetadataPath(content, "AssemblySymbolsName");

        return Encoding.UTF8.GetBytes(content);
    }

    /// <summary>
    /// Replaces full paths in Metadata elements with just the filename.
    /// </summary>
    /// <remarks>
    /// Matches patterns like:
    /// <code>&lt;Metadata Name="FileName" Value="C:\path\to\file.dacpac" /&gt;</code>
    /// and replaces with:
    /// <code>&lt;Metadata Name="FileName" Value="file.dacpac" /&gt;</code>
    /// </remarks>
    private static string NormalizeMetadataPath(string xml, string metadataName)
        // Pattern matches: <Metadata Name="FileName" Value="any/path/here" />
        // or: <Metadata Name="FileName" Value="any\path\here" />
        => MetadataRegex(metadataName).Replace(xml, match =>
        {
            var prefix = match.Groups[1].Value;
            var fullPath = match.Groups[2].Value;
            var suffix = match.Groups[3].Value;

            // Extract just the filename from the path
            var fileName = GetFileName(fullPath);
            return $"{prefix}{fileName}{suffix}";
        });

    /// <summary>
    /// Extracts the filename from a path, handling both forward and back slashes.
    /// </summary>
    private static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var lastSlash = path.LastIndexOfAny(['/', '\\']);
        return lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
    }

    /// <summary>
    /// Reads all bytes from a ZIP archive entry.
    /// </summary>
    private static byte[] ReadEntryBytes(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
    
    
    private static Regex MetadataRegex(string metadataName) => metadataName switch
    {
        "FileName" => FileNameMetadataRegex(),
        "AssemblySymbolsName" => AssemblySymbolsMetadataRegex(),
        _ => new Regex($"""(<Metadata\s+Name="{metadataName}"\s+Value=")([^"]+)(")""", RegexOptions.Compiled)
    };

#if NET7_0_OR_GREATER
    /// <summary>
    /// Regex for matching Metadata elements with specific Name attributes.
    /// </summary>
    [GeneratedRegex("""(<Metadata\s+Name="FileName"\s+Value=")([^"]+)(")""", RegexOptions.Compiled)]
    private static partial Regex FileNameMetadataRegex();

    [GeneratedRegex("""(<Metadata\s+Name="AssemblySymbolsName"\s+Value=")([^"]+)(")""", RegexOptions.Compiled)]
    private static partial Regex AssemblySymbolsMetadataRegex();
#else
    private static readonly Regex _fileNameMetadataRegex = new(@"(<Metadata\s+Name=""FileName""\s+Value="")([^""]+)("")", RegexOptions.Compiled);
    private static Regex FileNameMetadataRegex() => _fileNameMetadataRegex;

    private static readonly Regex _assemblySymbolsMetadataRegex = new(@"(<Metadata\s+Name=""AssemblySymbolsName""\s+Value="")([^""]+)("")", RegexOptions.Compiled);
    private static Regex AssemblySymbolsMetadataRegex() => _assemblySymbolsMetadataRegex;
#endif

}
