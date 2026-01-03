using System.Text;
using System.Xml;
using System.Xml.Linq;
using JD.Efcpt.Build.Tasks.Decorators;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that generates a SQL project file from extracted SQL scripts.
/// </summary>
/// <remarks>
/// <para>
/// This task creates a .sqlproj, .csproj, or .fsproj file that references the SQL scripts
/// extracted by sqlpackage. It supports three project formats:
/// <list type="bullet">
///   <item><description>Microsoft.Build.Sql (.sqlproj)</description></item>
///   <item><description>MSBuild.Sdk.SqlProj (.csproj or .fsproj)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class GenerateSqlProj : Task
{
    /// <summary>
    /// Type of SQL project to generate.
    /// </summary>
    /// <remarks>
    /// Valid values:
    /// <list type="bullet">
    ///   <item><description>microsoft-build-sql - Microsoft.Build.Sql SDK (.sqlproj)</description></item>
    ///   <item><description>msbuild-sdk-sqlproj - MSBuild.Sdk.SqlProj (.csproj or .fsproj)</description></item>
    /// </list>
    /// </remarks>
    [Required]
    public string ProjectType { get; set; } = "";

    /// <summary>
    /// Language for MSBuild.Sdk.SqlProj projects.
    /// </summary>
    /// <remarks>
    /// Valid values: "csharp", "fsharp". Only used when ProjectType is "msbuild-sdk-sqlproj".
    /// </remarks>
    public string Language { get; set; } = "csharp";

    /// <summary>
    /// Output path for the generated project file.
    /// </summary>
    [Required]
    public string OutputPath { get; set; } = "";

    /// <summary>
    /// Name of the SQL project (used as the project name).
    /// </summary>
    [Required]
    public string ProjectName { get; set; } = "";

    /// <summary>
    /// Target framework for the project.
    /// </summary>
    public string TargetFramework { get; set; } = "net8.0";

    /// <summary>
    /// SQL Server version (e.g., Sql160 for SQL Server 2022).
    /// </summary>
    public string SqlServerVersion { get; set; } = "Sql160";

    /// <summary>
    /// Microsoft.Build.Sql SDK version.
    /// </summary>
    public string MicrosoftBuildSqlVersion { get; set; } = "2.0.0";

    /// <summary>
    /// MSBuild.Sdk.SqlProj SDK version.
    /// </summary>
    public string MsBuildSdkSqlProjVersion { get; set; } = "3.3.0";

    /// <summary>
    /// Directory containing the extracted SQL scripts.
    /// </summary>
    [Required]
    public string ScriptsDirectory { get; set; } = "";

    /// <summary>
    /// Log verbosity level.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Output parameter: The generated project file path.
    /// </summary>
    [Output]
    public string GeneratedProjectPath { get; set; } = "";

    /// <summary>
    /// Executes the task.
    /// </summary>
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);

        try
        {
            log.Info($"Generating SQL project: {ProjectType}");

            // Validate inputs
            if (!ValidateInputs(log))
            {
                return false;
            }

            // Generate the appropriate project file
            var success = ProjectType.ToLowerInvariant() switch
            {
                "microsoft-build-sql" => GenerateMicrosoftBuildSqlProject(log),
                "msbuild-sdk-sqlproj" => GenerateMsBuildSdkSqlProjProject(log),
                _ => throw new ArgumentException($"Unsupported project type: {ProjectType}")
            };

            if (success)
            {
                GeneratedProjectPath = OutputPath;
                log.Info($"SQL project generated successfully: {OutputPath}");
            }

            return success;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to generate SQL project: {ex.Message}");
            log.Detail($"Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Validates the task inputs.
    /// </summary>
    private bool ValidateInputs(IBuildLog log)
    {
        if (string.IsNullOrEmpty(ProjectType))
        {
            log.Error("ProjectType is required");
            return false;
        }

        if (string.IsNullOrEmpty(OutputPath))
        {
            log.Error("OutputPath is required");
            return false;
        }

        if (string.IsNullOrEmpty(ProjectName))
        {
            log.Error("ProjectName is required");
            return false;
        }

        if (string.IsNullOrEmpty(ScriptsDirectory))
        {
            log.Error("ScriptsDirectory is required");
            return false;
        }

        if (!Directory.Exists(ScriptsDirectory))
        {
            log.Error($"Scripts directory does not exist: {ScriptsDirectory}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Generates a Microsoft.Build.Sql project (.sqlproj).
    /// </summary>
    private bool GenerateMicrosoftBuildSqlProject(IBuildLog log)
    {
        try
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Project",
                    new XAttribute("DefaultTargets", "Build"),
                    new XElement("Sdk",
                        new XAttribute("Name", "Microsoft.Build.Sql"),
                        new XAttribute("Version", MicrosoftBuildSqlVersion)
                    ),
                    new XElement("PropertyGroup",
                        new XElement("Name", ProjectName),
                        new XElement("DSP", GetDatabaseSchemaProvider()),
                        new XElement("ModelCollation", "1033, CI")
                    )
                )
            );

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                log.Detail($"Created output directory: {outputDir}");
            }

            // Save with proper formatting
            using var writer = XmlWriter.Create(OutputPath, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n",
                Encoding = new UTF8Encoding(false) // UTF-8 without BOM
            });
            doc.Save(writer);

            log.Detail($"Generated Microsoft.Build.Sql project: {OutputPath}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to generate Microsoft.Build.Sql project: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Generates an MSBuild.Sdk.SqlProj project (.csproj or .fsproj).
    /// </summary>
    private bool GenerateMsBuildSdkSqlProjProject(IBuildLog log)
    {
        try
        {
            var extension = Language.ToLowerInvariant() == "fsharp" ? ".fsproj" : ".csproj";
            if (!OutputPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                OutputPath = Path.ChangeExtension(OutputPath, extension);
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Project",
                    new XAttribute("Sdk", $"MSBuild.Sdk.SqlProj/{MsBuildSdkSqlProjVersion}"),
                    new XElement("PropertyGroup",
                        new XElement("Name", ProjectName),
                        new XElement("TargetFramework", TargetFramework),
                        new XElement("SqlServerVersion", SqlServerVersion),
                        new XElement("RunSqlCodeAnalysis", "True")
                    )
                )
            );

            // Create output directory if needed
            var outputDir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                log.Detail($"Created output directory: {outputDir}");
            }

            // Save with proper formatting
            using var writer = XmlWriter.Create(OutputPath, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n",
                Encoding = new UTF8Encoding(false) // UTF-8 without BOM
            });
            doc.Save(writer);

            log.Detail($"Generated MSBuild.Sdk.SqlProj project: {OutputPath}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to generate MSBuild.Sdk.SqlProj project: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the database schema provider string based on SQL Server version.
    /// </summary>
    private string GetDatabaseSchemaProvider()
    {
        return SqlServerVersion switch
        {
            "Sql160" => "Microsoft.Data.Tools.Schema.Sql.Sql160DatabaseSchemaProvider",
            "Sql150" => "Microsoft.Data.Tools.Schema.Sql.Sql150DatabaseSchemaProvider",
            "Sql140" => "Microsoft.Data.Tools.Schema.Sql.Sql140DatabaseSchemaProvider",
            "Sql130" => "Microsoft.Data.Tools.Schema.Sql.Sql130DatabaseSchemaProvider",
            "Sql120" => "Microsoft.Data.Tools.Schema.Sql.Sql120DatabaseSchemaProvider",
            "Sql110" => "Microsoft.Data.Tools.Schema.Sql.Sql110DatabaseSchemaProvider",
            "Sql100" => "Microsoft.Data.Tools.Schema.Sql.Sql100DatabaseSchemaProvider",
            _ => "Microsoft.Data.Tools.Schema.Sql.Sql160DatabaseSchemaProvider"
        };
    }
}
