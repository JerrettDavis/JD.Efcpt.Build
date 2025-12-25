using System.Data;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Data.SqlClient;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from SQL Server databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class SqlServerSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from a SQL Server database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // Use GetSchema for columns (standardized across providers)
        var columnsData = connection.GetSchema("Columns");

        // Get table list using GetSchema with restrictions
        var tablesList = GetUserTables(connection);

        // Get metadata using GetSchema
        var indexesData = GetIndexes(connection);
        var indexColumnsData = GetIndexColumns(connection);

        var tables = tablesList
            .Select(t => TableModel.Create(
                t.Schema,
                t.Name,
                ReadColumnsForTable(columnsData, t.Schema, t.Name),
                ReadIndexesForTable(indexesData, indexColumnsData, t.Schema, t.Name),
                [])) // GetSchema doesn't provide constraints
            .ToList();

        return SchemaModel.Create(tables);
    }

    private static List<(string Schema, string Name)> GetUserTables(SqlConnection connection)
    {
        // Use GetSchema with restrictions to get base tables
        // Restrictions array: [0]=Catalog, [1]=Schema, [2]=TableName, [3]=TableType
        var restrictions = new string?[4];
        restrictions[3] = "BASE TABLE"; // Only get base tables, not views

        return connection.GetSchema("Tables", restrictions)
            .AsEnumerable()
            .Select(row => (
                Schema: row.GetString("TABLE_SCHEMA"),
                Name: row.GetString("TABLE_NAME")))
            .Where(t => !string.Equals(t.Schema, "sys", StringComparison.OrdinalIgnoreCase))
            .Where(t => !string.Equals(t.Schema, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Schema)
            .ThenBy(t => t.Name)
            .ToList();
    }

    private static IEnumerable<ColumnModel> ReadColumnsForTable(
        DataTable columnsData,
        string schemaName,
        string tableName)
        => columnsData
            .Select($"TABLE_SCHEMA = '{EscapeSql(schemaName)}' AND TABLE_NAME = '{EscapeSql(tableName)}'", "ORDINAL_POSITION ASC")
            .Select(row => new ColumnModel(
                Name: row.GetString("COLUMN_NAME"),
                DataType: row.GetString("DATA_TYPE"),
                MaxLength: row.IsNull("CHARACTER_MAXIMUM_LENGTH") ? 0 : Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]),
                Precision: row.IsNull("NUMERIC_PRECISION") ? 0 : Convert.ToInt32(row["NUMERIC_PRECISION"]),
                Scale: row.IsNull("NUMERIC_SCALE") ? 0 : Convert.ToInt32(row["NUMERIC_SCALE"]),
                IsNullable: row["IS_NULLABLE"].ToString() == "YES",
                OrdinalPosition: Convert.ToInt32(row["ORDINAL_POSITION"]),
                DefaultValue: row.IsNull("COLUMN_DEFAULT") ? null : row["COLUMN_DEFAULT"].ToString()
            ));

    private static DataTable GetIndexes(SqlConnection connection)
    {
        // Use GetSchema("Indexes") for standardized index metadata
        return connection.GetSchema("Indexes");
    }

    private static DataTable GetIndexColumns(SqlConnection connection)
    {
        // Use GetSchema("IndexColumns") for index column metadata
        return connection.GetSchema("IndexColumns");
    }

    private static IEnumerable<IndexModel> ReadIndexesForTable(
        DataTable indexesData,
        DataTable indexColumnsData,
        string schemaName,
        string tableName)
        => indexesData
            .Select($"table_schema = '{EscapeSql(schemaName)}' AND table_name = '{EscapeSql(tableName)}'")
            .Select(row => new { row, indexName = row.GetString("index_name") })
            .Where(rowInfo => !string.IsNullOrEmpty(rowInfo.indexName))
            .Select(rowInfo => new
            {
                rowInfo.row,
                rowInfo.indexName,
                typeDesc = rowInfo.row.Table.Columns.Contains("type_desc")
                    ? rowInfo.row.GetString("type_desc")
                    : "",
                isClustered = rowInfo.row.Table.Columns.Contains("type_desc") &&
                    (rowInfo.row.GetString("type_desc")).Contains("CLUSTERED", StringComparison.OrdinalIgnoreCase),
                indexColumns = ReadIndexColumnsForIndex(indexColumnsData, schemaName, tableName, rowInfo.indexName)
            })
            .Select(t => IndexModel.Create(
                t.indexName,
                isUnique: false, // Not available from GetSchema
                isPrimaryKey: false, // Not available from GetSchema
                t.isClustered,
                t.indexColumns))
            .ToList();

    private static IEnumerable<IndexColumnModel> ReadIndexColumnsForIndex(
        DataTable indexColumnsData,
        string schemaName,
        string tableName,
        string indexName)
        => indexColumnsData.Select(
                $"table_schema = '{EscapeSql(schemaName)}' AND table_name = '{EscapeSql(tableName)}' AND index_name = '{EscapeSql(indexName)}'",
                "ordinal_position ASC")
            .Select(row => new IndexColumnModel(
                ColumnName: row.GetString("column_name"),
                OrdinalPosition: Convert.ToInt32(row["ordinal_position"]),
                IsDescending: false)); // Not available from GetSchema, default to ascending

    private static string EscapeSql(string value) => value.Replace("'", "''");
}
