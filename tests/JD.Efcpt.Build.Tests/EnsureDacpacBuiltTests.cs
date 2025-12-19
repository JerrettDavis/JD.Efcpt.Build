using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

[Feature("EnsureDacpacBuilt task: builds or reuses DACPAC based on timestamps")]
[Collection(nameof(AssemblySetup))]
public sealed class EnsureDacpacBuiltTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(
        TestFolder Folder,
        string SqlProj,
        string DacpacPath,
        TestBuildEngine Engine);

    private sealed record TaskResult(
        SetupState Setup,
        EnsureDacpacBuilt Task,
        bool Success);

    private static SetupState SetupCurrentDacpac()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        File.WriteAllText(dacpac, "dacpac");

        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow.AddMinutes(-10));
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow);

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupStaleDacpac()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        File.WriteAllText(dacpac, "old");

        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow.AddMinutes(-5));

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static TaskResult ExecuteTask(SetupState setup, bool useFakeBuild = false)
    {
        var initialFakes = Environment.GetEnvironmentVariable("EFCPT_FAKE_BUILD");
        if (useFakeBuild)
            Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", "1");

        var task = new EnsureDacpacBuilt
        {
            BuildEngine = setup.Engine,
            SqlProjPath = setup.SqlProj,
            Configuration = "Debug",
            DotNetExe = "dotnet",
            LogVerbosity = "detailed"
        };

        var success = task.Execute();

        Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", initialFakes);

        return new TaskResult(setup, task, success);
    }

    [Scenario("Uses existing DACPAC when it is newer than sqlproj")]
    [Fact]
    public async Task Uses_existing_dacpac_when_current()
    {
        await Given("sqlproj and current dacpac", SetupCurrentDacpac)
            .When("execute task", s => ExecuteTask(s, useFakeBuild: false))
            .Then("task succeeds", r => r.Success)
            .And("dacpac path is correct", r => r.Task.DacpacPath == Path.GetFullPath(r.Setup.DacpacPath))
            .And("no errors logged", r => r.Setup.Engine.Errors.Count == 0)
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Rebuilds DACPAC when it is older than sqlproj")]
    [Fact]
    public async Task Rebuilds_when_dacpac_is_stale()
    {
        await Given("sqlproj newer than dacpac", SetupStaleDacpac)
            .When("execute task with fake build", s => ExecuteTask(s, useFakeBuild: true))
            .Then("task succeeds", r => r.Success)
            .And("dacpac path is correct", r => r.Task.DacpacPath == Path.GetFullPath(r.Setup.DacpacPath))
            .And("dacpac contains fake content", r =>
            {
                var content = File.ReadAllText(r.Setup.DacpacPath);
                return content.Contains("fake dacpac");
            })
            .And(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }
}
