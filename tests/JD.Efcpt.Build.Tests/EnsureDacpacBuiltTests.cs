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
            .Finally(r => r.Setup.Folder.Dispose())
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
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Builds DACPAC when none exists")]
    [Fact]
    public async Task Builds_dacpac_when_missing()
    {
        await Given("sqlproj without dacpac", SetupMissingDacpac)
            .When("execute task with fake build", s => ExecuteTask(s, useFakeBuild: true))
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .And("dacpac path is set", r => !string.IsNullOrWhiteSpace(r.Task.DacpacPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Passes EFCPT_TEST_DACPAC environment variable to build process")]
    [Fact]
    public async Task Passes_test_dacpac_environment_variable()
    {
        await Given("sqlproj without dacpac and test env var", SetupWithTestDacpacEnv)
            .When("execute task with fake build", s => ExecuteTask(s, useFakeBuild: true))
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .Finally(r =>
            {
                Environment.SetEnvironmentVariable("EFCPT_TEST_DACPAC", null);
                r.Setup.Folder.Dispose();
            })
            .AssertPassed();
    }

    [Scenario("Uses dotnet build for modern SDK projects")]
    [Fact]
    public async Task Uses_dotnet_build_for_modern_sdk()
    {
        await Given("modern SDK sqlproj without dacpac", SetupModernSdkProject)
            .When("execute task with fake build", s => ExecuteTask(s, useFakeBuild: true))
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Uses msbuild.exe when available on Windows")]
    [Fact]
    public async Task Uses_msbuild_when_available()
    {
        await Given("sqlproj without dacpac and msbuild path", SetupWithMsBuildPath)
            .When("execute task with fake build", s => ExecuteTaskWithMsBuild(s, useFakeBuild: true))
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Executes real process using PowerShell to create DACPAC")]
    [Fact]
    public async Task Executes_real_process_with_powershell()
    {
        await Given("sqlproj with PowerShell build script", SetupWithPowerShellScript)
            .When("execute task without fake build", ExecuteTaskWithCustomTool)
            .Then("task succeeds", r =>
            {
                if (!r.Success)
                {
                    var errors = string.Join("; ", r.Setup.Engine.Errors.Select(e => e.Message));
                    var messages = string.Join("; ", r.Setup.Engine.Messages.Select(m => m.Message));
                    var wrapperPath = Path.Combine(r.Setup.Folder.Root, "mock-dotnet.cmd");
                    var psScriptPath = Path.Combine(r.Setup.Folder.Root, "build.ps1");
                    var wrapperExists = File.Exists(wrapperPath);
                    var psExists = File.Exists(psScriptPath);
                    var dacpacPath = r.Setup.DacpacPath;
                    var dacpacExists = File.Exists(dacpacPath);
                    
                    throw new Exception($"Task failed. Wrapper exists: {wrapperExists}, PS exists: {psExists}, DACPAC exists: {dacpacExists}, DACPAC path: {dacpacPath}, Errors: [{errors}], Messages: [{messages}]");
                }
                return r.Success;
            })
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Executes real process that produces stdout output")]
    [Fact]
    public async Task Executes_real_process_captures_stdout()
    {
        await Given("sqlproj with script that outputs to stdout", SetupWithStdoutScript)
            .When("execute task without fake build", ExecuteTaskWithCustomTool)
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .And("stdout was captured", r => r.Setup.Engine.Messages.Any(m => m.Message!.Contains("Build completed")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Executes real process that produces stderr output")]
    [Fact]
    public async Task Executes_real_process_captures_stderr()
    {
        await Given("sqlproj with script that outputs to stderr", SetupWithStderrScript)
            .When("execute task without fake build", ExecuteTaskWithCustomTool)
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .And("stderr was captured", r => r.Setup.Engine.Messages.Any(m => m.Message!.Contains("Warning message")))
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    [Scenario("Executes real process with EFCPT_TEST_DACPAC environment variable")]
    [Fact]
    public async Task Executes_real_process_with_env_var()
    {
        await Given("sqlproj with script that checks env var", SetupWithEnvVarScript)
            .When("execute task with test dacpac env", ExecuteTaskWithTestDacpacEnv)
            .Then("task succeeds", r => r.Success)
            .And("dacpac is created", r => File.Exists(r.Task.DacpacPath))
            .Finally(r =>
            {
                Environment.SetEnvironmentVariable("EFCPT_TEST_DACPAC", null);
                r.Setup.Folder.Dispose();
            })
            .AssertPassed();
    }

    [Scenario("Executes real process that fails with non-zero exit code")]
    [Fact]
    public async Task Executes_real_process_handles_failure()
    {
        await Given("sqlproj with failing script", SetupWithFailingScript)
            .When("execute task without fake build", ExecuteTaskWithCustomTool)
            .Then("task fails", r => !r.Success)
            .And("errors are logged", r => r.Setup.Engine.Errors.Count > 0)
            .Finally(r => r.Setup.Folder.Dispose())
            .AssertPassed();
    }

    // ========== Additional Setup Methods ==========

    private static SetupState SetupMissingDacpac()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupWithTestDacpacEnv()
    {
        Environment.SetEnvironmentVariable("EFCPT_TEST_DACPAC", "C:\\test\\path\\test.dacpac");
        
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupModernSdkProject()
    {
        var folder = new TestFolder();
        var sqlprojContent = """
                             <Project Sdk="Microsoft.Build.Sql">
                               <PropertyGroup>
                                 <TargetFramework>net8.0</TargetFramework>
                               </PropertyGroup>
                             </Project>
                             """;
        var sqlproj = folder.WriteFile("db/Db.sqlproj", sqlprojContent);
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupWithMsBuildPath()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static TaskResult ExecuteTaskWithMsBuild(SetupState setup, bool useFakeBuild = false)
    {
        var initialFakes = Environment.GetEnvironmentVariable("EFCPT_FAKE_BUILD");
        if (useFakeBuild)
            Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", "1");

        // Create a fake MSBuild.exe that just echoes
        var fakeMsBuild = Path.Combine(setup.Folder.Root, "msbuild.exe");
        File.WriteAllText(fakeMsBuild, "@echo off");

        var task = new EnsureDacpacBuilt
        {
            BuildEngine = setup.Engine,
            SqlProjPath = setup.SqlProj,
            Configuration = "Debug",
            DotNetExe = "dotnet",
            MsBuildExe = fakeMsBuild,
            LogVerbosity = "detailed"
        };

        var success = task.Execute();

        Environment.SetEnvironmentVariable("EFCPT_FAKE_BUILD", initialFakes);

        return new TaskResult(setup, task, success);
    }

    // ========== Process Execution Test Helpers ==========

    private static SetupState SetupWithPowerShellScript()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project Sdk=\"Microsoft.Build.Sql\" />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        // Create a PowerShell script
        var psScriptPath = Path.Combine(folder.Root, "build.ps1");
        var dacpacDir = Path.GetDirectoryName(dacpac)!;
        var psContent = """
                        param()
                        $dacpacDir = $args[0]
                        $dacpacPath = $args[1]
                        New-Item -ItemType Directory -Path $dacpacDir -Force | Out-Null
                        Set-Content -Path $dacpacPath -Value 'fake dacpac content' -Encoding UTF8
                        exit 0
                        """;
        File.WriteAllText(psScriptPath, psContent);

        // Create a batch wrapper that calls PowerShell with arguments
        var wrapperPath = Path.Combine(folder.Root, "mock-dotnet.cmd");
        var wrapperContent = $"""
                              @echo off
                              powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{psScriptPath}" "{dacpacDir}" "{dacpac}"
                              exit /b %ERRORLEVEL%
                              """;
        File.WriteAllText(wrapperPath, wrapperContent, new System.Text.UTF8Encoding(false));

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupWithStdoutScript()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project Sdk=\"Microsoft.Build.Sql\" />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        // Create a PowerShell script
        var psScriptPath = Path.Combine(folder.Root, "build.ps1");
        var dacpacDir = Path.GetDirectoryName(dacpac)!;
        var psContent = """
                        param()
                        $dacpacDir = $args[0]
                        $dacpacPath = $args[1]
                        New-Item -ItemType Directory -Path $dacpacDir -Force | Out-Null
                        Set-Content -Path $dacpacPath -Value 'fake dacpac content' -Encoding UTF8
                        Write-Output 'Build completed successfully'
                        exit 0
                        """;
        File.WriteAllText(psScriptPath, psContent);

        // Create a batch wrapper that calls PowerShell with arguments
        var wrapperPath = Path.Combine(folder.Root, "mock-dotnet.cmd");
        var wrapperContent = $"""
                              @echo off
                              powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{psScriptPath}" "{dacpacDir}" "{dacpac}"
                              exit /b %ERRORLEVEL%
                              """;
        File.WriteAllText(wrapperPath, wrapperContent, new System.Text.UTF8Encoding(false));

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupWithStderrScript()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project Sdk=\"Microsoft.Build.Sql\" />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        // Create a PowerShell script
        var psScriptPath = Path.Combine(folder.Root, "build.ps1");
        var dacpacDir = Path.GetDirectoryName(dacpac)!;
        var psContent = """
                        param()
                        $dacpacDir = $args[0]
                        $dacpacPath = $args[1]
                        New-Item -ItemType Directory -Path $dacpacDir -Force | Out-Null
                        Set-Content -Path $dacpacPath -Value 'fake dacpac content' -Encoding UTF8
                        Write-Error 'Warning message from build'
                        exit 0
                        """;
        File.WriteAllText(psScriptPath, psContent);

        // Create a batch wrapper that calls PowerShell with arguments
        var wrapperPath = Path.Combine(folder.Root, "mock-dotnet.cmd");
        var wrapperContent = $"""
                              @echo off
                              powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{psScriptPath}" "{dacpacDir}" "{dacpac}"
                              exit /b %ERRORLEVEL%
                              """;
        File.WriteAllText(wrapperPath, wrapperContent, new System.Text.UTF8Encoding(false));

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupWithEnvVarScript()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project Sdk=\"Microsoft.Build.Sql\" />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        // Create a PowerShell script
        var psScriptPath = Path.Combine(folder.Root, "build.ps1");
        var dacpacDir = Path.GetDirectoryName(dacpac)!;
        var markerFile = Path.Combine(folder.Root, "env-check.txt");
        var psContent = """
                        param()
                        $dacpacDir = $args[0]
                        $dacpacPath = $args[1]
                        $markerFile = $args[2]
                        New-Item -ItemType Directory -Path $dacpacDir -Force | Out-Null
                        Set-Content -Path $dacpacPath -Value 'fake dacpac content' -Encoding UTF8
                        if ($env:EFCPT_TEST_DACPAC) {
                            Set-Content -Path $markerFile -Value 'env var passed' -Encoding UTF8
                        }
                        exit 0
                        """;
        File.WriteAllText(psScriptPath, psContent);

        // Create a batch wrapper that calls PowerShell with arguments
        var wrapperPath = Path.Combine(folder.Root, "mock-dotnet.cmd");
        var wrapperContent = $"""
                              @echo off
                              powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{psScriptPath}" "{dacpacDir}" "{dacpac}" "{markerFile}"
                              exit /b %ERRORLEVEL%
                              """;
        File.WriteAllText(wrapperPath, wrapperContent, new System.Text.UTF8Encoding(false));

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static SetupState SetupWithFailingScript()
    {
        var folder = new TestFolder();
        var sqlproj = folder.WriteFile("db/Db.sqlproj", "<Project />");
        var dacpac = Path.Combine(folder.Root, "db", "bin", "Debug", "Db.dacpac");

        var psScriptPath = Path.Combine(folder.Root, "build.ps1");
        var psContent = """
                        Write-Output 'Build failed'
                        Write-Error 'Error: compilation failed'
                        exit 1
                        """;
        File.WriteAllText(psScriptPath, psContent);

        var wrapperPath = Path.Combine(folder.Root, "mock-dotnet.cmd");
        var wrapperContent = $"""
                              @echo off
                              powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{psScriptPath}"
                              exit /b %ERRORLEVEL%
                              """;
        File.WriteAllText(wrapperPath, wrapperContent, new System.Text.UTF8Encoding(false));

        var engine = new TestBuildEngine();
        return new SetupState(folder, sqlproj, dacpac, engine);
    }

    private static TaskResult ExecuteTaskWithCustomTool(SetupState setup)
    {
        // Find the wrapper batch file
        var wrapperPath = Path.Combine(setup.Folder.Root, "mock-dotnet.cmd");

        var task = new EnsureDacpacBuilt
        {
            BuildEngine = setup.Engine,
            SqlProjPath = setup.SqlProj,
            Configuration = "Debug",
            DotNetExe = wrapperPath,
            LogVerbosity = "detailed"
        };

        // DO NOT set EFCPT_FAKE_BUILD - we want real process execution
        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

    private static TaskResult ExecuteTaskWithTestDacpacEnv(SetupState setup)
    {
        Environment.SetEnvironmentVariable("EFCPT_TEST_DACPAC", "C:\\test\\sample.dacpac");

        var wrapperPath = Path.Combine(setup.Folder.Root, "mock-dotnet.cmd");

        var task = new EnsureDacpacBuilt
        {
            BuildEngine = setup.Engine,
            SqlProjPath = setup.SqlProj,
            Configuration = "Debug",
            DotNetExe = wrapperPath,
            LogVerbosity = "detailed"
        };

        // DO NOT set EFCPT_FAKE_BUILD
        var success = task.Execute();
        return new TaskResult(setup, task, success);
    }

}
