using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using JD.Efcpt.Build.Core.Logging;

namespace JD.Efcpt.Build.Core.Config;

/// <summary>
/// Generates efcpt-config.json from the EFCorePowerTools JSON schema.
/// </summary>
public static class EfcptConfigGenerator
{
    private const string PrimarySchemaUrl = "https://raw.githubusercontent.com/ErikEJ/EFCorePowerTools/master/samples/efcpt-config.schema.json";
    private const string FallbackSchemaUrl = "https://raw.githubusercontent.com/JerrettDavis/JD.Efcpt.Build/refs/heads/main/lib/efcpt-config.schema.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Bounded per-request timeout for the online schema fetch. Keeps <c>init --online</c>
    /// aligned with the tool's documented offline-first (&lt;10s) design instead of blocking on
    /// <see cref="HttpClient"/>'s 100s default when a URL is unreachable.
    /// </summary>
    private static readonly TimeSpan OnlineFetchTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Generates a default efcpt-config.json from a schema URL.
    /// </summary>
    /// <param name="schemaUrl">
    /// Explicit schema URL to fetch. When <see langword="null"/>, the primary GitHub URL is tried
    /// first and, on any failure, the fallback mirror is used (the failure and the URL that
    /// ultimately supplied the schema are reported via <paramref name="log"/>).
    /// </param>
    /// <param name="dbContextName">Optional custom DbContext name (default: "ApplicationDbContext")</param>
    /// <param name="rootNamespace">Optional custom root namespace (default: "EfcptProject")</param>
    /// <param name="log">
    /// Optional build log used to surface a primary-URL failure (type + message) before falling
    /// back, and to record which URL ultimately supplied the schema. When <see langword="null"/>,
    /// the fetch proceeds silently (fallback still occurs, but without a diagnostic trail).
    /// </param>
    /// <returns>Generated JSON string</returns>
    /// <remarks>
    /// A single <see cref="HttpClient"/> with a bounded <see cref="OnlineFetchTimeout"/> is used
    /// for the fetch. The previous "probe primary, then re-fetch" double round-trip has been
    /// collapsed into a single primary <c>GetStringAsync</c> (falling back to the mirror on
    /// failure), so a healthy primary URL is fetched exactly once.
    /// </remarks>
    public static async Task<string> GenerateFromUrlAsync(
        string? schemaUrl = null,
        string? dbContextName = null,
        string? rootNamespace = null,
        IBuildLog? log = null)
    {
        using var client = new HttpClient { Timeout = OnlineFetchTimeout };

        string resolvedUrl;
        string schemaJson;

        if (!string.IsNullOrWhiteSpace(schemaUrl))
        {
            // Caller pinned an explicit URL: fetch it directly, no primary/fallback dance.
            resolvedUrl = schemaUrl!;
            schemaJson = await client.GetStringAsync(resolvedUrl);
        }
        else
        {
            try
            {
                resolvedUrl = PrimarySchemaUrl;
                schemaJson = await client.GetStringAsync(PrimarySchemaUrl);
            }
            catch (Exception ex)
            {
                // Don't silently swap to the mirror: surface why the primary failed so a
                // stale/renamed primary URL is diagnosable rather than invisibly masked.
                log?.Warn(
                    $"Primary schema URL failed ({ex.GetType().Name}: {ex.Message}); falling back to {FallbackSchemaUrl}");
                resolvedUrl = FallbackSchemaUrl;
                schemaJson = await client.GetStringAsync(FallbackSchemaUrl);
            }
        }

        log?.Info($"Fetched efcpt-config schema from {resolvedUrl}");
        return GenerateFromSchema(schemaJson, dbContextName, rootNamespace, resolvedUrl);
    }

    /// <summary>
    /// Generates a default efcpt-config.json from a local schema file.
    /// </summary>
    /// <param name="schemaPath">Path to the schema file</param>
    /// <param name="dbContextName">Optional custom DbContext name (default: "ApplicationDbContext")</param>
    /// <param name="rootNamespace">Optional custom root namespace (default: "EfcptProject")</param>
    /// <param name="schemaUrl">Optional schema URL to include in $schema property (default: primary schema URL)</param>
    /// <returns>Generated JSON string</returns>
    public static string GenerateFromFile(
        string schemaPath,
        string? dbContextName = null,
        string? rootNamespace = null,
        string? schemaUrl = null)
    {
        var schemaJson = File.ReadAllText(schemaPath);
        schemaUrl ??= PrimarySchemaUrl;
        return GenerateFromSchema(schemaJson, dbContextName, rootNamespace, schemaUrl);
    }

    /// <summary>
    /// Generates a default efcpt-config.json from schema JSON string.
    /// </summary>
    /// <param name="schemaJson">The JSON schema as a string</param>
    /// <param name="dbContextName">Optional custom DbContext name (default: "ApplicationDbContext")</param>
    /// <param name="rootNamespace">Optional custom root namespace (default: "EfcptProject")</param>
    /// <param name="schemaUrl">Optional schema URL to include in $schema property (default: primary schema URL)</param>
    /// <returns>Generated JSON string</returns>
    public static string GenerateFromSchema(
        string schemaJson,
        string? dbContextName = null,
        string? rootNamespace = null,
        string? schemaUrl = null)
    {
        var schema = JsonNode.Parse(schemaJson);
        if (schema is null)
            throw new InvalidOperationException("Failed to parse schema JSON");

        var config = new JsonObject();

        // Add $schema property first
        schemaUrl ??= PrimarySchemaUrl;
        config["$schema"] = schemaUrl;

        var definitions = schema["definitions"]?.AsObject();
        if (definitions is null)
            throw new InvalidOperationException("Schema does not contain definitions section");

        // Process each top-level section - only required properties
        ProcessCodeGeneration(config, definitions);
        ProcessFileLayout(config, definitions);
        ProcessNames(config, definitions, dbContextName, rootNamespace);
        // Don't process TypeMappings as it's not required

        // Serialize with indentation
        return JsonSerializer.Serialize(config, JsonOptions);
    }

    private static void ProcessCodeGeneration(JsonObject config, JsonObject definitions)
    {
        var codeGenDef = definitions["CodeGeneration"]?.AsObject();
        if (codeGenDef is null) return;

        var required = GetRequiredProperties(codeGenDef);
        var properties = codeGenDef["properties"]?.AsObject();
        if (properties is null) return;

        var codeGenConfig = new JsonObject();

        // Process only required properties
        foreach (var propName in required)
        {
            // Skip preview properties
            if (propName.Contains("-preview", StringComparison.OrdinalIgnoreCase))
                continue;

            var propDef = properties[propName]?.AsObject();
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

        var required = GetRequiredProperties(namesDef);
        var properties = namesDef["properties"]?.AsObject();
        if (properties is null) return;

        var namesConfig = new JsonObject();

        // Process only required properties
        foreach (var propName in required)
        {
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
                var propDef = properties[propName]?.AsObject();
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

        var required = GetRequiredProperties(fileLayoutDef);
        var properties = fileLayoutDef["properties"]?.AsObject();
        if (properties is null) return;

        var fileLayoutConfig = new JsonObject();

        // Process only required properties
        foreach (var propName in required)
        {
            // Skip preview properties
            if (propName.Contains("-preview", StringComparison.OrdinalIgnoreCase))
                continue;

            var propDef = properties[propName]?.AsObject();
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
