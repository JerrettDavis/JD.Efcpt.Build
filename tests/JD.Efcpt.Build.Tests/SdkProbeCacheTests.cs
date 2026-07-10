using System.Threading;
using JD.Efcpt.Build.Tasks.Utilities;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the SdkProbeCache class that memoizes SDK/dnx capability probes so that
/// expensive <c>Process.Start</c> invocations only run once per key within a build session.
/// </summary>
[Feature("SdkProbeCache: memoized SDK/dnx capability probes")]
[Collection(nameof(AssemblySetup))]
public sealed partial class SdkProbeCacheTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("GetOrProbe invokes the factory exactly once for repeated calls with the same key")]
    [Fact]
    public async Task GetOrProbe_invokes_factory_once_for_same_key()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        bool Probe()
        {
            Interlocked.Increment(ref counter);
            return true;
        }

        await Given("a probe factory and a fixed probeName/dotnetExe key", () => (ProbeName: "list-sdks", DotnetExe: "C:\\fake\\dotnet.exe"))
            .When("GetOrProbe is invoked twice with the same key", key =>
            {
                var first = SdkProbeCache.GetOrProbe(key.ProbeName, key.DotnetExe, Probe);
                var second = SdkProbeCache.GetOrProbe(key.ProbeName, key.DotnetExe, Probe);
                return (First: first, Second: second);
            })
            .Then("both results equal the factory's return value", r => r is { First: true, Second: true })
            .And("the factory ran exactly once", _ => counter == 1)
            .AssertPassed();
    }

    [Scenario("GetOrProbe re-invokes the factory when dotnetExe differs")]
    [Fact]
    public async Task GetOrProbe_reinvokes_factory_for_different_dotnet_exe()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        bool Probe()
        {
            Interlocked.Increment(ref counter);
            return true;
        }

        await Given("a probe factory and the same probeName", () => "list-sdks")
            .When("GetOrProbe is invoked with two different dotnetExe paths", probeName =>
            {
                SdkProbeCache.GetOrProbe(probeName, "C:\\fake\\dotnet-a.exe", Probe);
                SdkProbeCache.GetOrProbe(probeName, "C:\\fake\\dotnet-b.exe", Probe);
                return counter;
            })
            .Then("the factory ran twice", c => c == 2)
            .AssertPassed();
    }

    [Scenario("GetOrProbe re-invokes the factory when probeName differs")]
    [Fact]
    public async Task GetOrProbe_reinvokes_factory_for_different_probe_name()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        bool Probe()
        {
            Interlocked.Increment(ref counter);
            return true;
        }

        await Given("a fixed dotnetExe and two different probe names", () => "C:\\fake\\dotnet.exe")
            .When("GetOrProbe is invoked with two different probeNames", dotnetExe =>
            {
                SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, Probe);
                SdkProbeCache.GetOrProbe("dnx-help", dotnetExe, Probe);
                return counter;
            })
            .Then("the factory ran twice", c => c == 2)
            .AssertPassed();
    }

    [Scenario("Clear resets the cache so the next call re-invokes the factory")]
    [Fact]
    public async Task Clear_resets_cache_for_next_call()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        bool Probe()
        {
            Interlocked.Increment(ref counter);
            return true;
        }

        await Given("a probe factory invoked once", () => "list-sdks")
            .When("GetOrProbe is called, then Clear, then GetOrProbe again", probeName =>
            {
                SdkProbeCache.GetOrProbe(probeName, "C:\\fake\\dotnet.exe", Probe);
                SdkProbeCache.Clear();
                SdkProbeCache.GetOrProbe(probeName, "C:\\fake\\dotnet.exe", Probe);
                return counter;
            })
            .Then("the factory ran twice (once per cache generation)", c => c == 2)
            .AssertPassed();
    }

    [Scenario("GetOrProbe is thread-safe: concurrent calls with the same key invoke the factory exactly once")]
    [Fact]
    public async Task GetOrProbe_is_thread_safe_for_same_key()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        bool Probe()
        {
            Interlocked.Increment(ref counter);
            return true;
        }

        await Given("a probe factory and a fixed key", () => "C:\\fake\\dotnet.exe")
            .When("100 iterations call GetOrProbe in parallel with the same key", dotnetExe =>
            {
                var results = new bool[100];
                Parallel.For(0, 100, i =>
                {
                    results[i] = SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, Probe);
                });
                return results;
            })
            .Then("all threads observe the same (true) result", results => results.All(r => r))
            .And("the factory ran exactly once", _ => counter == 1)
            .AssertPassed();
    }
}
