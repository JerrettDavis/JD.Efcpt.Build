namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Defines a contract for reading schema metadata from a database.
/// </summary>
internal interface ISchemaReader
{
    /// <summary>
    /// Reads the complete schema from the database specified by the connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <returns>A canonical schema model representing the database structure.</returns>
    SchemaModel ReadSchema(string connectionString);
}
