using System.Data;
using JD.Efcpt.Build.Tasks.Extensions;
using MySqlConnector;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from MySQL/MariaDB databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class MySqlSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from a MySQL database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new MySqlConnection(connectionString);
        connection.Open();

        // Get the database name for use as schema
        var databaseName = connection.Database;

        var columnsData = connection.GetSchema("Columns");
        var tablesList = GetUserTables(connection, databaseName);
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

    private static List<(string Schema, string Name)> GetUserTables(MySqlConnection connection, string databaseName)
    {
        var tablesData = connection.GetSchema("Tables");

        // MySQL uses TABLE_SCHEMA (database name) and TABLE_NAME
        return tablesData
            .AsEnumerable()
            .Where(row => row.GetString("TABLE_SCHEMA").EqualsIgnoreCase(databaseName))
            .Where(row => row.GetString("TABLE_TYPE").EqualsIgnoreCase("BASE TABLE"))
            .Select(row => (
                Schema: row.GetString("TABLE_SCHEMA"),
                Name: row.GetString("TABLE_NAME")))
            .OrderBy(t => t.Schema)
            .ThenBy(t => t.Name)
            .ToList();
    }

    private static IEnumerable<ColumnModel> ReadColumnsForTable(
        DataTable columnsData,
        string schemaName,
        string tableName)
        => columnsData
            .AsEnumerable()
            .Where(row => row.GetString("TABLE_SCHEMA").EqualsIgnoreCase(schemaName) &&
                          row.GetString("TABLE_NAME").EqualsIgnoreCase(tableName))
            .OrderBy(row => Convert.ToInt32(row["ORDINAL_POSITION"]))
            .Select(row => new ColumnModel(
                Name: row.GetString("COLUMN_NAME"),
                DataType: row.GetString("DATA_TYPE"),
                MaxLength: row.IsNull("CHARACTER_MAXIMUM_LENGTH") ? 0 : Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]),
                Precision: row.IsNull("NUMERIC_PRECISION") ? 0 : Convert.ToInt32(row["NUMERIC_PRECISION"]),
                Scale: row.IsNull("NUMERIC_SCALE") ? 0 : Convert.ToInt32(row["NUMERIC_SCALE"]),
                IsNullable: row.GetString("IS_NULLABLE").EqualsIgnoreCase("YES"),
                OrdinalPosition: Convert.ToInt32(row["ORDINAL_POSITION"]),
                DefaultValue: row.IsNull("COLUMN_DEFAULT") ? null : row.GetString("COLUMN_DEFAULT")
            ));

    private static IEnumerable<IndexModel> ReadIndexesForTable(
        DataTable indexesData,
        DataTable indexColumnsData,
        string schemaName,
        string tableName)
    {
        // Check column names that exist in the table
        var schemaCol = GetExistingColumn(indexesData, "TABLE_SCHEMA", "INDEX_SCHEMA");
        var tableCol = GetExistingColumn(indexesData, "TABLE_NAME");
        var indexNameCol = GetExistingColumn(indexesData, "INDEX_NAME");
        var uniqueCol = GetExistingColumn(indexesData, "NON_UNIQUE", "UNIQUE");

        return indexesData
            .AsEnumerable()
            .Where(row => (schemaCol == null || (row[schemaCol]?.ToString()).EqualsIgnoreCase(schemaName)) &&
                          (tableCol == null || (row[tableCol]?.ToString()).EqualsIgnoreCase(tableName)))
            .Select(row => indexNameCol != null ? row[indexNameCol].ToString() ?? "" : "")
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .Select(indexName =>
            {
                var indexRow = indexesData.AsEnumerable()
                    .FirstOrDefault(r => indexNameCol != null && (r[indexNameCol]?.ToString()).EqualsIgnoreCase(indexName));

                var isPrimary = indexName.EqualsIgnoreCase("PRIMARY");
                var isUnique = isPrimary;

                if (indexRow != null && uniqueCol != null && !indexRow.IsNull(uniqueCol))
                {
                    // NON_UNIQUE = 0 means unique, = 1 means not unique
                    isUnique = Convert.ToInt32(indexRow[uniqueCol]) == 0;
                }

                return IndexModel.Create(
                    indexName,
                    isUnique: isUnique,
                    isPrimaryKey: isPrimary,
                    isClustered: isPrimary, // InnoDB clusters on primary key
                    ReadIndexColumnsForIndex(indexColumnsData, schemaName, tableName, indexName));
            })
            .ToList();
    }

    private static IEnumerable<IndexColumnModel> ReadIndexColumnsForIndex(
        DataTable indexColumnsData,
        string schemaName,
        string tableName,
        string indexName)
    {
        var schemaCol = GetExistingColumn(indexColumnsData, "TABLE_SCHEMA", "INDEX_SCHEMA");
        var tableCol = GetExistingColumn(indexColumnsData, "TABLE_NAME");
        var indexNameCol = GetExistingColumn(indexColumnsData, "INDEX_NAME");
        var columnNameCol = GetExistingColumn(indexColumnsData, "COLUMN_NAME");
        var ordinalCol = GetExistingColumn(indexColumnsData, "ORDINAL_POSITION", "SEQ_IN_INDEX");

        return indexColumnsData
            .AsEnumerable()
            .Where(row => (schemaCol == null || (row[schemaCol]?.ToString()).EqualsIgnoreCase(schemaName)) &&
                          (tableCol == null || (row[tableCol]?.ToString()).EqualsIgnoreCase(tableName)) &&
                          (indexNameCol == null || (row[indexNameCol]?.ToString()).EqualsIgnoreCase(indexName)))
            .Select(row => new IndexColumnModel(
                ColumnName: columnNameCol != null ? row[columnNameCol]?.ToString() ?? "" : "",
                OrdinalPosition: ordinalCol != null && !row.IsNull(ordinalCol)
                    ? Convert.ToInt32(row[ordinalCol])
                    : 1,
                IsDescending: false));
    }

    private static string? GetExistingColumn(DataTable table, params string[] possibleNames)
        => possibleNames.FirstOrDefault(name => table.Columns.Contains(name));
}
