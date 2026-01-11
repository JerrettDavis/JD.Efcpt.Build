using JD.Efcpt.Build.Tasks.Profiling;
using System.Text.Json;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests.Profiling;

/// <summary>
/// Tests for the JsonTimeSpanConverter class that serializes TimeSpan to ISO 8601 duration format.
/// </summary>
[Feature("JsonTimeSpanConverter: TimeSpan JSON serialization")]
[Collection(nameof(AssemblySetup))]
public sealed class JsonTimeSpanConverterTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed class TestObject
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan Duration { get; set; }
    }

    [Scenario("TimeSpan is serialized to ISO 8601 duration format")]
    [Fact]
    public async Task TimeSpan_is_serialized_to_iso8601()
    {
        var obj = new TestObject { Duration = TimeSpan.FromMinutes(1.5) };
        string json = string.Empty;

        await Given("an object with a TimeSpan", () => obj)
            .When("object is serialized to JSON", o =>
            {
                json = JsonSerializer.Serialize(o);
                return o;
            })
            .Then("JSON contains ISO 8601 duration", _ =>
                json.Contains("PT1M30S") || json.Contains("PT"))
            .AssertPassed();
    }

    [Scenario("ISO 8601 duration is deserialized to TimeSpan")]
    [Fact]
    public async Task Iso8601_is_deserialized_to_timespan()
    {
        var json = """{"Duration":"PT1M30S"}""";
        TestObject? obj = null;

        await Given("JSON with ISO 8601 duration", () => json)
            .When("JSON is deserialized", j =>
            {
                obj = JsonSerializer.Deserialize<TestObject>(j);
                return j;
            })
            .Then("TimeSpan is correctly parsed", _ =>
                obj != null && obj.Duration == TimeSpan.FromSeconds(90))
            .AssertPassed();
    }

    [Scenario("Zero duration is handled correctly")]
    [Fact]
    public async Task Zero_duration_is_handled()
    {
        var obj = new TestObject { Duration = TimeSpan.Zero };
        string json = string.Empty;
        TestObject? deserialized = null;

        await Given("an object with zero duration", () => obj)
            .When("object is serialized and deserialized", o =>
            {
                json = JsonSerializer.Serialize(o);
                deserialized = JsonSerializer.Deserialize<TestObject>(json);
                return o;
            })
            .Then("duration remains zero", _ =>
                deserialized != null && deserialized.Duration == TimeSpan.Zero)
            .AssertPassed();
    }
}
