using System.Collections;
using Microsoft.Build.Framework;

namespace JD.Efcpt.Build.Tests.Infrastructure;

internal sealed class TestBuildEngine : IBuildEngine
{
    public List<BuildErrorEventArgs> Errors { get; } = [];
    public List<BuildWarningEventArgs> Warnings { get; } = [];
    public List<BuildMessageEventArgs> Messages { get; } = [];

    public bool ContinueOnError => false;
    public int LineNumberOfTaskNode => 0;
    public int ColumnNumberOfTaskNode => 0;
    public string ProjectFileOfTaskNode => string.Empty;

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

    public void LogCustomEvent(CustomBuildEventArgs e) => Messages.Add(new BuildMessageEventArgs(e.Message, string.Empty, string.Empty, MessageImportance.Low));
    public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
    public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);
    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
}
