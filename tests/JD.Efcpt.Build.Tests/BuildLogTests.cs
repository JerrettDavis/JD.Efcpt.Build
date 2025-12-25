using JD.Efcpt.Build.Tests.Infrastructure;
using Microsoft.Build.Framework;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the BuildLog wrapper class that handles MSBuild logging with verbosity control.
/// </summary>
[Feature("BuildLog: MSBuild logging with verbosity control")]
[Collection(nameof(AssemblySetup))]
public sealed class BuildLogTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record SetupState(TestBuildEngine Engine);

    private static SetupState Setup() => new(new TestBuildEngine());

    [Scenario("Info logs message with high importance")]
    [Fact]
    public async Task Info_logs_with_high_importance()
    {
        await Given("a build engine", Setup)
            .When("Info is called", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "minimal");
                log.Info("Test info message");
                return s;
            })
            .Then("message is logged", s =>
                s.Engine.Messages.Any(m => m.Message == "Test info message"))
            .And("importance is high", s =>
                s.Engine.Messages.Any(m => m.Message == "Test info message" && m.Importance == MessageImportance.High))
            .AssertPassed();
    }

    [Scenario("Detail logs message when verbosity is detailed")]
    [Fact]
    public async Task Detail_logs_when_verbosity_detailed()
    {
        await Given("a build engine", Setup)
            .When("Detail is called with detailed verbosity", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "detailed");
                log.Detail("Detailed message");
                return s;
            })
            .Then("message is logged", s =>
                s.Engine.Messages.Any(m => m.Message == "Detailed message"))
            .AssertPassed();
    }

    [Scenario("Detail does not log when verbosity is minimal")]
    [Fact]
    public async Task Detail_skipped_when_verbosity_minimal()
    {
        await Given("a build engine", Setup)
            .When("Detail is called with minimal verbosity", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "minimal");
                log.Detail("Should not appear");
                return s;
            })
            .Then("message is not logged", s => s.Engine.Messages.All(m => m.Message != "Should not appear"))
            .AssertPassed();
    }

    [Scenario("Detail does not log when verbosity is empty")]
    [Fact]
    public async Task Detail_skipped_when_verbosity_empty()
    {
        await Given("a build engine", Setup)
            .When("Detail is called with empty verbosity", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "");
                log.Detail("Should not appear");
                return s;
            })
            .Then("message is not logged", s => s.Engine.Messages.All(m => m.Message != "Should not appear"))
            .AssertPassed();
    }

    [Scenario("Detail does not log when verbosity is null equivalent")]
    [Fact]
    public async Task Detail_skipped_when_verbosity_whitespace()
    {
        await Given("a build engine", Setup)
            .When("Detail is called with whitespace verbosity", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "   ");
                log.Detail("Should not appear");
                return s;
            })
            .Then("message is not logged", s => s.Engine.Messages.All(m => m.Message != "Should not appear"))
            .AssertPassed();
    }

    [Scenario("Detail is case-insensitive for verbosity")]
    [Fact]
    public async Task Detail_verbosity_case_insensitive()
    {
        await Given("a build engine", Setup)
            .When("Detail is called with DETAILED verbosity", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "DETAILED");
                log.Detail("Case insensitive message");
                return s;
            })
            .Then("message is logged", s =>
                s.Engine.Messages.Any(m => m.Message == "Case insensitive message"))
            .AssertPassed();
    }

    [Scenario("Warn logs warning message")]
    [Fact]
    public async Task Warn_logs_warning()
    {
        await Given("a build engine", Setup)
            .When("Warn is called", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "minimal");
                log.Warn("Test warning");
                return s;
            })
            .Then("warning is logged", s =>
                s.Engine.Warnings.Any(w => w.Message == "Test warning"))
            .AssertPassed();
    }

    [Scenario("Warn logs warning with code")]
    [Fact]
    public async Task Warn_logs_warning_with_code()
    {
        await Given("a build engine", Setup)
            .When("Warn with code is called", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "minimal");
                log.Warn("EFCPT001", "Warning with code");
                return s;
            })
            .Then("warning is logged", s =>
                s.Engine.Warnings.Any(w => w.Message == "Warning with code"))
            .And("warning has code", s =>
                s.Engine.Warnings.Any(w => w.Code == "EFCPT001"))
            .AssertPassed();
    }

    [Scenario("Error logs error message")]
    [Fact]
    public async Task Error_logs_error()
    {
        await Given("a build engine", Setup)
            .When("Error is called", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "minimal");
                log.Error("Test error");
                return s;
            })
            .Then("error is logged", s =>
                s.Engine.Errors.Any(e => e.Message == "Test error"))
            .AssertPassed();
    }

    [Scenario("Error logs error with code")]
    [Fact]
    public async Task Error_logs_error_with_code()
    {
        await Given("a build engine", Setup)
            .When("Error with code is called", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "minimal");
                log.Error("EFCPT002", "Error with code");
                return s;
            })
            .Then("error is logged", s =>
                s.Engine.Errors.Any(e => e.Message == "Error with code"))
            .And("error has code", s =>
                s.Engine.Errors.Any(e => e.Code == "EFCPT002"))
            .AssertPassed();
    }

    [Scenario("Multiple messages can be logged")]
    [Fact]
    public async Task Multiple_messages_logged()
    {
        await Given("a build engine", Setup)
            .When("multiple log methods are called", s =>
            {
                var log = new Tasks.BuildLog(s.Engine.TaskLoggingHelper, "detailed");
                log.Info("Info 1");
                log.Info("Info 2");
                log.Detail("Detail 1");
                log.Warn("Warning 1");
                log.Error("Error 1");
                return s;
            })
            .Then("all info messages logged", s =>
                s.Engine.Messages.Count(m => m.Message?.StartsWith("Info") == true) == 2)
            .And("detail message logged", s =>
                s.Engine.Messages.Any(m => m.Message == "Detail 1"))
            .And("warning logged", s =>
                s.Engine.Warnings.Count == 1)
            .And("error logged", s =>
                s.Engine.Errors.Count == 1)
            .AssertPassed();
    }
}

/// <summary>
/// Tests for the NullBuildLog no-op implementation.
/// </summary>
[Feature("NullBuildLog: no-op logging for testing")]
[Collection(nameof(AssemblySetup))]
public sealed class NullBuildLogTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("NullBuildLog.Instance is singleton")]
    [Fact]
    public async Task Instance_is_singleton()
    {
        await Given("the NullBuildLog class", () => true)
            .When("accessing Instance twice", _ =>
            {
                var first = Tasks.NullBuildLog.Instance;
                var second = Tasks.NullBuildLog.Instance;
                return (first, second);
            })
            .Then("same instance is returned", r => ReferenceEquals(r.first, r.second))
            .AssertPassed();
    }

    [Scenario("Info does not throw")]
    [Fact]
    public async Task Info_does_not_throw()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("Info is called", log =>
            {
                log.Info("Test message");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("Detail does not throw")]
    [Fact]
    public async Task Detail_does_not_throw()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("Detail is called", log =>
            {
                log.Detail("Detailed message");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("Warn does not throw")]
    [Fact]
    public async Task Warn_does_not_throw()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("Warn is called", log =>
            {
                log.Warn("Warning message");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("Warn with code does not throw")]
    [Fact]
    public async Task Warn_with_code_does_not_throw()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("Warn with code is called", log =>
            {
                log.Warn("CODE001", "Warning with code");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("Error does not throw")]
    [Fact]
    public async Task Error_does_not_throw()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("Error is called", log =>
            {
                log.Error("Error message");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("Error with code does not throw")]
    [Fact]
    public async Task Error_with_code_does_not_throw()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("Error with code is called", log =>
            {
                log.Error("CODE002", "Error with code");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("All methods can be called in sequence")]
    [Fact]
    public async Task All_methods_can_be_called_in_sequence()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("all methods are called", log =>
            {
                log.Info("Info");
                log.Detail("Detail");
                log.Warn("Warn");
                log.Warn("CODE", "Warn with code");
                log.Error("Error");
                log.Error("CODE", "Error with code");
                return true;
            })
            .Then("no exception is thrown", success => success)
            .AssertPassed();
    }

    [Scenario("NullBuildLog implements IBuildLog")]
    [Fact]
    public async Task Implements_IBuildLog()
    {
        await Given("a NullBuildLog instance", () => Tasks.NullBuildLog.Instance)
            .When("checking interface", log => log is Tasks.IBuildLog)
            .Then("implements IBuildLog", result => result)
            .AssertPassed();
    }
}
