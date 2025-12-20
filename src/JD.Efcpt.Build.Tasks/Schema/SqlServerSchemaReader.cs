using Microsoft.Data.SqlClient;

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Reads schema metadata from SQL Server databases using system catalog views.
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

        var tables = new List<TableModel>();

        // Query all user tables (exclude system tables)
        var tableQuery = @"
            SELECT
                SCHEMA_NAME(schema_id) AS SchemaName,
                name AS TableName,
                object_id AS ObjectId
            FROM sys.tables
            WHERE is_ms_shipped = 0
            ORDER BY schema_id, name
        ";

        using var tableCommand = new SqlCommand(tableQuery, connection);
        using var tableReader = tableCommand.ExecuteReader();

        var tableData = new List<(string Schema, string Name, int ObjectId)>();
        while (tableReader.Read())
        {
            tableData.Add((
                tableReader.GetString(0),
                tableReader.GetString(1),
                tableReader.GetInt32(2)
            ));
        }
        tableReader.Close();

        foreach (var (schema, name, objectId) in tableData)
        {
            var columns = ReadColumns(connection, objectId);
            var indexes = ReadIndexes(connection, objectId);
            var constraints = ReadConstraints(connection, objectId);

            tables.Add(TableModel.Create(schema, name, columns, indexes, constraints));
        }

        return SchemaModel.Create(tables);
    }

    private static IEnumerable<ColumnModel> ReadColumns(SqlConnection connection, int objectId)
    {
        var query = @"
            SELECT
                c.name AS ColumnName,
                t.name AS DataType,
                c.max_length AS MaxLength,
                c.precision AS Precision,
                c.scale AS Scale,
                c.is_nullable AS IsNullable,
                c.column_id AS OrdinalPosition,
                dc.definition AS DefaultValue
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            LEFT JOIN sys.default_constraints dc ON c.object_id = dc.parent_object_id
                AND c.column_id = dc.parent_column_id
            WHERE c.object_id = @ObjectId
            ORDER BY c.column_id
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ObjectId", objectId);
        using var reader = command.ExecuteReader();

        var columns = new List<ColumnModel>();
        while (reader.Read())
        {
            columns.Add(new ColumnModel(
                Name: reader.GetString(0),
                DataType: reader.GetString(1),
                MaxLength: reader.GetInt16(2),
                Precision: reader.GetByte(3),
                Scale: reader.GetByte(4),
                IsNullable: reader.GetBoolean(5),
                OrdinalPosition: reader.GetInt32(6),
                DefaultValue: reader.IsDBNull(7) ? null : reader.GetString(7)
            ));
        }

        return columns;
    }

    private static IEnumerable<IndexModel> ReadIndexes(SqlConnection connection, int objectId)
    {
        var query = @"
            SELECT
                i.name AS IndexName,
                i.is_unique AS IsUnique,
                i.is_primary_key AS IsPrimaryKey,
                i.type_desc AS TypeDesc,
                i.index_id AS IndexId
            FROM sys.indexes i
            WHERE i.object_id = @ObjectId AND i.type > 0
            ORDER BY i.name
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ObjectId", objectId);
        using var reader = command.ExecuteReader();

        var indexData = new List<(string Name, bool IsUnique, bool IsPrimaryKey, string TypeDesc, int IndexId)>();
        while (reader.Read())
        {
            indexData.Add((
                reader.GetString(0),
                reader.GetBoolean(1),
                reader.GetBoolean(2),
                reader.GetString(3),
                reader.GetInt32(4)
            ));
        }
        reader.Close();

        var indexes = new List<IndexModel>();
        foreach (var (name, isUnique, isPrimaryKey, typeDesc, indexId) in indexData)
        {
            var columns = ReadIndexColumns(connection, objectId, indexId);
            indexes.Add(IndexModel.Create(
                name,
                isUnique,
                isPrimaryKey,
                typeDesc.Contains("CLUSTERED"),
                columns
            ));
        }

        return indexes;
    }

    private static IEnumerable<IndexColumnModel> ReadIndexColumns(
        SqlConnection connection,
        int objectId,
        int indexId)
    {
        var query = @"
            SELECT
                c.name AS ColumnName,
                ic.key_ordinal AS OrdinalPosition,
                ic.is_descending_key AS IsDescending
            FROM sys.index_columns ic
            INNER JOIN sys.columns c ON ic.object_id = c.object_id
                AND ic.column_id = c.column_id
            WHERE ic.object_id = @ObjectId AND ic.index_id = @IndexId
            ORDER BY ic.key_ordinal
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ObjectId", objectId);
        command.Parameters.AddWithValue("@IndexId", indexId);
        using var reader = command.ExecuteReader();

        var columns = new List<IndexColumnModel>();
        while (reader.Read())
        {
            columns.Add(new IndexColumnModel(
                ColumnName: reader.GetString(0),
                OrdinalPosition: reader.GetByte(1),
                IsDescending: reader.GetBoolean(2)
            ));
        }

        return columns;
    }

    private static IEnumerable<ConstraintModel> ReadConstraints(SqlConnection connection, int objectId)
    {
        var constraints = new List<ConstraintModel>();

        // Check constraints
        var checkQuery = @"
            SELECT name, definition
            FROM sys.check_constraints
            WHERE parent_object_id = @ObjectId
        ";

        using (var command = new SqlCommand(checkQuery, connection))
        {
            command.Parameters.AddWithValue("@ObjectId", objectId);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                constraints.Add(new ConstraintModel(
                    Name: reader.GetString(0),
                    Type: ConstraintType.Check,
                    CheckExpression: reader.GetString(1),
                    ForeignKey: null
                ));
            }
        }

        // Foreign keys
        var fkQuery = @"
            SELECT
                fk.name AS FKName,
                SCHEMA_NAME(ref_t.schema_id) AS RefSchema,
                ref_t.name AS RefTable,
                fk.object_id AS FKObjectId
            FROM sys.foreign_keys fk
            INNER JOIN sys.tables ref_t ON fk.referenced_object_id = ref_t.object_id
            WHERE fk.parent_object_id = @ObjectId
        ";

        using (var command = new SqlCommand(fkQuery, connection))
        {
            command.Parameters.AddWithValue("@ObjectId", objectId);
            using var reader = command.ExecuteReader();

            var fkData = new List<(string Name, string RefSchema, string RefTable, int FKObjectId)>();
            while (reader.Read())
            {
                fkData.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3)
                ));
            }
            reader.Close();

            foreach (var (name, refSchema, refTable, fkObjectId) in fkData)
            {
                var fkColumns = ReadForeignKeyColumns(connection, fkObjectId);
                constraints.Add(new ConstraintModel(
                    Name: name,
                    Type: ConstraintType.ForeignKey,
                    CheckExpression: null,
                    ForeignKey: ForeignKeyModel.Create(refSchema, refTable, fkColumns)
                ));
            }
        }

        return constraints;
    }

    private static IEnumerable<ForeignKeyColumnModel> ReadForeignKeyColumns(
        SqlConnection connection,
        int fkObjectId)
    {
        var query = @"
            SELECT
                c.name AS ColumnName,
                ref_c.name AS RefColumnName,
                fkc.constraint_column_id AS OrdinalPosition
            FROM sys.foreign_key_columns fkc
            INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id
                AND fkc.parent_column_id = c.column_id
            INNER JOIN sys.columns ref_c ON fkc.referenced_object_id = ref_c.object_id
                AND fkc.referenced_column_id = ref_c.column_id
            WHERE fkc.constraint_object_id = @FKObjectId
            ORDER BY fkc.constraint_column_id
        ";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@FKObjectId", fkObjectId);
        using var reader = command.ExecuteReader();

        var columns = new List<ForeignKeyColumnModel>();
        while (reader.Read())
        {
            columns.Add(new ForeignKeyColumnModel(
                ColumnName: reader.GetString(0),
                ReferencedColumnName: reader.GetString(1),
                OrdinalPosition: reader.GetInt32(2)
            ));
        }

        return columns;
    }
}
