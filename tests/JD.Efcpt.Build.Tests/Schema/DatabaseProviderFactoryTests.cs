using FirebirdSql.Data.FirebirdClient;
using JD.Efcpt.Build.Tasks.Schema;
using JD.Efcpt.Build.Tasks.Schema.Providers;
using JD.Efcpt.Build.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Snowflake.Data.Client;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Schema;

[Feature("DatabaseProviderFactory: creates connections and schema readers for all providers")]
[Collection(nameof(AssemblySetup))]
public sealed class DatabaseProviderFactoryTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region NormalizeProvider Tests

    [Scenario("Normalizes SQL Server provider aliases")]
    [Theory]
    [InlineData("mssql", "mssql")]
    [InlineData("sqlserver", "mssql")]
    [InlineData("sql-server", "mssql")]
    [InlineData("MSSQL", "mssql")]
    [InlineData("SqlServer", "mssql")]
    public async Task Normalizes_sql_server_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalizes PostgreSQL provider aliases")]
    [Theory]
    [InlineData("postgres", "postgres")]
    [InlineData("postgresql", "postgres")]
    [InlineData("pgsql", "postgres")]
    [InlineData("POSTGRES", "postgres")]
    public async Task Normalizes_postgres_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalizes MySQL provider aliases")]
    [Theory]
    [InlineData("mysql", "mysql")]
    [InlineData("mariadb", "mysql")]
    [InlineData("MySQL", "mysql")]
    public async Task Normalizes_mysql_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalizes SQLite provider aliases")]
    [Theory]
    [InlineData("sqlite", "sqlite")]
    [InlineData("sqlite3", "sqlite")]
    [InlineData("SQLite", "sqlite")]
    public async Task Normalizes_sqlite_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalizes Oracle provider aliases")]
    [Theory]
    [InlineData("oracle", "oracle")]
    [InlineData("oracledb", "oracle")]
    [InlineData("ORACLE", "oracle")]
    public async Task Normalizes_oracle_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalizes Firebird provider aliases")]
    [Theory]
    [InlineData("firebird", "firebird")]
    [InlineData("fb", "firebird")]
    [InlineData("Firebird", "firebird")]
    public async Task Normalizes_firebird_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Normalizes Snowflake provider aliases")]
    [Theory]
    [InlineData("snowflake", "snowflake")]
    [InlineData("sf", "snowflake")]
    [InlineData("Snowflake", "snowflake")]
    public async Task Normalizes_snowflake_aliases(string input, string expected)
    {
        await Given($"provider input '{input}'", () => input)
            .When("normalized", p => DatabaseProviderFactory.NormalizeProvider(p))
            .Then($"returns '{expected}'", result => result == expected)
            .AssertPassed();
    }

    [Scenario("Throws for unsupported provider")]
    [Fact]
    public async Task Throws_for_unsupported_provider()
    {
        await Given("an unsupported provider", () => "mongodb")
            .When("normalized", p =>
            {
                try
                {
                    DatabaseProviderFactory.NormalizeProvider(p);
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .Then("throws NotSupportedException", ex => ex is NotSupportedException)
            .And("message contains provider name", ex => ex!.Message.Contains("mongodb"))
            .AssertPassed();
    }

    [Scenario("Throws for null provider")]
    [Fact]
    public async Task Throws_for_null_provider()
    {
        await Given("a null provider", () => (string?)null)
            .When("normalized", p =>
            {
                try
                {
                    DatabaseProviderFactory.NormalizeProvider(p!);
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .Then("throws ArgumentException", ex => ex is ArgumentException)
            .AssertPassed();
    }

    #endregion

    #region CreateConnection Tests

    [Scenario("Creates SQL Server connection")]
    [Fact]
    public async Task Creates_sql_server_connection()
    {
        await Given("mssql provider and connection string", () => ("mssql", "Server=localhost;Database=test"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns SqlConnection", conn => conn is SqlConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates PostgreSQL connection")]
    [Fact]
    public async Task Creates_postgres_connection()
    {
        await Given("postgres provider and connection string", () => ("postgres", "Host=localhost;Database=test"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns NpgsqlConnection", conn => conn is NpgsqlConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates MySQL connection")]
    [Fact]
    public async Task Creates_mysql_connection()
    {
        await Given("mysql provider and connection string", () => ("mysql", "Server=localhost;Database=test"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns MySqlConnection", conn => conn is MySqlConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates SQLite connection")]
    [Fact]
    public async Task Creates_sqlite_connection()
    {
        await Given("sqlite provider and connection string", () => ("sqlite", "Data Source=:memory:"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns SqliteConnection", conn => conn is SqliteConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates Oracle connection")]
    [Fact]
    public async Task Creates_oracle_connection()
    {
        await Given("oracle provider and connection string", () => ("oracle", "Data Source=localhost:1521/ORCL"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns OracleConnection", conn => conn is OracleConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates Firebird connection")]
    [Fact]
    public async Task Creates_firebird_connection()
    {
        await Given("firebird provider and connection string", () => ("firebird", "Database=localhost:test.fdb"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns FbConnection", conn => conn is FbConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    [Scenario("Creates Snowflake connection")]
    [Fact]
    public async Task Creates_snowflake_connection()
    {
        await Given("snowflake provider and connection string", () => ("snowflake", "account=test;user=test"))
            .When("connection created", t => DatabaseProviderFactory.CreateConnection(t.Item1, t.Item2))
            .Then("returns SnowflakeDbConnection", conn => conn is SnowflakeDbConnection)
            .Finally(conn => conn.Dispose())
            .AssertPassed();
    }

    #endregion

    #region CreateSchemaReader Tests

    [Scenario("Creates SQL Server schema reader")]
    [Fact]
    public async Task Creates_sql_server_schema_reader()
    {
        await Given("mssql provider", () => "mssql")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns SqlServerSchemaReader", reader => reader is SqlServerSchemaReader)
            .AssertPassed();
    }

    [Scenario("Creates PostgreSQL schema reader")]
    [Fact]
    public async Task Creates_postgres_schema_reader()
    {
        await Given("postgres provider", () => "postgres")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns PostgreSqlSchemaReader", reader => reader is PostgreSqlSchemaReader)
            .AssertPassed();
    }

    [Scenario("Creates MySQL schema reader")]
    [Fact]
    public async Task Creates_mysql_schema_reader()
    {
        await Given("mysql provider", () => "mysql")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns MySqlSchemaReader", reader => reader is MySqlSchemaReader)
            .AssertPassed();
    }

    [Scenario("Creates SQLite schema reader")]
    [Fact]
    public async Task Creates_sqlite_schema_reader()
    {
        await Given("sqlite provider", () => "sqlite")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns SqliteSchemaReader", reader => reader is SqliteSchemaReader)
            .AssertPassed();
    }

    [Scenario("Creates Oracle schema reader")]
    [Fact]
    public async Task Creates_oracle_schema_reader()
    {
        await Given("oracle provider", () => "oracle")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns OracleSchemaReader", reader => reader is OracleSchemaReader)
            .AssertPassed();
    }

    [Scenario("Creates Firebird schema reader")]
    [Fact]
    public async Task Creates_firebird_schema_reader()
    {
        await Given("firebird provider", () => "firebird")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns FirebirdSchemaReader", reader => reader is FirebirdSchemaReader)
            .AssertPassed();
    }

    [Scenario("Creates Snowflake schema reader")]
    [Fact]
    public async Task Creates_snowflake_schema_reader()
    {
        await Given("snowflake provider", () => "snowflake")
            .When("schema reader created", p => DatabaseProviderFactory.CreateSchemaReader(p))
            .Then("returns SnowflakeSchemaReader", reader => reader is SnowflakeSchemaReader)
            .AssertPassed();
    }

    #endregion

    #region GetProviderDisplayName Tests

    [Scenario("Returns correct display names")]
    [Theory]
    [InlineData("mssql", "SQL Server")]
    [InlineData("postgres", "PostgreSQL")]
    [InlineData("mysql", "MySQL/MariaDB")]
    [InlineData("sqlite", "SQLite")]
    [InlineData("oracle", "Oracle")]
    [InlineData("firebird", "Firebird")]
    [InlineData("snowflake", "Snowflake")]
    public async Task Returns_correct_display_names(string provider, string expected)
    {
        await Given($"provider '{provider}'", () => provider)
            .When("display name requested", p => DatabaseProviderFactory.GetProviderDisplayName(p))
            .Then($"returns '{expected}'", name => name == expected)
            .AssertPassed();
    }

    #endregion
}
