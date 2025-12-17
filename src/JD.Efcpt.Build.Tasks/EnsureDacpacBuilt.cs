using Microsoft.Build.Framework;
using System.Diagnostics;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that ensures a DACPAC exists for a given SQL project and build configuration.
/// </summary>
/// <remarks>
/// <para>
/// This task is typically invoked by the <c>EfcptEnsureDacpac</c> target in the JD.Efcpt.Build
/// pipeline. It locates the SQL project (<c>.sqlproj</c>), determines whether an existing DACPAC is
/// up to date, and, if necessary, triggers a build using either <c>msbuild.exe</c> or
/// <c>dotnet msbuild</c>.
/// </para>
/// <para>
/// The staleness heuristic compares the last write time of the most recently modified source file
/// (excluding <c>bin</c> and <c>obj</c> directories) with the last write time of the DACPAC. When the
/// DACPAC is missing or older than any source file, the SQL project is rebuilt.
/// </para>
/// <para>
/// For testing and diagnostics, the task honours the following environment variables:
/// <list type="bullet">
///   <item><description><c>EFCPT_FAKE_BUILD</c> - when set, no external build is invoked. Instead a fake DACPAC file is written under the expected <c>bin/&lt;Configuration&gt;</c> folder.</description></item>
///   <item><description><c>EFCPT_TEST_DACPAC</c> - if present, forwarded to the child process as an environment variable of the same name.</description></item>
/// </list>
/// These hooks are primarily intended for the test suite and are not considered a stable public API.
/// </para>
/// </remarks>
public sealed class EnsureDacpacBuilt : Task
{
    /// <summary>
    /// Path to the SQL project (<c>.sqlproj</c>) that produces the DACPAC.
    /// </summary>
    [Required] public string SqlProjPath { get; set; } = "";

    /// <summary>
    /// Build configuration to use when compiling the SQL project.
    /// </summary>
    /// <value>Typically <c>Debug</c> or <c>Release</c>, but any valid configuration is accepted.</value>
    [Required] public string Configuration { get; set; } = "";

    /// <summary>Path to <c>msbuild.exe</c> when available (Windows/Visual Studio scenarios).</summary>
    /// <value>
    /// When non-empty and the file exists, this executable is preferred over <see cref="DotNetExe"/> for
    /// building the SQL project.
    /// </value>
    public string MsBuildExe { get; set; } = "";

    /// <summary>Path to the <c>dotnet</c> host executable.</summary>
    /// <value>
    /// Defaults to <c>dotnet</c>. Used to run <c>dotnet msbuild</c> when <see cref="MsBuildExe"/> is not
    /// provided or does not exist.
    /// </value>
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Full path to the resolved DACPAC after the task completes.
    /// </summary>
    /// <value>
    /// When an up-to-date DACPAC already exists, this is set to that file. Otherwise it points to the
    /// DACPAC produced by the build.
    /// </value>
    [Output] public string DacpacPath { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);
        try
        {
            var sqlproj = Path.GetFullPath(SqlProjPath);
            if (!File.Exists(sqlproj))
                throw new FileNotFoundException("sqlproj not found", sqlproj);

            var binDir = Path.Combine(Path.GetDirectoryName(sqlproj)!, "bin", Configuration);
            Directory.CreateDirectory(binDir);

            var latestSourceWrite = LatestSourceWrite(sqlproj);
            // Heuristic: first dacpac under bin/<Configuration>
            var existing = FindDacpac(binDir);
            if (existing is not null)
            {
                // Staleness check: rebuild if any source is newer than dacpac
                var dacTime = File.GetLastWriteTimeUtc(existing);
                if (dacTime >= latestSourceWrite)
                {
                    DacpacPath = existing;
                    log.Detail($"Using existing DACPAC: {DacpacPath}");
                    return true;
                }
                log.Detail("DACPAC exists but appears stale. Rebuilding sqlproj...");
            }
            else
            {
                log.Detail("DACPAC not found. Building sqlproj...");
            }

            BuildSqlProj(log, sqlproj);

            var built = FindDacpac(binDir) ?? FindDacpac(Path.Combine(Path.GetDirectoryName(sqlproj)!, "bin")) 
                        ?? throw new FileNotFoundException($"DACPAC not found after build. Looked under: {binDir}");

            DacpacPath = built;
            log.Info($"DACPAC: {DacpacPath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private static string? FindDacpac(string dir)
    {
        if (!Directory.Exists(dir)) return null;
        return Directory.EnumerateFiles(dir, "*.dacpac", SearchOption.AllDirectories)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault();
    }

    private static DateTime LatestSourceWrite(string sqlproj)
    {
        var root = Path.GetDirectoryName(sqlproj)!;
        var latest = File.GetLastWriteTimeUtc(sqlproj);

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsUnder(file, Path.Combine(root, "bin")) || IsUnder(file, Path.Combine(root, "obj")))
                continue;

            var t = File.GetLastWriteTimeUtc(file);
            if (t > latest) latest = t;
        }

        return latest;
    }

    private static bool IsUnder(string path, string root)
    {
        var rel = Path.GetRelativePath(root, path);
        return !rel.StartsWith("..", StringComparison.Ordinal);
    }

    private void BuildSqlProj(BuildLog log, string sqlproj)
    {
        var fake = Environment.GetEnvironmentVariable("EFCPT_FAKE_BUILD");
        if (!string.IsNullOrWhiteSpace(fake))
        {
            var projectName = Path.GetFileNameWithoutExtension(sqlproj);
            var dest = Path.Combine(Path.GetDirectoryName(sqlproj)!, "bin", Configuration, projectName + ".dacpac");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.WriteAllText(dest, "fake dacpac");
            log.Info($"EFCPT_FAKE_BUILD set to {fake}; wrote {dest}");
            return;
        }

        var useMsbuildExe = !string.IsNullOrWhiteSpace(MsBuildExe) && File.Exists(MsBuildExe);
        var requestedFileName = useMsbuildExe ? MsBuildExe : DotNetExe;
        var requestedArgs = useMsbuildExe
            ? $"\"{sqlproj}\" /t:Restore /t:Build /p:Configuration=\"{Configuration}\" /nologo"
            : $"msbuild \"{sqlproj}\" /t:Restore /t:Build /p:Configuration=\"{Configuration}\" /nologo";
        var (fileName, args) = NormalizeCommand(requestedFileName, requestedArgs);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(sqlproj) ?? "",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        var testDac = Environment.GetEnvironmentVariable("EFCPT_TEST_DACPAC");
        if (!string.IsNullOrWhiteSpace(testDac))
            psi.Environment["EFCPT_TEST_DACPAC"] = testDac;

        var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {fileName}");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            log.Error(stdout);
            log.Error(stderr);
            throw new InvalidOperationException($"sqlproj build failed with exit code {p.ExitCode}");
        }

        if (!string.IsNullOrWhiteSpace(stdout)) log.Detail(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) log.Detail(stderr);
    }

    private static (string fileName, string args) NormalizeCommand(string command, string args)
    {
        if (OperatingSystem.IsWindows() && (command.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || command.EndsWith(".bat", StringComparison.OrdinalIgnoreCase)))
        {
            return ("cmd.exe", $"/c \"{command}\" {args}");
        }

        return (command, args);
    }
}
