using System.Data;
using System.Data.Common;
using JD.Efcpt.Build.Tasks.Extensions;
using MySqlConnector;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from MySQL/MariaDB databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class MySqlSchemaReader : SchemaReaderBase
{
    /// <summary>
    /// Creates a MySQL database connection for the specified connection string.
    /// </summary>
    protected override DbConnection CreateConnection(string connectionString)
        => new MySqlConnection(connectionString);

    /// <summary>
    /// Gets a list of user-defined tables from MySQL.
    /// </summary>
    protected override List<(string Schema, string Name)> GetUserTables(DbConnection connection)
    {
        var databaseName = connection.Database;
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

    /// <summary>
    /// Reads all indexes for a specific table from MySQL.
    /// </summary>
    protected override IEnumerable<IndexModel> ReadIndexesForTable(
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
}
