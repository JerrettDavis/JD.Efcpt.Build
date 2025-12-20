using JD.Efcpt.Build.Tasks.Schema;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Build.Tests.Schema;

[Feature("SchemaFingerprinter: computes deterministic fingerprints of database schemas")]
[Collection(nameof(AssemblySetup))]
public sealed class SchemaFingerprinterTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestResult(
        string Fingerprint1,
        string Fingerprint2);

    [Scenario("Empty schema produces consistent fingerprint")]
    [Fact]
    public async Task Empty_schema_produces_consistent_fingerprint()
    {
        await Given("empty schema", () => SchemaModel.Empty)
            .When("compute fingerprint twice", schema =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schema);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schema);
                return new TestResult(fp1, fp2);
            })
            .Then("both fingerprints are not empty", r => !string.IsNullOrEmpty(r.Fingerprint1))
            .And("both fingerprints are identical", r => r.Fingerprint1 == r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Single table schema produces deterministic fingerprint")]
    [Fact]
    public async Task Single_table_schema_produces_deterministic_fingerprint()
    {
        await Given("schema with single table", () =>
            {
                var table = TableModel.Create(
                    "dbo",
                    "Users",
                    new[]
                    {
                        new ColumnModel("Id", "int", 0, 10, 0, false, 1, null),
                        new ColumnModel("Name", "nvarchar", 100, 0, 0, false, 2, null)
                    },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );
                return SchemaModel.Create(new[] { table });
            })
            .When("compute fingerprint twice", schema =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schema);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schema);
                return new TestResult(fp1, fp2);
            })
            .Then("both fingerprints are not empty", r => !string.IsNullOrEmpty(r.Fingerprint1))
            .And("both fingerprints are identical", r => r.Fingerprint1 == r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Different table names produce different fingerprints")]
    [Fact]
    public async Task Different_table_names_produce_different_fingerprints()
    {
        await Given("two schemas with different table names", () =>
            {
                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Id", "int", 0, 10, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );
                var table2 = TableModel.Create(
                    "dbo",
                    "Products",
                    new[] { new ColumnModel("Id", "int", 0, 10, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Different column data types produce different fingerprints")]
    [Fact]
    public async Task Different_column_data_types_produce_different_fingerprints()
    {
        await Given("two schemas with different column types", () =>
            {
                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Name", "nvarchar", 100, 0, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );
                var table2 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Name", "varchar", 100, 0, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Adding a column produces different fingerprint")]
    [Fact]
    public async Task Adding_column_produces_different_fingerprint()
    {
        await Given("two schemas with different column counts", () =>
            {
                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Id", "int", 0, 10, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );
                var table2 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[]
                    {
                        new ColumnModel("Id", "int", 0, 10, 0, false, 1, null),
                        new ColumnModel("Name", "nvarchar", 100, 0, 0, false, 2, null)
                    },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Index changes produce different fingerprint")]
    [Fact]
    public async Task Index_changes_produce_different_fingerprint()
    {
        await Given("two schemas with different indexes", () =>
            {
                var columns = new[] { new ColumnModel("Id", "int", 0, 10, 0, false, 1, null) };

                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    columns,
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var index = IndexModel.Create(
                    "PK_Users",
                    isUnique: true,
                    isPrimaryKey: true,
                    isClustered: true,
                    new[] { new IndexColumnModel("Id", 1, false) }
                );

                var table2 = TableModel.Create(
                    "dbo",
                    "Users",
                    columns,
                    new[] { index },
                    Array.Empty<ConstraintModel>()
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Foreign key constraint changes produce different fingerprint")]
    [Fact]
    public async Task Foreign_key_constraint_changes_produce_different_fingerprint()
    {
        await Given("two schemas with different foreign keys", () =>
            {
                var columns = new[] { new ColumnModel("UserId", "int", 0, 10, 0, false, 1, null) };

                var table1 = TableModel.Create(
                    "dbo",
                    "Orders",
                    columns,
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var fk = ForeignKeyModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ForeignKeyColumnModel("UserId", "Id", 1) }
                );

                var constraint = new ConstraintModel(
                    "FK_Orders_Users",
                    ConstraintType.ForeignKey,
                    null,
                    fk
                );

                var table2 = TableModel.Create(
                    "dbo",
                    "Orders",
                    columns,
                    Array.Empty<IndexModel>(),
                    new[] { constraint }
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Check constraint changes produce different fingerprint")]
    [Fact]
    public async Task Check_constraint_changes_produce_different_fingerprint()
    {
        await Given("two schemas with different check constraints", () =>
            {
                var columns = new[] { new ColumnModel("Age", "int", 0, 10, 0, false, 1, null) };

                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    columns,
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var checkConstraint = new ConstraintModel(
                    "CK_Users_Age",
                    ConstraintType.Check,
                    "Age >= 18",
                    null
                );

                var table2 = TableModel.Create(
                    "dbo",
                    "Users",
                    columns,
                    Array.Empty<IndexModel>(),
                    new[] { checkConstraint }
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Multiple tables produce deterministic fingerprint")]
    [Fact]
    public async Task Multiple_tables_produce_deterministic_fingerprint()
    {
        await Given("schema with multiple tables in random order", () =>
            {
                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Id", "int", 0, 10, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var table2 = TableModel.Create(
                    "dbo",
                    "Products",
                    new[] { new ColumnModel("Id", "int", 0, 10, 0, false, 1, null) },
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                // SchemaModel.Create normalizes (sorts) the tables
                return SchemaModel.Create(new[] { table2, table1 }); // Intentionally out of order
            })
            .When("compute fingerprint twice", schema =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schema);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schema);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are identical", r => r.Fingerprint1 == r.Fingerprint2)
            .AssertPassed();
    }

    [Scenario("Nullable column change produces different fingerprint")]
    [Fact]
    public async Task Nullable_column_change_produces_different_fingerprint()
    {
        await Given("two schemas with different column nullability", () =>
            {
                var table1 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Email", "nvarchar", 100, 0, 0, false, 1, null) }, // NOT NULL
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var table2 = TableModel.Create(
                    "dbo",
                    "Users",
                    new[] { new ColumnModel("Email", "nvarchar", 100, 0, 0, true, 1, null) }, // NULL
                    Array.Empty<IndexModel>(),
                    Array.Empty<ConstraintModel>()
                );

                var schema1 = SchemaModel.Create(new[] { table1 });
                var schema2 = SchemaModel.Create(new[] { table2 });

                return (schema1, schema2);
            })
            .When("compute fingerprints", schemas =>
            {
                var fp1 = SchemaFingerprinter.ComputeFingerprint(schemas.schema1);
                var fp2 = SchemaFingerprinter.ComputeFingerprint(schemas.schema2);
                return new TestResult(fp1, fp2);
            })
            .Then("fingerprints are different", r => r.Fingerprint1 != r.Fingerprint2)
            .AssertPassed();
    }
}
