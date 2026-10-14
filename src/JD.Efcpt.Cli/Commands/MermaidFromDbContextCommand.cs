using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

namespace JD.Efcpt.Cli.Commands;

internal static class MermaidFromDbContextCommand
{
    public static Command Build()
    {
        var assemblyOption = new Option<string>("--assembly")
        {
            Description = "Path to the built project assembly containing the DbContext.",
            Required = true
        };
        var contextOption = new Option<string?>("--context")
        {
            Description = "DbContext type name when the project contains more than one context."
        };
        var startupAssemblyOption = new Option<string?>("--startup-assembly")
        {
            Description = "Optional startup-project assembly used for design-time host and factory discovery."
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Path to the Mermaid Markdown output file.",
            Required = true
        };

        var command = new Command("mermaid-from-dbcontext", "Generate a Mermaid ER diagram from a code-first DbContext.")
        {
            assemblyOption,
            contextOption,
            startupAssemblyOption,
            outputOption
        };

        command.SetAction(parseResult => Execute(
            parseResult.GetValue(assemblyOption)!,
            parseResult.GetValue(contextOption),
            parseResult.GetValue(startupAssemblyOption),
            parseResult.GetValue(outputOption)!));

        return command;
    }

    public static int Execute(string assemblyPath, string? contextName, string? startupAssemblyPath, string outputPath)
    {
        try
        {
            var fullAssemblyPath = Path.GetFullPath(assemblyPath);
            if (!File.Exists(fullAssemblyPath))
                throw new FileNotFoundException("The project assembly was not found.", fullAssemblyPath);

            var fullStartupAssemblyPath = string.IsNullOrWhiteSpace(startupAssemblyPath)
                ? fullAssemblyPath
                : Path.GetFullPath(startupAssemblyPath);
            if (!File.Exists(fullStartupAssemblyPath))
                throw new FileNotFoundException("The startup-project assembly was not found.", fullStartupAssemblyPath);

            var loadContext = new ProjectAssemblyLoadContext(fullAssemblyPath, fullStartupAssemblyPath);
            try
            {
                var projectAssembly = loadContext.LoadFromAssemblyPath(fullAssemblyPath);
                var startupAssembly = string.Equals(fullAssemblyPath, fullStartupAssemblyPath, StringComparison.OrdinalIgnoreCase)
                    ? projectAssembly
                    : loadContext.LoadFromAssemblyPath(fullStartupAssemblyPath);
                var contextType = FindDbContextType(projectAssembly, contextName);
                var context = CreateDbContext(projectAssembly, startupAssembly, contextType);
                try
                {
                    var actualContext = context is HostOwnedContext ownedContext ? ownedContext.Context : context;
                    var markdown = RenderModel(actualContext.GetType().GetProperty("Model")!.GetValue(actualContext)!);

                    var fullOutputPath = Path.GetFullPath(outputPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
                    File.WriteAllText(fullOutputPath, markdown, new UTF8Encoding(false));
                    Console.WriteLine($"Generated Mermaid ER diagram: {fullOutputPath}");
                    return 0;
                }
                finally
                {
                    (context as IDisposable)?.Dispose();
                }
            }
            finally
            {
                loadContext.Unload();
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"DbContext Mermaid generation failed: {exception.GetBaseException().Message}");
            return 1;
        }
    }

    private static Type FindDbContextType(Assembly assembly, string? contextName)
    {
        var dbContextTypes = GetLoadableTypes(assembly)
            .Where(type => IsDbContext(type) && !type.IsAbstract && !type.ContainsGenericParameters)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(contextName))
        {
            var fullNameMatch = dbContextTypes.FirstOrDefault(type =>
                string.Equals(type.FullName, contextName, StringComparison.Ordinal));
            if (fullNameMatch is not null)
                return fullNameMatch;

            var shortNameMatches = dbContextTypes
                .Where(type => string.Equals(type.Name, contextName, StringComparison.Ordinal))
                .ToArray();
            return shortNameMatches.Length switch
            {
                1 => shortNameMatches[0],
                > 1 => throw new InvalidOperationException(
                    $"DbContext name '{contextName}' is ambiguous. Use a fully qualified name: {FormatTypes(shortNameMatches)}"),
                _ => throw new InvalidOperationException(
                    $"DbContext '{contextName}' was not found. Discovered contexts: {FormatTypes(dbContextTypes)}")
            };
        }

        return dbContextTypes.Length switch
        {
            0 => throw new InvalidOperationException($"No DbContext types were found in '{assembly.Location}'."),
            1 => dbContextTypes[0],
            _ => throw new InvalidOperationException(
                $"Multiple DbContext types were found. Set EfcptCodeFirstDbContextName to one of: {FormatTypes(dbContextTypes)}")
        };
    }

    private static object CreateDbContext(Assembly assembly, Assembly startupAssembly, Type contextType)
    {
        var factoryInterfaceName = "Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory`1";
        var factories = GetLoadableTypes(assembly).Concat(GetLoadableTypes(startupAssembly))
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType &&
                    contract.GetGenericTypeDefinition().FullName == factoryInterfaceName &&
                    contract.GetGenericArguments()[0] == contextType)
                .Select(_ => type))
            .Distinct()
            .ToArray();

        if (factories.Length > 1)
            throw new InvalidOperationException($"Multiple design-time factories were found for '{contextType.FullName}'.");

        if (factories.Length == 1)
        {
            var factory = Activator.CreateInstance(factories[0])
                ?? throw new InvalidOperationException($"Could not create design-time factory '{factories[0].FullName}'.");
            var factoryContract = factories[0].GetInterfaces().First(contract =>
                contract.IsGenericType &&
                contract.GetGenericTypeDefinition().FullName == factoryInterfaceName &&
                contract.GetGenericArguments()[0] == contextType);
            var contractMethod = factoryContract.GetMethod("CreateDbContext")!;
            var createMethod = factories[0].GetInterfaceMap(factoryContract).TargetMethods[
                Array.IndexOf(factoryContract.GetMethods(), contractMethod)];
            return createMethod.Invoke(factory, [Array.Empty<string>()])
                ?? throw new InvalidOperationException($"Design-time factory '{factories[0].FullName}' returned null.");
        }

        var host = TryCreateHostContext(startupAssembly, contextType);
        if (host is not null)
            return host;

        return Activator.CreateInstance(contextType)
            ?? throw new InvalidOperationException(
                $"Could not create '{contextType.FullName}'. Add an IDesignTimeDbContextFactory<{contextType.Name}> or a parameterless constructor.");
    }

    private static object? TryCreateHostContext(Assembly assembly, Type contextType)
    {
        var program = GetLoadableTypes(assembly)
            .FirstOrDefault(type => type.Name == "Program");
        if (program is null)
            return null;

        var builderFactory = program.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.GetParameters() is [{ ParameterType: var parameterType }] &&
                parameterType == typeof(string[]) &&
                (method.Name == "CreateHostBuilder" || method.Name == "CreateWebHostBuilder"))
            .OrderBy(method => method.Name == "CreateHostBuilder" ? 0 : 1)
            .FirstOrDefault();
        if (builderFactory is null)
            return null;

        var builder = builderFactory.Invoke(null, [Array.Empty<string>()]);
        var host = builder?.GetType().GetMethod("Build", Type.EmptyTypes)?.Invoke(builder, null);
        var services = host?.GetType().GetProperty("Services")?.GetValue(host) as IServiceProvider;
        if (services is null)
        {
            (host as IDisposable)?.Dispose();
            throw new InvalidOperationException($"'{builderFactory.Name}' did not produce a host with a service provider.");
        }

        var scopeFactoryType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .SelectMany(type => type.GetInterfaces().Append(type))
            .FirstOrDefault(type => type.FullName == "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory");
        var scopeFactory = scopeFactoryType is null ? null : services.GetService(scopeFactoryType);
        if (scopeFactory is null)
        {
            (host as IDisposable)?.Dispose();
            throw new InvalidOperationException("The application host did not provide an IServiceScopeFactory.");
        }

        var scope = scopeFactory.GetType().GetMethod("CreateScope", Type.EmptyTypes)!.Invoke(scopeFactory, null) as IDisposable
            ?? throw new InvalidOperationException("The application host could not create a service scope.");
        var scopedServices = scope.GetType().GetProperty("ServiceProvider")!.GetValue(scope) as IServiceProvider
            ?? throw new InvalidOperationException("The application host scope did not expose a service provider.");
        var context = scopedServices.GetService(contextType);
        if (context is null)
        {
            scope.Dispose();
            (host as IDisposable)?.Dispose();
            return null;
        }

        return new HostOwnedContext(context, scope, host as IDisposable);
    }

    private static string RenderModel(object model)
    {
        var entityTypes = InvokeEnumerable(model, "GetEntityTypes")
            .Select(entity => new EntityShape(
                entity,
                ReadString(entity, "Name"),
                InvokeEnumerable(entity, "GetProperties").Select(property => new PropertyShape(
                        InvokeStringExtension("GetColumnName", property) ?? ReadString(property, "Name"),
                        ReadString(property, "Name"),
                        ReadType(property, "ClrType"),
                        ReadBool(property, "IsNullable")))
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .ToArray(),
                InvokeMethod(entity, "FindPrimaryKey") is { } primaryKey
                    ? ReadEnumerableProperty(primaryKey, "Properties").Select(property => ReadString(property, "Name")).ToHashSet(StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal),
                InvokeEnumerable(entity, "GetForeignKeys")
                    .SelectMany(foreignKey => ReadEnumerableProperty(foreignKey, "Properties"))
                    .Select(property => ReadString(property, "Name"))
                    .ToHashSet(StringComparer.Ordinal)))
            .OrderBy(entity => entity.Name, StringComparer.Ordinal)
            .ToArray();

        if (entityTypes.Length == 0)
            throw new InvalidOperationException("The selected DbContext model contains no entity types.");

        var entityIdentifiers = CreateUniqueIdentifiers(entityTypes.Select(entity => entity.Name));
        var identifiers = entityTypes
            .Select((entity, index) => (entity.Instance, Identifier: entityIdentifiers[index]))
            .ToDictionary(pair => pair.Instance, pair => pair.Identifier, ReferenceEqualityComparer.Instance);
        var output = new StringBuilder("# Entity Relationship Diagram\n\n```mermaid\nerDiagram\n");
        foreach (var entity in entityTypes)
        {
            output.Append("  ").Append(identifiers[entity.Instance]).AppendLine(" {");
            var properties = entity.Properties
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ThenBy(property => property.ClrName, StringComparer.Ordinal)
                .ToArray();
            var propertyIdentifiers = CreateUniqueIdentifiers(properties.Select(property => property.Name));
            for (var index = 0; index < properties.Length; index++)
            {
                var property = properties[index];
                output.Append("    ").Append(MermaidType(property.TypeName)).Append(' ')
                    .Append(propertyIdentifiers[index]);
                if (entity.PrimaryKey.Contains(property.ClrName))
                    output.Append(" PK");
                if (entity.ForeignKeys.Contains(property.ClrName))
                    output.Append(" FK");
                if (property.Nullable)
                    output.Append(" \"nullable\"");
                output.AppendLine();
            }

            output.AppendLine("  }");
        }

        foreach (var entity in entityTypes)
        foreach (var foreignKey in InvokeEnumerable(entity.Instance, "GetForeignKeys")
                     .OrderBy(foreignKey => string.Join(",", ReadEnumerableProperty(foreignKey, "Properties")
                         .Select(property => ReadString(property, "Name"))), StringComparer.Ordinal))
        {
            var principal = ReadProperty(foreignKey, "PrincipalEntityType")!;
            if (!identifiers.TryGetValue(principal, out var principalId))
                continue;
            var required = ReadBool(foreignKey, "IsRequired");
            var unique = ReadBool(foreignKey, "IsUnique");
            var principalCardinality = required ? "||" : "o|";
            var dependentCardinality = unique
                ? (ReadBool(foreignKey, "IsRequiredDependent") ? "||" : "o|")
                : "o{";
            var label = string.Join(", ", ReadEnumerableProperty(foreignKey, "Properties").Select(property => ReadString(property, "Name")));
            output.Append("  ").Append(principalId).Append(' ').Append(principalCardinality).Append("--")
                .Append(dependentCardinality).Append(' ').Append(identifiers[entity.Instance])
                .Append(" : \"").Append(label.Replace("\"", "\\\"", StringComparison.Ordinal)).AppendLine("\"");
        }

        return output.AppendLine("```").ToString();
    }

    private static IEnumerable<object> InvokeEnumerable(object target, string methodName)
    {
        var value = InvokeMethod(target, methodName)
            ?? throw new InvalidOperationException($"EF model metadata method '{methodName}' returned null.");
        return ((System.Collections.IEnumerable)value).Cast<object>();
    }

    private static IEnumerable<object> ReadEnumerableProperty(object target, string propertyName)
    {
        var value = ReadProperty(target, propertyName)
            ?? throw new InvalidOperationException($"EF model metadata property '{propertyName}' returned null.");
        return ((System.Collections.IEnumerable)value).Cast<object>();
    }

    private static object? InvokeMethod(object target, string methodName)
    {
        var targetType = target.GetType();
        var method = targetType.GetMethod(methodName, Type.EmptyTypes)
            ?? targetType.GetInterfaces()
                .Select(type => type.GetMethod(methodName, Type.EmptyTypes))
                .FirstOrDefault(candidate => candidate is not null);
        return method?.Invoke(target, null);
    }

    private static string? InvokeStringExtension(string methodName, object target)
    {
        var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type => type.IsAbstract && type.IsSealed)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(method => method.Name == methodName && method.GetParameters() is [{ } parameter] &&
                parameter.ParameterType.IsInstanceOfType(target));
        var method = methods.FirstOrDefault();
        return method?.Invoke(null, [target]) as string;
    }

    private static string ReadString(object target, string propertyName)
        => ReadProperty(target, propertyName)?.ToString()
           ?? throw new InvalidOperationException($"EF model metadata property '{propertyName}' was not available.");

    private static string ReadType(object target, string propertyName)
    {
        var type = (Type)ReadProperty(target, propertyName)!;
        return Nullable.GetUnderlyingType(type)?.Name ?? type.Name;
    }

    private static bool ReadBool(object target, string propertyName)
        => ReadProperty(target, propertyName) is true;

    private static object? ReadProperty(object target, string propertyName)
    {
        var targetType = target.GetType();
        var property = targetType.GetProperty(propertyName)
            ?? targetType.GetInterfaces()
                .Select(type => type.GetProperty(propertyName))
                .FirstOrDefault(candidate => candidate is not null);
        return property?.GetValue(target);
    }

    private static bool IsDbContext(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.FullName == "Microsoft.EntityFrameworkCore.DbContext")
                return true;
        }

        return false;
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var types = exception.Types.OfType<Type>().ToArray();
            if (types.Length == 0)
            {
                var loaderErrors = string.Join("; ", exception.LoaderExceptions
                    .Where(loaderException => loaderException is not null)
                    .Select(loaderException => loaderException!.Message));
                throw new InvalidOperationException($"Could not load types from '{assembly.Location}': {loaderErrors}", exception);
            }

            return types;
        }
    }

    private static string FormatTypes(IEnumerable<Type> types)
        => string.Join(", ", types.Select(type => type.FullName));

    private static string[] CreateUniqueIdentifiers(IEnumerable<string> values)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        return values.Select(value =>
        {
            var identifier = MermaidIdentifier(value);
            var candidate = identifier;
            var suffix = 2;
            while (!used.Add(candidate))
                candidate = $"{identifier}_{suffix++}";
            return candidate;
        }).ToArray();
    }

    private static string MermaidIdentifier(string value)
    {
        var identifier = new string(value.Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray());
        return identifier.Length > 0 && char.IsLetter(identifier[0]) ? identifier : $"Entity_{identifier}";
    }

    private static string MermaidType(string typeName)
        => typeName switch
        {
            "Boolean" => "boolean",
            "Byte" or "Int16" or "Int32" or "Int64" => "int",
            "Single" or "Double" or "Decimal" => "float",
            "DateTime" or "DateTimeOffset" or "DateOnly" => "datetime",
            "String" or "Char" => "string",
            "Guid" => "string",
            _ => MermaidIdentifier(typeName)
        };

    private sealed record EntityShape(
        object Instance,
        string Name,
        IReadOnlyList<PropertyShape> Properties,
        HashSet<string> PrimaryKey,
        HashSet<string> ForeignKeys);

    private sealed record PropertyShape(string Name, string ClrName, string TypeName, bool Nullable);

    private sealed class HostOwnedContext(object context, IDisposable scope, IDisposable? host) : IDisposable
    {
        public object Context { get; } = context;

        public void Dispose()
        {
            (Context as IDisposable)?.Dispose();
            scope.Dispose();
            host?.Dispose();
        }
    }

    private sealed class ProjectAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver[] _resolvers;
        private readonly Dictionary<string, string> _packageAssemblies;
        private readonly string[] _sharedFrameworkDirectories;

        public ProjectAssemblyLoadContext(params string[] assemblyPaths) : base(isCollectible: true)
        {
            _resolvers = assemblyPaths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => new AssemblyDependencyResolver(path))
                .ToArray();
            _packageAssemblies = assemblyPaths
                .SelectMany(FindPackageAssemblies)
                .GroupBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);
            var sharedDirectory = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory()).Parent?.Parent;
            var aspNetCoreDirectory = sharedDirectory is null
                ? null
                : Path.Combine(sharedDirectory.FullName, "Microsoft.AspNetCore.App");
            _sharedFrameworkDirectories = aspNetCoreDirectory is not null && Directory.Exists(aspNetCoreDirectory)
                ? Directory.GetDirectories(aspNetCoreDirectory)
                    .OrderByDescending(path => Version.TryParse(Path.GetFileName(path), out var version) ? version : new Version())
                    .ToArray()
                : [];
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = _resolvers
                .Select(resolver => resolver.ResolveAssemblyToPath(assemblyName))
                .FirstOrDefault(resolvedPath => resolvedPath is not null);
            if (path is null && assemblyName.Name is { } name)
                _packageAssemblies.TryGetValue(name, out path);
            if (path is null && assemblyName.Name is { } frameworkAssemblyName)
            {
                path = _sharedFrameworkDirectories
                    .Select(directory => Path.Combine(directory, $"{frameworkAssemblyName}.dll"))
                    .FirstOrDefault(File.Exists);
            }
            return path is null ? null : LoadFromAssemblyPath(path);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolvers
                .Select(resolver => resolver.ResolveUnmanagedDllToPath(unmanagedDllName))
                .FirstOrDefault(resolvedPath => resolvedPath is not null);
            return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
        }

        private static IEnumerable<KeyValuePair<string, string>> FindPackageAssemblies(string assemblyPath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyPath)!);
            while (directory is not null)
            {
                var assetsPath = Path.Combine(directory.FullName, "obj", "project.assets.json");
                if (File.Exists(assetsPath))
                    return ReadPackageAssemblies(assetsPath);
                directory = directory.Parent;
            }

            return [];
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadPackageAssemblies(string assetsPath)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(assetsPath));
            var root = document.RootElement;
            var targetFramework = root.GetProperty("project").GetProperty("restore").GetProperty("originalTargetFrameworks")
                .EnumerateArray().FirstOrDefault().GetString();
            var targets = root.GetProperty("targets");
            var target = targets.EnumerateObject()
                .FirstOrDefault(item => string.Equals(item.Name, targetFramework, StringComparison.OrdinalIgnoreCase) ||
                    item.Name.StartsWith($"{targetFramework}/", StringComparison.OrdinalIgnoreCase))
                .Value;
            if (target.ValueKind != JsonValueKind.Object)
                yield break;

            var libraries = root.GetProperty("libraries");
            var packageFolders = root.GetProperty("packageFolders").EnumerateObject()
                .Select(folder => folder.Name)
                .ToArray();
            foreach (var library in target.EnumerateObject())
            {
                if (!libraries.TryGetProperty(library.Name, out var metadata) ||
                    !metadata.TryGetProperty("path", out var packagePath))
                    continue;

                var packageRelativePath = packagePath.GetString();
                if (string.IsNullOrEmpty(packageRelativePath))
                    continue;
                foreach (var assetGroupName in new[] { "runtime", "runtimeTargets" })
                {
                    if (!library.Value.TryGetProperty(assetGroupName, out var assets))
                        continue;
                    foreach (var asset in assets.EnumerateObject())
                    {
                        if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                            asset.Name.Contains("_._", StringComparison.Ordinal))
                            continue;
                        foreach (var packageFolder in packageFolders)
                        {
                            var resolvedPath = Path.Combine(packageFolder, packageRelativePath, asset.Name.Replace('/', Path.DirectorySeparatorChar));
                            if (!File.Exists(resolvedPath))
                                continue;
                            yield return new KeyValuePair<string, string>(Path.GetFileNameWithoutExtension(asset.Name), resolvedPath);
                            break;
                        }
                    }
                }
            }
        }
    }
}
