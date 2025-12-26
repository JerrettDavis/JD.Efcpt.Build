using System.Data;
using JD.Efcpt.Build.Tasks.Extensions;
using Oracle.ManagedDataAccess.Client;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from Oracle databases using GetSchema() for standard metadata.
/// </summary>
internal sealed class OracleSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from an Oracle database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new OracleConnection(connectionString);
        connection.Open();

        var tablesList = GetUserTables(connection);
        var columnsData = connection.GetSchema("Columns");
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

    private static List<(string Schema, string Name)> GetUserTables(OracleConnection connection)
    {
        var tablesData = connection.GetSchema("Tables");

        // Oracle uses OWNER as schema and TABLE_NAME
        var ownerCol = GetExistingColumn(tablesData, "OWNER", "TABLE_SCHEMA");
        var tableNameCol = GetExistingColumn(tablesData, "TABLE_NAME");
        var tableTypeCol = GetExistingColumn(tablesData, "TYPE", "TABLE_TYPE");

        return tablesData
            .AsEnumerable()
            .Where(row =>
            {
                if (tableTypeCol != null)
                {
                    var tableType = row[tableTypeCol]?.ToString() ?? "";
                    // Filter to user tables, exclude system objects
                    if (!string.IsNullOrEmpty(tableType) &&
                        !tableType.EqualsIgnoreCase("User") &&
                        !tableType.EqualsIgnoreCase("TABLE"))
                        return false;
                }
                return true;
            })
            .Where(row =>
            {
                // Filter out system schemas
                var schema = ownerCol != null ? row[ownerCol]?.ToString() ?? "" : "";
                return !IsSystemSchema(schema);
            })
            .Select(row => (
                Schema: ownerCol != null ? row[ownerCol]?.ToString() ?? "" : "",
                Name: tableNameCol != null ? row[tableNameCol]?.ToString() ?? "" : ""))
            .Where(t => !string.IsNullOrEmpty(t.Name))
            .OrderBy(t => t.Schema)
            .ThenBy(t => t.Name)
            .ToList();
    }

    private static bool IsSystemSchema(string schema)
    {
        var systemSchemas = new[]
        {
            "SYS", "SYSTEM", "OUTLN", "DIP", "ORACLE_OCM", "DBSNMP", "APPQOSSYS",
            "WMSYS", "EXFSYS", "CTXSYS", "XDB", "ANONYMOUS", "ORDDATA", "ORDPLUGINS",
            "ORDSYS", "SI_INFORMTN_SCHEMA", "MDSYS", "OLAPSYS", "MDDATA"
        };
        return systemSchemas.Contains(schema, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<ColumnModel> ReadColumnsForTable(
        DataTable columnsData,
        string schemaName,
        string tableName)
    {
        var ownerCol = GetExistingColumn(columnsData, "OWNER", "TABLE_SCHEMA");
        var tableNameCol = GetExistingColumn(columnsData, "TABLE_NAME");
        var columnNameCol = GetExistingColumn(columnsData, "COLUMN_NAME");
        var dataTypeCol = GetExistingColumn(columnsData, "DATATYPE", "DATA_TYPE");
        var lengthCol = GetExistingColumn(columnsData, "LENGTH", "DATA_LENGTH", "CHARACTER_MAXIMUM_LENGTH");
        var precisionCol = GetExistingColumn(columnsData, "PRECISION", "DATA_PRECISION", "NUMERIC_PRECISION");
        var scaleCol = GetExistingColumn(columnsData, "SCALE", "DATA_SCALE", "NUMERIC_SCALE");
        var nullableCol = GetExistingColumn(columnsData, "NULLABLE", "IS_NULLABLE");
        var idCol = GetExistingColumn(columnsData, "ID", "COLUMN_ID", "ORDINAL_POSITION");
        var defaultCol = GetExistingColumn(columnsData, "DATA_DEFAULT", "COLUMN_DEFAULT");

        var ordinal = 1;
        return columnsData
            .AsEnumerable()
            .Where(row =>
                (ownerCol == null || (row[ownerCol]?.ToString()).EqualsIgnoreCase(schemaName)) &&
                (tableNameCol == null || (row[tableNameCol]?.ToString()).EqualsIgnoreCase(tableName)))
            .OrderBy(row => idCol != null && !row.IsNull(idCol) ? Convert.ToInt32(row[idCol]) : ordinal++)
            .Select((row, index) => new ColumnModel(
                Name: columnNameCol != null ? row[columnNameCol]?.ToString() ?? "" : "",
                DataType: dataTypeCol != null ? row[dataTypeCol]?.ToString() ?? "" : "",
                MaxLength: lengthCol != null && !row.IsNull(lengthCol) ? Convert.ToInt32(row[lengthCol]) : 0,
                Precision: precisionCol != null && !row.IsNull(precisionCol) ? Convert.ToInt32(row[precisionCol]) : 0,
                Scale: scaleCol != null && !row.IsNull(scaleCol) ? Convert.ToInt32(row[scaleCol]) : 0,
                IsNullable: nullableCol != null && ((row[nullableCol]?.ToString()).EqualsIgnoreCase("Y") || (row[nullableCol]?.ToString()).EqualsIgnoreCase("YES")),
                OrdinalPosition: idCol != null && !row.IsNull(idCol) ? Convert.ToInt32(row[idCol]) : index + 1,
                DefaultValue: defaultCol != null && !row.IsNull(defaultCol) ? row[defaultCol]?.ToString() : null
            ));
    }

    private static IEnumerable<IndexModel> ReadIndexesForTable(
        DataTable indexesData,
        DataTable indexColumnsData,
        string schemaName,
        string tableName)
    {
        var ownerCol = GetExistingColumn(indexesData, "OWNER", "INDEX_OWNER", "TABLE_SCHEMA");
        var tableNameCol = GetExistingColumn(indexesData, "TABLE_NAME");
        var indexNameCol = GetExistingColumn(indexesData, "INDEX_NAME");
        var uniquenessCol = GetExistingColumn(indexesData, "UNIQUENESS");

        return indexesData
            .AsEnumerable()
            .Where(row =>
                (ownerCol == null || (row[ownerCol]?.ToString()).EqualsIgnoreCase(schemaName)) &&
                (tableNameCol == null || (row[tableNameCol]?.ToString()).EqualsIgnoreCase(tableName)))
            .Select(row => indexNameCol != null ? row[indexNameCol]?.ToString() ?? "" : "")
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .Select(indexName =>
            {
                var indexRow = indexesData.AsEnumerable()
                    .FirstOrDefault(r => indexNameCol != null && (r[indexNameCol]?.ToString()).EqualsIgnoreCase(indexName));

                var isUnique = indexRow != null && uniquenessCol != null &&
                    (indexRow[uniquenessCol]?.ToString()).EqualsIgnoreCase("UNIQUE");

                // Check if it's a primary key index (Oracle names them with _PK suffix typically)
                var isPrimary = indexName.EndsWith("_PK", StringComparison.OrdinalIgnoreCase) ||
                    indexName.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase);

                return IndexModel.Create(
                    indexName,
                    isUnique: isUnique || isPrimary,
                    isPrimaryKey: isPrimary,
                    isClustered: false, // Oracle uses IOT (Index Organized Tables) differently
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
        var ownerCol = GetExistingColumn(indexColumnsData, "OWNER", "INDEX_OWNER", "TABLE_SCHEMA");
        var tableNameCol = GetExistingColumn(indexColumnsData, "TABLE_NAME");
        var indexNameCol = GetExistingColumn(indexColumnsData, "INDEX_NAME");
        var columnNameCol = GetExistingColumn(indexColumnsData, "COLUMN_NAME");
        var positionCol = GetExistingColumn(indexColumnsData, "COLUMN_POSITION", "ORDINAL_POSITION");
        var descendCol = GetExistingColumn(indexColumnsData, "DESCEND");

        return indexColumnsData
            .AsEnumerable()
            .Where(row =>
                (ownerCol == null || (row[ownerCol]?.ToString()).EqualsIgnoreCase(schemaName)) &&
                (tableNameCol == null || (row[tableNameCol]?.ToString()).EqualsIgnoreCase(tableName)) &&
                (indexNameCol == null || (row[indexNameCol]?.ToString()).EqualsIgnoreCase(indexName)))
            .Select(row => new IndexColumnModel(
                ColumnName: columnNameCol != null ? row[columnNameCol]?.ToString() ?? "" : "",
                OrdinalPosition: positionCol != null && !row.IsNull(positionCol) ? Convert.ToInt32(row[positionCol]) : 1,
                IsDescending: descendCol != null && (row[descendCol]?.ToString()).EqualsIgnoreCase("DESC")));
    }

    private static string? GetExistingColumn(DataTable table, params string[] possibleNames)
        => possibleNames.FirstOrDefault(name => table.Columns.Contains(name));
}
