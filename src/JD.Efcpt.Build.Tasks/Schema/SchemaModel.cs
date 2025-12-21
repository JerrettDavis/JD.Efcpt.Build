namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Canonical, deterministic representation of database schema.
/// All collections are sorted for consistent fingerprinting.
/// </summary>
public sealed record SchemaModel(
    IReadOnlyList<TableModel> Tables
)
{
    /// <summary>
    /// Gets an empty schema model with no tables.
    /// </summary>
    public static SchemaModel Empty => new([]);

    /// <summary>
    /// Creates a sorted, normalized schema model.
    /// </summary>
    public static SchemaModel Create(IEnumerable<TableModel> tables)
    {
        var sorted = tables
            .OrderBy(t => t.Schema, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SchemaModel(sorted);
    }
}

/// <summary>
/// Represents a database table with its columns, indexes, and constraints.
/// </summary>
public sealed record TableModel(
    string Schema,
    string Name,
    IReadOnlyList<ColumnModel> Columns,
    IReadOnlyList<IndexModel> Indexes,
    IReadOnlyList<ConstraintModel> Constraints
)
{
    /// <summary>
    /// Creates a sorted, normalized table model.
    /// </summary>
    public static TableModel Create(
        string schema,
        string name,
        IEnumerable<ColumnModel> columns,
        IEnumerable<IndexModel> indexes,
        IEnumerable<ConstraintModel> constraints)
    {
        return new TableModel(
            schema,
            name,
            columns.OrderBy(c => c.OrdinalPosition).ToList(),
            indexes.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            constraints.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
        );
    }
}

/// <summary>
/// Represents a database column.
/// </summary>
public sealed record ColumnModel(
    string Name,
    string DataType,
    int MaxLength,
    int Precision,
    int Scale,
    bool IsNullable,
    int OrdinalPosition,
    string? DefaultValue
);

/// <summary>
/// Represents a database index.
/// </summary>
public sealed record IndexModel(
    string Name,
    bool IsUnique,
    bool IsPrimaryKey,
    bool IsClustered,
    IReadOnlyList<IndexColumnModel> Columns
)
{
    /// <summary>
    /// Creates a sorted, normalized index model.
    /// </summary>
    public static IndexModel Create(
        string name,
        bool isUnique,
        bool isPrimaryKey,
        bool isClustered,
        IEnumerable<IndexColumnModel> columns)
    {
        return new IndexModel(
            name,
            isUnique,
            isPrimaryKey,
            isClustered,
            columns.OrderBy(c => c.OrdinalPosition).ToList()
        );
    }
}

/// <summary>
/// Represents a column within an index.
/// </summary>
public sealed record IndexColumnModel(
    string ColumnName,
    int OrdinalPosition,
    bool IsDescending
);

/// <summary>
/// Represents a database constraint.
/// </summary>
public sealed record ConstraintModel(
    string Name,
    ConstraintType Type,
    string? CheckExpression,
    ForeignKeyModel? ForeignKey
);

/// <summary>
/// Defines the types of database constraints.
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Primary key constraint.
    /// </summary>
    PrimaryKey,

    /// <summary>
    /// Foreign key constraint.
    /// </summary>
    ForeignKey,

    /// <summary>
    /// Check constraint.
    /// </summary>
    Check,

    /// <summary>
    /// Default value constraint.
    /// </summary>
    Default,

    /// <summary>
    /// Unique constraint.
    /// </summary>
    Unique
}

/// <summary>
/// Represents a foreign key constraint.
/// </summary>
public sealed record ForeignKeyModel(
    string ReferencedSchema,
    string ReferencedTable,
    IReadOnlyList<ForeignKeyColumnModel> Columns
)
{
    /// <summary>
    /// Creates a sorted, normalized foreign key model.
    /// </summary>
    public static ForeignKeyModel Create(
        string referencedSchema,
        string referencedTable,
        IEnumerable<ForeignKeyColumnModel> columns)
    {
        return new ForeignKeyModel(
            referencedSchema,
            referencedTable,
            columns.OrderBy(c => c.OrdinalPosition).ToList()
        );
    }
}

/// <summary>
/// Represents a column mapping in a foreign key constraint.
/// </summary>
public sealed record ForeignKeyColumnModel(
    string ColumnName,
    string ReferencedColumnName,
    int OrdinalPosition
);
