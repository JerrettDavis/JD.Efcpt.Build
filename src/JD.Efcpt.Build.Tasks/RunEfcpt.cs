using Microsoft.Build.Framework;
using System.Diagnostics;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// MSBuild task that invokes the EF Core Power Tools CLI (efcpt) using one of several dotnet tool modes.
/// </summary>
/// <remarks>
/// <para>
/// This task is typically invoked from the <c>EfcptGenerateModels</c> MSBuild target defined in
/// <c>JD.Efcpt.Build</c>. It executes the efcpt CLI against a DACPAC and configuration files in order to
/// generate EF Core model C# files into <see cref="OutputDir"/>.
/// </para>
/// <para>
/// Tool resolution follows this order:
/// <list type="number">
///   <item>
///     <description>
///       If <see cref="ToolPath"/> is a non-empty explicit path, that executable is run directly.
///     </description>
///   </item>
///   <item>
///     <description>
///       Otherwise, if <see cref="ToolMode"/> is <c>tool-manifest</c>, or is <c>auto</c> and a
///       <c>.config/dotnet-tools.json</c> file is found by walking up from <see cref="WorkingDirectory"/>,
///       the task runs <c>dotnet tool run &lt;ToolCommand&gt;</c> using the discovered manifest. When
///       <see cref="ToolRestore"/> evaluates to <c>true</c>, <c>dotnet tool restore</c> is run first.
///     </description>
///   </item>
///   <item>
///     <description>
///       Otherwise the global tool path is used. When <see cref="ToolRestore"/> evaluates to <c>true</c>
///       and <see cref="ToolPackageId"/> has a value, the task runs <c>dotnet tool update --global</c>
///       for the specified package (and optional <see cref="ToolVersion"/>), then invokes
///       <see cref="ToolCommand"/> directly.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The task always creates <see cref="WorkingDirectory"/> and <see cref="OutputDir"/> before invoking the
/// external tool. All paths passed to efcpt are absolute.
/// </para>
/// <para>
/// For test and troubleshooting scenarios, the following environment variables are honoured:
/// <list type="bullet">
///   <item>
///     <description>
///       <c>EFCPT_FAKE_EFCPT</c> - when set to a non-empty value, the task does not invoke any
///       external process. Instead it writes a single <c>SampleModel.cs</c> file into
///       <see cref="OutputDir"/> and returns success.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>EFCPT_TEST_DACPAC</c> - if present, its value is forwarded to the child process as an
///       environment variable of the same name. This is primarily used by the test suite.
///     </description>
///   </item>
/// </list>
/// These hooks are intended for testing and diagnostics and are not considered a stable public API.
/// </para>
/// </remarks>
public sealed class RunEfcpt : Task
{
    /// <summary>
    /// Controls how the efcpt dotnet tool is resolved.
    /// </summary>
    /// <value>
    /// One of:
    /// <list type="bullet">
    ///   <item><description><c>auto</c> (default) - use a local tool manifest if one is discovered by walking up from <see cref="WorkingDirectory"/>; otherwise fall back to the global tool.</description></item>
    ///   <item><description><c>tool-manifest</c> - require a local tool manifest; the task will run within the directory that contains <c>.config/dotnet-tools.json</c>.</description></item>
    ///   <item><description>Any other non-empty value behaves like the global tool mode but is reserved for future extension.</description></item>
    /// </list>
    /// </value>
    [Required] public string ToolMode { get; set; } = "auto";

    /// <summary>
    /// Package identifier of the efcpt dotnet tool used when restoring or updating the global tool.
    /// </summary>
    /// <value>
    /// Defaults to <c>ErikEJ.EFCorePowerTools.Cli</c>. Only used when <see cref="ToolMode"/> selects the
    /// global tool path and <see cref="ToolRestore"/> evaluates to <c>true</c>.
    /// </value>
    [Required] public string ToolPackageId { get; set; } = "ErikEJ.EFCorePowerTools.Cli";

    /// <summary>
    /// Optional version constraint for the efcpt tool package.
    /// </summary>
    /// <value>
    /// When non-empty and the task performs a global tool restore, the value is passed as a
    /// <c>--version</c> argument. When empty, the latest available version is used.
    /// </value>
    public string ToolVersion { get; set; } = "";

    /// <summary>
    /// Indicates whether the task should restore or update the dotnet tool before running it.
    /// </summary>
    /// <value>
    /// The value is interpreted case-insensitively. The strings <c>true</c>, <c>1</c>, and <c>yes</c>
    /// enable restore; any other value disables it. Defaults to <c>true</c>.
    /// </value>
    public string ToolRestore { get; set; } = "true";

    /// <summary>
    /// Name of the efcpt tool command to execute.
    /// </summary>
    /// <value>
    /// Defaults to <c>efcpt</c>. When running under a tool manifest, the command is executed via
    /// <c>dotnet tool run</c>. In global mode the command name is executed directly.
    /// </value>
    public string ToolCommand { get; set; } = "efcpt";

    /// <summary>
    /// Explicit path to the efcpt executable.
    /// </summary>
    /// <value>
    /// When non-empty and contains a rooted or relative directory component, this path is resolved
    /// against <see cref="WorkingDirectory"/> and executed directly, bypassing dotnet tool resolution.
    /// </value>
    public string ToolPath { get; set; } = "";

    /// <summary>
    /// Path to the <c>dotnet</c> host executable.
    /// </summary>
    /// <value>
    /// Defaults to <c>dotnet</c>. Used for <c>dotnet tool</c> operations and, where applicable,
    /// when invoking the tool via a manifest.
    /// </value>
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>
    /// Working directory for the efcpt invocation and manifest discovery.
    /// </summary>
    /// <value>
    /// Typically points at the intermediate output directory created by earlier pipeline stages.
    /// The directory is created if it does not already exist.
    /// </value>
    [Required] public string WorkingDirectory { get; set; } = "";

    /// <summary>
    /// Full path to the DACPAC file that efcpt will inspect.
    /// </summary>
    [Required] public string DacpacPath { get; set; } = "";

    /// <summary>
    /// Full path to the efcpt configuration JSON file.
    /// </summary>
    [Required] public string ConfigPath { get; set; } = "";

    /// <summary>
    /// Full path to the efcpt renaming JSON file.
    /// </summary>
    [Required] public string RenamingPath { get; set; } = "";

    /// <summary>
    /// Path to the template directory that contains the C# template files used by efcpt.
    /// </summary>
    [Required] public string TemplateDir { get; set; } = "";

    /// <summary>
    /// Directory where generated C# model files will be written.
    /// </summary>
    /// <value>
    /// The directory is created if it does not exist. Generated files are later renamed to
    /// <c>.g.cs</c> and added to compilation by the <c>EfcptAddToCompile</c> target.
    /// </value>
    [Required] public string OutputDir { get; set; } = "";

    /// <summary>
    /// Controls how much diagnostic information the task writes to the MSBuild log.
    /// </summary>
    /// <value>
    /// When set to <c>detailed</c> (case-insensitive), additional informational messages are emitted.
    /// Any other value results in a minimal log. Defaults to <c>minimal</c>.
    /// </value>
    public string LogVerbosity { get; set; } = "minimal";

    /// <summary>
    /// Database provider identifier passed to efcpt.
    /// </summary>
    /// <value>
    /// Defaults to <c>mssql</c>. The concrete set of supported providers is determined by the efcpt
    /// CLI version in use.
    /// </value>
    public string Provider { get; set; } = "mssql";

    /// <summary>
    /// Invokes the efcpt CLI against the specified DACPAC and configuration files.
    /// </summary>
    /// <returns>>True on success; false on error.</returns>
    public override bool Execute()
    {
        var log = new BuildLog(Log, LogVerbosity);

        try
        {
            log.Info($"Running in working directory {WorkingDirectory}: efcpt {BuildArgs()}");
            log.Info($"Output will be written to {OutputDir}");
            Directory.CreateDirectory(WorkingDirectory);
            Directory.CreateDirectory(OutputDir);

            var fake = Environment.GetEnvironmentVariable("EFCPT_FAKE_EFCPT");
            if (!string.IsNullOrWhiteSpace(fake))
            {
                var sample = Path.Combine(OutputDir, "SampleModel.cs");
                Directory.CreateDirectory(OutputDir);
                File.WriteAllText(sample, $"// generated from {DacpacPath}");
                log.Detail("EFCPT_FAKE_EFCPT set; wrote sample output.");
                return true;
            }

            var workingDir = Path.GetFullPath(WorkingDirectory);
            var args = BuildArgs();

            if (PathUtils.HasExplicitPath(ToolPath))
            {
                var command = PathUtils.FullPath(ToolPath, workingDir);
                RunProcess(log, command, args, workingDir);
                return true;
            }

            var manifestDir = FindManifestDir(workingDir);
            var useManifest = string.Equals(ToolMode, "tool-manifest", StringComparison.OrdinalIgnoreCase)
                              || (string.Equals(ToolMode, "auto", StringComparison.OrdinalIgnoreCase) && manifestDir is not null);

            if (useManifest)
            {
                if (IsTrue(ToolRestore))
                    RunProcess(log, DotNetExe, "tool restore", manifestDir ?? workingDir);

                var cmd = $"tool run {ToolCommand} -- {args}";
                RunProcess(log, DotNetExe, cmd, workingDir);
            }
            else
            {
                if (IsTrue(ToolRestore) && PathUtils.HasValue(ToolPackageId))
                {
                    var versionArg = string.IsNullOrWhiteSpace(ToolVersion) ? "" : $" --version \"{ToolVersion}\"";
                    RunProcess(log, DotNetExe, $"tool update --global {ToolPackageId}{versionArg}", workingDir);
                }

                RunProcess(log, ToolCommand, args, workingDir);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private static bool IsTrue(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1" || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private string BuildArgs()
        => $"\"{DacpacPath}\" {Provider} -i \"{ConfigPath}\" -r \"{RenamingPath}\"" + (WorkingDirectory.Equals(OutputDir) ? string.Empty : $" -o \"{OutputDir}\"");

    private static string? FindManifestDir(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var manifest = Path.Combine(dir.FullName, ".config", "dotnet-tools.json");
            if (File.Exists(manifest)) return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    private static void RunProcess(BuildLog log, string fileName, string args, string workingDir)
    {
        var (exe, finalArgs) = NormalizeCommand(fileName, args);
        log.Info($"> {exe} {finalArgs}");

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = finalArgs,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        var testDac = Environment.GetEnvironmentVariable("EFCPT_TEST_DACPAC");
        if (!string.IsNullOrWhiteSpace(testDac))
            psi.Environment["EFCPT_TEST_DACPAC"] = testDac;

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {exe}");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout)) log.Info(stdout);
        if (!string.IsNullOrWhiteSpace(stderr)) log.Error(stderr);

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"Process failed ({p.ExitCode}): {exe} {finalArgs}");
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
