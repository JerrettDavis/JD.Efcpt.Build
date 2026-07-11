using System.CommandLine;
using JD.Efcpt.Cli.Commands;

var rootCommand = new RootCommand("jd-efcpt: CLI companion for JD.Efcpt.Build (init, doctor).")
{
    InitCommand.Build(),
    DoctorCommand.Build()
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
