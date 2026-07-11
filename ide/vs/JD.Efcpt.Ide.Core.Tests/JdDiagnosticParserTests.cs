using JD.Efcpt.Ide.Core;
using Xunit;

namespace JD.Efcpt.Ide.Core.Tests;

/// <summary>
/// Exercises <see cref="JdDiagnosticParser"/>, mirroring
/// <c>ide/vscode/src/test/unit/jdDiagnostics.test.ts</c> so both extensions surface the same
/// diagnostics from the same MSBuild output.
/// </summary>
public sealed class JdDiagnosticParserTests
{
    [Fact]
    public void TryParseLine_parses_a_warning_line()
    {
        var result = JdDiagnosticParser.TryParseLine(
            "warning JD0002: Connection string 'MyDatabase' not found in appsettings.json");

        Assert.NotNull(result);
        Assert.Equal(JdDiagnosticSeverity.Warning, result!.Severity);
        Assert.Equal("JD0002", result.Code);
        Assert.Equal("Connection string 'MyDatabase' not found in appsettings.json", result.Message);
    }

    [Fact]
    public void TryParseLine_parses_an_error_line()
    {
        var result = JdDiagnosticParser.TryParseLine(
            "error JD0011: Failed to parse configuration file 'appsettings.json'");

        Assert.NotNull(result);
        Assert.Equal(JdDiagnosticSeverity.Error, result!.Severity);
        Assert.Equal("JD0011", result.Code);
    }

    [Fact]
    public void TryParseLine_strips_trailing_msbuild_project_suffix()
    {
        var result = JdDiagnosticParser.TryParseLine(
            @"C:\proj\Program.cs(1,1): warning JD0002: message here [C:\proj\Project.csproj]");

        Assert.NotNull(result);
        Assert.Equal("message here", result!.Message);
    }

    [Fact]
    public void TryParseLine_returns_null_for_a_non_diagnostic_line()
    {
        Assert.Null(JdDiagnosticParser.TryParseLine("Build succeeded."));
    }

    [Fact]
    public void TryParseLine_returns_null_for_an_empty_line()
    {
        Assert.Null(JdDiagnosticParser.TryParseLine(string.Empty));
    }

    [Fact]
    public void TryParseLine_ignores_non_JD_diagnostic_codes()
    {
        Assert.Null(JdDiagnosticParser.TryParseLine("warning CS0168: variable declared but never used"));
    }

    [Fact]
    public void ParseLines_finds_every_diagnostic_in_multi_line_output_in_order()
    {
        var output =
            "Build started.\r\n" +
            "warning JD0002: first\r\n" +
            "some other line\r\n" +
            "error JD0011: second [C:\\proj\\Project.csproj]\r\n" +
            "Build FAILED.\r\n";

        var results = JdDiagnosticParser.ParseLines(output);

        Assert.Equal(2, results.Count);
        Assert.Equal("JD0002", results[0].Code);
        Assert.Equal(JdDiagnosticSeverity.Warning, results[0].Severity);
        Assert.Equal("JD0011", results[1].Code);
        Assert.Equal(JdDiagnosticSeverity.Error, results[1].Severity);
        Assert.Equal("second", results[1].Message);
    }

    [Fact]
    public void ParseLines_handles_unix_line_endings()
    {
        var output = "warning JD0001: a\nerror JD0002: b\n";

        var results = JdDiagnosticParser.ParseLines(output);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void ParseLines_returns_empty_for_empty_input()
    {
        Assert.Empty(JdDiagnosticParser.ParseLines(string.Empty));
    }
}
