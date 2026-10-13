using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace JD.Efcpt.Cli.Commands;

public static class MermaidFromDbContextCommand
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
        var outputOption = new Option<string>("--output")
        {
            Description = "Path to the Mermaid Markdown output file.",
            Required = true
        };

        var command = new Command("mermaid-from-dbcontext", "Generate a Mermaid ER diagram from a code-first DbContext.")
        {
            assemblyOption,
            contextOption,
            outputOption
        };

        command.SetAction(parseResult => Execute(
            parseResult.GetValue(assemblyOption)!,
            parseResult.GetValue(contextOption),
            parseResult.GetValue(outputOption)!));

        return command;
    }

    public static int Execute(string assemblyPath, string? contextName, string outputPath)
    {
        try
        {
            var fullAssemblyPath = Path.GetFullPath(assemblyPath);
            if (!File.Exists(fullAssemblyPath))
                throw new FileNotFoundException("The project assembly was not found.", fullAssemblyPath);

            var loadContext = new ProjectAssemblyLoadContext(fullAssemblyPath);
            try
            {
                var projectAssembly = loadContext.LoadFromAssemblyPath(fullAssemblyPath);
                var contextType = FindDbContextType(projectAssembly, contextName);
                using var context = CreateDbContext(projectAssembly, contextType);
                var markdown = RenderModel(context.GetType().GetProperty("Model")!.GetValue(context)!);

                var fullOutputPath = Path.GetFullPath(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
                File.WriteAllText(fullOutputPath, markdown, new UTF8Encoding(false));
                Console.WriteLine($"Generated Mermaid ER diagram: {fullOutputPath}");
                return 0;
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
            .Where(IsDbContext)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(contextName))
        {
            var selected = dbContextTypes.FirstOrDefault(type =>
                string.Equals(type.FullName, contextName, StringComparison.Ordinal) ||
                string.Equals(type.Name, contextName, StringComparison.Ordinal));
            return selected ?? throw new InvalidOperationException(
                $"DbContext '{contextName}' was not found. Discovered contexts: {FormatTypes(dbContextTypes)}");
        }

        return dbContextTypes.Length switch
        {
            0 => throw new InvalidOperationException($"No DbContext types were found in '{assembly.Location}'."),
            1 => dbContextTypes[0],
            _ => throw new InvalidOperationException(
                $"Multiple DbContext types were found. Set EfcptCodeFirstDbContextName to one of: {FormatTypes(dbContextTypes)}")
        };
    }

    private static object CreateDbContext(Assembly assembly, Type contextType)
    {
        var factoryInterfaceName = "Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory`1";
        var factories = GetLoadableTypes(assembly)
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
            var createMethod = factories[0].GetMethods()
                .First(method => method.Name == "CreateDbContext" && method.GetParameters().Length == 1);
            return createMethod.Invoke(factory, [Array.Empty<string>()])
                ?? throw new InvalidOperationException($"Design-time factory '{factories[0].FullName}' returned null.");
        }

        var host = TryCreateHostContext(assembly, contextType);
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

        var scopeFactory = services.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
        if (scopeFactory is null)
        {
            (host as IDisposable)?.Dispose();
            throw new InvalidOperationException("The application host did not provide an IServiceScopeFactory.");
        }

        var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetService(contextType);
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
                InvokeStringExtension("GetTableName", entity) ?? ReadString(entity, "Name"),
                InvokeStringExtension("GetSchema", entity),
                InvokeEnumerable(entity, "GetProperties").Select(property => new PropertyShape(
                    ReadString(property, "Name"),
                    ReadType(property, "ClrType"),
                    ReadBool(property, "IsNullable"),
                    property)).ToArray(),
                InvokeEnumerable(entity, "GetKeys")
                    .FirstOrDefault(key => ReadBool(key, "IsPrimaryKey")) is { } primaryKey
                    ? InvokeEnumerable(primaryKey, "Properties").Select(property => ReadString(property, "Name")).ToHashSet(StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal)))
            .OrderBy(entity => entity.Schema, StringComparer.Ordinal)
            .ThenBy(entity => entity.Table, StringComparer.Ordinal)
            .ToArray();

        if (entityTypes.Length == 0)
            throw new InvalidOperationException("The selected DbContext model contains no entity types.");

        var identifiers = entityTypes.ToDictionary(
            entity => entity.Instance,
            entity => MermaidIdentifier(entity.Table),
            ReferenceEqualityComparer.Instance);
        var output = new StringBuilder("# Entity Relationship Diagram\n\n```mermaid\nerDiagram\n");
        foreach (var entity in entityTypes)
        {
            output.Append("  ").Append(identifiers[entity.Instance]).AppendLine(" {");
            foreach (var property in entity.Properties)
            {
                output.Append("    ").Append(MermaidType(property.TypeName)).Append(' ')
                    .Append(MermaidIdentifier(property.Name));
                if (entity.PrimaryKey.Contains(property.Name))
                    output.Append(" PK");
                if (property.Nullable)
                    output.Append(" \"nullable\"");
                output.AppendLine();
            }

            output.AppendLine("  }");
        }

        foreach (var entity in entityTypes)
        foreach (var foreignKey in InvokeEnumerable(entity.Instance, "GetForeignKeys"))
        {
            var principal = foreignKey.GetType().GetProperty("PrincipalEntityType")!.GetValue(foreignKey)!;
            if (!identifiers.TryGetValue(principal, out var principalId))
                continue;
            var required = ReadBool(foreignKey, "IsRequired");
            var unique = ReadBool(foreignKey, "IsUnique");
            var principalCardinality = unique ? "||" : "||";
            var dependentCardinality = unique ? (required ? "||" : "|o") : (required ? "|{" : "o{");
            var label = string.Join(", ", InvokeEnumerable(foreignKey, "Properties").Select(property => ReadString(property, "Name")));
            output.Append("  ").Append(principalId).Append(' ').Append(principalCardinality).Append("--")
                .Append(dependentCardinality).Append(' ').Append(identifiers[entity.Instance])
                .Append(" : \"").Append(label.Replace("\"", "\\\"", StringComparison.Ordinal)).AppendLine("\"");
        }

        return output.AppendLine("```").ToString();
    }

    private static IEnumerable<object> InvokeEnumerable(object target, string methodName)
    {
        var value = target.GetType().GetMethod(methodName, Type.EmptyTypes)?.Invoke(target, null)
            ?? throw new InvalidOperationException($"EF model metadata method '{methodName}' returned null.");
        return ((System.Collections.IEnumerable)value).Cast<object>();
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
        => target.GetType().GetProperty(propertyName)?.GetValue(target)?.ToString()
           ?? throw new InvalidOperationException($"EF model metadata property '{propertyName}' was not available.");

    private static string ReadType(object target, string propertyName)
    {
        var type = (Type)target.GetType().GetProperty(propertyName)!.GetValue(target)!;
        return Nullable.GetUnderlyingType(type)?.Name ?? type.Name;
    }

    private static bool ReadBool(object target, string propertyName)
        => target.GetType().GetProperty(propertyName)?.GetValue(target) is true;

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
            return exception.Types.OfType<Type>().ToArray();
        }
    }

    private static string FormatTypes(IEnumerable<Type> types)
        => string.Join(", ", types.Select(type => type.FullName));

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
        string Table,
        string? Schema,
        IReadOnlyList<PropertyShape> Properties,
        HashSet<string> PrimaryKey);

    private sealed record PropertyShape(string Name, string TypeName, bool Nullable, object Instance);

    private sealed class HostOwnedContext(object context, IServiceScope scope, IDisposable? host) : IDisposable
    {
        public object Context { get; } = context;

        public void Dispose()
        {
            (Context as IDisposable)?.Dispose();
            scope.Dispose();
            host?.Dispose();
        }
    }

    private sealed class ProjectAssemblyLoadContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: true)
    {
        private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path is null ? null : LoadFromAssemblyPath(path);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
        }
    }
}
