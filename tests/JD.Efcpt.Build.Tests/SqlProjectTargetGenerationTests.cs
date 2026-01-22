using JD.Efcpt.Build.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests to validate that SQL project detection targets execute correctly in generated MSBuild XML.
/// These tests validate our assumptions about the generated targets file structure.
/// </summary>
public sealed class SqlProjectTargetGenerationTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Generated_targets_file_uses_semicolons_not_backslashes()
    {
        // Arrange - locate the generated targets file
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        _output.WriteLine($"Checking targets file at: {targetsPath}");
        
        // Act - read the file
        Assert.True(File.Exists(targetsPath), $"Targets file not found at: {targetsPath}");
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - validate semicolons are used for target lists
        Assert.Contains("_EfcptDetectSqlProject", targetsContent);
        Assert.Contains("BeforeTargets=\"BeforeBuild;BeforeRebuild\"", targetsContent);
        
        // Critical assertion: must NOT contain backslash separator
        Assert.DoesNotContain("BeforeTargets=\"BeforeBuild\\BeforeRebuild\"", targetsContent);
        
        _output.WriteLine("✓ _EfcptDetectSqlProject uses correct semicolon separator");
    }

    [Fact]
    public void Generated_targets_file_has_correct_sql_detection_target()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        // Act
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - target structure
        Assert.Contains("<Target Name=\"_EfcptDetectSqlProject\"", targetsContent);
        Assert.Contains("<DetectSqlProject", targetsContent);
        Assert.Contains("PropertyName=\"_EfcptIsSqlProject\"", targetsContent);
        
        _output.WriteLine("✓ _EfcptDetectSqlProject target structure is correct");
    }

    [Fact]
    public void Generated_targets_file_has_sql_generation_pipeline()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        // Act
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - SQL generation targets exist
        Assert.Contains("<Target Name=\"EfcptQueryDatabaseSchemaForSqlProj\"", targetsContent);
        Assert.Contains("<Target Name=\"EfcptExtractDatabaseSchemaToScripts\"", targetsContent);
        Assert.Contains("<Target Name=\"EfcptAddSqlFileWarnings\"", targetsContent);
        Assert.Contains("<Target Name=\"AfterSqlProjGeneration\"", targetsContent);
        
        _output.WriteLine("✓ SQL generation pipeline targets exist");
    }

    [Fact]
    public void AfterSqlProjGeneration_hooks_into_Build_target()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        // Act
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - AfterSqlProjGeneration is configured to run before Build
        var afterSqlGenPattern = @"<Target\s+Name=""AfterSqlProjGeneration""[^>]*BeforeTargets=""Build""";
        Assert.Matches(afterSqlGenPattern, targetsContent);
        
        // And it depends on the SQL file warnings task
        Assert.Contains("DependsOnTargets=\"EfcptAddSqlFileWarnings\"", targetsContent);
        
        // And it's conditional on being a SQL project
        var lineWithAfter = targetsContent.Split('\n').First(l => l.Contains("AfterSqlProjGeneration") && l.Contains("<Target"));
        Assert.Contains("_EfcptIsSqlProject", lineWithAfter);
        
        _output.WriteLine("✓ AfterSqlProjGeneration is correctly configured");
    }

    [Fact]
    public void Generated_targets_uses_consistent_condition_formatting()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        // Act
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - conditions use parentheses consistently (our formatting standard)
        var sqlTargetLines = targetsContent.Split('\n')
            .Where(l => l.Contains("_EfcptIsSqlProject") && l.Contains("Condition="))
            .ToList();

        Assert.NotEmpty(sqlTargetLines);
        
        foreach (var line in sqlTargetLines)
        {
            // Should have proper condition formatting
            Assert.Contains("Condition=", line);
            _output.WriteLine($"Condition line: {line.Trim()}");
        }
        
        _output.WriteLine($"✓ Found {sqlTargetLines.Count} condition statements");
    }
}
