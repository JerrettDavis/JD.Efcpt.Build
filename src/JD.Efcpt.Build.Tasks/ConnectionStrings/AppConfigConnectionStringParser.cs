using System.Xml;
using System.Xml.Linq;

namespace JD.Efcpt.Build.Tasks.ConnectionStrings;

/// <summary>
/// Parses connection strings from app.config or web.config files.
/// </summary>
internal sealed class AppConfigConnectionStringParser
{
    /// <summary>
    /// Attempts to parse a connection string from an app.config or web.config file.
    /// </summary>
    /// <param name="filePath">The path to the config file.</param>
    /// <param name="connectionStringName">The name of the connection string to retrieve.</param>
    /// <param name="log">The build log for warnings and errors.</param>
    /// <returns>A result indicating success or failure, along with the connection string if found.</returns>
    public ConnectionStringResult Parse(string filePath, string connectionStringName, BuildLog log)
    {
        try
        {
            var doc = XDocument.Load(filePath);
            var connectionStrings = doc.Descendants("connectionStrings")
                .Descendants("add")
                .Select(x => new
                {
                    Name = x.Attribute("name")?.Value,
                    ConnectionString = x.Attribute("connectionString")?.Value
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Name) &&
                           !string.IsNullOrWhiteSpace(x.ConnectionString))
                .ToList();

            // Try requested key
            var match = connectionStrings.FirstOrDefault(
                x => x.Name!.Equals(connectionStringName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                return ConnectionStringResult.WithSuccess(match.ConnectionString!, filePath, match.Name!);

            // Fallback to first available
            if (connectionStrings.Any())
            {
                var first = connectionStrings.First();
                log.Warn("JD0002",
                    $"Connection string key '{connectionStringName}' not found in {filePath}. " +
                    $"Using first available connection string '{first.Name}'.");
                return ConnectionStringResult.WithSuccess(first.ConnectionString!, filePath, first.Name!);
            }

            return ConnectionStringResult.NotFound();
        }
        catch (XmlException ex)
        {
            log.Error("JD0011", $"Failed to parse configuration file '{filePath}': {ex.Message}");
            return ConnectionStringResult.Failed();
        }
        catch (IOException ex)
        {
            log.Error("JD0011", $"Failed to read configuration file '{filePath}': {ex.Message}");
            return ConnectionStringResult.Failed();
        }
    }
}
