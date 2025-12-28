using System.Data;
using JD.Efcpt.Build.Tasks.Extensions;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the DataRowExtensions class.
/// </summary>
[Feature("DataRowExtensions: Provides safe access to DataRow values")]
[Collection(nameof(AssemblySetup))]
public sealed class DataRowExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static DataTable CreateTestTable()
    {
        var table = new DataTable();
        table.Columns.Add("StringColumn", typeof(string));
        table.Columns.Add("IntColumn", typeof(int));
        table.Columns.Add("NullableColumn", typeof(string));
        return table;
    }

    [Scenario("Returns string value for string column")]
    [Fact]
    public async Task Returns_string_value_for_string_column()
    {
        await Given("a DataRow with string value", () =>
            {
                var table = CreateTestTable();
                var row = table.NewRow();
                row["StringColumn"] = "Hello World";
                table.Rows.Add(row);
                return row;
            })
            .When("getting string value", row => row.GetString("StringColumn"))
            .Then("returns the string", result => result == "Hello World")
            .AssertPassed();
    }

    [Scenario("Returns empty string for DBNull value")]
    [Fact]
    public async Task Returns_empty_string_for_dbnull()
    {
        await Given("a DataRow with DBNull value", () =>
            {
                var table = CreateTestTable();
                var row = table.NewRow();
                row["NullableColumn"] = DBNull.Value;
                table.Rows.Add(row);
                return row;
            })
            .When("getting string value", row => row.GetString("NullableColumn"))
            .Then("returns empty string", result => result == string.Empty)
            .AssertPassed();
    }

    [Scenario("Converts non-string value to string")]
    [Fact]
    public async Task Converts_non_string_value_to_string()
    {
        await Given("a DataRow with integer value", () =>
            {
                var table = CreateTestTable();
                var row = table.NewRow();
                row["IntColumn"] = 42;
                table.Rows.Add(row);
                return row;
            })
            .When("getting string value", row => row.GetString("IntColumn"))
            .Then("returns converted string", result => result == "42")
            .AssertPassed();
    }

    [Scenario("Throws ArgumentNullException for null row")]
    [Fact]
    public async Task Throws_for_null_row()
    {
        await Given("a null DataRow", () => (DataRow)null!)
            .When("getting string value", row =>
            {
                try
                {
                    row.GetString("Column");
                    return "no exception";
                }
                catch (ArgumentNullException)
                {
                    return "ArgumentNullException";
                }
            })
            .Then("throws ArgumentNullException", result => result == "ArgumentNullException")
            .AssertPassed();
    }

    [Scenario("Throws ArgumentException for null column name")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Throws_for_invalid_column_name(string? columnName)
    {
        await Given("a DataRow with valid data", () =>
            {
                var table = CreateTestTable();
                var row = table.NewRow();
                row["StringColumn"] = "Test";
                table.Rows.Add(row);
                return row;
            })
            .When("getting value with invalid column name", row =>
            {
                try
                {
                    row.GetString(columnName!);
                    return "no exception";
                }
                catch (ArgumentException)
                {
                    return "ArgumentException";
                }
            })
            .Then("throws ArgumentException", result => result == "ArgumentException")
            .AssertPassed();
    }

    [Scenario("Throws ArgumentOutOfRangeException for non-existent column")]
    [Fact]
    public async Task Throws_for_non_existent_column()
    {
        await Given("a DataRow", () =>
            {
                var table = CreateTestTable();
                var row = table.NewRow();
                table.Rows.Add(row);
                return row;
            })
            .When("getting value for non-existent column", row =>
            {
                try
                {
                    row.GetString("NonExistentColumn");
                    return "no exception";
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return ex.Message.Contains("NonExistentColumn") ? "ArgumentOutOfRangeException" : "wrong message";
                }
            })
            .Then("throws ArgumentOutOfRangeException", result => result == "ArgumentOutOfRangeException")
            .AssertPassed();
    }

    [Scenario("Handles empty string value correctly")]
    [Fact]
    public async Task Handles_empty_string_value()
    {
        await Given("a DataRow with empty string value", () =>
            {
                var table = CreateTestTable();
                var row = table.NewRow();
                row["StringColumn"] = string.Empty;
                table.Rows.Add(row);
                return row;
            })
            .When("getting string value", row => row.GetString("StringColumn"))
            .Then("returns empty string", result => result == string.Empty)
            .AssertPassed();
    }
}
