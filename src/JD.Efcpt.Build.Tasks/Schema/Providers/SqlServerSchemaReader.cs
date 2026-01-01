using System.Data;
using System.Data.Common;
using JD.Efcpt.Build.Tasks.Extensions;
using Microsoft.Data.SqlClient;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from SQL Server databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class SqlServerSchemaReader : SchemaReaderBase
{
    /// <summary>
    /// Creates a SQL Server database connection for the specified connection string.
    /// </summary>
    protected override DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    /// <summary>
    /// Gets a list of user-defined tables from SQL Server, excluding system tables.
    /// </summary>
    protected override List<(string Schema, string Name)> GetUserTables(DbConnection connection)
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
            .Where(t => !t.Schema.EqualsIgnoreCase("sys"))
            .Where(t => !t.Schema.EqualsIgnoreCase("INFORMATION_SCHEMA"))
            .OrderBy(t => t.Schema)
            .ThenBy(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// Reads columns for a table using DataTable.Select() for efficient filtering.
    /// </summary>
    /// <remarks>
    /// SQL Server's GetSchema returns uppercase column names, which allows using
    /// DataTable.Select() with filter expressions for better performance.
    /// </remarks>
    protected override IEnumerable<ColumnModel> ReadColumnsForTable(
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
                IsNullable: row.GetString("IS_NULLABLE").EqualsIgnoreCase("YES"),
                OrdinalPosition: Convert.ToInt32(row["ORDINAL_POSITION"]),
                DefaultValue: row.IsNull("COLUMN_DEFAULT") ? null : row.GetString("COLUMN_DEFAULT")
            ));

    /// <summary>
    /// Reads all indexes for a specific table from SQL Server.
    /// </summary>
    protected override IEnumerable<IndexModel> ReadIndexesForTable(
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
}
