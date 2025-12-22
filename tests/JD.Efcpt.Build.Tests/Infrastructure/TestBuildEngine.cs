using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JD.Efcpt.Build.Tests.Infrastructure;

internal sealed class TestBuildEngine : IBuildEngine
{
    private readonly Lazy<TaskLoggingHelper> _loggingHelper;

    public TestBuildEngine()
    {
        _loggingHelper = new Lazy<TaskLoggingHelper>(() =>
        {
            var task = new TestTask { BuildEngine = this };
            return new TaskLoggingHelper(task);
        });
    }

    public List<BuildErrorEventArgs> Errors { get; } = [];
    public List<BuildWarningEventArgs> Warnings { get; } = [];
    public List<BuildMessageEventArgs> Messages { get; } = [];

    /// <summary>
    /// Gets a TaskLoggingHelper instance for use with BuildLog tests.
    /// </summary>
    public TaskLoggingHelper TaskLoggingHelper => _loggingHelper.Value;

    public bool ContinueOnError => false;
    public int LineNumberOfTaskNode => 0;
    public int ColumnNumberOfTaskNode => 0;
    public string ProjectFileOfTaskNode => string.Empty;

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

    public void LogCustomEvent(CustomBuildEventArgs e) => Messages.Add(new BuildMessageEventArgs(e.Message, string.Empty, string.Empty, MessageImportance.Low));
    public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
    public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);
    public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);

    /// <summary>
    /// Minimal task implementation to satisfy TaskLoggingHelper requirements.
    /// </summary>
    private sealed class TestTask : ITask
    {
        public IBuildEngine? BuildEngine { get; set; }
        public ITaskHost? HostObject { get; set; }
        public bool Execute() => true;
    }
}
