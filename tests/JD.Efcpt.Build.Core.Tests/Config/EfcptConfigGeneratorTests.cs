using System.Text.Json.Nodes;
using JD.Efcpt.Build.Core.Config;
using JD.Efcpt.Build.Core.Tests.Infrastructure;
using Xunit;

namespace JD.Efcpt.Build.Core.Tests.Config;

public class EfcptConfigGeneratorTests
{
    private readonly string _schemaPath;

    public EfcptConfigGeneratorTests()
    {
        // Locate the schema file relative to the test project
        var repoRoot = FindRepoRoot();
        _schemaPath = Path.Combine(repoRoot, "lib", "efcpt-config.schema.json");

        if (!File.Exists(_schemaPath))
            throw new FileNotFoundException($"Schema file not found at: {_schemaPath}");
    }

    [Fact]
    public void GenerateFromFile_ProducesValidJson()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify it's valid JSON
        var parsed = JsonNode.Parse(result);
        Assert.NotNull(parsed);

        // Verify $schema property is present
        Assert.NotNull(parsed["$schema"]);
        Assert.Equal("https://raw.githubusercontent.com/ErikEJ/EFCorePowerTools/master/samples/efcpt-config.schema.json",
            parsed["$schema"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateFromFile_IncludesCodeGenerationSection()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);
        var config = JsonNode.Parse(result);

        // Assert
        Assert.NotNull(config);
        var codeGen = config["code-generation"];
        Assert.NotNull(codeGen);

        // Verify required properties exist
        Assert.NotNull(codeGen["enable-on-configuring"]);
        Assert.NotNull(codeGen["type"]);
        Assert.NotNull(codeGen["use-database-names"]);
        Assert.NotNull(codeGen["use-data-annotations"]);
        Assert.NotNull(codeGen["use-nullable-reference-types"]);
        Assert.NotNull(codeGen["use-inflector"]);
        Assert.NotNull(codeGen["use-legacy-inflector"]);
        Assert.NotNull(codeGen["use-many-to-many-entity"]);
        Assert.NotNull(codeGen["use-t4"]);
        Assert.NotNull(codeGen["remove-defaultsql-from-bool-properties"]);
        Assert.NotNull(codeGen["soft-delete-obsolete-files"]);
        Assert.NotNull(codeGen["use-alternate-stored-procedure-resultset-discovery"]);
    }

    [Fact]
    public void GenerateFromFile_IncludesNamesSection()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);
        var config = JsonNode.Parse(result);

        // Assert
        Assert.NotNull(config);
        var names = config["names"];
        Assert.NotNull(names);

        // Verify required properties exist with defaults
        Assert.Equal("ApplicationDbContext", names["dbcontext-name"]?.GetValue<string>());
        Assert.Equal("EfcptProject", names["root-namespace"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateFromFile_IncludesFileLayoutSection()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);
        var config = JsonNode.Parse(result);

        // Assert
        Assert.NotNull(config);
        var fileLayout = config["file-layout"];
        Assert.NotNull(fileLayout);

        // Verify required properties exist
        Assert.NotNull(fileLayout["output-path"]);
        Assert.Equal("Models", fileLayout["output-path"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateFromFile_ExcludesPreviewProperties()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);
        var config = JsonNode.Parse(result);

        // Assert - verify no preview properties are present
        Assert.NotNull(config);
        var jsonString = result.ToLowerInvariant();
        Assert.DoesNotContain("-preview", jsonString);
    }

    [Fact]
    public void GenerateFromFile_WithCustomNames()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(
            _schemaPath,
            dbContextName: "MyCustomContext",
            rootNamespace: "MyCustomNamespace");

        var config = JsonNode.Parse(result);

        // Assert
        Assert.NotNull(config);
        var names = config["names"];
        Assert.NotNull(names);
        Assert.Equal("MyCustomContext", names["dbcontext-name"]?.GetValue<string>());
        Assert.Equal("MyCustomNamespace", names["root-namespace"]?.GetValue<string>());
    }

    [Fact]
    public void GenerateFromFile_UsesSchemaDefaults()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);
        var config = JsonNode.Parse(result);

        // Assert - verify defaults from schema
        Assert.NotNull(config);
        var codeGen = config["code-generation"];
        Assert.NotNull(codeGen);

        // Check known defaults from schema
        Assert.Equal("all", codeGen["type"]?.GetValue<string>());
        Assert.True(codeGen["use-inflector"]?.GetValue<bool>());
        Assert.True(codeGen["soft-delete-obsolete-files"]?.GetValue<bool>());
    }

    [Fact]
    public void GenerateFromFile_ProducesExpectedStructure()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);

        // Assert - verify the structure matches expected format
        Assert.Contains("\"code-generation\":", result);
        Assert.Contains("\"names\":", result);
        Assert.Contains("\"file-layout\":", result);
        Assert.Contains("\"$schema\":", result);

        // Verify indentation (should be formatted)
        Assert.Contains("  ", result);

        // Verify type-mappings is NOT present (not required)
        Assert.DoesNotContain("\"type-mappings\":", result);
    }

    [Fact]
    public void GenerateFromFile_OnlyIncludesRequiredProperties()
    {
        // Act
        var result = EfcptConfigGenerator.GenerateFromFile(_schemaPath);
        var config = JsonNode.Parse(result);

        // Assert
        Assert.NotNull(config);

        // Verify only required sections are present
        Assert.NotNull(config["$schema"]);
        Assert.NotNull(config["code-generation"]);
        Assert.NotNull(config["names"]);
        Assert.NotNull(config["file-layout"]);

        // Verify optional sections are NOT present
        Assert.Null(config["type-mappings"]);
        Assert.Null(config["tables"]);
        Assert.Null(config["views"]);
        Assert.Null(config["stored-procedures"]);
        Assert.Null(config["functions"]);
        Assert.Null(config["replacements"]);

        // Verify code-generation has exactly 13 required properties
        var codeGen = config["code-generation"]?.AsObject();
        Assert.NotNull(codeGen);
        Assert.Equal(13, codeGen.Count);

        // Verify names has exactly 2 required properties
        var names = config["names"]?.AsObject();
        Assert.NotNull(names);
        Assert.Equal(2, names.Count);

        // Verify file-layout has exactly 1 required property
        var fileLayout = config["file-layout"]?.AsObject();
        Assert.NotNull(fileLayout);
        Assert.Single(fileLayout);
    }

    private static string FindRepoRoot()
    {
        // Locate the repo root by the solution file rather than the ".git" entry.
        // In a git worktree, ".git" at the root is a FILE (a gitdir pointer), not a
        // directory, so a Directory.Exists(".git") check fails and the walk-up never
        // matches. Matching on JD.Efcpt.Build.sln mirrors the existing convention in
        // JD.Efcpt.Build.Tests and works in clones and worktrees alike. The walk-up
        // itself lives in RepoRootLocator so it can be exercised directly by a
        // regression test without depending on the real repo layout.
        var fromCwd = RepoRootLocator.FindRepoRootFrom(Directory.GetCurrentDirectory());
        if (fromCwd is not null)
            return fromCwd;

        var assemblyLocation = typeof(EfcptConfigGeneratorTests).Assembly.Location;
        var fromAssembly = RepoRootLocator.FindRepoRootFrom(Path.GetDirectoryName(assemblyLocation) ?? assemblyLocation);
        if (fromAssembly is not null)
            return fromAssembly;

        throw new DirectoryNotFoundException("Could not find repository root");
    }

    [Fact]
    public void FindRepoRoot_DetectsRootWhenGitIsAFile_LikeInAWorktree()
    {
        // Regression test for #190: in a git worktree, ".git" at the root is a FILE
        // (a gitdir pointer to the main repo's worktrees dir), not a directory. A
        // walk-up that keys on Directory.Exists(".git") silently fails in that layout,
        // so this simulates a worktree without needing a real one, proving detection
        // keys on JD.Efcpt.Build.sln instead. Normal CI runs from a full clone (where
        // ".git" is a directory), so this bug would not be caught without this test.
        var tempRoot = Directory.CreateTempSubdirectory("efcpt-repo-root-test-");
        try
        {
            File.WriteAllText(Path.Combine(tempRoot.FullName, "JD.Efcpt.Build.sln"), string.Empty);
            File.WriteAllText(Path.Combine(tempRoot.FullName, ".git"), "gitdir: ../.git/worktrees/foo");

            var nested = Directory.CreateDirectory(Path.Combine(tempRoot.FullName, "tests", "SomeProject"));

            var result = RepoRootLocator.FindRepoRootFrom(nested.FullName);

            Assert.Equal(tempRoot.FullName, result);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public void FindRepoRoot_DoesNotMatchOnGitDirectoryAlone_WithoutSolutionFile()
    {
        // Detection must key on JD.Efcpt.Build.sln, not on the presence of ".git"
        // (file or directory). A directory containing only a ".git" directory and no
        // solution file must not be mistaken for the repo root.
        var tempRoot = Directory.CreateTempSubdirectory("efcpt-repo-root-test-");
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot.FullName, ".git"));
            var nested = Directory.CreateDirectory(Path.Combine(tempRoot.FullName, "tests", "SomeProject"));

            var result = RepoRootLocator.FindRepoRootFrom(nested.FullName);

            Assert.Null(result);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}
