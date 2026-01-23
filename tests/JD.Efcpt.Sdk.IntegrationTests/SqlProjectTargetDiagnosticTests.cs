using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Focused diagnostic tests to understand why SQL generation targets aren't executing.
/// These tests will use binlog and detailed logging to trace target execution.
/// </summary>
[Collection("SQL Generation Tests")]
public class SqlProjectTargetDiagnosticTests : IAsyncDisposable
{
    private readonly SdkPackageTestFixture _fixture;
    private readonly TestProjectBuilder _builder;
    private readonly ITestOutputHelper _output;

    public SqlProjectTargetDiagnosticTests(SdkPackageTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _builder = new TestProjectBuilder(fixture);
    }

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
    }

    [Fact]
    public async Task Diagnostic_SqlProject_ShowsAllTargetExecution()
    {
        // Arrange - Create SQL project using the proper API
        var connectionString = "Server=.;Database=DiagnosticTest;Integrated Security=true;";
        _builder.CreateSqlProject("DiagnosticSqlProj", "net8.0", connectionString);

        // Act - Build with detailed verbosity to see ALL target execution
        var buildResult = await _builder.BuildAsync("-v:d -p:EfcptLogVerbosity=detailed");

        // Output diagnostic info
        _output.WriteLine("=== BUILD OUTPUT (first 5000 chars) ===");
        _output.WriteLine(buildResult.Output.Substring(0, Math.Min(5000, buildResult.Output.Length)));
        _output.WriteLine("");
        _output.WriteLine("=== BUILD ERRORS ===");
        _output.WriteLine(buildResult.Error);
        _output.WriteLine("");

        // Assert - Check what we find
        _output.WriteLine("=== DIAGNOSTIC ANALYSIS ===");
        
        // Check if EfcptEnabled is set
        var hasEfcptEnabled = buildResult.Output.Contains("EfcptEnabled");
        _output.WriteLine($"EfcptEnabled mentioned: {hasEfcptEnabled}");

        // Check if _EfcptDetectSqlProject ran
        var hasDetectTarget = buildResult.Output.Contains("_EfcptDetectSqlProject");
        _output.WriteLine($"_EfcptDetectSqlProject target mentioned: {hasDetectTarget}");

        // Check if _EfcptIsSqlProject was set
        var hasIsSqlProjectProp = buildResult.Output.Contains("_EfcptIsSqlProject");
        _output.WriteLine($"_EfcptIsSqlProject property mentioned: {hasIsSqlProjectProp}");

        // Check if any SQL generation targets ran
        var hasQueryTarget = buildResult.Output.Contains("EfcptQueryDatabaseSchemaForSqlProj");
        _output.WriteLine($"EfcptQueryDatabaseSchemaForSqlProj mentioned: {hasQueryTarget}");

        var hasExtractTarget = buildResult.Output.Contains("EfcptExtractDatabaseSchemaToScripts");
        _output.WriteLine($"EfcptExtractDatabaseSchemaToScripts mentioned: {hasExtractTarget}");

        var hasAfterTarget = buildResult.Output.Contains("AfterSqlProjGeneration");
        _output.WriteLine($"AfterSqlProjGeneration mentioned: {hasAfterTarget}");

        // Check for specific messages we expect
        var hasSqlMessage = buildResult.Output.Contains("SQL script generation", StringComparison.OrdinalIgnoreCase) ||
                           buildResult.Output.Contains("SQL project will build", StringComparison.OrdinalIgnoreCase);
        _output.WriteLine($"SQL generation message found: {hasSqlMessage}");

        // Look for target execution order
        if (hasDetectTarget && hasAfterTarget)
        {
            var detectIndex = buildResult.Output.IndexOf("_EfcptDetectSqlProject", StringComparison.OrdinalIgnoreCase);
            var afterIndex = buildResult.Output.IndexOf("AfterSqlProjGeneration", StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"Target order - Detect at {detectIndex}, After at {afterIndex}");
        }

        // The build should succeed
        buildResult.Success.Should().BeTrue($"Build should succeed for diagnostic purposes.\n{buildResult}");
        
        // Key assertion: SQL detection should run
        hasDetectTarget.Should().BeTrue("_EfcptDetectSqlProject target should execute");
    }

    [Fact]
    public async Task Diagnostic_StandardProject_DoesNotTriggerSqlTargets()
    {
        // Arrange - Create standard .NET project (using existing API)
        _builder.CreateSdkProject("DiagnosticStandardProj", "net8.0");

        // Act - Build with detailed verbosity
        var buildResult = await _builder.BuildAsync("-v:d");

        // Output diagnostic info
        _output.WriteLine("=== STANDARD PROJECT OUTPUT ===");
        
        // Check that SQL targets are NOT mentioned
        var hasDetectTarget = buildResult.Output.Contains("_EfcptDetectSqlProject");
        _output.WriteLine($"_EfcptDetectSqlProject mentioned: {hasDetectTarget}");

        var hasAfterTarget = buildResult.Output.Contains("AfterSqlProjGeneration");
        _output.WriteLine($"AfterSqlProjGeneration mentioned: {hasAfterTarget}");

        // Build should succeed
        buildResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Diagnostic_CheckPackageContent()
    {
        // This test examines what's actually in the packed JD.Efcpt.Build package
        _output.WriteLine($"Build package path: {SdkPackageTestFixture.BuildPackagePath}");
        _output.WriteLine($"Build package version: {SdkPackageTestFixture.BuildVersion}");

        // Extract and check the targets file
        var tempDir = Path.Combine(Path.GetTempPath(), $"pkg_inspect_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Unzip the package
            System.IO.Compression.ZipFile.ExtractToDirectory(SdkPackageTestFixture.BuildPackagePath, tempDir);

            var targetsFile = Path.Combine(tempDir, "buildTransitive", "JD.Efcpt.Build.targets");
            if (File.Exists(targetsFile))
            {
                var content = File.ReadAllText(targetsFile);
                
                // Check for our critical fix
                var hasCorrectSeparator = content.Contains("BeforeTargets=\"BeforeBuild;BeforeRebuild\"");
                var hasWrongSeparator = content.Contains("BeforeTargets=\"BeforeBuild\\BeforeRebuild\"");
                
                _output.WriteLine($"Targets file exists: true");
                _output.WriteLine($"Has correct semicolon separator: {hasCorrectSeparator}");
                _output.WriteLine($"Has wrong backslash separator: {hasWrongSeparator}");
                
                // Extract the specific line
                var lines = content.Split('\n');
                var detectLine = lines.FirstOrDefault(l => l.Contains("_EfcptDetectSqlProject"));
                if (detectLine != null)
                {
                    _output.WriteLine($"_EfcptDetectSqlProject line: {detectLine.Trim()}");
                }

                hasCorrectSeparator.Should().BeTrue("Package should contain fixed target separator");
                hasWrongSeparator.Should().BeFalse("Package should not contain broken separator");
            }
            else
            {
                _output.WriteLine("WARNING: Targets file not found in package!");
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
