using System.Data;
using JD.Efcpt.Build.Tasks.Extensions;
using Npgsql;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from PostgreSQL databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class PostgreSqlSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from a PostgreSQL database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var columnsData = connection.GetSchema("Columns");
        var tablesList = GetUserTables(connection);
        var indexesData = connection.GetSchema("Indexes");
        var indexColumnsData = connection.GetSchema("IndexColumns");

        var tables = tablesList
            .Select(t => TableModel.Create(
                t.Schema,
                t.Name,
                ReadColumnsForTable(columnsData, t.Schema, t.Name),
                ReadIndexesForTable(indexesData, indexColumnsData, t.Schema, t.Name),
                []))
            .ToList();

        return SchemaModel.Create(tables);
    }

    private static List<(string Schema, string Name)> GetUserTables(NpgsqlConnection connection)
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

    private static IEnumerable<ColumnModel> ReadColumnsForTable(
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

    private static IEnumerable<IndexModel> ReadIndexesForTable(
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

    private static string GetColumnName(DataTable table, params string[] possibleNames)
        => possibleNames.FirstOrDefault(name => table.Columns.Contains(name)) ?? possibleNames[0];
}
