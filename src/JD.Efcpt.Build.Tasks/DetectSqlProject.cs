using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that detects whether the current project is a SQL database project.
/// Uses the SqlProjectDetector to check for SDK-based projects first, then falls back to property-based detection.
/// </summary>
public sealed class DetectSqlProject : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Gets or sets the full path to the project file.
    /// </summary>
    [Required]
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the SqlServerVersion property (for legacy SSDT detection).
    /// </summary>
    public string? SqlServerVersion { get; set; }

    /// <summary>
    /// Gets or sets the DSP property (for legacy SSDT detection).
    /// </summary>
    public string? DSP { get; set; }

    /// <summary>
    /// Gets a value indicating whether the project is a SQL project.
    /// </summary>
    [Output]
    public bool IsSqlProject { get; private set; }

    /// <summary>
    /// Executes the task to detect if the project is a SQL database project.
    /// </summary>
    /// <returns>True if the task executes successfully; otherwise, false.</returns>
    public override bool Execute()
    {
        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            Log.LogError("ProjectPath is required.");
            return false;
        }

        // First, check if project uses a modern SQL SDK via SDK attribute
        var usesModernSdk = SqlProjectDetector.IsSqlProjectReference(ProjectPath);
        
        if (usesModernSdk)
        {
            IsSqlProject = true;
            Log.LogMessage(MessageImportance.Low, 
                "Detected SQL project via SDK attribute: {0}", ProjectPath);
            return true;
        }

        // Fall back to property-based detection for legacy SSDT projects
        var hasLegacyProperties = !string.IsNullOrEmpty(SqlServerVersion) || !string.IsNullOrEmpty(DSP);
        
        if (hasLegacyProperties)
        {
            IsSqlProject = true;
            Log.LogMessage(MessageImportance.Low, 
                "Detected SQL project via MSBuild properties (legacy SSDT): {0}", ProjectPath);
            return true;
        }

        IsSqlProject = false;
        Log.LogMessage(MessageImportance.Low, 
            "Not a SQL project: {0}", ProjectPath);
        return true;
    }
}
