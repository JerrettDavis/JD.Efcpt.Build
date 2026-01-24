using JD.MSBuild.Fluent;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// Main definition factory for JD.Efcpt.Build package.
/// This factory coordinates all the build, buildTransitive, and SDK assets.
/// </summary>
public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        var buildProps = BuildPropsFactory.Create();
        var buildTargets = BuildTargetsFactory.Create();
        var buildTransitiveProps = BuildTransitivePropsFactory.Create();
        var buildTransitiveTargets = BuildTransitiveTargetsFactory.Create();
        
        return new PackageDefinition
        {
            Id = "JD.Efcpt.Build",
            Description = "MSBuild tasks and targets for Entity Framework Core power tools",
            BuildProps = buildProps.Props,
            BuildTargets = buildTargets.Targets,
            BuildTransitiveProps = buildTransitiveProps.Props,
            BuildTransitiveTargets = buildTransitiveTargets.Targets,
            Packaging =
            {
                BuildTransitive = true
            }
        };
    }
}
