using JD.Efcpt.Build.Tasks;
using JD.Efcpt.Build.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JD.Efcpt.Build.Tests;

/// <summary>
/// Tests for the FileSystemHelpers utility class.
/// </summary>
[Feature("FileSystemHelpers: file system operation utilities")]
[Collection(nameof(AssemblySetup))]
public sealed class FileSystemHelpersTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region CopyDirectory Tests

    [Scenario("CopyDirectory copies all files and subdirectories")]
    [Fact]
    public async Task CopyDirectory_copies_entire_tree()
    {
        await Given("a source directory with files and subdirectories", () =>
            {
                var folder = new TestFolder();
                var sourceDir = folder.CreateDir("source");
                folder.WriteFile("source/file1.txt", "content1");
                folder.WriteFile("source/sub/file2.txt", "content2");
                folder.WriteFile("source/sub/deep/file3.txt", "content3");
                var destDir = Path.Combine(folder.Root, "dest");
                return (folder, sourceDir, destDir);
            })
            .When("CopyDirectory is called", t =>
            {
                FileSystemHelpers.CopyDirectory(t.sourceDir, t.destDir);
                return (t.folder, t.destDir);
            })
            .Then("all files are copied with correct content", t =>
            {
                var file1 = File.ReadAllText(Path.Combine(t.destDir, "file1.txt"));
                var file2 = File.ReadAllText(Path.Combine(t.destDir, "sub/file2.txt"));
                var file3 = File.ReadAllText(Path.Combine(t.destDir, "sub/deep/file3.txt"));
                return file1 == "content1" && file2 == "content2" && file3 == "content3";
            })
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("CopyDirectory preserves directory structure")]
    [Fact]
    public async Task CopyDirectory_preserves_structure()
    {
        await Given("a source directory with nested structure", () =>
            {
                var folder = new TestFolder();
                var sourceDir = folder.CreateDir("source");
                folder.CreateDir("source/a/b/c");
                folder.CreateDir("source/x/y");
                folder.WriteFile("source/a/b/c/file.txt", "deep");
                var destDir = Path.Combine(folder.Root, "dest");
                return (folder, sourceDir, destDir);
            })
            .When("CopyDirectory is called", t =>
            {
                FileSystemHelpers.CopyDirectory(t.sourceDir, t.destDir);
                return (t.folder, t.destDir);
            })
            .Then("directory structure is preserved", t =>
                Directory.Exists(Path.Combine(t.destDir, "a/b/c")) &&
                Directory.Exists(Path.Combine(t.destDir, "x/y")) &&
                File.Exists(Path.Combine(t.destDir, "a/b/c/file.txt")))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("CopyDirectory overwrites existing destination by default")]
    [Fact]
    public async Task CopyDirectory_overwrites_existing()
    {
        await Given("source and pre-existing destination directories", () =>
            {
                var folder = new TestFolder();
                var sourceDir = folder.CreateDir("source");
                folder.WriteFile("source/new.txt", "new content");

                var destDir = folder.CreateDir("dest");
                folder.WriteFile("dest/old.txt", "old content");
                folder.WriteFile("dest/new.txt", "old new content");

                return (folder, sourceDir, destDir);
            })
            .When("CopyDirectory is called with overwrite=true", t =>
            {
                FileSystemHelpers.CopyDirectory(t.sourceDir, t.destDir, overwrite: true);
                return (t.folder, t.destDir);
            })
            .Then("destination is replaced with source content", t =>
                !File.Exists(Path.Combine(t.destDir, "old.txt")) &&
                File.ReadAllText(Path.Combine(t.destDir, "new.txt")) == "new content")
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("CopyDirectory throws when source does not exist")]
    [Fact]
    public async Task CopyDirectory_throws_when_source_missing()
    {
        await Given("a non-existent source directory", () =>
            {
                var folder = new TestFolder();
                var sourceDir = Path.Combine(folder.Root, "nonexistent");
                var destDir = Path.Combine(folder.Root, "dest");
                return (folder, sourceDir, destDir);
            })
            .When("CopyDirectory is called", t =>
            {
                try
                {
                    FileSystemHelpers.CopyDirectory(t.sourceDir, t.destDir);
                    return (t.folder, threw: false);
                }
                catch (DirectoryNotFoundException)
                {
                    return (t.folder, threw: true);
                }
            })
            .Then("DirectoryNotFoundException is thrown", t => t.threw)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("CopyDirectory throws when source is null")]
    [Fact]
    public async Task CopyDirectory_throws_when_source_null()
    {
        await Given("null source parameter", () =>
            {
                var folder = new TestFolder();
                var destDir = Path.Combine(folder.Root, "dest");
                return (folder, (string?)null, destDir);
            })
            .When("CopyDirectory is called", t =>
            {
                try
                {
                    FileSystemHelpers.CopyDirectory(t.Item2!, t.destDir);
                    return (t.folder, threw: false);
                }
                catch (ArgumentNullException)
                {
                    return (t.folder, threw: true);
                }
            })
            .Then("ArgumentNullException is thrown", t => t.threw)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("CopyDirectory handles empty source directory")]
    [Fact]
    public async Task CopyDirectory_handles_empty_source()
    {
        await Given("an empty source directory", () =>
            {
                var folder = new TestFolder();
                var sourceDir = folder.CreateDir("empty-source");
                var destDir = Path.Combine(folder.Root, "dest");
                return (folder, sourceDir, destDir);
            })
            .When("CopyDirectory is called", t =>
            {
                FileSystemHelpers.CopyDirectory(t.sourceDir, t.destDir);
                return (t.folder, t.destDir);
            })
            .Then("destination directory is created and empty", t =>
                Directory.Exists(t.destDir) &&
                !Directory.EnumerateFileSystemEntries(t.destDir).Any())
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    #endregion

    #region DeleteDirectoryIfExists Tests

    [Scenario("DeleteDirectoryIfExists deletes existing directory")]
    [Fact]
    public async Task DeleteDirectoryIfExists_deletes_existing()
    {
        await Given("an existing directory with files", () =>
            {
                var folder = new TestFolder();
                var dir = folder.CreateDir("to-delete");
                folder.WriteFile("to-delete/file.txt", "content");
                return (folder, dir);
            })
            .When("DeleteDirectoryIfExists is called", t =>
            {
                var result = FileSystemHelpers.DeleteDirectoryIfExists(t.dir);
                return (t.folder, t.dir, result);
            })
            .Then("directory is deleted and returns true", t =>
                t.result && !Directory.Exists(t.dir))
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("DeleteDirectoryIfExists returns false for non-existent directory")]
    [Fact]
    public async Task DeleteDirectoryIfExists_nonexistent_returns_false()
    {
        await Given("a non-existent directory path", () =>
            {
                var folder = new TestFolder();
                var path = Path.Combine(folder.Root, "nonexistent");
                return (folder, path);
            })
            .When("DeleteDirectoryIfExists is called", t =>
            {
                var result = FileSystemHelpers.DeleteDirectoryIfExists(t.path);
                return (t.folder, result);
            })
            .Then("returns false", t => !t.result)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    #endregion

    #region EnsureDirectoryExists Tests

    [Scenario("EnsureDirectoryExists creates directory if missing")]
    [Fact]
    public async Task EnsureDirectoryExists_creates_directory()
    {
        await Given("a path to a non-existent directory", () =>
            {
                var folder = new TestFolder();
                var path = Path.Combine(folder.Root, "new-dir", "nested");
                return (folder, path);
            })
            .When("EnsureDirectoryExists is called", t =>
            {
                var info = FileSystemHelpers.EnsureDirectoryExists(t.path);
                return (t.folder, info);
            })
            .Then("directory is created", t => t.info.Exists)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    [Scenario("EnsureDirectoryExists returns existing directory")]
    [Fact]
    public async Task EnsureDirectoryExists_returns_existing()
    {
        await Given("an existing directory", () =>
            {
                var folder = new TestFolder();
                var path = folder.CreateDir("existing");
                return (folder, path);
            })
            .When("EnsureDirectoryExists is called", t =>
            {
                var info = FileSystemHelpers.EnsureDirectoryExists(t.path);
                return (t.folder, info);
            })
            .Then("existing directory is returned", t => t.info.Exists)
            .Finally(t => t.folder.Dispose())
            .AssertPassed();
    }

    #endregion
}
