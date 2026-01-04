using Xunit;

namespace JD.Efcpt.Sdk.IntegrationTests;

/// <summary>
/// Collection fixture that provides access to the assembly-level packed packages.
/// The actual packing happens once at assembly load via AssemblyFixture.
/// </summary>
public class SdkPackageTestFixture
{
    public string PackageOutputPath => AssemblyFixture.PackageOutputPath;
    public string SdkPackagePath => AssemblyFixture.SdkPackagePath;
    public string BuildPackagePath => AssemblyFixture.BuildPackagePath;
    public string SdkVersion => AssemblyFixture.SdkVersion;
    public string BuildVersion => AssemblyFixture.BuildVersion;
    public string SharedDatabaseProjectPath => AssemblyFixture.SharedDatabaseProjectPath;

    public string GetTestFixturesPath() => AssemblyFixture.TestFixturesPath;
}

// Collection definitions for parallel test execution
// Tests in different collections run in parallel, tests within a collection run sequentially
// SDK tests are marked with DisableParallelization to prevent NuGet package file locking conflicts

[CollectionDefinition("SDK Net8.0 Tests", DisableParallelization = true)]
public class SdkNet80TestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("SDK Net9.0 Tests", DisableParallelization = true)]
public class SdkNet90TestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("SDK Net10.0 Tests", DisableParallelization = true)]
public class SdkNet100TestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("Build Package Tests", DisableParallelization = true)]
public class BuildPackageTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("Package Content Tests", DisableParallelization = true)]
public class PackageContentTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("Code Generation Tests", DisableParallelization = true)]
public class CodeGenerationTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

[CollectionDefinition("SQL Generation Tests", DisableParallelization = true)]
public class SqlGenerationTestCollection : ICollectionFixture<SdkPackageTestFixture> { }

// Legacy collection for backwards compatibility
[CollectionDefinition("SDK Package Tests", DisableParallelization = true)]
public class SdkPackageTestCollection : ICollectionFixture<SdkPackageTestFixture> { }
