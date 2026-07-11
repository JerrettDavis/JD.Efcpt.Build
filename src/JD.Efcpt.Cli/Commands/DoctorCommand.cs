using System.CommandLine;
using JD.Efcpt.Build.Core.Diagnostics;
using JD.Efcpt.Cli.Logging;

namespace JD.Efcpt.Cli.Commands;

/// <summary>
/// <c>jd-efcpt doctor</c>: reports how the efcpt MSBuild pipeline would resolve and run the
/// efcpt tool for a given target framework, without a full build.
/// </summary>
/// <remarks>
/// A thin CLI wrapper around <see cref="DoctorEngine.Diagnose"/> - the same engine
/// <c>JD.Efcpt.Build.Tasks.EfcptDoctor</c> uses, so this command's report can never drift out of
/// sync with what a real build would do.
/// </remarks>
public static class DoctorCommand
{
    /// <summary>Exit code when a viable execution path was found.</summary>
    public const int ExitViable = 0;

    /// <summary>Exit code when no viable execution path was found but <c>--strict</c> was not passed (advisory only).</summary>
    public const int ExitNoViablePathAdvisory = 2;

    /// <summary>Exit code when no viable execution path was found and <c>--strict</c> was passed.</summary>
    public const int ExitNoViablePathStrict = 1;

    /// <summary>Builds the <c>doctor</c> subcommand.</summary>
    public static Command Build()
    {
        var targetFrameworkOption = new Option<string>("--target-framework")
        {
            Description = "Target framework to diagnose (e.g. net8.0, net10.0).",
            DefaultValueFactory = _ => ""
        };

        var toolModeOption = new Option<string>("--tool-mode")
        {
            Description = "Tool resolution mode: auto, tool-manifest, or global.",
            DefaultValueFactory = _ => "auto"
        };

        var toolPackageIdOption = new Option<string>("--tool-package-id")
        {
            Description = "The dotnet tool package id.",
            DefaultValueFactory = _ => "ErikEJ.EFCorePowerTools.Cli"
        };

        var toolCommandOption = new Option<string>("--tool-command")
        {
            Description = "The tool command name.",
            DefaultValueFactory = _ => "efcpt"
        };

        var toolPathOption = new Option<string>("--tool-path")
        {
            Description = "Explicit path to the efcpt executable, bypassing automatic resolution.",
            DefaultValueFactory = _ => ""
        };

        var offlineOption = new Option<bool>("--offline")
        {
            Description = "Diagnose as if EfcptOfflineMode were enabled."
        };

        var autoAcquireOption = new Option<bool>("--auto-acquire")
        {
            Description = "Diagnose as if EfcptAutoAcquireTool were enabled.",
            DefaultValueFactory = _ => true
        };

        var strictOption = new Option<bool>("--strict")
        {
            Description = "Exit with code 1 (instead of 2) when no viable execution path is found."
        };

        var workingDirOption = new Option<string>("--working-dir")
        {
            Description = "Working directory used for tool-manifest discovery (defaults to the current directory).",
            DefaultValueFactory = _ => ""
        };

        var command = new Command("doctor", "Diagnose how the efcpt tool would be resolved and run.")
        {
            targetFrameworkOption,
            toolModeOption,
            toolPackageIdOption,
            toolCommandOption,
            toolPathOption,
            offlineOption,
            autoAcquireOption,
            strictOption,
            workingDirOption
        };

        command.SetAction(parseResult =>
        {
            var log = new ConsoleBuildLog();
            var inputs = new DoctorInputs(
                TargetFramework: parseResult.GetValue(targetFrameworkOption) ?? "",
                ToolMode: parseResult.GetValue(toolModeOption) ?? "auto",
                ToolPackageId: parseResult.GetValue(toolPackageIdOption) ?? "ErikEJ.EFCorePowerTools.Cli",
                ToolVersion: "",
                ToolCommand: parseResult.GetValue(toolCommandOption) ?? "efcpt",
                ToolPath: parseResult.GetValue(toolPathOption) ?? "",
                DotNetExe: "dotnet",
                WorkingDirectory: parseResult.GetValue(workingDirOption) ?? "",
                Offline: parseResult.GetValue(offlineOption),
                AutoAcquire: parseResult.GetValue(autoAcquireOption),
                Strict: parseResult.GetValue(strictOption));

            return Execute(log, inputs, new DefaultSdkProbe());
        });

        return command;
    }

    /// <summary>
    /// Runs the <c>doctor</c> diagnosis and prints its report. Exposed separately from
    /// <see cref="Build"/> so tests can drive it directly with a fake <see cref="ISdkProbe"/>,
    /// without spawning any process.
    /// </summary>
    /// <returns>
    /// <see cref="ExitViable"/> (0) if a viable path was found; <see cref="ExitNoViablePathStrict"/>
    /// (1) if none was found and <see cref="DoctorInputs.Strict"/> is <see langword="true"/>;
    /// otherwise <see cref="ExitNoViablePathAdvisory"/> (2).
    /// </returns>
    public static int Execute(ConsoleBuildLog log, DoctorInputs inputs, ISdkProbe probe)
    {
        var (verdict, hasViablePath, messages) = DoctorEngine.Diagnose(inputs, probe);

        foreach (var message in messages)
            log.Info(message);

        if (hasViablePath)
            return ExitViable;

        log.Error(verdict);
        return inputs.Strict ? ExitNoViablePathStrict : ExitNoViablePathAdvisory;
    }
}
