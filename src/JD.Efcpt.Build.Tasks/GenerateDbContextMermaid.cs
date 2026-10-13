using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Runs the CLI extractor against a built project assembly and writes its code-first Mermaid model.
/// </summary>
public sealed class GenerateDbContextMermaid : Task
{
    /// <summary>Path to the dotnet host.</summary>
    [Required]
    public string DotNetExe { get; set; } = "dotnet";

    /// <summary>Path to the packaged CLI assembly.</summary>
    [Required]
    public string CliPath { get; set; } = "";

    /// <summary>Path to the built assembly containing the DbContext.</summary>
    [Required]
    public string AssemblyPath { get; set; } = "";

    /// <summary>Optional DbContext type name used to disambiguate multiple contexts.</summary>
    public string ContextName { get; set; } = "";

    /// <summary>Optional startup assembly used for design-time discovery.</summary>
    public string StartupAssemblyPath { get; set; } = "";

    /// <summary>Path to the generated Mermaid Markdown file.</summary>
    [Required]
    public string OutputPath { get; set; } = "";

    /// <summary>Working directory used to create the design-time context.</summary>
    [Required]
    public string WorkingDirectory { get; set; } = "";

    /// <inheritdoc />
    public override bool Execute()
    {
        try
        {
            var arguments = new List<string>
            {
                CliPath,
                "mermaid-from-dbcontext",
                "--assembly",
                AssemblyPath
            };
            if (!string.IsNullOrWhiteSpace(ContextName))
            {
                arguments.Add("--context");
                arguments.Add(ContextName);
            }
            if (!string.IsNullOrWhiteSpace(StartupAssemblyPath))
            {
                arguments.Add("--startup-assembly");
                arguments.Add(StartupAssemblyPath);
            }
            arguments.Add("--output");
            arguments.Add(OutputPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = DotNetExe,
                WorkingDirectory = WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
#if NETFRAMEWORK
            startInfo.Arguments = string.Join(" ", arguments.Select(QuoteWindowsArgument));
#else
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);
#endif

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                Log.LogError("Unable to start the dotnet process for code-first Mermaid generation.");
                return false;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(180000))
            {
                process.Kill();
                Log.LogError("Code-first Mermaid generation did not finish within three minutes.");
                return false;
            }

            System.Threading.Tasks.Task.WaitAll(stdoutTask, stderrTask);
            if (!string.IsNullOrWhiteSpace(stdoutTask.Result))
                Log.LogMessage(MessageImportance.High, stdoutTask.Result.TrimEnd());
            if (process.ExitCode != 0)
            {
                Log.LogError(string.IsNullOrWhiteSpace(stderrTask.Result)
                    ? $"Code-first Mermaid generation failed with exit code {process.ExitCode}."
                    : stderrTask.Result.TrimEnd());
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception, showStackTrace: false);
            return false;
        }
    }

#if NETFRAMEWORK
    private static string QuoteWindowsArgument(string argument)
    {
        var result = new StringBuilder(argument.Length + 2).Append('"');
        var backslashes = 0;
        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                result.Append('\\', (backslashes * 2) + 1).Append('"');
                backslashes = 0;
                continue;
            }

            result.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        result.Append('\\', backslashes * 2).Append('"');
        return result.ToString();
    }
#endif
}
