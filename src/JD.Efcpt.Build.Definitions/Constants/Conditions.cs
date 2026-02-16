namespace JD.Efcpt.Build.Definitions.Constants;

/// <summary>
/// Common MSBuild condition expressions used throughout the build definitions.
/// Provides type-safe, reusable condition strings to eliminate duplication.
/// </summary>
public static class Conditions
{
    /// <summary>
    /// Checks if EFCPT is enabled for the project.
    /// </summary>
    public const string EfcptEnabled = "'$(EfcptEnabled)' == 'true'";
    
    /// <summary>
    /// Checks if the current project is a SQL database project.
    /// </summary>
    public const string IsSqlProject = "'$(_EfcptIsSqlProject)' == 'true'";
    
    /// <summary>
    /// Checks if the current project is NOT a SQL database project.
    /// </summary>
    public const string IsNotSqlProject = "'$(_EfcptIsSqlProject)' != 'true'";
    
    /// <summary>
    /// Checks if connection string mode is being used.
    /// </summary>
    public const string UseConnectionString = "'$(_EfcptUseConnectionString)' == 'true'";
    
    /// <summary>
    /// Checks if direct DACPAC mode is being used.
    /// </summary>
    public const string UseDirectDacpac = "'$(_EfcptUseDirectDacpac)' == 'true'";
    
    /// <summary>
    /// Checks if no DACPAC path is specified.
    /// </summary>
    public const string NoDacpac = "'$(EfcptDacpac)' == ''";
    
    /// <summary>
    /// Checks if split outputs mode is enabled.
    /// </summary>
    public const string SplitOutputs = "'$(EfcptSplitOutputs)' == 'true'";
    
    /// <summary>
    /// Checks if the EFCPT fingerprint has changed.
    /// </summary>
    public const string FingerprintChanged = "'$(_EfcptFingerprintChanged)' == 'true'";
    
    /// <summary>
    /// Combines multiple conditions with AND logic.
    /// </summary>
    /// <param name="conditions">The conditions to combine.</param>
    /// <returns>A combined condition string.</returns>
    public static string And(params string[] conditions) => 
        string.Join(" and ", conditions);
    
    /// <summary>
    /// Combines multiple conditions with OR logic.
    /// </summary>
    /// <param name="conditions">The conditions to combine.</param>
    /// <returns>A combined condition string.</returns>
    public static string Or(params string[] conditions) => 
        string.Join(" or ", conditions);
    
    /// <summary>
    /// Creates a condition that checks if EFCPT is enabled AND another condition is true.
    /// </summary>
    /// <param name="condition">The additional condition.</param>
    /// <returns>A combined condition string.</returns>
    public static string EfcptEnabledAnd(string condition) => 
        And(EfcptEnabled, condition);
    
    /// <summary>
    /// Creates a condition for EFCPT-enabled SQL projects.
    /// </summary>
    public static string EfcptEnabledSqlProject => 
        And(EfcptEnabled, IsSqlProject);
    
    /// <summary>
    /// Creates a condition for EFCPT-enabled data access projects (not SQL projects).
    /// </summary>
    public static string EfcptEnabledDataAccess => 
        And(EfcptEnabled, IsNotSqlProject);
}
