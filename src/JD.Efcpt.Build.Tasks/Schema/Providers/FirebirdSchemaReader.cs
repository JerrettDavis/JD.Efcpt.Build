using System.Data;
using FirebirdSql.Data.FirebirdClient;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from Firebird databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class FirebirdSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from a Firebird database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new FbConnection(connectionString);
        connection.Open();

        var tablesList = GetUserTables(connection);
        var columnsData = connection.GetSchema("Columns");
        var indexesData = connection.GetSchema("Indexes");
        var indexColumnsData = connection.GetSchema("IndexColumns");

        var tables = tablesList
            .Select(t => TableModel.Create(
                t.Schema,
                t.Name,
                ReadColumnsForTable(columnsData, t.Name),
                ReadIndexesForTable(indexesData, indexColumnsData, t.Name),
                []))
            .ToList();

        return SchemaModel.Create(tables);
    }

    private static List<(string Schema, string Name)> GetUserTables(FbConnection connection)
    {
        var tablesData = connection.GetSchema("Tables");

        // Firebird uses TABLE_NAME and IS_SYSTEM_TABLE
        var tableNameCol = GetExistingColumn(tablesData, "TABLE_NAME");
        var systemCol = GetExistingColumn(tablesData, "IS_SYSTEM_TABLE", "SYSTEM_TABLE");
        var typeCol = GetExistingColumn(tablesData, "TABLE_TYPE");

        return tablesData
            .AsEnumerable()
            .Where(row =>
            {
                // Filter out system tables
                if (systemCol != null && !row.IsNull(systemCol))
                {
                    var isSystem = row[systemCol];
                    if (isSystem is bool b && b) return false;
                    if (isSystem is int i && i != 0) return false;
                    if (string.Equals(isSystem?.ToString(), "true", StringComparison.OrdinalIgnoreCase)) return false;
                }

                // Filter to base tables if type column exists
                if (typeCol != null && !row.IsNull(typeCol))
                {
                    var tableType = row[typeCol]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(tableType) &&
                        !tableType.Contains("TABLE", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            })
            .Where(row =>
            {
                // Filter out RDB$ system tables
                var tableName = tableNameCol != null ? row[tableNameCol]?.ToString() ?? "" : "";
                return !tableName.StartsWith("RDB$", StringComparison.OrdinalIgnoreCase) &&
                       !tableName.StartsWith("MON$", StringComparison.OrdinalIgnoreCase);
            })
            .Select(row => (
                Schema: "dbo", // Firebird doesn't have schemas, use default
                Name: (tableNameCol != null ? row[tableNameCol]?.ToString() ?? "" : "").Trim()))
            .Where(t => !string.IsNullOrEmpty(t.Name))
            .OrderBy(t => t.Name)
            .ToList();
    }

    private static IEnumerable<ColumnModel> ReadColumnsForTable(
        DataTable columnsData,
        string tableName)
    {
        var tableNameCol = GetExistingColumn(columnsData, "TABLE_NAME");
        var columnNameCol = GetExistingColumn(columnsData, "COLUMN_NAME");
        var dataTypeCol = GetExistingColumn(columnsData, "COLUMN_DATA_TYPE", "DATA_TYPE");
        var sizeCol = GetExistingColumn(columnsData, "COLUMN_SIZE", "CHARACTER_MAXIMUM_LENGTH");
        var precisionCol = GetExistingColumn(columnsData, "NUMERIC_PRECISION");
        var scaleCol = GetExistingColumn(columnsData, "NUMERIC_SCALE");
        var nullableCol = GetExistingColumn(columnsData, "IS_NULLABLE");
        var ordinalCol = GetExistingColumn(columnsData, "ORDINAL_POSITION", "COLUMN_POSITION");
        var defaultCol = GetExistingColumn(columnsData, "COLUMN_DEFAULT");

        var ordinal = 1;
        return columnsData
            .AsEnumerable()
            .Where(row => tableNameCol == null ||
                string.Equals((row[tableNameCol]?.ToString() ?? "").Trim(), tableName.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(row => ordinalCol != null && !row.IsNull(ordinalCol) ? Convert.ToInt32(row[ordinalCol]) : ordinal++)
            .Select((row, index) => new ColumnModel(
                Name: (columnNameCol != null ? row[columnNameCol]?.ToString() ?? "" : "").Trim(),
                DataType: (dataTypeCol != null ? row[dataTypeCol]?.ToString() ?? "" : "").Trim(),
                MaxLength: sizeCol != null && !row.IsNull(sizeCol) ? Convert.ToInt32(row[sizeCol]) : 0,
                Precision: precisionCol != null && !row.IsNull(precisionCol) ? Convert.ToInt32(row[precisionCol]) : 0,
                Scale: scaleCol != null && !row.IsNull(scaleCol) ? Convert.ToInt32(row[scaleCol]) : 0,
                IsNullable: nullableCol != null && (row[nullableCol]?.ToString() == "YES" || row[nullableCol]?.ToString() == "true"),
                OrdinalPosition: ordinalCol != null && !row.IsNull(ordinalCol) ? Convert.ToInt32(row[ordinalCol]) : index + 1,
                DefaultValue: defaultCol != null && !row.IsNull(defaultCol) ? row[defaultCol]?.ToString()?.Trim() : null
            ));
    }

    private static IEnumerable<IndexModel> ReadIndexesForTable(
        DataTable indexesData,
        DataTable indexColumnsData,
        string tableName)
    {
        var tableNameCol = GetExistingColumn(indexesData, "TABLE_NAME");
        var indexNameCol = GetExistingColumn(indexesData, "INDEX_NAME");
        var uniqueCol = GetExistingColumn(indexesData, "IS_UNIQUE", "UNIQUE_FLAG");
        var primaryCol = GetExistingColumn(indexesData, "IS_PRIMARY");

        return indexesData
            .AsEnumerable()
            .Where(row => tableNameCol == null ||
                string.Equals((row[tableNameCol]?.ToString() ?? "").Trim(), tableName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(row =>
            {
                var indexName = indexNameCol != null ? (row[indexNameCol]?.ToString() ?? "").Trim() : "";
                // Filter out RDB$ system indexes
                return !indexName.StartsWith("RDB$", StringComparison.OrdinalIgnoreCase);
            })
            .Select(row => (indexNameCol != null ? row[indexNameCol]?.ToString() ?? "" : "").Trim())
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .Select(indexName =>
            {
                var indexRow = indexesData.AsEnumerable()
                    .FirstOrDefault(r => indexNameCol != null && string.Equals((r[indexNameCol]?.ToString() ?? "").Trim(), indexName, StringComparison.OrdinalIgnoreCase));

                bool isUnique = false, isPrimary = false;

                if (indexRow != null)
                {
                    if (uniqueCol != null && !indexRow.IsNull(uniqueCol))
                    {
                        var val = indexRow[uniqueCol];
                        isUnique = val is bool b ? b : (val is int i && i != 0) || val?.ToString() == "1";
                    }

                    if (primaryCol != null && !indexRow.IsNull(primaryCol))
                    {
                        var val = indexRow[primaryCol];
                        isPrimary = val is bool b ? b : (val is int i && i != 0) || val?.ToString() == "1";
                    }
                }

                // Primary key indexes often start with "PK_" or "RDB$PRIMARY"
                if (indexName.StartsWith("PK_", StringComparison.OrdinalIgnoreCase))
                    isPrimary = true;

                return IndexModel.Create(
                    indexName,
                    isUnique: isUnique || isPrimary,
                    isPrimaryKey: isPrimary,
                    isClustered: false,
                    ReadIndexColumnsForIndex(indexColumnsData, tableName, indexName));
            })
            .ToList();
    }

    private static IEnumerable<IndexColumnModel> ReadIndexColumnsForIndex(
        DataTable indexColumnsData,
        string tableName,
        string indexName)
    {
        var tableNameCol = GetExistingColumn(indexColumnsData, "TABLE_NAME");
        var indexNameCol = GetExistingColumn(indexColumnsData, "INDEX_NAME");
        var columnNameCol = GetExistingColumn(indexColumnsData, "COLUMN_NAME");
        var ordinalCol = GetExistingColumn(indexColumnsData, "ORDINAL_POSITION", "COLUMN_POSITION");

        return indexColumnsData
            .AsEnumerable()
            .Where(row =>
                (tableNameCol == null || string.Equals((row[tableNameCol]?.ToString() ?? "").Trim(), tableName.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                (indexNameCol == null || string.Equals((row[indexNameCol]?.ToString() ?? "").Trim(), indexName.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Select(row => new IndexColumnModel(
                ColumnName: (columnNameCol != null ? row[columnNameCol]?.ToString() ?? "" : "").Trim(),
                OrdinalPosition: ordinalCol != null && !row.IsNull(ordinalCol) ? Convert.ToInt32(row[ordinalCol]) : 1,
                IsDescending: false));
    }

    private static string? GetExistingColumn(DataTable table, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (table.Columns.Contains(name))
                return name;
        }
        return null;
    }
}
