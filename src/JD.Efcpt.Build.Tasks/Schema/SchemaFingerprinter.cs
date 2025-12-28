using System.IO.Hashing;
using System.Text;
#if NETFRAMEWORK
using JD.Efcpt.Build.Tasks.Compatibility;
#endif

namespace JD.Efcpt.Build.Tasks.Schema;

/// <summary>
/// Computes deterministic fingerprints of database schema models using XxHash64.
/// </summary>
internal sealed class SchemaFingerprinter
{
    /// <summary>
    /// Computes a deterministic fingerprint of the schema model using XxHash64.
    /// </summary>
    /// <param name="schema">The schema model to fingerprint.</param>
    /// <returns>A hexadecimal string representation of the hash.</returns>
    public static string ComputeFingerprint(SchemaModel schema)
    {
        var hash = new XxHash64();
        var writer = new SchemaHashWriter(hash);

        writer.Write($"Tables:{schema.Tables.Count}");

        foreach (var table in schema.Tables)
        {
            writer.Write($"Table:{table.Schema}.{table.Name}");

            // Columns
            writer.Write($"Columns:{table.Columns.Count}");
            foreach (var col in table.Columns)
            {
                writer.Write($"Col:{col.Name}|{col.DataType}|{col.MaxLength}|" +
                           $"{col.Precision}|{col.Scale}|{col.IsNullable}|{col.OrdinalPosition}|{col.DefaultValue ?? ""}");
            }

            // Indexes
            writer.Write($"Indexes:{table.Indexes.Count}");
            foreach (var idx in table.Indexes)
            {
                writer.Write($"Idx:{idx.Name}|{idx.IsUnique}|{idx.IsPrimaryKey}|{idx.IsClustered}");
                foreach (var idxCol in idx.Columns)
                {
                    writer.Write($"IdxCol:{idxCol.ColumnName}|{idxCol.OrdinalPosition}|{idxCol.IsDescending}");
                }
            }

            // Constraints
            writer.Write($"Constraints:{table.Constraints.Count}");
            foreach (var constraint in table.Constraints)
            {
                writer.Write($"Const:{constraint.Name}|{constraint.Type}");

                if (constraint.Type == ConstraintType.Check && constraint.CheckExpression != null)
                    writer.Write($"CheckExpr:{constraint.CheckExpression}");

                if (constraint.Type == ConstraintType.ForeignKey && constraint.ForeignKey != null)
                {
                    var fk = constraint.ForeignKey;
                    writer.Write($"FK:{fk.ReferencedSchema}.{fk.ReferencedTable}");
                    foreach (var fkCol in fk.Columns)
                    {
                        writer.Write($"FKCol:{fkCol.ColumnName}->{fkCol.ReferencedColumnName}|{fkCol.OrdinalPosition}");
                    }
                }
            }
        }

        var hashBytes = hash.GetCurrentHash();
#if NETFRAMEWORK
        return NetFrameworkPolyfills.ToHexString(hashBytes);
#else
        return Convert.ToHexString(hashBytes);
#endif
    }

    private sealed class SchemaHashWriter
    {
        private readonly XxHash64 _hash;

        public SchemaHashWriter(XxHash64 hash) => _hash = hash;

        public void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value + "\n");
            _hash.Append(bytes);
        }
    }
}
