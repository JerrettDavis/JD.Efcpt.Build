using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace JD.Efcpt.Build.Tasks.Config;

/// <summary>
/// Generates efcpt-config.json from the EFCorePowerTools JSON schema.
/// </summary>
public static class EfcptConfigGenerator
{
    private const string DefaultSchemaUrl = "https://raw.githubusercontent.com/JerrettDavis/JD.Efcpt.Build/refs/heads/main/lib/efcpt-config.schema.json";

    /// <summary>
    /// Generates a default efcpt-config.json from a schema URL.
    /// </summary>
    /// <param name="schemaUrl">URL to the schema (optional, uses default if not provided)</param>
    /// <param name="dbContextName">Optional custom DbContext name (default: "ApplicationDbContext")</param>
    /// <param name="rootNamespace">Optional custom root namespace (default: "EfcptProject")</param>
    /// <returns>Generated JSON string</returns>
    public static async Task<string> GenerateFromUrlAsync(
        string? schemaUrl = null,
        string? dbContextName = null,
        string? rootNamespace = null)
    {
        schemaUrl ??= DefaultSchemaUrl;

        using var client = new HttpClient();
        var schemaJson = await client.GetStringAsync(schemaUrl);
        return GenerateFromSchema(schemaJson, dbContextName, rootNamespace);
    }

    /// <summary>
    /// Generates a default efcpt-config.json from a local schema file.
    /// </summary>
    /// <param name="schemaPath">Path to the schema file</param>
    /// <param name="dbContextName">Optional custom DbContext name (default: "ApplicationDbContext")</param>
    /// <param name="rootNamespace">Optional custom root namespace (default: "EfcptProject")</param>
    /// <returns>Generated JSON string</returns>
    public static string GenerateFromFile(
        string schemaPath,
        string? dbContextName = null,
        string? rootNamespace = null)
    {
        var schemaJson = File.ReadAllText(schemaPath);
        return GenerateFromSchema(schemaJson, dbContextName, rootNamespace);
    }

    /// <summary>
    /// Generates a default efcpt-config.json from schema JSON string.
    /// </summary>
    /// <param name="schemaJson">The JSON schema as a string</param>
    /// <param name="dbContextName">Optional custom DbContext name (default: "ApplicationDbContext")</param>
    /// <param name="rootNamespace">Optional custom root namespace (default: "EfcptProject")</param>
    /// <returns>Generated JSON string</returns>
    public static string GenerateFromSchema(
        string schemaJson,
        string? dbContextName = null,
        string? rootNamespace = null)
    {
        var schema = JsonNode.Parse(schemaJson);
        if (schema is null)
            throw new InvalidOperationException("Failed to parse schema JSON");

        var config = new JsonObject();
        var definitions = schema["definitions"]?.AsObject();
        if (definitions is null)
            throw new InvalidOperationException("Schema does not contain definitions section");

        // Process each top-level section
        ProcessCodeGeneration(config, definitions);
        ProcessFileLayout(config, definitions);
        ProcessNames(config, definitions, dbContextName, rootNamespace);
        ProcessTypeMappings(config, definitions);

        // Serialize with indentation
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(config, options);
    }

    private static void ProcessCodeGeneration(JsonObject config, JsonObject definitions)
    {
        var codeGenDef = definitions["CodeGeneration"]?.AsObject();
        if (codeGenDef is null) return;

        var properties = codeGenDef["properties"]?.AsObject();
        if (properties is null) return;

        var codeGenConfig = new JsonObject();

        // Process all properties, not just required ones
        foreach (var kvp in properties)
        {
            var propName = kvp.Key;
            
            // Skip preview properties
            if (propName.Contains("-preview", StringComparison.OrdinalIgnoreCase))
                continue;

            var propDef = kvp.Value?.AsObject();
            if (propDef is null) continue;

            if (TryGetDefaultValue(propDef, propName, out var defaultValue))
            {
                codeGenConfig[propName] = defaultValue;
            }
        }

        if (codeGenConfig.Count > 0)
        {
            config["code-generation"] = codeGenConfig;
        }
    }

    private static void ProcessNames(
        JsonObject config,
        JsonObject definitions,
        string? dbContextName,
        string? rootNamespace)
    {
        var namesDef = definitions["Names"]?.AsObject();
        if (namesDef is null) return;

        var properties = namesDef["properties"]?.AsObject();
        if (properties is null) return;

        var namesConfig = new JsonObject();

        // Process all properties
        foreach (var kvp in properties)
        {
            var propName = kvp.Key;
            
            // Skip preview properties
            if (propName.Contains("-preview", StringComparison.OrdinalIgnoreCase))
                continue;

            // Use custom values if provided
            if (propName == "dbcontext-name" && !string.IsNullOrEmpty(dbContextName))
            {
                namesConfig[propName] = dbContextName;
            }
            else if (propName == "root-namespace" && !string.IsNullOrEmpty(rootNamespace))
            {
                namesConfig[propName] = rootNamespace;
            }
            else
            {
                var propDef = kvp.Value?.AsObject();
                if (propDef is null) continue;

                if (TryGetDefaultValue(propDef, propName, out var defaultValue))
                {
                    namesConfig[propName] = defaultValue!;
                }
                else
                {
                    // Provide sensible defaults for required string properties
                    if (propName == "dbcontext-name")
                        namesConfig[propName] = "ApplicationDbContext";
                    else if (propName == "root-namespace")
                        namesConfig[propName] = "EfcptProject";
                    else
                        namesConfig[propName] = JsonValue.Create<string?>(null);
                }
            }
        }

        if (namesConfig.Count > 0)
        {
            config["names"] = namesConfig;
        }
    }

    private static void ProcessFileLayout(JsonObject config, JsonObject definitions)
    {
        var fileLayoutDef = definitions["FileLayout"]?.AsObject();
        if (fileLayoutDef is null) return;

        var properties = fileLayoutDef["properties"]?.AsObject();
        if (properties is null) return;

        var fileLayoutConfig = new JsonObject();

        // Process all properties, not just required ones
        foreach (var kvp in properties)
        {
            var propName = kvp.Key;
            
            // Skip preview properties
            if (propName.Contains("-preview", StringComparison.OrdinalIgnoreCase))
                continue;

            var propDef = kvp.Value?.AsObject();
            if (propDef is null) continue;

            if (TryGetDefaultValue(propDef, propName, out var defaultValue))
            {
                fileLayoutConfig[propName] = defaultValue;
            }
        }

        if (fileLayoutConfig.Count > 0)
        {
            config["file-layout"] = fileLayoutConfig;
        }
    }

    private static void ProcessTypeMappings(JsonObject config, JsonObject definitions)
    {
        var typeMappingsDef = definitions["TypeMappings"]?.AsObject();
        if (typeMappingsDef is null) return;

        var properties = typeMappingsDef["properties"]?.AsObject();
        if (properties is null) return;

        var typeMappingsConfig = new JsonObject();

        // Process all properties
        foreach (var kvp in properties)
        {
            var propName = kvp.Key;
            
            // Skip preview properties
            if (propName.Contains("-preview", StringComparison.OrdinalIgnoreCase))
                continue;

            var propDef = kvp.Value?.AsObject();
            if (propDef is null) continue;

            if (TryGetDefaultValue(propDef, propName, out var defaultValue))
            {
                typeMappingsConfig[propName] = defaultValue;
            }
        }

        if (typeMappingsConfig.Count > 0)
        {
            config["type-mappings"] = typeMappingsConfig;
        }
    }

    private static List<string> GetRequiredProperties(JsonObject definition)
    {
        var requiredArray = definition["required"]?.AsArray();
        if (requiredArray is null)
            return new List<string>();

        return requiredArray
            .Select(item => item?.GetValue<string>())
            .Where(s => s is not null)
            .Cast<string>()
            .ToList();
    }

    private static bool TryGetDefaultValue(JsonObject propertyDef, string propertyName, out JsonNode? defaultValue)
    {
        // Check if there's an explicit default value
        if (propertyDef.TryGetPropertyValue("default", out defaultValue) && defaultValue is not null)
        {
            defaultValue = defaultValue.DeepClone();
            return true;
        }

        // Check type to determine implicit defaults
        var type = propertyDef["type"];
        if (type is null)
        {
            defaultValue = null;
            return false;
        }

        // Handle type as string
        if (type is JsonValue typeValue)
        {
            var typeStr = typeValue.GetValue<string>();
            if (typeStr == "boolean")
            {
                defaultValue = JsonValue.Create(false);
                return true;
            }
            
            defaultValue = null;
            return false;
        }

        // Handle type as array (e.g., ["string", "null"]) - nullable types
        if (type is JsonArray typeArray)
        {
            // Return null for nullable properties
            defaultValue = JsonValue.Create<string?>(null);
            return true;
        }

        defaultValue = null;
        return false;
    }
}
