using JD.Efcpt.Cli.Commands;
using JD.Efcpt.Cli.Tests.Infrastructure;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace JD.Efcpt.Cli.Tests.Commands;

[Feature("Code-first Mermaid generation discovers and reads an EF Core model")]
[Collection(nameof(AssemblySetup))]
public sealed partial class MermaidFromDbContextCommandTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record Fixture(TestFolder Folder, string ProjectPath, string AssemblyPath, string BuildDiagramPath) : IDisposable
    {
        public void Dispose() => Folder.Dispose();
    }

    private sealed record RunResult(Fixture Fixture, int ExitCode, string Output);

    [Scenario("Design-time factory, application host, and parameterless discovery generate Mermaid without connecting to a database")]
    [Fact]
    public async Task Generates_diagram_from_code_first_model()
        => await Given("a compiled code-first project with multiple DbContexts", () => BuildFixture())
            .When("testing context selection and design-time creation patterns", fixture =>
            {
                var ambiguous = Run(fixture, null, "ambiguous.md");
                var factory = Run(fixture, "StoreContext", "store.md");
                var hosted = Run(fixture, "HostedContext", "hosted.md");
                var parameterless = Run(fixture, "ParameterlessContext", "parameterless.md");
                var ambiguousShortName = Run(fixture, "DuplicateContext", "duplicate.md");
                var abstractContext = Run(fixture, "AbstractContext", "abstract.md");
                return new TestResult(fixture, ambiguous, factory, hosted, parameterless, ambiguousShortName, abstractContext);
            })
            .Then("multiple contexts require a selector and each design-time creation pattern produces a diagram", result =>
            {
                if (result.Ambiguous.ExitCode == 0 ||
                    !result.Ambiguous.Output.Contains("Multiple DbContext types were found", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Expected an ambiguity diagnostic. Output: {result.Ambiguous.Output}");
                if (result.Factory.ExitCode != 0)
                    throw new InvalidOperationException($"Factory-based Mermaid generation failed: {result.Factory.Output}");
                if (result.Hosted.ExitCode != 0)
                    throw new InvalidOperationException($"Host-based Mermaid generation failed: {result.Hosted.Output}");
                if (result.Parameterless.ExitCode != 0)
                    throw new InvalidOperationException($"Parameterless context generation failed: {result.Parameterless.Output}");
                if (result.AmbiguousShortName.ExitCode == 0 ||
                    !result.AmbiguousShortName.Output.Contains("is ambiguous", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Expected an ambiguous short-name diagnostic. Output: {result.AmbiguousShortName.Output}");
                if (result.AbstractContext.ExitCode == 0 ||
                    !result.AbstractContext.Output.Contains("was not found", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Expected abstract contexts to be excluded from discovery. Output: {result.AbstractContext.Output}");

                var markdown = File.ReadAllText(Path.Combine(result.Fixture.Folder.Root, "store.md"));
                foreach (var expected in new[]
                         {
                             "CodeFirst_Store {",
                             "CodeFirst_First_Second_Record {",
                             "CodeFirst_First_Second_Record_2 {",
                             "string store_title",
                             "string store_title_2",
                             "||--o{",
                             "int StoreId FK"
                         })
                {
                    if (!markdown.Contains(expected, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Expected '{expected}' in generated Mermaid Markdown: {markdown}");
                }

                if (!File.Exists(Path.Combine(result.Fixture.Folder.Root, "parameterless.md")))
                    throw new InvalidOperationException("The parameterless context did not produce its Mermaid file.");
                if (!File.Exists(Path.Combine(result.Fixture.Folder.Root, "hosted.md")))
                    throw new InvalidOperationException("The host-created context did not produce its Mermaid file.");
                if (!File.Exists(result.Fixture.BuildDiagramPath))
                    throw new InvalidOperationException("The post-build target did not create a Mermaid diagram.");
                if (!File.Exists(Path.Combine(result.Fixture.Folder.Root, "Code First", "Generated", "CodeFirst.net10.0-windows.md")))
                    throw new InvalidOperationException("The post-build target did not create a diagram for the second target framework.");
                var buildDiagram = File.ReadAllText(result.Fixture.BuildDiagramPath);
                if (!buildDiagram.Contains("CodeFirst_Store {", StringComparison.Ordinal))
                    throw new InvalidOperationException($"The post-build target did not export the selected context model: {buildDiagram}");
                return true;
            })
            .Finally(result => result.Fixture.Dispose())
            .AssertPassed();

    [Scenario("Standalone code-first Mermaid generation is enabled for each target framework and honors EfcptEnabled")]
    [Fact]
    public async Task Standalone_target_dispatches_code_first_generation_to_inner_builds()
        => await Given("a multi-targeted project with code-first Mermaid unset and EfcptEnabled=false", () => BuildFixture(enableCodeFirstMermaid: false))
            .When("invoking the standalone target with EfcptEnabled=false and then true", fixture =>
            {
                var disabled = RunStandalone(fixture);
                var disabledGeneratedDiagrams =
                    File.Exists(Path.Combine(fixture.Folder.Root, "Code First", "Generated", "CodeFirst.net10.0.md")) ||
                    File.Exists(Path.Combine(fixture.Folder.Root, "Code First", "Generated", "CodeFirst.net10.0-windows.md"));
                var enabled = RunStandalone(fixture, "EfcptEnabled=true");
                return new StandaloneResult(fixture, disabled, disabledGeneratedDiagrams, enabled);
            })
            .Then("the disabled invocation skips generation and the enabled invocation generates diagrams for each target framework", result =>
            {
                if (result.Disabled.ExitCode != 0)
                    throw new InvalidOperationException($"The disabled standalone target failed: {result.Disabled.Output}");
                if (result.DisabledGeneratedDiagrams)
                    throw new InvalidOperationException("The standalone target generated diagrams while EfcptEnabled=false.");
                if (result.Enabled.ExitCode != 0)
                    throw new InvalidOperationException($"The enabled standalone target failed: {result.Enabled.Output}");
                if (!File.Exists(Path.Combine(result.Fixture.Folder.Root, "Code First", "Generated", "CodeFirst.net10.0.md")) ||
                    !File.Exists(Path.Combine(result.Fixture.Folder.Root, "Code First", "Generated", "CodeFirst.net10.0-windows.md")))
                    throw new InvalidOperationException($"The standalone target did not generate diagrams for each target framework: {result.Enabled.Output}");
                return true;
            })
            .Finally(result => result.Fixture.Dispose())
            .AssertPassed();

    private sealed record TestResult(
        Fixture Fixture,
        RunResult Ambiguous,
        RunResult Factory,
        RunResult Hosted,
        RunResult Parameterless,
        RunResult AmbiguousShortName,
        RunResult AbstractContext);

    private sealed record StandaloneResult(
        Fixture Fixture,
        (int ExitCode, string Output) Disabled,
        bool DisabledGeneratedDiagrams,
        (int ExitCode, string Output) Enabled);

    private static Fixture BuildFixture(bool enableCodeFirstMermaid = true)
    {
        var folder = new TestFolder();
        var projectDir = Path.Combine(folder.Root, "Code First");
        Directory.CreateDirectory(projectDir);
        var buildRoot = Path.Combine(FindRepoRoot(), "src", "JD.Efcpt.Build").Replace('\\', '/');
        var projectPath = Path.Combine(projectDir, "CodeFirst.csproj");
        var codeFirstProperty = enableCodeFirstMermaid
            ? "    <EfcptCodeFirstMermaid>true</EfcptCodeFirstMermaid>\n"
            : "    <EfcptEnabled>false</EfcptEnabled>\n";
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>
                <EnableWindowsTargeting>true</EnableWindowsTargeting>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <Import Project="__EFCPT_BUILD_ROOT__/JD.Efcpt.Build.props" />
              <PropertyGroup>
            __CODE_FIRST_PROPERTY__
                <EfcptCodeFirstDbContextName>StoreContext</EfcptCodeFirstDbContextName>
                <EfcptMermaidOutputDir>$(MSBuildProjectDirectory)/Generated</EfcptMermaidOutputDir>
              </PropertyGroup>
              <Import Project="__EFCPT_BUILD_ROOT__/JD.Efcpt.Build.targets" />
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.1" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
                <FrameworkReference Include="Microsoft.AspNetCore.App" />
              </ItemGroup>
            </Project>
            """.Replace("__EFCPT_BUILD_ROOT__", buildRoot, StringComparison.Ordinal)
                .Replace("__CODE_FIRST_PROPERTY__", codeFirstProperty.TrimEnd('\n'), StringComparison.Ordinal);
        File.WriteAllText(projectPath, projectContent);
        File.WriteAllText(Path.Combine(projectDir, "Contexts.cs"), """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore.Design;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            namespace CodeFirst;

            public abstract class AbstractContext(DbContextOptions options) : DbContext(options)
            {
            }

            public sealed class StoreContext(DbContextOptions<StoreContext> options) : AbstractContext(options)
            {
                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<Store>(entity =>
                    {
                        entity.ToTable("store_records");
                        entity.HasKey(item => item.Id);
                        entity.Property(item => item.Id).HasColumnName("store_id");
                        entity.Property(item => item.Title).HasColumnName("store-title");
                        entity.Property<string>("StoreName").HasColumnName("store_title");
                        entity.HasMany(item => item.Products).WithOne(item => item.Store).HasForeignKey(item => item.StoreId);
                    });
                    modelBuilder.Entity<Product>(entity =>
                    {
                        entity.ToTable("product_records");
                        entity.HasKey(item => item.Id);
                    });
                    modelBuilder.Entity<First_Second.Record>().HasKey(item => item.Id);
                    modelBuilder.Entity<First.Second_Record>().HasKey(item => item.Id);
                }

                public DbSet<Store> Stores => Set<Store>();
                public DbSet<Product> Products => Set<Product>();
            }

            public sealed class StoreContextFactory : IDesignTimeDbContextFactory<StoreContext>
            {
                public StoreContext CreateDbContext(string[] args)
                    => new(new DbContextOptionsBuilder<StoreContext>().UseSqlServer("Server=localhost;Database=unused").Options);
            }

            public sealed class ParameterlessContext : DbContext
            {
                public ParameterlessContext()
                {
                }

                protected override void OnConfiguring(DbContextOptionsBuilder options)
                    => options.UseSqlServer("Server=localhost;Database=unused");

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                    => modelBuilder.Entity<ParameterlessEntity>().HasKey(entity => entity.Id);
            }

            public sealed class HostedContext(DbContextOptions<HostedContext> options) : DbContext(options)
            {
                protected override void OnModelCreating(ModelBuilder modelBuilder)
                    => modelBuilder.Entity<HostedEntity>().HasKey(entity => entity.Id);
            }

            public static class Program
            {
                public static IHostBuilder CreateHostBuilder(string[] args)
                    => Host.CreateDefaultBuilder(args)
                        .ConfigureServices((_, services) => services.AddDbContext<HostedContext>(options =>
                            options.UseSqlServer("Server=localhost;Database=unused")));
            }

            public sealed class Store
            {
                public int Id { get; set; }
                public string Title { get; set; } = "";
                public List<Product> Products { get; set; } = [];
            }

            public sealed class Product
            {
                public int Id { get; set; }
                public int StoreId { get; set; }
                public Store Store { get; set; } = null!;
            }

            public sealed class ParameterlessEntity
            {
                public int Id { get; set; }
            }

            public sealed class HostedEntity
            {
                public int Id { get; set; }
            }

            public static class First
            {
                public sealed class DuplicateContext : DbContext
                {
                    public DuplicateContext() { }
                }

                public sealed class Second_Record
                {
                    public int Id { get; set; }
                }
            }

            public static class Second
            {
                public sealed class DuplicateContext : DbContext
                {
                    public DuplicateContext() { }
                }
            }

            public static class First_Second
            {
                public sealed class Record
                {
                    public int Id { get; set; }
                }
            }

            """);

        var buildConfiguration = BuildConfiguration();
        var build = RunProcess("dotnet", projectDir, ["build", projectPath, "--configuration", buildConfiguration, "--nologo", "-v:q"]);
        if (build.ExitCode != 0)
        {
            folder.Dispose();
            throw new InvalidOperationException($"Could not build the code-first test fixture: {build.Output}");
        }

        var assemblyPath = Path.Combine(projectDir, "bin", buildConfiguration, "net10.0", "CodeFirst.dll");
        var buildDiagramPath = Path.Combine(projectDir, "Generated", "CodeFirst.net10.0.md");
        return new Fixture(folder, projectPath, assemblyPath, buildDiagramPath);
    }

    private static (int ExitCode, string Output) RunStandalone(Fixture fixture, string? property = null)
    {
        var arguments = new List<string>
        {
            "msbuild",
            fixture.ProjectPath,
            "-target:EfcptGenerateMermaidFromDbContext",
            "--nologo",
            "-v:q",
            $"-p:Configuration={BuildConfiguration()}"
        };
        if (property is not null)
            arguments.Add($"-p:{property}");

        return RunProcess("dotnet", Path.GetDirectoryName(fixture.ProjectPath)!, arguments);
    }

    private static RunResult Run(Fixture fixture, string? contextName, string outputFile)
    {
        var cliAssembly = Path.Combine(FindRepoRoot(), "src", "JD.Efcpt.Cli", "bin", BuildConfiguration(), "net8.0", "JD.Efcpt.Cli.dll");
        var outputPath = Path.Combine(fixture.Folder.Root, outputFile);
        var arguments = new List<string> { cliAssembly, "mermaid-from-dbcontext", "--assembly", fixture.AssemblyPath };
        if (contextName is not null)
        {
            arguments.Add("--context");
            arguments.Add(contextName);
        }

        arguments.Add("--output");
        arguments.Add(outputPath);
        var result = RunProcess("dotnet", fixture.Folder.Root, arguments);
        return new RunResult(fixture, result.ExitCode, result.Output);
    }

    private static (int ExitCode, string Output) RunProcess(string executable, string workingDirectory, IEnumerable<string> arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{executable}'.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(180000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"'{executable}' did not exit within three minutes.");
        }

        Task.WaitAll(stdoutTask, stderrTask);
        return (process.ExitCode, stdoutTask.Result + stderrTask.Result);
    }

    private static string BuildConfiguration()
        => new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "JD.Efcpt.Build.sln")))
            current = current.Parent;

        return current?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
