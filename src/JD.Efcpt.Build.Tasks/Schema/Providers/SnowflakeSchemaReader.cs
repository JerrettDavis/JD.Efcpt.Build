using System.Data;
using Snowflake.Data.Client;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from Snowflake databases using GetSchema() for standard metadata.
/// </summary>
/// <remarks>
/// Snowflake's GetSchema() support is limited. This implementation uses what's available
/// and falls back to INFORMATION_SCHEMA queries when necessary.
/// </remarks>
internal sealed class SnowflakeSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from a Snowflake database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new SnowflakeDbConnection(connectionString);
        connection.Open();

        // Snowflake has limited GetSchema support, so we use INFORMATION_SCHEMA
        var tablesList = GetUserTables(connection);

        var tables = tablesList
            .Select(t => TableModel.Create(
                t.Schema,
                t.Name,
                GetColumnsForTable(connection, t.Schema, t.Name),
                GetIndexesForTable(connection, t.Schema, t.Name),
                []))
            .ToList();

        return SchemaModel.Create(tables);
    }

    private static List<(string Schema, string Name)> GetUserTables(SnowflakeDbConnection connection)
    {
        // Try GetSchema first
        try
        {
            var tablesData = connection.GetSchema("Tables");
            if (tablesData.Rows.Count > 0)
            {
                return tablesData
                    .AsEnumerable()
                    .Where(row => !IsSystemSchema(row["TABLE_SCHEMA"]?.ToString() ?? ""))
                    .Where(row => row["TABLE_TYPE"]?.ToString() == "BASE TABLE" ||
                                  row["TABLE_TYPE"]?.ToString() == "TABLE")
                    .Select(row => (
                        Schema: row["TABLE_SCHEMA"]?.ToString() ?? "",
                        Name: row["TABLE_NAME"]?.ToString() ?? ""))
                    .Where(t => !string.IsNullOrEmpty(t.Name))
                    .OrderBy(t => t.Schema)
                    .ThenBy(t => t.Name)
                    .ToList();
            }
        }
        catch
        {
            // Fall through to INFORMATION_SCHEMA query
        }

        // Fall back to INFORMATION_SCHEMA
        return QueryTables(connection);
    }

    private static List<(string Schema, string Name)> QueryTables(SnowflakeDbConnection connection)
    {
        var result = new List<(string Schema, string Name)>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA NOT IN ('INFORMATION_SCHEMA')
            ORDER BY TABLE_SCHEMA, TABLE_NAME";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add((
                Schema: reader.GetString(0),
                Name: reader.GetString(1)));
        }

        return result;
    }

    private static bool IsSystemSchema(string schema)
        => string.Equals(schema, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<ColumnModel> GetColumnsForTable(
        SnowflakeDbConnection connection,
        string schemaName,
        string tableName)
    {
        // Try GetSchema first
        try
        {
            var columnsData = connection.GetSchema("Columns");
            if (columnsData.Rows.Count > 0)
            {
                return columnsData
                    .AsEnumerable()
                    .Where(row => string.Equals(row["TABLE_SCHEMA"]?.ToString(), schemaName, StringComparison.OrdinalIgnoreCase) &&
                                  string.Equals(row["TABLE_NAME"]?.ToString(), tableName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(row => Convert.ToInt32(row["ORDINAL_POSITION"]))
                    .Select(row => new ColumnModel(
                        Name: row["COLUMN_NAME"]?.ToString() ?? "",
                        DataType: row["DATA_TYPE"]?.ToString() ?? "",
                        MaxLength: row.IsNull("CHARACTER_MAXIMUM_LENGTH") ? 0 : Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]),
                        Precision: row.IsNull("NUMERIC_PRECISION") ? 0 : Convert.ToInt32(row["NUMERIC_PRECISION"]),
                        Scale: row.IsNull("NUMERIC_SCALE") ? 0 : Convert.ToInt32(row["NUMERIC_SCALE"]),
                        IsNullable: row["IS_NULLABLE"]?.ToString() == "YES",
                        OrdinalPosition: Convert.ToInt32(row["ORDINAL_POSITION"]),
                        DefaultValue: row.IsNull("COLUMN_DEFAULT") ? null : row["COLUMN_DEFAULT"]?.ToString()
                    ))
                    .ToList();
            }
        }
        catch
        {
            // Fall through to direct query
        }

        // Fall back to INFORMATION_SCHEMA
        return QueryColumns(connection, schemaName, tableName);
    }

    private static List<ColumnModel> QueryColumns(
        SnowflakeDbConnection connection,
        string schemaName,
        string tableName)
    {
        var result = new List<ColumnModel>();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                COLUMN_NAME,
                DATA_TYPE,
                COALESCE(CHARACTER_MAXIMUM_LENGTH, 0) as MAX_LENGTH,
                COALESCE(NUMERIC_PRECISION, 0) as PRECISION,
                COALESCE(NUMERIC_SCALE, 0) as SCALE,
                IS_NULLABLE,
                ORDINAL_POSITION,
                COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = :schema AND TABLE_NAME = :table
            ORDER BY ORDINAL_POSITION";

        var schemaParam = command.CreateParameter();
        schemaParam.ParameterName = "schema";
        schemaParam.Value = schemaName;
        command.Parameters.Add(schemaParam);

        var tableParam = command.CreateParameter();
        tableParam.ParameterName = "table";
        tableParam.Value = tableName;
        command.Parameters.Add(tableParam);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ColumnModel(
                Name: reader.GetString(0),
                DataType: reader.GetString(1),
                MaxLength: reader.GetInt32(2),
                Precision: reader.GetInt32(3),
                Scale: reader.GetInt32(4),
                IsNullable: reader.GetString(5) == "YES",
                OrdinalPosition: reader.GetInt32(6),
                DefaultValue: reader.IsDBNull(7) ? null : reader.GetString(7)
            ));
        }

        return result;
    }

    private static IEnumerable<IndexModel> GetIndexesForTable(
        SnowflakeDbConnection connection,
        string schemaName,
        string tableName)
    {
        // Snowflake doesn't have traditional indexes - it uses micro-partitioning
        // and automatic clustering. We can return primary key constraints as "indexes"
        // for fingerprinting purposes.

        var result = new List<IndexModel>();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    c.CONSTRAINT_NAME,
                    c.CONSTRAINT_TYPE,
                    kcu.COLUMN_NAME,
                    kcu.ORDINAL_POSITION
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                    ON c.CONSTRAINT_CATALOG = kcu.CONSTRAINT_CATALOG
                    AND c.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA
                    AND c.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                WHERE c.TABLE_SCHEMA = :schema
                    AND c.TABLE_NAME = :table
                    AND c.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE')
                ORDER BY c.CONSTRAINT_NAME, kcu.ORDINAL_POSITION";

            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "schema";
            schemaParam.Value = schemaName;
            command.Parameters.Add(schemaParam);

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "table";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);

            var constraints = new Dictionary<string, (string Type, List<IndexColumnModel> Columns)>();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var constraintName = reader.GetString(0);
                var constraintType = reader.GetString(1);
                var columnName = reader.GetString(2);
                var ordinalPosition = reader.GetInt32(3);

                if (!constraints.TryGetValue(constraintName, out var constraint))
                {
                    constraint = (constraintType, new List<IndexColumnModel>());
                    constraints[constraintName] = constraint;
                }

                constraint.Columns.Add(new IndexColumnModel(
                    ColumnName: columnName,
                    OrdinalPosition: ordinalPosition,
                    IsDescending: false));
            }

            foreach (var (name, (type, columns)) in constraints)
            {
                result.Add(IndexModel.Create(
                    name,
                    isUnique: true, // Both PK and UNIQUE constraints are unique
                    isPrimaryKey: type == "PRIMARY KEY",
                    isClustered: false, // Snowflake doesn't have clustered indexes
                    columns));
            }
        }
        catch
        {
            // If constraints query fails, return empty list
        }

        return result;
    }
}
