using System.Xml.Linq;

namespace JD.Efcpt.Build.Tasks;

internal static class SqlProjectDetector
{
    private static readonly IReadOnlySet<string> SupportedSdkNames = new HashSet<string>(
        ["Microsoft.Build.Sql", "MSBuild.Sdk.SqlProj"],
        StringComparer.OrdinalIgnoreCase);

    public static bool IsSqlProjectReference(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return false;

        var ext = Path.GetExtension(projectPath);
        if (ext.Equals(".sqlproj", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase) &&
            !ext.Equals(".fsproj", StringComparison.OrdinalIgnoreCase))
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
            if (project == null || !string.Equals(project.Name.LocalName, "Project", StringComparison.OrdinalIgnoreCase))
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
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
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
