using System.Text;
using System.Text.RegularExpressions;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Generates DbContext names from SQL projects, DACPACs, or connection strings.
/// </summary>
/// <remarks>
/// <para>
/// This class provides logic to automatically derive a meaningful DbContext name from various sources:
/// <list type="bullet">
///   <item><description>SQL Project: Uses the project file name (e.g., "Database.csproj" → "DatabaseContext")</description></item>
///   <item><description>DACPAC: Uses the DACPAC filename with special characters removed (e.g., "Our_Database20251225.dacpac" → "OurDatabaseContext")</description></item>
///   <item><description>Connection String: Extracts the database name (e.g., "Database=MyDb" → "MyDbContext")</description></item>
/// </list>
/// </para>
/// <para>
/// All names are humanized by:
/// <list type="bullet">
///   <item><description>Removing file extensions</description></item>
///   <item><description>Removing non-letter characters except underscores (replaced with empty string)</description></item>
///   <item><description>Converting PascalCase (handling underscores as word boundaries)</description></item>
///   <item><description>Appending "Context" suffix if not already present</description></item>
/// </list>
/// </para>
/// </remarks>
#if NET7_0_OR_GREATER
public static partial class DbContextNameGenerator
#else
public static class DbContextNameGenerator
#endif
{
    private const string DefaultContextName = "MyDbContext";
    private const string ContextSuffix = "Context";

    /// <summary>
    /// Generates a DbContext name from the provided SQL project path.
    /// </summary>
    /// <param name="sqlProjPath">Full path to the SQL project file</param>
    /// <returns>Generated context name or null if unable to resolve</returns>
    /// <example>
    /// <code>
    /// var name = DbContextNameGenerator.FromSqlProject("/path/to/Database.csproj");
    /// // Returns: "DatabaseContext"
    /// 
    /// var name = DbContextNameGenerator.FromSqlProject("/path/to/Org.Unit.SystemData.sqlproj");
    /// // Returns: "SystemDataContext"
    /// </code>
    /// </example>
    public static string? FromSqlProject(string? sqlProjPath)
    {
        if (string.IsNullOrWhiteSpace(sqlProjPath))
            return null;

        try
        {
            var fileName = GetFileNameWithoutExtension(sqlProjPath);
            return HumanizeName(fileName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a DbContext name from the provided DACPAC file path.
    /// </summary>
    /// <param name="dacpacPath">Full path to the DACPAC file</param>
    /// <returns>Generated context name or null if unable to resolve</returns>
    /// <example>
    /// <code>
    /// var name = DbContextNameGenerator.FromDacpac("/path/to/Our_Database20251225.dacpac");
    /// // Returns: "OurDatabaseContext"
    /// 
    /// var name = DbContextNameGenerator.FromDacpac("/path/to/MyDb.dacpac");
    /// // Returns: "MyDbContext"
    /// </code>
    /// </example>
    public static string? FromDacpac(string? dacpacPath)
    {
        if (string.IsNullOrWhiteSpace(dacpacPath))
            return null;

        try
        {
            var fileName = GetFileNameWithoutExtension(dacpacPath);
            return HumanizeName(fileName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the filename without extension from a path, handling both Unix and Windows paths.
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The filename without extension</returns>
    private static string GetFileNameWithoutExtension(string path)
    {
        // Handle both Unix (/) and Windows (\) path separators
        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        var fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        
        // Remove extension
        var lastDot = fileName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            fileName = fileName.Substring(0, lastDot);
        }
        
        return fileName;
    }

    /// <summary>
    /// Generates a DbContext name from the provided connection string.
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <returns>Generated context name or null if unable to resolve</returns>
    /// <example>
    /// <code>
    /// var name = DbContextNameGenerator.FromConnectionString(
    ///     "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
    /// // Returns: "MyDataBaseContext"
    /// 
    /// var name = DbContextNameGenerator.FromConnectionString(
    ///     "Data Source=sample.db");
    /// // Returns: "SampleContext" (from filename if Database keyword not found)
    /// </code>
    /// </example>
    public static string? FromConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        try
        {
            // Try to extract database name using various patterns
            var dbName = TryExtractDatabaseName(connectionString);
            if (!string.IsNullOrWhiteSpace(dbName))
                return HumanizeName(dbName);

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a DbContext name using multiple strategies in priority order.
    /// </summary>
    /// <param name="sqlProjPath">Optional SQL project path</param>
    /// <param name="dacpacPath">Optional DACPAC file path</param>
    /// <param name="connectionString">Optional connection string</param>
    /// <returns>Generated context name or the default "MyDbContext" if unable to resolve</returns>
    /// <remarks>
    /// Priority order:
    /// 1. SQL Project name
    /// 2. DACPAC filename
    /// 3. Connection string database name
    /// 4. Default "MyDbContext"
    /// </remarks>
    public static string Generate(
        string? sqlProjPath,
        string? dacpacPath,
        string? connectionString)
    {
        // Priority 1: SQL Project
        var name = FromSqlProject(sqlProjPath);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        // Priority 2: DACPAC
        name = FromDacpac(dacpacPath);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        // Priority 3: Connection String
        name = FromConnectionString(connectionString);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        // Fallback: Default name
        return DefaultContextName;
    }

    /// <summary>
    /// Humanizes a raw name into a proper DbContext name.
    /// </summary>
    /// <param name="rawName">The raw name to humanize</param>
    /// <returns>Humanized context name</returns>
    /// <remarks>
    /// Process:
    /// 1. Handle dotted namespaces by taking the last segment (e.g., "Org.Unit.SystemData" → "SystemData")
    /// 2. Remove trailing digits (e.g., "Database20251225" → "Database")
    /// 3. Split on underscores/hyphens and capitalize each part
    /// 4. Remove all non-letter characters
    /// 5. Ensure PascalCase
    /// 6. Append "Context" suffix if not already present
    /// </remarks>
    private static string HumanizeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return DefaultContextName;

        // Handle dotted namespaces (e.g., "Org.Unit.SystemData" → "SystemData")
        var dotParts = rawName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        var baseName = dotParts.Length > 0 ? dotParts[^1] : rawName;

        // Remove digits at the end (common in DACPAC names like "MyDb20251225.dacpac")
        var nameWithoutTrailingDigits = TrailingDigitsRegex().Replace(baseName, "");
        if (string.IsNullOrWhiteSpace(nameWithoutTrailingDigits))
            nameWithoutTrailingDigits = baseName; // Keep original if only digits

        // Split on underscores/hyphens and capitalize each part, then join
        var parts = nameWithoutTrailingDigits
            .Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(ToPascalCase)
            .ToArray();

        if (parts.Length == 0)
            return DefaultContextName;

        // Join all parts together (e.g., "sample_db" → "SampleDb")
        var joined = string.Concat(parts);

        // Remove any remaining non-letter characters
        var cleaned = NonLetterRegex().Replace(joined, "");

        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length == 0)
            return DefaultContextName;

        // Ensure it starts with uppercase
        cleaned = cleaned.Length == 1 
            ? char.ToUpperInvariant(cleaned[0]).ToString()
            : char.ToUpperInvariant(cleaned[0]) + cleaned[1..];

        // Add "Context" suffix if not already present
        if (!cleaned.EndsWith(ContextSuffix, StringComparison.OrdinalIgnoreCase))
            cleaned += ContextSuffix;

        return cleaned;
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length == 0)
            return string.Empty;

        // If already PascalCase or single word, just ensure first letter is uppercase
        if (!input.Contains(' ') && !input.Contains('-'))
        {
            return input.Length == 1
                ? char.ToUpperInvariant(input[0]).ToString()
                : char.ToUpperInvariant(input[0]) + input[1..];
        }

        // Split on spaces or hyphens and capitalize each word
        var words = input.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        
        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                result.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                    result.Append(word[1..]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Attempts to extract the database name from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string</param>
    /// <returns>Database name if found, otherwise null</returns>
    private static string? TryExtractDatabaseName(string connectionString)
    {
        // Try "Database=" pattern (SQL Server, PostgreSQL, MySQL)
        var match = DatabaseKeywordRegex().Match(connectionString);
        if (match.Success)
            return match.Groups["name"].Value.Trim();

        // Try "Initial Catalog=" pattern (SQL Server)
        match = InitialCatalogKeywordRegex().Match(connectionString);
        if (match.Success)
            return match.Groups["name"].Value.Trim();

        // Try "Data Source=" for SQLite (extract filename without path and extension)
        match = DataSourceKeywordRegex().Match(connectionString);
        if (match.Success)
        {
            var dataSource = match.Groups["name"].Value.Trim();
            // If it's a file path (contains / or \) or file with extension, extract just the filename without extension
            if (dataSource.Contains('/') || 
                dataSource.Contains('\\') ||
                dataSource.Contains('.'))
            {
                // Handle both Unix and Windows paths
                var fileName = dataSource;
                var lastSlash = Math.Max(dataSource.LastIndexOf('/'), dataSource.LastIndexOf('\\'));
                if (lastSlash >= 0)
                {
                    fileName = dataSource.Substring(lastSlash + 1);
                }
                
                // Remove extension if present
                var lastDot = fileName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    fileName = fileName.Substring(0, lastDot);
                }
                
                return fileName;
            }
            // Plain database name without path or extension
            return dataSource;
        }

        return null;
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"[^a-zA-Z]", RegexOptions.Compiled)]
    private static partial Regex NonLetterRegex();

    [GeneratedRegex(@"\d+$", RegexOptions.Compiled)]
    private static partial Regex TrailingDigitsRegex();

    [GeneratedRegex(@"(?:Database|Db)\s*=\s*(?<name>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DatabaseKeywordRegex();

    [GeneratedRegex(@"Initial\s+Catalog\s*=\s*(?<name>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InitialCatalogKeywordRegex();

    [GeneratedRegex(@"Data\s+Source\s*=\s*(?<name>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DataSourceKeywordRegex();
#else
    private static readonly Regex _nonLetterRegex = new(@"[^a-zA-Z]", RegexOptions.Compiled);
    private static Regex NonLetterRegex() => _nonLetterRegex;

    private static readonly Regex _trailingDigitsRegex = new(@"\d+$", RegexOptions.Compiled);
    private static Regex TrailingDigitsRegex() => _trailingDigitsRegex;

    private static readonly Regex _databaseKeywordRegex = new(@"(?:Database|Db)\s*=\s*(?<name>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex DatabaseKeywordRegex() => _databaseKeywordRegex;

    private static readonly Regex _initialCatalogKeywordRegex = new(@"Initial\s+Catalog\s*=\s*(?<name>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex InitialCatalogKeywordRegex() => _initialCatalogKeywordRegex;

    private static readonly Regex _dataSourceKeywordRegex = new(@"Data\s+Source\s*=\s*(?<name>[^;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex DataSourceKeywordRegex() => _dataSourceKeywordRegex;
#endif
}
