using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tasks.ConnectionStrings;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.ConnectionStrings;

/// <summary>
/// Tests for the ConfigurationFileTypeValidator class.
/// </summary>
[Feature("ConfigurationFileTypeValidator: Validates configuration file types and logs warnings")]
[Collection(nameof(AssemblySetup))]
public sealed class ConfigurationFileTypeValidatorTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record ValidationContext(
        ConfigurationFileTypeValidator Validator,
        TestBuildEngine BuildEngine,
        BuildLog Log);

    private static ValidationContext CreateContext()
    {
        var buildEngine = new TestBuildEngine();
        var log = new BuildLog(buildEngine.TaskLoggingHelper, "minimal");
        return new ValidationContext(new ConfigurationFileTypeValidator(), buildEngine, log);
    }

    [Scenario("Warns when EfcptAppSettings receives a .config file")]
    [Fact]
    public async Task Warns_when_app_settings_receives_config_file()
    {
        await Given("a validator context", CreateContext)
            .When("validating .config file for EfcptAppSettings", ctx =>
            {
                ConfigurationFileTypeValidator.ValidateAndWarn("/path/to/app.config", "EfcptAppSettings", ctx.Log);
                return ctx;
            })
            .Then("logs a warning about file type mismatch", ctx =>
                ctx.BuildEngine.Warnings.Any(w => w.Message != null && w.Message.Contains("EfcptAppSettings received a .config file")))
            .And("suggests using EfcptAppConfig", ctx =>
                ctx.BuildEngine.Warnings.Any(w => w.Message != null && w.Message.Contains("Consider using EfcptAppConfig")))
            .AssertPassed();
    }

    [Scenario("Warns when EfcptAppConfig receives a .json file")]
    [Fact]
    public async Task Warns_when_app_config_receives_json_file()
    {
        await Given("a validator context", CreateContext)
            .When("validating .json file for EfcptAppConfig", ctx =>
            {
                ConfigurationFileTypeValidator.ValidateAndWarn("/path/to/appsettings.json", "EfcptAppConfig", ctx.Log);
                return ctx;
            })
            .Then("logs a warning about file type mismatch", ctx =>
                ctx.BuildEngine.Warnings.Any(w => w.Message != null && w.Message.Contains("EfcptAppConfig received a .json file")))
            .And("suggests using EfcptAppSettings", ctx =>
                ctx.BuildEngine.Warnings.Any(w => w.Message != null && w.Message.Contains("Consider using EfcptAppSettings")))
            .AssertPassed();
    }

    [Scenario("No warning when EfcptAppSettings receives a .json file")]
    [Fact]
    public async Task No_warning_when_app_settings_receives_json_file()
    {
        await Given("a validator context", CreateContext)
            .When("validating .json file for EfcptAppSettings", ctx =>
            {
                ConfigurationFileTypeValidator.ValidateAndWarn("/path/to/appsettings.json", "EfcptAppSettings", ctx.Log);
                return ctx;
            })
            .Then("no warnings logged", ctx => ctx.BuildEngine.Warnings.Count == 0)
            .AssertPassed();
    }

    [Scenario("No warning when EfcptAppConfig receives a .config file")]
    [Fact]
    public async Task No_warning_when_app_config_receives_config_file()
    {
        await Given("a validator context", CreateContext)
            .When("validating .config file for EfcptAppConfig", ctx =>
            {
                ConfigurationFileTypeValidator.ValidateAndWarn("/path/to/app.config", "EfcptAppConfig", ctx.Log);
                return ctx;
            })
            .Then("no warnings logged", ctx => ctx.BuildEngine.Warnings.Count == 0)
            .AssertPassed();
    }

    [Scenario("No warning for unknown file types")]
    [Theory]
    [InlineData("/path/to/settings.xml", "EfcptAppSettings")]
    [InlineData("/path/to/settings.xml", "EfcptAppConfig")]
    [InlineData("/path/to/settings.yaml", "EfcptAppSettings")]
    public async Task No_warning_for_unknown_file_types(string filePath, string parameterName)
    {
        await Given("a validator context", CreateContext)
            .When("validating unknown file type", ctx =>
            {
                ConfigurationFileTypeValidator.ValidateAndWarn(filePath, parameterName, ctx.Log);
                return ctx;
            })
            .Then("no warnings logged", ctx => ctx.BuildEngine.Warnings.Count == 0)
            .AssertPassed();
    }

    [Scenario("Handles case-insensitive extensions")]
    [Theory]
    [InlineData("/path/to/app.CONFIG", "EfcptAppSettings")]
    [InlineData("/path/to/appsettings.JSON", "EfcptAppConfig")]
    public async Task Handles_case_insensitive_extensions(string filePath, string parameterName)
    {
        await Given("a validator context", CreateContext)
            .When("validating file with mixed-case extension", ctx =>
            {
                ConfigurationFileTypeValidator.ValidateAndWarn(filePath, parameterName, ctx.Log);
                return ctx;
            })
            .Then("logs appropriate warning", ctx => ctx.BuildEngine.Warnings.Count == 1)
            .AssertPassed();
    }
}
