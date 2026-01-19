using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// MSBuild package definition scaffolded from JD.Efcpt.Build.xml
/// </summary>
public static class BuildTargetsFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("JD.Efcpt.Build")
            .Targets(t =>
            {
                t.Import("..\\buildTransitive\\JD.Efcpt.Build.targets");
            })
            .Build();
    }
}
