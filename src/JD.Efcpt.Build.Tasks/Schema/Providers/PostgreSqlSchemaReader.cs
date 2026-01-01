using System.Data;
using System.Data.Common;
using JD.Efcpt.Build.Tasks.Extensions;
using Npgsql;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from PostgreSQL databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class PostgreSqlSchemaReader : SchemaReaderBase
{
    /// <summary>
    /// Creates a PostgreSQL database connection for the specified connection string.
    /// </summary>
    protected override DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    /// <summary>
    /// Gets a list of user-defined tables from PostgreSQL, excluding system tables.
    /// </summary>
    protected override List<(string Schema, string Name)> GetUserTables(DbConnection connection)
    {
        // PostgreSQL GetSchema("Tables") returns tables with table_schema and table_name columns
        var tablesData = connection.GetSchema("Tables");

        return tablesData
            .AsEnumerable()
            .Where(row => row.GetString("table_type") == "BASE TABLE" ||
                          row.GetString("table_type") == "table")
            .Select(row => (
                Schema: row.GetString("table_schema"),
                Name: row.GetString("table_name")))
            .Where(t => !t.Schema.StartsWith("pg_", StringComparison.OrdinalIgnoreCase))
            .Where(t => !t.Schema.EqualsIgnoreCase("information_schema"))
            .OrderBy(t => t.Schema)
            .ThenBy(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// Reads columns for a table, handling PostgreSQL's case-sensitive column names.
    /// </summary>
    /// <remarks>
    /// PostgreSQL uses lowercase column names in GetSchema results, so we need to check both cases.
    /// </remarks>
    protected override IEnumerable<ColumnModel> ReadColumnsForTable(
        DataTable columnsData,
        string schemaName,
        string tableName)
    {
        // PostgreSQL uses lowercase column names in GetSchema results
        var schemaCol = GetColumnName(columnsData, "table_schema", "TABLE_SCHEMA");
        var tableCol = GetColumnName(columnsData, "table_name", "TABLE_NAME");
        var colNameCol = GetColumnName(columnsData, "column_name", "COLUMN_NAME");
        var dataTypeCol = GetColumnName(columnsData, "data_type", "DATA_TYPE");
        var maxLengthCol = GetColumnName(columnsData, "character_maximum_length", "CHARACTER_MAXIMUM_LENGTH");
        var precisionCol = GetColumnName(columnsData, "numeric_precision", "NUMERIC_PRECISION");
        var scaleCol = GetColumnName(columnsData, "numeric_scale", "NUMERIC_SCALE");
        var nullableCol = GetColumnName(columnsData, "is_nullable", "IS_NULLABLE");
        var ordinalCol = GetColumnName(columnsData, "ordinal_position", "ORDINAL_POSITION");
        var defaultCol = GetColumnName(columnsData, "column_default", "COLUMN_DEFAULT");

        return columnsData
            .AsEnumerable()
            .Where(row => (row[schemaCol]?.ToString()).EqualsIgnoreCase(schemaName) &&
                          (row[tableCol]?.ToString()).EqualsIgnoreCase(tableName))
            .OrderBy(row => Convert.ToInt32(row[ordinalCol]))
            .Select(row => new ColumnModel(
                Name: row[colNameCol]?.ToString() ?? "",
                DataType: row[dataTypeCol]?.ToString() ?? "",
                MaxLength: row.IsNull(maxLengthCol) ? 0 : Convert.ToInt32(row[maxLengthCol]),
                Precision: row.IsNull(precisionCol) ? 0 : Convert.ToInt32(row[precisionCol]),
                Scale: row.IsNull(scaleCol) ? 0 : Convert.ToInt32(row[scaleCol]),
                IsNullable: (row[nullableCol]?.ToString()).EqualsIgnoreCase("YES"),
                OrdinalPosition: Convert.ToInt32(row[ordinalCol]),
                DefaultValue: row.IsNull(defaultCol) ? null : row[defaultCol]?.ToString()
            ));
    }

    /// <summary>
    /// Reads all indexes for a specific table from PostgreSQL.
    /// </summary>
    protected override IEnumerable<IndexModel> ReadIndexesForTable(
        DataTable indexesData,
        DataTable indexColumnsData,
        string schemaName,
        string tableName)
    {
        var schemaCol = GetColumnName(indexesData, "table_schema", "TABLE_SCHEMA");
        var tableCol = GetColumnName(indexesData, "table_name", "TABLE_NAME");
        var indexNameCol = GetColumnName(indexesData, "index_name", "INDEX_NAME");

        return indexesData
            .AsEnumerable()
            .Where(row => (row[schemaCol]?.ToString()).EqualsIgnoreCase(schemaName) &&
                          (row[tableCol]?.ToString()).EqualsIgnoreCase(tableName))
            .Select(row => row[indexNameCol]?.ToString() ?? "")
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .Select(indexName => IndexModel.Create(
                indexName,
                isUnique: false, // Not reliably available from GetSchema
                isPrimaryKey: false,
                isClustered: false, // PostgreSQL doesn't have clustered indexes in the SQL Server sense
                ReadIndexColumnsForIndex(indexColumnsData, schemaName, tableName, indexName)))
            .ToList();
    }

    private static IEnumerable<IndexColumnModel> ReadIndexColumnsForIndex(
        DataTable indexColumnsData,
        string schemaName,
        string tableName,
        string indexName)
    {
        var schemaCol = GetColumnName(indexColumnsData, "table_schema", "TABLE_SCHEMA");
        var tableCol = GetColumnName(indexColumnsData, "table_name", "TABLE_NAME");
        var indexNameCol = GetColumnName(indexColumnsData, "index_name", "INDEX_NAME");
        var columnNameCol = GetColumnName(indexColumnsData, "column_name", "COLUMN_NAME");
        var ordinalCol = GetColumnName(indexColumnsData, "ordinal_position", "ORDINAL_POSITION");

        var ordinal = 1;
        return indexColumnsData
            .AsEnumerable()
            .Where(row => (row[schemaCol]?.ToString()).EqualsIgnoreCase(schemaName) &&
                          (row[tableCol]?.ToString()).EqualsIgnoreCase(tableName) &&
                          (row[indexNameCol]?.ToString()).EqualsIgnoreCase(indexName))
            .Select(row => new IndexColumnModel(
                ColumnName: row[columnNameCol]?.ToString() ?? "",
                OrdinalPosition: indexColumnsData.Columns.Contains(ordinalCol)
                    ? Convert.ToInt32(row[ordinalCol])
                    : ordinal++,
                IsDescending: false));
    }
}
