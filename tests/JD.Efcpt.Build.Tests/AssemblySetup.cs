using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace JD.Efcpt.Build.Tests;

// Ensure MSBuild assemblies are discoverable for the task types at test load time.
public static class AssemblySetup
{
    [ModuleInitializer]
    public static void RegisterMsBuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
