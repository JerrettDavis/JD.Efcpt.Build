using System.Reflection;
using System.Runtime.InteropServices;

namespace JD.Efcpt.Build.Tasks;

/// <summary>
/// Helper to resolve native libraries when running inside MSBuild's task host.
/// </summary>
/// <remarks>
/// <para>
/// When MSBuild loads task assemblies, the default native library resolution doesn't
/// work properly with the runtimes/{rid}/native folder structure. This helper registers
/// a custom resolver to find native libraries (like Microsoft.Data.SqlClient.SNI.dll)
/// in the correct location.
/// </para>
/// </remarks>
internal static class NativeLibraryLoader
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures native library resolution is configured for the task assembly.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            // Register resolver for Microsoft.Data.SqlClient assembly
            try
            {
                var sqlClientAssembly = typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly;
                NativeLibrary.SetDllImportResolver(sqlClientAssembly, ResolveNativeLibrary);
            }
            catch (InvalidOperationException)
            {
                // A resolver is already set - that's fine
            }

            _initialized = true;
        }
    }

    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Handle SNI library for SQL Server
        if (libraryName.Contains("Microsoft.Data.SqlClient.SNI", StringComparison.OrdinalIgnoreCase))
        {
            return TryLoadFromRuntimesFolder(libraryName, "Microsoft.Data.SqlClient.SNI.dll");
        }

        // Default resolution
        return IntPtr.Zero;
    }

    private static IntPtr TryLoadFromRuntimesFolder(string libraryName, string fileName)
    {
        // Get the directory where the Tasks DLL is located
        var tasksDir = Path.GetDirectoryName(typeof(NativeLibraryLoader).Assembly.Location);
        if (string.IsNullOrEmpty(tasksDir))
            return IntPtr.Zero;

        // Determine the runtime identifier
        var rid = GetRuntimeIdentifier();

        // Try to load from runtimes/{rid}/native
        var nativePath = Path.Combine(tasksDir, "runtimes", rid, "native", fileName);
        if (File.Exists(nativePath) && NativeLibrary.TryLoad(nativePath, out var handle))
        {
            return handle;
        }

        // Fallback: try platform-generic path (e.g., runtimes/win/native)
        var genericRid = GetGenericRuntimeIdentifier();
        if (genericRid != rid)
        {
            nativePath = Path.Combine(tasksDir, "runtimes", genericRid, "native", fileName);
            if (File.Exists(nativePath) && NativeLibrary.TryLoad(nativePath, out handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private static string GetRuntimeIdentifier()
    {
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64"
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"win-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"linux-{arch}";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"osx-{arch}";

        return $"win-{arch}"; // Fallback
    }

    private static string GetGenericRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx";

        return "win"; // Fallback
    }
}
