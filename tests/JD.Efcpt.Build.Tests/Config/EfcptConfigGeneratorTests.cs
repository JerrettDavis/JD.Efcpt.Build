using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using JD.Efcpt.Build.Tasks.Config;
using Xunit;

namespace JD.Efcpt.Build.Tests.Config;

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
        
        // Verify code-generation has exactly 12 required properties
        var codeGen = config["code-generation"]?.AsObject();
        Assert.NotNull(codeGen);
        Assert.Equal(12, codeGen.Count);
        
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
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;

            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find repository root");
    }
}
