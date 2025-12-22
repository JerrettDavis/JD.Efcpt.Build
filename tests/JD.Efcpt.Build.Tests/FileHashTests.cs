using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the FileHash utility class that provides XxHash64-based hashing.
/// </summary>
[Feature("FileHash: XxHash64-based hashing utilities")]
[Collection(nameof(AssemblySetup))]
public sealed class FileHashTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("HashString produces deterministic 16-character hex output")]
    [Fact]
    public async Task HashString_produces_deterministic_hex_output()
    {
        await Given("a test string", () => "Hello, World!")
            .When("hash is computed", FileHash.HashString)
            .Then("hash is 16 characters", h => h.Length == 16)
            .And("hash contains only hex characters", h => h.All(c => char.IsAsciiHexDigit(c)))
            .And("hash is deterministic", h =>
            {
                var secondHash = FileHash.HashString("Hello, World!");
                return h == secondHash;
            })
            .AssertPassed();
    }

    [Scenario("HashString produces different hashes for different inputs")]
    [Fact]
    public async Task HashString_different_inputs_produce_different_hashes()
    {
        await Given("two different strings", () => ("Hello", "World"))
            .When("hashes are computed", t => (FileHash.HashString(t.Item1), FileHash.HashString(t.Item2)))
            .Then("hashes are different", t => t.Item1 != t.Item2)
            .AssertPassed();
    }

    [Scenario("HashString handles empty string")]
    [Fact]
    public async Task HashString_handles_empty_string()
    {
        await Given("an empty string", () => "")
            .When("hash is computed", FileHash.HashString)
            .Then("hash is 16 characters", h => h.Length == 16)
            .And("hash is deterministic", h => h == FileHash.HashString(""))
            .AssertPassed();
    }

    [Scenario("HashString handles unicode content")]
    [Fact]
    public async Task HashString_handles_unicode_content()
    {
        await Given("a unicode string", () => "こんにちは世界 🌍")
            .When("hash is computed", FileHash.HashString)
            .Then("hash is 16 characters", h => h.Length == 16)
            .And("hash is deterministic", h => h == FileHash.HashString("こんにちは世界 🌍"))
            .AssertPassed();
    }

    [Scenario("HashBytes produces same hash as HashString for equivalent content")]
    [Fact]
    public async Task HashBytes_matches_HashString_for_equivalent_content()
    {
        await Given("a test string and its UTF8 bytes", () =>
            {
                var str = "Test content";
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                return (str, bytes);
            })
            .When("both hashes are computed", t => (FileHash.HashString(t.str), FileHash.HashBytes(t.bytes)))
            .Then("hashes match", t => t.Item1 == t.Item2)
            .AssertPassed();
    }

    [Scenario("HashBytes handles empty byte array")]
    [Fact]
    public async Task HashBytes_handles_empty_array()
    {
        await Given("an empty byte array", Array.Empty<byte>)
            .When("hash is computed", FileHash.HashBytes)
            .Then("hash is 16 characters", h => h.Length == 16)
            .And("hash matches empty string hash", h => h == FileHash.HashString(""))
            .AssertPassed();
    }

    [Scenario("HashFile produces deterministic hash for file content")]
    [Fact]
    public async Task HashFile_produces_deterministic_hash()
    {
        await Given("a temporary file with content", () =>
            {
                var folder = new TestFolder();
                var path = folder.WriteFile("test.txt", "File content for hashing");
                return (folder, path);
            })
            .When("hash is computed twice", t => (t.folder, FileHash.HashFile(t.path), FileHash.HashFile(t.path)))
            .Then("hashes match", t => t.Item2 == t.Item3)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HashFile produces hash matching HashString for file content")]
    [Fact]
    public async Task HashFile_matches_HashString_for_content()
    {
        await Given("a temporary file with known content", () =>
            {
                var folder = new TestFolder();
                var content = "Known content";
                var path = folder.WriteFile("test.txt", content);
                return (folder, path, content);
            })
            .When("file hash and string hash are computed", t =>
                (FileHash.HashFile(t.path), FileHash.HashString(t.content), t.folder))
            .Then("hashes match", t => t.Item1 == t.Item2)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HashFile throws for non-existent file")]
    [Fact]
    public async Task HashFile_throws_for_missing_file()
    {
        await Given("a non-existent file path", () => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.txt"))
            .When("hash is attempted", path =>
            {
                try
                {
                    FileHash.HashFile(path);
                    return (threw: false, exType: null!);
                }
                catch (Exception ex)
                {
                    return (threw: true, exType: ex.GetType());
                }
            })
            .Then("exception is thrown", r => r.threw)
            .And("exception is FileNotFoundException or DirectoryNotFoundException", r =>
                r.exType == typeof(FileNotFoundException) || r.exType == typeof(DirectoryNotFoundException))
            .AssertPassed();
    }

    [Scenario("HashFile handles binary content")]
    [Fact]
    public async Task HashFile_handles_binary_content()
    {
        await Given("a file with binary content", () =>
            {
                var folder = new TestFolder();
                var path = Path.Combine(folder.Root, "binary.bin");
                Directory.CreateDirectory(folder.Root);
                var bytes = new byte[] { 0x00, 0x01, 0xFF, 0xFE, 0x80, 0x7F };
                File.WriteAllBytes(path, bytes);
                return (folder, path, bytes);
            })
            .When("file hash and bytes hash are computed", t =>
                (FileHash.HashFile(t.path), FileHash.HashBytes(t.bytes), t.folder))
            .Then("hashes match", t => t.Item1 == t.Item2)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("HashFile handles large files")]
    [Fact]
    public async Task HashFile_handles_large_files()
    {
        await Given("a large file (1MB)", () =>
            {
                var folder = new TestFolder();
                var path = Path.Combine(folder.Root, "large.bin");
                Directory.CreateDirectory(folder.Root);
                var bytes = new byte[1024 * 1024]; // 1MB
                new Random(42).NextBytes(bytes);
                File.WriteAllBytes(path, bytes);
                return (folder, path);
            })
            .When("hash is computed", t => (FileHash.HashFile(t.path), t.folder))
            .Then("hash is 16 characters", t => t.Item1.Length == 16)
            .And("hash is deterministic", t => t.Item1 == FileHash.HashFile(t.folder.Root + "/large.bin"))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }
}
