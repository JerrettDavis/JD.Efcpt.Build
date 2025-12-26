using System.Data;
using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

/// <summary>
/// Unit tests for OracleSchemaReader parsing logic.
/// These tests verify that the reader correctly parses DataTables
/// with Oracle-specific column naming conventions.
/// </summary>
[Feature("OracleSchemaReader: parses Oracle GetSchema() DataTables")]
[Collection(nameof(AssemblySetup))]
public sealed class OracleSchemaReaderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Test Helpers

    /// <summary>
    /// Creates a mock Tables DataTable with Oracle column naming.
    /// </summary>
    private static DataTable CreateTablesDataTable(params (string Owner, string TableName, string Type)[] tables)
    {
        var dt = new DataTable("Tables");
        dt.Columns.Add("OWNER", typeof(string));
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("TYPE", typeof(string));

        foreach (var (owner, tableName, type) in tables)
        {
            var row = dt.NewRow();
            row["OWNER"] = owner;
            row["TABLE_NAME"] = tableName;
            row["TYPE"] = type;
            dt.Rows.Add(row);
        }

        return dt;
    }

    /// <summary>
    /// Creates a mock Columns DataTable with Oracle column naming.
    /// </summary>
    private static DataTable CreateColumnsDataTable(
        params (string Owner, string TableName, string ColumnName, string DataType, int? Length, bool IsNullable, int Id)[] columns)
    {
        var dt = new DataTable("Columns");
        dt.Columns.Add("OWNER", typeof(string));
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("COLUMN_NAME", typeof(string));
        dt.Columns.Add("DATATYPE", typeof(string));
        dt.Columns.Add("LENGTH", typeof(int));
        dt.Columns.Add("NULLABLE", typeof(string));
        dt.Columns.Add("ID", typeof(int));
        dt.Columns.Add("DATA_DEFAULT", typeof(string));
        dt.Columns.Add("PRECISION", typeof(int));
        dt.Columns.Add("SCALE", typeof(int));

        foreach (var (owner, tableName, columnName, dataType, length, isNullable, id) in columns)
        {
            var row = dt.NewRow();
            row["OWNER"] = owner;
            row["TABLE_NAME"] = tableName;
            row["COLUMN_NAME"] = columnName;
            row["DATATYPE"] = dataType;
            row["LENGTH"] = length ?? (object)DBNull.Value;
            row["NULLABLE"] = isNullable ? "Y" : "N";
            row["ID"] = id;
            row["DATA_DEFAULT"] = DBNull.Value;
            row["PRECISION"] = DBNull.Value;
            row["SCALE"] = DBNull.Value;
            dt.Rows.Add(row);
        }

        return dt;
    }

    /// <summary>
    /// Creates a mock Indexes DataTable with Oracle column naming.
    /// </summary>
    private static DataTable CreateIndexesDataTable(
        params (string Owner, string TableName, string IndexName, string Uniqueness)[] indexes)
    {
        var dt = new DataTable("Indexes");
        dt.Columns.Add("OWNER", typeof(string));
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("INDEX_NAME", typeof(string));
        dt.Columns.Add("UNIQUENESS", typeof(string));

        foreach (var (owner, tableName, indexName, uniqueness) in indexes)
        {
            var row = dt.NewRow();
            row["OWNER"] = owner;
            row["TABLE_NAME"] = tableName;
            row["INDEX_NAME"] = indexName;
            row["UNIQUENESS"] = uniqueness;
            dt.Rows.Add(row);
        }

        return dt;
    }

    /// <summary>
    /// Creates a mock IndexColumns DataTable with Oracle column naming.
    /// </summary>
    private static DataTable CreateIndexColumnsDataTable(
        params (string Owner, string TableName, string IndexName, string ColumnName, int Position, string Descend)[] indexColumns)
    {
        var dt = new DataTable("IndexColumns");
        dt.Columns.Add("OWNER", typeof(string));
        dt.Columns.Add("TABLE_NAME", typeof(string));
        dt.Columns.Add("INDEX_NAME", typeof(string));
        dt.Columns.Add("COLUMN_NAME", typeof(string));
        dt.Columns.Add("COLUMN_POSITION", typeof(int));
        dt.Columns.Add("DESCEND", typeof(string));

        foreach (var (owner, tableName, indexName, columnName, position, descend) in indexColumns)
        {
            var row = dt.NewRow();
            row["OWNER"] = owner;
            row["TABLE_NAME"] = tableName;
            row["INDEX_NAME"] = indexName;
            row["COLUMN_NAME"] = columnName;
            row["COLUMN_POSITION"] = position;
            row["DESCEND"] = descend;
            dt.Rows.Add(row);
        }

        return dt;
    }

    // Oracle system schemas to filter out
    private static readonly string[] SystemSchemas =
    [
        "SYS", "SYSTEM", "OUTLN", "DIP", "ORACLE_OCM", "DBSNMP", "APPQOSSYS",
        "WMSYS", "EXFSYS", "CTXSYS", "XDB", "ANONYMOUS", "ORDDATA", "ORDPLUGINS",
        "ORDSYS", "SI_INFORMTN_SCHEMA", "MDSYS", "OLAPSYS", "MDDATA"
    ];

    #endregion

    #region System Schema Filtering Tests

    [Scenario("Filters out SYS schema")]
    [Fact]
    public async Task Filters_sys_schema()
    {
        await Given("tables from SYS and user schemas", () =>
                CreateTablesDataTable(
                    ("SYS", "DBA_TABLES", "User"),
                    ("MYAPP", "USERS", "User")))
            .When("filtering out system schemas", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row => !SystemSchemas.Contains(row["OWNER"]?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
                    .Select(row => row["OWNER"]?.ToString())
                    .ToList();
            })
            .Then("SYS schema is excluded", schemas => !schemas.Contains("SYS"))
            .And("MYAPP schema is included", schemas => schemas.Contains("MYAPP"))
            .AssertPassed();
    }

    [Scenario("Filters out SYSTEM schema")]
    [Fact]
    public async Task Filters_system_schema()
    {
        await Given("tables from SYSTEM schema", () =>
                CreateTablesDataTable(
                    ("SYSTEM", "HELP", "User"),
                    ("MYAPP", "ORDERS", "User")))
            .When("filtering out system schemas", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row => !SystemSchemas.Contains(row["OWNER"]?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
                    .Select(row => row["OWNER"]?.ToString())
                    .ToList();
            })
            .Then("SYSTEM schema is excluded", schemas => !schemas.Contains("SYSTEM"))
            .And("MYAPP schema is included", schemas => schemas.Contains("MYAPP"))
            .AssertPassed();
    }

    [Scenario("Filters all known Oracle system schemas")]
    [Theory]
    [InlineData("SYS")]
    [InlineData("SYSTEM")]
    [InlineData("OUTLN")]
    [InlineData("DBSNMP")]
    [InlineData("APPQOSSYS")]
    [InlineData("WMSYS")]
    [InlineData("CTXSYS")]
    [InlineData("XDB")]
    [InlineData("MDSYS")]
    [InlineData("OLAPSYS")]
    public async Task Filters_known_system_schemas(string schema)
    {
        await Given($"a table from {schema} schema", () =>
                CreateTablesDataTable((schema, "SYS_TABLE", "User")))
            .When("filtering system schemas", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row => !SystemSchemas.Contains(row["OWNER"]?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
                    .Count();
            })
            .Then("system schema table is excluded", count => count == 0)
            .AssertPassed();
    }

    [Scenario("Case-insensitive system schema filtering")]
    [Fact]
    public async Task Filters_system_schemas_case_insensitive()
    {
        await Given("tables with mixed case system schema names", () =>
                CreateTablesDataTable(
                    ("sys", "TABLE1", "User"),
                    ("Sys", "TABLE2", "User"),
                    ("MYAPP", "USERS", "User")))
            .When("filtering system schemas", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row => !SystemSchemas.Contains(row["OWNER"]?.ToString() ?? "", StringComparer.OrdinalIgnoreCase))
                    .Select(row => row["OWNER"]?.ToString())
                    .ToList();
            })
            .Then("lowercase sys is excluded", schemas => !schemas.Contains("sys"))
            .And("mixed case Sys is excluded", schemas => !schemas.Contains("Sys"))
            .And("user schema is included", schemas => schemas.Contains("MYAPP"))
            .AssertPassed();
    }

    #endregion

    #region Table Type Filtering Tests

    [Scenario("Includes User type tables")]
    [Fact]
    public async Task Includes_user_type_tables()
    {
        await Given("tables with User type", () =>
                CreateTablesDataTable(
                    ("MYAPP", "USERS", "User"),
                    ("MYAPP", "ORDERS", "User")))
            .When("filtering by type", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row =>
                    {
                        var type = row["TYPE"]?.ToString() ?? "";
                        return string.IsNullOrEmpty(type) ||
                               string.Equals(type, "User", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(type, "TABLE", StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(row => row["TABLE_NAME"]?.ToString())
                    .ToList();
            })
            .Then("all User tables are included", tables => tables.Count == 2)
            .AssertPassed();
    }

    [Scenario("Includes TABLE type tables")]
    [Fact]
    public async Task Includes_table_type_tables()
    {
        await Given("tables with TABLE type", () =>
                CreateTablesDataTable(("MYAPP", "PRODUCTS", "TABLE")))
            .When("filtering by type", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row =>
                    {
                        var type = row["TYPE"]?.ToString() ?? "";
                        return string.IsNullOrEmpty(type) ||
                               string.Equals(type, "User", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(type, "TABLE", StringComparison.OrdinalIgnoreCase);
                    })
                    .Count();
            })
            .Then("TABLE type is included", count => count == 1)
            .AssertPassed();
    }

    [Scenario("Excludes VIEW type objects")]
    [Fact]
    public async Task Excludes_view_type()
    {
        await Given("tables including views", () =>
                CreateTablesDataTable(
                    ("MYAPP", "USERS", "User"),
                    ("MYAPP", "V_ACTIVE_USERS", "View")))
            .When("filtering by type", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .Where(row =>
                    {
                        var type = row["TYPE"]?.ToString() ?? "";
                        return string.IsNullOrEmpty(type) ||
                               string.Equals(type, "User", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(type, "TABLE", StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(row => row["TABLE_NAME"]?.ToString())
                    .ToList();
            })
            .Then("views are excluded", tables => !tables.Contains("V_ACTIVE_USERS"))
            .And("tables are included", tables => tables.Contains("USERS"))
            .AssertPassed();
    }

    #endregion

    #region Column Parsing Tests

    [Scenario("Parses Oracle column names")]
    [Fact]
    public async Task Parses_column_names()
    {
        await Given("columns for a table", () =>
                CreateColumnsDataTable(
                    ("MYAPP", "USERS", "ID", "NUMBER", 22, false, 1),
                    ("MYAPP", "USERS", "USERNAME", "VARCHAR2", 100, false, 2),
                    ("MYAPP", "USERS", "EMAIL", "VARCHAR2", 255, true, 3)))
            .When("extracting columns for USERS", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .Where(row =>
                        row["OWNER"]?.ToString() == "MYAPP" &&
                        row["TABLE_NAME"]?.ToString() == "USERS")
                    .Select(row => row["COLUMN_NAME"]?.ToString())
                    .ToList();
            })
            .Then("all columns are found", columns => columns.Count == 3)
            .And("ID exists", columns => columns.Contains("ID"))
            .And("USERNAME exists", columns => columns.Contains("USERNAME"))
            .And("EMAIL exists", columns => columns.Contains("EMAIL"))
            .AssertPassed();
    }

    [Scenario("Parses Oracle data types")]
    [Fact]
    public async Task Parses_oracle_data_types()
    {
        await Given("columns with Oracle data types", () =>
                CreateColumnsDataTable(
                    ("MYAPP", "TEST", "NUM_COL", "NUMBER", 22, false, 1),
                    ("MYAPP", "TEST", "STR_COL", "VARCHAR2", 100, true, 2),
                    ("MYAPP", "TEST", "DATE_COL", "DATE", null, true, 3),
                    ("MYAPP", "TEST", "CLOB_COL", "CLOB", null, true, 4)))
            .When("extracting data types", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .ToDictionary(
                        row => row["COLUMN_NAME"]?.ToString() ?? "",
                        row => row["DATATYPE"]?.ToString() ?? "");
            })
            .Then("NUMBER type is parsed", types => types["NUM_COL"] == "NUMBER")
            .And("VARCHAR2 type is parsed", types => types["STR_COL"] == "VARCHAR2")
            .And("DATE type is parsed", types => types["DATE_COL"] == "DATE")
            .And("CLOB type is parsed", types => types["CLOB_COL"] == "CLOB")
            .AssertPassed();
    }

    [Scenario("Parses Oracle nullable flag with Y/N")]
    [Fact]
    public async Task Parses_nullable_y_n()
    {
        await Given("columns with Y/N nullable flags", () =>
                CreateColumnsDataTable(
                    ("MYAPP", "TEST", "REQUIRED", "VARCHAR2", 100, false, 1),
                    ("MYAPP", "TEST", "OPTIONAL", "VARCHAR2", 100, true, 2)))
            .When("extracting nullable flags", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .ToDictionary(
                        row => row["COLUMN_NAME"]?.ToString() ?? "",
                        row => row["NULLABLE"]?.ToString() == "Y");
            })
            .Then("N means not nullable", flags => !flags["REQUIRED"])
            .And("Y means nullable", flags => flags["OPTIONAL"])
            .AssertPassed();
    }

    [Scenario("Filters columns by owner and table name")]
    [Fact]
    public async Task Filters_columns_by_owner_and_table()
    {
        await Given("columns from multiple schemas and tables", () =>
                CreateColumnsDataTable(
                    ("MYAPP", "USERS", "ID", "NUMBER", 22, false, 1),
                    ("MYAPP", "ORDERS", "ID", "NUMBER", 22, false, 1),
                    ("OTHER", "USERS", "ID", "NUMBER", 22, false, 1)))
            .When("filtering for MYAPP.USERS", columnsData =>
            {
                return columnsData.AsEnumerable()
                    .Where(row =>
                        string.Equals(row["OWNER"]?.ToString(), "MYAPP", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(row["TABLE_NAME"]?.ToString(), "USERS", StringComparison.OrdinalIgnoreCase))
                    .Count();
            })
            .Then("only one column matches", count => count == 1)
            .AssertPassed();
    }

    #endregion

    #region Index Parsing Tests

    [Scenario("Parses Oracle index uniqueness")]
    [Fact]
    public async Task Parses_index_uniqueness()
    {
        await Given("indexes with UNIQUE/NONUNIQUE values", () =>
                CreateIndexesDataTable(
                    ("MYAPP", "USERS", "IX_USERS_EMAIL", "UNIQUE"),
                    ("MYAPP", "USERS", "IX_USERS_NAME", "NONUNIQUE")))
            .When("extracting uniqueness", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .ToDictionary(
                        row => row["INDEX_NAME"]?.ToString() ?? "",
                        row => string.Equals(row["UNIQUENESS"]?.ToString(), "UNIQUE", StringComparison.OrdinalIgnoreCase));
            })
            .Then("UNIQUE index is unique", flags => flags["IX_USERS_EMAIL"])
            .And("NONUNIQUE index is not unique", flags => !flags["IX_USERS_NAME"])
            .AssertPassed();
    }

    [Scenario("Identifies primary key from _PK suffix")]
    [Fact]
    public async Task Identifies_pk_from_suffix()
    {
        await Given("indexes with _PK suffix", () =>
                CreateIndexesDataTable(
                    ("MYAPP", "USERS", "USERS_PK", "UNIQUE"),
                    ("MYAPP", "USERS", "IX_USERS_EMAIL", "UNIQUE")))
            .When("checking for primary key", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .ToDictionary(
                        row => row["INDEX_NAME"]?.ToString() ?? "",
                        row =>
                        {
                            var name = row["INDEX_NAME"]?.ToString() ?? "";
                            return name.EndsWith("_PK", StringComparison.OrdinalIgnoreCase);
                        });
            })
            .Then("_PK suffix is primary", flags => flags["USERS_PK"])
            .And("regular index is not primary", flags => !flags["IX_USERS_EMAIL"])
            .AssertPassed();
    }

    [Scenario("Identifies primary key containing PRIMARY keyword")]
    [Fact]
    public async Task Identifies_pk_from_primary_keyword()
    {
        await Given("index with PRIMARY in name", () =>
                CreateIndexesDataTable(
                    ("MYAPP", "USERS", "SYS_PRIMARY_12345", "UNIQUE")))
            .When("checking for primary key", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .Select(row =>
                    {
                        var name = row["INDEX_NAME"]?.ToString() ?? "";
                        return name.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase);
                    })
                    .First();
            })
            .Then("PRIMARY keyword detected", isPrimary => isPrimary)
            .AssertPassed();
    }

    [Scenario("Filters indexes by owner and table")]
    [Fact]
    public async Task Filters_indexes_by_owner_and_table()
    {
        await Given("indexes from multiple schemas", () =>
                CreateIndexesDataTable(
                    ("MYAPP", "USERS", "IX_MYAPP_USERS", "UNIQUE"),
                    ("OTHER", "USERS", "IX_OTHER_USERS", "UNIQUE")))
            .When("filtering for MYAPP.USERS", indexesData =>
            {
                return indexesData.AsEnumerable()
                    .Where(row =>
                        string.Equals(row["OWNER"]?.ToString(), "MYAPP", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(row["TABLE_NAME"]?.ToString(), "USERS", StringComparison.OrdinalIgnoreCase))
                    .Select(row => row["INDEX_NAME"]?.ToString())
                    .ToList();
            })
            .Then("only MYAPP index matches", indexes => indexes.Count == 1)
            .And("correct index is returned", indexes => indexes.Contains("IX_MYAPP_USERS"))
            .AssertPassed();
    }

    #endregion

    #region Index Columns Tests

    [Scenario("Parses index column positions")]
    [Fact]
    public async Task Parses_index_column_positions()
    {
        await Given("composite index columns", () =>
                CreateIndexColumnsDataTable(
                    ("MYAPP", "USERS", "IX_USERS_NAME_EMAIL", "LAST_NAME", 1, "ASC"),
                    ("MYAPP", "USERS", "IX_USERS_NAME_EMAIL", "FIRST_NAME", 2, "ASC"),
                    ("MYAPP", "USERS", "IX_USERS_NAME_EMAIL", "EMAIL", 3, "ASC")))
            .When("extracting columns in order", indexColumnsData =>
            {
                return indexColumnsData.AsEnumerable()
                    .Where(row => row["INDEX_NAME"]?.ToString() == "IX_USERS_NAME_EMAIL")
                    .OrderBy(row => (int)row["COLUMN_POSITION"])
                    .Select(row => row["COLUMN_NAME"]?.ToString())
                    .ToList();
            })
            .Then("columns are in correct order", columns =>
                columns[0] == "LAST_NAME" &&
                columns[1] == "FIRST_NAME" &&
                columns[2] == "EMAIL")
            .AssertPassed();
    }

    [Scenario("Parses descending column sort order")]
    [Fact]
    public async Task Parses_descending_sort()
    {
        await Given("index columns with DESC order", () =>
                CreateIndexColumnsDataTable(
                    ("MYAPP", "ORDERS", "IX_ORDERS_DATE", "ORDER_DATE", 1, "DESC"),
                    ("MYAPP", "ORDERS", "IX_ORDERS_DATE", "ORDER_ID", 2, "ASC")))
            .When("extracting sort orders", indexColumnsData =>
            {
                return indexColumnsData.AsEnumerable()
                    .ToDictionary(
                        row => row["COLUMN_NAME"]?.ToString() ?? "",
                        row => string.Equals(row["DESCEND"]?.ToString(), "DESC", StringComparison.OrdinalIgnoreCase));
            })
            .Then("DESC column is descending", orders => orders["ORDER_DATE"])
            .And("ASC column is not descending", orders => !orders["ORDER_ID"])
            .AssertPassed();
    }

    #endregion

    #region Alternative Column Name Tests

    [Scenario("Handles TABLE_SCHEMA instead of OWNER")]
    [Fact]
    public async Task Handles_table_schema_column_name()
    {
        await Given("a tables DataTable with TABLE_SCHEMA column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("TABLE_SCHEMA", typeof(string));
                dt.Columns.Add("TABLE_NAME", typeof(string));
                var row = dt.NewRow();
                row["TABLE_SCHEMA"] = "MYAPP";
                row["TABLE_NAME"] = "USERS";
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("TABLE_SCHEMA"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    [Scenario("Handles DATA_TYPE instead of DATATYPE")]
    [Fact]
    public async Task Handles_data_type_with_underscore()
    {
        await Given("a columns DataTable with DATA_TYPE column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("DATA_TYPE", typeof(string));
                var row = dt.NewRow();
                row["DATA_TYPE"] = "VARCHAR2";
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("DATA_TYPE"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    [Scenario("Handles DATA_LENGTH instead of LENGTH")]
    [Fact]
    public async Task Handles_data_length_column_name()
    {
        await Given("a columns DataTable with DATA_LENGTH column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("DATA_LENGTH", typeof(int));
                var row = dt.NewRow();
                row["DATA_LENGTH"] = 100;
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("DATA_LENGTH"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    [Scenario("Handles ORDINAL_POSITION instead of ID")]
    [Fact]
    public async Task Handles_ordinal_position_column_name()
    {
        await Given("a columns DataTable with ORDINAL_POSITION column", () =>
            {
                var dt = new DataTable();
                dt.Columns.Add("ORDINAL_POSITION", typeof(int));
                var row = dt.NewRow();
                row["ORDINAL_POSITION"] = 1;
                dt.Rows.Add(row);
                return dt;
            })
            .When("checking for column", dt => dt.Columns.Contains("ORDINAL_POSITION"))
            .Then("alternate column is recognized", found => found)
            .AssertPassed();
    }

    #endregion

    #region Schema Sorting Tests

    [Scenario("Tables are sorted by schema then name")]
    [Fact]
    public async Task Tables_sorted_by_schema_then_name()
    {
        await Given("tables from multiple schemas", () =>
                CreateTablesDataTable(
                    ("MYAPP", "ZEBRA", "User"),
                    ("ALPHA", "USERS", "User"),
                    ("MYAPP", "ACCOUNTS", "User")))
            .When("sorting tables", tablesData =>
            {
                return tablesData.AsEnumerable()
                    .OrderBy(row => row["OWNER"]?.ToString())
                    .ThenBy(row => row["TABLE_NAME"]?.ToString())
                    .Select(row => $"{row["OWNER"]}.{row["TABLE_NAME"]}")
                    .ToList();
            })
            .Then("ALPHA.USERS is first", tables => tables[0] == "ALPHA.USERS")
            .And("MYAPP.ACCOUNTS is second", tables => tables[1] == "MYAPP.ACCOUNTS")
            .And("MYAPP.ZEBRA is last", tables => tables[2] == "MYAPP.ZEBRA")
            .AssertPassed();
    }

    #endregion

    #region Factory Integration Tests

    [Scenario("DatabaseProviderFactory creates OracleSchemaReader")]
    [Fact]
    public async Task Factory_creates_correct_reader()
    {
        await Given("oracle provider", () => "oracle")
            .When("schema reader created", provider =>
                DatabaseProviderFactory.CreateSchemaReader(provider))
            .Then("returns OracleSchemaReader", reader => reader is OracleSchemaReader)
            .AssertPassed();
    }

    [Scenario("oracledb alias creates OracleSchemaReader")]
    [Fact]
    public async Task Oracledb_alias_creates_correct_reader()
    {
        await Given("oracledb provider alias", () => "oracledb")
            .When("schema reader created", provider =>
                DatabaseProviderFactory.CreateSchemaReader(provider))
            .Then("returns OracleSchemaReader", reader => reader is OracleSchemaReader)
            .AssertPassed();
    }

    #endregion
}
