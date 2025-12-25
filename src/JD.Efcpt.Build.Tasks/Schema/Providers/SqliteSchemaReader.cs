using Microsoft.Data.Sqlite;

namespace JD.Efcpt.Build.Tasks.Schema.Providers;

/// <summary>
/// Reads schema metadata from SQLite databases using native SQLite system tables and PRAGMA commands.
/// </summary>
/// <remarks>
/// Microsoft.Data.Sqlite doesn't fully support the ADO.NET GetSchema() API, so this reader
/// uses SQLite's native metadata sources:
/// - sqlite_master table for tables and indexes
/// - PRAGMA table_info() for columns
/// - PRAGMA index_list() for table indexes
/// - PRAGMA index_info() for index columns
/// </remarks>
internal sealed class SqliteSchemaReader : ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from a SQLite database.
    /// </summary>
    public SchemaModel ReadSchema(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var tablesList = GetUserTables(connection);
        var tables = tablesList
            .Select(t => TableModel.Create(
                t.Schema,
                t.Name,
                ReadColumnsForTable(connection, t.Name),
                ReadIndexesForTable(connection, t.Name),
                []))
            .ToList();

        return SchemaModel.Create(tables);
    }

    private static List<(string Schema, string Name)> GetUserTables(SqliteConnection connection)
    {
        var tables = new List<(string Schema, string Name)>();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var tableName = reader.GetString(0);
            tables.Add(("main", tableName));
        }

        return tables;
    }

    private static IEnumerable<ColumnModel> ReadColumnsForTable(
        SqliteConnection connection,
        string tableName)
    {
        var columns = new List<ColumnModel>();

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({EscapeIdentifier(tableName)})";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // PRAGMA table_info returns: cid, name, type, notnull, dflt_value, pk
            var cid = reader.GetInt32(0);
            var name = reader.GetString(1);
            var type = reader.IsDBNull(2) ? "TEXT" : reader.GetString(2);
            var notNull = reader.GetInt32(3) == 1;
            var defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4);
            var isPrimaryKey = reader.GetInt32(5) == 1;

            columns.Add(new ColumnModel(
                Name: name,
                DataType: type,
                MaxLength: 0, // SQLite doesn't have length limits in the same way
                Precision: 0,
                Scale: 0,
                IsNullable: !notNull,
                OrdinalPosition: cid + 1, // Make 1-based
                DefaultValue: defaultValue
            ));
        }

        return columns;
    }

    private static IEnumerable<IndexModel> ReadIndexesForTable(
        SqliteConnection connection,
        string tableName)
    {
        var indexes = new List<IndexModel>();

        using var listCommand = connection.CreateCommand();
        listCommand.CommandText = $"PRAGMA index_list({EscapeIdentifier(tableName)})";

        using var listReader = listCommand.ExecuteReader();
        var indexInfos = new List<(int Seq, string Name, bool IsUnique, string Origin)>();

        while (listReader.Read())
        {
            // PRAGMA index_list returns: seq, name, unique, origin, partial
            var seq = listReader.GetInt32(0);
            var name = listReader.GetString(1);
            var isUnique = listReader.GetInt32(2) == 1;
            var origin = listReader.IsDBNull(3) ? "c" : listReader.GetString(3);

            indexInfos.Add((seq, name, isUnique, origin));
        }

        foreach (var indexInfo in indexInfos)
        {
            var columns = ReadIndexColumns(connection, indexInfo.Name);
            var isPrimaryKey = indexInfo.Origin == "pk";

            indexes.Add(IndexModel.Create(
                indexInfo.Name,
                isUnique: indexInfo.IsUnique,
                isPrimaryKey: isPrimaryKey,
                isClustered: false, // SQLite doesn't have clustered indexes in the traditional sense
                columns));
        }

        return indexes;
    }

    private static IEnumerable<IndexColumnModel> ReadIndexColumns(
        SqliteConnection connection,
        string indexName)
    {
        var columns = new List<IndexColumnModel>();

        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_info({EscapeIdentifier(indexName)})";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // PRAGMA index_info returns: seqno, cid, name
            var seqno = reader.GetInt32(0);
            var columnName = reader.IsDBNull(2) ? "" : reader.GetString(2);

            if (!string.IsNullOrEmpty(columnName))
            {
                columns.Add(new IndexColumnModel(
                    ColumnName: columnName,
                    OrdinalPosition: seqno + 1, // Make 1-based
                    IsDescending: false // SQLite index_info doesn't report sort order
                ));
            }
        }

        return columns;
    }

    private static string EscapeIdentifier(string identifier)
    {
        // Escape double quotes by doubling them, then wrap in quotes
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
