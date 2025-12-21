using System.Data;

namespace JD.Efcpt.Build.Tasks.Extensions;

/// <summary>
/// Provides extension methods for DataRow objects to simplify common operations and improve null handling.
/// </summary>
public static class DataRowExtensions
{
    /// <summary>
    /// Returns a string value for the column, using empty string when the value is null/DBNull.
    /// Equivalent intent to: row["col"].ToString() ?? ""
    /// but correctly handles DBNull.
    /// </summary>
    public static string GetString(this DataRow row, string columnName)
    {
        ArgumentNullException.ThrowIfNull(row);
        
        if (string.IsNullOrWhiteSpace(columnName)) throw new ArgumentException("Column name is required.", nameof(columnName));

        if (!row.Table.Columns.Contains(columnName))
            throw new ArgumentOutOfRangeException(nameof(columnName), $"Column '{columnName}' does not exist in the DataRow's table.");

        var value = row[columnName];

        if (value == DBNull.Value)
            return string.Empty;

        // If the underlying value is already a string, avoid extra formatting.
        return value as string ?? Convert.ToString(value) ?? string.Empty;
    }
}
