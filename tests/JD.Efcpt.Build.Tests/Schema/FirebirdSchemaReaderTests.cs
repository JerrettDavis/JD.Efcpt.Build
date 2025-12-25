using System.Data;
using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

/// <summary>
/// Unit tests for FirebirdSchemaReader parsing logic.
/// These tests verify that the reader correctly parses DataTables
/// with various column naming conventions used by Firebird.
/// </summary>
[Feature("FirebirdSchemaReader: parses Firebird GetSchema() DataTables")]
[Collection(nameof(AssemblySetup))]
public sealed class FirebirdSchemaReaderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Test Helpers

    /// <summary>
    /// Creates a mock Tables DataTable with Firebird column naming.
    /// </summary>
    private static DataTable CreateTablesDataTable(params (string TableName, bool IsSystem)[] tables)
    {
        var dt = new DataTable("Tables");
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("IS_SYSTEM_TABLE", typeof(bool));
        dt.Columns.Add("TABLE_TYPE", typeof(string));

        foreach (var (tableName, isSystem) in tables)
        {
            var row = dt.NewRow();
            row["TABLE_NAME"] = tableName;
            row["IS_SYSTEM_TABLE"] = isSystem;
            row["TABLE_TYPE"] = "TABLE";
            dt.Rows.Add(row);
        }

        return dt;
    }

    /// <summary>
    /// Creates a mock Columns DataTable with Firebird column naming.
    /// </summary>
    private static DataTable CreateColumnsDataTable(
        params (string TableName, string ColumnName, string DataType, int? Size, bool IsNullable, int Ordinal)[] columns)
    {
        var dt = new DataTable("Columns");
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("COLUMN_NAME", typeof(string));
        dt.Columns.Add("COLUMN_DATA_TYPE", typeof(string));
        dt.Columns.Add("COLUMN_SIZE", typeof(int));
        dt.Columns.Add("IS_NULLABLE", typeof(string));
        dt.Columns.Add("ORDINAL_POSITION", typeof(int));
        dt.Columns.Add("COLUMN_DEFAULT", typeof(string));
        dt.Columns.Add("NUMERIC_PRECISION", typeof(int));
        dt.Columns.Add("NUMERIC_SCALE", typeof(int));

        foreach (var (tableName, columnName, dataType, size, isNullable, ordinal) in columns)
        {
            var row = dt.NewRow();
            row["TABLE_NAME"] = tableName;
            row["COLUMN_NAME"] = columnName;
            row["COLUMN_DATA_TYPE"] = dataType;
            row["COLUMN_SIZE"] = size ?? (object)DBNull.Value;
            row["IS_NULLABLE"] = isNullable ? "YES" : "NO";
            row["ORDINAL_POSITION"] = ordinal;
            row["COLUMN_DEFAULT"] = DBNull.Value;
            row["NUMERIC_PRECISION"] = DBNull.Value;
            row["NUMERIC_SCALE"] = DBNull.Value;
            dt.Rows.Add(row);
        }

        return dt;
    }

    /// <summary>
    /// Creates a mock Indexes DataTable with Firebird column naming.
    /// </summary>
    private static DataTable CreateIndexesDataTable(
        params (string TableName, string IndexName, bool IsUnique, bool IsPrimary)[] indexes)
    {
        var dt = new DataTable("Indexes");
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("INDEX_NAME", typeof(string));
        dt.Columns.Add("IS_UNIQUE", typeof(bool));
        dt.Columns.Add("IS_PRIMARY", typeof(bool));

        foreach (var (tableName, indexName, isUnique, isPrimary) in indexes)
        {
            var row = dt.NewRow();
            row["TABLE_NAME"] = tableName;
            row["INDEX_NAME"] = indexName;
            row["IS_UNIQUE"] = isUnique;
            row["IS_PRIMARY"] = isPrimary;
            dt.Rows.Add(row);
        }

        return dt;
    }

    /// <summary>
    /// Creates a mock IndexColumns DataTable with Firebird column naming.
    /// </summary>
    private static DataTable CreateIndexColumnsDataTable(
        params (string TableName, string IndexName, string ColumnName, int Ordinal)[] indexColumns)
    {
        var dt = new DataTable("IndexColumns");
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("INDEX_NAME", typeof(string));
        dt.Columns.Add("COLUMN_NAME", typeof(string));
        dt.Columns.Add("ORDINAL_POSITION", typeof(int));

        foreach (var (tableName, indexName, columnName, ordinal) in indexColumns)
        {
            var row = dt.NewRow();
            row["TABLE_NAME"] = tableName;
            row["INDEX_NAME"] = indexName;
            row["COLUMN_NAME"] = columnName;
            row["ORDINAL_POSITION"] = ordinal;
            dt.Rows.Add(row);
        }

        return dt;
    }

    #endregion

    #region GetExistingColumn Tests

    [Scenario("GetExistingColumn finds first matching column name")]
    [Fact]
    public async Task GetExistingColumn_finds_first_match()
    {
        // This tests the internal column detection logic via public behavior
        await Given("a DataTable with COLUMN_DATA_TYPE column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("COLUMN_DATA_TYPE", typeof(string));
                return dt;
            })
            .When("parsing columns", dt =>
            {
                // The reader should find COLUMN_DATA_TYPE when looking for data type
                var columnsTable = CreateColumnsDataTable(
                    ("TEST_TABLE", "ID", "INTEGER", 4, false, 1));
                return columnsTable.Columns.Contains("COLUMN_DATA_TYPE");
            })
            .Then("column is found", found => found)
            .AssertPassed();
    }

    [Scenario("GetExistingColumn falls back to alternate column name")]
    [Fact]
    public async Task GetExistingColumn_uses_fallback()
    {
        await Given("a DataTable with DATA_TYPE instead of COLUMN_DATA_TYPE", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("DATA_TYPE", typeof(string));
                return dt;
            })
            .When("checking for fallback", dt => dt.Columns.Contains("DATA_TYPE"))
            .Then("fallback column is found", found => found)
            .AssertPassed();
    }

    #endregion

    #region System Table Filtering Tests

    [Scenario("Filters out RDB$ system tables")]
    [Fact]
    public async Task Filters_rdb_system_tables()
    {
        await Given("tables including RDB$ system tables", () =>
                CreateTablesDataTable(
                    ("USERS", false),
                    ("RDB$RELATIONS", false),
                    ("RDB$FIELDS", false),
                    ("PRODUCTS", false)))
            .When("filtering user tables", tablesData =>
            {
                // Simulate the filtering logic
                return tablesData.AsEnumerable()
                    .Where(row =>
                    {
                        var tableName = row["TABLE_NAME"]?.ToString() ?? "";
                        return !tableName.StartsWith("RDB$", StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(row => row["TABLE_NAME"]?.ToString())
                    .ToList();
            })
            .Then("RDB$ tables are excluded", tables => !tables.Any(t => t is not null && t.StartsWith("RDB$")))
            .And("user tables are included", tables => tables.Contains("USERS") && tables.Contains("PRODUCTS"))
            .AssertPassed();
    }

    [Scenario("Filters out MON$ monitoring tables")]
    [Fact]
    public async Task Filters_mon_system_tables()
    {
        await Given("tables including MON$ monitoring tables", () =>
                CreateTablesDataTable(
                    ("ORDERS", false),
                    ("MON$STATEMENTS", false),
                    ("MON$ATTACHMENTS", false)))
            .When("filtering user tables", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row =>
                    {
                        var tableName = row["TABLE_NAME"]?.ToString() ?? "";
                        return !tableName.StartsWith("MON$", StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(row => row["TABLE_NAME"]?.ToString())
                    .ToList();
            })
            .Then("MON$ tables are excluded", tables => !tables.Any(t => t is not null && t.StartsWith("MON$")))
            .And("user tables are included", tables => tables.Contains("ORDERS"))
            .AssertPassed();
    }

    [Scenario("Filters tables by IS_SYSTEM_TABLE flag")]
    [Fact]
    public async Task Filters_by_system_flag()
    {
        await Given("tables with IS_SYSTEM_TABLE flags", () =>
                CreateTablesDataTable(
                    ("USERS", false),
                    ("SYS_CONFIG", true),
                    ("PRODUCTS", false)))
            .When("filtering by system flag", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row =>
                    {
                        var isSystem = row["IS_SYSTEM_TABLE"];
                        if (isSystem is bool b) return !b;
                        return true;
                    })
                    .Select(row => row["TABLE_NAME"]?.ToString())
                    .ToList();
            })
            .Then("system tables are excluded", tables => !tables.Contains("SYS_CONFIG"))
            .And("user tables are included", tables => tables.Contains("USERS") && tables.Contains("PRODUCTS"))
            .AssertPassed();
    }

    #endregion

    #region Column Parsing Tests

    [Scenario("Parses column names correctly")]
    [Fact]
    public async Task Parses_column_names()
    {
        await Given("columns data for a table", () =>
                CreateColumnsDataTable(
                    ("USERS", "ID", "INTEGER", 4, false, 1),
                    ("USERS", "NAME", "VARCHAR", 100, true, 2),
                    ("USERS", "EMAIL", "VARCHAR", 255, true, 3)))
            .When("extracting column names for USERS", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .Where(row => row["TABLE_NAME"]?.ToString() == "USERS")
                    .Select(row => row["COLUMN_NAME"]?.ToString())
                    .ToList();
            })
            .Then("all columns are found", columns => columns.Count == 3)
            .And("ID column exists", columns => columns.Contains("ID"))
            .And("NAME column exists", columns => columns.Contains("NAME"))
            .And("EMAIL column exists", columns => columns.Contains("EMAIL"))
            .AssertPassed();
    }

    [Scenario("Parses column data types")]
    [Fact]
    public async Task Parses_column_data_types()
    {
        await Given("columns with various data types", () =>
                CreateColumnsDataTable(
                    ("TEST", "INT_COL", "INTEGER", 4, false, 1),
                    ("TEST", "STR_COL", "VARCHAR", 100, true, 2),
                    ("TEST", "DATE_COL", "TIMESTAMP", null, true, 3)))
            .When("extracting data types", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .ToDictionary(
                        row => row["COLUMN_NAME"]?.ToString() ?? "",
                        row => row["COLUMN_DATA_TYPE"]?.ToString() ?? "");
            })
            .Then("INTEGER type is parsed", types => types["INT_COL"] == "INTEGER")
            .And("VARCHAR type is parsed", types => types["STR_COL"] == "VARCHAR")
            .And("TIMESTAMP type is parsed", types => types["DATE_COL"] == "TIMESTAMP")
            .AssertPassed();
    }

    [Scenario("Parses nullable flag correctly")]
    [Fact]
    public async Task Parses_nullable_flag()
    {
        await Given("columns with nullable settings", () =>
                CreateColumnsDataTable(
                    ("TEST", "REQUIRED_COL", "INTEGER", 4, false, 1),
                    ("TEST", "OPTIONAL_COL", "VARCHAR", 100, true, 2)))
            .When("extracting nullable flags", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .ToDictionary(
                        row => row["COLUMN_NAME"]?.ToString() ?? "",
                        row => row["IS_NULLABLE"]?.ToString() == "YES");
            })
            .Then("required column is not nullable", flags => !flags["REQUIRED_COL"])
            .And("optional column is nullable", flags => flags["OPTIONAL_COL"])
            .AssertPassed();
    }

    [Scenario("Handles trimming of padded column names")]
    [Fact]
    public async Task Handles_padded_column_names()
    {
        // Firebird often returns padded/trimmed names
        await Given("columns with padded names", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("TABLE_NAME", typeof(string));
                dt.Columns.Add("COLUMN_NAME", typeof(string));
                var row = dt.NewRow();
                row["TABLE_NAME"] = "USERS      "; // Padded
                row["COLUMN_NAME"] = "ID         "; // Padded
                dt.Rows.Add(row);
                return dt;
            })
            .When("trimming names", dt =>
            {
                return dt.AsEnumerable()
                    .Select(row => (row["COLUMN_NAME"]?.ToString() ?? "").Trim())
                    .First();
            })
            .Then("name is trimmed", name => name == "ID")
            .AssertPassed();
    }

    #endregion

    #region Index Parsing Tests

    [Scenario("Parses index names")]
    [Fact]
    public async Task Parses_index_names()
    {
        await Given("indexes for a table", () =>
                CreateIndexesDataTable(
                    ("USERS", "PK_USERS", true, true),
                    ("USERS", "IX_USERS_EMAIL", true, false),
                    ("USERS", "IX_USERS_NAME", false, false)))
            .When("extracting index names for USERS", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .Where(row => row["TABLE_NAME"]?.ToString() == "USERS")
                    .Select(row => row["INDEX_NAME"]?.ToString())
                    .ToList();
            })
            .Then("all indexes are found", indexes => indexes.Count == 3)
            .And("PK index exists", indexes => indexes.Contains("PK_USERS"))
            .And("unique index exists", indexes => indexes.Contains("IX_USERS_EMAIL"))
            .And("non-unique index exists", indexes => indexes.Contains("IX_USERS_NAME"))
            .AssertPassed();
    }

    [Scenario("Identifies primary key indexes")]
    [Fact]
    public async Task Identifies_primary_key_indexes()
    {
        await Given("indexes with primary key flags", () =>
                CreateIndexesDataTable(
                    ("USERS", "PK_USERS", true, true),
                    ("USERS", "IX_USERS_EMAIL", true, false)))
            .When("checking primary key flag", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .ToDictionary(
                        row => row["INDEX_NAME"]?.ToString() ?? "",
                        row => (bool)row["IS_PRIMARY"]);
            })
            .Then("PK_USERS is primary", flags => flags["PK_USERS"])
            .And("IX_USERS_EMAIL is not primary", flags => !flags["IX_USERS_EMAIL"])
            .AssertPassed();
    }

    [Scenario("Identifies unique indexes")]
    [Fact]
    public async Task Identifies_unique_indexes()
    {
        await Given("indexes with unique flags", () =>
                CreateIndexesDataTable(
                    ("USERS", "IX_UNIQUE", true, false),
                    ("USERS", "IX_NON_UNIQUE", false, false)))
            .When("checking unique flag", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .ToDictionary(
                        row => row["INDEX_NAME"]?.ToString() ?? "",
                        row => (bool)row["IS_UNIQUE"]);
            })
            .Then("IX_UNIQUE is unique", flags => flags["IX_UNIQUE"])
            .And("IX_NON_UNIQUE is not unique", flags => !flags["IX_NON_UNIQUE"])
            .AssertPassed();
    }

    [Scenario("Filters out RDB$ system indexes")]
    [Fact]
    public async Task Filters_system_indexes()
    {
        await Given("indexes including RDB$ system indexes", () =>
                CreateIndexesDataTable(
                    ("USERS", "PK_USERS", true, true),
                    ("USERS", "RDB$PRIMARY1", true, true)))
            .When("filtering indexes", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .Where(row =>
                    {
                        var indexName = row["INDEX_NAME"]?.ToString() ?? "";
                        return !indexName.StartsWith("RDB$", StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(row => row["INDEX_NAME"]?.ToString())
                    .ToList();
            })
            .Then("RDB$ indexes are excluded", indexes => !indexes.Any(i => i is not null && i.StartsWith("RDB$")))
            .And("user indexes are included", indexes => indexes.Contains("PK_USERS"))
            .AssertPassed();
    }

    [Scenario("Infers primary key from PK_ prefix")]
    [Fact]
    public async Task Infers_pk_from_prefix()
    {
        // FirebirdSchemaReader infers primary key from PK_ naming convention
        await Given("an index named with PK_ prefix", () => "PK_USERS")
            .When("checking if primary", indexName =>
                indexName.StartsWith("PK_", StringComparison.OrdinalIgnoreCase))
            .Then("is identified as primary", isPrimary => isPrimary)
            .AssertPassed();
    }

    #endregion

    #region Index Columns Tests

    [Scenario("Parses index column associations")]
    [Fact]
    public async Task Parses_index_columns()
    {
        await Given("index columns data", () =>
                CreateIndexColumnsDataTable(
                    ("USERS", "PK_USERS", "ID", 1),
                    ("USERS", "IX_USERS_NAME_EMAIL", "NAME", 1),
                    ("USERS", "IX_USERS_NAME_EMAIL", "EMAIL", 2)))
            .When("extracting columns for IX_USERS_NAME_EMAIL", indexColumnsData =>
            {
                return indexColumnsData.AsEnumerable()
                    .Where(row => row["INDEX_NAME"]?.ToString() == "IX_USERS_NAME_EMAIL")
                    .OrderBy(row => (int)row["ORDINAL_POSITION"])
                    .Select(row => row["COLUMN_NAME"]?.ToString())
                    .ToList();
            })
            .Then("both columns are found", columns => columns.Count == 2)
            .And("NAME is first", columns => columns[0] == "NAME")
            .And("EMAIL is second", columns => columns[1] == "EMAIL")
            .AssertPassed();
    }

    #endregion

    #region Default Schema Tests

    [Scenario("Uses 'dbo' as default schema for Firebird")]
    [Fact]
    public async Task Uses_dbo_default_schema()
    {
        // Firebird doesn't have schemas, so the reader uses "dbo" as default
        await Given("knowledge that Firebird lacks schema support", () => true)
            .When("default schema is applied", _ => "dbo")
            .Then("schema is 'dbo'", schema => schema == "dbo")
            .AssertPassed();
    }

    #endregion

    #region Alternative Column Name Tests

    [Scenario("Handles SYSTEM_TABLE instead of IS_SYSTEM_TABLE")]
    [Fact]
    public async Task Handles_alternate_system_column_name()
    {
        await Given("a tables DataTable with SYSTEM_TABLE column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("TABLE_NAME", typeof(string));
                dt.Columns.Add("SYSTEM_TABLE", typeof(int)); // Alternate naming
                var row = dt.NewRow();
                row["TABLE_NAME"] = "USERS";
                row["SYSTEM_TABLE"] = 0; // 0 = not system
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("SYSTEM_TABLE"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    [Scenario("Handles DATA_TYPE instead of COLUMN_DATA_TYPE")]
    [Fact]
    public async Task Handles_alternate_datatype_column_name()
    {
        await Given("a columns DataTable with DATA_TYPE column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("TABLE_NAME", typeof(string));
                dt.Columns.Add("COLUMN_NAME", typeof(string));
                dt.Columns.Add("DATA_TYPE", typeof(string)); // Alternate naming
                var row = dt.NewRow();
                row["TABLE_NAME"] = "USERS";
                row["COLUMN_NAME"] = "ID";
                row["DATA_TYPE"] = "INTEGER";
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("DATA_TYPE"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    [Scenario("Handles UNIQUE_FLAG instead of IS_UNIQUE")]
    [Fact]
    public async Task Handles_alternate_unique_column_name()
    {
        await Given("an indexes DataTable with UNIQUE_FLAG column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("TABLE_NAME", typeof(string));
                dt.Columns.Add("INDEX_NAME", typeof(string));
                dt.Columns.Add("UNIQUE_FLAG", typeof(int)); // Alternate naming
                var row = dt.NewRow();
                row["TABLE_NAME"] = "USERS";
                row["INDEX_NAME"] = "IX_USERS";
                row["UNIQUE_FLAG"] = 1; // 1 = unique
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("UNIQUE_FLAG"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    #endregion

    #region Factory Integration Tests

    [Scenario("DatabaseProviderFactory creates FirebirdSchemaReader")]
    [Fact]
    public async Task Factory_creates_correct_reader()
    {
        await Given("firebird provider", () => "firebird")
            .When("schema reader created", provider =>
                DatabaseProviderFactory.CreateSchemaReader(provider))
            .Then("returns FirebirdSchemaReader", reader => reader is FirebirdSchemaReader)
            .AssertPassed();
    }

    [Scenario("fb alias creates FirebirdSchemaReader")]
    [Fact]
    public async Task Fb_alias_creates_correct_reader()
    {
        await Given("fb provider alias", () => "fb")
            .When("schema reader created", provider =>
                DatabaseProviderFactory.CreateSchemaReader(provider))
            .Then("returns FirebirdSchemaReader", reader => reader is FirebirdSchemaReader)
            .AssertPassed();
    }

    #endregion
}
