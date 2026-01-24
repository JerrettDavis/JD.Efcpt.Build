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
                .Import("Sdk.props", "Microsoft.NET.Sdk")
                .Import("$(MSBuildThisFileDirectory)..\\build\\JD.Efcpt.Sdk.props"))
            // Sdk/Sdk.targets - imports Microsoft.NET.Sdk then our targets
            .SdkTargets(t => t
                .Import("Sdk.targets", "Microsoft.NET.Sdk")
                .Import("$(MSBuildThisFileDirectory)..\\build\\JD.Efcpt.Sdk.targets"))
            // build/JD.Efcpt.Sdk.props - SDK-specific properties
            .BuildProps(p => p
                .Property("_EfcptIsDirectReference", "true")
                .PropertyGroup("'$(EfcptCheckForUpdates)' == ''", g => g
                    .Property("EfcptCheckForUpdates", "true"))
                .Import("$(MSBuildThisFileDirectory)JD.Efcpt.Sdk.Version.props",
                    condition: "Exists('$(MSBuildThisFileDirectory)JD.Efcpt.Sdk.Version.props')")
                .Import("$(MSBuildThisFileDirectory)JD.Efcpt.Build.props"))
            // build/JD.Efcpt.Sdk.targets - imports shared targets
            .BuildTargets(t => t
                .Import("$(MSBuildThisFileDirectory)JD.Efcpt.Build.targets"))
            .Pack(o =>
            {
                o.EmitSdk = true;
                o.SdkFlatLayout = true;
            })
            .Build();
    }
}
