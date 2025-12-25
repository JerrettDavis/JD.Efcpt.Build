using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JD.Efcpt.Build.Tasks.Config;

/// <summary>
/// Applies config overrides to an existing efcpt-config.json file.
/// </summary>
/// <remarks>
/// Uses reflection to iterate over non-null properties in the override model
/// and applies them to the corresponding JSON sections. Property names are
/// determined from <see cref="JsonPropertyNameAttribute"/> attributes.
/// </remarks>
internal static class EfcptConfigOverrideApplicator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // Cache section names by type for performance
    private static readonly Dictionary<Type, string> SectionNameCache = new()
    {
        [typeof(NamesOverrides)] = "names",
        [typeof(FileLayoutOverrides)] = "file-layout",
        [typeof(CodeGenerationOverrides)] = "code-generation",
        [typeof(TypeMappingsOverrides)] = "type-mappings",
        [typeof(ReplacementsOverrides)] = "replacements"
    };

    /// <summary>
    /// Reads the config JSON, applies non-null overrides, and writes back.
    /// </summary>
    /// <param name="configPath">Path to the staged efcpt-config.json file.</param>
    /// <param name="overrides">The overrides to apply.</param>
    /// <param name="log">Logger for diagnostic output.</param>
    /// <returns>Number of overrides applied.</returns>
    public static int Apply(string configPath, EfcptConfigOverrides overrides, IBuildLog log)
    {
        var json = File.ReadAllText(configPath);
        var root = JsonNode.Parse(json) ?? new JsonObject();

        var count = 0;
        count += ApplySection(root, overrides.Names, log);
        count += ApplySection(root, overrides.FileLayout, log);
        count += ApplySection(root, overrides.CodeGeneration, log);
        count += ApplySection(root, overrides.TypeMappings, log);
        count += ApplySection(root, overrides.Replacements, log);

        if (count > 0)
        {
            File.WriteAllText(configPath, root.ToJsonString(JsonOptions));
            log.Info($"Applied {count} config override(s) to {Path.GetFileName(configPath)}");
        }

        return count;
    }

    /// <summary>
    /// Applies overrides for a single section to the JSON root.
    /// </summary>
    private static int ApplySection<T>(JsonNode root, T? overrides, IBuildLog log) where T : class
    {
        if (overrides is null)
            return 0;

        var sectionName = GetSectionName<T>();
        var section = EnsureSection(root, sectionName);

        var count = 0;
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(overrides);
            if (value is null)
                continue;

            var jsonName = GetJsonPropertyName(prop);
            section[jsonName] = CreateJsonValue(value);
            log.Detail($"Override: {jsonName} = {FormatValue(value)}");
            count++;
        }

        return count;
    }

    /// <summary>
    /// Gets the section name for a given type from the cache.
    /// </summary>
    private static string GetSectionName<T>()
    {
        if (SectionNameCache.TryGetValue(typeof(T), out var name))
            return name;

        throw new InvalidOperationException($"Unknown section type: {typeof(T).Name}");
    }

    /// <summary>
    /// Gets the JSON property name from the <see cref="JsonPropertyNameAttribute"/> or falls back to the property name.
    /// </summary>
    private static string GetJsonPropertyName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attr?.Name ?? prop.Name;
    }

    /// <summary>
    /// Creates a JsonNode from a value.
    /// </summary>
    private static JsonNode? CreateJsonValue(object value)
    {
        return value switch
        {
            bool b => JsonValue.Create(b),
            string s => JsonValue.Create(s),
            int i => JsonValue.Create(i),
            _ => JsonValue.Create(value.ToString())
        };
    }

    /// <summary>
    /// Formats a value for logging.
    /// </summary>
    private static string FormatValue(object value)
    {
        return value switch
        {
            bool b => b.ToString().ToLowerInvariant(),
            string s => $"\"{s}\"",
            _ => value.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Ensures a section exists in the JSON root, creating it if necessary.
    /// </summary>
    private static JsonNode EnsureSection(JsonNode root, string sectionName)
    {
        if (root[sectionName] is null)
            root[sectionName] = new JsonObject();

        return root[sectionName]!;
    }
}
