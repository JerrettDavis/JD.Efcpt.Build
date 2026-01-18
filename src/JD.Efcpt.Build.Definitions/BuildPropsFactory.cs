using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.Typed;

namespace JD.Efcpt.Build.Definitions;

/// <summary>
/// MSBuild package definition scaffolded from JD.Efcpt.Build.xml
/// </summary>
public static class BuildPropsFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("JD.Efcpt.Build")
            .Props(p =>
            {
                p.Property<EfcptIsDirectReference>("true");
                p.Import("..\\buildTransitive\\JD.Efcpt.Build.props");
            })
            .Build();
    }

    // Strongly-typed property names
    public readonly struct EfcptIsDirectReference : IMsBuildPropertyName
    {
        public string Name => "_EfcptIsDirectReference";
    }
}


