using JD.Efcpt.Build.Tasks.Decorators;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Decorators;

/// <summary>
/// Tests for profile decorator attributes used in MSBuild property definitions.
/// </summary>
[Feature("ProfileAttribute: Decorators for mapping MSBuild properties to config overrides")]
public sealed class ProfileAttributeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("ProfileInputAttribute has default values")]
    [Fact]
    public async Task ProfileInputAttribute_defaults()
    {
        await Given("ProfileInputAttribute constructed", () => new ProfileInputAttribute())
            .Then("Exclude is false by default", attr => attr.Exclude == false)
            .AssertPassed();
    }

    [Scenario("ProfileInputAttribute with Exclude=true sets property")]
    [Fact]
    public async Task ProfileInputAttribute_with_exclude()
    {
        await Given("ProfileInputAttribute with Exclude", () => new ProfileInputAttribute { Exclude = true })
            .Then("Exclude is true", attr => attr.Exclude)
            .AssertPassed();
    }

    [Scenario("ProfileInputAttribute with custom name sets Name property")]
    [Fact]
    public async Task ProfileInputAttribute_with_custom_name()
    {
        const string customName = "CustomProperty";
        await Given("ProfileInputAttribute with name", () => new ProfileInputAttribute { Name = customName })
            .Then("Name matches", attr => attr.Name == customName)
            .AssertPassed();
    }

    [Scenario("ProfileOutputAttribute can be instantiated")]
    [Fact]
    public async Task ProfileOutputAttribute_instantiates()
    {
        await Given("ProfileOutputAttribute created", () => new ProfileOutputAttribute())
            .Then("instance is not null", attr => attr != null)
            .AssertPassed();
    }

    [Scenario("ProfileOutputAttribute can be applied to properties")]
    [Fact]
    public async Task ProfileOutputAttribute_applies_to_properties()
    {
        await Given("class with ProfileOutput attribute", () =>
            {
                var type = typeof(TestClassWithProfileOutput);
                var prop = type.GetProperty(nameof(TestClassWithProfileOutput.Output));
                var attr = prop?.GetCustomAttributes(typeof(ProfileOutputAttribute), false).FirstOrDefault();
                return attr;
            })
            .Then("attribute is found", attr => attr is ProfileOutputAttribute)
            .AssertPassed();
    }

    // Helper class for testing attribute application
    private class TestClassWithProfileOutput
    {
        [ProfileOutput]
        public string? Output { get; set; }
    }
}
