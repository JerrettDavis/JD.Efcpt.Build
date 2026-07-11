using System.Threading;
using JD.Efcpt.Build.Core.Diagnostics;
using JD.Efcpt.Build.Tests.Infrastructure;
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
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
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
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
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
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
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
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
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
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
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

    [Scenario("GetOrProbe re-invokes the factory when the muxer file's last-write-time changes")]
    [Fact]
    public async Task GetOrProbe_reinvokes_factory_when_muxer_mtime_changes()
    {
        SdkProbeCache.Clear();
        using var folder = new TestFolder();
        var muxerPath = folder.WriteFile("dotnet.exe", "stub-v1");

        var counter = 0;
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
        }

        await Given("a temp file standing in for the dotnet muxer binary", () => muxerPath)
            .When("GetOrProbe is called, the muxer file's mtime is advanced, then GetOrProbe is called again", path =>
            {
                var first = SdkProbeCache.GetOrProbe("list-sdks", path, Probe);

                // Advance the last-write-time so the cache key's mtime stamp component changes,
                // simulating an SDK upgrade that replaces the muxer binary mid-session.
                File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(5));

                var second = SdkProbeCache.GetOrProbe("list-sdks", path, Probe);
                return (First: first, Second: second);
            })
            .Then("both calls report the factory's result", r => r is { First: true, Second: true })
            .And("the factory ran twice - once per distinct muxer mtime", _ => counter == 2)
            .AssertPassed();
    }

    [Scenario("GetOrProbe tolerates a bare (unqualified) dotnetExe command without throwing")]
    [Fact]
    public async Task GetOrProbe_does_not_throw_for_bare_dotnet_exe()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        ProbeOutcome Probe()
        {
            Interlocked.Increment(ref counter);
            return ProbeOutcome.Available;
        }

        await Given("a bare 'dotnet' command with no directory separator", () => "dotnet")
            .When("GetOrProbe is invoked", bareExe => SdkProbeCache.GetOrProbe("list-sdks", bareExe, Probe))
            .Then("no exception propagates and the factory's result is returned", result => result)
            .And("the factory ran exactly once", _ => counter == 1)
            .AssertPassed();
    }

    [Scenario("ResolveDotnetExecutable does not throw for a bare command and either resolves it via PATH or falls back to null")]
    [Fact]
    public async Task ResolveDotnetExecutable_is_safe_for_bare_command()
    {
        await Given("a bare 'dotnet' command", () => "dotnet")
            .When("ResolveDotnetExecutable is invoked", SdkProbeCache.ResolveDotnetExecutable)
            .Then("the result is either a resolved existing file or null (unresolved)", resolved => resolved is null || File.Exists(resolved))
            .AssertPassed();
    }

    [Scenario("ResolveDotnetExecutable returns null for a null or empty dotnetExe")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ResolveDotnetExecutable_returns_null_for_null_or_empty(string? dotnetExe)
    {
        await Given("a null or empty dotnetExe", () => dotnetExe)
            .When("ResolveDotnetExecutable is invoked", SdkProbeCache.ResolveDotnetExecutable)
            .Then("returns null", result => result is null)
            .AssertPassed();
    }

    [Scenario("ResolveDotnetExecutable returns null for a bare command when PATH is empty")]
    [Fact]
    public async Task ResolveDotnetExecutable_returns_null_when_path_is_empty()
    {
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            await Given("PATH cleared and a bare command", () =>
                {
                    Environment.SetEnvironmentVariable("PATH", string.Empty);
                    return "dotnet";
                })
                .When("ResolveDotnetExecutable is invoked", SdkProbeCache.ResolveDotnetExecutable)
                .Then("returns null", result => result is null)
                .AssertPassed();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Scenario("ResolveDotnetExecutable skips blank PATH entries and still resolves a later match")]
    [Fact]
    public async Task ResolveDotnetExecutable_skips_blank_path_entries()
    {
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        using var folder = new TestFolder();
        try
        {
            var stubPath = folder.WriteFile("dotnet.exe", "stub");
            var stubDir = Path.GetDirectoryName(stubPath)!;

            await Given("a PATH with a leading blank entry followed by a directory containing the stub", () =>
                {
                    Environment.SetEnvironmentVariable("PATH", $"{Path.PathSeparator}{stubDir}");
                    return "dotnet";
                })
                .When("ResolveDotnetExecutable is invoked", SdkProbeCache.ResolveDotnetExecutable)
                .Then("resolves to the stub file", result => result == Path.GetFullPath(stubPath))
                .AssertPassed();
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Scenario("GetOrProbe does not cache a transient probe failure, so the next call retries")]
    [Fact]
    public async Task GetOrProbe_does_not_cache_transient_failure()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        ProbeOutcome Probe()
        {
            var invocation = Interlocked.Increment(ref counter);
            return invocation == 1 ? ProbeOutcome.Transient : ProbeOutcome.Available;
        }

        await Given("a probe factory that fails transiently on its first call and succeeds on its second", () => "C:\\fake\\dotnet-transient.exe")
            .When("GetOrProbe is invoked twice with the same key", dotnetExe =>
            {
                var first = SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, Probe);
                var second = SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, Probe);
                return (First: first, Second: second);
            })
            .Then("the first call reports a negative result without caching it", r => r.First == false)
            .And("the second call reports success", r => r.Second)
            .And("the factory was invoked twice - the transient result was not cached", _ => counter == 2)
            .AssertPassed();
    }

    [Scenario("GetOrProbe treats a thrown exception as a transient failure and does not poison the cache")]
    [Fact]
    public async Task GetOrProbe_does_not_poison_cache_when_probe_throws()
    {
        SdkProbeCache.Clear();
        var counter = 0;
        ProbeOutcome Probe()
        {
            var invocation = Interlocked.Increment(ref counter);
            if (invocation == 1)
                throw new InvalidOperationException("simulated probe failure");

            return ProbeOutcome.Available;
        }

        await Given("a probe factory that throws on its first call and succeeds on its second", () => "C:\\fake\\dotnet-throws.exe")
            .When("GetOrProbe is invoked twice with the same key", dotnetExe =>
            {
                var first = SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, Probe);
                var second = SdkProbeCache.GetOrProbe("list-sdks", dotnetExe, Probe);
                return (First: first, Second: second);
            })
            .Then("the first call reports a negative result without throwing or caching it", r => r.First == false)
            .And("the second call reports the real (successful) outcome", r => r.Second)
            .And("the factory was invoked twice - the thrown exception was not memoized", _ => counter == 2)
            .AssertPassed();
    }
}
