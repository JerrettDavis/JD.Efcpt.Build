using JD.Efcpt.Build.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests to validate that SQL project detection targets execute correctly in generated MSBuild XML.
/// These tests validate our assumptions about the generated targets file structure.
/// </summary>
public sealed partial class SqlProjectTargetGenerationTests(ITestOutputHelper output)
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

    [Fact]
    public void EfcptForceRegenerate_property_defaults_to_false_in_generated_props()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var propsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.props");

        // Act
        Assert.True(File.Exists(propsPath), $"Props file not found at: {propsPath}");
        var propsContent = File.ReadAllText(propsPath);

        // Assert - the extension-facing force-regenerate property exists and defaults to false,
        // preserving today's incremental/fingerprint-gated generation when unset (#191).
        Assert.Contains(
            "<EfcptForceRegenerate Condition=\"'$(EfcptForceRegenerate)'==''\">false</EfcptForceRegenerate>",
            propsContent);

        _output.WriteLine("✓ EfcptForceRegenerate defaults to false in generated props");
    }

    [Fact]
    public void EfcptGenerateModels_condition_honors_EfcptForceRegenerate()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        // Act
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - EfcptGenerateModels's Condition now also runs generation when
        // EfcptForceRegenerate=true, in addition to the pre-existing fingerprint-changed /
        // stamp-missing gates (#191).
        var generateModelsLine = targetsContent.Split('\n')
            .First(l => l.Contains("<Target Name=\"EfcptGenerateModels\""));
        Assert.Contains("'$(_EfcptFingerprintChanged)' == 'true'", generateModelsLine);
        Assert.Contains("!Exists('$(EfcptStampFile)')", generateModelsLine);
        Assert.Contains("'$(EfcptForceRegenerate)' == 'true'", generateModelsLine);

        _output.WriteLine("✓ EfcptGenerateModels Condition honors EfcptForceRegenerate");
    }

    [Fact]
    public void ForceRegenerate_stamp_invalidation_target_precedes_EfcptGenerateModels()
    {
        // Arrange
        var testAssemblyPath = typeof(SqlProjectTargetGenerationTests).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testAssemblyPath)!, "..", "..", "..", "..", ".."));
        var targetsPath = Path.Combine(repoRoot, "src", "JD.Efcpt.Build", "buildTransitive", "JD.Efcpt.Build.targets");

        // Act
        var targetsContent = File.ReadAllText(targetsPath);

        // Assert - the stamp-invalidation target exists, is hooked in immediately before
        // EfcptGenerateModels (so it runs before that target's Inputs/Outputs up-to-date check),
        // is gated on EfcptForceRegenerate=true, and deletes the stamp file so the Outputs of
        // EfcptGenerateModels are genuinely missing (defeating the incremental gate) rather than
        // relying solely on the Condition change above (#191).
        Assert.Contains(
            "<Target Name=\"_EfcptForceRegenerateInvalidateStamp\" BeforeTargets=\"EfcptGenerateModels\"",
            targetsContent);

        var invalidationTargetLine = targetsContent.Split('\n')
            .First(l => l.Contains("<Target Name=\"_EfcptForceRegenerateInvalidateStamp\""));
        Assert.Contains("'$(EfcptForceRegenerate)' == 'true'", invalidationTargetLine);

        Assert.Contains("<Delete Condition=\"Exists('$(EfcptStampFile)')\" Files=\"$(EfcptStampFile)\" />", targetsContent);

        // And the invalidation target must appear before EfcptGenerateModels in document order,
        // since BeforeTargets ordering relative to sibling BeforeTargets hooks can depend on
        // declaration order for readability/debuggability even though MSBuild itself doesn't
        // require it.
        var invalidationIndex = targetsContent.IndexOf("<Target Name=\"_EfcptForceRegenerateInvalidateStamp\"", StringComparison.Ordinal);
        var generateModelsIndex = targetsContent.IndexOf("<Target Name=\"EfcptGenerateModels\"", StringComparison.Ordinal);
        Assert.True(invalidationIndex >= 0 && generateModelsIndex >= 0 && invalidationIndex < generateModelsIndex);

        _output.WriteLine("✓ _EfcptForceRegenerateInvalidateStamp is correctly wired before EfcptGenerateModels");
    }
}

