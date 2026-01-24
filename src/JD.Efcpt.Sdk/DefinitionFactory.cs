using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Sdk.Definitions;

/// <summary>
/// Main definition factory for JD.Efcpt.Sdk package.
/// </summary>
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("JD.Efcpt.Sdk")
            .Description("MSBuild SDK for Entity Framework Core power tools")
            // Sdk/Sdk.props - imports Microsoft.NET.Sdk then our props
            .SdkProps(p => p
                .Comment(@"
    JD.Efcpt.Sdk - MSBuild SDK for EF Core Power Tools Build Integration

    This SDK extends Microsoft.NET.Sdk to provide automatic EF Core code generation
    from DACPAC files, SQL projects, or database connections during build.

    Usage:
      <Project Sdk=""JD.Efcpt.Sdk/1.0.0"">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
        </PropertyGroup>
      </Project>
  ")
                .Comment("Import Microsoft.NET.Sdk props first (base .NET SDK)")
                .Import("Sdk.props", sdk: "Microsoft.NET.Sdk")
                .Comment("Import our SDK-specific props")
                .Import("$(MSBuildThisFileDirectory)..\\build\\JD.Efcpt.Sdk.props"))
            // Sdk/Sdk.targets - imports Microsoft.NET.Sdk then our targets
            .SdkTargets(t => t
                .Comment(@"
    JD.Efcpt.Sdk - MSBuild SDK Targets

    Imports Microsoft.NET.Sdk targets first, then our SDK-specific targets.
    This ensures our targets run after the standard .NET SDK targets are defined.
  ")
                .Comment("Import Microsoft.NET.Sdk targets first (base .NET SDK)")
                .Import("Sdk.targets", sdk: "Microsoft.NET.Sdk")
                .Comment("Import our SDK-specific targets")
                .Import("$(MSBuildThisFileDirectory)..\\build\\JD.Efcpt.Sdk.targets"))
            // build/JD.Efcpt.Sdk.props - SDK-specific properties
            .BuildProps(p => p
                .Comment(@"
    JD.Efcpt.Sdk Props

    This file imports the shared property definitions from the build folder.
    The build folder contains the actual EFCPT configuration properties.

    NOTE: We use build/ (not buildTransitive/) so targets only apply to
    projects that DIRECTLY use this SDK, not transitive consumers.
  ")
                .Comment(@"
    Mark this as a direct SDK reference.
    This marker is used to only enable generation for direct consumers,
    not transitive ones.
  ")
                .PropertyGroup(null, g => g
                    .Property("_EfcptIsDirectReference", "true")
                    .Comment(@"
      SDK users get automatic version checking enabled by default.
      This helps ensure SDK users are always aware of updates.
      Users can still opt-out by setting EfcptCheckForUpdates=false in their project.
    ")
                    .Property("EfcptCheckForUpdates", "true", "'$(EfcptCheckForUpdates)' == ''"))
                .Comment("Import SDK version for update check feature")
                .Import("$(MSBuildThisFileDirectory)JD.Efcpt.Sdk.Version.props",
                    condition: "Exists('$(MSBuildThisFileDirectory)JD.Efcpt.Sdk.Version.props')")
                .Comment("Import the shared props (same as JD.Efcpt.Build uses)")
                .Import("$(MSBuildThisFileDirectory)JD.Efcpt.Build.props"))
            // build/JD.Efcpt.Sdk.targets - imports shared targets
            .BuildTargets(t => t
                .Comment(@"
    JD.Efcpt.Sdk Targets

    This file imports the shared target definitions from the build folder.
    The build folder contains the actual EFCPT build targets and tasks.

    NOTE: We use build/ (not buildTransitive/) so targets only apply to
    projects that DIRECTLY use this SDK, not transitive consumers.
  ")
                .Comment("Import the shared targets (same as JD.Efcpt.Build uses)")
                .Import("$(MSBuildThisFileDirectory)JD.Efcpt.Build.targets"))
            .Pack(o =>
            {
                o.EmitSdk = true;
                o.SdkFlatLayout = true;
            })
            .Build();
    }
}
