using JD.Efcpt.Build.Tasks.Utilities;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the SqlContentNormalizer utility.
/// </summary>
[Feature("SqlContentNormalizer: Whitespace and comment normalization for SQL fingerprinting")]
[Collection(nameof(AssemblySetup))]
public sealed class SqlContentNormalizerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Normalizes multiple whitespace sequences to single spaces")]
    [Fact]
    public async Task Normalizes_whitespace_sequences()
    {
        await Given("SQL with extra whitespace", () => "SELECT    id,    name    FROM    users")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("whitespace is reduced to single spaces", normalized => normalized == "SELECT id, name FROM users")
            .AssertPassed();
    }

    [Scenario("Preserves string literal content including internal whitespace")]
    [Fact]
    public async Task Preserves_string_literal_whitespace()
    {
        await Given("SQL with string containing spaces", () => "INSERT INTO Products (Name) VALUES ('Product   With   Spaces')")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("string literal whitespace is preserved", normalized =>
                normalized.Contains("'Product   With   Spaces'"))
            .AssertPassed();
    }

    [Scenario("Removes line comments")]
    [Fact]
    public async Task Removes_line_comments()
    {
        await Given("SQL with line comments", () => @"
SELECT id -- This is a comment
FROM users -- Another comment
")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("comments are removed", normalized => !normalized.Contains("--"))
            .And("SQL structure remains", normalized => normalized.Contains("SELECT") && normalized.Contains("FROM"))
            .AssertPassed();
    }

    [Scenario("Removes block comments")]
    [Fact]
    public async Task Removes_block_comments()
    {
        await Given("SQL with block comments", () => @"
SELECT id /* multi-line
comment here */
FROM users
")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("block comment is removed", normalized => !normalized.Contains("/*") && !normalized.Contains("*/"))
            .And("SQL structure remains", normalized => normalized.Contains("SELECT") && normalized.Contains("FROM"))
            .AssertPassed();
    }

    [Scenario("Handles strings with escaped single quotes")]
    [Fact]
    public async Task Handles_escaped_quotes_in_strings()
    {
        await Given("SQL with escaped quotes", () => "INSERT INTO Test (Value) VALUES ('It''s working')")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("escaped quote is preserved", normalized => normalized.Contains("'It''s working'"))
            .AssertPassed();
    }

    [Scenario("Normalizes newlines and tabs to spaces")]
    [Fact]
    public async Task Normalizes_newlines_and_tabs()
    {
        await Given("SQL with newlines and tabs", () => "SELECT\tid,\n\tname\nFROM\tusers")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("newlines and tabs become spaces", normalized => normalized == "SELECT id, name FROM users")
            .AssertPassed();
    }

    [Scenario("Trims leading and trailing whitespace")]
    [Fact]
    public async Task Trims_leading_trailing_whitespace()
    {
        await Given("SQL with leading/trailing whitespace", () => "   SELECT id FROM users   ")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("leading/trailing whitespace is removed", normalized => normalized == "SELECT id FROM users")
            .AssertPassed();
    }

    [Scenario("Returns empty string for null or whitespace input")]
    [Fact]
    public async Task Returns_empty_for_null_or_whitespace()
    {
        await Given("null or whitespace inputs", () => new[] { null!, "", "   ", "\t\n" })
            .When("each is normalized", inputs => inputs.Select(i => SqlContentNormalizer.Normalize(i)).ToList())
            .Then("all return empty string", results => results.All(r => r == string.Empty))
            .AssertPassed();
    }

    [Scenario("Handles multiple string literals in one statement")]
    [Fact]
    public async Task Handles_multiple_string_literals()
    {
        await Given("SQL with multiple strings", () =>
            "INSERT INTO Test (Col1, Col2) VALUES ('First  Value', 'Second  Value')")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("both strings preserve internal whitespace", normalized =>
                normalized.Contains("'First  Value'") && normalized.Contains("'Second  Value'"))
            .AssertPassed();
    }

    [Scenario("Normalizes complex SQL with mixed content")]
    [Fact]
    public async Task Normalizes_complex_sql()
    {
        await Given("complex SQL statement", () => @"
-- Create table comment
CREATE TABLE  Users  (
    Id   INT   PRIMARY KEY, /* primary key */
    Name NVARCHAR(100)  DEFAULT  'Unknown  User'  -- default name
)
")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("result is normalized", normalized =>
            {
                // Comments removed
                var noComments = !normalized.Contains("--") && !normalized.Contains("/*");
                // Whitespace normalized except in string
                var hasNormalizedSpaces = normalized.Contains("CREATE TABLE Users");
                // String preserved
                var stringPreserved = normalized.Contains("'Unknown  User'");
                return noComments && hasNormalizedSpaces && stringPreserved;
            })
            .AssertPassed();
    }

    [Scenario("Produces same result for equivalent SQL with different formatting")]
    [Fact]
    public async Task Same_result_for_equivalent_sql()
    {
        await Given("two equivalent SQL statements with different formatting", () =>
            {
                var sql1 = "SELECT    id,   name   FROM   users   WHERE   active=1";
                var sql2 = @"SELECT
    id,
    name
FROM
    users
WHERE
    active=1";
                return (sql1, sql2);
            })
            .When("both are normalized", t => (SqlContentNormalizer.Normalize(t.sql1), SqlContentNormalizer.Normalize(t.sql2)))
            .Then("normalized results match", t => t.Item1 == t.Item2)
            .AssertPassed();
    }

    [Scenario("Detects material difference in SQL content")]
    [Fact]
    public async Task Detects_material_differences()
    {
        await Given("two SQL statements with material difference", () =>
            {
                var sql1 = "SELECT id, name FROM users";
                var sql2 = "SELECT id, name, email FROM users";  // Added 'email' column
                return (sql1, sql2);
            })
            .When("both are normalized", t => (SqlContentNormalizer.Normalize(t.sql1), SqlContentNormalizer.Normalize(t.sql2)))
            .Then("normalized results differ", t => t.Item1 != t.Item2)
            .AssertPassed();
    }

    [Scenario("Handles empty SQL file")]
    [Fact]
    public async Task Handles_empty_sql()
    {
        await Given("empty SQL content", () => "")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("result is empty", normalized => normalized == string.Empty)
            .AssertPassed();
    }

    [Scenario("Handles SQL with only comments")]
    [Fact]
    public async Task Handles_only_comments()
    {
        await Given("SQL with only comments", () => @"
-- This is just a comment file
/* 
  Multi-line comment
  block
*/
-- Another comment
")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("result is empty or whitespace", normalized => string.IsNullOrWhiteSpace(normalized))
            .AssertPassed();
    }

    [Scenario("Handles Unicode content in strings")]
    [Fact]
    public async Task Handles_unicode_in_strings()
    {
        await Given("SQL with Unicode in strings", () => "INSERT INTO Test (Name) VALUES ('こんにちは 世界')")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("Unicode is preserved", normalized => normalized.Contains("こんにちは 世界"))
            .AssertPassed();
    }

    [Scenario("Handles strings at different positions")]
    [Fact]
    public async Task Handles_strings_at_different_positions()
    {
        await Given("SQL with strings in various positions", () =>
            "SELECT 'prefix' + Col1 + 'middle' + Col2 + 'suffix' FROM Table")
            .When("content is normalized", SqlContentNormalizer.Normalize)
            .Then("all strings are preserved", normalized =>
                normalized.Contains("'prefix'") && 
                normalized.Contains("'middle'") && 
                normalized.Contains("'suffix'"))
            .AssertPassed();
    }
}
