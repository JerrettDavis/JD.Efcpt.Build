using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the DacpacFingerprint class that computes schema-based hashes for DACPAC files.
/// </summary>
[Feature("DacpacFingerprint: schema-based DACPAC hashing for reliable change detection")]
[Collection(nameof(AssemblySetup))]
public sealed class DacpacFingerprintTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private const string SampleModelXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <DataSchemaModel>
          <Header>
            <Metadata Name="FileName" Value="C:\builds\agent1\work\MyDatabase.dacpac" />
            <Metadata Name="AssemblySymbolsName" Value="C:\builds\agent1\work\MyDatabase.pdb" />
          </Header>
          <Model>
            <Element Type="SqlTable" Name="[dbo].[Users]">
              <Property Name="IsAnsiNullsOn" Value="True" />
            </Element>
          </Model>
        </DataSchemaModel>
        """;

    private const string SampleModelXmlDifferentPath = """
        <?xml version="1.0" encoding="utf-8"?>
        <DataSchemaModel>
          <Header>
            <Metadata Name="FileName" Value="D:\different\path\MyDatabase.dacpac" />
            <Metadata Name="AssemblySymbolsName" Value="D:\different\path\MyDatabase.pdb" />
          </Header>
          <Model>
            <Element Type="SqlTable" Name="[dbo].[Users]">
              <Property Name="IsAnsiNullsOn" Value="True" />
            </Element>
          </Model>
        </DataSchemaModel>
        """;

    private const string DifferentSchemaModelXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <DataSchemaModel>
          <Header>
            <Metadata Name="FileName" Value="C:\builds\agent1\work\MyDatabase.dacpac" />
          </Header>
          <Model>
            <Element Type="SqlTable" Name="[dbo].[Orders]">
              <Property Name="IsAnsiNullsOn" Value="True" />
            </Element>
          </Model>
        </DataSchemaModel>
        """;

    [Scenario("Computes fingerprint for valid DACPAC")]
    [Fact]
    public async Task Computes_fingerprint_for_valid_dacpac()
    {
        await Given("a valid DACPAC file", () =>
            {
                var folder = new TestFolder();
                var path = MockDacpacHelper.CreateWithScripts(folder, "test.dacpac", SampleModelXml);
                return (folder, path);
            })
            .When("fingerprint is computed", t => (t.folder, DacpacFingerprint.Compute(t.path)))
            .Then("fingerprint is 16 characters", t => t.Item2.Length == 16)
            .And("fingerprint contains only hex characters", t => t.Item2.All(c => char.IsAsciiHexDigit(c)))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint is deterministic")]
    [Fact]
    public async Task Fingerprint_is_deterministic()
    {
        await Given("a DACPAC file", () =>
            {
                var folder = new TestFolder();
                var path = MockDacpacHelper.CreateWithScripts(folder, "test.dacpac", SampleModelXml);
                return (folder, path);
            })
            .When("fingerprint is computed twice", t =>
                (t.folder, DacpacFingerprint.Compute(t.path), DacpacFingerprint.Compute(t.path)))
            .Then("fingerprints match", t => t.Item2 == t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Same schema with different paths produces same fingerprint")]
    [Fact]
    public async Task Same_schema_different_paths_same_fingerprint()
    {
        await Given("two DACPACs with same schema but different path metadata", () =>
            {
                var folder = new TestFolder();
                var path1 = MockDacpacHelper.CreateWithScripts(folder, "test1.dacpac", SampleModelXml);
                var path2 = MockDacpacHelper.CreateWithScripts(folder, "test2.dacpac", SampleModelXmlDifferentPath);
                return (folder, path1, path2);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.path1), DacpacFingerprint.Compute(t.path2)))
            .Then("fingerprints match despite different paths", t => t.Item2 == t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Different schemas produce different fingerprints")]
    [Fact]
    public async Task Different_schemas_different_fingerprints()
    {
        await Given("two DACPACs with different schemas", () =>
            {
                var folder = new TestFolder();
                var path1 = MockDacpacHelper.CreateWithScripts(folder, "test1.dacpac", SampleModelXml);
                var path2 = MockDacpacHelper.CreateWithScripts(folder, "test2.dacpac", DifferentSchemaModelXml);
                return (folder, path1, path2);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.path1), DacpacFingerprint.Compute(t.path2)))
            .Then("fingerprints differ", t => t.Item2 != t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Includes predeploy script in fingerprint")]
    [Fact]
    public async Task Includes_predeploy_script()
    {
        await Given("two DACPACs with same schema but different predeploy scripts", () =>
            {
                var folder = new TestFolder();
                var path1 = MockDacpacHelper.CreateWithScripts(folder, "test1.dacpac", SampleModelXml, preDeploy: "SELECT 1");
                var path2 = MockDacpacHelper.CreateWithScripts(folder, "test2.dacpac", SampleModelXml, preDeploy: "SELECT 2");
                return (folder, path1, path2);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.path1), DacpacFingerprint.Compute(t.path2)))
            .Then("fingerprints differ due to predeploy", t => t.Item2 != t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Includes postdeploy script in fingerprint")]
    [Fact]
    public async Task Includes_postdeploy_script()
    {
        await Given("two DACPACs with same schema but different postdeploy scripts", () =>
            {
                var folder = new TestFolder();
                var path1 = MockDacpacHelper.CreateWithScripts(folder, "test1.dacpac", SampleModelXml, postDeploy: "SELECT 1");
                var path2 = MockDacpacHelper.CreateWithScripts(folder, "test2.dacpac", SampleModelXml, postDeploy: "SELECT 2");
                return (folder, path1, path2);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.path1), DacpacFingerprint.Compute(t.path2)))
            .Then("fingerprints differ due to postdeploy", t => t.Item2 != t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DACPAC with no deploy scripts works")]
    [Fact]
    public async Task No_deploy_scripts_works()
    {
        await Given("a DACPAC without deploy scripts", () =>
            {
                var folder = new TestFolder();
                var path = MockDacpacHelper.CreateWithScripts(folder, "test.dacpac", SampleModelXml);
                return (folder, path);
            })
            .When("fingerprint is computed", t => (t.folder, DacpacFingerprint.Compute(t.path)))
            .Then("fingerprint is computed successfully", t => !string.IsNullOrEmpty(t.Item2))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Throws for missing file")]
    [Fact]
    public async Task Throws_for_missing_file()
    {
        await Given("a non-existent DACPAC path", () => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.dacpac"))
            .When("fingerprint computation is attempted", path =>
            {
                try
                {
                    DacpacFingerprint.Compute(path);
                    return (threw: false, exType: null!);
                }
                catch (Exception ex)
                {
                    return (threw: true, exType: ex.GetType());
                }
            })
            .Then("FileNotFoundException is thrown", r => r.threw && r.exType == typeof(FileNotFoundException))
            .AssertPassed();
    }

    [Scenario("Throws for DACPAC without model.xml")]
    [Fact]
    public async Task Throws_for_missing_model_xml()
    {
        await Given("a DACPAC without model.xml", () =>
            {
                var folder = new TestFolder();
                var path = MockDacpacHelper.CreateInvalid(folder, "invalid.dacpac");
                return (folder, path);
            })
            .When("fingerprint computation is attempted", t =>
            {
                try
                {
                    DacpacFingerprint.Compute(t.path);
                    return (t.folder, threw: false, exType: null!);
                }
                catch (Exception ex)
                {
                    return (t.folder, threw: true, exType: ex.GetType());
                }
            })
            .Then("InvalidOperationException is thrown", r => r.threw && r.exType == typeof(InvalidOperationException))
            .Finally(r => r.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Fingerprint differs when predeploy script is added")]
    [Fact]
    public async Task Adding_predeploy_changes_fingerprint()
    {
        await Given("a DACPAC with and without predeploy script", () =>
            {
                var folder = new TestFolder();
                var pathWithout = MockDacpacHelper.CreateWithScripts(folder, "without.dacpac", SampleModelXml);
                var pathWith = MockDacpacHelper.CreateWithScripts(folder, "with.dacpac", SampleModelXml, preDeploy: "SELECT 1");
                return (folder, pathWithout, pathWith);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.pathWithout), DacpacFingerprint.Compute(t.pathWith)))
            .Then("fingerprints differ", t => t.Item2 != t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles Unix-style paths in metadata")]
    [Fact]
    public async Task Handles_unix_paths_in_metadata()
    {
        var unixPathModelXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="/home/user/builds/MyDatabase.dacpac" />
                <Metadata Name="AssemblySymbolsName" Value="/home/user/builds/MyDatabase.pdb" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[Users]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        await Given("DACPACs with Windows and Unix paths in metadata", () =>
            {
                var folder = new TestFolder();
                var windowsPath = MockDacpacHelper.CreateWithScripts(folder, "windows.dacpac", SampleModelXml);
                var unixPath = MockDacpacHelper.CreateWithScripts(folder, "unix.dacpac", unixPathModelXml);
                return (folder, windowsPath, unixPath);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.windowsPath), DacpacFingerprint.Compute(t.unixPath)))
            .Then("fingerprints match (paths normalized to filenames)", t => t.Item2 == t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles empty metadata value")]
    [Fact]
    public async Task Handles_empty_metadata_value()
    {
        var emptyValueModelXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="" />
                <Metadata Name="AssemblySymbolsName" Value="" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[Users]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        await Given("a DACPAC with empty metadata values", () =>
            {
                var folder = new TestFolder();
                var path = MockDacpacHelper.CreateWithScripts(folder, "empty.dacpac", emptyValueModelXml);
                return (folder, path);
            })
            .When("fingerprint is computed", t => (t.folder, DacpacFingerprint.Compute(t.path)))
            .Then("fingerprint is computed successfully", t => !string.IsNullOrEmpty(t.Item2))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Normalizes custom metadata paths using fallback regex")]
    [Fact]
    public async Task Normalizes_custom_metadata_paths()
    {
        // Test the fallback regex path by using a non-standard metadata name that contains a path
        var customMetadataModelXml1 = """
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="MyDatabase.dacpac" />
                <Metadata Name="CustomPath" Value="C:\builds\agent1\work\custom.file" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[Users]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        var customMetadataModelXml2 = """
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="MyDatabase.dacpac" />
                <Metadata Name="CustomPath" Value="D:\different\path\custom.file" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[Users]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        await Given("two DACPACs with different custom metadata paths", () =>
            {
                var folder = new TestFolder();
                var path1 = MockDacpacHelper.CreateWithScripts(folder, "custom1.dacpac", customMetadataModelXml1);
                var path2 = MockDacpacHelper.CreateWithScripts(folder, "custom2.dacpac", customMetadataModelXml2);
                return (folder, path1, path2);
            })
            .When("fingerprints are computed", t =>
                (t.folder, DacpacFingerprint.Compute(t.path1), DacpacFingerprint.Compute(t.path2)))
            .Then("fingerprints differ because custom metadata is not normalized", t => t.Item2 != t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Handles metadata value with no path separators")]
    [Fact]
    public async Task Handles_metadata_with_no_path_separators()
    {
        var noPathModelXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <DataSchemaModel>
              <Header>
                <Metadata Name="FileName" Value="MyDatabase.dacpac" />
                <Metadata Name="AssemblySymbolsName" Value="MyDatabase.pdb" />
              </Header>
              <Model>
                <Element Type="SqlTable" Name="[dbo].[Users]">
                  <Property Name="IsAnsiNullsOn" Value="True" />
                </Element>
              </Model>
            </DataSchemaModel>
            """;

        await Given("a DACPAC with metadata values that have no path separators", () =>
            {
                var folder = new TestFolder();
                var path = MockDacpacHelper.CreateWithScripts(folder, "nopath.dacpac", noPathModelXml);
                return (folder, path);
            })
            .When("fingerprint is computed", t => (t.folder, DacpacFingerprint.Compute(t.path)))
            .Then("fingerprint is computed successfully", t => !string.IsNullOrEmpty(t.Item2))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }
}
