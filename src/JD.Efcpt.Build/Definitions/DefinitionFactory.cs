using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Packaging;

namespace JDEfcptBuild;

public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        var def = new PackageDefinition
        {
            Id = "JD.Efcpt.Build",
            BuildProps = BuildPropsFactory.Create(),
            BuildTargets = BuildTargetsFactory.Create(),
            BuildTransitiveProps = BuildTransitivePropsFactory.Create(),
            BuildTransitiveTargets = BuildTransitiveTargetsFactory.Create()
        };
        
        // Enable buildTransitive folder generation
        def.Packaging.BuildTransitive = true;
        
        return def;
    }
}
