using System.Xml.Linq;
using JD.Efcpt.Build.Tasks.Extensions;

namespace JD.Efcpt.Build.Tasks;

internal static class SqlProjectDetector
{
    private static readonly HashSet<string> SupportedSdkNames = new HashSet<string>(
        ["Microsoft.Build.Sql", "MSBuild.Sdk.SqlProj"],
        StringComparer.OrdinalIgnoreCase);

    public static bool IsSqlProjectReference(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return false;

        var ext = Path.GetExtension(projectPath);
        if (ext.EqualsIgnoreCase(".sqlproj"))
            return true;

        if (!ext.EqualsIgnoreCase(".csproj") &&
            !ext.EqualsIgnoreCase(".fsproj"))
            return false;

        return UsesModernSqlSdk(projectPath);
    }

    public static bool UsesModernSqlSdk(string projectPath)
        => HasSupportedSdk(projectPath);

    private static bool HasSupportedSdk(string projectPath)
    {
        try
        {
            if (!File.Exists(projectPath))
                return false;

            var doc = XDocument.Load(projectPath);
            var project = doc.Root;
            if (project == null || !project.Name.LocalName.EqualsIgnoreCase("Project"))
                project = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Project");
            if (project == null)
                return false;

            if (HasSupportedSdkAttribute(project))
                return true;

            return project
                .Descendants()
                .Where(e => e.Name.LocalName == "Sdk")
                .Select(e => e.Attributes().FirstOrDefault(a => a.Name.LocalName == "Name")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Any(IsSupportedSdkName);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasSupportedSdkAttribute(XElement project)
    {
        var sdkAttr = project.Attributes().FirstOrDefault(a => a.Name.LocalName == "Sdk");
        return sdkAttr != null && ParseSdkNames(sdkAttr.Value).Any(IsSupportedSdkName);
    }

    private static IEnumerable<string> ParseSdkNames(string raw)
        => raw
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .Select(entry =>
            {
                var slashIndex = entry.IndexOf('/');
                return slashIndex >= 0 ? entry[..slashIndex].Trim() : entry;
            });

    private static bool IsSupportedSdkName(string? name)
        => !string.IsNullOrWhiteSpace(name) &&
           SupportedSdkNames.Contains(name.Trim());
}
