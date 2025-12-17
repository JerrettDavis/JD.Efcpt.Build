using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using Xunit;

namespace JD.Efcpt.Build.Tests;

[Collection(nameof(AssemblySetup))]
public class EnsureDacpacBuiltTests
{
    [Fact]
    public void Uses_existing_dacpac_when_current()
    {
        using var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        File.WriteAllText(dacpac, "dacpac");

        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow.AddMinutes(-10));
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow);

        var engine = new TestBuildEngine();
        var task = new EnsureDacpacBuilt
        {
            BuildEngine = engine,
            SqlProjPath = sqlproj,
            Configuration = "Debug",
            DotNetExe = "dotnet", // should not be invoked because dacpac is current
            LogVerbosity = "detailed"
        };

        var ok = task.Execute();

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(dacpac), task.DacpacPath);
        Assert.Empty(engine.Errors);
    }

    [Fact]
    public void Rebuilds_when_dacpac_is_stale()
    {
        using var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");
        Directory.CreateDirectory(Path.GetDirectoryName(dacpac)!);
        File.WriteAllText(dacpac, "old");

        File.SetLastWriteTimeUtc(sqlproj, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(dacpac, DateTime.UtcNow.AddMinutes(-5));
        
        var initialFakes = Environment.GetEnvironmentVariable("EFCPT_FAKE_BUILD");

        Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", "1");

        var engine = new TestBuildEngine();
        var task = new EnsureDacpacBuilt
        {
            BuildEngine = engine,
            SqlProjPath = sqlproj,
            Configuration = "Debug",
            DotNetExe = "dotnet",
            LogVerbosity = "minimal"
        };

        var ok = task.Execute();

        Assert.True(ok, TestOutput.DescribeErrors(engine));
        Assert.Equal(Path.GetFullPath(dacpac), task.DacpacPath);
        var content = File.ReadAllText(dacpac);
        Assert.Contains("fake dacpac", content);
        
        Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", initialFakes);
    }
}
