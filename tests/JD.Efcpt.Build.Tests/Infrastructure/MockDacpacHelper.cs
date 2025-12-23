using System.IO.Compression;
using System.Text;

namespace JD.Efcpt.Build.Tests.Infrastructure;

/// <summary>
/// Helper class for creating mock DACPAC files (ZIP archives with model.xml) in tests.
/// </summary>
/// <remarks>
/// A DACPAC is a ZIP archive containing schema metadata. This helper creates minimal
/// valid DACPACs for testing purposes, with support for pre/post deploy scripts.
/// </remarks>
internal static class MockDacpacHelper
{
    /// <summary>
    /// Creates a mock DACPAC file with a simple table schema.
    /// </summary>
    /// <param name="folder">The test folder to create the DACPAC in.</param>
    /// <param name="fileName">The DACPAC file name (e.g., "test.dacpac").</param>
    /// <param name="tableName">The table name to include in the schema (e.g., "Users").</param>
    /// <returns>The full path to the created DACPAC file.</returns>
    public static string Create(TestFolder folder, string fileName, string tableName)
    {
        var dacpacPath = Path.Combine(folder.Root, fileName);
        CreateAtPath(dacpacPath, tableName);
        return dacpacPath;
    }

    /// <summary>
    /// Creates a mock DACPAC file at a specific path with a simple table schema.
    /// </summary>
    /// <param name="dacpacPath">The full path where the DACPAC should be created.</param>
    /// <param name="tableName">The table name to include in the schema (e.g., "Users").</param>
    /// <remarks>
    /// If a file already exists at the path, it will be deleted before creating the new DACPAC.
    /// </remarks>
    public static void CreateAtPath(string dacpacPath, string tableName)
    {
        var modelXml = GenerateModelXml(Path.GetFileName(dacpacPath), tableName);
        CreateFromModelXml(dacpacPath, modelXml);
    }

    /// <summary>
    /// Creates a mock DACPAC file with custom model XML and optional deploy scripts.
    /// </summary>
    /// <param name="folder">The test folder to create the DACPAC in.</param>
    /// <param name="fileName">The DACPAC file name (e.g., "test.dacpac").</param>
    /// <param name="modelXml">The complete model.xml content.</param>
    /// <param name="preDeploy">Optional pre-deployment script content.</param>
    /// <param name="postDeploy">Optional post-deployment script content.</param>
    /// <returns>The full path to the created DACPAC file.</returns>
    public static string CreateWithScripts(
        TestFolder folder,
        string fileName,
        string modelXml,
        string? preDeploy = null,
        string? postDeploy = null)
    {
        var dacpacPath = Path.Combine(folder.Root, fileName);
        CreateFromModelXml(dacpacPath, modelXml, preDeploy, postDeploy);
        return dacpacPath;
    }

    /// <summary>
    /// Generates standard model.xml content for a simple table schema.
    /// </summary>
    /// <param name="fileName">The DACPAC file name for metadata.</param>
    /// <param name="tableName">The table name to include in the schema.</param>
    /// <returns>The model.xml content as a string.</returns>
    public static string GenerateModelXml(string fileName, string tableName)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="C:\\builds\\{fileName}" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[{tableName}]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;
    }

    /// <summary>
    /// Creates a DACPAC file from model XML content with optional deploy scripts.
    /// </summary>
    private static void CreateFromModelXml(
        string dacpacPath,
        string modelXml,
        string? preDeploy = null,
        string? postDeploy = null)
    {
        // Delete existing file if present (ZipArchiveMode.Create throws if file exists)
        if (File.Exists(dacpacPath))
            File.Delete(dacpacPath);

        using var archive = ZipFile.Open(dacpacPath, ZipArchiveMode.Create);

        // Add model.xml (required)
        WriteEntry(archive, "model.xml", modelXml);

        // Add optional pre-deployment script
        if (preDeploy != null)
            WriteEntry(archive, "predeploy.sql", preDeploy);

        // Add optional post-deployment script
        if (postDeploy != null)
            WriteEntry(archive, "postdeploy.sql", postDeploy);
    }

    /// <summary>
    /// Creates an invalid DACPAC file (ZIP archive without model.xml) for testing error handling.
    /// </summary>
    /// <param name="folder">The test folder to create the DACPAC in.</param>
    /// <param name="fileName">The DACPAC file name (e.g., "invalid.dacpac").</param>
    /// <returns>The full path to the created DACPAC file.</returns>
    public static string CreateInvalid(TestFolder folder, string fileName)
    {
        var dacpacPath = Path.Combine(folder.Root, fileName);

        // Delete existing file if present
        if (File.Exists(dacpacPath))
            File.Delete(dacpacPath);

        using var archive = ZipFile.Open(dacpacPath, ZipArchiveMode.Create);
        // Create a DACPAC without model.xml (invalid)
        WriteEntry(archive, "other.txt", "not a model");

        return dacpacPath;
    }

    private static void WriteEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(content);
    }
}
