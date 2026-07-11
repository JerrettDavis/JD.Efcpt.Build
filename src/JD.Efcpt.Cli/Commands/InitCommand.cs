using System.CommandLine;
using JD.Efcpt.Build.Core.Config;
using JD.Efcpt.Build.Core.Providers;
using JD.Efcpt.Cli.Logging;

namespace JD.Efcpt.Cli.Commands;

/// <summary>
/// <c>jd-efcpt init</c>: bootstraps an <c>efcpt-config.json</c> file.
/// </summary>
/// <remarks>
/// Offline-first: by default, the config is generated from the schema bundled with this tool
/// (<c>tools-assets/efcpt-config.schema.json</c>, copied next to the tool's own assembly at pack
/// time) - no network access is required. Pass <c>--online</c> to instead fetch the latest schema
/// from GitHub (primary URL, falling back to a secondary mirror), matching
/// <see cref="EfcptConfigGenerator.GenerateFromUrlAsync"/>'s existing behavior.
/// </remarks>
public static class InitCommand
{
    /// <summary>Builds the <c>init</c> subcommand.</summary>
    public static Command Build()
    {
        var outputDirArgument = new Argument<string?>("output-dir")
        {
            Description = "Directory to write efcpt-config.json into (defaults to the current directory).",
            Arity = ArgumentArity.ZeroOrOne
        };

        var providerOption = new Option<string?>("--provider")
        {
            Description = "Database provider (mssql, postgres, mysql, sqlite, oracle, firebird, snowflake). Validated only; recorded for your reference."
        };

        var dbContextNameOption = new Option<string?>("--dbcontext-name")
        {
            Description = "Name of the generated DbContext class (default: ApplicationDbContext)."
        };

        var namespaceOption = new Option<string?>("--namespace")
        {
            Description = "Root namespace for generated code (default: EfcptProject)."
        };

        var forceOption = new Option<bool>("--force")
        {
            Description = "Overwrite an existing efcpt-config.json."
        };

        var onlineOption = new Option<bool>("--online")
        {
            Description = "Fetch the latest efcpt-config schema from GitHub instead of using the schema bundled with this tool."
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Emit detailed diagnostics, including full exception details (type + stack trace) on failure."
        };

        var command = new Command("init", "Bootstrap an efcpt-config.json file.")
        {
            outputDirArgument,
            providerOption,
            dbContextNameOption,
            namespaceOption,
            forceOption,
            onlineOption,
            verboseOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var log = new ConsoleBuildLog { Verbose = parseResult.GetValue(verboseOption) };
            return await ExecuteAsync(
                log,
                parseResult.GetValue(outputDirArgument),
                parseResult.GetValue(providerOption),
                parseResult.GetValue(dbContextNameOption),
                parseResult.GetValue(namespaceOption),
                parseResult.GetValue(forceOption),
                parseResult.GetValue(onlineOption),
                cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// Runs the <c>init</c> command logic. Exposed separately from <see cref="Build"/> so tests
    /// can drive it directly without going through <c>System.CommandLine</c> argument parsing.
    /// </summary>
    /// <returns>0 on success; 1 on any error (unsupported provider, existing file without
    /// <paramref name="force"/>, schema read/fetch failure, or write failure).</returns>
    public static async Task<int> ExecuteAsync(
        ConsoleBuildLog log,
        string? outputDir,
        string? provider,
        string? dbContextName,
        string? rootNamespace,
        bool force,
        bool online,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(provider))
            {
                // Validated only (throws NotSupportedException, listing valid values, if
                // unrecognized); efcpt-config.json has no top-level "provider" property, so the
                // normalized value is not otherwise written into the generated config.
                ProviderNames.Normalize(provider);
            }

            var targetDir = string.IsNullOrWhiteSpace(outputDir) ? Directory.GetCurrentDirectory() : Path.GetFullPath(outputDir);
            Directory.CreateDirectory(targetDir);

            var configPath = Path.Combine(targetDir, "efcpt-config.json");
            if (File.Exists(configPath) && !force)
            {
                log.Error($"'{configPath}' already exists. Pass --force to overwrite it.");
                return 1;
            }

            string json;
            if (online)
            {
                log.Info("Fetching efcpt-config schema from GitHub (--online)...");
                json = await EfcptConfigGenerator.GenerateFromUrlAsync(
                    dbContextName: dbContextName, rootNamespace: rootNamespace, log: log);
            }
            else
            {
                // The bundled schema (../../lib/efcpt-config.schema.json, see the csproj's None
                // item) is copied flat into the build/publish output directory next to this
                // assembly - CopyToOutputDirectory does not honor the nupkg-only
                // PackagePath="tools-assets/" used for packing, so at runtime (including once
                // installed as a global/local dotnet tool) it lands directly in
                // AppContext.BaseDirectory, not a "tools-assets" subdirectory.
                var bundledSchemaPath = Path.Combine(AppContext.BaseDirectory, "efcpt-config.schema.json");
                if (!File.Exists(bundledSchemaPath))
                {
                    log.Error(
                        $"Bundled schema not found at '{bundledSchemaPath}'. Pass --online to fetch it from GitHub instead.");
                    return 1;
                }

                log.Detail($"Using bundled schema: {bundledSchemaPath}");
                json = EfcptConfigGenerator.GenerateFromFile(bundledSchemaPath, dbContextName, rootNamespace);
            }

            await File.WriteAllTextAsync(configPath, json, cancellationToken);
            log.Info($"Wrote {configPath}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Always surface the exception type alongside the message so a bare "file not found"
            // or "unauthorized" is attributable; the full ToString() (type + stack) is gated
            // behind --verbose to keep the default output clean.
            log.Error($"{ex.GetType().Name}: {ex.Message}");
            if (log.Verbose)
                log.Error(ex.ToString());
            return 1;
        }
    }
}
