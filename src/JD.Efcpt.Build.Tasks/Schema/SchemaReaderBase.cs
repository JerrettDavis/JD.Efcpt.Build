using System.Data;
using System.Data.Common;
using JD.Efcpt.Build.Tasks.Extensions;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Base class for schema readers that use ADO.NET's GetSchema() API.
/// </summary>
/// <remarks>
/// This base class consolidates common schema reading logic for database providers
/// that support the standard ADO.NET metadata collections (Columns, Tables, Indexes, IndexColumns).
/// Providers with unique metadata mechanisms (like SQLite) should implement ISchemaReader directly.
/// </remarks>
internal abstract class SchemaReaderBase : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from the database specified by the connection string.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = CreateConnection(connectionString);
        connection.Open();

        var columnsData = connection.GetSchema("Columns");
        var tablesList = GetUserTables(connection);
        var indexesData = GetIndexes(connection);
        var indexColumnsData = GetIndexColumns(connection);

        var tables = tablesList
            .Select(t => TableModel.Create(
                t.Schema,
                t.Name,
                ReadColumnsForTable(columnsData, t.Schema, t.Name),
                ReadIndexesForTable(indexesData, indexColumnsData, t.Schema, t.Name),
                [])) // Constraints not reliably available from GetSchema across providers
            .ToList();

        return SchemaModel.Create(tables);
    }

    /// <summary>
    /// Creates a database connection for the specified connection string.
    /// </summary>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Gets a list of user-defined tables from the database.
    /// </summary>
    /// <remarks>
    /// Implementations should filter out system tables and return only user tables.
    /// </remarks>
    protected abstract List<(string Schema, string Name)> GetUserTables(DbConnection connection);

    /// <summary>
    /// Gets indexes metadata from the database.
    /// </summary>
    /// <remarks>
    /// Default implementation calls GetSchema("Indexes"). Override if provider requires custom logic.
    /// </remarks>
    protected virtual DataTable GetIndexes(DbConnection connection)
        => connection.GetSchema("Indexes");

    /// <summary>
    /// Gets index columns metadata from the database.
    /// </summary>
    /// <remarks>
    /// Default implementation calls GetSchema("IndexColumns"). Override if provider requires custom logic.
    /// </remarks>
    protected virtual DataTable GetIndexColumns(DbConnection connection)
        => connection.GetSchema("IndexColumns");

    /// <summary>
    /// Reads all columns for a specific table.
    /// </summary>
    /// <remarks>
    /// Default implementation assumes standard column names from GetSchema("Columns").
    /// Override if provider uses different column names or requires custom logic.
    /// </remarks>
    protected virtual IEnumerable<ColumnModel> ReadColumnsForTable(
        DataTable columnsData,
        string schemaName,
        string tableName)
    {
        var columnMapping = GetColumnMapping();

        return columnsData
            .AsEnumerable()
            .Where(row => MatchesTable(row, columnMapping, schemaName, tableName))
            .OrderBy(row => Convert.ToInt32(row[columnMapping.OrdinalPosition]))
            .Select(row => new ColumnModel(
                Name: row.GetString(columnMapping.ColumnName),
                DataType: row.GetString(columnMapping.DataType),
                MaxLength: row.IsNull(columnMapping.MaxLength) ? 0 : Convert.ToInt32(row[columnMapping.MaxLength]),
                Precision: row.IsNull(columnMapping.Precision) ? 0 : Convert.ToInt32(row[columnMapping.Precision]),
                Scale: row.IsNull(columnMapping.Scale) ? 0 : Convert.ToInt32(row[columnMapping.Scale]),
                IsNullable: row.GetString(columnMapping.IsNullable).EqualsIgnoreCase("YES"),
                OrdinalPosition: Convert.ToInt32(row[columnMapping.OrdinalPosition]),
                DefaultValue: row.IsNull(columnMapping.DefaultValue) ? null : row.GetString(columnMapping.DefaultValue)
            ));
    }

    /// <summary>
    /// Reads all indexes for a specific table.
    /// </summary>
    protected abstract IEnumerable<IndexModel> ReadIndexesForTable(
        DataTable indexesData,
        DataTable indexColumnsData,
        string schemaName,
        string tableName);

    /// <summary>
    /// Gets the column name mapping for this provider's GetSchema results.
    /// </summary>
    /// <remarks>
    /// Provides column names used in the GetSchema("Columns") result set.
    /// Default implementation returns uppercase standard names.
    /// Override to provide provider-specific column names (e.g., lowercase for PostgreSQL).
    /// </remarks>
    protected virtual ColumnNameMapping GetColumnMapping()
        => new(
            TableSchema: "TABLE_SCHEMA",
            TableName: "TABLE_NAME",
            ColumnName: "COLUMN_NAME",
            DataType: "DATA_TYPE",
            MaxLength: "CHARACTER_MAXIMUM_LENGTH",
            Precision: "NUMERIC_PRECISION",
            Scale: "NUMERIC_SCALE",
            IsNullable: "IS_NULLABLE",
            OrdinalPosition: "ORDINAL_POSITION",
            DefaultValue: "COLUMN_DEFAULT"
        );

    /// <summary>
    /// Determines if a row matches the specified table.
    /// </summary>
    protected virtual bool MatchesTable(
        DataRow row,
        ColumnNameMapping mapping,
        string schemaName,
        string tableName)
        => row.GetString(mapping.TableSchema).EqualsIgnoreCase(schemaName) &&
           row.GetString(mapping.TableName).EqualsIgnoreCase(tableName);

    /// <summary>
    /// Helper method to resolve column names that may vary across providers.
    /// </summary>
    /// <remarks>
    /// Returns the first column name from the candidates that exists in the table,
    /// or the first candidate if none are found.
    /// </remarks>
    protected static string GetColumnName(DataTable table, params string[] candidates)
        => candidates.FirstOrDefault(name => table.Columns.Contains(name)) ?? candidates[0];

    /// <summary>
    /// Helper method to get an existing column name from a list of candidates.
    /// </summary>
    /// <remarks>
    /// Returns the first column name from the candidates that exists in the table,
    /// or null if none are found.
    /// </remarks>
    protected static string? GetExistingColumn(DataTable table, params string[] candidates)
        => candidates.FirstOrDefault(table.Columns.Contains);

    /// <summary>
    /// Escapes SQL string values for use in DataTable.Select() expressions.
    /// </summary>
    protected static string EscapeSql(string value) => value.Replace("'", "''");
}

/// <summary>
/// Maps column names used in GetSchema("Columns") results for a specific database provider.
/// </summary>
/// <remarks>
/// Different providers may use different casing (e.g., PostgreSQL uses lowercase, others use uppercase).
/// </remarks>
internal sealed record ColumnNameMapping(
    string TableSchema,
    string TableName,
    string ColumnName,
    string DataType,
    string MaxLength,
    string Precision,
    string Scale,
    string IsNullable,
    string OrdinalPosition,
    string DefaultValue
);
